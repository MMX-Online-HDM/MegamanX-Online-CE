using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DeviceId;
using DeviceId.Encoders;
using DeviceId.Formatters;
using Newtonsoft.Json;
using SFML.Graphics;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public partial class Global {
	public static decimal version = 20m;
	public static string versionName = "Revision 20";
	public static string subVersionName = "Alpha 13 [RELEASE CANDIDATE 1]";
	public static string subVersionShortName = "A13 RC1-T2";

	// THIS VALUE MUST ALWAYS MANUALLY BE SET AFTER UPDATING ASSETS BEFORE BUILDING A RELEASE BUILD.
	// Obtain it by pressing F1 in main menu.
	// This step could be automated as future improvement in build scripts.
	private const string assetChecksum = "86B00C17076AD59E94D34BEF561B5717";

	// For forks/mods of the game, add a prefix here so that different forks
	// don't conflict with each other or the base game
	public const string checksumPrefix = "[Community Edition]";
	// Use this to make sure the checksum varies.
	// Better to use together with "checksumPrefix" and be diferent from it.
	public const string checksumPrefix2 = "CE-A13-RC1-T2";
	// Final checksum key.
	public const string checksumKey = checksumPrefix + " " + checksumPrefix2;
	// For displaying the name of the mod in the version string.
	public static string shortForkName = "CE";

	public static string prodChecksum = checksumPrefix + " " + assetChecksum;

	// Gemplay variables.
	public static readonly bool canFlinchCombo = true;

	// Some terminology related stuff.
	public static string nameCoin = "Metal";
	public static string nameCoins = "Metals";

	public static bool isLoading;

	public static void promptDebugSettings() {
		//testDocumentsInDebug = Helpers.showMessageBoxYesNo("Test documents in debug?", "Debug Settings");
		//useOptimizedAssetsOverride = Helpers.showMessageBoxYesNo("Test optimized assets?", "Debug Settings");
	}

	/*
	 * A useful section of debug variables you can set to quickly jump into testing something on F5/Ctrl+F5
	 * For example, _quickStart = true and quickStartMap = "training" immediately spawns you in training in launch.
	 * Can set a lot of other things like character, opponent character, game mode, etc.
	 * IF YOU ADD ANY DEBUG SETTINGS YOU MUST SET THEM TO OFF VALUE IN INIT().
	 * Don't distribute a build with these improperly set!
	 */

	// Most common by far
	public static int quickStartCharNum = 2;
	public static int quickStartBotCharNum = 0;

	public static bool _quickStart = true;
	public static string quickStartMap = "training";
	public static bool quickStartMirrored = false;

	// Training
	public static int quickStartTrainingBotCount = 1;
	public static string quickStartTrainingGameMode = GameMode.Deathmatch;
	public static bool spawnTrainingHealth = true;
	public static bool underwaterTraining = false;
	public static bool quickStartTrainingLoadout = true;
	public static bool disableBackwalls = true;
	public static bool quickStartDisableVehiclesTraining = true;

	// Non-training
	public static string quickStartGameMode = GameMode.Deathmatch;
	public static int quickStartBotCount = 0;
	public static int quickStartPlayTo = 40;
	public static int quickStartTeam = 1;
	public static bool quickStartDisableVehicles = false;

	// Both
	public static bool quickStartFixedCam = false;
	public static int? quickStartSpawn = null;
	public static int? quickStartSameChar = null;
	public static int? quickStartMechNum = 0;
	public static bool? quickStartVileMK2 = false;
	public static bool? quickStartVileMK5 = false;
	public static string? overrideSpawnPoint = null;//"Spawn Point3";

	// Online quickstart
	public static bool quickStartOnline = false;
	public static string quickStartOnlineMap = "training";
	public static string quickStartOnlineGameMode = GameMode.CTF;
	public static NetcodeModel quickStartNetcodeModel = NetcodeModel.FavorAttacker;
	public static int quickStartNetcodePing = 500;

	public static int quickStartOnlineHostCharNum = 2;
	public static int quickStartOnlineClientCharNum = 0;

	public static int quickStartOnlineHostSound = 0;
	public static int quickStartOnlineHostMusic = 0;
	public static int quickStartOnlineClientSound = 1;
	public static int quickStartOnlineClientMusic = 1;

	public static int quickStartOnlineBotCount = 1;

	// Network options to simulate lag when developing locally.
	// Note, both relay server and client use this so if both are built and
	// run using these settings, values will be doubled
	public static float simulatedLatency = 0;
	public static float simulatedPacketLoss = 0;
	public static float simulatedDuplicates = 0;

	// Rarely used
	public static bool? overrideFullscreen = null;
	public static int? overrideAimMode = null;
	public static bool? autoFire = false;
	public static bool quickStartDisableHtSt = false;

	public static bool quickStart { get { return _quickStart && !quickStartOnline; } }
	public static bool anyQuickStart { get { return _quickStart || quickStartOnline; } }

	// Default region JSON.
	public static string defaultRegionJson =
@"{
   ""name"": """",
   ""ip"": """"
}";

	// Feature switches
	public static bool maverickWallClimb = false;

	public static bool debug = false;

	public static bool consoleDebugLogging = false;

	public static bool showHitboxes = false;
	public static bool showGridHitboxes = false;
	public static bool showTerrainGridHitboxes = false;
	public static bool showAIDebug = false;
	public static bool debugDrop = false;
	public static bool debugCharMovement = false;
	public static int calledPerFrame = 0;
	public static int collisionCalls = 0;
	public static string debugString1 = "";
	public static string debugString2 = "";
	public static string debugString3 = "";
	public static bool skipCharWepSel = false;
	public static bool showDiagnostics = false;
	public static bool debugDNACores = false;
	// Generic global that can be used for quick conditional breakpoints in low-level physics methods
	public static bool breakpoint = false;
	public static int? overrideFPS = 60;
	public static bool disableShaderOverride = false;
	public static bool? useOptimizedAssetsOverride = false;
	public static bool useLocalIp = false;
	public static bool testDocumentsInDebug;
	public static int? fontTypeOverride = null;
	public static bool shouldAiAutoRevive;
	public static bool overrideDrawCursorChar = false;
	public static bool overrideDrawName = false;
	public static bool overrideDrawHealth = false;

	// IP Data.
	public static string radminIP = "";

	// IF YOU ADD ANY DEBUG SETTINGS YOU MUST SET THEM TO INACTIVE VALUE IN THIS FUNCTION
	public static void Init() {
		// Notes: This is not needed anymore.
		// spf = 1f / 60f;
		if (!debug) {
			showDiagnostics = false;
			_quickStart = false;
			quickStartOnline = false;
			quickStartSpawn = null;
			showHitboxes = false;
			showGridHitboxes = false;
			debugDrop = false;
			debugCharMovement = false;
			skipCharWepSel = false;
			simulatedLatency = 0f;
			simulatedPacketLoss = 0f;
			simulatedDuplicates = 0f;
			overrideFullscreen = null;
			overrideAimMode = null;
			autoFire = null;
			overrideFPS = null;
			quickStartMechNum = null;
			quickStartVileMK2 = null;
			spawnTrainingHealth = true;
			disableBackwalls = false;
			quickStartFixedCam = false;
			quickStartSameChar = null;
			breakpoint = false;
			disableShaderOverride = false;
			useOptimizedAssetsOverride = null;
			useLocalIp = false;
			quickStartTrainingLoadout = false;
			quickStartDisableHtSt = false;
			fontTypeOverride = 0;
			shouldAiAutoRevive = false;
			overrideDrawCursorChar = false;
			overrideDrawName = false;
			overrideDrawHealth = false;
			overrideSpawnPoint = null;
		}
	}

	public static void cheats() {
		if (Global.level == null) return;

		//if (!Global.showAIDebug)
		if (Global.input.isPressed(Key.F1)) {
			//Global.breakpoint = true;
			//Global.showAIDebug = true;
			//Global.level.setMainPlayerSpectate();
			//Global.level.mainPlayer.character.addInfectedTime(null, 8);
			//Global.level.otherPlayer.character.addInfectedTime(null, 8);
			//DevConsole.toggleFTD();
		}
		if (Global.input.isPressed(Key.F2)) {
			Global.showHitboxes = !Global.showHitboxes;
		}
		if (Global.input.isPressed(Key.F3)) {
			Global.showGridHitboxes = !Global.showGridHitboxes;
			//Global.showAIDebug = !Global.showAIDebug;
		}
		if (Global.input.isPressed(Key.F4)) {
			Global.level.mainPlayer?.forceKill();
			//Global.level?.mainPlayer?.character?.setHurt(1, Global.defFlinch);
		}
		if (Global.input.isPressed(Key.F5)) {
			Global.showDiagnostics = !Global.showDiagnostics;
		}
		if (Global.input.isPressed(Key.F6)) {
			Global.level.mainPlayer.currency = 100;
		}

		if (Global.input.isPressed(Key.F7)) {
			DevConsole.toggleInvulnFrames(10);
		}

		//if (Global.input.isPressed(Key.F8)) {
		//DevConsole.changeTeam();
		//}

		if (Global.input.isPressed(Key.F9)) {
			if (AI.trainingBehavior == AITrainingBehavior.Default) AI.trainingBehavior = AITrainingBehavior.Idle;
			else if (AI.trainingBehavior == AITrainingBehavior.Idle) AI.trainingBehavior = AITrainingBehavior.Attack;
			else if (AI.trainingBehavior == AITrainingBehavior.Attack) AI.trainingBehavior = AITrainingBehavior.Jump;
			else if (AI.trainingBehavior == AITrainingBehavior.Jump) AI.trainingBehavior = AITrainingBehavior.Default;
		}
		if (Global.input.isPressed(Key.F8)) {
			var aiPlayer = Global.level.players[1];
			if (aiPlayer?.character != null) {
				if (Global.level.mainPlayer.input == Global.input) {
					Global.level.mainPlayer.input = new Input(true);
					aiPlayer.isAI = false;
					aiPlayer.character.ai = null;
					aiPlayer.input = Global.input;
				} else {
					Global.level.mainPlayer.input = Global.input;
					aiPlayer.character.ai = new AI(aiPlayer.character);
					aiPlayer.isAI = true;
					aiPlayer.input = new Input(true);
				}
			}
		}

		if (Global.input.isPressed(Key.F11)) {
			var ms = Global.level.musicSources.FirstOrDefault();
			if (ms != null) {
				ms.setNearEndCheat();
			} else {
				Global.music.setNearEndCheat();
			}
		}
	}

	// End debug section.

	public static bool hideMouse = false;
	public static int randomTipIndex = 0;
	public const int maxUnconnectedMTUSize = 8191;
	public static HashSet<string> shadersFailed = new HashSet<string>();
	public static bool shadersNotSupported;

	public static Dictionary<string, LevelData> levelDatas = new Dictionary<string, LevelData>();
	public static Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
	public static Dictionary<string, Texture> fontTextures = new Dictionary<string, Texture>();
	public static Dictionary<string, Texture[,]> mapTextures = new Dictionary<string, Texture[,]>();
	public static Dictionary<string, AnimData> sprites = new();
	public static Dictionary<string, SoundBufferWrapper> soundBuffers = new();
	public static Dictionary<string, SoundBufferWrapper> voiceBuffers = new();
	public static Dictionary<string, SoundBufferWrapper> charSoundBuffers = new();
	public static Dictionary<string, MusicWrapper> musics = new Dictionary<string, MusicWrapper>();
	public static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
	public static Dictionary<string, ShaderWrapper> shaderWrappers = new Dictionary<string, ShaderWrapper>();
	public static Dictionary<string, string> shaderCodes = new Dictionary<string, string>();

	// For indexing purposes.
	public static Dictionary<string, ushort> spriteIndexByName = new();
	public static Dictionary<int, string> spriteNameByIndex = new();
	public static int spriteCount = 0;
	public static int realSpriteCount = 0;

	// For indexing purposes.
	public static Dictionary<string, ushort> soundIndexByName = new();
	public static Dictionary<int, string> soundNameByIndex = new();
	public static int soundCount = 0;

	// First: existing, second: new cloned sprite
	public static Dictionary<string, string> spriteAliases = new Dictionary<string, string>()
	{
			{ "chillp_fall", "chillp_enter" },
			{ "chillp_jump", "chillp_exit" },
			{ "chillp_die", "chillp_hurt" },

			{ "sparkm_fall", "sparkm_enter" },
			{ "sparkm_jump", "sparkm_exit" },
			{ "sparkm_die", "sparkm_hurt" },

			{ "armoreda_roll", "armoreda_na_roll" },
			{ "armoreda_fall", "armoreda_enter" },
			{ "armoreda_jump", "armoreda_exit" },
			{ "armoreda_die", "armoreda_hurt" },
			{ "armoreda_na_die", "armoreda_na_hurt" },

			{ "launcho_fall", "launcho_enter" },
			{ "launcho_jump", "launcho_exit" },
			{ "launcho_die", "launcho_hurt" },

			{ "boomerk_fall", "boomerk_enter" },
			{ "boomerk_jump", "boomerk_exit" },
			{ "boomerk_die", "boomerk_hurt" },

			{ "stingc_fall", "stingc_enter" },
			{ "stingc_jump", "stingc_exit" },
			{ "stingc_die", "stingc_hurt" },

			{ "storme_fall", "storme_enter" },
			{ "storme_jump", "storme_exit" },
			{ "storme_die", "storme_hurt" },

			{ "flamem_fall", "flamem_enter" },
			{ "flamem_jump", "flamem_exit" },
			{ "flamem_die", "flamem_hurt" },

			{ "velg_fall", "velg_enter" },
			{ "velg_jump", "velg_exit" },
			{ "velg_die", "velg_hurt" },

			{ "sigma2_viral_enter", "sigma2_viral_possess,sigma2_viral_exit" },

			{ "wsponge_fall", "wsponge_enter" },
			{ "wsponge_jump", "wsponge_exit" },
			{ "wsponge_die", "wsponge_hurt" },

			{ "wheelg_fall", "wheelg_enter" },
			{ "wheelg_jump", "wheelg_exit" },
			{ "wheelg_die", "wheelg_hurt" },

			{ "bcrab_fall", "bcrab_enter" },
			{ "bcrab_jump", "bcrab_exit" },
			{ "bcrab_die", "bcrab_hurt" },

			{ "fstag_fall", "fstag_enter" },
			{ "fstag_jump", "fstag_exit" },
			{ "fstag_die", "fstag_hurt" },

			{ "morphm_fall", "morphm_enter" },
			{ "morphm_jump", "morphm_exit" },
			{ "morphm_die", "morphm_hurt" },

			{ "morphmc_fall", "morphmc_enter" },
			{ "morphmc_jump", "morphmc_exit" },
			{ "morphmc_die", "morphmc_hurt" },
			{ "morphmc_idle", "morphmc_latch" },

			{ "magnac_fall", "magnac_enter" },
			{ "magnac_jump", "magnac_exit" },

			{ "csnail_fall", "csnail_enter" },
			{ "csnail_jump", "csnail_exit" },

			{ "overdriveo_fall", "overdriveo_enter" },
			{ "overdriveo_jump", "overdriveo_exit" },
			{ "overdriveo_die", "overdriveo_hurt" },

			{ "fakezero_fall", "fakezero_enter" },
			{ "fakezero_jump", "fakezero_exit" },
			{ "fakezero_die", "fakezero_hurt" },

			{ "bbuffalo_fall", "bbuffalo_enter" },
			{ "bbuffalo_jump", "bbuffalo_exit" },

			{ "tseahorse_fall", "tseahorse_enter" },
			{ "tseahorse_jump", "tseahorse_exit" },

			{ "tunnelr_fall", "tunnelr_enter" },
			{ "tunnelr_jump", "tunnelr_exit" },

			{ "voltc_fall", "voltc_enter" },
			{ "voltc_jump", "voltc_exit" },

			{ "crushc_fall", "crushc_enter" },
			{ "crushc_jump", "crushc_exit" },

			{ "neont_fall", "neont_enter" },
			{ "neont_jump", "neont_exit" },

			{ "gbeetle_fall", "gbeetle_enter" },
			{ "gbeetle_jump", "gbeetle_exit" },

			{ "bhornet_fall", "bhornet_enter" },
			{ "bhornet_jump", "bhornet_exit" },

			{ "drdoppler_jump", "drdoppler_exit" },

			{ "kaisersigma_idle_body", "kaisersigma_taunt_body" },
			{ "kaisersigma_empty", "kaisersigma_empty_fadeout" }
		};

	private static MatchmakingQuerier _matchmakingQuerier;
	public static MatchmakingQuerier matchmakingQuerier {
		get {
			if (_matchmakingQuerier == null) {
				_matchmakingQuerier = new MatchmakingQuerier();
			}
			return _matchmakingQuerier;
		}
	}

	public static string fileChecksumBlob = checksumKey;
	private static string _checksum;
	public static string checksum {
		get {
			return checksumPrefix + " " + _checksum;
		}
		set {
			_checksum = value;
		}
	}
	public static string MD5Checksum => (_checksum);

	public static string CRC32Checksum;

	public static string getShortChecksum() {
		string retStr = "";
		bool once = false;
		for (int i = 0; i < checksum.Length; i++) {
			char c = checksum[i];
			if (i < 3 || i > checksum.Length - 4) retStr += c;
			else if (!once) {
				once = true;
				retStr += "..";
			}
		}
		return retStr;
	}

	public static void computeChecksum() {
		var checksumBytes = Encoding.UTF8.GetBytes(fileChecksumBlob);

		using (MD5 md5 = MD5.Create()) {
			checksum = BitConverter.ToString(md5.ComputeHash(checksumBytes)).Replace("-", String.Empty);
		}
		CRC32Checksum = BitConverter.ToString(Crc32.Hash(checksumBytes)).Replace("-", String.Empty);
		fileChecksumBlob = "";
	}

	public static string assetPath = "./";

	public static string writePath = "";

	public static float lastFrameProcessTime;
	public static List<float> lastFrameProcessTimes = new List<float>(120);
	public static List<long> lastFramePacketIncreases = new List<long>(120);
	// [5 packets received (second 1), 3 packets received (second 2), 2 packets received (second 3), ...]
	public static List<long> last10SecondsPacketsReceived = new List<long>();
	public static long packetTotal1SecondAgo;

	public const int fpsCap = 60;
	public static float currentFPS = 60;
	public static float logicFPS = 60;
	public static float speedMul = 1;
	public static readonly float secondsFrameDuration = 1f / 60f;
	public static float spf {
		get {
			if (speedMul != 1) {
				return secondsFrameDuration * speedMul;
			}
			return secondsFrameDuration;
		}
	}
	public static float time;
	public static int frameCount = 0;
	public static int normalizeFrames(int frames) {
		/*
		float fpsRatio = 1;
		frames = MathInt.Round(frames * fpsRatio);
		*/
		if (frames <= 0) frames = 1;
		return frames;
	}
	public static bool isOnFrame(int frame) {
		return frameCount % normalizeFrames(frame) == 0;
	}
	// cycle = 2: 2 frames show visible, 2 frames hide, for a blink/flash effect
	public static bool isOnFrameCycle(int cycle) {
		int frames = normalizeFrames(cycle);
		return frameCount % frames * 2 < frames;
	}

	public static bool isSkippingFrames = false;

	public static bool paused = false;

	public const int maxPlayerNameLength = 10;

	public static Point startPos;
	public static Input input;
	public static bool once = false;
	public static bool isMouseLocked;

	public static int defaultMusicVolume = 50;

	public static List<SoundWrapper> sounds = new List<SoundWrapper>();
	public static MusicWrapper music = null;

	public static int defaultThresholdPing = 300;
	public static Level level;
	public static ServerClient? serverClient;
	public static Server? localServer;
	public static bool isOffline { get { return serverClient == null; } }
	public static bool isHost { get { return level != null && level.isHost; } }
	public static bool canControlKillscore { get { return level != null && (isOffline || level.isHost); } }
	public static LeaveMatchSignal? leaveMatchSignal;
	public const int basePort = 14242;
	public const int clientPort = 14240;
	public static bool firstTimeVersionCheck = false;

	public static string encryptionKey;
	public static string secretPrefix = null;
	public static List<BanEntry> banList = new List<BanEntry>();
	public static bool checkBan;
	public static BanEntry banEntry;
	public static bool isChatBanned { get { return banEntry != null && banEntry.banType == 1; } }
	private static string _deviceId;
	public static string deviceId {
		get {
#if RELAYSERVER
			return "";
#endif
			if (_deviceId == null) {
				try {
					_deviceId = new DeviceIdBuilder()
						.AddMachineName()
						.AddMacAddress()
					.UseFormatter(new HashDeviceIdFormatter(() => SHA256.Create(), new Base64UrlByteArrayEncoder()))
					.ToString();
				} catch (Exception ex1) {
					Logger.logException(ex1, false, "DeviceIdAttempt1", forceLog: true);
					try {
						_deviceId = new DeviceIdBuilder()
							.AddMachineName()
							.AddUserName()
							.AddOsVersion()
						.UseFormatter(
							new HashDeviceIdFormatter(
								() => SHA256.Create(), new Base64UrlByteArrayEncoder()
							)
						)
						.ToString();
					} catch (Exception ex2) {
						_deviceId = "";
						Logger.logException(ex2, false, "DeviceIdAttempt2", forceLog: true);
						if (Options.main.isDeveloperConsoleEnabled()) {
							Logger.LogFatalException(ex2);
						}
					}
				}
			}
			return _deviceId;
		}
	}

	public const int maxServers = 5;
	public static int tickRate = 1;

	public static readonly int superFlinch = 36;
	public static readonly int defFlinch = 26;
	public static readonly int halfFlinch = 13;
	public static readonly int miniFlinch = 6;

	public static DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

	public static bool levelStarted() {
		return level != null && level.started;
	}

	public static void playSound(string soundKey, bool playIfExists = true) {
		soundKey = soundKey.ToLowerInvariant();
		if (!playIfExists && sounds.Any(s => s.soundBuffer.soundKey == soundKey)) {
			return;
		}
		SoundWrapper soundWrapper = new SoundWrapper(soundBuffers[soundKey], null);
		Global.sounds.Add(soundWrapper);
		soundWrapper.sound.Play();
	}

	public static void changeMusic(string newMusic) {
		if (music != null) music.stop();
		if (level?.mainPlayer != null) {
			string musicExtension = level.mainPlayer.charNum switch {
				0 => "mmx",
				1 => "zero",
				2 => "axl",
				3 => "vile",
				4 => "sigma",
				_ => "nullchar"
			};
			if (musics.ContainsKey(newMusic + "." + musicExtension)) {
				newMusic = newMusic + ".mmx";
			}
		}
		if (musics.ContainsKey(newMusic)) {
			music = musics[newMusic];
		} else {
			music = new MusicWrapper();
		}
		music.updateVolume();
		music.play();
	}

	/////////////////////////////////////
	// Regions/Versions/Update section //
	/////////////////////////////////////
	private static List<Region> _regions;
	public static List<Region> regions {
		get {
			if (_regions == null) {
				if (debug && useLocalIp) {
					_regions = new List<Region>()
					{
						new Region("LAN", LANIPHelper.GetLocalIPAddress()),
					};
				} else {
					string text = Global.defaultRegionJson;
					if (Helpers.FileExists("region.json")) {
						text = Helpers.ReadFromFile("region.json");
					}
					if (!string.IsNullOrEmpty(text)) {
						Region region;

						try {
							region = JsonConvert.DeserializeObject<Region>(text);
						} catch {
							throw new Exception(
								"region.json has improper format and could not be parsed. Must be valid JSON."
							);
						}

						// Validate
						if (!string.IsNullOrEmpty(region.url)) {
							try {
								region.ip = System.Net.Dns.GetHostAddresses(region.url)[0].ToString();
							} catch {
								region.ip = null;
							}
						}
						if (string.IsNullOrEmpty(region.name) || string.IsNullOrEmpty(region.ip)) {
							//throw new Exception("region.json has missing fields.");
							region = new Region();
						} else {
							if (!region.ip.IsValidIpAddress()) {
								throw new Exception("region.json has an invalid IP.");
							}
							if (region.name.Length > 10) {
								region.name = region.name.Substring(0, 10);
							}
						}
						_regions = new List<Region>() { region };
					} else {
						return new List<Region>();
					}
				}
			}
			return _regions;
		}
	}

	public static List<Region> lanRegions = new List<Region>();

	public static Task regionPingTask;
	public static void updateRegionPings() {
		Task.Run(() => {
			lock (regions) {
				foreach (var region in regions) {
					region.computePing();
				}
			}
		});
	}

	public static void updateLANRegionPings() {
		Task.Run(() => {
			lock (lanRegions) {
				foreach (var lanRegion in lanRegions) {
					lanRegion.computePing();
				}
			}
		});
	}

	// Since the official game is no longer under development,
	// we won't fetch the server version for an update check anymore.
	/*
	public static decimal serverVersion = decimal.MaxValue;

	public static decimal officialServerVersion = decimal.MaxValue;
	public static void fetchServerVersion() {
		var mainRegion = Options.main.getRegionOrDefault();
		if (mainRegion == null) return;

		int timeoutMs = debug ? 500 : 5000;
		var response = matchmakingQuerier.send(mainRegion.ip, "GetVersion", "GetVersion", timeoutMs);
		if (response != null) {
			officialServerVersion = decimal.Parse(response, CultureInfo.InvariantCulture);
		}
	}

	public static decimal githubServerVersion = decimal.MaxValue;
	public static void fetchLaunchDataFromGithub() {
		if (Global.debug) return;

		var stopWatch = new Stopwatch();
		try {
			stopWatch.Start();
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = (
				SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls
			);
			using (FastWebClient wc = new FastWebClient()) {
				string launchDataStr = wc.DownloadString(
					"http://gamemaker19.github.io/MMXOnlineDesktop/launchData.txt"
				);
				var pieces = launchDataStr.Split(',');

				githubServerVersion = decimal.Parse(pieces[0], CultureInfo.InvariantCulture);
			}
		} catch (Exception e) {
			//string additionalDetails = (
			"Failed to fetch github server version. Elapsed ms: " +
			stopWatch.ElapsedMilliseconds.ToString()
  			);
			string additionalDetails = "Failed to fetch github server version.";
			Logger.logException(e, false, additionalDetails);
		}
	}
	*/
}
