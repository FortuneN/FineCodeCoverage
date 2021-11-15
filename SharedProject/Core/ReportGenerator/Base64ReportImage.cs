using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExCSS;
using Svg;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    public class Base64ReportImage
    {
        private readonly string originalBase64;

        public Base64ReportImage(string selector, string originalBase64)
        {
            this.Selector = selector;
            this.originalBase64 = originalBase64;
        }

        public string Selector { get; }

        public string Base64FromColour(string fillColor)
        {
            System.Drawing.Color fillColorColor;
            if (fillColor.StartsWith("#"))
            {
                fillColorColor = System.Drawing.ColorTranslator.FromHtml(fillColor);
            }
            else
            {
                fillColor = fillColor.Substring(5);
                fillColor = fillColor.Replace(")", string.Empty);
                var parts = fillColor.Split(new string[] { "," }, StringSplitOptions.None);
                fillColorColor = System.Drawing.Color.FromArgb(int.Parse(parts[3]), int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
            }

            return Base64Full(GetBase64WithColor(fillColorColor));
        }

        public void FillSvg(IEnumerable<IStyleRule> styleRules, string fillColor)
        {
            var rule = styleRules.First(r => r.SelectorText == Selector);
            rule.Style.BackgroundImage = Base64FromColour(fillColor);
        }

        private string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes

                  = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);

            string returnValue

                  = System.Convert.ToBase64String(toEncodeAsBytes);

            return returnValue;
        }

        private string GetBase64WithColor(System.Drawing.Color color)
        {
            string encoded = null;
            var bytes = Convert.FromBase64String(originalBase64);
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                var svgDoc = SvgDocument.Open<SvgDocument>(ms);
                var child = svgDoc.Children[0];
                child.Fill = new SvgColourServer(color);
                var svgDocString = svgDoc.GetXML();
                encoded = EncodeTo64(svgDocString);
            }

            return encoded;
        }

        private string Base64Full(string base64)
        {
            return $"url(data:image/svg+xml;base64,{base64})";
        }
    }

}
