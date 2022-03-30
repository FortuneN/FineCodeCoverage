using FineCodeCoverage.Core.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(IFCCSettingsFilesProvider))]
    internal class FCCSettingsFilesProvider : IFCCSettingsFilesProvider
    {
        internal const string fccOptionsFileName = "finecodecoverage-settings.xml";
        private const string topLevelAttributeName = "topLevel";
        private readonly IFileUtil fileUtil;

        [ImportingConstructor]
        public FCCSettingsFilesProvider(
            IFileUtil fileUtil
        )
        {
            this.fileUtil = fileUtil;
        }

        public List<XElement> Provide(string projectPath)
        {
            var fccOptionsElements = new List<XElement>();
            var directoryPath = projectPath;
            var ascend = true;
            while (ascend)
            {
                ascend = AddFromDirectory(fccOptionsElements, directoryPath);
                if (ascend)
                {
                    directoryPath = fileUtil.DirectoryParentPath(directoryPath);
                    if (directoryPath == null)
                    {
                        ascend = false;
                    }
                }

            }

            fccOptionsElements.Reverse();
            return fccOptionsElements;

        }

        private bool AddFromDirectory(List<XElement> fccOptionsElements, string directory)
        {
            var ascend = true;
            var fccOptionsPath = GetFCCOptionsPath(directory);
            if (fileUtil.Exists(fccOptionsPath))
            {
                var fccOptions = fileUtil.ReadAllText(fccOptionsPath);
                try
                {
                    var element = XElement.Parse(fccOptions);
                    fccOptionsElements.Add(element);
                    ascend = !IsTopLevel(element);
                }
                catch
                {

                }
            }

            return ascend;
        }


        private bool IsTopLevel(XElement root)
        {
            var topLevel = false;
            var topLevelAttribute = root.Attribute(topLevelAttributeName);
            if (topLevelAttribute != null && topLevelAttribute.Value.ToLower() == "true")
            {
                topLevel = true;
            }
            return topLevel;
        }

        private string GetFCCOptionsPath(string directory)
        {
            return Path.Combine(directory, fccOptionsFileName);
        }

    }

}
