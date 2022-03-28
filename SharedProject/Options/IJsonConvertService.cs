using System;

namespace FineCodeCoverage.Options
{
    internal interface IJsonConvertService
    {
        object DeserializeObject(string strValue, Type propertyType);
        string SerializeObject(object objValue);
    }
}
