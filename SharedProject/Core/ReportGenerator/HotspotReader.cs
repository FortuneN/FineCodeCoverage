using Esprima.Ast;
using Esprima;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HtmlAgilityPack;
using System.Linq;
using System.Reflection;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    public static class HotspotProperties
    {
        public static readonly List<PropertyInfo> Get = typeof(Hotspot).GetProperties().ToList();
    }

    internal class Metric
    {
        public string Name { get; set; }
        public double? Value { get; set; }
        public bool Exceeded { get; set; }
    }
    internal class Hotspot
    {
        public string Assembly { get; set; }
        public string Class { get; set; }
        public string MethodName { get; set; }
        public string ShortName { get; set; }
        public double FileIndex { get; set; }
        public double? Line { get; set; }
        public List<Metric> Metrics { get; set; }
    }
    class RiskHotspotVars
    {
        public string RiskHotspotMetrics { get; set; }
        public string RiskHotspots { get; set; }
    }
    internal class HotspotReader
    {
        public List<Hotspot> Read(HtmlDocument doc)
        {
            var vars = ReadHotspotsVars(doc);
            if (vars == null)
            {
                return new List<Hotspot>();
            }
            return UseEsprima(vars);

        }

        private List<Metric> GetMetrics(ArrayExpression arrayExpression, string[] metricNames)
        {
            return arrayExpression.Elements.Select((elementExpression,index) =>
            {
                var objectExpression = elementExpression as ObjectExpression;
                var metric = new Metric { Name = metricNames[index] };

                var properties = objectExpression.Properties;
                properties.ToList().ForEach(node =>
                {
                    var property = node as Property;
                    var propertyName = GetPropertyName(node).ToLower();
                    var literalValue = property.Value as Literal;
                    switch (propertyName)
                    {
                        case "exceeded":
                            metric.Exceeded = literalValue.BooleanValue.Value;
                            break;
                        case "value":
                            if (literalValue.NumericValue.HasValue)
                            {
                                metric.Value = literalValue.NumericValue.Value;
                            }
                            break;
                    }
                    
                });
                return metric;
            }).ToList();

        }

        private string GetPropertyName(Node node)
        {
            var property = node as Property;
            var pNameLiteral = property.Key as Literal;
            if (pNameLiteral.TokenType != TokenType.StringLiteral)
            {
                throw new Exception("Unexpected");
            }
            return pNameLiteral.StringValue as string;
        }

        private IEnumerable<ObjectExpression> GetArrayObjects(ArrayExpression arrayExpression)
        {
            return arrayExpression.Elements.Select(expression =>
            {
                return expression as ObjectExpression;
            });
        }

        private List<Hotspot> MapHotspot(ArrayExpression riskHotspots, string[] metricNames)
        {
            return GetArrayObjects(riskHotspots).Select(objectExpression =>
            {
                var hotspot = new Hotspot();
                var properties = objectExpression.Properties;
                properties.ToList().ForEach(node =>
                {
                    var property = node as Property;
                    var pNameLiteral = property.Key as Literal;
                    var pName = GetPropertyName(node);
                    var hotspotProperty = HotspotProperties.Get.FirstOrDefault(p => p.Name.ToLower() == pName.ToLower());
                    if (hotspotProperty != null)
                    {
                        var pNameType = pNameLiteral.Type;
                        var pValue = property.Value;
                        var pValueType = pValue.Type;
                        object value = null;
                        switch (pValueType)
                        {
                            case Nodes.ArrayExpression:
                                value = GetMetrics(pValue as ArrayExpression,metricNames);
                                break;
                            case Nodes.Literal:
                                var literal = pValue as Literal;
                                var tokenType = literal.TokenType;
                                switch (tokenType)
                                {
                                    case TokenType.BooleanLiteral:
                                        value = literal.BooleanValue.Value;
                                        break;
                                    case TokenType.NumericLiteral:
                                        value = literal.NumericValue.Value;
                                        break;
                                    case TokenType.StringLiteral:
                                        value = literal.StringValue as string;
                                        break;
                                    case TokenType.NullLiteral:
                                        break;
                                    default:
                                        throw new Exception("Unexpected");
                                }

                                break;
                            default:
                                throw new Exception("Unexpected");
                        }
                        hotspotProperty.SetValue(hotspot, value);

                    }
                });


                return hotspot;
            }).ToList();
        }

        private List<Hotspot> UseEsprima(RiskHotspotVars riskHotspotVars)
        {
            var metricNames = ParseMetricNames(ParseArray(riskHotspotVars.RiskHotspotMetrics));
            var riskHotspots = ParseArray(riskHotspotVars.RiskHotspots);
            return MapHotspot(riskHotspots, metricNames);
        }

        private string[] ParseMetricNames(ArrayExpression riskHotspotMetrics)
        {
            return GetArrayObjects(riskHotspotMetrics).Select(objectExpression =>
            {
                var hotspot = new Hotspot();
                var properties = objectExpression.Properties;

                var nameProperty = properties.ToList().First() as Property;
                var value = nameProperty.Value as Literal;
                return value.StringValue;
            }).ToArray();
            
        }

        private ArrayExpression ParseArray(string code)
        {
            var javascriptParser = new JavaScriptParser();
            var script = javascriptParser.ParseScript(code);
            var variableDeclaration = script.ChildNodes.First() as VariableDeclaration;
            var declarations = variableDeclaration.Declarations;
            var declaration = declarations.First();
            return declaration.Init as ArrayExpression;
        }

        private RiskHotspotVars ReadHotspotsVars(HtmlDocument doc)
        {
            var scriptElements = doc.DocumentNode.Descendants("script");
            if (scriptElements != null)
            {
                var scriptElement = scriptElements.First();
                var script = scriptElement.InnerText;

                var riskHotspotMetrics = ReadArrayVariableAsString(script, "var riskHotspotMetrics = [");
                if(riskHotspotMetrics == null)
                {
                    return null;
                }
                var riskhotspots = ReadArrayVariableAsString(script, "var riskHotspots = [");
                if (riskhotspots == null)
                {
                    return null;
                }
                return new RiskHotspotVars { RiskHotspotMetrics = riskHotspotMetrics, RiskHotspots = riskhotspots };
            }
            return null;
        }

        // todo change so does not rely on opening bracket
        private string ReadArrayVariableAsString(string script, string findString)
        {
            var riskhotspotsElementIndex = script.IndexOf(findString);
            if (riskhotspotsElementIndex != -1)
            {
                var result = findString;
                var toRead = script.Substring(riskhotspotsElementIndex + findString.Length);
                using (var sr = new StringReader(toRead))
                {
                    string line;
                    var bracketCount = 1;
                    while ((line = sr.ReadLine()) != null && bracketCount != 0)
                    {
                        foreach (var c in line)
                        {
                            if (c == '[')
                            {
                                bracketCount++;
                            }
                            if (c == ']')
                            {
                                bracketCount--;
                            }
                        }
                        result += line;

                    }
                }
                return result;
            }
            return null;
        }
        
    }

}
