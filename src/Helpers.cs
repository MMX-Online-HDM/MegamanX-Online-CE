using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ProtoBuf;
using SFML.Graphics;

namespace MMXOnline;

public class Helpers {
	public static Color Gray { get { return new Color(128, 128, 128); } }
	public static Color DarkRed { get { return new Color(192, 0, 0); } }
	public static Color DarkBlue { get { return new Color(0, 0, 192); } }
	public static Color MenuBgColor { get { return new Color(0, 0, 0, 224); } }
	public static Color FadedIconColor = new Color(0, 0, 0, 164);
	public static Color LoadoutBorderColor = new Color(138, 192, 255);
	public static Color DarkGreen {
		get {
			if (Global.level == null) {
				return new Color(64, 255, 64);
			}
			/*return new Color(
				0, (byte)(200 + (MathF.Sin(Global.time * 4) * 55)),
				(byte)(63 + (MathF.Sin(Global.time * 4) * 63)));
			*/
			return new Color(0, 209, 63);
		}
	}

	public static List<Type> wallTypeList = new List<Type> { typeof(Wall) };

	public static void decrementTime(ref float num) {
		num = clampMin0(num - Global.spf);
	}

	public static float clampMin0(float num) {
		if (num < 0) return 0;
		return num;
	}

	public static int clamp(int val, int min, int max) {
		if (min > max) return val;
		if (val < min) return min;
		if (val > max) return max;
		return val;
	}

	public static float clamp(float val, float min, float max) {
		if (min > max) return val;
		if (val < min) return min;
		if (val > max) return max;
		return val;
	}

	public static float clampMin(float val, float min) {
		if (val < min) return min;
		return val;
	}

	public static float clampMax(float val, float max) {
		if (val > max) return max;
		return val;
	}

	public static string getTypedString(string str, int maxLength) {
		var pressedChar = Global.input.getKeyCharPressed();
		if (pressedChar != null) {
			if (pressedChar == Input.backspaceChar) {
				if (str.Length > 0) {
					str = str.Substring(0, str.Length - 1);
				}
			} else if (str.Length < maxLength) {
				str += pressedChar;
			}
		}

		return str;
	}

	public static float clamp01(float val) {
		return clamp(val, 0, 1);
	}

	public static Color getAllianceColor(Player player) {
		if (player == null) {
			return Helpers.DarkBlue;
		}
		if (player.alliance == GameMode.blueAlliance) {
			return Helpers.DarkBlue;
		} else if (player.alliance == GameMode.redAlliance) {
			return Helpers.DarkRed;
		} else {
			return Helpers.DarkBlue;
		}
	}

	public static Color getAllianceColor() {
		return getAllianceColor(Global.level?.mainPlayer);
	}

	public static string addPrefix(dynamic dStr, string prefix) {
		string str = dStr ?? "";
		if (!string.IsNullOrEmpty(str)) {
			return prefix + str;
		}
		return str;
	}

	public static void drawWeaponSlotSymbol(float topLeftSlotX, float topLeftSlotY, string symbol) {
		Fonts.drawText(FontType.Yellow, symbol, topLeftSlotX + 16, topLeftSlotY + 11, Alignment.Right);
	}

	static Random rnd = new Random();
	//Inclusive
	public static int randomRange(int start, int end) {
		int rndNum = rnd.Next(start, end + 1);
		return rndNum;
	}

	public static float randomRange(float start, float end) {
		double rndNum = rnd.NextDouble() * (end - start);
		rndNum += start;
		return (float)rndNum;
	}

	public static void tryWrap(Action action, bool isServer) {
		/*
		try {
			action.Invoke();
		}
		catch (AccessViolationException) { throw; }
		catch (StackOverflowException) { throw; }
		catch (OutOfMemoryException) { throw; }
		catch (Exception e) {
			Logger.logException(e, isServer);
		}
		*/
		action.Invoke();
	}

	public static List<T> getRandomSubarray<T>(List<T> list, int count) {
		if (count >= list.Count)
			count = list.Count - 1;

		int[] indexes = Enumerable.Range(0, list.Count).ToArray();

		List<T> results = new List<T>();

		for (int i = 0; i < count; i++) {
			int j = randomRange(i, list.Count - 1);

			int temp = indexes[i];
			indexes[i] = indexes[j];
			indexes[j] = temp;

			results.Add(list[indexes[i]]);
		}

		return results;
	}

	public static string[] openglversions = new string[]
	{
			"#version 110",
			"#version 120",
			"#version 130",
			"#version 140",
			"#version 150",
			"#version 330",
			"#version 400",
			"#version 410",
			"#version 420",
			"#version 430",
			"#version 440",
			"#version 450",
			"#version 460",
	};

	public const string noShaderSupportMsg = "The system does not support shaders.";

	// Very slow, only do once on startup
	public static Shader createShader(string shaderName) {
		string shaderCode = Global.shaderCodes[shaderName];

		var result = createShaderHelper(shaderCode, "");
		if (result != null) return result;

		if (!Shader.IsAvailable) {
			var ex = new Exception(noShaderSupportMsg);
			//Logger.logException(ex, false);
			throw ex;
		}

		var ex2 = new Exception("Could not load shaders after trying all possible opengl versions.");
		//Logger.logException(ex2, false);
		throw ex2;
	}

	// Very slow, only do once on startup
	private static Shader createShaderHelper(string shaderCode, string header) {
		if (!string.IsNullOrEmpty(header)) header += Environment.NewLine;
		byte[] byteArray = Encoding.ASCII.GetBytes(header + shaderCode);
		MemoryStream stream = new MemoryStream(byteArray);
		try {
			return new Shader(null, null, stream);
		} catch {
			stream.Dispose();
			return null;
		}
	}

	// Fast way to get a new shader wrapper that remembers SetUniform state
	// while reusing the same base underlying shader
	public static ShaderWrapper cloneShaderSafe(string shaderName) {
		if (!Global.shaders.ContainsKey(shaderName)) {
			return null;
		}
		return new ShaderWrapper(shaderName);
	}

	public static ShaderWrapper cloneGenericPaletteShader(string textureName) {
		var texture = Global.textures[textureName];
		var genericPaletteShader = cloneShaderSafe("genericPalette");
		genericPaletteShader?.SetUniform("paletteTexture", texture);
		genericPaletteShader?.SetUniform("palette", 1);
		genericPaletteShader?.SetUniform("rows", texture.Size.Y);
		genericPaletteShader?.SetUniform("cols", texture.Size.X);
		return genericPaletteShader;
	}

	public static ShaderWrapper cloneNightmareZeroPaletteShader(string textureName) {
		var texture = Global.textures[textureName];
		var genericPaletteShader = cloneShaderSafe("nightmareZero");
		genericPaletteShader?.SetUniform("paletteTexture", texture);
		genericPaletteShader?.SetUniform("palette", 1);
		genericPaletteShader?.SetUniform("rows", texture.Size.Y);
		genericPaletteShader?.SetUniform("cols", texture.Size.X);
		return genericPaletteShader;
	}

	public static float toZero(float num, float inc, int dir) {
		if (dir == 1) {
			num -= inc;
			if (num < 0) num = 0;
			return num;
		} else if (dir == -1) {
			num += inc;
			if (num > 0) num = 0;
			return num;
		} else {
			throw new Exception("Must pass in -1 or 1 for dir");
		}
	}

	public static float sind(float degrees) {
		var radians = degrees * MathF.PI / 180f;
		return MathF.Sin(radians);
	}

	public static float cosd(float degrees) {
		var radians = degrees * MathF.PI / 180f;
		return MathF.Cos(radians);
	}

	public static float sinb(float degrees) {
		var radians = degrees * MathF.PI / 128f;
		return MathF.Sin(radians);
	}

	public static float cosb(float degrees) {
		var radians = degrees * MathF.PI / 128f;
		return MathF.Cos(radians);
	}

	public static float moveTo(float num, float dest, float inc, bool snap = false) {
		float diff = dest - num;
		inc *= MathF.Sign(diff);
		if (snap && MathF.Abs(diff) < MathF.Abs(inc * 2)) {
			return dest;
		}
		num += inc;
		return num;
	}

	public static float RoundEpsilon(float num) {
		var numRound = MathF.Round(num);
		var diff = MathF.Abs(numRound - num);
		if (diff < 0.0001f) {
			return numRound;
		}
		return num;
	}

	static int autoInc = 0;
	public static int getAutoIncId() {
		autoInc++;
		return autoInc;
	}

	//Expects angle and destAngle to be > 0 and < 360
	public static float lerpAngle(float angle, float destAngle, float timeScale) {
		var dir = 1;
		if (MathF.Abs(destAngle - angle) > 180) {
			dir = -1;
		}
		angle = angle + dir * (destAngle - angle) * timeScale;
		return to360(angle);
	}

	public static float lerp(float num, float dest, float timeScale) {
		return num + (dest - num) * timeScale;
	}

	public static float moveAngle(float angle, float destAngle, float timeScale, bool snap = false) {
		var dir = 1;
		if (MathF.Abs(destAngle - angle) > 180) {
			dir = -1;
		}
		angle = angle + dir * MathF.Sign(destAngle - angle) * timeScale;

		if (snap && MathF.Abs(destAngle - angle) < timeScale * 2) {
			angle = destAngle;
		}

		return to360(angle);
	}

	public static float to360(float angle) {
		if (angle < 0) angle += 360;
		if (angle > 360) angle -= 360;
		return angle;
	}

	// Given 2 angles, get the smallest difference between their values.
	// Math.Abs(angle1 - angle2) won't work in such cases as angle1 = 359 and angle2 = 0,
	// the closest angle difference should be 1
	public static float getClosestAngleDiff(float angle1, float angle2) {
		angle1 = to360(angle1);
		angle2 = to360(angle2);
		float diff = Math.Abs(angle1 - angle2);
		if (diff > 180) {
			return 360 - diff;
		}
		return diff;
	}

	public static int incrementRange(int num, int min, int max) {
		num++;
		if (num >= max) num = min;
		return num;
	}

	public static int decrementRange(int num, int min, int max) {
		num--;
		if (num < min) num = max - 1;
		return num;
	}

	public static byte[] convertToBytes(short networkId) {
		return BitConverter.GetBytes(networkId);
	}

	public static byte[] convertToBytes(ushort networkId) {
		return BitConverter.GetBytes(networkId);
	}

	public static byte toColorByte(float value) {
		return (byte)(int)(255f * clamp01(value));
	}

	public static byte toByte(float value) {
		return (byte)(int)(value);
	}

	public static byte angleToByte(float netArmAngle) {
		float newAngle = netArmAngle;
		if (newAngle < 0) newAngle += 360;
		if (newAngle > 360) newAngle -= 360;
		newAngle /= 2;
		return (byte)((int)newAngle);
	}

	public static float byteToAngle(byte angleByte) {
		return (float)angleByte * 2;
	}

	public static byte dirToByte(int dir) {
		return (byte)(dir + 128);
	}

	public static int byteToDir(byte dirByte) {
		return dirByte - 128;
	}

	public static string menuControlText(string text, bool isController = false) {
		return "(" + controlText(text, isController) + ")";
	}

	public static string controlText(string text, bool isController = false) {
		if (isController) isController = Control.isJoystick();
		// Menu keys.
		text = text.Replace("[OK]", Control.getKeyOrButtonName(Control.MenuConfirm, isController));
		text = text.Replace("[ALT]", Control.getKeyOrButtonName(Control.MenuAlt, isController));
		text = text.Replace("[BACK]", Control.getKeyOrButtonName(Control.MenuBack, isController));
		text = text.Replace("[TAB]", Control.getKeyOrButtonName(Control.Scoreboard, isController));
		text = text.Replace("[ESC]", Control.getKeyOrButtonName(Control.MenuPause, isController));
		text = text.Replace("[MLEFT]", Control.getKeyOrButtonName(Control.MenuLeft, isController));
		text = text.Replace("[MRIGHT]", Control.getKeyOrButtonName(Control.MenuRight, isController));
		text = text.Replace("[MUP]", Control.getKeyOrButtonName(Control.MenuUp, isController));
		text = text.Replace("[MDOWN]", Control.getKeyOrButtonName(Control.MenuDown, isController));
		// Normal keys.
		text = text.Replace("[JUMP]", Control.getKeyOrButtonName(Control.Jump, isController));
		text = text.Replace("[SHOOT]", Control.getKeyOrButtonName(Control.Shoot, isController));
		text = text.Replace("[SPC]", Control.getKeyOrButtonName(Control.Special1, isController));
		text = text.Replace("[DASH]", Control.getKeyOrButtonName(Control.Dash, isController));
		text = text.Replace("[WeaponL]", Control.getKeyOrButtonName(Control.WeaponLeft, isController));
		text = text.Replace("[WeaponR]", Control.getKeyOrButtonName(Control.WeaponRight, isController));
		text = text.Replace("[CMD]", Control.getKeyOrButtonName(Control.Special2, isController));

		return text;
	}

	public static string removeMapSuffix(string mapName) {
		return mapName.Replace("_md", "").Replace("_1v1", "");
	}

	public static int getGridCoordKey(ushort x, ushort y) {
		return x << 16 | y;
	}

#if WINDOWS
	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);
#endif

	public static void showMessageBox(string message, string caption) {
#if WINDOWS
		if (Global.window != null) {
			Global.window.SetMouseCursorVisible(true);
		}
		MessageBox(IntPtr.Zero, message, caption, 0);
		if (Global.window != null && Options.main != null) {
			Global.window.SetMouseCursorVisible(!Options.main.fullScreen);
		}
#else
			Console.WriteLine(caption + Environment.NewLine + message);
#endif
	}

	public static bool showMessageBoxYesNo(string message, string caption) {
#if WINDOWS
		if (Global.window != null) {
			Global.window.SetMouseCursorVisible(true);
		}
		int dialogResult = MessageBox(IntPtr.Zero, message, caption, 4);
		if (Global.window != null && Options.main != null) {
			Global.window.SetMouseCursorVisible(!Options.main.fullScreen);
		}
		if (dialogResult == 6) {
			return true;
		} else {
			return false;
		}
#else
       		Console.WriteLine(caption + Environment.NewLine + message);
    		return true;
#endif
	}

	public static void menuUpDown(ref int val, int minVal, int maxVal, bool wrap = true, bool playSound = true) {
		if (Global.input.isPressedMenu(Control.MenuUp)) {
			menuDec(ref val, minVal, maxVal, wrap, playSound);
		} else if (Global.input.isPressedMenu(Control.MenuDown)) {
			menuInc(ref val, minVal, maxVal, wrap, playSound);
		}
	}

	public static void menuDec(ref int val, int minVal, int maxVal, bool wrap = true, bool playSound = true) {
		val--;
		if (val < minVal) {
			val = wrap ? maxVal : minVal;
			if (wrap) {
				Global.playSound("menu");
			}
		} else {
			Global.playSound("menu");
		}
	}

	public static void menuInc(ref int val, int minVal, int maxVal, bool wrap = true, bool playSound = true) {
		val++;
		if (val > maxVal) {
			val = wrap ? minVal : maxVal;
			if (wrap) {
				Global.playSound("menu");
			}
		} else {
			Global.playSound("menu");
		}
	}

	public static void menuLeftRightInc(ref int val, int min, int max, bool wrap = false, bool playSound = false) {
		if (min == max) return;
		if (Global.input.isPressedMenu(Control.MenuLeft)) {
			val--;
			if (val < min) {
				val = wrap ? max : min;
				if (wrap && playSound) Global.playSound("menuX2");
			} else {
				if (playSound) Global.playSound("menuX2");
			}
		} else if (Global.input.isPressedMenu(Control.MenuRight)) {
			val++;
			if (val > max) {
				val = wrap ? min : max;
				if (wrap && playSound) Global.playSound("menuX2");
			} else {
				if (playSound) Global.playSound("menuX2");
			}
		}
	}

	public static void menuLeftRightBool(ref bool val) {
		if (Global.input.isPressedMenu(Control.MenuLeft)) {
			val = false;
		} else if (Global.input.isPressedMenu(Control.MenuRight)) {
			val = true;
		}
	}

	public static List<Weapon> sortWeapons(List<Weapon> weapons, int weaponOrdering) {
		if (weaponOrdering == 1) {
			if (weapons.Count == 3) return new List<Weapon>() { weapons[1], weapons[0], weapons[2] };
			else if (weapons.Count == 2) return new List<Weapon>() { weapons[1], weapons[0] };
		}
		return weapons;
	}

	public static string boolYesNo(bool b) {
		return b ? "Yes" : "No";
	}

	public static void debugLog(string message) {
		if (Global.debug || Global.consoleDebugLogging) {
			Console.WriteLine(message);
		}
	}

	private static ProfanityFilter.ProfanityFilter _profanityFilter;
	public static ProfanityFilter.ProfanityFilter profanityFilter {
		get {
			if (_profanityFilter == null) {
				_profanityFilter = new ProfanityFilter.ProfanityFilter(BadWords.badWords);
				_profanityFilter.AllowList.Add("azazel");
			}
			return _profanityFilter;
		}
	}
	public static string censor(string text) {
		var censored = profanityFilter.CensorString(text);
		return censored;
	}

	public static List<string> getFiles(string path, bool recursive, params string[] filters) {
		var files = new List<string>();
		if (Directory.Exists(path)) {
			files = Directory.GetFiles(
				path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
			).ToList();
		}

		return files.Where(f => {
			if (filters == null) return true;
			foreach (var filter in filters) {
				if (f.EndsWith(filter, StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
			}
			return false;
		}).Select(f => normalizePath(f)).ToList();
	}

	public static string getBaseDocumentsPath() {
		try {
			return normalizePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
		} catch {
			return null;
		}
	}

	public static string getMMXODDocumentsPath() {
		string myDocumentsPath = getBaseDocumentsPath();
		if (!string.IsNullOrEmpty(myDocumentsPath)) {
			string fullPath = myDocumentsPath + "/MMXOD/";
			if (Directory.Exists(fullPath)) {
				return fullPath;
			}
		}
		return "";
	}

	public static string normalizePath(string path) {
		return path.Replace('\\', '/').Replace("\\\\", "/");
	}

	public static bool FileExists(string filePath) {
		filePath = Global.writePath + filePath;
		if (File.Exists(filePath)) {
			return true;
		}
		return false;
	}

	public static string ReadFromFile(string filePath) {
		filePath = Global.writePath + filePath;
		string text = "";
		if (File.Exists(filePath)) {
			text = File.ReadAllText(filePath);
		}
		return text;
	}

	public static string WriteToFile(string filePath, string text) {
		filePath = Global.writePath + filePath;
		try {
			if (File.Exists(filePath)) {
				File.SetAttributes(filePath, FileAttributes.Normal);
			}
			File.WriteAllText(filePath, text);
			return null;
		} catch (Exception e) {
			return e.Message;
		}
	}

	public void CreateFileWithDirectory(string filePath) {
		if (!File.Exists(filePath)) {
			var parent = Directory.GetParent(filePath);
			Directory.CreateDirectory(parent.FullName);
			File.Create(filePath).Dispose();
		}
	}

	public static T deserialize<T>(byte[] bytes) {
		using (var stream = new MemoryStream(bytes)) {
			return Serializer.Deserialize<T>(stream);
		}
	}

	public static byte[] serialize<T>(T obj) {
		using (var stream = new MemoryStream()) {
			Serializer.Serialize(stream, obj);
			return stream.ToArray();
		}
	}

	public static T cloneProtobuf<T>(T obj) {
		return deserialize<T>(serialize(obj));
	}

	public static bool getByteValue(byte byteValue, int index) {
		List<bool> bits = Convert.ToString(byteValue, 2).Select(s => s == '0' ? false : true).ToList();
		while (bits.Count < 8) {
			bits.Insert(0, false);
		}
		return bits[index];
	}

	public static void setByteValue(ref byte byteValue, int index, bool value) {
		List<char> bits = Convert.ToString(byteValue, 2).ToList();
		while (bits.Count < 8) {
			bits.Insert(0, '0');
		}
		bits[index] = value == true ? '1' : '0';
		byteValue = Convert.ToByte(string.Join("", bits), 2);
	}

	public static byte boolArrayToByte(bool[] boolArray) {
		if (boolArray.Length > 8) {
			throw new Exception("Bool array is too big to convert to byte.");
		}
		byte byteVal = 0;
		for (int i = 0; i < boolArray.Length; i++) {
			if (boolArray[i]) {
				byteVal += (byte)(1 << 7 - i);
			}
		}
		return byteVal;
	}

	public static bool[] byteToBoolArray(byte byteValue) {
		bool[] boolArray = new bool[8];
		for (int i = 0; i < 8; i++) {
			boolArray[i] = (byteValue & (1 << 7 - i)) != 0;
		}
		return boolArray;
	}

	public static Point? getClosestHitPoint(List<CollideData> hits, Point pos, params Type[] types) {
		hits.RemoveAll(h => !isOfClass(h.gameObject, types.ToList()));

		var points = new List<Point>();
		foreach (var hit in hits) {
			if (hit.hitData?.hitPoints != null) {
				points.AddRange(hit.hitData.hitPoints);
			}
		}

		Point? bestPoint = null;
		float minDist = float.MaxValue;
		foreach (var point in points) {
			float dist = point.distanceTo(pos);
			if (dist < minDist) {
				minDist = dist;
				bestPoint = point;
			}
		}

		return bestPoint;
	}

	public static bool isOfClass(object go, Type type) {
		if (go == null) return false;
		if (go is Type) {
			return true;
		}
		return false;
	}

	public static bool isOfClass(object go, List<Type> classNames) {
		if (classNames == null || classNames.Count == 0) return true;
		var found = false;
		foreach (var className in classNames) {
			if (go.GetType() == className || go.GetType().IsSubclassOf(className)) {
				found = true;
				break;
			}
		}
		return found;
	}

	public static string getNetcodeModelString(NetcodeModel netcodeModel) {
		if (netcodeModel == NetcodeModel.FavorAttacker) return "Favor Attacker";
		if (netcodeModel == NetcodeModel.FavorDefender) return "Favor Defender";
		return "";
	}

	public static Color getPingColor(int? ping, int thresholdPing) {
		if (ping == null) return Color.White;
		if (ping >= thresholdPing) return Color.Red;
		else if (ping < thresholdPing && ping > thresholdPing * 0.5f) return Color.Yellow;
		return Color.Green;
	}

	public static byte boolToByte(bool boolean) {
		if (boolean) return 1;
		return 0;
	}

	public static bool byteToBool(byte value) {
		if (value == 1) return true;
		return false;
	}

	public static string getNthString(int place) {
		string placeStr = "";
		if (place == 1) placeStr = "1st";
		else if (place == 2) placeStr = "2nd";
		else if (place == 3) placeStr = "3rd";
		else placeStr = place.ToString() + "th";
		return placeStr;
	}

	public static SoundBufferWrapper getRandomMatchingVoice(
		Dictionary<string, SoundBufferWrapper> buffers, string soundKey, int charNum
	) {
		var voices = buffers.Values.ToList().FindAll(
			v => v.soundKey.Split('.')[0] == soundKey && (v.charNum == null || v.charNum.Value == charNum)
		);
		return voices.GetRandomItem();
	}

	public static bool parseFileDotParam(string piece, char c, out int val) {
		val = 0;
		if (piece.Length == 0) return false;
		if (piece[0] != c) return false;
		if (piece == c.ToString()) {
			val = 0;
			return true;
		}
		var rest = piece.Substring(1);
		if (int.TryParse(rest, out int result)) {
			val = result;
			return true;
		}
		return false;
	}

	public static Point getTextureArraySize(Texture[,] textures) {
		uint w = 0;
		uint h = 0;
		for (int i = 0; i < textures.GetLength(0); i++) {
			if (textures[i, 0] == null) continue;
			h += textures[i, 0].Size.Y;
		}
		for (int i = 0; i < textures.GetLength(1); i++) {
			if (textures[0, i] == null) continue;
			w += textures[0, i].Size.X;
		}

		return new Point(w, h);
	}

	public static int convertDynamicToDir(dynamic dirDynamic) {
		string dirStr = (string)dirDynamic;
		if (dirStr == "left") return -1;
		if (dirStr == "right") return 1;
		if (dirStr == "up") return -1;
		if (dirStr == "down") return 1;
		return 0;
	}

	public static int invariantStringCompare(string a, string b) {
		return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
	}

	public static float progress(float done, float total) {
		return 1 - (done / total);
	}

	public static float twave(float time, float amplitude = 1, float period = 1) {
		return 4 * amplitude / period * MathF.Abs(
			(((time - period / 4) % period) + period) % period - period / 2
		) - amplitude;
	}

	public static int SignOr1(float val) {
		int sign = MathF.Sign(val);
		if (sign == 0) sign = 1;
		return sign;
	}

	// -1 = less than, 0 = equal, 1 = greater than
	// Example order: 19.1, 19.2, ..., 19.9, 19.10, 19.11, ... 19.19, 19.20, 19.21 ... 
	public static int compareVersions(decimal versionA, decimal versionB) {
		string strA = versionA.ToString(CultureInfo.InvariantCulture);
		string strB = versionB.ToString(CultureInfo.InvariantCulture);

		int rightOfDotNumA = 0;
		var piecesA = strA.Split('.');
		if (piecesA.Length >= 2) int.TryParse(piecesA[1], out rightOfDotNumA);

		int rightOfDotNumB = 0;
		var piecesB = strB.Split('.');
		if (piecesB.Length >= 2) int.TryParse(piecesB[1], out rightOfDotNumB);

		if (rightOfDotNumA < rightOfDotNumB) return -1;
		else if (rightOfDotNumA > rightOfDotNumB) return 1;
		else return 0;
	}
}
