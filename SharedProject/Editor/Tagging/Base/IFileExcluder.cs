using System;
using System.Collections.Generic;
using System.Text;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal interface IFileExcluder
    {
        string ContentTypeName { get; }
        bool Exclude(string filePath);
    }
}
