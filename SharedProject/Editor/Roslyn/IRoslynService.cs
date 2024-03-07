using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.Roslyn
{
    internal interface IRoslynService
    {
        Task<List<TextSpan>> GetContainingCodeSpansAsync(ITextSnapshot textSnapshot);
    }
}
