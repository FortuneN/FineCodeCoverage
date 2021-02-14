namespace FineCodeCoverage.Impl
{
    internal interface IInitializeStatusProvider
    {
        InitializeStatus InitializeStatus { get; set; }
        string InitializeExceptionMessage { get; set; }
    }

}

