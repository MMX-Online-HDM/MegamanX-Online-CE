using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SFML.Graphics;

namespace MMXOnline;

public class Parallax {
	public string path;
	public float startX;
	public float startY;
	public float speedX;
	public float speedY;
	public int mirrorX;
	public float scrollSpeedX;
	public float scrollSpeedY;
	public bool isLargeCamOverride;
}

public class GameModeMirrorSupport {
	public bool nonMirrored;
	public bool mirrored;
	public GameModeMirrorSupport(bool nonMirrored, bool mirrored) {
		this.nonMirrored = nonMirrored;
		this.mirrored = mirrored;
	}
}

public class WallPathNode {
	public Point point;
	public WallPathNode next;

	public WallPathNode(Point point) {
		this.point = point;
	}

	public Line line {
		get {
			return new Line(point, next.point);
		}
	}

	public float angle {
		get {
			return point.directionTo(next.point).angle;
		}
	}

	public bool isPointTooFar(Point pos, int maxDist) {
		float minX = Math.Min(point.x, next.point.x);
		float maxX = Math.Max(point.x, next.point.x);
		float minY = Math.Min(point.y, next.point.y);
		float maxY = Math.Max(point.y, next.point.y);

		if (pos.x < minX - maxDist || pos.x > maxX + maxDist || pos.y < minY - maxDist || pos.y > maxY + maxDist) {
			return true;
		}

		return false;
	}
}

public class LevelData {
	public dynamic levelJson;
	public string name;
	public string path;
	public bool fixedCam;
	public float? killY;
	public int maxPlayers = Server.maxPlayerCap;
	public double playToMultiplier;
	public int width;
	public int height;
	public List<string> supportedGameModes;
	public Dictionary<string, GameModeMirrorSupport> supportedGameModesToMirrorSupport = new Dictionary<string, GameModeMirrorSupport>();
	public bool supportsLargeCam;
	public bool defaultLargeCam;
	public Color bgColor;
	public string shortName;
	public string displayName;
	public List<string> mapSpritePaths = new List<string>();
	public string backgroundPath;
	public string backwallPath;
	public string foregroundPath;
	private List<Parallax> rawParallaxes = new List<Parallax>();
	public string thumbnailPath;
	public int mirrorX;
	public bool isMirrored;
	public bool supportsMirrored;
	public bool mirroredOnly;
	public bool mirrorMapImages;
	public bool isCustomMap;
	public string checksum;
	public string customMapUrl;
	public List<WallPathNode> wallPathNodes = new List<WallPathNode>();
	public List<WallPathNode> wallPathNodesInverted = new List<WallPathNode>();
	public string parallaxShader;
	public string parallaxShaderImage;
	public string backgroundShader;
	public string backgroundShaderImage;
	public string backwallShader;
	public string backwallShaderImage;
	public string foregroundShader;
	public string foregroundShaderImage;
	public bool supportsVehicles;
	public bool raceOnly;
	public int customSize = -1;
	public bool twoDisplayNames;
	public string displayName2;

	public LevelData() {
	}

	public LevelData(string levelJsonStr, string levelIniStr, bool isCustomMap) {
		levelJson = JsonConvert.DeserializeObject<dynamic>(levelJsonStr);
		if (levelIniStr != "") {
			dynamic levelIni = IniParser.ParseText(levelIniStr);
			if (levelIni["MapData"]["size"] is String customSizeStr) {
				customSize = customSizeStr.ToLowerInvariant() switch {
					"training" => 0,
					"1v1" => 1,
					"small" => 2,
					"medium" => 3,
					"large" => 4,
					"xl" or "collosal" => 5,
					_ => -1
				};
			}
		}
		name = levelJson.name;
		path = levelJson.path;
		width = levelJson.width;
		height = levelJson.height;
		maxPlayers = levelJson.maxPlayers ?? Server.maxPlayerCap;
		killY = levelJson.killY;
		mirrorMapImages = levelJson.mirrorMapImages ?? true;
		this.isCustomMap = isCustomMap;

		mirrorX = levelJson.mirrorX ?? 0;
		isMirrored = name.EndsWith("_mirrored");
		supportsMirrored = mirrorX > 0;
		mirroredOnly = levelJson.mirroredOnly ?? false;

		supportsVehicles = levelJson.supportsVehicles ?? true;
		supportsLargeCam = levelJson.supportsLargeCam ?? false;
		defaultLargeCam = levelJson.defaultLargeCam ?? false;
		shortName = levelJson.shortName ?? name;
		displayName = levelJson.displayName ?? name;
		string bgColorHex = levelJson.bgColorHex ?? "";
		if (!string.IsNullOrEmpty(bgColorHex)) {
			bgColorHex = bgColorHex + "FF";
			uint argb = UInt32.Parse(bgColorHex.Replace("#", ""), NumberStyles.HexNumber);
			bgColor = new Color(argb);
		} else {
			bgColor = Color.Black;
		}

		if (levelJson.mapSpritePaths != null) {
			foreach (string path in levelJson.mapSpritePaths) {
				mapSpritePaths.Add(path);
			}
		}

		string shaderPrefix = "";
		if (isCustomMap) shaderPrefix = name + ":";

		parallaxShader = Helpers.addPrefix(levelJson.parallaxShader, shaderPrefix);
		parallaxShaderImage = Helpers.addPrefix(levelJson.parallaxShaderImage, shaderPrefix);
		backgroundShader = Helpers.addPrefix(levelJson.backgroundShader, shaderPrefix);
		backgroundShaderImage = Helpers.addPrefix(levelJson.backgroundShaderImage, shaderPrefix);

		backgroundPath = levelJson.backgroundPath ?? "";
		backwallPath = levelJson.backwallPath ?? "";
		foregroundPath = levelJson.foregroundPath ?? "";

		if (levelJson.parallaxes != null) {
			foreach (var parallaxJson in levelJson.parallaxes) {
				var parallax = new Parallax() {
					path = parallaxJson.path ?? "",
					startX = parallaxJson.startX ?? 0,
					startY = parallaxJson.startY ?? 0,
					speedX = parallaxJson.speedX ?? 0.5f,
					speedY = parallaxJson.speedY ?? 0.5f,
					mirrorX = parallaxJson.mirrorX ?? 0,
					scrollSpeedX = parallaxJson.scrollSpeedX ?? 0,
					scrollSpeedY = parallaxJson.scrollSpeedY ?? 0,
					isLargeCamOverride = parallaxJson.isLargeCamOverride ?? false
				};

				rawParallaxes.Add(parallax);
			}
		}

		wallPathNodes.Clear();
		wallPathNodesInverted.Clear();
		if (levelJson.mergedWalls != null) {
			// Normal
			foreach (var mergedWall in levelJson.mergedWalls) {
				var currentShapeNodes = new List<WallPathNode>();
				foreach (var point in mergedWall) {
					float x = Convert.ToSingle(point[0]);
					float y = Convert.ToSingle(point[1]);
					currentShapeNodes.Add(new WallPathNode(new Point(x, y)));
				}
				for (int i = 0; i < currentShapeNodes.Count; i++) {
					var current = currentShapeNodes[i];
					var next = i + 1 < currentShapeNodes.Count ? currentShapeNodes[i + 1] : currentShapeNodes[0];
					current.next = next;
					wallPathNodes.Add(current);
				}
			}

			// Inverted
			foreach (var mergedWall in levelJson.mergedWalls) {
				var currentShapeNodes = new List<WallPathNode>();
				foreach (var point in mergedWall) {
					float x = Convert.ToSingle(point[0]);
					float y = Convert.ToSingle(point[1]);
					currentShapeNodes.Add(new WallPathNode(new Point(x, y)));
				}
				for (int i = currentShapeNodes.Count - 1; i >= 0; i--) {
					var current = currentShapeNodes[i];
					var next = i - 1 >= 0 ? currentShapeNodes[i - 1] : currentShapeNodes[currentShapeNodes.Count - 1];
					current.next = next;
					wallPathNodesInverted.Add(current);
				}
			}
		}

		var supportedGameModesSet = new HashSet<string>();

		if (is1v1()) {
			maxPlayers = 4;
			supportedGameModesSet.Add(GameMode.Elimination);
			supportedGameModesSet.Add(GameMode.TeamElimination);
		} else {
			maxPlayers = Server.maxPlayerCap;
			supportedGameModesSet.Add(GameMode.Deathmatch);
			supportedGameModesSet.Add(GameMode.TeamDeathmatch);
		}
		if (levelJson.supportsCTF == true) {
			supportedGameModesSet.Add(GameMode.CTF);
		}
		if (levelJson.supportsKOTH == true) {
			supportedGameModesSet.Add(GameMode.KingOfTheHill);
		}
		if (levelJson.supportsCP == true) {
			supportedGameModesSet.Add(GameMode.ControlPoint);
		}
		if (levelJson.supportsRace == true) {
			supportedGameModesSet.Add(GameMode.Race);
		}

		if (!is1v1()) {
			supportedGameModesSet.Add(GameMode.Elimination);
			supportedGameModesSet.Add(GameMode.TeamElimination);
		}

		if (levelJson.raceOnly == true) {
			raceOnly = true;
			supportedGameModesSet.Clear();
			supportedGameModesSet.Add(GameMode.Race);
		}

		supportedGameModes = supportedGameModesSet.ToList();
		supportedGameModes.Sort(gameModeSortFunc);

		foreach (var gameMode in supportedGameModes) {
			supportedGameModesToMirrorSupport[gameMode] = new GameModeMirrorSupport(true, false);
		}

		if (isCustomMap) {
			string tryPath = Global.assetPath + "assets/" + getFolderPath() + "/thumbnail.png";
			if (File.Exists(tryPath)) {
				thumbnailPath = tryPath;
			} else {
				thumbnailPath = getThumbnailPath("placeholder");
			}
		} else {
			string thumbnailName = this.name;
			if (!File.Exists(getThumbnailPath(thumbnailName))) {
				thumbnailName = name.Replace("_md", "").Replace("_1v1", "");
			}
			if (!File.Exists(getThumbnailPath(thumbnailName))) {
				thumbnailName = name.Replace("_md", "").Replace("_1v1", "").TrimEnd('1').TrimEnd('2').TrimEnd('3').TrimEnd('4');
			}
			if (!File.Exists(getThumbnailPath(thumbnailName))) {
				thumbnailName = "placeholder";
			}
			thumbnailPath = getThumbnailPath(thumbnailName);
		}

		if (isCustomMap) {
			string rawMapSpriteChecksumString = loadCustomMapSprites();
			string rawChecksumString = levelJsonStr + "|" + rawMapSpriteChecksumString;
			using (MD5 md5 = MD5.Create()) {
				checksum = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(rawChecksumString))).Replace("-", String.Empty);
			}
			customMapUrl = levelJson.customMapUrl ?? null;
		}
		correctMapNames();
		validate();
		mapShadersStuff();
	}

	public string loadCustomMapSprites() {
		var customSpriteJsonPaths = Helpers.getFiles(Global.assetPath + "assets/maps_custom/" + name + "/sprites", true, "json");
		var fileChecksumDict = new SortedDictionary<string, string>();
		foreach (var customSpriteJsonPath in customSpriteJsonPaths) {
			string fileName = Path.GetFileNameWithoutExtension(customSpriteJsonPath);
			string spriteName = name + ":" + fileName;
			string json = File.ReadAllText(customSpriteJsonPath);
			fileChecksumDict[spriteName] = json;

			AnimData sprite = new AnimData(json, spriteName, name);
			Global.sprites[spriteName] = sprite;
		}

		string spritesChecksum = "";
		foreach (var kvp in fileChecksumDict) {
			spritesChecksum += kvp.Key + " " + kvp.Value;
		}

		return spritesChecksum;
	}

	public void validate() {
		if (isMirrored || name.EndsWith("inverted")) return;
		if (width > 35000 || height > 35000) {
			throw new Exception("Map too big.");
		}
		if (customMapUrl?.Length > 128) {
			throw new Exception("Map URL too long.");
		}
		if (name?.Length > 40) {
			throw new Exception("Map name too long.");
		}
		if (shortName?.Length > 14) {
			throw new Exception("Short name too long.");
		}
		if (displayName?.Length > 48) {
			throw new Exception("Display name too long.");
		}
		if (maxPlayers > Server.maxPlayerCap) {
			throw new Exception("Max Players too high.");
		}
	}

	public void populateMirrorMetadata() {
		if (isMirrored) return;
		LevelData? mirroredVersion = Global.levelDatas.GetValueOrDefault(name + "_mirrored");
		if (mirroredVersion == null) return;

		foreach (var otherGameMode in mirroredVersion.supportedGameModes) {
			if (!supportedGameModes.Contains(otherGameMode)) {
				supportedGameModes.Add(otherGameMode);
			}
			if (!supportedGameModesToMirrorSupport.ContainsKey(otherGameMode)) {
				supportedGameModesToMirrorSupport[otherGameMode] = new GameModeMirrorSupport(false, true);
			} else {
				supportedGameModesToMirrorSupport[otherGameMode].mirrored = true;
			}
		}

		supportedGameModes.Sort(gameModeSortFunc);
	}

	public static string getChecksumFromName(string level) {
		if (!Global.levelDatas.ContainsKey(level)) return null;
		return Global.levelDatas[level].checksum;
	}

	public static string getCustomMapUrlFromName(string level) {
		if (!Global.levelDatas.ContainsKey(level)) return null;
		return Global.levelDatas[level].customMapUrl;
	}

	public List<string> gameModeSortOrder = new List<string> { GameMode.Deathmatch, GameMode.TeamDeathmatch, GameMode.CTF, GameMode.KingOfTheHill, GameMode.ControlPoint, GameMode.Elimination, GameMode.TeamElimination, GameMode.Race };
	public int gameModeSortFunc(string a, string b) {
		int aIndex = gameModeSortOrder.IndexOf(a);
		int bIndex = gameModeSortOrder.IndexOf(b);

		if (aIndex < bIndex) return -1;
		else if (aIndex > bIndex) return 1;
		else return 0;
	}


	public List<Parallax> getParallaxes() {
		return rawParallaxes.Where(p => !p.isLargeCamOverride).ToList();
	}

	public List<Parallax> getLargeCamParallaxes() {
		var retParallaxes = new List<Parallax>();
		for (int i = 0; i < rawParallaxes.Count; i++) {
			if (i < rawParallaxes.Count - 1 && rawParallaxes[i + 1].isLargeCamOverride) {
				retParallaxes.Add(rawParallaxes[i + 1]);
				i++;
			} else {
				retParallaxes.Add(rawParallaxes[i]);
			}
		}
		return retParallaxes;
	}

	public Texture[,] getBackgroundTextures() {
		return Global.mapTextures.GetValueOrDefault(backgroundPath);
	}

	public Texture[,] getBackwallTextures() {
		return Global.mapTextures.GetValueOrDefault(backwallPath);
	}

	public Texture[,] getForegroundTextures() {
		return Global.mapTextures.GetValueOrDefault(foregroundPath);
	}

	public Texture[,] getParallaxTextures(string path) {
		return Global.mapTextures.GetValueOrDefault(path);
	}

	public void loadLevelImages() {
		loadImage(backgroundPath, mirrorX);
		loadImage(backwallPath, mirrorX);
		loadImage(foregroundPath, mirrorX);
		foreach (var parallax in rawParallaxes) {
			loadImage(parallax.path, parallax.mirrorX);
		}
	}

	public void unloadLevelImages() {
		unloadImage(backgroundPath);
		unloadImage(backwallPath);
		unloadImage(foregroundPath);
		foreach (var parallax in rawParallaxes) {
			unloadImage(parallax.path);
		}
	}

	private void loadImage(string relativeImagePath, int mirrorX) {
		if (string.IsNullOrEmpty(relativeImagePath)) return;

		string fullImagePath = Global.assetPath + "assets/" + relativeImagePath;
		if (!File.Exists(fullImagePath)) return;

		Texture[,] tempTextureMDA = new Texture[50, 50];
		int maxJ = 0;
		int maxI = 0;
		const int size = 1024;

		var image = new Image(fullImagePath);

		if (isMirrored && mirrorX != 0 && mirrorMapImages) {
			var image2 = new Image((uint)(1 + (mirrorX * 2)), image.Size.Y);
			image2.Copy(image, 0, 0, new IntRect(0, 0, mirrorX, height));
			image.FlipHorizontally();
			var a = (int)image.Size.X - mirrorX;
			uint b = 0;
			if (a < 0) {
				b = (uint)Math.Abs(a);
				a = 0;
			}
			image2.Copy(image, (uint)mirrorX + b, 0, new IntRect(a, 0, mirrorX, height));

			image.Dispose();
			image = image2;
		}

		for (int i = 0; i <= image.Size.Y / size; i++) {
			for (int j = 0; j <= image.Size.X / size; j++) {
				int height = Math.Min(size, (int)image.Size.Y - (i * size));
				int width = Math.Min(size, (int)image.Size.X - (j * size));

				if (width == 0 || height == 0) continue;

				var tile = new Image((uint)width, (uint)height);
				tile.Copy(image, 0, 0, new IntRect(j * size, i * size, width, height));

				Texture texture = new Texture(tile);
				tempTextureMDA[i, j] = texture;
				maxI = Math.Max(i + 1, maxI);
				maxJ = Math.Max(j + 1, maxJ);
			}
		}

		image.Dispose();

		if (maxI > 0 && maxJ > 0) {
			Texture[,] textureMDA = new Texture[maxI, maxJ];
			for (int i = 0; i < maxI; i++) {
				for (int j = 0; j < maxJ; j++) {
					textureMDA[i, j] = tempTextureMDA[i, j];
				}
			}
			Global.mapTextures[relativeImagePath] = tempTextureMDA;
		}
	}

	private void unloadImage(string name) {
		if (string.IsNullOrEmpty(name)) return;

		if (Global.mapTextures.ContainsKey(name)) {
			Global.mapTextures.Remove(name);
		}
	}

	public string getFolderPath() {
		var pieces = path.Split('/').ToList();
		pieces.Pop();
		return string.Join('/', pieces);
	}

	public bool isTraining() {
		if (customSize != -1) {
			return customSize == 0;
		}
		return name == "training" || name == "training2" || name.EndsWith("_training");
	}

	public bool is1v1() {
		if (customSize != -1) {
			return customSize == 1;
		}
		return name.EndsWith("_1v1");
	}

	public bool isSmall() {
		if (customSize != -1) {
			return customSize == 2;
		}
		if (name is "nodetest" or "airport_1v1" or "sigma1_1v1") {
			return true;
		}
		return name.EndsWith("_small");
	}

	public bool isMedium() {
		if (customSize != -1) {
			return customSize == 3;
		}
		return name.EndsWith("_md");
	}

	public bool isCollosal() {
		if (customSize != -1) {
			return customSize == 5;
		}
		if (name is "giantdam" or "gallery") {
			return true;
		}
		return name.EndsWith("_collosal") || name.EndsWith("_xl");
	}

	// TODO: Add this info to the level format themsleves
	public static Dictionary<string, string> stageSongs = new Dictionary<string, string>() {
		// X1 stuff.
		{ "airport", "stormEagle" },
		{ "bossroom", "boss_X1" },
		{ "factory", "flameMammoth" },
		{ "gallery", "armoredArmadillo" },
		{ "forest", "stingChameleon" },
		{ "forest2", "stingChameleon" },
		{ "highway", "centralHighway" },
		{ "highway2", "castRoll_X1" },
		{ "mountain", "chillPenguin" },
		{ "ocean", "launchOctopus" },
		{ "powerplant", "sparkMandrill" },
		{ "powerplant2", "sparkMandrill" },
		{ "sigma1", "sigmaFortress" },
		{ "sigma2", "sigmaFortress2" },
		{ "sigma3", "sigmaFortress3" },
		{ "tower", "boomerangKuwanger" },
		// X2 stuff.
		{ "centralcomputer", "magnetCentipede" },
		{ "crystalmine", "crystalSnail" },
		{ "deepseabase", "bubbleCrab" },
		{ "desertbase", "overdriveOstrich" },
		{ "desertbase2", "credits_X2" },
		{ "dinosaurtank", "wheelGator" },
		{ "maverickfactory", "maverickFactory" },
		{ "robotjunkyard", "morphMoth" },
		{ "volcaniczone", "flameStag" },
		{ "weathercontrol", "wireSponge" },
		{ "xhunter1", "counterHunter1" },
		{ "xhunter2", "counterHunter2" },
		// X3 stuff.
		{ "aircraftcarrier", "gravityBeetle" },
		{ "dopplerlab", "dopplerLab" },
		{ "protoDopplerB", "psx_dopplerLab" },
		{ "protoDopplerC", "psx_dopplerLab2" },
		{ "protoDopplerD", "psx_dopplerLab2" },
		{ "frozentown", "blizzardBuffalo" },
		{ "giantdam", "toxicSeahorse" },
		{ "giantdam2", "toxicSeahorse" },
		{ "hunterbase", "hunterBase" },
		{ "hunterbase2", "credits_X3" },
		{ "powercenter", "voltCatfish" },
		{ "quarry", "tunnelRhino" },
		{ "safaripark", "neonTiger" },
		{ "shipyard", "crushCrawfish" },
		{ "weaponsfactory", "blastHornet" },

		// Alt music.
		{ "dopplerlab_1v1", "fortressBoss_X3" },
		{ "zerovirus_1v1", "XvsZeroV2_megasfc" },
		{ "centralcomputer_1v1", "boss_X2" },
		{ "sigma4_1v1", "boss_X1" },

		// Others.
		{ "japetribute", "variableX" },
		{ "nodetest", "credits_X1" },
		{ "training", "training_vodaz" },
		{ "training2", "training_vodaz" },
	};

	public string getMusicKey(List<Player> players) {
		if (isCustomMap) {
			return name;
		}
		if (stageSongs.ContainsKey(name)) {
			return stageSongs[name];
		}
		if (stageSongs.ContainsKey(Helpers.removeMapSuffix(name))) {
			return stageSongs[Helpers.removeMapSuffix(name)];
		}
		return Helpers.removeMapSuffix(name);
	}

	public string getWinTheme() {
		if (name.Contains("xhunter1") ||
			name.Contains("deepseabase") ||
			name.Contains("maverickfactory") ||
			name.Contains("robotjunkyard") ||
			name.Contains("volcaniczone") ||
			name.Contains("dinosaurtank") ||
			name.Contains("centralcomputer") ||
			name.Contains("crystalmine") ||
			name.Contains("desertbase") ||
			name.Contains("weathercontrol")
		) {
			return "stageClear_X2";
		}
		if (name.Contains("hunterbase") ||
			name.Contains("giantdam") ||
			name.Contains("weaponsfactory") ||
			name.Contains("frozentown") ||
			name.Contains("aircraftcarrier") ||
			name.Contains("powercenter") ||
			name.Contains("shipyard") ||
			name.Contains("quarry") ||
			name.Contains("safaripark") ||
			name.Contains("dopplerlab")
		) {
			return "stageClear_X3";
		}
		if (isCustomMap) {
			return Helpers.randomRange(0, 2) switch {
				1 => "stageClear_X2",
				2 => "stageClear_X3",
				_ => "stageClear_X1"
			};
		}

		return "stageClear_X1";
	}

	public Texture getMapThumbnail() {
		if (!Global.textures.ContainsKey(thumbnailPath)) {
			Global.textures[thumbnailPath] = new Texture(thumbnailPath);
		}

		return Global.textures[thumbnailPath];
	}

	public string getThumbnailPath(string name) {
		return Global.assetPath + "assets/maps_shared/thumbnails/" + name + ".png";
	}

	public bool canChangeMirror(string gameMode) {
		if (supportedGameModesToMirrorSupport[gameMode].mirrored != supportedGameModesToMirrorSupport[gameMode].nonMirrored) {
			return false;
		}
		return supportsMirrored && !mirroredOnly;
	}
	public string getLooseTheme() {
		if (name.Contains("xhunter1") ||
			name.Contains("deepseabase") ||
			name.Contains("maverickfactory") ||
			name.Contains("robotjunkyard") ||
			name.Contains("volcaniczone") ||
			name.Contains("dinosaurtank") ||
			name.Contains("centralcomputer") ||
			name.Contains("crystalmine") ||
			name.Contains("desertbase") ||
			name.Contains("weathercontrol")
		) {
			return "password_X2";
		}
		if (name.Contains("hunterbase") ||
			name.Contains("giantdam") ||
			name.Contains("weaponsfactory") ||
			name.Contains("frozentown") ||
			name.Contains("aircraftcarrier") ||
			name.Contains("powercenter") ||
			name.Contains("shipyard") ||
			name.Contains("quarry") ||
			name.Contains("safaripark") ||
			name.Contains("dopplerlab")
		) {
			return "password_X3";
		}
		if (isCustomMap) {
			return Helpers.randomRange(0, 2) switch {
				1 => "password_X2",
				2 => "password_X3",
				_ => "password_X1"
			};
		}

		return "password_X1";
	}
	public void correctMapNames() {
		var nameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
			#region Large-Collosal
			{ "doppler's lab", "Doppler A" },
			{ "safari park", "Safari Park" },
			{ "quarry", "Quarry" },
			{ "power control center", "Power Control Center" },
			{ "shipyard", "Shipyard" },
			{ "airborne aircraft carrier", "Airborne Aircraft Carrier" },
			{ "weapons factory", "Weapons Factory" },
			{ "giant dam", "Giant Dam" },
			{ "giant dam 2", "Giant Dam 2" },
			{ "frozen town", "Frozen Town" },
			{ "hunter base", "Hunter Base" },
			{ "hunterbase 2", "Credits Scenario X3" },
			{ "dinosaur tank", "Dinosaur Type Terrestrial"},
			{ "x-hunter stage 2", "Counter Hunter 2"},
			{ "x-hunter stage 1", "Counter Hunter 1"},
			{ "central computer", "Central Computer"},
			{ "energen crystal", "Energen Crystal"},
			{ "desert base", "Desert Base"},
			{ "desert base 2", "Credits Scenario X2"},
			{ "weather control", "Weather Control Center"},
			{ "robot junkyard", "Robot Scrap"},
			{ "volcanic zone", "Volcanic Zone"},
			{ "deep-sea base", "Deep-Sea Base"},
			{ "maverick factory", "Maverick Factory"},
			{ "highway", "Highway"},
			{ "highway 2", "Credits Scenario X1"},
			{ "powerplant", "Power Plant"},
			{ "powerplant2", "Power Plant 2"},
			{ "factory", "Factory"},
			{ "Missile Base", "Snow Mountain"},
			{ "ocean", "Ocean"},
			{ "tower", "Tower"},
			{ "forest", "Forest"},
			{ "forest 2", "Forest 2"},
			{ "airport", "Sky"},
			{ "gallery", "Gallery"},
			{ "sigma stage 1", "Sigma 1"},
			{ "sigma stage 2", "Sigma 2"},
			{ "sigma stage 3", "Sigma 3"},
			#endregion
			#region Medium
			{ "sigma stage 1 md", "Sigma 1 MD"},
			{ "sigma stage 2 md", "Sigma 2 MD"},
			{ "forest md", "Forest MD"},
			{ "ocean md", "Ocean MD"},
			{ "Missile Base MD", "Snow Mountain MD"},
			{ "highway md", "Highway MD"},
			{ "weather control md", "Weather Control Center MD"},
			{ "desert base md", "Desert Base MD"},
			{ "maverick factory md", "Maverick Factory MD"},
			{ "factory md", "Factory MD"},
			{ "airport md", "Sky MD"},
			#endregion
			#region small
			{ "sigma1_1v1", "Sigma 1 VS. 1 "},
			{ "airport 1v1", "Sky 1 VS. 1"},
			{ "doppler lab 1v1", "Doppler B 1 VS. 1" },
			{ "sigma stage 4 1v1", "Sigma 4 1 VS. 1"},
			{ "factory 1v1", "Factory 1 VS. 1"},
			{ "hunterbase 1v1", "Hunter Base 1 VS. 1" },
			{ "forest 1v1", "Forest 1 VS. 1"},
			{ "highway 1v1", "Highway 1 VS. 1"},
			{ "zero virus 1v1", "Zero Space 3: "},
			{ "central computer 1v1", "Central Computer 1 VS. 1"},
			{ "jape tribute 1v1", "Jape Tribute 1 VS. 1"},
			{ "ocean 1v1", "Ocean 1 VS. 1"},
			{ "Missile Base 1v1", "Snow Mountain 1 VS. 1"},
			{ "tower 1v1", "Tower 1 VS. 1"},
			{ "powerplant 1v1", "Power Plant 1 VS. 1"},
			#endregion
		};
		if (nameMappings.TryGetValue(displayName, out var newName))
			displayName = newName;
		if (displayName == "Dinosaur Type Terrestrial") {
			twoDisplayNames = true;
			displayName2 = "Aircraft Carrier";
		}
		if (displayName == "Zero Space 3: ") {
			twoDisplayNames = true;
			displayName2 = "Awakening 1 VS. 1";
		}
	}
	public void mapShadersStuff() {
		switch (displayName) {
			case "Counter Hunter 1":
				backwallShader = "xhunterBGBW";
				backwallShaderImage = "paletteXhunter1backwall";
				break;
			case "Energen Crystal":
				backwallShader = "energenCrystalBW";
				backwallShaderImage = "paletteEnergenCrystalBackWall";
				break;
			case "Factory":
				backwallShader = "FactoryBW";
				backwallShaderImage = "paletteFactoryBackWall";
				break;
			case "Maverick Factory":
				backwallShader = "MFactoryBW";
				backwallShaderImage = "paletteMFactoryBW";
				foregroundShader = "MFactoryBG";
				foregroundShaderImage = "paletteMFactoryFG";
				break;
			case "Hunter Base":
				backwallShader = "hunterBaseBg";
				backwallShaderImage = "paletteHunterBaseBackwall";
				break;
			case "Weapons Factory":
				backwallShader = "weaponsFactory";
				backwallShaderImage = "paletteWeaponsFactory";
				foregroundShader = "weaponsFactory";
				foregroundShaderImage = "paletteWeaponsFactory";
				break;
			case "Frozen Town":
				backwallShader = "frozenTown";
				backwallShaderImage = "paletteFrozenTown";
				foregroundShader = "frozenTown";
				foregroundShaderImage = "paletteFrozenTown";
				break;
			case "Airborne Aircraft Carrier":
				backwallShader = "aircraftCarrierBG";
				backwallShaderImage = "paletteaircraftcarrierBW";
				break;
			case "Power Control Center":
				backwallShader = "powerCenterBW";
				backwallShaderImage = "palettepowercenterBW";
				break;
			case "Quarry":
				backwallShader = "quarry";
				backwallShaderImage = "paletteQuarry";
				break;
			case "Safari Park":
				backwallShader = "safariParkBW";
				backwallShaderImage = "paletteSafariParkBW";
				foregroundShader = "safariParkBG";
				foregroundShaderImage = "paletteSafariParkBG";
				break;
			case "Doppler A":
				backwallShader = "dopplerA";
				backwallShaderImage = "paletteDopplerABW";
				break;
			case "Volcanic Zone":
				backwallShader = "volcanicZoneBW";
				backwallShaderImage = "palettevolcanicZoneBW";
				break;
			case "Prototype Doppler B":
				backwallShader = "PrototypeDopplerB";
				backwallShaderImage = "palettePDopplerBBW";
				break;
			case "Prototype Doppler C":
				backwallShader = "PrototypeDopplerC";
				backwallShaderImage = "palettePDopplerCBW";
				break;
		}
	}
	
}
