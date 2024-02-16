using FineCodeCoverage.Editor.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.Roslyn
{
    internal class VBContainingCodeVisitor_Tests : ContainingCodeVisitor_Tests_Base
    {
        protected override ILanguageContainingCodeVisitor GetVisitor() => new VBContainingCodeVisitor();
        
        protected override SyntaxNode ParseCompilation(string compilationText) => SyntaxFactory.ParseCompilationUnit(compilationText);
        

        [Test]
        public void Should_Visit_Module_Methods()
        {
            var text = @"
Namespace NS
    Public Module Module1
        Sub Sub1()
            Debug.WriteLine(""Sub1"")
        End Sub
    End Module
End Namespace
";
            AssertShouldVisit<MethodBlockSyntax>(text);
        }

        [Test]
        public void Should_Visit_Class_Methods()
        {
            var text = @"
Public Class Class1
    Function Method() As Int32
        Return 1
    End Function
End Class
";
            AssertShouldVisit<MethodBlockSyntax>(text);
        }

        [Test]
        public void Should_Not_Visit_Partial_Methods()
        {
            var text = @"
Partial Public Class PartialClass
    Partial Private Sub Method()
    End Sub
End Class
";
            AssertShouldNotVisit(text);
        }

        [Test]
        public void Should_Visit_Constructors()
        {
            var text = @"
Public Class Class1
    Public Sub New()
        Console.WriteLine(""Constructor"")
    End Sub
End Class
";
            AssertShouldVisit<ConstructorBlockSyntax>(text);
        }

        [Test]
        public void Should_Visit_Operators()
        {
            var text = @"
Public Class Class1
    Public Shared Operator +(class1 As Class1) As Class1
        Return class1
    End Operator
End Class
";
            AssertShouldVisit<OperatorBlockSyntax>(text);
        }

        [Test]
        public void Should_Visit_Property_Getters()
        {
            var text = @"
Public Class Class1
    Private getSetField As String
    Property GetSet As String
        Get
            Return getSetField
        End Get
    End Property
End Class
";
            AssertShouldVisit<AccessorBlockSyntax>(text);
        }

        [Test]
        public void Should_Visit_Property_Setters()
        {
            var text = @"
Public Class Class1
    Private getSetField As String
    Property GetSet As String
        Set(value As String)
            getSetField = value
        End Set
    End Property
End Class
";
            AssertShouldVisit<AccessorBlockSyntax>(text);
        }

        [Test]
        public void Should_Visit_Auto_Implemented_Properties()
        {
            var text = @"
Public Class Class1
        Public Property AutoImplemented() As Boolean
End Class
";
            AssertShouldVisit<PropertyStatementSyntax>(text);
        }

        [Test]
        public void Should_Not_Visit_Abstract_Properties()
        {
            var text = @"
Public MustInherit Class Abstract
    Public MustOverride Property P() As Integer
End Class
";
            AssertShouldNotVisit(text);
        }

        [Test]
        public void Should_Visit_Event_AddHandler()
        {
            var text = @"
Public Class Class1
    Public Custom Event TestEvent As EventHandler
        AddHandler(value As EventHandler)
            StaticMethod()
        End AddHandler
    End Event
End Class
";
            AssertShouldVisit<AccessorBlockSyntax>(text);
        }

        [Test]
        public void Should_Visit_Event_RemoveHandler()
        {
            var text = @"
Public Class Class1
    Public Custom Event TestEvent As EventHandler
        RemoveHandler(value As EventHandler)
            StaticMethod()
        End RemoveHandler
    End Event
End Class
";
            AssertShouldVisit<AccessorBlockSyntax>(text);
        }

        [Test]
        public void Should_Visit_Event_RaiseEvent()
        {
            var text = @"
Public Class Class1
    Public Custom Event TestEvent As EventHandler
        RaiseEvent(sender As Object, e As EventArgs)
            StaticMethod()
        End RaiseEvent
    End Event
End Class
";
            AssertShouldVisit<AccessorBlockSyntax>(text);
        }

        [Test]
        public void Should_Visit_Structs()
        {
            var text = @"
Public Structure Struct1
    Public Sub Sub1()
        Console.WriteLine(""Sub1"")
    End Sub
End Structure
    
End Class
";
            AssertShouldVisit<MethodBlockSyntax>(text);
        }
    }

}
