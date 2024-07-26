using System;
using Newtonsoft.Json;

namespace MMXOnline;

public class SavedMatchSettings {
	public HostMenuSettings hostMenuSettings;
	public CustomMatchSettings customMatchSettings;
	public ExtraCpuCharData extraCpuCharData;
	public string configName;

	public SavedMatchSettings() {
		hostMenuSettings = new HostMenuSettings();
		customMatchSettings = CustomMatchSettings.getDefaults();
		extraCpuCharData = new ExtraCpuCharData();
	}

	public static SavedMatchSettings createMatchSettingsFromFile(string fileName) {
		string text = Helpers.ReadFromFile(fileName + ".json");
		if (string.IsNullOrEmpty(text)) {
			return new SavedMatchSettings() {
				configName = fileName
			};
		} else {
			try {
				var result = JsonConvert.DeserializeObject<SavedMatchSettings>(text);
				result.configName = fileName;
				return result;
			} catch {
				throw new Exception("Your matchSettings.json file is corrupted, or does no longer work with this version. Please delete it and launch the game again.");
			}
		}
	}

	private static SavedMatchSettings _mainOffline;
	public static SavedMatchSettings mainOffline {
		get {
			if (_mainOffline == null) {
				_mainOffline = createMatchSettingsFromFile("matchSettingsOffline");
			}
			return _mainOffline;
		}
	}

	private static SavedMatchSettings _mainOnline;
	public static SavedMatchSettings mainOnline {
		get {
			if (_mainOnline == null) {
				_mainOnline = createMatchSettingsFromFile("matchSettingsOnline");
			}
			return _mainOnline;
		}
	}

	public void saveToFile() {
		string text = JsonConvert.SerializeObject(this, Formatting.Indented);
		Helpers.WriteToFile(configName + ".json", text);
	}
}
