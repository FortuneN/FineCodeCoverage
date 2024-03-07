using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Core.Utilities
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IJsonConvertService))]
    public class JsonConvertService : IJsonConvertService
    {
        public object DeserializeObject(string serialized, Type propertyType)
        {
            return JsonConvert.DeserializeObject(serialized, propertyType);
        }

        public T DeserializeObject<T>(string serialized)
        {
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }
    }
}
