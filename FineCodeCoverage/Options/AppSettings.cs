using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;

namespace FineCodeCoverage.Options
{
	public class AppSettings : DialogPage
	{
		[Description(
		@"Filter expressions to exclude specific modules and types (multiple)
		
		Wildcards
		* => matches zero or more characters
		? => the prefixed character is optional
		
		Examples
		[*]* => Excludes all types in all assemblies (nothing is instrumented)
		[coverlet.*]Coverlet.Core.Coverage => Excludes the Coverage class in the Coverlet.Core namespace belonging to any assembly that matches coverlet.* (e.g coverlet.core)
		[*]Coverlet.Core.Instrumentation.* => Excludes all types belonging to Coverlet.Core.Instrumentation namespace in any assembly
		[coverlet.*.tests?] * => Excludes all types in any assembly starting with coverlet. and ending with .test or .tests (the ? makes the s optional)
		
		Both 'Exclude' and 'Include' options can be used together but 'Exclude' takes precedence.
		")]
		public string[] Exclude { get; set; }

		[Description(
		@"Filter expressions to include specific modules and types (multiple)
		
		Wildcards
		* => matches zero or more characters
		? => the prefixed character is optional
		
		Examples
		[*]*"" => Includes all types in all assemblies (nothing is instrumented)
		[coverlet.*]Coverlet.Core.Coverage => Includes the Coverage class in the Coverlet.Core namespace belonging to any assembly that matches coverlet.* (e.g coverlet.core)
		[*]Coverlet.Core.Instrumentation.* => Includes all types belonging to Coverlet.Core.Instrumentation namespace in any assembly
		[coverlet.*.tests?] * => Includes all types in any assembly starting with coverlet. and ending with .test or .tests (the ? makes the s optional)
		
		Both 'Exclude' and 'Include' options can be used together but 'Exclude' takes precedence.
		")]
		public string[] Include { get; set; }

		[Description("Include directories containing additional assemblies to be instrumented (multiple)")]
		public string[] IncludeDirectories { get; set; }

		[Description(
		@"Glob patterns specifying source files to exclude (multiple)
		Use file path or directory path with globbing (e.g dir1/*.cs)
		")]
		public string[] ExcludeByFiles { get; set; } = new[] { "Migrations/**.cs" };

		[Description(
		@"Attributes to exclude from code coverage (multiple)

		You can ignore a method or an entire class from code coverage by creating and applying the [ExcludeFromCodeCoverage] attribute present in the System.Diagnostics.CodeAnalysis namespace.
		
		You can also ignore additional attributes by adding to this list (short name or full name supported) e.g. :
		[GeneratedCode] => Present in System.CodeDom.Compiler namespace
		[CompilerGenerated] => Present in System.Runtime.CompilerServices namespace
		[CustomExcludeFromCodeCoverage] => Any custom attribute that you may define
		")]
		public string[] ExcludeByAttributes { get; set; } = new[] { "GeneratedCode", "CompilerGenerated" };

		[Description("Specifies whether to include code coverage of the test assembly")]
		public bool IncludeTestAssembly { get; set; } = true;

		//https://github.com/coverlet-coverage/coverlet/issues/961
		//[Description("Neither track nor record auto-implemented properties (e.g. int Example { get; set; })")]
		//public bool SkipAutoProperties { get; set; } = true;

		//https://github.com/coverlet-coverage/coverlet/issues/962
		//[Description(@"
		//Attributes that mark methods that do not return (multiple)
		//e.g.
		//[DoesNotReturn] => Present in System.Diagnostics.CodeAnalysis
		//[CustomDoesNotReturn] => Any custom attribute that you may define
		//")]
		//public string[] DoesNotReturnAttributes { get; set; } = new[] { "DoesNotReturn" };

		[SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread")]
		public override void SaveSettingsToStorage()
		{
			var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
			var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

			if (!settingsStore.CollectionExists(Vsix.Code))
			{
				settingsStore.CreateCollection(Vsix.Code);
			}

			foreach (var property in GetType().GetProperties())
			{
				try
				{
					var objValue = property.GetValue(this);
					var strValue = JsonConvert.SerializeObject(objValue);

					settingsStore.SetString(Vsix.Code, property.Name, strValue);
				}
				catch (Exception exception)
				{
					Logger.Log($"Failed to save '{property.Name}' setting", exception);
				}
			}
		}

		[SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread")]
		private static void LoadSettingsInto(AppSettings instance)
		{
			var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
			var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

			if (!settingsStore.CollectionExists(Vsix.Code))
			{
				settingsStore.CreateCollection(Vsix.Code);
			}

			foreach (var property in instance.GetType().GetProperties())
			{
				try
				{
					if (!settingsStore.PropertyExists(Vsix.Code, property.Name))
					{
						continue;
					}

					var strValue = settingsStore.GetString(Vsix.Code, property.Name);

					if (string.IsNullOrWhiteSpace(strValue))
					{
						continue;
					}

					var objValue = JsonConvert.DeserializeObject(strValue, property.PropertyType);

					property.SetValue(instance, objValue);
				}
				catch (Exception exception)
				{
					Logger.Log($"Failed to load '{property.Name}' setting", exception);
				}
			}
		}

		public override void LoadSettingsFromStorage()
		{
			LoadSettingsInto(this);
		}

		public static AppSettings Get()
		{
			var options = new AppSettings();
			LoadSettingsInto(options);
			return options;
		}
	}
}
