using FineCodeCoverage.Editor.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System;

namespace FineCodeCoverageTests.Editor.Roslyn
{
    internal class CSharpContainingCodeVisitor_Tests : ContainingCodeVisitor_Tests_Base
    {
        protected override SyntaxNode ParseCompilation(string compilationText) => SyntaxFactory.ParseCompilationUnit(compilationText);

        protected override ILanguageContainingCodeVisitor GetVisitor() => new CSharpContainingCodeVisitor();
        
        [Test]
        public void Should_Visit_Methods()
        {
            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        public void MyMethod()
        {
            var x = 1;
        }
    }
}
";
            AssertShouldVisit<MethodDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Method_With_Expression_Bodies()
        {
            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        public int MyMethod() => 5
    }
}
";
            AssertShouldVisit<MethodDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Constructors()
        {
            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        public MyClass(){

        }
    }
}
";
            AssertShouldVisit<ConstructorDeclarationSyntax>(text);
        }
        
        [Test]
        public void Should_Visit_Finalizers()
        {
            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        ~MyClass(){

        }
    }
}
";
            AssertShouldVisit<DestructorDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Operators()
        {
            var text = @"
    public class Conv1
    {
        public static Conv1 operator +(Conv1 a, Conv1 b)
        {
            return new Conv1();
        }
    }
";
            AssertShouldVisit<OperatorDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Conversions()
        {
            var text = @"
    public class Conv1
    {
        public static implicit operator Conv2(Conv1 d)
        {
            return new Conv2();
        }
    }

    public class Conv2
    {

    }
";
            AssertShouldVisit<ConversionOperatorDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Property_Getters()
        {
            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        public int Property {
            get {
                return 5;
            }
        }
    }
}
";
            AssertShouldVisit<AccessorDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Property_Setters()
        {
            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        private int v;
        public int Property {
            set {
                v = value;
            }
        }
    }
}
";
            AssertShouldVisit<AccessorDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Auto_Implemented_Properties()
        {
            var text = @"
public class MyClass
{
    public int MyProperty { get; set; }
}
";
            var (textSpans, rootNode) = Visit(text);
            Assert.That(textSpans.Count, Is.EqualTo(2));
            textSpans.ForEach(textSpan => AssertTextSpan<AccessorDeclarationSyntax>(rootNode, textSpan));
        }

        [Test]
        public void Should_Not_Visit_Abstract_Properties()
        {
            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        public abstract int AbstractProperty {get;set;}
    }
}
";
            AssertShouldNotVisit(text);
        }

        [Test]
        public void Should_Visit_Indexers()
        {
            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        public string this[int i]
        {
            set {
                int.Parse(value);
            }
        }
    }
}
";
            AssertShouldVisit<AccessorDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Event_Accessors()
        {
            var text = @"
namespace MyNamespace
{
    public class MyClass
    {
        public event EventHandler AnEvent
        {
            add
            {

            }
            
        }
    }
}
";
            AssertShouldVisit<AccessorDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Structs()
        {
            var text = @"
namespace MyNamespace
{
    public struct MyStruct
    {
        public void MyMethod()
        {
            var x = 1;
        }
    }
}
";
            AssertShouldVisit<MethodDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Records()
        {
            var text = @"
namespace MyNamespace
{
    public record Person(string FirstName, string LastName, string[] PhoneNumbers)
    {
        public virtual bool PrintMembers(StringBuilder stringBuilder)
        {
            stringBuilder.Append($""FirstName = {FirstName}, LastName = {LastName}, "");
            stringBuilder.Append($""PhoneNumber1 = {PhoneNumbers[0]}, PhoneNumber2 = {PhoneNumbers[1]}"");
            return true;
        }
    }
}
";
            AssertShouldVisit<MethodDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Visit_Interface_Default_Methods()
        {
            var text = @"
namespace MyNamespace
{
    public interface IMyInterface
    {
        void MyMethod()
        {
            var x = 1;
        }
    }
}
";
            AssertShouldVisit<MethodDeclarationSyntax>(text);
        }

        [Test]
        public void Should_Not_Visit_Interface_Properties_Without_Body()
        {
            var text = @"
namespace MyNamespace
{
    public interface IMyInterface
    {
        int Property {get;}
    }
}
";
            AssertShouldNotVisit(text);
        }
    }

}
