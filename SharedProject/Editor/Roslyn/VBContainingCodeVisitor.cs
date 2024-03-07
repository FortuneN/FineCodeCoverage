using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace FineCodeCoverage.Editor.Roslyn
{
    internal class VBContainingCodeVisitor : VisualBasicSyntaxVisitor, ILanguageContainingCodeVisitor
    {
        private readonly List<TextSpan> spans = new List<TextSpan>();
        public List<TextSpan> GetSpans(SyntaxNode rootNode)
        {
            this.Visit(rootNode);
            return this.spans;
        }
        public override void VisitCompilationUnit(CompilationUnitSyntax node) => this.VisitMembers(node.Members);

        public override void VisitNamespaceBlock(NamespaceBlockSyntax node) => this.VisitMembers(node.Members);

        private void VisitMembers(SyntaxList<StatementSyntax> members)
        {
            foreach (StatementSyntax member in members)
            {
                this.Visit(member);
            }
        }

        public override void VisitClassBlock(ClassBlockSyntax node) => this.VisitMembers(node.Members);

        public override void VisitStructureBlock(StructureBlockSyntax node) => this.VisitMembers(node.Members);

        public override void VisitModuleBlock(ModuleBlockSyntax node) => this.VisitMembers(node.Members);

        public override void VisitConstructorBlock(ConstructorBlockSyntax node) => this.AddNode(node);

        public override void VisitMethodBlock(MethodBlockSyntax node)
        {
            if (!this.IsPartial(node.SubOrFunctionStatement.Modifiers))
            {
                this.AddNode(node);
            }
        }

        private bool IsPartial(SyntaxTokenList modifiers) => modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));

        private bool IsAbstract(SyntaxTokenList modifiers) => modifiers.Any(modifier => modifier.IsKind(SyntaxKind.MustOverrideKeyword));

        public override void VisitOperatorBlock(OperatorBlockSyntax node) => this.AddNode(node);

        public override void VisitPropertyBlock(PropertyBlockSyntax node) => this.VisitAccessors(node.Accessors);

        // Coverlet instruments C# auto properties but not VB.  May be able to remove this
        public override void VisitPropertyStatement(PropertyStatementSyntax node)
        {
            if (!this.IsAbstract(node.Modifiers))
            {
                this.AddNode(node);
            }
        }

        public override void VisitEventBlock(EventBlockSyntax node) => this.VisitAccessors(node.Accessors);

        private void VisitAccessors(SyntaxList<AccessorBlockSyntax> accessors)
        {
            foreach (AccessorBlockSyntax accessor in accessors)
            {
                this.Visit(accessor);
            }
        }

        public override void VisitAccessorBlock(AccessorBlockSyntax node) => this.AddNode(node);

        private void AddNode(SyntaxNode node) => this.spans.Add(node.Span);
    }
}
