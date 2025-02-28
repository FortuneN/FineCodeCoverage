using System.IO;
using FineCodeCoverage.Engine.Model;
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

        [TestCase(true)]
        [TestCase(false)]
        public void Should_ExcludeFromCodeCoverage_If_Has_Project_Property_FCCExcludeFromCodeCoverage(bool addProperty)
        {
            var property = addProperty ? $"<{ReferencedProject.excludeFromCodeCoveragePropertyName}/>" : "";
            WriteProperty(property);
            var referencedProject = new ReferencedProject(tempProjectFilePath, "",true);
            Assert.AreEqual(addProperty, referencedProject.ExcludeFromCodeCoverage);

        }
        private void WriteProperty(string property)
        {
            var projectFileXml = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        {property}
    </PropertyGroup>
</Project >
";
            tempProjectFilePath = Path.GetTempFileName();
            File.WriteAllText(tempProjectFilePath, projectFileXml);
        }
        private ReferencedProject SetUpProjectForAssemblyName(string assemblyName)
        {
            var property = string.IsNullOrEmpty(assemblyName) ? "" : $"<AssemblyName>{assemblyName}</AssemblyName>";
            WriteProperty(property);
            return new ReferencedProject(tempProjectFilePath);
        }

        [TestCase]
        public void Should_Use_The_AssemblyName_Project_Property_If_AssemblyName_Is_Not_Provided()
        {
            var referencedProject = SetUpProjectForAssemblyName("MyAssembly");
            Assert.AreEqual("MyAssembly", referencedProject.AssemblyName);
        }

        [TestCase]
        public void Should_Fallback_AssemblyName_To_The_Project_File_Name()
        {
            var referencedProject = SetUpProjectForAssemblyName(null);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(tempProjectFilePath), referencedProject.AssemblyName);
        }
    }
}