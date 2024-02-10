using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace FineCodeCoverage.Editor.Management
{
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ProvideTextMarker : RegistrationAttribute
    {
        private readonly string _markerName, _markerGUID, _markerProviderGUID, _displayName;

        public ProvideTextMarker(string markerName, string displayName, string markerGUID, string markerProviderGUID)
        {
            Contract.Requires(markerName != null);
            Contract.Requires(markerGUID != null);
            Contract.Requires(markerProviderGUID != null);

            _markerName = markerName;
            _displayName = displayName;
            _markerGUID = markerGUID;
            _markerProviderGUID = markerProviderGUID;
        }

        public override void Register(RegistrationContext context)
        {
            Key markerkey = context.CreateKey("Text Editor\\External Markers\\" + _markerGUID);
            markerkey.SetValue("", _markerName);
            markerkey.SetValue("Service", "{" + _markerProviderGUID + "}");
            markerkey.SetValue("DisplayName", _displayName);
            markerkey.SetValue("Package", "{" + context.ComponentType.GUID + "}");
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey("Text Editor\\External Markers\\" + _markerGUID);
        }
    }


}
