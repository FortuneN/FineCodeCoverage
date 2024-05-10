using System;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage.Utilities
{
    [Export(typeof(IDateTimeService))]
    internal class DateTimeService : IDateTimeService
    {
        public DateTime Now => DateTime.Now;
    }
}
