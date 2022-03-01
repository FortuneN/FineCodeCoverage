using System;

namespace FineCodeCoverage.Engine
{
    interface ICoverageToolOutputFolderSolutionProvider
    {
        string Provide(Func<string> solutionFolderProvider);
    }
}
