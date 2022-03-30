using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.Model
{
    internal interface ISettingsMergeLogic
    {
        bool CanMerge(Type type);
        object Merge(Type type,object first, object second);
    }

    public class SettingsMergeLogic : ISettingsMergeLogic
    {
        private interface ITypeMerger
        {
            object Merge(object first, object second);
        }

        private abstract class TypeMerger<T> : ITypeMerger
        {
            public abstract T Merge(T first, T second);

            public object Merge(object first, object second)
            {
                return Merge((T)first, (T)second);
            }
        }

        private class StringArrayMerger : TypeMerger<string[]>
        {
            public override string[] Merge(string[] first, string[] second)
            {
                return first.Concat(second).ToArray();
            }
        }

        private readonly Dictionary<Type, ITypeMerger> typeMergers;

        public SettingsMergeLogic()
        {
            typeMergers = new Dictionary<Type, ITypeMerger>
            {
                { typeof(string[]),new StringArrayMerger()}
            };
        }

        public bool CanMerge(Type type)
        {
            return typeMergers.ContainsKey(type);
        }

        public object Merge(Type type,object first, object second)
        {
            return typeMergers[type].Merge(first, second);
        }
    }

    [Export(typeof(ISettingsMerger))]
    internal class SettingsMerger : ISettingsMerger
    {
        private const bool projectSettingsDefaultMerge = false;
        private const bool settingsFileDefaultMerge = false;
        private const string defaultMergeAttributeName = "defaultMerge";
        private const string mergeAttributeName = "merge";
        private readonly ILogger logger;
        private readonly ISettingsMergeLogic settingsMergeLogic = new SettingsMergeLogic();

        private class SettingsElementDefaultMerge
        {
            public XElement SettingsElement { get; set; }
            public bool DefaultMerge { get; set; }
        }

        private readonly PropertyInfo[] settingsPropertyInfos;
        

        [ImportingConstructor]
        public SettingsMerger(
            ILogger logger
        )
        {
            settingsPropertyInfos = typeof(IAppOptions).GetPublicProperties();
            this.logger = logger;
            
        }

        public IAppOptions Merge(IAppOptions globalOptions, List<XElement> settingsFileElements, XElement projectSettingsElement)
        {
            var settingsElementsWithDefaultMergeStrategy =
                settingsFileElements.Select(e => new SettingsElementDefaultMerge { SettingsElement = e, DefaultMerge = settingsFileDefaultMerge }).ToList();
            if (projectSettingsElement != null)
            {
                settingsElementsWithDefaultMergeStrategy.Add(new SettingsElementDefaultMerge { SettingsElement = projectSettingsElement, DefaultMerge = projectSettingsDefaultMerge });
            }

            if (settingsElementsWithDefaultMergeStrategy.Count != 0)
            {
                Merge(globalOptions, settingsElementsWithDefaultMergeStrategy);
            }

            return globalOptions;
        }

        private void Merge(IAppOptions globalOptions, PropertyInfo settingPropertyInfo, List<SettingsElementDefaultMerge> settingsElementsWithDefaultMergeStrategy)
        {
            var canMerge = settingsMergeLogic.CanMerge(settingPropertyInfo.PropertyType);
            if (canMerge)
            {
                foreach (var settingsElementWithDefaultMerge in settingsElementsWithDefaultMergeStrategy)
                {
                    var settingsElement = settingsElementWithDefaultMerge.SettingsElement;
                    var defaultMerge = GetDefaultMerge(settingsElementWithDefaultMerge.DefaultMerge, settingsElement);
                    var propertyElement = GetPropertyElement(settingsElement, settingPropertyInfo.Name);
                    if (propertyElement != null)
                    {
                        var merge = GetMerge(defaultMerge, propertyElement);
                        if (merge)
                        {
                            Merge(globalOptions, settingPropertyInfo, settingsElement);
                        }
                        else
                        {
                            Overwrite(globalOptions, settingPropertyInfo, settingsElement);
                        }
                    }

                }
            }
            else
            {
                var settingsElements = settingsElementsWithDefaultMergeStrategy.Select(x => x.SettingsElement);
                Overwrite(globalOptions, settingPropertyInfo, settingsElements);
            }
        }

        private void Merge(IAppOptions globalOptions, PropertyInfo settingPropertyInfo, XElement settingsElement)
        {
            var value = TryGetValueFromXml(settingsElement, settingPropertyInfo);
            if (value != null)
            {
                var currentValue = settingPropertyInfo.GetValue(globalOptions);
                object merged;
                if (currentValue == null)
                {
                    merged = value;
                }
                else
                {
                    merged = settingsMergeLogic.Merge(settingPropertyInfo.PropertyType, currentValue, value);
                }

                settingPropertyInfo.SetValue(globalOptions, merged);
            }
        }

        private bool GetMerge(bool defaultMerge, XElement propertyElement)
        {
            var mergeAttribute = propertyElement.Attribute(mergeAttributeName);
            if (mergeAttribute == null)
            {
                return defaultMerge;
            }
            return mergeAttribute.Value.ToLower() == "true";
        }

        private bool GetDefaultMerge(bool defaultDefaultMerge, XElement root)
        {
            var defaultMergeAttribute = root.Attribute(defaultMergeAttributeName);
            if (defaultMergeAttribute == null)
            {
                return defaultDefaultMerge;
            }
            return defaultMergeAttribute.Value.ToLower() == "true";
        }

        private void Overwrite(IAppOptions globalOptions, PropertyInfo settingPropertyInfo, IEnumerable<XElement> settingsElements)
        {
            foreach (var settingsElement in settingsElements)
            {
                Overwrite(globalOptions, settingPropertyInfo, settingsElement);
            }
        }

        private void Overwrite(IAppOptions globalOptions, PropertyInfo settingPropertyInfo, XElement settingsElement)
        {
            var value = TryGetValueFromXml(settingsElement, settingPropertyInfo);
            if (value != null)
            {
                settingPropertyInfo.SetValue(globalOptions, value);
            }
        }

        private XElement GetPropertyElement(XElement settingsElement, string propertyName)
        {
            return settingsElement.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        }

        private object TryGetValueFromXml(XElement settingsElement, PropertyInfo property)
        {
            try
            {
                return GetValueFromXml(settingsElement, property);
            }
            catch (Exception exception)
            {
                logger.Log($"Failed to override '{property.Name}' setting", exception);
            }
            return null;
        }

        internal object GetValueFromXml(XElement settingsElement, PropertyInfo property)
        {
            var xproperty = GetPropertyElement(settingsElement, property.Name);

            if (xproperty == null)
            {
                return null;
            }

            var strValue = xproperty.Value;

            var strValueArr = strValue.Split('\n', '\r').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

            if (TypeMatch(property.PropertyType, typeof(string)))
            {
                var value = strValueArr.FirstOrDefault();
                return value ?? "";
            }
            else if (TypeMatch(property.PropertyType, typeof(string[])))
            {
                return strValueArr;
            }

            else if (TypeMatch(property.PropertyType, typeof(bool), typeof(bool?)))
            {
                if (bool.TryParse(strValueArr.FirstOrDefault(), out bool value))
                {
                    return value;
                }
            }
            else if (TypeMatch(property.PropertyType, typeof(bool[]), typeof(bool?[])))
            {
                var arr = strValueArr.Where(x => bool.TryParse(x, out var _)).Select(x => bool.Parse(x));
                if (arr.Any())
                {
                    return arr;
                }
            }

            else if (TypeMatch(property.PropertyType, typeof(int), typeof(int?)))
            {
                if (int.TryParse(strValueArr.FirstOrDefault(), out var value))
                {
                    return value;
                }
            }
            else if (TypeMatch(property.PropertyType, typeof(int[]), typeof(int?[])))
            {
                var arr = strValueArr.Where(x => int.TryParse(x, out var _)).Select(x => int.Parse(x));
                if (arr.Any())
                {
                    return arr;
                }
            }

            else if (TypeMatch(property.PropertyType, typeof(short), typeof(short?)))
            {
                if (short.TryParse(strValueArr.FirstOrDefault(), out var vaue))
                {
                    return vaue;
                }
            }
            else if (TypeMatch(property.PropertyType, typeof(short[]), typeof(short?[])))
            {
                var arr = strValueArr.Where(x => short.TryParse(x, out var _)).Select(x => short.Parse(x));
                if (arr.Any())
                {
                    return arr;
                }
            }

            else if (TypeMatch(property.PropertyType, typeof(long), typeof(long?)))
            {
                if (long.TryParse(strValueArr.FirstOrDefault(), out var value))
                {
                    return value;
                }
            }
            else if (TypeMatch(property.PropertyType, typeof(long[]), typeof(long?[])))
            {
                var arr = strValueArr.Where(x => long.TryParse(x, out var _)).Select(x => long.Parse(x));
                if (arr.Any())
                {
                    return arr;
                }
            }

            else if (TypeMatch(property.PropertyType, typeof(decimal), typeof(decimal?)))
            {
                if (decimal.TryParse(strValueArr.FirstOrDefault(), out var value))
                {
                    return value;
                }
            }
            else if (TypeMatch(property.PropertyType, typeof(decimal[]), typeof(decimal?[])))
            {
                var arr = strValueArr.Where(x => decimal.TryParse(x, out var _)).Select(x => decimal.Parse(x));
                if (arr.Any())
                {
                    return arr;
                }
            }

            else if (TypeMatch(property.PropertyType, typeof(double), typeof(double?)))
            {
                if (double.TryParse(strValueArr.FirstOrDefault(), out var value))
                {
                    return value;
                }
            }
            else if (TypeMatch(property.PropertyType, typeof(double[]), typeof(double?[])))
            {
                var arr = strValueArr.Where(x => double.TryParse(x, out var _)).Select(x => double.Parse(x));
                if (arr.Any())
                {
                    return arr;
                }
            }

            else if (TypeMatch(property.PropertyType, typeof(float), typeof(float?)))
            {
                if (float.TryParse(strValueArr.FirstOrDefault(), out var value))
                {
                    return value;
                }
            }
            else if (TypeMatch(property.PropertyType, typeof(float[]), typeof(float?[])))
            {
                var arr = strValueArr.Where(x => float.TryParse(x, out var _)).Select(x => float.Parse(x));
                if (arr.Any())
                {
                    return arr;
                }
            }

            else if (TypeMatch(property.PropertyType, typeof(char), typeof(char?)))
            {
                if (char.TryParse(strValueArr.FirstOrDefault(), out var value))
                {
                    return value;
                }
            }
            else if (TypeMatch(property.PropertyType, typeof(char[]), typeof(char?[])))
            {
                var arr = strValueArr.Where(x => char.TryParse(x, out var _)).Select(x => char.Parse(x));
                if (arr.Any())
                {
                    return arr;
                }
            }

            else
            {
                throw new Exception($"Cannot handle '{property.PropertyType.Name}' yet");
            }
            return null;

        }

        private void Merge(IAppOptions globalOptions, List<SettingsElementDefaultMerge> settingsElementsWithDefaultMergeStrategy)
        {
            foreach (var settingsProperty in settingsPropertyInfos)
            {
                Merge(globalOptions, settingsProperty, settingsElementsWithDefaultMergeStrategy);
            }
        }

        private bool TypeMatch(Type type, params Type[] otherTypes)
        {
            return (otherTypes ?? new Type[0]).Any(ot => type == ot);
        }
    }

}
