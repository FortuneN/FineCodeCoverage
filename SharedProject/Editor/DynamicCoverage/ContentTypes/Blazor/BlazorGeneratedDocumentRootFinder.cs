using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IBlazorGeneratedDocumentRootFinder))]
    internal class BlazorGeneratedDocumentRootFinder : IBlazorGeneratedDocumentRootFinder
    {
        public async Task<SyntaxNode> FindSyntaxRootAsync(ITextBuffer textBuffer, string filePath, IBlazorGeneratedFilePathMatcher blazorGeneratedFilePathMatcher)
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
                        if (blazorGeneratedFilePathMatcher.IsBlazorGeneratedFilePath(filePath,docFilePath))
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
