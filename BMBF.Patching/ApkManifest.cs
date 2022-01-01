using System;
using System.Collections.Generic;
using System.Linq;
using QuestPatcher.Axml;

namespace BMBF.Patching
{
    public class ApkManifest
    {
        /// <summary>
        /// Manifest/root element of AndroidManifest.xml
        /// </summary>
        public AxmlElement Manifest { get; }
        
        /// <summary>
        /// <code>application</code> element
        /// </summary>
        public AxmlElement ApplicationElement { get; }

        private readonly Dictionary<string, AxmlElement> _existingPermissions;
        private readonly Dictionary<string, AxmlElement> _existingFeatures;


        private static readonly Uri AndroidNamespace = new Uri("http://schemas.android.com/apk/res/android");
        private const int NameAttributeResourceId = 16842755;
        private const int RequiredAttributeResourceId = 16843406;
        private const int DebuggableAttributeResourceId = 16842767;
        private const int LegacyStorageAttributeResourceId = 16844291;

        internal ApkManifest(AxmlElement rootElement)
        {
            if (rootElement.Name != "manifest")
            {
                throw new FormatException("Root element of manifest was not a 'manifest' tag");
            }
            
            Manifest = rootElement;
            ApplicationElement = rootElement.Children.Single(e => e.Name == "application");
            _existingPermissions = GetExistingChildren(Manifest, "uses-permission");
            _existingFeatures = GetExistingChildren(Manifest, "uses-feature");
        }
        
        /// <summary>
        /// Scans the attributes of the children of the given element for their "name" attribute.
        /// </summary>
        /// <param name="manifest">Manifest to scan</param>
        /// <param name="childNames">Names of the children to return the name attributes of</param>
        /// <returns>A set of the values of the "name" attributes of children (does not error on children without this attribute)</returns>
        private Dictionary<string, AxmlElement> GetExistingChildren(AxmlElement manifest, string childNames)
        {
            var result = new Dictionary<string, AxmlElement>();

            foreach (AxmlElement element in manifest.Children)
            {
                if (element.Name != childNames)
                {
                    continue;
                }

                var nameAttributes = element.Attributes.Where(attribute => attribute.Namespace == AndroidNamespace && attribute.Name == "name").ToList();
                if (nameAttributes.Count > 0)
                {
                    result[(string)nameAttributes[0].Value] = element;
                }
            }

            return result;
        }

        /// <summary>
        /// Adds a permission to the manifest
        /// </summary>
        /// <param name="name">Name of the permission to add</param>
        public void AddPermission(string name)
        {
            if (_existingPermissions.ContainsKey(name))
            {
                return;
            }
            
            var permElement = new AxmlElement("uses-permission");
            permElement.Attributes.Add(new AxmlAttribute("name", AndroidNamespace, NameAttributeResourceId, name));
            Manifest.Children.Add(permElement);
            _existingPermissions[name] = permElement;
        }
        
        /// <summary>
        /// Adds a feature to the manifest
        /// </summary>
        /// <param name="name">Name of the feature to add</param>
        /// <param name="required">Whether or not the feature will be set as required</param>
        public void AddFeature(string name, bool required = false)
        {
            // If the feature already exists, simply set the required attribute to the right value
            if (_existingFeatures.TryGetValue(name, out var element))
            {
                element.Attributes.Single(attr => attr.Namespace == AndroidNamespace && attr.Name == "required").Value = required;
            }
            else
            {
                // Otherwise, we'll add a new feature attribute to the manifest
                var featElement = new AxmlElement("uses-feature");
                featElement.Attributes.Add(new AxmlAttribute("name", AndroidNamespace, NameAttributeResourceId, name));
                featElement.Attributes.Add(new AxmlAttribute("required", AndroidNamespace, RequiredAttributeResourceId, required));
                Manifest.Children.Add(featElement);
                _existingFeatures[name] = featElement;
            }
        }

        private void SetBooleanManifestAttribute(string name, int resourceId, bool value)
        {
            var attribute = Manifest.Attributes.FirstOrDefault(e => e.Name == name);
            if (attribute != null)
            {
                // If we already have an attribute, simply set its value
                attribute.Value = value;
            }
            else if(value) // Only bother adding the attribute if we actually want the APK to have it be true
            {
                Manifest.Attributes.Add(new AxmlAttribute(name, AndroidNamespace, resourceId, value));
            }
        }

        /// <summary>
        /// Sets whether or not to make the APK debuggable
        /// </summary>
        /// <param name="debuggable">Whether or not to make the APK debuggable</param>
        public void SetDebuggable(bool debuggable)
        {
            SetBooleanManifestAttribute("debuggable", DebuggableAttributeResourceId, debuggable);
        }

        /// <summary>
        /// Sets whether or not the APK will request legacy external storage privileges
        /// </summary>
        /// <param name="externalStorage">Whether or not the APK will request legacy external storage privileges</param>
        public void SetRequestLegacyExternalStorage(bool externalStorage)
        {
            SetBooleanManifestAttribute("requestLegacyExternalStorage", LegacyStorageAttributeResourceId, externalStorage);
        }
    }
}