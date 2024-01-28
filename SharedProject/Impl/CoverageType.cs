using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    internal enum CoverageType { Covered, Partial, NotCovered }

    internal interface ICoverageLineCoverageTypeInfo
    {
        CoverageType CoverageType { get; }
		string EditorFormatDefinitionName { get; }
    }

	internal class CoverageLineCoverageTypeInfo : ICoverageLineCoverageTypeInfo
	{
		public CoverageLineCoverageTypeInfo(CoverageType coverageType, string editorFormatDefinitionName)
		{
            CoverageType = coverageType;
            EditorFormatDefinitionName = editorFormatDefinitionName;
        }

        public CoverageType CoverageType { get; }

        public string EditorFormatDefinitionName { get; }
    }

    internal interface ICoverageLineCoverageTypeInfoHelper
	{
        ICoverageLineCoverageTypeInfo GetInfo(Engine.Cobertura.Line line);
        

    }

    [Export(typeof(ICoverageLineCoverageTypeInfoHelper))]
    internal class CoverageLineCoverageTypeInfoHelper : ICoverageLineCoverageTypeInfoHelper
    {
        private readonly ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames;

        [ImportingConstructor]
        public CoverageLineCoverageTypeInfoHelper(
            ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames)
        {
            this.coverageColoursEditorFormatMapNames = coverageColoursEditorFormatMapNames;
        }
        public ICoverageLineCoverageTypeInfo GetInfo(Line line)
        {
            var coverageType = GetCoverageType(line);
            
            return new CoverageLineCoverageTypeInfo(coverageType, GetEditorFormatDefinitionName(coverageType));

        }

        private string GetEditorFormatDefinitionName(CoverageType coverageType)
        {
            var editorFormatDefinitionName = coverageColoursEditorFormatMapNames.CoverageTouchedArea;
            switch (coverageType)
            {
                case CoverageType.Partial:
                    editorFormatDefinitionName = coverageColoursEditorFormatMapNames.CoveragePartiallyTouchedArea;
                    break;
                case CoverageType.NotCovered:
                    editorFormatDefinitionName = coverageColoursEditorFormatMapNames.CoverageNotTouchedArea;
                    break;
            }
            return editorFormatDefinitionName;
        }

        private static CoverageType GetCoverageType(Engine.Cobertura.Line line)
        {
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

  //  internal static class CoverageLineExtensions
  //  {
  //      public static CoverageType GetCoverageType(this Engine.Cobertura.Line line)
  //      {
		//	var lineHitCount = line?.Hits ?? 0;
		//	var lineConditionCoverage = line?.ConditionCoverage?.Trim();

		//	var coverageType = CoverageType.NotCovered;

		//	if (lineHitCount > 0)
		//	{
		//		coverageType = CoverageType.Covered;

		//		if (!string.IsNullOrWhiteSpace(lineConditionCoverage) && !lineConditionCoverage.StartsWith("100"))
		//		{
		//			coverageType = CoverageType.Partial;
		//		}
		//	}

		//	return coverageType;
		//}
  //  }
}
