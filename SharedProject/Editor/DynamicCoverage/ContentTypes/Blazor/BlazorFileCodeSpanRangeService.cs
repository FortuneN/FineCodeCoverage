using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    [Export(typeof(IBlazorFileCodeSpanRangeService))]
    internal class BlazorFileCodeSpanRangeService : IBlazorFileCodeSpanRangeService
    {
        private readonly IRazorGeneratedDocumentRootFinder razorGeneratedDocumentRootFinder;
        private readonly ICSharpNodeVisitor cSharpNodeVisitor;
        private readonly ITextInfoFactory textInfoFactory;
        private readonly IThreadHelper threadHelper;

        [ImportingConstructor]
        public BlazorFileCodeSpanRangeService(
            IRazorGeneratedDocumentRootFinder razorGeneratedDocumentRootFinder,
            ICSharpNodeVisitor cSharpNodeVisitor,
            ITextInfoFactory textInfoFactory,
            IThreadHelper threadHelper
        )
        {
            this.razorGeneratedDocumentRootFinder = razorGeneratedDocumentRootFinder;
            this.cSharpNodeVisitor = cSharpNodeVisitor;
            this.textInfoFactory = textInfoFactory;
            this.threadHelper = threadHelper;
        }

        public List<CodeSpanRange> GetFileCodeSpanRanges(ITextSnapshot snapshot)
        {
            string filePath = this.textInfoFactory.Create(null, snapshot.TextBuffer).FilePath;
            SyntaxNode generatedDocumentSyntaxRoot = this.threadHelper.JoinableTaskFactory.Run(
                () => this.razorGeneratedDocumentRootFinder.FindSyntaxRootAsync(snapshot.TextBuffer, filePath)
            );
            if (generatedDocumentSyntaxRoot != null)
            {
                List<SyntaxNode> nodes = this.cSharpNodeVisitor.GetNodes(generatedDocumentSyntaxRoot);
                // will not be able to mock this
                return nodes.Select(node => new { Node = node, MappedLineSpan = node.GetLocation().GetMappedLineSpan() })
                    .Where(a => a.MappedLineSpan.Path == filePath)
                    .Select(a => new CodeSpanRange(
                        a.MappedLineSpan.StartLinePosition.Line,
                        a.MappedLineSpan.EndLinePosition.Line)
                    ).ToList();
            }

            return null;
        }
    }
}
