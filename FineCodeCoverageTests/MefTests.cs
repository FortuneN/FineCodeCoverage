using FineCodeCoverage.Output;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;

namespace FineCodeCoverageTests
{
    internal class MefTests
    {
        class ExportedTypeInfo
        {
            public ExportedTypeInfo(Type type,List<Type> exportedTypes, List<Type> importedTypes)
            {
                Type = type;
                ExportedTypes = exportedTypes;
                ImportedTypes = importedTypes;
            }

            public Type Type { get; }
            public List<Type> ExportedTypes { get; }
            public List<Type> ImportedTypes { get; }
        }
        [Test]
        public void Mef_Setup_Correctly()
        {
            var assembly = typeof(OutputToolWindowPackage).Assembly;
            var mefTypeInfos = assembly.GetTypes().Select(t =>
            {
                var mefExportedTypes = t.GetCustomAttributes<ExportAttribute>().Select(exportAttribute =>
                {
                    var contractType = exportAttribute.ContractType;
                    if (contractType != null)
                    {
                        var isAssignableToContractType = contractType.IsAssignableFrom(t);
                        if (!isAssignableToContractType)
                        {
                            throw new Exception();
                        }
                    }
                    return contractType ?? t;
                }).ToList();
                List<Type> importedTypes = null;
                var importingConstructor = t.GetConstructors().FirstOrDefault(c => c.GetCustomAttribute<ImportingConstructorAttribute>() != null);
                if(importingConstructor != null)
                {
                    if(mefExportedTypes.Count == 0)
                    {
                        throw new Exception("Imports but does not export");
                    }
                    var parameters = importingConstructor.GetParameters();
                    importedTypes = parameters.Select(p =>
                    {
                        var importAttribute = p.GetCustomAttribute<ImportAttribute>();
                        if (importAttribute != null)
                        {
                            return importAttribute.ContractType;
                        }
                        if (p.GetCustomAttribute<ImportManyAttribute>() != null)
                        {
                            if (p.ParameterType.IsArray)
                            {
                                return p.ParameterType.GetElementType();
                            }
                            var enumerableType = p.ParameterType.GetGenericArguments().FirstOrDefault();
                            if (enumerableType.GetGenericTypeDefinition() == typeof(Lazy<,>))
                            {
                                return enumerableType.GetGenericArguments().FirstOrDefault();
                            }
                            return enumerableType;
                        }
                        return p.ParameterType;
                    }).ToList();
                }
                return new ExportedTypeInfo(t, mefExportedTypes,importedTypes);
            }).Where(info => info.ExportedTypes.Count > 0).ToList();
            foreach(var mefTypeInfo in mefTypeInfos)
            {
                if(mefTypeInfo.ImportedTypes != null)
                {
                    foreach(var importedType in mefTypeInfo.ImportedTypes.Where(t => t.Assembly == assembly))
                    {
                        var isExported = mefTypeInfos.Any(info => info.ExportedTypes.Contains(importedType));
                        if (!isExported)
                        {
                            throw new Exception($"{mefTypeInfo.Type.Name} imports {importedType.Name} but it is not exported");
                        }
                    }
                }
            }

            // need circular test
        }
    }
}
