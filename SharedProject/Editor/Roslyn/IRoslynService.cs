using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Editor.Roslyn
{
    interface IRoslynService
    {
        Task<List<TextSpan>> GetContainingCodeSpansAsync(ITextSnapshot textSnapshot);
    }
}
