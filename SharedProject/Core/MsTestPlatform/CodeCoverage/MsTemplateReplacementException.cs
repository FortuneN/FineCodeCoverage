using System;
using System.Xml;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    public class MsTemplateReplacementException : Exception
    {
        private readonly XmlException innerException;
        private readonly string replacedRunSettingsTemplate;
        public MsTemplateReplacementException(XmlException innerException, string replacedRunSettingsTemplate)
        {
            this.innerException = innerException;
            this.replacedRunSettingsTemplate = replacedRunSettingsTemplate;
        }

        public override string ToString()
        {
            return $@"${innerException} 
Replaced template :
${replacedRunSettingsTemplate}
";
        }

    }
}
