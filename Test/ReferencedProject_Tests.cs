using System.IO;
using FineCodeCoverage.Core.Model;
using NUnit.Framework;

namespace Test
{
    public class ReferencedProject_Tests
    {
        private string tempProjectFilePath;


        [TearDown]
        public void Delete_ProjectFile()
        {
            if (tempProjectFilePath != null)
            {
                File.Delete(tempProjectFilePath);
            }
        }

        [Test]
        public void AssemblyName_Should_Be_The_Name_Of_The_Dll()
        {
            var referencedProject = new ReferencedProject("", @"C:\Users\tonyh\Source\Repos\RefProject\RefProject\bin\Debug\RefProject.dll");
            Assert.AreEqual("RefProject", referencedProject.AssemblyName);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_ExcludeFromCodeCoverage_If_Has_Project_Property_FCCExcludeFromCodeCoverage(bool addProperty)
        {
            var property = addProperty ? $"<{ReferencedProject.excludeFromCodeCoveragePropertyName}/>" : "";
            var projectFileXml = @$"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        {property}
    </PropertyGroup>
</Project >
";
            tempProjectFilePath = Path.GetTempFileName();
            File.WriteAllText(tempProjectFilePath, projectFileXml);
            var referencedProject = new ReferencedProject(tempProjectFilePath, "");
            Assert.AreEqual(addProperty, referencedProject.ExcludeFromCodeCoverage);

        }
    }
}