using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
    interface ILanguageContainingCodeVisitor
    {
        List<TextSpan> GetSpans(SyntaxNode rootNode);
    }

    class CSharpContainingCodeVisitor: CSharpSyntaxVisitor, ILanguageContainingCodeVisitor
    {
        private List<TextSpan> spans = new List<TextSpan>();
        public List<TextSpan> GetSpans(SyntaxNode rootNode)
        {
            Visit(rootNode);
            return spans;
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            AddIfHasBody(node);
        }

        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            AddIfHasBody(node);
        }

        public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            AddIfHasBody(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            AddIfHasBody(node);
        }

        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            AddIfHasBody(node);
        }

        private void AddIfHasBody(BaseMethodDeclarationSyntax node)
        {
            var hasBody = node.Body != null || node.ExpressionBody != null;
            if (hasBody)
            {
                spans.Add(node.Span);
            }
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            VisitBasePropertyDeclaration(node);
        }

        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            VisitBasePropertyDeclaration(node);
        }

        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            VisitBasePropertyDeclaration(node);
        }

        private void VisitBasePropertyDeclaration(BasePropertyDeclarationSyntax node)
        {
            if(node.AccessorList != null)
            {
                foreach(var accessor in node.AccessorList.Accessors)
                {
                    spans.Add(accessor.Span);
                }
            }
        }

        private void VisitMembers(SyntaxList<MemberDeclarationSyntax> members)
        {
            foreach (var member in members)
            {
                Visit(member);
            }
        }

    }

    class VBContainingCodeVisitor : VisualBasicSyntaxVisitor, ILanguageContainingCodeVisitor
    {
        private List<TextSpan> spans = new List<TextSpan>();
        public List<TextSpan> GetSpans(SyntaxNode rootNode)
        {
            Visit(rootNode);
            return spans;
        }
        public override void VisitCompilationUnit(Microsoft.CodeAnalysis.VisualBasic.Syntax.CompilationUnitSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitNamespaceBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.NamespaceBlockSyntax node)
        {
            VisitMembers(node.Members);
        }

        private void VisitMembers(SyntaxList<Microsoft.CodeAnalysis.VisualBasic.Syntax.StatementSyntax> members)
        {
            foreach (var member in members)
            {
                Visit(member);
            }
        }

        public override void VisitClassBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.ClassBlockSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitStructureBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.StructureBlockSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitModuleBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.ModuleBlockSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitConstructorBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.ConstructorBlockSyntax node)
        {
            spans.Add(node.Span);
        }

        public override void VisitMethodBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockSyntax node)
        {
            spans.Add(node.Span);// should check for body - abstract ?
        }

        public override void VisitOperatorBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.OperatorBlockSyntax node)
        {
            spans.Add(node.Span);
        }

        public override void VisitPropertyBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.PropertyBlockSyntax node)
        {
            VisitAccessors(node.Accessors);
        }

        public override void VisitEventBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.EventBlockSyntax node)
        {
            VisitAccessors(node.Accessors);
        }

        private void VisitAccessors(SyntaxList<Microsoft.CodeAnalysis.VisualBasic.Syntax.AccessorBlockSyntax> accessors)
        {
            foreach (var accessor in accessors)
            {
                Visit(accessor);
            }
        }

        public override void VisitAccessorBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.AccessorBlockSyntax node)
        {
            spans.Add(node.Span);
        }
    }

    [Export(typeof(IRoslynService))]
    internal class RoslynService : IRoslynService
    {
        public async Task<List<ContainingCodeLineRange>> GetContainingCodeLineRangesAsync(ITextSnapshot textSnapshot,List<int> list)
        {
            var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var language = document.Project.Language;
                var isCSharp = language == LanguageNames.CSharp;
                var root = await document.GetSyntaxRootAsync();
                var languageContainingCodeVisitor = isCSharp ? new CSharpContainingCodeVisitor() as ILanguageContainingCodeVisitor : new VBContainingCodeVisitor();
                var textSpans = languageContainingCodeVisitor.GetSpans(root);
                return textSpans.Select(textSpan =>
                {
                    var span = textSpan.ToSpan();
                    var startLine = textSnapshot.GetLineFromPosition(span.Start);
                    var endLine = textSnapshot.GetLineFromPosition(span.End);
                    return new ContainingCodeLineRange
                    {
                        StartLine = startLine.LineNumber,
                        EndLine = endLine.LineNumber
                    };
                }).Where(containingCode => containingCode.ContainsAny(list)).ToList();
            }
            return new List<ContainingCodeLineRange>();
        }
    }
}
