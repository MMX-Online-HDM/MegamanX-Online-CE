using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SFML.Audio;
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
	public bool supportsVehicles;
	public bool raceOnly;

	public LevelData() {
	}

	public LevelData(string levelJsonStr, bool isCustomMap) {
		levelJson = JsonConvert.DeserializeObject<dynamic>(levelJsonStr);
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
		string bgColorHex = levelJson.bgColorHex ?? null;
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
		} else if (isMedium()) {
			//maxPlayers = 4;
			maxPlayers = 12;
			supportedGameModesSet.Add(GameMode.Deathmatch);
			supportedGameModesSet.Add(GameMode.TeamDeathmatch);
		} else {
			maxPlayers = 22;
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

		validate();
	}

	public string loadCustomMapSprites() {
		var customSpriteJsonPaths = Helpers.getFiles(Global.assetPath + "assets/maps_custom/" + name + "/sprites", true, "json");
		var fileChecksumDict = new SortedDictionary<string, string>();
		foreach (var customSpriteJsonPath in customSpriteJsonPaths) {
			string fileName = Path.GetFileNameWithoutExtension(customSpriteJsonPath);
			string spriteName = name + ":" + fileName;
			string json = File.ReadAllText(customSpriteJsonPath);
			fileChecksumDict[spriteName] = json;

			Sprite sprite = new Sprite(json, spriteName, name);
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
		if (displayName?.Length > 25) {
			throw new Exception("Display name too long.");
		}
		if (maxPlayers > Server.maxPlayerCap) {
			throw new Exception("Max Players too high.");
		}
	}

	public void populateMirrorMetadata() {
		if (isMirrored) return;
		LevelData mirroredVersion = Global.levelDatas.GetValueOrDefault(name + "_mirrored");
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

	public bool is1v1() {
		return name.EndsWith("1v1");
	}

	public bool isTraining() {
		return name == "training";
	}

	public bool isMedium() {
		return name.EndsWith("_md");
	}

	public string getMusicKey(List<Player> players) {
		if (name == "japetribute_1v1") {
			return "mmx1_demo";
		}

		if (isCustomMap) {
			return name;
		}
		if (name == "training"){
			return "training_vodaz";
		}

		//MMX3
		if (name == "hunterbase2") return "mmx3_credits";
		if (name == "dopplerlab") return "mmx3_dopplerstage";
		if (name == "safaripark") return "mmx3_neontiger";
		if (name == "quarry") return "mmx3_tunnelrhino";
		if (name == "shipyard") return "mmx3_crushcrawfish";
		if (name == "powercenter") return "mmx3_voltcatfish";
		if (name == "aircraftcarrier") return "mmx3_gravitybeetle";
		if (name == "weaponsfactory") return "mmx3_blasthornet";
		if (name == "giantdam") return "mmx3_toxicseahorse";
		if (name == "giantdam2") return "mmx3_toxicseahorse";
		if (name == "hunterbase") return "mmx3_introstage";
		if (name == "frozentown") return "mmx3_blizzardbuffalo";
		//MMX2
		if (name == "xhunter2") return "mmx2_deeperintotheabyss";
		if (name == "xhunter1") return "mmx2_absolutezero";
		if (name == "centralcomputer") return "mmx2_redalert";
		if (name == "crystalmine") return "mmx2_sinistergleam";
		if (name == "desertbase2") return "mmx2_hailtheconqueringhero";
		if (name == "desertbase") return "mmx2_dustdevil";
		if (name == "robotjunkyard") return "mmx2_wasteland";
		if (name == "volcaniczone") return "mmx2_volcanosfury";
		if (name == "deepseabase") return "mmx2_dangerousreef";
		if (name == "dinosaurtank") return "mmx2_panzerdesdrachens";
		if (name == "weathercontrol") return "mmx2_weatherwizard";
		if (name == "maverickfactory") return "mmx2_themaverickslaststand";
		//MMX1
		if (name == "sigma3") return "mmx1_sigmafortress3";
		if (name == "sigma2") return "mmx1_sigmafortress2";
		if (name == "sigma1") return "mmx1_sigmafortress1";
		if (name == "airport") return "mmx1_stormeagle";
		if (name == "forest3") return "mmx1_stingchameleon";
		if (name == "forest2") return "mmx1_stingchameleon";
		if (name == "forest") return "mmx1_stingchameleon";
		if (name == "highway2") return "mmx1_castings";
		if (name == "highway") return "mmx1_openingstage";
		if (name == "ocean") return "mmx1_launchoctopus";
		if (name == "mountain") return "mmx1_chillpenguin";
		if (name == "tower") return "mmx1_boomerkuwanger";
		if (name == "powerplant") return "mmx1_sparkmandrill";
		if (name == "powerplant2") return "mmx1_sparkmandrill";
		if (name == "factory") return "mmx1_flamemammoth";
		if (name == "gallery") return "mmx1_armoredarmadillo";
		//Small Maps
		if (name == "factory_md") return "mmx1_flamemammoth";
		if (name == "airport_md") return "mmx1_stormeagle";
		if (name == "maverickfactory_md") return "mmx2_themaverickslaststand";
		if (name == "desertbase_md") return "mmx2_dustdevil";
		if (name == "weathercontrol_md") return "mmx2_weatherwizard";
		if (name == "highway_md") return "mmx1_openingstage";
		if (name == "mountain_md") return "mmx1_chillpenguin";
		if (name == "ocean_md") return "mmx1_launchoctopus";
		if (name == "forest_md") return "mmx1_stingchameleon";
		if (name == "sigma1_md") return "mmx1_sigmafortress1";
		if (name == "sigma2_md") return "mmx1_sigmafortress2";
		//1V1 Maps
		if (name == "centralcomputer_1v1") return "mmx2_redalert";
		if (name == "sigma4_1v1") return "mmx1_sigmabattle";
		if (name == "dopplerlab_1v1") return "mmx3_dopplerbattle";
		if (name == "sigma1_1v1") return "mmx1_sigmafortress1";
		if (name == "factory_1v1") return "mmx1_flamemammoth";
		if (name == "hunterbase_1v1") return "mmx3_introstage";
		if (name == "airport_1v1") return "mmx1_stormeagle";
		if (name == "forest_1v1") return "mmx1_stingchameleon";
		if (name == "highway_1v1") return "mmx1_introstage";
		if (name == "hunterbase_1v1") return "mmx3_introstage";
		if (name == "mountain_1v1") return "mmx1_chillpenguin";
		if (name == "ocean_1v1") return "mmx1_launchoctopus";
		if (name == "tower_1v1") return "mmx1_boomerkuwanger";

		if (name == "zerovirus_1v1") {
			return "mmx5_xvszerov2_megasfc";
		} else if (players.Count == 2 && is1v1() && ((players[0].isZero && players[1].isX) || (players[0].isX && players[1].isZero))) {
			return "mmx5_xvszerov2_megasfc";
		} else if (players.Count == 2 && is1v1() && (players[0].is1v1MaverickFakeZero() || players[1].is1v1MaverickFakeZero())) {
			return "mmx2_zerosrebirth";
		} else if 
		(players.Count == 2 && is1v1() && (players[0].isNon1v1MaverickSigma() || players[1].isNon1v1MaverickSigma())) 
		{
			if (players[0].isSigma1AndSigma() || players[1].isSigma1AndSigma()) return "mmx1_sigmabattle";

			else if (players[0].isSigma2AndSigma() || players[1].isSigma2AndSigma()) return "mmx2_grimvigilante";

			else return "mmx3_sigma";
		} else if (players.Count == 2 && is1v1() && (players[0].is1v1MaverickX1() || players[1].is1v1MaverickX1())) {
			return "mmx1_bossbattle";
		} else if (players.Count == 2 && is1v1() && (players[0].is1v1MaverickX2() || players[1].is1v1MaverickX2())) {
			return "mmx2_thebattleensues";
		} else if (players.Count == 2 && is1v1() && (players[0].is1v1MaverickX3() || players[1].is1v1MaverickX3())) {
			return "mmx3_maverickbattle";
		} else if (players.Count == 2 && name == "highway_1v1" && (players[0].isVile || players[1].isVile)) {
			return "mmx1_vile";
		} else {
			return Helpers.removeMapSuffix(name);
		}	
	}

	public string getWinTheme() {
		if (name.Contains("xhunter1") || name.Contains("deepseabase") || name.Contains("maverickfactory") ||
			 name.Contains("robotjunkyard") || name.Contains("volcaniczone") || name.Contains("dinosaurtank") || name.Contains("centralcomputer")
			|| name.Contains("crystalmine") || name.Contains("desertbase") || name.Contains("weathercontrol")) {
			return "mmx2_missionaccomplished";
		}
		if (name.Contains("hunterbase") || name.Contains("giantdam") || name.Contains("weaponsfactory") ||
		 name.Contains("frozentown") || name.Contains("aircraftcarrier") || name.Contains("powercenter") || name.Contains("shipyard") || 
		 name.Contains("quarry") || name.Contains("safaripark") || name.Contains("dopplerlab")) {

			return "mmx3_victory";
		}
		return "mmx1_bossdefeated";
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
}
