using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    [Export(typeof(IRazorGeneratedDocumentRootFinder))]
    internal class RazorGeneratedDocumentRootFinder : IRazorGeneratedDocumentRootFinder
    {
        private readonly IRazorGeneratedFilePathMatcher razorGeneratedFilePathMatcher;

        [ImportingConstructor]
        public RazorGeneratedDocumentRootFinder(IRazorGeneratedFilePathMatcher razorGeneratedFilePathMatcher) 
            => this.razorGeneratedFilePathMatcher = razorGeneratedFilePathMatcher;

        public async Task<SyntaxNode> FindSyntaxRootAsync(ITextBuffer textBuffer, string filePath)
        {
            Workspace ws = textBuffer.GetWorkspace();
            if (ws != null)
            {
                IEnumerable<Project> projects = ws.CurrentSolution.Projects;
                foreach (Project project in projects)
                {
                    foreach (Document document in project.Documents)
                    {
                        string docFilePath = document.FilePath;
                        if (this.razorGeneratedFilePathMatcher.IsRazorGeneratedFilePath(filePath,docFilePath))
                        {   
                            return await document.GetSyntaxRootAsync();
                        }
                    }
                }
            }

            return null;
        }
    }
}
