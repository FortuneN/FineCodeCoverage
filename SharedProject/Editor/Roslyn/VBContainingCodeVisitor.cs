using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Linq;

namespace FineCodeCoverage.Editor.Roslyn
{
    class VBContainingCodeVisitor : VisualBasicSyntaxVisitor, ILanguageContainingCodeVisitor
    {
        private readonly List<TextSpan> spans = new List<TextSpan>();
        public List<TextSpan> GetSpans(SyntaxNode rootNode)
        {
            Visit(rootNode);
            return spans;
        }
        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitNamespaceBlock(NamespaceBlockSyntax node)
        {
            VisitMembers(node.Members);
        }

        private void VisitMembers(SyntaxList<StatementSyntax> members)
        {
            foreach (var member in members)
            {
                Visit(member);
            }
        }

        public override void VisitClassBlock(ClassBlockSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitStructureBlock(StructureBlockSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitModuleBlock(ModuleBlockSyntax node)
        {
            VisitMembers(node.Members);
        }

        public override void VisitConstructorBlock(ConstructorBlockSyntax node)
        {
            AddNode(node);
        }

        public override void VisitMethodBlock(MethodBlockSyntax node)
        {
            if (!IsPartial(node.SubOrFunctionStatement.Modifiers))
            {
                AddNode(node);
            }
        }

        private bool IsPartial(SyntaxTokenList modifiers)
        {
            return modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        }

        private bool IsAbstract(SyntaxTokenList modifiers)
        {
            return modifiers.Any(modifier => modifier.IsKind(SyntaxKind.MustOverrideKeyword));
        }


        public override void VisitOperatorBlock(OperatorBlockSyntax node)
        {
            AddNode(node);
        }

        public override void VisitPropertyBlock(PropertyBlockSyntax node)
        {
            VisitAccessors(node.Accessors);
        }

        // Coverlet instruments C# auto properties but not VB.  May be able to remove this
        public override void VisitPropertyStatement(PropertyStatementSyntax node)
        {
            if (!IsAbstract(node.Modifiers))
            {
                AddNode(node);
            }
        }

        public override void VisitEventBlock(EventBlockSyntax node)
        {
            VisitAccessors(node.Accessors);
        }

        private void VisitAccessors(SyntaxList<AccessorBlockSyntax> accessors)
        {
            foreach (var accessor in accessors)
            {
                Visit(accessor);
            }
        }

        public override void VisitAccessorBlock(AccessorBlockSyntax node)
        {
            AddNode(node);
        }
    
        private void AddNode(SyntaxNode node)
        {
            spans.Add(node.GetLeadingNoTrailingSpan());
        }
    }

}
