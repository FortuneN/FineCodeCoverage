using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FineCodeCoverage.Engine.Utilities
{
	internal static class AssemblyUtil
	{
		public static void RunInAssemblyResolvingContext(Action action)
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

		public static T RunInAssemblyResolvingContext<T>(Func<T> func)
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

		public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
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

		public static CompilationMode GetCompilationMode(string dllPath)
		{
			//https://stackoverflow.com/questions/270531/how-can-i-determine-if-a-net-assembly-was-built-for-x86-or-x64/36316170

			if (!File.Exists(dllPath))
			{
				throw new ArgumentException($"File '{dllPath}' does not exist");
			}

			var intPtr = IntPtr.Zero;
			try
			{
				uint unmanagedBufferSize = 4096;
				intPtr = Marshal.AllocHGlobal((int)unmanagedBufferSize);

				using (var stream = File.Open(dllPath, FileMode.Open, FileAccess.Read))
				{
					var bytes = new byte[unmanagedBufferSize];
					stream.Read(bytes, 0, bytes.Length);
					Marshal.Copy(bytes, 0, intPtr, bytes.Length);
				}

				//Check DOS header magic number
				
				if (Marshal.ReadInt16(intPtr) != 0x5a4d)
				{
					return CompilationMode.Invalid;
				}

				// This will get the address for the WinNT header  
				
				var ntHeaderAddressOffset = Marshal.ReadInt32(intPtr + 60);

				// Check WinNT header signature
				
				var signature = Marshal.ReadInt32(intPtr + ntHeaderAddressOffset);
				if (signature != 0x4550)
				{
					return CompilationMode.Invalid;
				}

				// Determine file bitness by reading magic from IMAGE_OPTIONAL_HEADER
				
				var magic = Marshal.ReadInt16(intPtr + ntHeaderAddressOffset + 24);

				var result = CompilationMode.Invalid;
				uint clrHeaderSize;

				if (magic == 0x10b)
				{
					clrHeaderSize = (uint)Marshal.ReadInt32(intPtr + ntHeaderAddressOffset + 24 + 208 + 4);
					result |= CompilationMode.Bit32;
				}
				else if (magic == 0x20b)
				{
					clrHeaderSize = (uint)Marshal.ReadInt32(intPtr + ntHeaderAddressOffset + 24 + 224 + 4);
					result |= CompilationMode.Bit64;
				}
				else
				{
					return CompilationMode.Invalid;
				}

				result |= clrHeaderSize != 0 ? CompilationMode.CLR : CompilationMode.Native;

				return result;
			}
			finally
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
		}
	}

	[Flags]
	internal enum CompilationMode
	{
		Invalid = 0,
		Native = 0x1,
		CLR = Native << 1,
		Bit32 = CLR << 1,
		Bit64 = Bit32 << 1
	}
}
