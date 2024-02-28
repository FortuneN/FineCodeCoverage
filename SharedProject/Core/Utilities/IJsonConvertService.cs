using System;

namespace FineCodeCoverage.Core.Utilities
{
    internal interface IJsonConvertService
    {
        object DeserializeObject(string serialized, Type propertyType);
        T DeserializeObject<T>(string serialized);
        string SerializeObject(object objValue);
    }
}
