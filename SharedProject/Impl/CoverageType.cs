using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
{
    internal enum CoverageType { Covered, Partial, NotCovered }

    internal static class CoverageLineExtensions
    {
        public static CoverageType GetCoverageType(this CoverageLine coverageLine)
        {
			var line = coverageLine?.Line;
			var lineHitCount = line?.Hits ?? 0;
			var lineConditionCoverage = line?.ConditionCoverage?.Trim();

			var coverageType = CoverageType.NotCovered;

			if (lineHitCount > 0)
			{
				coverageType = CoverageType.Covered;

				if (!string.IsNullOrWhiteSpace(lineConditionCoverage) && !lineConditionCoverage.StartsWith("100"))
				{
					coverageType = CoverageType.Partial;
				}
			}

			return coverageType;
		}
    }
}
