using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    public static class MsCodeCoverageIOperationExtensions
    {
        public static IEnumerable<Uri> GetRunSettingsMsDataCollectorResultUri(this IOperation operation)
        {
            return operation.GetRunSettingsDataCollectorResultUri(new Uri(RunSettingsHelper.MsDataCollectorUri));
        }
    }
}
