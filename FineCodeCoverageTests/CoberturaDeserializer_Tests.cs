using NUnit.Framework;
using System.IO;
using FineCodeCoverage.Engine.Cobertura;
using System;
using System.Linq;

namespace FineCodeCoverageTests
{
    public class CoberturaDeserializer_Tests
    {
        [Test]
        public void Should_Deserialize_What_Is_Required()
        {
            var cobertura = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE coverage SYSTEM ""http://cobertura.sourceforge.net/xml/coverage-04.dtd"">
<coverage line-rate=""0.127074535991358"" branch-rate=""0.101282051282051"" lines-covered=""1294"" lines-valid=""10183"" branches-covered=""395"" branches-valid=""3900"" complexity=""5304"" version=""1"" timestamp=""1709634604"">
  <sources />
  <packages>
    <package name=""DemoOpenCover"" line-rate=""0.0271464646464646"" branch-rate=""0.5"" complexity=""537"">
      <classes>
        <class name="".LargeClass"" filename=""C:\Users\tonyh\source\repos\DemoOpenCover\DemoOpenCover\LargeClass.cs"" line-rate=""1"" branch-rate=""1"" complexity=""501"">
          <methods>
            <method name=""Method0"" signature=""()"" line-rate=""1"" branch-rate=""1"" complexity=""1"">
              <lines>
                <line number=""1"" hits=""1"" branch=""false"" />
              </lines>
            </method>
          </methods>
          <lines>
            <line number=""1"" hits=""1"" branch=""true"" />
          </lines>
        </class>
       </classes>
    </package>
    </packages>
</coverage>
";

            string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".xml";
            File.WriteAllText(fileName, cobertura);
            var report = new CoberturaDerializer().Deserialize(fileName);
            var package = report.Packages.Single();
            Assert.AreEqual("DemoOpenCover", package.Name);
            var packageClass = package.Classes.Single();
            Assert.AreEqual(".LargeClass", packageClass.Name);
            Assert.AreEqual(@"C:\Users\tonyh\source\repos\DemoOpenCover\DemoOpenCover\LargeClass.cs", packageClass.Filename);
            Assert.AreEqual(1, packageClass.LineRate);
            Assert.AreEqual(1, packageClass.BranchRate);
            Assert.AreEqual(501, packageClass.Complexity);
            var method = packageClass.Methods.Single();
            Assert.AreEqual("Method0", method.Name);
            Assert.AreEqual("()", method.Signature);
            Assert.AreEqual(1, method.LineRate);
            Assert.AreEqual(1, method.BranchRate);
            var line = method.Lines.Single();
            Assert.AreEqual(1, line.Number);
            Assert.AreEqual(1, line.Hits);
            line = packageClass.Lines.Single();
            Assert.AreEqual(1, line.Number);
            Assert.AreEqual(1, line.Hits);
        }
    }
}
