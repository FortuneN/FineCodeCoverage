namespace FineCodeCoverage.Output
{
    internal static class StatusMarkerProvider
    {
        internal static string Get(string status = "")
        {
            status = status == "" ? "" : $" {status} ";
            return $"=================================={status.ToUpper()}==================================";
        }
    }
}
