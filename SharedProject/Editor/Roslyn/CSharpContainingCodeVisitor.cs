using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FineCodeCoverage.Editor.Roslyn
{
    internal interface ICSharpNodeVisitor
    {
        List<SyntaxNode> GetNodes(SyntaxNode rootNode);
    }

    [Export(typeof(ICSharpNodeVisitor))]
    internal class CSharpContainingCodeVisitor : CSharpSyntaxVisitor, ILanguageContainingCodeVisitor, ICSharpNodeVisitor
    {
        private readonly List<SyntaxNode> nodes = new List<SyntaxNode>();
        public List<TextSpan> GetSpans(SyntaxNode rootNode)
        {
            this.nodes.Clear();
            this.Visit(rootNode);
            return this.nodes.Select(node => node.Span).ToList();
        }

        public List<SyntaxNode> GetNodes(SyntaxNode rootNode)
        {
            this.nodes.Clear();
            this.Visit(rootNode);
            return this.nodes;
        }


#if VS2022
        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            => this.VisitMembers(node.Members);
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

        private bool HasBody(BaseMethodDeclarationSyntax node) => node.Body != null || node.ExpressionBody != null;
        private void AddIfHasBody(BaseMethodDeclarationSyntax node)
        {
            if (this.HasBody(node))
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
                this.VisitNonAbstractBasePropertyDeclaration(node);
            }
        }

        private void AddIfPropertyDeclaration(BasePropertyDeclarationSyntax node)
        {
            if (node is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                this.AddNode(propertyDeclarationSyntax);
            }
        }

        private void VisitNonAbstractBasePropertyDeclaration(BasePropertyDeclarationSyntax node)
        {
            if (node.AccessorList == null)
            {
                this.AddIfPropertyDeclaration(node);
            }
            else
            {
                this.AddAccessors(node.AccessorList.Accessors, node.Parent is InterfaceDeclarationSyntax);
            }
        }

        private void AddAccessors(SyntaxList<AccessorDeclarationSyntax> accessors, bool typeIsInterface)
            => accessors.Where(accessor => !typeIsInterface || this.AccessorHasBody(accessor)).ToList().ForEach(this.AddNode);

        private bool AccessorHasBody(AccessorDeclarationSyntax accessor) => accessor.Body != null || accessor.ExpressionBody != null;

        private void VisitMembers(SyntaxList<MemberDeclarationSyntax> members)
        {
            foreach (MemberDeclarationSyntax member in members)
            {
                this.Visit(member);
            }
        }

        private bool IsAbstract(SyntaxTokenList modifiers) => modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword));

        private void AddNode(SyntaxNode node) => this.nodes.Add(node);
    }
}
