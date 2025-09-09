
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SFML.Window;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public enum InputAction {
	MenuUp,
	MenuDown,
	MenuLeft,
	MenuRight,
	MenuSelectPrimary,
	MenuSelectSecondary,
	MenuBack,
	MenuSlideLeft,
	MenuSlideRight,
	Up,
	Down,
	Left,
	Right,
	Action,
	Sword,
	Map,
	Item1,
	Item2,
	Item3,
	Item4,
	Item5,
	DropItem1,
	DropItem2,
	DropItem3,
	DropItem4,
	DropItem5,
	CycleItemLeft,
	CycleItemRight,
	UseItem,
	DropItem
}

public class Input {
	public Dictionary<Key, bool> keyHeld = new Dictionary<Key, bool>();
	public Dictionary<Key, bool> keyPressed = new Dictionary<Key, bool>();

	public Dictionary<int, bool> buttonHeld = new Dictionary<int, bool>();
	public Dictionary<int, bool> buttonPressed = new Dictionary<int, bool>();

	// Used in Viral Sigma possess scenarios
	public Dictionary<string, bool> possessedControlHeld = new Dictionary<string, bool>();
	public Dictionary<string, bool> possessedControlPressed = new Dictionary<string, bool>();

	// 0 is always current frame, 1 is prev frame, all the way to 60
	public List<List<string>> frameToHeldControls = new List<List<string>>();
	public List<List<string>> frameToPressedControls = new List<List<string>>();

	public float lastUpdateTime;

	public Input(bool isAI) {
		this.isAI = isAI;
		for (int i = 0; i < 60; i++) {
			frameToHeldControls.Add(new List<string>());
			frameToPressedControls.Add(new List<string>());
		}
	}

	public static readonly List<string> frameControls = new List<string>()
	{
			Control.Left,
			Control.Right,
			Control.Up,
			Control.Down,
			Control.Special1,
			Control.Shoot,
			Control.Dash
		};

	public void updateFrameToPressedControls(Player player) {
		// Shift everything down
		for (int i = 59; i > 0; i--) {
			frameToHeldControls[i] = frameToHeldControls[i - 1];
			frameToPressedControls[i] = frameToPressedControls[i - 1];
		}

		var pressedControls = new List<string>();
		var heldControls = new List<string>();
		foreach (var frameControl in frameControls) {
			if (isPressed(frameControl, player)) pressedControls.Add(frameControl);
		}
		frameToPressedControls[0] = pressedControls;

		foreach (var frameControl in frameControls) {
			if (isHeld(frameControl, player)) heldControls.Add(frameControl);
		}
		frameToHeldControls[0] = heldControls;

		/*
		Global.debugString1 = "";
		for (int i = 0; i < 60; i++)
		{
			foreach (string c in frameToHeldControls[i])
			{
				Global.debugString1 += c[0];
			}
			Global.debugString1 += ",";
		}
		*/
	}

	// Used by X's dash
	public bool checkDoubleTap(string control) {
		if (frameToPressedControls[1].Contains(control)) return false;
		for (int i = 2; i < 13; i++) {
			if (frameToPressedControls[i].Contains(Control.Up) || frameToPressedControls[i].Contains(Control.Down)) return false;
			if (frameToPressedControls[i].Contains(control)) {
				return true;
			}
		}
		return false;
	}

	// Used by Zero's techniques
	public bool checkDoubleTap2(string control) {
		bool gapFound = false;
		bool afterGapFound = false;
		for (int i = 2; i < 13; i++) {
			if (!frameToHeldControls[i].Contains(control)) {
				gapFound = true;
			} else {
				if (gapFound) afterGapFound = true;
			}
		}
		return afterGapFound;
	}

	public bool checkHadoken(Player player, int xDir, string triggerBtn) {
		if (xDir == 1) {
			return checkHadokenHelper("right", player, triggerBtn);
		} else if (xDir == -1) {
			return checkHadokenHelper("left", player, triggerBtn);
		}
		return false;
	}

	public bool checkShoryuken(Player player, int xDir, string triggerBtn) {
		if (xDir == 1) {
			return checkShoryukenHelper("right", player, triggerBtn);
		} else if (xDir == -1) {
			return checkShoryukenHelper("left", player, triggerBtn);
		}
		return false;
	}

	private int[][] commandList = {
		new int[3],
		new int[3]
	};

	// TODO: Un-hardcode the input check.
	private bool checkHadokenHelper(string forwardDir, Player player, string triggerBtn) {
		int[] command = commandList[0];
		int time = 30;
		bool completed = false;

		if (command[2] > 0) {
			if (isPressed(triggerBtn, player)) {
				command[2] = 0;
				completed = true;
			} else {
				command[2]--;
			}
		}
		if (command[1] > 0) {
			if (!isHeld("down", player) && isHeld(forwardDir, player)) {
				command[2] = command[1];
				command[1] = 0;
			} else {
				command[1]--;
			}
		}
		if (command[0] > 0) {
			if (isHeld("down", player) && isHeld(forwardDir, player)) {
				command[1] = command[0];
				command[0] = 0;
			} else {
				command[0]--;
			}
		}
		if (isHeld("down", player) && !isHeld(forwardDir, player)) {
			command[0] = time;
		}

		return completed;
	}

	private bool checkShoryukenHelper(string forwardDir, Player player, string triggerBtn) {
		int[] command = commandList[1];
		int time = 30;
		bool completed = false;

		if (command[2] > 0) {
			if (isPressed(triggerBtn, player)) {
				command[2] = 0;
				completed = true;
			} else {
				command[2]--;
			}
		}
		if (command[1] > 0) {
			if (isHeld("down", player) && isHeld(forwardDir, player)) {
				command[2] = command[1];
				command[1] = 0;
			} else {
				command[1]--;
			}
		}
		if (command[0] > 0) {
			if (isHeld("down", player) && !isHeld(forwardDir, player)) {
				command[1] = command[0];
				command[0] = 0;
			} else {
				command[0]--;
			}
		}
		if (!isHeld("down", player) && isHeld(forwardDir, player)) {
			command[0] = time;
		}
		return completed;
	}

	public Dictionary<Key, char> capsLockMapping = new Dictionary<Key, char>() {
		{ Key.A, 'A' },
		{ Key.B, 'B' },
		{ Key.C, 'C' },
		{ Key.D, 'D' },
		{ Key.E, 'E' },
		{ Key.F, 'F' },
		{ Key.G, 'G' },
		{ Key.H, 'H' },
		{ Key.I, 'I' },
		{ Key.J, 'J' },
		{ Key.K, 'K' },
		{ Key.L, 'L' },
		{ Key.M, 'M' },
		{ Key.N, 'N' },
		{ Key.O, 'O' },
		{ Key.P, 'P' },
		{ Key.Q, 'Q' },
		{ Key.R, 'R' },
		{ Key.S, 'S' },
		{ Key.T, 'T' },
		{ Key.U, 'U' },
		{ Key.V, 'V' },
		{ Key.W, 'W' },
		{ Key.X, 'X' },
		{ Key.Y, 'Y' },
		{ Key.Z, 'Z' },
	};

	public Dictionary<Key, char> keyToCharMappingShift = new Dictionary<Key, char>() {
		{ Key.A, 'A' },
		{ Key.B, 'B' },
		{ Key.C, 'C' },
		{ Key.D, 'D' },
		{ Key.E, 'E' },
		{ Key.F, 'F' },
		{ Key.G, 'G' },
		{ Key.H, 'H' },
		{ Key.I, 'I' },
		{ Key.J, 'J' },
		{ Key.K, 'K' },
		{ Key.L, 'L' },
		{ Key.M, 'M' },
		{ Key.N, 'N' },
		{ Key.O, 'O' },
		{ Key.P, 'P' },
		{ Key.Q, 'Q' },
		{ Key.R, 'R' },
		{ Key.S, 'S' },
		{ Key.T, 'T' },
		{ Key.U, 'U' },
		{ Key.V, 'V' },
		{ Key.W, 'W' },
		{ Key.X, 'X' },
		{ Key.Y, 'Y' },
		{ Key.Z, 'Z' },
		{ Key.Num1, '!' },
		{ Key.Num2, '@' },
		{ Key.Num3, '#' },
		{ Key.Num4, '$' },
		{ Key.Num5, '%' },
		{ Key.Num6, '^' },
		{ Key.Num7, '&' },
		{ Key.Num8, '*' },
		{ Key.Num9, '(' },
		{ Key.Num0, ')' },
		{ Key.Hyphen, '_' },
		{ Key.Equal, '+' },
		{ Key.LBracket, '[' },
		{ Key.RBracket, ']' },
		{ Key.Semicolon, ':' },
		{ Key.Apostrophe, '"' },
		{ Key.Comma, '<' },
		{ Key.Period, '>' },
		{ Key.Slash, '?' },
		{ Key.Backspace, backspaceChar },
	};

	public Dictionary<Key, char> keyToCharMapping = new Dictionary<Key, char>() {
		{ Key.A, 'a' },
		{ Key.B, 'b' },
		{ Key.C, 'c' },
		{ Key.D, 'd' },
		{ Key.E, 'e' },
		{ Key.F, 'f' },
		{ Key.G, 'g' },
		{ Key.H, 'h' },
		{ Key.I, 'i' },
		{ Key.J, 'j' },
		{ Key.K, 'k' },
		{ Key.L, 'l' },
		{ Key.M, 'm' },
		{ Key.N, 'n' },
		{ Key.O, 'o' },
		{ Key.P, 'p' },
		{ Key.Q, 'q' },
		{ Key.R, 'r' },
		{ Key.S, 's' },
		{ Key.T, 't' },
		{ Key.U, 'u' },
		{ Key.V, 'v' },
		{ Key.W, 'w' },
		{ Key.X, 'x' },
		{ Key.Y, 'y' },
		{ Key.Z, 'z' },
		{ Key.Num0, '0' },
		{ Key.Num1, '1' },
		{ Key.Num2, '2' },
		{ Key.Num3, '3' },
		{ Key.Num4, '4' },
		{ Key.Num5, '5' },
		{ Key.Num6, '6' },
		{ Key.Num7, '7' },
		{ Key.Num8, '8' },
		{ Key.Num9, '9' },
		{ Key.Space, ' ' },
		{ Key.Hyphen, '-' },
		{ Key.Equal, '=' },
		{ Key.LBracket, '[' },
		{ Key.RBracket, ']' },
		{ Key.Semicolon, ';' },
		{ Key.Apostrophe, '\'' },
		{ Key.Comma, ',' },
		{ Key.Period, '.' },
		{ Key.Slash, '/' },
		{ Key.Backspace, backspaceChar },
	};

	public int mashCount;

	public const char backspaceChar = '~';

	public static float mouseX;
	public static float mouseY;
	public static float mouseDeltaX;
	public static float mouseDeltaY;

	public static bool mouseScrollUp;
	public static bool mouseScrollDown;

	public static float aimX;
	public static float aimY;
	public static float lastAimX;
	public static float lastAimY;
	public static float aimX2;
	public static float aimY2;

	public (int x, int y) lastDirPressed = (1, 1);

	public bool isAI;

	public bool isPressedMenu(string inputName) {
		return isPressed(inputName, null);
	}

	public bool isHeldMenu(string inputName) {
		return isHeld(inputName, null);
	}

	public int getMenuXDir() {
		int dir = 0;
		if (isPressedMenu(Control.MenuLeft)) { dir--; }
		if (isPressedMenu(Control.MenuRight)) { dir++; }
		return dir;
	}

	public Dictionary<string, int> heldFrames = new Dictionary<string, int>();
	public bool isPressedOrHeldMenu(string inputName) {
		if (!heldFrames.ContainsKey(inputName)) heldFrames[inputName] = 0;

		if (isPressedMenu(inputName)) {
			return true;
		} else if (isHeldMenu(inputName)) {
			if (heldFrames[inputName] > 20) {
				return true;
			}
			heldFrames[inputName]++;
			return false;
		} else {
			heldFrames[inputName] = 0;
			return false;
		}
	}

	public bool useAxlCursorControls(Player? player) {
		return (
			player != Global.level.mainPlayer &&
			Options.main.useMouseAim &&
			!isAI &&
			player?.character is Axl
		);
	}

	public bool isAimingBackwards(Player player) {
		if (player.character is not Axl axl) {
			return false;
		}
		if (axl.aimBackwardsToggle == true) {
			return true;
		}
		return Options.main.aimKeyFunction == 0 && isHeld(Control.AxlAimBackwards, player);
	}

	public bool isPositionLocked(Player player) {
		if (player.character is not Axl axl) {
			return false;
		}
		if (axl.positionLockToggle == true) {
			return true;
		}
		return Options.main.aimKeyFunction == 1 && isHeld(Control.AxlAimBackwards, player);
	}

	public bool isCursorLocked(Player player) {
		if (player.character is not Axl axl) {
			return false;
		}
		if (axl.cursorLockToggle == true) {
			return true;
		}
		return Options.main.aimKeyFunction == 2 && isHeld(Control.AxlAimBackwards, player);
	}

	public void updateAimToggle(Player player) {
		if (!player.ownedByLocalPlayer) return;
		if (player.character is not Axl axl) return;

		if (!Options.main.aimKeyToggle || Options.main.axlAimMode == 2) {
			axl.resetToggle();
			return;
		}

		if (isPressed(Control.AxlAimBackwards, player)) {
			if (Options.main.aimKeyFunction == 0) {
				axl.aimBackwardsToggle = !axl.aimBackwardsToggle;
			} else if (Options.main.aimKeyFunction == 1) {
				axl.positionLockToggle = !axl.positionLockToggle;
			} else if (Options.main.aimKeyFunction == 2) {
				axl.cursorLockToggle = !axl.cursorLockToggle;
			}
		}
	}

	public bool allowInput(Player player, string inputName) {
		/*
		if (player != null && player.isVile && player.weapon is MechMenuWeapon mmw && mmw.isMenuOpened) {
			if (inputName == Control.Up || inputName == Control.Down) {
				return false;
			}
		}
		if (player != null && player.isSigma && player.isSelectingCommand()) {
			if (inputName == Control.Up || inputName == Control.Down) {
				return false;
			}
		}*/
		return true;
	}

	public bool isHeld(string inputName, Player? player) {
		if (possessedControlHeld.ContainsKey(inputName)) return possessedControlHeld[inputName];

		if (player != null && !player.canControl) {
			return false;
		}
		if (player == null || player.isAI) {
			var keyboardMapping2 = Control.getKeyboardMapping(-1, 0);
			int? keyboardKey2 = keyboardMapping2.GetValueOrDefault(inputName);
			if (keyboardKey2 != null && isHeld((Key)keyboardKey2)) {
				return true;
			}
			if (Control.isJoystick()) {
				var controllerMapping2 = Control.getControllerMapping(-1, 0);
				if (!controllerMapping2.ContainsKey(inputName)) return false;
				int? buttonKey = controllerMapping2[inputName];
				return isHeld(buttonKey);
			}
			return false;
		}

		if (!allowInput(player, inputName)) {
			return false;
		}
		int charId = (int?)(player?.character?.charId) ?? player?.charNum ?? -1;

		if (player?.character != null && player.gridModeHeld) {
			if (inputName == Control.Left || inputName == Control.Right ||
				inputName == Control.Up || inputName == Control.Down
			) {
				return false;
			}
		}

		if (useAxlCursorControls(player)) {
			if (inputName == Control.Shoot) {
				if (isMouseHeld(Mouse.Button.Left, true)) return true;
			} else if (inputName == Control.Special1) {
				if (isMouseHeld(Mouse.Button.Right, true)) return true;
			}
		}

		var keyboardMapping = Control.getKeyboardMapping(charId, Options.main.axlAimMode);

		int? keyboardKey = keyboardMapping.GetValueOrDefault(inputName);
		if (keyboardKey != null && isHeld((Key)keyboardKey)) {
			return true;
		}
		if (Control.isJoystick()) {
			var controllerMapping = Control.getControllerMapping(charId, Options.main.axlAimMode);
			if (!controllerMapping.ContainsKey(inputName)) {
				return false;
			}
			int? buttonKey = controllerMapping[inputName];
			return isHeld(buttonKey);
		}
		return false;
	}

	public bool isPressed(string inputName, Player? player) {
		if (possessedControlPressed.ContainsKey(inputName)) {
			return possessedControlPressed[inputName];
		}
		if (player != null && !player.canControl) {
			return false;
		}
		if (player == null || player.isAI) {
			var keyboardMapping2 = Control.getKeyboardMapping(-1, 0);
			int? keyboardKey2 = keyboardMapping2.GetValueOrDefault(inputName);
			if (keyboardKey2 != null && isPressed((Key)keyboardKey2)) {
				return true;
			}
			if (Control.isJoystick()) {
				var controllerMapping2 = Control.getControllerMapping(-1, 0);
				int? buttonKey = controllerMapping2.GetValueOrDefault(inputName);
				return isPressed(buttonKey);
			}
			return false;
		}

		if (!allowInput(player, inputName)) {
			return false;
		}
		int charId = (int?)(player?.character?.charId) ?? player?.charNum ?? -1;

		if (useAxlCursorControls(player)) {
			if (inputName == Control.Shoot) {
				if (isMousePressed(Mouse.Button.Left, true)) return true;
			} else if (inputName == Control.Special1) {
				if (isMousePressed(Mouse.Button.Right, true)) return true;
			}
		}

		var keyboardMapping = Control.getKeyboardMapping(charId, Options.main.axlAimMode);

		int? keyboardKey = keyboardMapping.GetValueOrDefault(inputName);

		if (player?.character != null && player.gridModeHeld) {
			if (inputName == Control.Left || inputName == Control.Right || inputName == Control.Up || inputName == Control.Down) {
				return false;
			}
		}

		if (keyboardKey != null && isPressed((Key)keyboardKey)) {
			return true;
		}
		if (Control.isJoystick()) {
			var controllerMapping = Control.getControllerMapping(charId, Options.main.axlAimMode);
			int? buttonKey = controllerMapping.GetValueOrDefault(inputName);
			return isPressed(buttonKey);
		}
		return false;
	}

	public void setLastUpdateTime() {
		if (Global.level != null) {
			lastUpdateTime = Global.level.time;
		} else {
			lastUpdateTime = 0;
		}
	}

	public bool isHeld(Key key, bool canControl = true) {
		if (!canControl) return false;
		if (!keyHeld.ContainsKey(key)) {
			return false;
		}
		return keyHeld[key];
	}

	public bool isPressed(Key key, bool canControl = true) {
		if (!canControl) return false;
		if (Global.autoFire == true) return isHeld(key, canControl);
		if (!keyPressed.ContainsKey(key)) {
			return false;
		}
		return keyPressed[key];
	}

	private bool isHeld(int? button) {
		if (button == null) return false;
		int buttonInt = (int)button;

		if (!buttonHeld.ContainsKey(buttonInt)) {
			return false;
		}
		return buttonHeld[buttonInt];
	}

	private bool isPressed(int? button) {
		if (button == null) return false;
		int buttonInt = (int)button;

		if (!buttonPressed.ContainsKey(buttonInt)) {
			return false;
		}
		return buttonPressed[buttonInt];
	}

	public static Dictionary<Mouse.Button, bool> mousePressed = new Dictionary<Mouse.Button, bool>();
	public static Dictionary<Mouse.Button, bool> mouseHeld = new Dictionary<Mouse.Button, bool>();

	public static bool isMousePressed(Mouse.Button button, bool canControl) {
		if (!canControl) return false;
		return mousePressed.ContainsKey(button) && mousePressed[button];
	}

	public static bool isMouseHeld(Mouse.Button button, bool canControl) {
		if (!canControl) return false;
		return mouseHeld.ContainsKey(button) && mouseHeld[button];
	}

	public bool isXDirHeld(int xDir, Player player) {
		return (getXDir(player) == xDir);
	}

	public bool isWeaponLeftOrRightPressed(Player player) {
		return isPressed(Control.WeaponLeft, player) || isPressed(Control.WeaponRight, player);
	}

	public bool isWeaponLeftOrRightHeld(Player player) {
		return isHeld(Control.WeaponLeft, player) || isHeld(Control.WeaponRight, player);
	}

	// Return if left or right is pressed. Not both.
	// We use XOR (^) for this.
	public bool isLeftOrRightHeld(Player player) {
		return (isHeld(Control.Left, player) ^ isHeld(Control.Right, player));
	}

	public bool isCommandButtonPressed(Player player) {
		return isPressed(Control.Special2, player);
	}

	public bool isCommandButtonHeld(Player player) {
		return isHeld(Control.Special2, player);
	}

	public Point getInputDir(Player player) {
		return new Point(getXDir(player), getYDir(player));
	}

	public int getXDir(Player player) {
		int xDir = 0;
		bool pressedDir = false;
		if (isHeld(Control.Left, player)) { xDir--; pressedDir = true; }
		if (isHeld(Control.Right, player)) { xDir++; pressedDir = true; }
		if (xDir == 0 && pressedDir) {
			xDir = -lastDirPressed.y;
		} else {
			lastDirPressed.y = xDir;
		}
		return xDir;
	}

	public int getYDir(Player player) {
		int yDir = 0;
		bool pressedDir = false;
		if (isHeld(Control.Up, player)) { yDir--; pressedDir = true; }
		if (isHeld(Control.Down, player)) { yDir++; pressedDir = true; }
		if (yDir == 0 && pressedDir) {
			yDir = -lastDirPressed.y;
		} else {
			lastDirPressed.y = yDir;
		}
		return yDir;
	}

#if WINDOWS
	[DllImport(
		"user32.dll", CharSet = CharSet.Auto,
		ExactSpelling = true,
		CallingConvention = CallingConvention.Winapi)
	]
	public static extern short GetKeyState(int keyCode);
	enum LockKeys {
		CapsLock = 0x14,
		NumLock = 0x90,
		ScrollLock = 0x91
	}
#endif

	public char? getKeyCharPressed() {
		foreach (var kvp in keyToCharMapping) {
			if (keyPressed.ContainsKey(kvp.Key) && keyPressed[kvp.Key]) {
#if WINDOWS
				if (((ushort)GetKeyState((int)LockKeys.CapsLock) & 0xffff) != 0) {
					if (capsLockMapping.ContainsKey(kvp.Key)) return capsLockMapping[kvp.Key];
				}
#endif
				if (keyHeld.ContainsKey(Key.LShift) && keyHeld[Key.LShift]) {
					if (keyToCharMappingShift.ContainsKey(kvp.Key)) return keyToCharMappingShift[kvp.Key];
					else return null;
				}
				return keyToCharMapping[kvp.Key];
			}
		}
		return null;
	}

	public void clearMashCount() {
		mashCount = 0;
	}

	public void clearInput() {
		foreach (var key in keyPressed.Keys.ToList()) {
			if (keyPressed[key]) {
				keyPressed[key] = false;
			}
		}
		foreach (var key in Input.mousePressed.Keys.ToList()) {
			if (Input.mousePressed[key]) {
				Input.mousePressed[key] = false;
			}
		}

		if (Control.isJoystick()) {
			foreach (uint button in buttonPressed.Keys.ToList()) {
				if (buttonPressed[(int)button]) {
					buttonPressed[(int)button] = false;
				}
			}
		}

		Input.mouseScrollUp = false;
		Input.mouseScrollDown = false;
	}
}
