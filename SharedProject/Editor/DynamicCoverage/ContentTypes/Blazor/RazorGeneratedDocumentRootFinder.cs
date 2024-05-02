using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    [Export(typeof(IRazorGeneratedDocumentRootFinder))]
    internal class RazorGeneratedDocumentRootFinder : IRazorGeneratedDocumentRootFinder
    {
        public async Task<SyntaxNode> FindSyntaxRootAsync(ITextBuffer textBuffer, string fileName)
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
                        string docFileName = Path.GetFileName(docFilePath);
                        if (docFileName.StartsWith(fileName))
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
