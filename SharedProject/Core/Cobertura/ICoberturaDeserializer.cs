namespace FineCodeCoverage.Engine.Cobertura
{
    internal interface ICoberturaDeserializer
    {
        CoverageReport Deserialize(string xmlFile);
    }
}
