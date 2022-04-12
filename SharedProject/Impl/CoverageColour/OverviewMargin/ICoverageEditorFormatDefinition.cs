using System.Windows;
using System.Windows.Media;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverageEditorFormatDefinition
    {
        string Identifier { get; }
        CoverageType CoverageType { get; }
        void SetBackgroundColor(Color backgroundColor);
        ResourceDictionary CreateResourceDictionary();
    }
}
