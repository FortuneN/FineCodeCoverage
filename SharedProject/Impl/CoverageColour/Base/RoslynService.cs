using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IRoslynService))]
    internal class RoslynService : IRoslynService
    {
        public async Task<List<TextSpan>> GetContainingCodeSpansAsync(ITextSnapshot textSnapshot)
        {
            var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var language = document.Project.Language;
                var isCSharp = language == LanguageNames.CSharp;
                var root = await document.GetSyntaxRootAsync();
                var languageContainingCodeVisitor = isCSharp ? new CSharpContainingCodeVisitor() as ILanguageContainingCodeVisitor : new VBContainingCodeVisitor();
                return languageContainingCodeVisitor.GetSpans(root);
            }
            return Enumerable.Empty<TextSpan>().ToList();
        }
    }
}
