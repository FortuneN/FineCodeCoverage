using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using System.Linq;

namespace FineCodeCoverage.Impl
{
    class CSharpContainingCodeVisitor : CSharpSyntaxVisitor, ILanguageContainingCodeVisitor
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
                spans.Add(node.FullSpan);
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
            if (!IsAbstract(node.Modifiers) && node.AccessorList != null)
            {
                foreach (var accessor in node.AccessorList.Accessors)
                {
                    spans.Add(accessor.FullSpan);
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

        private bool IsAbstract(SyntaxTokenList modifiers)
        {
            return modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword));
        }
    }

}
