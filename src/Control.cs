using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SFML.Window;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public class Control {
	public const string KeyboardName = "keyboard";
	public const string GenericControllerName = "generic";

	// The "inputNames"
	public const string Up = "up";
	public const string Down = "down";
	public const string Left = "left";
	public const string Right = "right";
	public const string Jump = "jump";
	public const string Shoot = "shoot";
	public const string Dash = "dash";
	public const string Special1 = "special1";
	public const string WeaponLeft = "weaponleft";
	public const string WeaponRight = "weaponright";
	public const string Scoreboard = "scoreboard";
	public const string AimUp = "aimup";
	public const string AimDown = "aimdown";
	public const string AimLeft = "aimleft";
	public const string AimRight = "aimright";
	public const string AimLock = "aimlock2";
	public const string AimAngleUp = "aimangleup";
	public const string AimAngleDown = "aimangledown";
	public const string AimAngleReset = "aimanglereset";
	public const string AxlCrouch = "axlcrouch2";
	public const string AxlAimBackwards = "axlaimbackwards";
	public const string MenuUp = "menuup";
	public const string MenuDown = "menudown";
	public const string MenuLeft = "menuleft";
	public const string MenuRight = "menuright";
	public const string MenuPause = "menuenter";
	public const string MenuConfirm = "menuselectprimary";
	public const string MenuAlt = "menuselectsecondary";
	public const string MenuBack = "menuback";
	public const string TeamChat = "menuteamchat";
	public const string AllChat = "menuallchat";
	public const string Taunt = "taunt";
	public const string Special2 = "command";

	public static JoystickInfo? joystick;

	public static bool isJoystick() {
		return joystick != null;
	}

	public static string getKeyName(string inputName) {
		return getKeyName(getKeyboardMapping(-1, 0).GetValueOrDefault(inputName));
	}

	// Given an input name, get either the friendly button text if joystick is plugged in, or keyboard friendly text
	public static string getKeyOrButtonName(string inputName, bool isController) {
		if (isController) {
			var cMap = getControllerMapping(-1, 0);
			if (cMap.ContainsKey(inputName)) {
				return getButtonName(cMap[inputName]);
			}
		}
		return getKeyName(getKeyboardMapping(-1, 0).GetValueOrDefault(inputName));
	}

	public static string getKeyOrButtonName(string inputName, Dictionary<string, int?> mappingClone, bool isController) {
		if (!mappingClone.ContainsKey(inputName)) {
			return "";
		}

		if (isController) {
			return getButtonName(mappingClone[inputName]);
		}
		return getKeyName(mappingClone[inputName]);
	}

	public static string getKeyName(int? keyCode) {
		if (keyCode == null) return "";
		Key key = (Key)keyCode;

		if (key == Key.Enter) return "Enter";

		return Enum.GetName(typeof(Key), key) ?? "";
	}

	public static string getButtonName(int? button) {
		if (button == null) return "";
		else {
			switch (button) {
				default: return "button " + button.ToString();
			}
		}
	}

	public static string getControllerName() {
		if (joystick == null) return "";
		return joystick.name;
	}

	public static string getCharSpecificName(string baseStr, int charNum, int aimMode) {
		if (charNum == -1 || charNum == -2) return baseStr;

		baseStr += "_";
		if (charNum == 0) baseStr += "x";
		if (charNum == 1) baseStr += "zero";
		if (charNum == 2) baseStr += "vile";
		if (charNum == 3) {
			baseStr += "axl";
			if (aimMode == 0) baseStr += "_dir";
			if (aimMode == 1) baseStr += "_ang";
			if (aimMode == 2) baseStr += "_cur";
		}
		if (charNum == 4) baseStr += "sigma";
		return baseStr;
	}

	public static Dictionary<string, int?> getKeyboardMapping(int charNum, int aimMode, bool inControlMenu = false) {
		string charSpecificName = getCharSpecificName(KeyboardName, charNum, aimMode);

		var baseKeyboardMapping = new Dictionary<string, int?>(controllerNameToMapping[KeyboardName]);
		var charSpecificMapping = controllerNameToMapping.GetValueOrCreate(charSpecificName, new Dictionary<string, int?>());

		if (inControlMenu) return charSpecificMapping;

		foreach (var kvp in charSpecificMapping) {
			if (kvp.Value != null) {
				baseKeyboardMapping[kvp.Key] = kvp.Value;
			}
		}

		return baseKeyboardMapping;
	}

	public static void setKeyboardMapping(Dictionary<string, int?> val, int charNum, int aimMode) {
		controllerNameToMapping[getCharSpecificName(KeyboardName, charNum, aimMode)] = val;
	}

	public static Dictionary<string, int?> getControllerMapping(int charNum, int aimMode, bool inControlMenu = false) {
		try {
			string controllerName = getControllerName();
			string charSpecificName = getCharSpecificName(controllerName, charNum, aimMode);

			var baseControllerMapping = new Dictionary<string, int?>(controllerNameToMapping[controllerName]);
			var charSpecificMapping = controllerNameToMapping.GetValueOrCreate(charSpecificName, new Dictionary<string, int?>());

			if (inControlMenu) return charSpecificMapping;

			foreach (var kvp in charSpecificMapping) {
				if (kvp.Value != null) {
					baseControllerMapping[kvp.Key] = kvp.Value;
				}
			}

			return baseControllerMapping;
		} catch {
			return new Dictionary<string, int?>();
		}
	}

	public static void setControllerMapping(Dictionary<string, int?> val, int charNum, int aimMode) {
		controllerNameToMapping[getCharSpecificName(getControllerName(), charNum, aimMode)] = val;
	}

	public static Dictionary<string, int?> getGenericMapping() {
		return new Dictionary<string, int?>()
		{
				{ Up, -1001 },
				{ Down, 1001 },
				{ Left, -1000 },
				{ Right, 1000 },
				{ MenuUp, -1001 },
				{ MenuDown, 1001 },
				{ MenuLeft, -1000 },
				{ MenuRight, 1000 },
				{ Jump, 0 },
				{ Shoot, 2 },
				{ Dash, 1 },
				{ Special1, 3 },
				{ WeaponLeft, 4 },
				{ WeaponRight, 5 },
				{ Scoreboard, 8 },
				{ MenuPause, 9 },
				{ MenuConfirm, 0 },
				{ MenuAlt, 2 },
				{ MenuBack, 1 },
				{ AimUp, -1001 },
				{ AimDown, 1001 },
				{ AimLeft, -1000 },
				{ AimRight, 1000 },
				{ AimAngleUp, -1001 },
				{ AimAngleDown, 1001 },
			};
	}

	private static Dictionary<string, Dictionary<string, int?>>? _controllerNameToMapping;
	public static Dictionary<string, Dictionary<string, int?>> controllerNameToMapping {
		get {
			if (_controllerNameToMapping == null) {
				string text = Helpers.ReadFromFile("controls.json");
				if (string.IsNullOrEmpty(text)) {
					_controllerNameToMapping = new Dictionary<string, Dictionary<string, int?>>()
					{
							// Default keyboard controls
							{
							   KeyboardName,
							   new Dictionary<string, int?>()
							   {
									{ Up, (int)Key.Up },
									{ Down, (int)Key.Down },
									{ Left, (int)Key.Left },
									{ Right, (int)Key.Right },
									{ Jump, (int)Key.X },
									{ Shoot, (int)Key.C },
									{ Dash, (int)Key.Z },
									{ Special1, (int)Key.V },
									{ WeaponLeft, (int)Key.D },
									{ WeaponRight, (int)Key.F },
									{ Scoreboard, (int)Key.Tab },
									{ TeamChat, (int)Key.T },
									{ AllChat, (int)Key.Y },
									{ Taunt, (int)Key.G },
									{ Special2, (int)Key.A },
									// Weird special keys
									{ MenuUp, (int)Key.Up },
									{ MenuDown, (int)Key.Down },
									{ MenuLeft, (int)Key.Left },
									{ MenuRight, (int)Key.Right },
									{ MenuPause, (int)Key.Escape },
									{ MenuConfirm, (int)Key.X },
									{ MenuAlt, (int)Key.Z },
									{ MenuBack, (int)Key.C },
							   }
							},
							// Axl directional aim controls
							{
							   getCharSpecificName(KeyboardName, 3, 0),
							   new Dictionary<string, int?>()
							   {
									{ AimUp, (int)Key.Up },
									{ AimDown, (int)Key.Down },
									{ AimLeft, (int)Key.Left },
									{ AimRight, (int)Key.Right },
									{ AxlAimBackwards, (int)Key.S }
							   }
							},
							// Axl angular aim controls
							{
							   getCharSpecificName(KeyboardName, 3, 1),
							   new Dictionary<string, int?>()
							   {
									{ AimAngleUp, (int)Key.Up },
									{ AimAngleDown, (int)Key.Down },
									{ AimAngleReset, (int)Key.Space },
									{ AxlAimBackwards, (int)Key.LShift }
							   }
							},
							// Axl cursor aim controls
							{
							   getCharSpecificName(KeyboardName, 3, 2),
							   new Dictionary<string, int?>()
							   {
									{ Up, (int)Key.W },
									{ Down, (int)Key.S },
									{ Left, (int)Key.A },
									{ Right, (int)Key.D },
									{ Jump, (int)Key.Space },
									{ Shoot, (int)Key.RControl },
									{ Dash, (int)Key.LShift },
									{ Special1, (int)Key.F },
									{ WeaponLeft, (int)Key.Q },
									{ WeaponRight, (int)Key.E },
									{ AimUp, (int)Key.Up },
									{ AimDown, (int)Key.Down },
									{ AimLeft, (int)Key.Left },
									{ AimRight, (int)Key.Right },
									{ Taunt, (int)Key.G },
									{ Special2, (int)Key.C },
							   }
							}
						};
				} else {
					try {
						_controllerNameToMapping = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int?>>>(text);
					} catch {
						throw new Exception("Your controls.json file is corrupted, or no longer works with this version. Please delete it, launch the game again and rebind your controls.");
					}
				}
			}
			return _controllerNameToMapping!;
		}
	}

	public static void saveToFile() {
		string text = JsonConvert.SerializeObject(_controllerNameToMapping);
		Helpers.WriteToFile("controls.json", text);
	}

	public static bool isNumberBound(int charNum, int axlAimMode) {
		foreach (var map in getKeyboardMapping(charNum, axlAimMode)) {
			if (map.Value == (int)Key.Num1 ||
				map.Value == (int)Key.Num2 ||
				map.Value == (int)Key.Num3 ||
				map.Value == (int)Key.Num4 ||
				map.Value == (int)Key.Num5 ||
				map.Value == (int)Key.Num6 ||
				map.Value == (int)Key.Num7 ||
				map.Value == (int)Key.Num8 ||
				map.Value == (int)Key.Num9) {
				return true;
			}
		}
		return false;
	}
}

public class JoystickInfo {
	public uint id;
	public string name;

	public JoystickInfo(uint id) {
		this.id = id;
		name = Joystick.GetIdentification(id).Name;
	}
}
