using System.IO;
using BMBF.Backend.Util;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Xunit;

namespace BMBF.Backend.Tests;

public class CertificateProviderTests
{
    [Fact]
    public void ShouldBeValidCertificateKeyPair()
    {
        X509Certificate? cert = null;
        AsymmetricKeyParameter? privateKey = null;
        using var reader = new StringReader(CertificateProvider.DebugCertificate);

        var pemReader = new PemReader(reader);
        object pemObject;
        while ((pemObject = pemReader.ReadObject()) != null)
        {
            cert ??= pemObject as X509Certificate;
            privateKey ??= (pemObject as AsymmetricCipherKeyPair)?.Private;
        }

        // Make sure that the debug certificate contains both a public cert and private key
        Assert.NotNull(cert);
        Assert.NotNull(privateKey);
    }
}