using System;

namespace FineCodeCoverage.Editor.Management
{
    internal interface ICoverageFontAndColorsCategoryItemNamesManager
    {
        event EventHandler Changed;
        void Initialize(FCCEditorFormatDefinitionNames fCCEditorFormatDefinitionNames);
        ICoverageFontAndColorsCategoryItemNames ItemNames { get; }
    }
}
