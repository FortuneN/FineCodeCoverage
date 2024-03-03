using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using System.Linq;

namespace FineCodeCoverage.Editor.Roslyn
{
    class CSharpContainingCodeVisitor : CSharpSyntaxVisitor, ILanguageContainingCodeVisitor
    {
        private readonly List<TextSpan> spans = new List<TextSpan>();
        public List<TextSpan> GetSpans(SyntaxNode rootNode)
        {
            this.Visit(rootNode);
            return this.spans;
        }

#if VS2022
        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            VisitMembers(node.Members);
        }
#endif
        public override void VisitCompilationUnit(CompilationUnitSyntax node) => this.VisitMembers(node.Members);

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) => this.VisitMembers(node.Members);

        public override void VisitClassDeclaration(ClassDeclarationSyntax node) => this.VisitMembers(node.Members);

        public override void VisitStructDeclaration(StructDeclarationSyntax node) => this.VisitMembers(node.Members);

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node) => this.VisitMembers(node.Members);

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => this.VisitMembers(node.Members);

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node) => this.AddIfHasBody(node);

        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node) => this.AddIfHasBody(node);

        public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node) => this.AddIfHasBody(node);

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) => this.AddIfHasBody(node);

        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node) => this.AddIfHasBody(node);

        private void AddIfHasBody(BaseMethodDeclarationSyntax node)
        {
            bool hasBody = node.Body != null || node.ExpressionBody != null;
            if (hasBody)
            {
                this.AddNode(node);
            }
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node) => this.VisitBasePropertyDeclaration(node);

        public override void VisitEventDeclaration(EventDeclarationSyntax node) => this.VisitBasePropertyDeclaration(node);

        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node) => this.VisitBasePropertyDeclaration(node);

        private void VisitBasePropertyDeclaration(BasePropertyDeclarationSyntax node)
        {
            if (!this.IsAbstract(node.Modifiers))
            {
                if(node.AccessorList == null)
                {
                    if(node is PropertyDeclarationSyntax propertyDeclarationSyntax)
                    {
                        this.AddNode(propertyDeclarationSyntax);
                    }
                }
                else
                {
                    bool isInterfaceProperty = node.Parent is InterfaceDeclarationSyntax;
                    foreach (AccessorDeclarationSyntax accessor in node.AccessorList.Accessors)
                    {
                        bool addAccessor = !isInterfaceProperty || this.AccessorHasBody(accessor);
                        if (addAccessor)
                        {
                            this.AddNode(accessor);
                        }
                    }
                }
            }
        }

        private bool AccessorHasBody(AccessorDeclarationSyntax accessor) => accessor.Body != null || accessor.ExpressionBody != null;

        private void VisitMembers(SyntaxList<MemberDeclarationSyntax> members)
        {
            foreach (MemberDeclarationSyntax member in members)
            {
                this.Visit(member);
            }
        }

        private bool IsAbstract(SyntaxTokenList modifiers) => modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword));

        private void AddNode(SyntaxNode node) => this.spans.Add(node.Span);
    }
}
