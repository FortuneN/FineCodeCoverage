using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.DynamicCoverage.Utilities;
using FineCodeCoverage.Editor.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    [Export(typeof(IBlazorFileCodeSpanRangeService))]
    internal class BlazorFileCodeSpanRangeService : IBlazorFileCodeSpanRangeService
    {
        private readonly IBlazorGeneratedDocumentRootFinder blazorGeneratedDocumentRootFinder;
        private readonly ICSharpCodeCoverageNodeVisitor cSharpCodeCoverageNodeVisitor;
        private readonly ISyntaxNodeLocationMapper syntaxNodeLocationMapper;
        private readonly ITextInfoFactory textInfoFactory;
        private readonly IBlazorGeneratedFilePathMatcher blazorGeneratedFilePathMatcher;
        private readonly IThreadHelper threadHelper;

        [ImportingConstructor]
        public BlazorFileCodeSpanRangeService(
            IBlazorGeneratedDocumentRootFinder blazorGeneratedDocumentRootFinder,
            ICSharpCodeCoverageNodeVisitor cSharpCodeCoverageNodeVisitor,
            ISyntaxNodeLocationMapper syntaxNodeLocationMapper,
            ITextInfoFactory textInfoFactory,
            IBlazorGeneratedFilePathMatcher blazorGeneratedFilePathMatcher,
            IThreadHelper threadHelper
        )
        {
            this.blazorGeneratedDocumentRootFinder = blazorGeneratedDocumentRootFinder;
            this.cSharpCodeCoverageNodeVisitor = cSharpCodeCoverageNodeVisitor;
            this.syntaxNodeLocationMapper = syntaxNodeLocationMapper;
            this.textInfoFactory = textInfoFactory;
            this.blazorGeneratedFilePathMatcher = blazorGeneratedFilePathMatcher;
            this.threadHelper = threadHelper;
        }

        public List<CodeSpanRange> GetFileCodeSpanRanges(ITextSnapshot snapshot)
        {
            string filePath = this.textInfoFactory.GetFilePath(snapshot.TextBuffer);
            SyntaxNode generatedDocumentSyntaxRoot = this.threadHelper.JoinableTaskFactory.Run(
                () => this.blazorGeneratedDocumentRootFinder.FindSyntaxRootAsync(snapshot.TextBuffer, filePath, this.blazorGeneratedFilePathMatcher)
            );
            if (generatedDocumentSyntaxRoot != null)
            {
               
                List<SyntaxNode> nodes = this.cSharpCodeCoverageNodeVisitor.GetNodes(generatedDocumentSyntaxRoot);
                if(nodes.Count == 0)
                {
                    return null; // sometimes the generated document has not been generated
                }

                return nodes.Select(node => new { Node = node, MappedLineSpan = this.syntaxNodeLocationMapper.Map(node) })
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
