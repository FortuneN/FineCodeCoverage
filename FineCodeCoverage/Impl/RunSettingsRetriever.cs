using System.Reflection;
using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
	internal class RunSettingsRetriever
	{
		private object userSettings;
		
		public async Task<string> GetRunSettingsFileAsync(object userSettings, object testContainer)
		{
			this.userSettings = userSettings;
			
			var runSettingsFile = GetDefaultRunSettingsFilePath();
			var projectRunSettingsFile = await GetProjectRunSettingFileAsync(testContainer);
			
			if (!string.IsNullOrEmpty(projectRunSettingsFile))
			{
				return projectRunSettingsFile;
			}

			return runSettingsFile;
		}

		private string GetAndUpdateSolutionRunSettingsFilePath()
		{
			return userSettings.GetType().GetMethod("GetAndUpdateSolutionRunSettingsFilePath", BindingFlags.Public | BindingFlags.Instance).Invoke(userSettings, new object[] { }) as string;
		}
		
		private string LastRunSettingsFilePath()
		{
			return userSettings.GetType().GetProperty("LastRunSettingsFilePath", BindingFlags.Public | BindingFlags.Instance).GetValue(userSettings) as string;
		}
		
		private bool AutomaticallyDetectRunSettings()
		{
			return (bool)userSettings.GetType().GetProperty("AutomaticallyDetectRunSettings", BindingFlags.Public | BindingFlags.Instance).GetValue(userSettings);
		}

		private string GetDefaultRunSettingsFilePath()
		{
			string settingsFilePath = GetAndUpdateSolutionRunSettingsFilePath();
			var lastRunSettingsFilePath = LastRunSettingsFilePath();
			
			if (!string.IsNullOrEmpty(lastRunSettingsFilePath))
			{
				return lastRunSettingsFilePath;
			}

			if (!AutomaticallyDetectRunSettings() || string.IsNullOrEmpty(settingsFilePath))
			{
				return null;
			}

			return settingsFilePath;
		}

		private static async Task<string> GetProjectRunSettingFileAsync(object container)
		{
			var projectDataProperty = container.GetType().GetProperty("ProjectData");
			
			if (projectDataProperty != null)
			{
				var projectData = projectDataProperty.GetValue(container);
				var projectRunSettingsFile = await (projectData.GetType().GetMethod("GetBuildPropertyAsync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(projectData, new object[] { "RunSettingsFilePath", (string)null }) as Task<string>);
				return projectRunSettingsFile;
			}

			return null;
		}
	}
}
