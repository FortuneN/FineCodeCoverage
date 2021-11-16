using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    internal interface IThemeResourceKeyProvider
    {
        ThemeResourceKey Provide(string reportPart);
    }
}
