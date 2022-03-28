using Newtonsoft.Json;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Options
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IJsonConvertService))]
    public class JsonConvertService : IJsonConvertService
    {
        public object DeserializeObject(string value, Type propertyType)
        {
            return JsonConvert.DeserializeObject(value, propertyType);
        }

        public string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }
    }
}
