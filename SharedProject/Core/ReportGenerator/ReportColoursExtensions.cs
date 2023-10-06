using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FineCodeCoverage.Engine.ReportGenerator
{
	internal static class ReportColoursExtensions
	{
        private class ColourReflection
		{
			public PropertyInfo ReportColoursPropertyInfo { get; set; }
			public FieldInfo JsThemeStylingFieldInfo { get; set; }
		}
		private static List<ColourReflection> colourReflections;
		private static List<ColourReflection> ColourReflections
		{
			get
			{
				if (colourReflections == null)
				{
					var reportColourPropertyInfos = typeof(IReportColours).GetProperties();
					var jsThemeStylingFieldInfos = typeof(JsThemeStyling).GetFields();
					colourReflections = reportColourPropertyInfos.Select(prop =>
					{
						var field = jsThemeStylingFieldInfos.FirstOrDefault(f => f.Name == prop.Name);
						if (field == null)
						{
							return null;
						}
						else
						{
							return new ColourReflection { ReportColoursPropertyInfo = prop, JsThemeStylingFieldInfo = field };
						}
					}).Where(cr => cr != null).ToList();
				}
				return colourReflections;
			}
		}
		public static JsThemeStyling Convert(this IReportColours reportColours)
		{
			var jsThemeStyling = new JsThemeStyling();
			ColourReflections.ForEach(cr =>
			{
				cr.JsThemeStylingFieldInfo.SetValue(jsThemeStyling, ((System.Drawing.Color)cr.ReportColoursPropertyInfo.GetValue(reportColours)).ToJsColour());
			});
			return jsThemeStyling;
		}

		public static string ToJsColour(this System.Drawing.Color colour)
		{
			return $"rgba({colour.R},{colour.G},{colour.B},{colour.A})";
		}

		public static System.Drawing.Color ToColor(string jsColor)
		{
			var rgba = jsColor.Replace("rgba(", "").Replace(")", "").Split(',');
			return System.Drawing.Color.FromArgb(int.Parse(rgba[3]), int.Parse(rgba[0]), int.Parse(rgba[1]), int.Parse(rgba[2]));

		}
        
	}

}
