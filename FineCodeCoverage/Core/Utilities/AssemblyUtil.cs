using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FineCodeCoverage.Engine.Utilities
{
	internal interface IAssemblyUtil
    {
		T RunInAssemblyResolvingContext<T>(Func<T> func);

	}

	[Export(typeof(IAssemblyUtil))]
	internal class AssemblyUtil:IAssemblyUtil
	{
		public void RunInAssemblyResolvingContext(Action action)
		{
			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

				action.Invoke();
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
			}
		}

		public T RunInAssemblyResolvingContext<T>(Func<T> func)
		{
			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

				return func.Invoke();
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
			}
		}

		public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var assemblyName = new AssemblyName(args.Name);

			try
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

				// try resolve by name

				try
				{
					var assembly = Assembly.Load(assemblyName.Name);
					if (assembly != null) return assembly;
				}
				catch
				{
					// ignore
				}

				// try resolve by path

				try
				{
					var dllName = $"{assemblyName.Name}.dll";
					var projectDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
					var dllPath = Directory.GetFiles(projectDllPath, "*.dll", SearchOption.AllDirectories).FirstOrDefault(x => Path.GetFileName(x).Equals(x.Equals(dllName, StringComparison.OrdinalIgnoreCase)));

					if (!string.IsNullOrWhiteSpace(dllPath))
					{
						var assembly = Assembly.LoadFile(dllPath);
						if (assembly != null) return assembly;
					}
				}
				catch
				{
					// ignore
				}
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			}

			return null;
		}
	}
}
