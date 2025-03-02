using System;
using System.Globalization;
using System.Text;

namespace FineCodeCoverage.Output
{
    internal static class NowForLog
    {
        public static string Get()
        {
            var stringBuilder = new StringBuilder();
            DateTime now = DateTime.Now;
            stringBuilder.Append('[');
            stringBuilder.Append(now.ToString("d", CultureInfo.CurrentCulture));
            stringBuilder.Append(' ');
            stringBuilder.Append(now.ToString("h:mm:ss.fff tt", CultureInfo.CurrentCulture));
            stringBuilder.Append(']');
            stringBuilder.Append(' ');
            return stringBuilder.ToString();
        }
    }
}
