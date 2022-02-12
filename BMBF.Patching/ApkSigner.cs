/*
 
This APK signing code was taken from emulamer's Apkifier library: https://github.com/emulamer/Apkifier/blob/master/Apkifier.cs
MIT License
Copyright (c) 2019 emulamer
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.IO.Pem;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using BigInteger = Org.BouncyCastle.Math.BigInteger;
using PemReader = Org.BouncyCastle.OpenSsl.PemReader;

namespace BMBF.Patching
{
    /// <summary>
    /// Utility for signing APKs
    /// </summary>
    public class ApkSigner : IApkSigner
    {
        private static readonly Encoding Encoding = new UTF8Encoding();
        private static readonly SHA1 Sha = SHA1.Create();

        /// <summary>
        /// Signs the signature file's content using the given certificate, and returns the RSA signature.
        /// </summary>
        /// <param name="signatureFileData">Content of the signature file to be signed</param>
        /// <param name="pemCertData">PEM data of the certificate and private key for signing</param>
        /// <returns>The RSA signature</returns>
        private byte[] GetSignature(byte[] signatureFileData, string pemCertData)
        {
            var (cert, privateKey) = LoadCertificate(pemCertData);

            var certStore = X509StoreFactory.Create("Certificate/Collection", new X509CollectionStoreParameters(new List<X509Certificate> { cert }));
            var dataGen = new CmsSignedDataGenerator();
            dataGen.AddCertificates(certStore);
            dataGen.AddSigner(privateKey, cert, CmsSignedGenerator.EncryptionRsa, CmsSignedGenerator.DigestSha256);

            // Content is detached - i.e. not included in the signature block itself
            var detachedContent = new CmsProcessableByteArray(signatureFileData);
            var signedContent = dataGen.Generate(detachedContent, false);

            // Get the signature in the proper ASN.1 structure for java to parse it properly.  Lots of trial and error
            var signerInfos = signedContent.GetSignerInfos();
            var signer = signerInfos.GetSigners().Cast<SignerInformation>().First();
            var signerInfo = signer.ToSignerInfo();
            var digestAlgorithmsVector = new Asn1EncodableVector();
            digestAlgorithmsVector.Add(new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1"), DerNull.Instance));
            var encapContentInfo = new ContentInfo(new DerObjectIdentifier("1.2.840.113549.1.7.1"), null);
            var asnVector = new Asn1EncodableVector
            {
                X509CertificateStructure.GetInstance(Asn1Object.FromByteArray(cert.GetEncoded()))
            };
            var signersVector = new Asn1EncodableVector { signerInfo.ToAsn1Object() };
            var signedData = new SignedData(new DerSet(digestAlgorithmsVector), encapContentInfo, new BerSet(asnVector), null, new DerSet(signersVector));
            var contentInfo = new ContentInfo(new DerObjectIdentifier("1.2.840.113549.1.7.2"), signedData);
            return contentInfo.GetDerEncoded();
        }

        /// <summary>
        /// Loads the certificate and private key from the given PEM data
        /// </summary>
        /// <param name="pemData"></param>
        /// <returns>The loaded certificate and private key</returns>
        /// <exception cref="System.Security.SecurityException">If the certificate or private key failed to load</exception>
        private static (X509Certificate certificate, AsymmetricKeyParameter privateKey) LoadCertificate(string pemData)
        {
            X509Certificate? cert = null;
            AsymmetricKeyParameter? privateKey = null;
            using (var reader = new StringReader(pemData))
            {
                // Iterate through the PEM objects until we find the public or private key
                var pemReader = new PemReader(reader);
                object pemObject;
                while ((pemObject = pemReader.ReadObject()) != null)
                {
                    cert ??= pemObject as X509Certificate;
                    privateKey ??= (pemObject as AsymmetricCipherKeyPair)?.Private;
                }
            }
            if (cert == null)
                throw new System.Security.SecurityException("Certificate could not be loaded from PEM data.");

            if (privateKey == null)
                throw new System.Security.SecurityException("Private Key could not be loaded from PEM data.");

            return (cert, privateKey);
        }

        /// <summary>
        /// Writes the MANIFEST.MF and signature file hashes for the given entry
        /// </summary>
        private async Task WriteEntryHash(ZipArchiveEntry entry, Stream manifestStream, Stream signatureStream)
        {
            await using Stream sourceStream = entry.Open();
            byte[] hash = Sha.ComputeHash(sourceStream);

            // Write the digest for this entry to the manifest
            await using var sectStream = new MemoryStream();
            await using (var sectWriter = OpenStreamWriter(sectStream))
            {
                await sectWriter.WriteLineAsync($"Name: {entry.FullName}");
                await sectWriter.WriteLineAsync($"SHA1-Digest: {Convert.ToBase64String(hash)}");
                await sectWriter.WriteLineAsync();
            }

            // Sign the section of manifest, then write it to the signature file
            sectStream.Position = 0;
            string sectHash = Convert.ToBase64String(Sha.ComputeHash(sectStream));
            await using (StreamWriter signatureWriter = OpenStreamWriter(signatureStream))
            {
                await signatureWriter.WriteLineAsync($"Name: {entry.FullName}");
                await signatureWriter.WriteLineAsync($"SHA1-Digest: {sectHash}");
                await signatureWriter.WriteLineAsync();
            }

            sectStream.Position = 0;
            await sectStream.CopyToAsync(manifestStream);
        }

        private static StreamWriter OpenStreamWriter(Stream stream)
        {
            return new StreamWriter(stream, Encoding, 1024, true);
        }
        
        public async Task SignApkAsync(IFileSystem fileSystem, string path, string pemData, string signerName, CancellationToken ct)
        {
            // Create streams to save the signature data to during the first path
            await using var manifestFile = new MemoryStream();
            await using var sigFileBody = new MemoryStream();
            await using (var manifestWriter = OpenStreamWriter(manifestFile))
            {
                await manifestWriter.WriteLineAsync("Manifest-Version: 1.0");
                await manifestWriter.WriteLineAsync($"Created-By: {signerName}");
                await manifestWriter.WriteLineAsync();
            }

            // Begin the first pass
            // In this pass, we compute the hash of each file within the APK, and save it in the manifest/sig files
            // Two passes are used since we require ZipArchiveMode.Update to save the hash
            // In this pass, we can open the APK with ZipArchiveMode.Read and avoid increasing the save time in the
            // update pass, as opening a file with ZipArchiveMode.Update triggers a recompression during dispose
            await using (var apkStream = fileSystem.File.OpenRead(path))
            using (var apkArchive = new ZipArchive(apkStream, ZipArchiveMode.Read))
            {
                foreach (var entry in apkArchive.Entries.Where(entry =>
                             !entry.FullName.StartsWith("META-INF"))) // Skip signature related files
                {
                    ct.ThrowIfCancellationRequested();
                    await WriteEntryHash(entry, manifestFile, sigFileBody);
                }
            }

            await using (var apkStream = fileSystem.File.Open(path, FileMode.Open, FileAccess.ReadWrite))
            using (var apkArchive = new ZipArchive(apkStream, ZipArchiveMode.Update))
            {
                // Delete the previous signature first!
                foreach (var entry in apkArchive.Entries.Where(entry => entry.FullName.StartsWith("META-INF")).ToList())
                {
                    entry.Delete();
                }

                await using var signaturesFile = apkArchive.CreateEntry("META-INF/BS.SF").Open();
                await using var rsaFile = apkArchive.CreateEntry("META-INF/BS.RSA").Open();
                await using var manifestStream = apkArchive.CreateEntry("META-INF/MANIFEST.MF").Open();

                // Find the hash of the manifest, then we can finally copy it to MANIFEST.MF
                manifestFile.Position = 0;
                byte[] manifestHash = Sha.ComputeHash(manifestFile);
                manifestFile.Position = 0;
                await manifestFile.CopyToAsync(manifestStream, ct);

                await using (var signatureWriter = OpenStreamWriter(signaturesFile))
                {
                    await signatureWriter.WriteLineAsync("Signature-Version: 1.0");
                    await signatureWriter.WriteLineAsync($"SHA1-Digest-Manifest: {Convert.ToBase64String(manifestHash)}");
                    await signatureWriter.WriteLineAsync($"Created-By: {signerName}");
                    await signatureWriter.WriteLineAsync();
                }

                // Copy the entry hashes into the signature file
                sigFileBody.Position = 0;
                await sigFileBody.CopyToAsync(signaturesFile, ct);
                signaturesFile.Position = 0;

                await using var sigFileMs = new MemoryStream();
                await signaturesFile.CopyToAsync(sigFileMs, ct);

                // Actually sign the digest file, and write to the RSA file
                byte[] keyFile = GetSignature(sigFileMs.ToArray(), pemData);
                await rsaFile.WriteAsync(keyFile, ct);
            }
        }
        
        public string GenerateNewCertificatePem()
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);
            var certificateGenerator = new X509V3CertificateGenerator();
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

#pragma warning disable 618
            certificateGenerator.SetSignatureAlgorithm("SHA256WithRSA");
#pragma warning restore 618
            var subjectDn = new X509Name("cn=Unknown");
            var issuerDn = subjectDn;
            certificateGenerator.SetIssuerDN(issuerDn);
            certificateGenerator.SetSubjectDN(subjectDn);
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date.AddYears(-10));
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(50));
            var keyGenerationParameters = new KeyGenerationParameters(random, 2048);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

#pragma warning disable 618
            X509Certificate cert = certificateGenerator.Generate(subjectKeyPair.Private);
#pragma warning restore 618

            using var writer = new StringWriter();
            var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(writer);

            pemWriter.WriteObject(new PemObject("CERTIFICATE", cert.GetEncoded()));
            pemWriter.WriteObject(subjectKeyPair.Private);
            return writer.ToString();
        }
    }
}
