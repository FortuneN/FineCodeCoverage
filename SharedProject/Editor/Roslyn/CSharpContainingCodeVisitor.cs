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
            Visit(rootNode);
            return spans;
        }

#if VS2022
        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            VisitMembers(node.Members);
        }
#endif
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
                AddNode(node);
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
            if (!IsAbstract(node.Modifiers))
            {
                if(node.AccessorList == null)
                {
                    if(node is PropertyDeclarationSyntax propertyDeclarationSyntax)
                    {
                        AddNode(propertyDeclarationSyntax);
                    }
                }
                else
                {
                    var isInterfaceProperty = node.Parent is InterfaceDeclarationSyntax;
                    foreach (var accessor in node.AccessorList.Accessors)
                    {
                        var addAccessor = !isInterfaceProperty || AccessorHasBody(accessor);
                        if (addAccessor)
                        {
                            AddNode(accessor);
                        }
                    }
                }
                
            }
        }

        private bool AccessorHasBody(AccessorDeclarationSyntax accessor)
        {
            return accessor.Body != null || accessor.ExpressionBody != null;
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

        private void AddNode(SyntaxNode node)
        {
            spans.Add(node.GetLeadingNoTrailingSpan());
        }
    }

}
