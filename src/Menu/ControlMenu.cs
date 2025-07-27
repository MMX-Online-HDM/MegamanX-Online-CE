using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class ControlMenu : IMainMenu {
	public int selectArrowPosY;
	public IMainMenu previous;
	public bool listenForKey = false;
	public int bindFrames = 0;
	public string error;
	public bool inGame;
	Dictionary<string, int?> mappingClone;
	public bool startedAsJoystick;
	public bool isController;
	public List<string[]> bindableControls;
	const float ySpace = 7;
	const uint fontSize = 24;
	public const float startX = 70;

	public int charNum;
	public int axlAimMode;

	public ControlMenu(IMainMenu mainMenu, bool inGame, bool isController, int charNum, int axlAimMode) {
		previous = mainMenu;
		this.inGame = inGame;
		this.isController = isController;
		this.charNum = charNum;
		this.axlAimMode = axlAimMode;

		if (isController) {
			mappingClone = new Dictionary<string, int?>(Control.getControllerMapping(charNum, axlAimMode, true));
			startedAsJoystick = true;
		} else {
			mappingClone = new Dictionary<string, int?>(Control.getKeyboardMapping(charNum, axlAimMode, true));
		}

		bindableControls = new List<string[]>() {
				new string[] { Control.Up, "Up" },
				new string[] { Control.Down, "Down" },
				new string[] { Control.Left, "Left" },
				new string[] { Control.Right, "Right" },
				new string[] { Control.Jump, "Jump" },
				new string[] { Control.Shoot, "Shoot" },
				new string[] { Control.Dash, "Dash" },
				new string[] { Control.Special1, "Special" },
				new string[] { Control.WeaponLeft, "WeaponL" },
				new string[] { Control.WeaponRight, "WeaponR" },
				new string[] { Control.Special2, "Command"},
				new string[] { Control.Taunt, "Taunt" }
			};

		// General menu controls not to be overridden on characters
		if (charNum == -2) {
			bindableControls = new List<string[]>();
			bindableControls.Add(new string[] { Control.MenuUp, "Up" });
			bindableControls.Add(new string[] { Control.MenuDown, "Down" });
			bindableControls.Add(new string[] { Control.MenuLeft, "Left" });
			bindableControls.Add(new string[] { Control.MenuRight, "Right" });
			bindableControls.Add(new string[] { Control.MenuPause, "Start" });
			bindableControls.Add(new string[] { Control.MenuConfirm, "Confirm" });
			bindableControls.Add(new string[] { Control.MenuBack, "Back/Cancel" });
			bindableControls.Add(new string[] { Control.MenuAlt, "Alt/Switch" });
			bindableControls.Add(new string[] { Control.Scoreboard, "Scoreboard" });
			bindableControls.Add(new string[] { Control.AllChat, "All Chat" });
			bindableControls.Add(new string[] { Control.TeamChat, "Team Chat" });
		}
		/* if (charNum == 4) {
			bindableControls.Add(new string[] { Control.SigmaCommand, "Command Button" });
		} */

		// Axl specific settings
		if (charNum == 3) {
			if (axlAimMode == 0) {
				bindableControls.Add(new string[] { Control.AimUp, "Aim Up" });
				bindableControls.Add(new string[] { Control.AimDown, "Aim Down" });
				bindableControls.Add(new string[] { Control.AimLeft, "Aim Left" });
				bindableControls.Add(new string[] { Control.AimRight, "Aim Right" });
				bindableControls.Add(new string[] { Control.AxlAimBackwards, "Aim Key" });
				bindableControls.Add(new string[] { Control.AxlCrouch, "Crouch" });
			} else if (axlAimMode == 1) {
				bindableControls.Add(new string[] { Control.AimAngleUp, "Aim Angle Up" });
				bindableControls.Add(new string[] { Control.AimAngleDown, "Aim Angle Down" });
				bindableControls.Add(new string[] { Control.AimAngleReset, "Aim Angle Reset" });
				bindableControls.Add(new string[] { Control.AxlAimBackwards, "Aim Backwards" });
				bindableControls.Add(new string[] { Control.AxlCrouch, "Crouch" });
			} else if (axlAimMode == 2) {
				bindableControls.Add(new string[] { Control.AimUp, "Aim Up" });
				bindableControls.Add(new string[] { Control.AimDown, "Aim Down" });
				bindableControls.Add(new string[] { Control.AimLeft, "Aim Left" });
				bindableControls.Add(new string[] { Control.AimRight, "Aim Right" });
			}
		}
	}

	public bool isBindingControl() {
		return listenForKey;
	}

	public void update() {
		if (isController && !Control.isJoystick()) {
			Menu.change(previous);
			return;
		}

		if (!listenForKey && !string.IsNullOrEmpty(error)) {
			if (Global.input.isPressedMenu(Control.MenuConfirm)) {
				error = null;
			}
			return;
		}

		if (listenForKey) {
			if (bindFrames > 0) {
				bindFrames--;
				if (bindFrames <= 0) {
					bindFrames = 0;
					listenForKey = false;
				}
			}
			return;
		}

		Helpers.menuUpDown(ref selectArrowPosY, 0, bindableControls.Count - 1);
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			/*
			if (mappingClone.Any(v => v.Key != Control.AimLock && v.Value == null))
			{
				error = "Error: Missing binding(s).";
				return;
			}
			*/

			if (isController) {
				Control.setControllerMapping(mappingClone, charNum, axlAimMode);
			} else {
				Control.setKeyboardMapping(mappingClone, charNum, axlAimMode);
			}

			Control.saveToFile();

			Menu.change(previous);
		} else if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			listenForKey = true;
		} else if (Global.input.isPressedMenu(Control.MenuAlt)) {
			string inputName = bindableControls[selectArrowPosY][0];
			if (mappingClone.ContainsKey(inputName)) mappingClone[inputName] = null;
		}
	}

	public void bind(int key) {
		string inputName = bindableControls[selectArrowPosY][0];

		if (!mappingClone.ContainsKey(inputName) || mappingClone[inputName] != key) {
			var keysToClear = new List<string>();
			foreach (var kvp in mappingClone) {
				if (kvp.Key.StartsWith("menu", StringComparison.OrdinalIgnoreCase)) continue;
				if (inputName.StartsWith("menu", StringComparison.OrdinalIgnoreCase)) continue;
				if (kvp.Key.StartsWith("aim", StringComparison.OrdinalIgnoreCase)) continue;
				if (inputName.StartsWith("aim", StringComparison.OrdinalIgnoreCase)) continue;
				if (kvp.Key.StartsWith("command", StringComparison.OrdinalIgnoreCase)) continue;
				if (inputName.StartsWith("command", StringComparison.OrdinalIgnoreCase)) continue;
				// Jump and up are the only only other exceptions to the "can't bind multiple with one key" rule
				if ((kvp.Key == Control.Jump && inputName == Control.Up) ||
					kvp.Key == Control.Up && inputName == Control.Jump
				) {
					continue;
				}
				if (kvp.Value == key) {
					keysToClear.Add(kvp.Key);
				}
			}
			foreach (var keyToClear in keysToClear) {
				mappingClone[keyToClear] = null;
			}

			mappingClone[inputName] = key;
		}

		bindFrames = 3;
		selectArrowPosY++;
		if (selectArrowPosY > bindableControls.Count - 1) selectArrowPosY = bindableControls.Count - 1;
	}

	public void render() {
		var topLeft = new Point(startX + 86, 34);
		int startYOff = 8;
		int cursorYOff = 6;

		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			//DrawWrappers.DrawTextureHUD(Global.textures["cursor"], startX, topLeft.y + startYOff + (selectArrowPosY * 8) + cursorYOff);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			//Global.sprites["cursor"].drawToHUD(0, startX, topLeft.y + startYOff + (selectArrowPosY * 8) + cursorYOff + 5);
		}

		string subtitle = charNum switch {
			-2 => "MENU CONTROLS",
			0 => "X CONTROLS",
			1 => "ZERO CONTROLS",
			2 => "VILE CONTROLS",
			3 when (axlAimMode == 0) => "AXL CONTROLS (CURSOR)",
			3 when (axlAimMode == 1) => "AXL CONTROLS (ANGULAR)",
			3 => "AXL CONTROLS (DIRECTIONAL)",
			4 => "SIGMA CONTROLS",
			_ => "GENERAL CONTROLS"
		};
		if (charNum != 3) {
			Fonts.drawText(inGame ? FontType.Yellow : FontType.Golden, subtitle, Global.halfScreenW,
			24, Alignment.Center); //Not axl
		} else {
			Fonts.drawText(inGame ? FontType.Yellow : FontType.Golden, subtitle, Global.halfScreenW,
			inGame ? - 10 : 3, Alignment.Center); // Axl
		}

		if (isController) {
				Fonts.drawText(
					inGame ? FontType.Yellow : FontType.Golden, "CONTROLLER CONFIG \"" +
					Control.getControllerName() + "\"",
					Global.halfScreenW, 44, alignment: Alignment.Center
				);
			} else {
				if (charNum != 3) { //Not Axl
					Fonts.drawText(
						inGame ? FontType.Yellow : FontType.Golden, "KEY CONFIG",
						Global.halfScreenW, 36, alignment: Alignment.Center
					);
				} else { // Axl
					Fonts.drawText(
						inGame ? FontType.Yellow : FontType.Golden, "KEY CONFIG",
						Global.halfScreenW, 12, alignment: Alignment.Center
					);
				}
			}

		for (int i = 0; i < bindableControls.Count; i++) {
			var bindableControl = bindableControls[i];
			string boundKeyDisplay = Control.getKeyOrButtonName(bindableControl[0], mappingClone, isController);
			if (string.IsNullOrEmpty(boundKeyDisplay) && charNum > -1) {
				boundKeyDisplay = "(Inherit)";
			}
			Fonts.drawText(
				inGame ? FontType.Blue : FontType.DarkBlue,
				bindableControl[1] + "",
				topLeft.x,
				charNum != 3 ? topLeft.y + startYOff + 9 * (i + 1) //Not Axl
				: topLeft.y - 29 + startYOff + 9 * (i + 1), // Axl
				alignment: Alignment.Center, selected: selectArrowPosY == i, selectedFont: FontType.Pink
			);
			Fonts.drawText(
				inGame ? FontType.Blue : FontType.DarkOrange, boundKeyDisplay,
				boundKeyDisplay != null ? topLeft.x + 70 : topLeft.x + 80,
				charNum != 3 ? topLeft.y + startYOff + 9 * (i + 1) //Not Axl
				: topLeft.y - 29 + startYOff + 9 * (i + 1), // Axl
				alignment: Alignment.Center, selected: selectArrowPosY == i,
				selectedFont: inGame ? FontType.Blue : FontType.DarkOrange
			);
		}

		if (!listenForKey) {
			Fonts.drawTextEX(
				FontType.Grey, "[OK] = Bind, [ALT] = Unbind, [BACK] = Save/Back",
				Global.halfScreenW, Global.screenH - 16, Alignment.Center
			);
		} else {
			if (isController) {
				Fonts.drawText(
					FontType.Grey, "Press desired controller button to bind",
					Global.halfScreenW, Global.screenH - 16, Alignment.Center
				);
			} else {
				Fonts.drawText(
					FontType.Grey, "Press desired key to bind",
					Global.halfScreenW, Global.screenH - 16, Alignment.Center
				);
			}
		}

		if (!string.IsNullOrEmpty(error)) {
			float top = Global.screenH * 0.4f;
			DrawWrappers.DrawRect(
				17, 17, Global.screenW - 17, Global.screenH - 17, true,
				new Color(0, 0, 0, 224), 0, ZIndex.HUD, false
			);
			Fonts.drawText(FontType.Red, "ERROR", Global.screenW / 2, top - 20, alignment: Alignment.Center);
			Fonts.drawText(FontType.RedishOrange, error, Global.screenW / 2, top, alignment: Alignment.Center);
			Fonts.drawTextEX(
				FontType.Grey, Helpers.controlText("Press [OK] to continue"),
				Global.screenW / 2, 20 + top, alignment: Alignment.Center
			);
		}
		if (charNum != 3) { //Not Axl
			for (int i = 0; i < 40; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(1, Global.halfScreenW - 80, Global.halfScreenH - 64 + i * 3);
			for (int i = 0; i < 40; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(1, Global.halfScreenW + 80, Global.halfScreenH - 64 + i * 3);
			for (int i = 0; i < 51; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(0, Global.halfScreenW - 75 + i * 3, Global.halfScreenH + 58);
			for (int i = 0; i < 11; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(0, Global.halfScreenW - 75 + i * 3, Global.halfScreenH - 69);
			for (int i = 0; i < 11; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(0, Global.halfScreenW + 45 + i * 3, Global.halfScreenH - 69);
			Global.sprites["optionMode_tubes"].drawToHUD(5, Global.halfScreenW - 79, Global.halfScreenH + 58);
			Global.sprites["optionMode_tubes"].drawToHUD(4, Global.halfScreenW + 80, Global.halfScreenH + 58);
			Global.sprites["optionMode_tubes"].drawToHUD(7, Global.halfScreenW - 79, Global.halfScreenH - 68);
			Global.sprites["optionMode_tubes"].drawToHUD(6, Global.halfScreenW + 80, Global.halfScreenH - 68);
			Global.sprites["optionMode_tubes"].drawToHUD(2, Global.halfScreenW + 42, Global.halfScreenH - 69);
			Global.sprites["optionMode_tubes"].drawToHUD(3, Global.halfScreenW - 42, Global.halfScreenH - 69);
		} else { //Axl
			for (int i = 0; i < 56; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(1, Global.halfScreenW - 80, Global.halfScreenH - 88 + i * 3);
			for (int i = 0; i < 56; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(1, Global.halfScreenW + 80, Global.halfScreenH - 88 + i * 3);
			for (int i = 0; i < 51; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(0, Global.halfScreenW - 75 + i * 3, Global.halfScreenH + 80);
			for (int i = 0; i < 11; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(0, Global.halfScreenW - 75 + i * 3, Global.halfScreenH - 94);
			for (int i = 0; i < 11; i++)
				Global.sprites["optionMode_tubes"].drawToHUD(0, Global.halfScreenW + 45 + i * 3, Global.halfScreenH - 93);
			Global.sprites["optionMode_tubes"].drawToHUD(5, Global.halfScreenW - 79, Global.halfScreenH + 80);
			Global.sprites["optionMode_tubes"].drawToHUD(4, Global.halfScreenW + 80, Global.halfScreenH + 80);
			Global.sprites["optionMode_tubes"].drawToHUD(7, Global.halfScreenW - 79, Global.halfScreenH - 92);
			Global.sprites["optionMode_tubes"].drawToHUD(6, Global.halfScreenW + 80, Global.halfScreenH - 92);
			Global.sprites["optionMode_tubes"].drawToHUD(2, Global.halfScreenW + 42, Global.halfScreenH - 93);
			Global.sprites["optionMode_tubes"].drawToHUD(3, Global.halfScreenW - 42, Global.halfScreenH - 94);
		}
	}
}
