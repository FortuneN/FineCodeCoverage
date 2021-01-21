﻿using System;
using Newtonsoft.Json;
using System.Reflection;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Settings;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Settings;

namespace FineCodeCoverage.Options
{
	public class AppOptions : DialogPage
	{
		public const string runCategory = "Run";
		public const string excludeIncludeCategory = "Exclude / Include";


		[Category(runCategory)]
		[Description("Specifies whether or not coverage output is enabled")]
		public bool Enabled { get; set; } = true;

		[Description(
		@"Filter expressions to exclude specific modules and types (multiple)
		
		Wildcards
		* => matches zero or more characters
		
		Examples
		[*]* => Excludes all types in all assemblies (nothing is instrumented)
		[coverlet.*]Coverlet.Core.Coverage => Excludes the Coverage class in the Coverlet.Core namespace belonging to any assembly that matches coverlet.* (e.g coverlet.core)
		[*]Coverlet.Core.Instrumentation.* => Excludes all types belonging to Coverlet.Core.Instrumentation namespace in any assembly
		[coverlet.*.tests]* => Excludes all types in any assembly starting with coverlet. and ending with .tests
		
		Both 'Exclude' and 'Include' options can be used together but 'Exclude' takes precedence.
		")]
		[Category(excludeIncludeCategory)]
		public string[] Exclude { get; set; }

		[Description(
		@"Filter expressions to include specific modules and types (multiple)
		
		Wildcards
		* => matches zero or more characters
		
		Examples
		[*]*"" => Includes all types in all assemblies (nothing is instrumented)
		[coverlet.*]Coverlet.Core.Coverage => Includes the Coverage class in the Coverlet.Core namespace belonging to any assembly that matches coverlet.* (e.g coverlet.core)
		[*]Coverlet.Core.Instrumentation.* => Includes all types belonging to Coverlet.Core.Instrumentation namespace in any assembly
		[coverlet.*.tests]* => Includes all types in any assembly starting with coverlet. and ending with .tests
		
		Both 'Exclude' and 'Include' options can be used together but 'Exclude' takes precedence.
		")]
		[Category(excludeIncludeCategory)]
		public string[] Include { get; set; }

		[Description(
		@"Glob patterns specifying source files to exclude (multiple)
		Use file path or directory path with globbing (e.g. **/Migrations/*)
		")]
		[Category(excludeIncludeCategory)]
		public string[] ExcludeByFile { get; set; } = new[] { "**/Migrations/*" };

		[Description(
		@"Specifies whether to report code coverage of the test assembly
		")]
		[Category(excludeIncludeCategory)]
		public bool IncludeTestAssembly { get; set; } = true;

		[Description(
		@"Attributes to exclude from code coverage (multiple)

		You can ignore a method or an entire class from code coverage by creating and applying the [ExcludeFromCodeCoverage] attribute present in the System.Diagnostics.CodeAnalysis namespace.
		You can also ignore additional attributes by adding to this list (short name or full name supported) e.g. :
		[GeneratedCode] => Present in the System.CodeDom.Compiler namespace
		[MyCustomExcludeFromCodeCoverage] => Any custom attribute that you may define
		")]
		[Category(excludeIncludeCategory)]
		public string[] ExcludeByAttribute { get; set; } = new[] { "GeneratedCode" };

		[Description("Specify true to not wait for tests to finish before running coverage")]
		[Category(runCategory)]
		public bool RunInParallel { get; set; }

		[Description("Specify false to prevent coverage when tests fail.  Cannot be used in conjunction with RunInParallel")]
		[Category(runCategory)]
		public bool RunWhenTestsFail { get; set; } = true;

		[Description("Specify a value to only run coverage based upon the number of executing tests")]
		[Category(runCategory)]
		public int RunWhenTestsExceed { get; set; }


		[SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread")]
		public override void SaveSettingsToStorage()
		{
			var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
			var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

			if (!settingsStore.CollectionExists(Vsix.Code))
			{
				settingsStore.CreateCollection(Vsix.Code);
			}

			foreach (var property in GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
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
		private static void LoadSettingsInto(AppOptions instance)
		{
			var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
			var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

			if (!settingsStore.CollectionExists(Vsix.Code))
			{
				settingsStore.CreateCollection(Vsix.Code);
			}

			foreach (var property in instance.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
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

		public static AppOptions Get()
		{
			var options = new AppOptions();
			LoadSettingsInto(options);
			return options;
		}
	}
}
