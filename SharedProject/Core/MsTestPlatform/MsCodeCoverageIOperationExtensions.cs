using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    public static class MsCodeCoverageIOperationExtensions
    {
        public static IEnumerable<Uri> GetRunSettingsMsDataCollectorResultUri(this IOperation operation)
        {
            return operation.GetRunSettingsDataCollectorResultUri(new Uri("datacollector://Microsoft/CodeCoverage/2.0"));
        }
    }
}
