using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace FineCodeCoverage.Editor.Roslyn
{
    [Export(typeof(IRoslynService))]
    internal class RoslynService : IRoslynService
    {
        private readonly ILanguageContainingCodeVisitorFactory languageContainingCodeVisitorFactory;
        private readonly ITextSnapshotToSyntaxService textSnapshotToSyntaxService;

        [ImportingConstructor]
        public RoslynService(
            ILanguageContainingCodeVisitorFactory languageContainingCodeVisitorFactory,
            ITextSnapshotToSyntaxService textSnapshotToSyntaxService)
        {
            this.languageContainingCodeVisitorFactory = languageContainingCodeVisitorFactory;
            this.textSnapshotToSyntaxService = textSnapshotToSyntaxService;
        }
        public async Task<List<TextSpan>> GetContainingCodeSpansAsync(ITextSnapshot textSnapshot)
        {
            RootNodeAndLanguage rootNodeAndLanguage = await this.textSnapshotToSyntaxService.GetRootAndLanguageAsync(textSnapshot);
            if(rootNodeAndLanguage == null)
            {
                return Enumerable.Empty<TextSpan>().ToList();
            }
            
            bool isCSharp = rootNodeAndLanguage.Language == LanguageNames.CSharp;
            ILanguageContainingCodeVisitor languageContainingCodeVisitor = this.languageContainingCodeVisitorFactory.Create(isCSharp);
            return languageContainingCodeVisitor.GetSpans(rootNodeAndLanguage.Root);
        }
    }
}
