using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProtoBuf;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public partial class Player {
	public static Player stagePlayer = new Player(
		"Stage", 255, -1,
		new PlayerCharData() { charNum = -1 },
		false, true, GameMode.neutralAlliance,
		new Input(false),
		new ServerPlayer(
			"Stage", 255, false,
			-1, GameMode.neutralAlliance, "NULL", null, 0
		)
	);
	
	public SpawnPoint? firstSpawn;
	public Input input;
	public Character? character;
	public Character lastCharacter;
	public bool ownedByLocalPlayer;
	public float hadoukenAmmo = 1920;
	public float shoryukenAmmo = 1920;
	public float fgMoveMaxAmmo = 1920;
	public bool isDefenderFavoredNonOwner;

	public bool isDefenderFavored {
		get {
			if (character != null && !character.ownedByLocalPlayer) {
				return isDefenderFavoredNonOwner;
			}
			if (Global.level?.server == null) {
				return false;
			}
			if (Global.serverClient == null) {
				return false;
			}
			if (Global.level.server.netcodeModel == NetcodeModel.FavorAttacker) {
				if (Global.serverClient?.isLagging() == true) {
					return true;
				}
				return (getPingOrStartPing() >= Global.level.server.netcodeModelPing);
			}
			return true;
		}
	}

	public string getDisplayPing() {
		int? pingOrStartPing = getPingOrStartPing();
		if (pingOrStartPing == null) return "?";
		return pingOrStartPing.Value.ToString();
	}
	public string getTeamDisplayPing() {
		int? pingOrStartPing = getPingOrStartPing();
		if (pingOrStartPing == null) {
			return "?";
		}
		return MathInt.Floor(pingOrStartPing.Value / 10f).ToString();
	}

	public int? getPingOrStartPing() {
		if (ping == null) {
			return serverPlayer?.startPing;
		}
		return ping.Value;
	}

	public Character preTransformedAxl;
	public bool isDisguisedAxl {
		get {
			return disguise != null;
		}
	}
	public List<Weapon> savedDNACoreWeapons = new List<Weapon>();
	public int axlBulletType;
	public List<bool> axlBulletTypeBought = new List<bool>() { true, false, false, false, false, false, false };
	public List<float> axlBulletTypeAmmo = new List<float>() { 0, 0, 0, 0, 0, 0, 0 };
	public List<float> axlBulletTypeLastAmmo = new List<float>() { 32, 32, 32, 32, 32, 32, 32 };
	public int lastDNACoreIndex = 4;
	public DNACore lastDNACore;
	
	public float zoomRange {
		get {
			if (character is Axl axl && (axl.isWhiteAxl() || axl.hyperAxlStillZoomed)) return 100000;
			return Global.viewScreenW * 2.5f;
		}
	}
	public RaycastHitData assassinHitPos;

	public bool canUpgradeXArmor() {
		return (realCharNum == 0);
	}

	public float adjustedZoomRange { get { return zoomRange - 40; } }

	public int getVileWeight(int? overrideLoadoutWeight = null) {
		if (overrideLoadoutWeight == null) {
			overrideLoadoutWeight = loadout.vileLoadout.getTotalWeight();
		}
		return overrideLoadoutWeight.Value;
	}

	public Point? lastDeathPos;
	public bool lastDeathWasVileMK2;
	public bool lastDeathWasVileV;
	public bool lastDeathWasSigmaHyper;
	public const int zeroHyperCost = 10;
	public const int zBusterZeroHyperCost = 8;
	public const int AxlHyperCost = 10;
	public const int reviveVileCost = 5;
	public const int reviveSigmaCost = 10;
	public const int reviveXCost = 10;
	public const int goldenArmorCost = 5;
	public const int ultimateArmorCost = 10;
	public bool lastDeathCanRevive;
	public int vileFormToRespawnAs;
	public bool hyperSigmaRespawn;
	public float trainingDpsTotalDamage;
	public float trainingDpsStartTime;
	public bool showTrainingDps { get { return isAI && Global.serverClient == null && Global.level.isTraining(); } }

	public bool aiTakeover;
	public MaverickAIBehavior currentMaverickCommand;

	public bool isX { get { return charNum == (int)CharIds.X; } }
	public bool isZero { get { return charNum == (int)CharIds.Zero; } }
	public bool isVile { get { return charNum == (int)CharIds.Vile; } }
	public bool isAxl { get { return charNum == (int)CharIds.Axl; } }
	public bool isSigma { get { return charNum == (int)CharIds.Sigma; } }

	public float healthBackup;

	public float health {
		get => (float)(character?.health ?? 0);
		set {
			if (character != null) {
				character.health = (decimal)value;
			}
		}
	}
	public float _maxHealth = 16;
	public float maxHealth {
		get {
			if (character != null) {
				_maxHealth = (float)character.maxHealth;
			}
			return _maxHealth;
		}
		set {
			if (character != null) {
				character.maxHealth = (decimal)value;
			}
			_maxHealth = value;
		}
	}

	public bool isDead {
		get {
			if (isSigma && currentMaverick != null) {
				return false;
			}
			if (character == null) return true;
			if (ownedByLocalPlayer && character.charState is Die) return true;
			else if (!ownedByLocalPlayer) {
				return health <= 0;
			}
			return false;
		}
	}
	public const float armorHealth = 16;
	public float respawnTime;
	public bool lockWeapon;
	public string[] randomTip;
	public int aiArmorPath;
	public float teamHealAmount;
	public List<CopyShotDamageEvent> copyShotDamageEvents = new List<CopyShotDamageEvent>();

	public bool scanned;
	public bool tagged;

	public List<int> aiArmorUpgradeOrder;
	public int aiArmorUpgradeIndex;
	public bool isAI;   //DO NOT USE THIS for determining if a player is a bot to non hosts in online matches, use isBot below
						//A bot is a subset of an AI; an AI that's in an online match
	public bool isBot {
		get {
			if (serverPlayer == null) {
				return isAI;
			}
			return serverPlayer.isBot;
		}
	}

	public bool isLocalAI {
		get { return isAI && Global.serverClient == null; }
	}

	public int realCharNum {
		get {
			if (isAxl || isDisguisedAxl) return 3;
			return charNum;
		}
	}

	public bool warpedInOnce;
	public bool spawnedOnce;

	public bool isMuted;

	// Subtanks
	private Dictionary<int, List<SubTank>> charSubTanks = new Dictionary<int, List<SubTank>>() {
		{ -1, new() },
		{ (int)CharIds.X, new() },
		{ (int)CharIds.Zero, new() },
		{ (int)CharIds.Vile, new() },
		{ (int)CharIds.Axl, new() },
		{ (int)CharIds.Sigma, new() },
		{ (int)CharIds.PunchyZero, new() },
		{ (int)CharIds.BusterZero, new() },
		{ (int)CharIds.Rock, new() },
	};

	// Heart tanks
	private Dictionary<int, ProtectedInt> charHeartTanks = new Dictionary<int, ProtectedInt>(){
		{ -1, new() },
		{ (int)CharIds.X, new() },
		{ (int)CharIds.Zero, new() },
		{ (int)CharIds.Vile, new() },
		{ (int)CharIds.Axl, new() },
		{ (int)CharIds.Sigma, new() },
		{ (int)CharIds.PunchyZero, new() },
		{ (int)CharIds.BusterZero, new() },
		{ (int)CharIds.Rock, new() },
	};

	// Getter functions.
	public List<SubTank> subtanks {
		get { return charSubTanks[isDisguisedAxl ? 3 : charNum]; }
		set { charSubTanks[isDisguisedAxl ? 3 : charNum] = value; }
	}
	public int heartTanks {
		get {
			if (!ownedByLocalPlayer) {
				return charHeartTanks[isDisguisedAxl ? 3 : charNum].unsafeVal;
			}
			return charHeartTanks[isDisguisedAxl ? 3 : charNum].value;
		}
		set {
			charHeartTanks[isDisguisedAxl ? 3 : charNum].value = value;
		}
	}

	// Currency
	public const int maxCharCurrencyId = 12;
	public static int curMul = Helpers.randomRange(2, 8);

	public ProtectedArrayInt charCurrency = new ProtectedArrayInt(maxCharCurrencyId);
	public int currency {
		get {
			if (!ownedByLocalPlayer) {
				return charCurrency.unsafeVal(isDisguisedAxl ? 3 : charNum);
			}
			return charCurrency[isDisguisedAxl ? 3 : charNum];
		}
		set {
			charCurrency[isDisguisedAxl ? 3 : charNum] = value;
		}
	}

	public bool isSpectator {
		get {
			if (Global.serverClient == null) return isOfflineSpectator;
			return serverPlayer.isSpectator;
		}
		set {
			if (Global.serverClient == null) isOfflineSpectator = value;
			else serverPlayer.isSpectator = value;
		}
	}
	private bool isOfflineSpectator;
	public bool is1v1Combatant;

	public void setSpectate(bool newSpectateValue) {
		if (Global.serverClient != null) {
			string msg = name + " now spectating.";
			if (newSpectateValue == false) msg = name + " stopped spectating.";
			Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(msg, "", null, true), sendRpc: true);
			RPC.makeSpectator.sendRpc(id, newSpectateValue);
		} else {
			isSpectator = newSpectateValue;
		}
	}

	public int selectedRAIndex = Global.quickStartMechNum ?? 0;
	public bool isSelectingRA() {
		if (character is Vile vile && vile.rideMenuWeapon.isMenuOpened) {
			return true;
		}
		return false;
	}

	public int selectedCommandIndex = 0;
	public bool isSelectingCommand() {
		if (weapon is MaverickWeapon mw && mw.isMenuOpened) {
			return true;
		}
		return false;
	}

	// Things needed to be synced to late joiners.
	// Note: these are not automatically applied,
	// you need to add code in Global.level.joinedLateSyncPlayers
	// and update PlayerSync class at top of this file
	public int kills;
	public int assists;
	public int deaths;
	public int getDeathScore() {
		if (Global.level.gameMode is Elimination ||
			Global.level.gameMode is TeamElimination
		) {
			return (Global.level.gameMode.playingTo - deaths);
		}
		return deaths;
	}
	public ushort curMaxNetId;
	public ushort curATransNetId;
	public bool warpedIn = false;
	public float readyTime;
	public const float maxReadyTime = 1.75f;
	public bool readyTextOver = false;
	public ServerPlayer serverPlayer;
	public LoadoutData loadout;
	public bool loadoutSet;
	public LoadoutData previousLoadout;
	public LoadoutData? atransLoadout;
	public AxlLoadout axlLoadout { get { return loadout.axlLoadout; } }

	public bool frozenCastlePurchased;
	public bool speedDevilPurchased;

	// Note: Every time you add an armor, add an "old" version and update DNA Core code appropriately
	public ushort armorFlag;
	public bool frozenCastle;
	public bool speedDevil;

	public Disguise? disguise;

	public int newAlliance;     // Not sure what this is useful for, seems like a pointless clone of alliance that needs to be kept in sync

	// Things that ServerPlayer already has
	public string name;
	public int id;
	public int alliance;    // Only set on spawn with data read from ServerPlayer alliance. The ServerPlayer alliance changes earlier on team change/autobalance
	public int charNum;

	public int newCharNum;
	public int? delayedNewCharNum;
	public int? ping;
	public void syncFromServerPlayer(ServerPlayer serverPlayer) {
		if (!this.serverPlayer.isHost && serverPlayer.isHost) {
			promoteToHost();
		}
		this.serverPlayer = serverPlayer;
		name = serverPlayer.name;
		ping = serverPlayer.ping;

		kills = serverPlayer.kills;
		deaths = serverPlayer.deaths;

		if (ownedByLocalPlayer && serverPlayer.autobalanceAlliance != null &&
			newAlliance != serverPlayer.autobalanceAlliance.Value
		) {
			Global.level.gameMode.chatMenu.addChatEntry(
				new ChatEntry(
					name + " was autobalanced to " +
					GameMode.getTeamName(serverPlayer.autobalanceAlliance.Value), "", null, true
				), true);
			forceKill();
			currency += 5;
			Global.serverClient?.rpc(RPC.switchTeam, RPCSwitchTeam.getSendMessage(id, serverPlayer.autobalanceAlliance.Value));
			newAlliance = serverPlayer.autobalanceAlliance.Value;
		}
	}

	// Shaders
	public ShaderWrapper xPaletteShader = Helpers.cloneGenericPaletteShader("paletteTexture");
	public ShaderWrapper xStingPaletteShader = Helpers.cloneGenericPaletteShader("cStingPalette");
	public ShaderWrapper invisibleShader = Helpers.cloneShaderSafe("invisible");
	public ShaderWrapper zeroPaletteShader = Helpers.cloneGenericPaletteShader("hyperZeroPalette");
	public ShaderWrapper blackBusterZeroPaletteShader = Helpers.cloneGenericPaletteShader("hyperBusterZeroPalette");
	public ShaderWrapper nightmareZeroShader = Helpers.cloneGenericPaletteShader("paletteViralZero");
	public ShaderWrapper zeroAzPaletteShader = Helpers.cloneGenericPaletteShader("paletteAwakenedZero");
	public ShaderWrapper axlPaletteShader = Helpers.cloneShaderSafe("hyperaxl");
	public ShaderWrapper viralSigmaShader = Helpers.cloneShaderSafe("viralsigma");
	public ShaderWrapper viralSigmaShader2 = Helpers.cloneShaderSafe("viralsigma");
	public ShaderWrapper sigmaShieldShader = Helpers.cloneGenericPaletteShader("paletteSigma3Shield");
	public ShaderWrapper acidShader = Helpers.cloneShaderSafe("acid");
	public ShaderWrapper oilShader = Helpers.cloneShaderSafe("oil");
	public ShaderWrapper igShader = Helpers.cloneShaderSafe("igIce");
	public ShaderWrapper infectedShader = Helpers.cloneShaderSafe("infected");
	public ShaderWrapper frozenCastleShader = Helpers.cloneShaderSafe("frozenCastle");
	public ShaderWrapper possessedShader = Helpers.cloneShaderSafe("possessed");
	public ShaderWrapper vaccineShader = Helpers.cloneShaderSafe("vaccine");
	public static ShaderWrapper darkHoldShader = Helpers.cloneShaderSafe("darkhold");
	public ShaderWrapper speedDevilShader = Helpers.cloneShaderSafe("speedDevilTrail");

	// Maverick shaders.
	// Duplicated mavericks are not a thing so this should not be a problem.
	public ShaderWrapper catfishChargeShader = Helpers.cloneGenericPaletteShader("paletteVoltCatfishCharge");
	public ShaderWrapper gatorArmorShader = Helpers.cloneShaderSafe("wheelgEaten");
	public ShaderWrapper spongeChargeShader = Helpers.cloneShaderSafe("wspongeCharge");

	// Projectile shaders.
	public ShaderWrapper timeSlowShader = Helpers.cloneShaderSafe("timeslow");
	public ShaderWrapper darkHoldScreenShader = Helpers.cloneShaderSafe("darkHoldScreen");
	// Charge Lv
	public static ShaderWrapper ZeroPinkC = Helpers.cloneGenericPaletteShader("zeroPinkCharge");
	public static ShaderWrapper ZeroGreenC = Helpers.cloneGenericPaletteShader("zeroGreenCharge");
	public static ShaderWrapper ZeroBlueC = Helpers.cloneGenericPaletteShader("zeroBlueCharge");
	public static ShaderWrapper XPinkC = Helpers.cloneGenericPaletteShader("xPinkCharge");
	public static ShaderWrapper XGreenC = Helpers.cloneGenericPaletteShader("xGreenCharge");
	public static ShaderWrapper XBlueC = Helpers.cloneGenericPaletteShader("xBlueCharge");
	public static ShaderWrapper XYellowC = Helpers.cloneGenericPaletteShader("xYellowCharge");
	public static ShaderWrapper XOrangeC = Helpers.cloneGenericPaletteShader("xOrangeCharge");

	public ShaderWrapper speedBurnerOrange = Helpers.cloneGenericPaletteShader("speedBurnerOrange");
	public ShaderWrapper speedBurnerGrey = Helpers.cloneGenericPaletteShader("speedBurnerGrey");


	// Character specific data populated on RPC request
	public ushort? charNetId;
	public ushort? charRollingShieldNetId;
	public float charXPos;
	public float charYPos;
	public int charXDir;
	public Dictionary<int, int> charNumToKills = new Dictionary<int, int>() {
	};

	public int hyperChargeSlot;
	public int xArmor1v1;
	public int? maverick1v1;
	public bool maverick1v1Spawned;

	public float possessedTime;
	public const float maxPossessedTime = 12;
	public Player possesser;

	public List<GrenadeProj> grenades = new List<GrenadeProj>();
	public List<ChillPIceStatueProj> iceStatues = new List<ChillPIceStatueProj>();
	public List<WSpongeSpike> seeds = new List<WSpongeSpike>();
	public List<Actor> mechaniloids = new List<Actor>();

	public ExplodeDieEffect? explodeDieEffect;
	public bool suicided;

	ushort savedArmorFlag;
	public bool[] headArmorsPurchased = new bool[] { false, false, false };
	public bool[] bodyArmorsPurchased = new bool[] { false, false, false };
	public bool[] armArmorsPurchased = new bool[] { false, false, false };
	public bool[] bootsArmorsPurchased = new bool[] { false, false, false };

	public float lastMashAmount;
	public int lastMashAmountSetFrame;

	public bool isNon1v1MaverickSigma() {
		return isSigma && maverick1v1 == null;
	}

	public bool is1v1MaverickX1() {
		return maverick1v1 <= 8;
	}

	public bool is1v1MaverickX2() {
		return maverick1v1 > 8 && maverick1v1 <= 17;
	}

	public bool is1v1MaverickX3() {
		return maverick1v1 > 17;
	}

	public bool is1v1MaverickFakeZero() {
		return maverick1v1 == 17;
	}

	public int getStartHeartTanks() {
		if (Global.level.isNon1v1Elimination() && Global.level.gameMode.playingTo < 3) {
			return 8;
		}
		if (Global.level.is1v1()) {
			return 8;
		}
		if (Global.level?.server?.customMatchSettings != null) {
			return Global.level.server.customMatchSettings.startHeartTanks;
		}

		return 0;
	}

	public int getStartHeartTanksForChar() {
		if (!Global.level.server.disableHtSt && Global.level?.server?.customMatchSettings == null && !Global.level.gameMode.isTeamMode) {
			int leaderKills = Global.level.getLeaderKills();
			if (leaderKills >= 32) return 8;
			if (leaderKills >= 28) return 7;
			if (leaderKills >= 24) return 6;
			if (leaderKills >= 20) return 5;
			if (leaderKills >= 16) return 4;
			if (leaderKills >= 12) return 3;
			if (leaderKills >= 8) return 2;
			if (leaderKills >= 4) return 1;
		}
		return 0;
	}

	public int getStartSubTanks() {
		if (Global.level?.server?.customMatchSettings != null) {
			return Global.level.server.customMatchSettings.startSubTanks;
		}

		return 0;
	}

	public int getStartSubTanksForChar() {
		if (!Global.level.server.disableHtSt && Global.level?.server?.customMatchSettings == null && !Global.level.gameMode.isTeamMode) {
			int leaderKills = Global.level.getLeaderKills();
			if (leaderKills >= 32) return 4;
			if (leaderKills >= 24) return 3;
			if (leaderKills >= 16) return 2;
			if (leaderKills >= 8) return 1;
		}

		return 0;
	}

	public int getSameCharNum() {
		if (Global.level?.server?.customMatchSettings != null) {
			if (Global.level.gameMode.isTeamMode && alliance == GameMode.redAlliance) {
				return Global.level.server.customMatchSettings.redSameCharNum;
			}
			return Global.level.server.customMatchSettings.sameCharNum;
		}
		return -1;
	}

	public Player(
		string name, int id, int charNum, PlayerCharData playerData,
		bool isAI, bool ownedByLocalPlayer, int alliance, Input input, ServerPlayer serverPlayer
	) {
		this.name = name;
		this.id = id;
		curMaxNetId = getFirstAvailableNetId();
		this.alliance = alliance;
		newAlliance = alliance;
		this.isAI = isAI;

		if (getSameCharNum() != -1) {
			charNum = getSameCharNum();
		}
		if (charNum >= 210) {
			if (Global.level.is1v1()) {
				maverick1v1 = charNum - 210;
				charNum = (int)CharIds.Sigma;
			} else {
				charNum = (int)CharIds.Sigma;
				playerData.charNum = (int)CharIds.Sigma;
			}
		}
		this.charNum = charNum;
		newCharNum = charNum;

		this.input = input;
		this.ownedByLocalPlayer = ownedByLocalPlayer;

		xArmor1v1 = playerData?.armorSet ?? 1;
		if (Global.level.is1v1() && isX) {
			legArmorNum = xArmor1v1;
			bodyArmorNum = xArmor1v1;
			helmetArmorNum = xArmor1v1;
			armArmorNum = xArmor1v1;
		}

		for (int i = 0; i < charCurrency.Length; i++) {
			charCurrency[i] = getStartCurrency();
		}
		foreach (var key in charHeartTanks.Keys) {
			int htCount = getStartHeartTanksForChar();
			int altHtCount = getStartHeartTanks();
			if (altHtCount > htCount) {
				htCount = altHtCount;
			}
			charHeartTanks[key].value = htCount;
		}
		foreach (var key in charSubTanks.Keys) {
			int stCount = key == charNum ? getStartSubTanksForChar() : getStartSubTanks();
			for (int i = 0; i < stCount; i++) {
				charSubTanks[key].Add(new SubTank());
			}
		}

		maxHealth = getMaxHealth();
		health = maxHealth;

		aiArmorPath = new List<int>() { 1, 2, 3 }.GetRandomItem();
		aiArmorUpgradeOrder = new List<int>() { 0, 1, 2, 3 }.Shuffle();

		this.serverPlayer = serverPlayer;

		if (ownedByLocalPlayer && !isAI) {
			loadout = LoadoutData.createFromOptions(id);
			loadoutSet = true;
		} else {
			loadout = LoadoutData.createRandom(id);
			if (ownedByLocalPlayer) {
				loadoutSet = true;
			}
		}

		is1v1Combatant = !isSpectator;
		maxHealth = getMaxHealth();
	}

	public int getHeartTankModifier() {
		return Global.level.server?.customMatchSettings?.heartTankHp ?? 1;
	}

	public float getMaverickMaxHp(MaverickMode controlMode) {
		if (!Global.level.is1v1() && controlMode == MaverickMode.TagTeam) {
			return getModifiedHealth(20) + (heartTanks * getHeartTankModifier());
		}
		return MathF.Ceiling(getModifiedHealth(24));
	}

	public bool hasAllItems() {
		return subtanks.Count >= UpgradeMenu.getMaxSubTanks() && heartTanks >= UpgradeMenu.getMaxHeartTanks();
	}

	public static float getBaseHealth() {
		if (Global.level.server.customMatchSettings != null) {
			return Global.level.server.customMatchSettings.healthModifier;
		}
		return 16;
	}

	public static float getModifiedHealth(float health) {
		if (Global.level.server.customMatchSettings != null) {
			float retHp = getBaseHealth();
			float extraHP = health - 16;

			float hpMulitiplier = MathF.Ceiling(getBaseHealth() / 16);
			retHp += MathF.Ceiling(extraHP * hpMulitiplier);

			if (retHp < 1) {
				retHp = 1;
			}
			return retHp;
		}
		return health;
	}

	public float getDamageModifier() {
		return 1;
	}

	public float getMaxHealth() {
		// 1v1 is the only mode without possible heart tanks/sub tanks
		if (Global.level.is1v1()) {
			return getModifiedHealth(28);
		}
		int bonus = 0;
		if (isSigma) {
			bonus = 6;
		}
		return MathF.Ceiling(
			getModifiedHealth(16 + bonus) + (heartTanks * getHeartTankModifier())
		);
	}

	public void creditHealing(float healAmount) {
		teamHealAmount += healAmount;
		if (teamHealAmount >= 16) {
			teamHealAmount = 0;
			currency++;
		}
	}

	public void applyLoadoutChange() {
		loadout = LoadoutData.createFromOptions(id);
		if (Global.level.is1v1() && isSigma) {
			if (maverick1v1 != null) loadout.sigmaLoadout.commandMode = 3;
			else loadout.sigmaLoadout.commandMode = 2;
		}
		syncLoadout();
	}

	public void syncLoadout() {
		RPC.broadcastLoadout.sendRpc(this);
	}

	public int? teamAlliance {
		get {
			if (Global.level.gameMode.isTeamMode) {
				return alliance;
			}
			return null;
		}
	}

	public int getHudLifeSpriteIndex() {
		return charNum + (maverick1v1 ?? -1) + 1;
	}

	public const int netIdsPerPlayer = 2000;

	// The first net id this player could possibly own. This includes the "reserved" ones
	public ushort getStartNetId() {
		return (ushort)(Level.firstNormalNetId + (ushort)(id * netIdsPerPlayer));
	}

	public static int? getPlayerIdFromCharNetId(ushort charNetId) {
		int netIdInt = charNetId;
		int maxIdInt = Level.firstNormalNetId;
		int diff = (netIdInt - maxIdInt);
		if (diff < 0) return null;
		if (diff % netIdsPerPlayer != 0) {
			return null;
		}
		netIdInt -= maxIdInt;
		return netIdInt / netIdsPerPlayer;
	}

	// First available unreserved net id for general instantiation use of new objects
	public ushort getFirstAvailableNetId() {
		// +0 = Char
		// +1 = Ride armor (GM19 changed this is not a RA anymore)
		return (ushort)(getStartNetId() + 10);
	}

	public ushort getNextATransNetId() {
		if (curATransNetId < getStartNetId()) {
			curATransNetId = (ushort)(getStartNetId());
		}
		ushort retId = curATransNetId;
		curATransNetId++;
		if (curATransNetId >= getStartNetId() + 10) {
			curATransNetId = (ushort)(getStartNetId());
		}
		return retId;
	}

	// Usually, only the main player is allowed to get the next actor net id. 
	// The exception is if you call setNextActorNetId() first. The assert checks for that in debug.
	public ushort getNextActorNetId(bool allowNonMainPlayer = false) {
		// Increase by 1 normall.
		int retId = curMaxNetId;
		curMaxNetId++;
		// Use this to avoid duplicates.
		if (ownedByLocalPlayer) {
			while (
				Global.level.actorsById.GetValueOrDefault(curMaxNetId) != null &&
				curMaxNetId <= getStartNetId() + netIdsPerPlayer
			) {
				// Overwrite if destroyed.
				if (Global.level.actorsById[curMaxNetId].destroyed) {
					break;
				}
				curMaxNetId++;
			}
		}
		if (curMaxNetId >= getStartNetId() + netIdsPerPlayer) {
			curMaxNetId = getFirstAvailableNetId();
		}
		return (ushort)retId;
	}

	public void setNextActorNetId(ushort curMaxNetId) {
		this.curMaxNetId = curMaxNetId;
	}

	public bool isCrouchHeld() {
		if (character == null) {
			return false;
		}
		if (character is not Axl || Options.main.axlAimMode == 2) {
			return input.isHeld(Control.Down, this);
		}
		if (input.isHeld(Control.AxlCrouch, this)) {
			return true;
		}
		if (!Options.main.axlSeparateAimDownAndCrouch) {
			return input.isHeld(Control.Down, this);
		}
		if (Options.main.axlAimMode == 1) {
			return input.isHeld(Control.Down, this) && !input.isHeld(Control.AimAngleDown, this);
		} else {
			return input.isHeld(Control.Down, this) && !input.isHeld(Control.AimDown, this);
		}
	}

	public void update() {
		for (int i = copyShotDamageEvents.Count - 1; i >= 0; i--) {
			if (Global.time - copyShotDamageEvents[i].time > 2) {
				copyShotDamageEvents.RemoveAt(i);
			}
		}
		for (int i = grenades.Count - 1; i >= 0; i--) {
			if (grenades[i].destroyed || grenades[i] == null) {
				grenades.RemoveAt(i);
			}
		}
		readyTime += Global.spf;
		if (readyTime >= maxReadyTime) {
			readyTextOver = true;
		}
		if (!ownedByLocalPlayer) {
			return;
		}
		if (Global.level.gameMode.isOver && aiTakeover) {
			aiTakeover = false;
			isAI = false;
			if (character != null) character.ai = null;
		}

		if (!Global.level.gameMode.isOver) {
			respawnTime -= Global.spf;
		}

		if (Global.canControlKillscore && Global.isOnFrame(30)) {
			RPC.updatePlayer.sendRpc(id, kills, deaths);
		}

		if (character == null && respawnTime <= 0 && eliminated()) {
			if (Global.serverClient != null && isMainPlayer) {
				RPC.makeSpectator.sendRpc(id, true);
			} else {
				isSpectator = true;
			}
			return;
		}

		vileFormToRespawnAs = 0;
		hyperSigmaRespawn = false;

		if (isVile) {
			if (isSelectingRA()) {
				int maxRAIndex = 3;
				if (character is Vile vile && !vile.isVileMK1) {
					maxRAIndex = 4;
				}
				if (input.isPressedMenu(Control.MenuDown)) {
					selectedRAIndex--;
					if (selectedRAIndex < 0) selectedRAIndex = maxRAIndex;
				} else if (input.isPressedMenu(Control.MenuUp)) {
					selectedRAIndex++;
					if (selectedRAIndex > maxRAIndex) selectedRAIndex = 0;
				}
			}

			if (canReviveVile()) {
				if (input.isPressed(Control.Special1, this) || Global.shouldAiAutoRevive) {
					reviveVile(false);
				} else if (input.isPressed(Control.Special2, this) && !lastDeathWasVileMK2) {
					reviveVile(true);
				}
			}
		} else if (isSigma) {
			if (isSelectingCommand()) {
				if (maverickWeapon.selCommandIndexX == 1) {
					if (input.isPressedMenu(Control.MenuDown)) {
						maverickWeapon.selCommandIndex--;
						if (maverickWeapon.selCommandIndex < 1) {
							maverickWeapon.selCommandIndex = MaverickWeapon.maxCommandIndex;
						}
					} else if (input.isPressedMenu(Control.MenuUp)) {
						maverickWeapon.selCommandIndex++;
						if (maverickWeapon.selCommandIndex > MaverickWeapon.maxCommandIndex) maverickWeapon.selCommandIndex = 1;
					}

					/*
					if (maverickWeapon.selCommandIndex == 2)
					{
						if (input.isPressedMenu(Control.Left))
						{
							maverickWeapon.selCommandIndexX--;
						}
						else if (input.isPressedMenu(Control.Right))
						{
							maverickWeapon.selCommandIndexX++;
						}
					}
					*/
				} else {
					if (input.isPressedMenu(Control.Left) && maverickWeapon.selCommandIndexX == 2) {
						maverickWeapon.selCommandIndexX = 1;
					} else if (input.isPressedMenu(Control.Right) && maverickWeapon.selCommandIndexX == 0) {
						maverickWeapon.selCommandIndexX = 1;
					}
				}
			}

			if (canReviveSigma(out var spawnPoint, 2) &&
				(input.isPressed(Control.Special2, this) ||
				Global.level.isHyper1v1() ||
				Global.shouldAiAutoRevive
			)
			) {
				reviveSigma(2, spawnPoint);
			}
		} else if (isX) {
			if (canReviveX() && (input.isPressed(Control.Special2, this) || Global.shouldAiAutoRevive)) {
				reviveX();
			}
		}

		// Never spawn a character if it already exists
		if (character == null && ownedByLocalPlayer) {
			if (!warpedInOnce && firstSpawn == null) {
				firstSpawn = Global.level.getSpawnPoint(this, true);
				Global.level.camX = MathF.Round(firstSpawn.pos.x) - Global.halfScreenW * Global.viewSize;
				Global.level.camY = MathF.Round(firstSpawn.getGroundY()) - Global.halfScreenH * Global.viewSize - 30;

				Global.level.camX = Helpers.clamp(Global.level.camX, 0, Global.level.width - Global.viewScreenW);
				Global.level.camY = Helpers.clamp(Global.level.camY, 0, Global.level.height - Global.viewScreenH);

				Global.level.computeCamPos(
					new Point(
						Global.level.camX + Global.halfScreenW * Global.viewSize,
						Global.level.camY + Global.halfScreenH * Global.viewSize
					),
					null
				);
			}
			if (shouldRespawn()) {
				ushort charNetId = getNextATransNetId();

				if (Global.level.gameMode is TeamDeathMatch && Global.level.teamNum > 2 && warpedInOnce) {
					List<Player> spawnPoints = Global.level.players.FindAll(
						p => p.teamAlliance == teamAlliance && p.character?.alive == true
					);
					if (spawnPoints.Count != 0) {
						Character randomChar = spawnPoints[Helpers.randomRange(0, spawnPoints.Count - 1)].character!;
						Point warpInPos = Global.level.getGroundPosNoKillzone(
							randomChar.pos, Global.screenH
						) ?? randomChar.pos;
						spawnCharAtPoint(
							newCharNum, getCharSpawnData(newCharNum),
							warpInPos, randomChar.xDir, charNetId, true
						);
					} else {
						SpawnPoint spawnPoint = firstSpawn ?? Global.level.getSpawnPoint(this, !warpedInOnce);
						firstSpawn = null;
						int spawnPointIndex = Global.level.spawnPoints.IndexOf(spawnPoint);
						spawnCharAtSpawnIndex(spawnPointIndex, charNetId, true);
					}
				}
				else {
					var spawnPoint = Global.level.getSpawnPoint(this, !warpedInOnce);
					if (spawnPoint == null) return;
					int spawnPointIndex = Global.level.spawnPoints.IndexOf(spawnPoint);
					spawnCharAtSpawnIndex(spawnPointIndex, charNetId, true);
				}
			}
		}

		updateWeapons();
	}

	public bool eliminated() {
		if (Global.level.gameMode is Elimination || Global.level.gameMode is TeamElimination) {
			if (!isSpectator && (deaths >= Global.level.gameMode.playingTo || (Global.level.isNon1v1Elimination() && serverPlayer?.joinedLate == true))) {
				return true;
			}
		}
		return false;
	}

	public bool shouldRespawn() {
		if (character != null) return false;
		if (respawnTime > 0) return false;
		if (!ownedByLocalPlayer) return false;
		if (isSpectator) return false;
		if (eliminated()) return false;
		if (isAI) return true;
		if (Global.level.is1v1()) return true;
		if (!readyTextOver) return false;
		if (!spawnedOnce) {
			spawnedOnce = true;
			return true;
		}
		if (!Menu.inMenu && input.isPressedMenu(Control.MenuConfirm)) {
			return true;
		}
		if (respawnTime < -10) {
			return true;
		}
		return false;
	}

	public void spawnCharAtSpawnIndex(int spawnPointIndex, ushort charNetId, bool sendRpc) {
		if (Global.level.spawnPoints == null || spawnPointIndex >= Global.level.spawnPoints.Count) {
			return;
		}

		var spawnPoint = Global.level.spawnPoints[spawnPointIndex];

		spawnCharAtPoint(
			newCharNum, getCharSpawnData(newCharNum),
			new Point(spawnPoint.pos.x, spawnPoint.getGroundY()), spawnPoint.xDir, charNetId, sendRpc
		);
	}

	
	public byte[] getCharSpawnData(int charNum) {
		if (ownedByLocalPlayer) {
			applyLoadoutChange();
			syncLoadout();
		}
		if (charNum == (int)CharIds.X) {
			return [
				(byte)loadout.xLoadout.weapon1,
				(byte)loadout.xLoadout.weapon2,
				(byte)loadout.xLoadout.weapon3,
				(byte)loadout.xLoadout.melee
			];
		}
		if (charNum == (int)CharIds.Axl) {
			return [
				(byte)loadout.axlLoadout.weapon2,
				(byte)loadout.axlLoadout.weapon3,
				(byte)loadout.axlLoadout.hyperMode
			];
		}
		if (charNum == (int)CharIds.Sigma) {
			return [
				(byte)loadout.sigmaLoadout.sigmaForm,
				(byte)loadout.sigmaLoadout.maverick1,
				(byte)loadout.sigmaLoadout.maverick2,
				(byte)loadout.sigmaLoadout.commandMode
			];
		}
		return [];
	}

	public Character? spawnCharAtPoint(
		int spawnCharNum, byte[] extraData,
		Point pos, int xDir, ushort charNetId, bool sendRpc,
		bool isMainChar = true, bool forceSpawn = false, bool isWarpIn = true
	) {
		if (sendRpc) {
			RPC.spawnCharacter.sendRpc(spawnCharNum, extraData, pos, xDir, id, charNetId);
		}

		if (Global.level.gameMode.isTeamMode) {
			alliance = newAlliance;
		}

		// Skip if a is a main character and it already exists.
		// Or if we are creating a character with the same netId as the current one.
		if (!forceSpawn && isMainChar && character != null || charNetId == character?.netId) {
			return null;
		}

		// ONRESPAWN, SPAWN, RESPAWN, ON RESPAWN, ON SPAWN LOGIC, SPAWNLOGIC
		charNum = spawnCharNum;
		if (isMainChar) {
			if (isMainPlayer) {
				previousLoadout = loadout;
				applyLoadoutChange();
				hyperChargeSlot = Global.level.is1v1() ? 0 : Options.main.hyperChargeSlot;
				currentMaverickCommand = (
					Options.main.maverickStartFollow ? MaverickAIBehavior.Follow : MaverickAIBehavior.Defend
				);
			} else if (isAI && Global.level.isTraining()) {
				previousLoadout = loadout;
				applyLoadoutChange();
				hyperChargeSlot = Global.level.is1v1() ? 0 : Options.main.hyperChargeSlot;
				currentMaverickCommand = (
					Options.main.maverickStartFollow ? MaverickAIBehavior.Follow : MaverickAIBehavior.Defend
				);
			}
		}
		maxHealth = getMaxHealth();
		health = maxHealth;
		assassinHitPos = null;

		Character newChar;
		// X
		if (charNum == (int)CharIds.X) {
			XLoadout xLoadout = new() {
				weapon1 = extraData[0],
				weapon2 = extraData[1],
				weapon3 = extraData[2],
				melee = extraData[3]
			};

			if (isMainChar) {
				loadout.xLoadout = xLoadout;
			}
			newChar = new MegamanX(
				this, pos.x, pos.y, xDir,
				false, charNetId, ownedByLocalPlayer,
				xLoadout: xLoadout, isWarpIn: isWarpIn
			);
		}
		// Saber Zero.
		else if (charNum == (int)CharIds.Zero) {
			newChar = new Zero(
				this, pos.x, pos.y, xDir,
				false, charNetId, ownedByLocalPlayer
			);
		}
		// Vile.
		else if (charNum == (int)CharIds.Vile) {
			bool mk2VileOverride = Global.level.isHyperMatch();

			newChar = new Vile(
				this, pos.x, pos.y, xDir, false, charNetId,
				ownedByLocalPlayer, mk2VileOverride: mk2VileOverride, isWarpIn: isWarpIn
			);
		}
		// GM19 Axl.
		else if (charNum == (int)CharIds.Axl) {
			AxlLoadout axlLoadout = loadout?.axlLoadout?.clone() ?? new();
			axlLoadout.weapon2 = extraData[0];
			axlLoadout.weapon3 = extraData[1];
			axlLoadout.hyperMode = extraData[2];

			if (isMainChar) {
				loadout.axlLoadout = axlLoadout;
			}
			newChar = new Axl(
				this, pos.x, pos.y, xDir,
				false, charNetId, ownedByLocalPlayer, isWarpIn: isWarpIn,
				axlLoadout: axlLoadout
			);
		}
		// Sigma.
		else if (charNum == (int)CharIds.Sigma) {
			int sigmaForm = extraData[0];
			loadout.sigmaLoadout.sigmaForm = extraData[0];
			loadout.sigmaLoadout.maverick1 = extraData[1];
			loadout.sigmaLoadout.maverick2 = extraData[2];
			loadout.sigmaLoadout.commandMode = extraData[3];

			if (sigmaForm == 2) {
				newChar = new Doppma(
					this, pos.x, pos.y, xDir,
					false, charNetId, ownedByLocalPlayer, isWarpIn: isWarpIn
				);
			} else if (sigmaForm == 1) {
				newChar = new NeoSigma(
					this, pos.x, pos.y, xDir,
					false, charNetId, ownedByLocalPlayer, isWarpIn: isWarpIn
				);
			} else {
				newChar = new CmdSigma(
					this, pos.x, pos.y, xDir,
					false, charNetId, ownedByLocalPlayer, isWarpIn: isWarpIn
				);
			}
		}
		// Buster Zero.
		else if (charNum == (int)CharIds.BusterZero) {
			newChar = new BusterZero(
				this, pos.x, pos.y, xDir,
				false, charNetId, ownedByLocalPlayer, isWarpIn: isWarpIn
			);
		}
		// Punchy Zero.
		else if (charNum == (int)CharIds.PunchyZero) {
			newChar = new PunchyZero(
				this, pos.x, pos.y, xDir,
				false, charNetId, ownedByLocalPlayer, isWarpIn: isWarpIn
			);
		}
		// Kaiser Sigma (Hypermode)
		else if  (charNum == (int)CharIds.KaiserSigma) {
			newChar = new KaiserSigma(
				this, pos.x, pos.y, xDir,
				false, charNetId, ownedByLocalPlayer, isWarpIn: isWarpIn
			);
		}
		// Raging Charge X.
		else if  (charNum == (int)CharIds.RagingChargeX) {
			newChar = new RagingChargeX(
				this, pos.x, pos.y, xDir,
				false, charNetId, ownedByLocalPlayer, isWarpIn: isWarpIn
			);
		}
		// Error out if invalid id.
		else {
			throw new Exception("Error: Non-valid char ID: " + charNum);
		}
		// Do this once char has spawned and is not null.
		if (isMainChar) {
			configureWeapons(newChar);
			character = newChar;
			lastCharacter = newChar;
		}
		// Hyper mode overrides (POST)
		if (Global.level.isHyperMatch() && ownedByLocalPlayer) {
			if (newChar is MegamanX mmx) {
				mmx.hasUltimateArmor = true;
			}
			if (newChar is Zero zero) {
				if (loadout.zeroLoadout.hyperMode == 0) {
					zero.isBlack = true;
				} else if (loadout.zeroLoadout.hyperMode == 1) {
					zero.awakenedPhase = 1;
				} else {
					zero.isViral = true;
				}
			}
			if (newChar is Axl axl) {
				if (loadout.axlLoadout.hyperMode == 0) {
					axl.whiteAxlTime = 100000;
					axl.hyperAxlUsed = true;
					var db = new DoubleBullet();
					weapons[0] = db;
				} else {
					axl.stingChargeTime = 8;
					axl.hyperAxlUsed = true;
					currency = 9999;
				}
			}
		}
		if (isAI) {
			newChar.addAI();
		}
		if (newChar.rideArmor != null) {
			newChar.rideArmor.xDir = xDir;
		}
		if (isMainChar) {
			if (isMainPlayer && isCamPlayer) {
				Global.level.snapCamPos(newChar.getCamCenterPos(), null);
				//console.log(Global.level.camX + "," + Global.level.camY);
			}
			warpedIn = true;
		}
		return newChar;
	}

	public void startPossess(Player possesser, bool sendRpc = false) {
		possessedTime = maxPossessedTime;
		this.possesser = possesser;
		if (sendRpc) {
			RPC.possess.sendRpc(possesser.id, id, false);
		}
	}

	public void possesseeUpdate() {
		if (Global.isOnFrameCycle(60) && character != null) {
			character.damageHistory.Add(new DamageEvent(possesser, 136, (int)ProjIds.Sigma2ViralPossess, true, Global.time));
		}

		float myMashValue = mashValue();
		possessedTime -= myMashValue;
		if (possessedTime < 0) {
			possessedTime = 0;
			RPC.possess.sendRpc(0, id, true);
		}
	}

	public void possesserUpdate() {
		if (character == null || character.destroyed) return;

		// Held section
		input.possessedControlHeld[Control.Left] = Global.input.isHeld(Control.Left, Global.level.mainPlayer);
		input.possessedControlHeld[Control.Right] = Global.input.isHeld(Control.Right, Global.level.mainPlayer);
		input.possessedControlHeld[Control.Up] = Global.input.isHeld(Control.Up, Global.level.mainPlayer);
		input.possessedControlHeld[Control.Down] = Global.input.isHeld(Control.Down, Global.level.mainPlayer);
		input.possessedControlHeld[Control.Jump] = Global.input.isHeld(Control.Jump, Global.level.mainPlayer);
		input.possessedControlHeld[Control.Dash] = Global.input.isHeld(Control.Dash, Global.level.mainPlayer);
		input.possessedControlHeld[Control.Taunt] = Global.input.isHeld(Control.Taunt, Global.level.mainPlayer);

		byte inputHeldByte = Helpers.boolArrayToByte(new bool[] {
				input.possessedControlHeld[Control.Left],
				input.possessedControlHeld[Control.Right],
				input.possessedControlHeld[Control.Up],
				input.possessedControlHeld[Control.Down],
				input.possessedControlHeld[Control.Jump],
				input.possessedControlHeld[Control.Dash],
				input.possessedControlHeld[Control.Taunt],
				false,
		});

		// Pressed section
		input.possessedControlPressed[Control.Left] = Global.input.isPressed(Control.Left, Global.level.mainPlayer);
		input.possessedControlPressed[Control.Right] = Global.input.isPressed(Control.Right, Global.level.mainPlayer);
		input.possessedControlPressed[Control.Up] = Global.input.isPressed(Control.Up, Global.level.mainPlayer);
		input.possessedControlPressed[Control.Down] = Global.input.isPressed(Control.Down, Global.level.mainPlayer);
		input.possessedControlPressed[Control.Jump] = Global.input.isPressed(Control.Jump, Global.level.mainPlayer);
		input.possessedControlPressed[Control.Dash] = Global.input.isPressed(Control.Dash, Global.level.mainPlayer);
		input.possessedControlPressed[Control.Taunt] = Global.input.isPressed(Control.Taunt, Global.level.mainPlayer);

		byte inputPressedByte = Helpers.boolArrayToByte(new bool[] {
				input.possessedControlPressed[Control.Left],
				input.possessedControlPressed[Control.Right],
				input.possessedControlPressed[Control.Up],
				input.possessedControlPressed[Control.Down],
				input.possessedControlPressed[Control.Jump],
				input.possessedControlPressed[Control.Dash],
				input.possessedControlPressed[Control.Taunt],
				false,
		});

		RPC.syncPossessInput.sendRpc(id, inputHeldByte, inputPressedByte);
	}

	public void unpossess(bool sendRpc = false) {
		possessedTime = 0;
		possesser = null;
		input.possessedControlHeld.Clear();
		input.possessedControlPressed.Clear();
		if (sendRpc) {
			RPC.possess.sendRpc(0, id, true);
		}
	}

	public bool isPossessed() {
		return possessedTime > 0;
	}

	public bool canBePossessed() {
		if (character == null || character.destroyed) return false;
		if (character.isStatusImmune()) return false;
		if (character.flag != null) return false;
		if (possessedTime > 0) return false;
		return true;
	}

	public void transformAxlNet(RPCAxlDisguiseJson data) {
		disguise = new Disguise(data.targetName);
		charNum = data.charNum;

		// We temporally change loadout to populate it.
		LoadoutData oldLoadout = loadout;
		// If somehow even after all the safeguards fail we use the default one.
		if (data.loadout == null) {
			data.loadout = new();
		}

		loadout = data.loadout;
		if (charNum == (int)CharIds.X) {
			loadout.xLoadout.weapon1 = data.extraData[0];
			loadout.xLoadout.weapon2 = data.extraData[1];
			loadout.xLoadout.weapon3 = data.extraData[2];
			loadout.xLoadout.melee = data.extraData[3];
		}
		if (charNum == (int)CharIds.Axl) {
			loadout.axlLoadout.weapon2 = data.extraData[0];
			loadout.xLoadout.weapon3 = data.extraData[1];
		}

		// Change character.
		Character? retChar = null;
		if (data.charNum == (int)CharIds.X) {
			retChar = new MegamanX(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, false, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.Zero) {
			retChar = new Zero(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, false, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.Vile) {
			retChar = new Vile(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, false, isWarpIn: false,
				mk2VileOverride: data.extraData[0] == 1, mk5VileOverride: data.extraData[0] == 2
			);
		} else if (data.charNum == (int)CharIds.Axl) {
			retChar = new Axl(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, false, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.Sigma) {
			SigmaLoadout sigmaLoadout = new() {
				sigmaForm = data.extraData[0],
				maverick1 = data.extraData[1],
				maverick2 = data.extraData[2]
			};

			if (data.extraData[0] == 2) {
				retChar = new Doppma(
					this, character.pos.x, character.pos.y, character.xDir,
					true, data.dnaNetId, false, isWarpIn: false, sigmaLoadout: sigmaLoadout
				);
			} else if (data.extraData[0] == 1) {
				retChar = new NeoSigma(
					this, character.pos.x, character.pos.y, character.xDir,
					true, data.dnaNetId, false, isWarpIn: false, sigmaLoadout: sigmaLoadout
				);
			} else {
				retChar = new CmdSigma(
					this, character.pos.x, character.pos.y, character.xDir,
					true, data.dnaNetId, false, isWarpIn: false, sigmaLoadout: sigmaLoadout
				);
			}
		} else if (data.charNum == (int)CharIds.Rock) {
			retChar = new Rock(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, false, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.BusterZero) {
			retChar = new BusterZero(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, false, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.PunchyZero) {
			retChar = new PunchyZero(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, false, isWarpIn: false
			);
		} else {
			throw new Exception("Error: Non-valid char ID: " + data.charNum);
		}

		// Status effects.
		retChar.burnTime = character.burnTime;
		retChar.acidTime = character.acidTime;
		retChar.oilTime = character.oilTime;
		retChar.igFreezeProgress = character.igFreezeProgress;
		retChar.virusTime = character.virusTime;

		// Hit cooldowns.
		retChar.projectileCooldown = character.projectileCooldown;
		retChar.flinchCooldown = character.flinchCooldown;

		// Change character.
		character.cleanupBeforeTransform();
		preTransformedAxl = character;
		Global.level.removeGameObject(preTransformedAxl);
		character = retChar;

		// Armor flags for X.
		if (data.charNum == (int)CharIds.X) {
			armorFlag = BitConverter.ToUInt16(
				new byte[] { data.extraData[0], data.extraData[1] }
			);
			if (retChar is MegamanX retX) {
				retX.hasUltimateArmor = (data.extraData[2] == 1);
			}
		}
		// Restore old loadout and save the transformed one.
		atransLoadout = loadout;
		loadout = oldLoadout;
	}

	public void transformAxl(DNACore dnaCore, ushort dnaNetId) {
		// Reload weapons at transform if not used before.
		if (!dnaCore.usedOnce && weapons != null) {
			foreach (var weapon in weapons) {
				if (!weapon.isCmWeapon()) {
					weapon.ammo = weapon.maxAmmo;
				}
			}
		}
		// Transform.
		disguise = new Disguise(dnaCore.name);
		charNum = dnaCore.charNum;

		bool isVileMK2 = charNum == 2 && dnaCore.hyperMode == DNACoreHyperMode.VileMK2;
		bool isVileMK5 = charNum == 2 && dnaCore.hyperMode == DNACoreHyperMode.VileMK5;

		// If somehow the DNA core loadout is null we copy current one.
		if (dnaCore.loadout == null) {
			dnaCore.loadout = loadout;
		}

		if (ownedByLocalPlayer) {
			byte[] extraData = getCharSpawnData(dnaCore.charNum);
			if (dnaCore.charNum == (int)CharIds.Vile) {
				extraData = new byte[1];
				if (isVileMK2) {
					extraData[0] = 1;
				}
				if (isVileMK5) {
					extraData[0] = 2;
				}
			}
			string json = JsonConvert.SerializeObject(
				new RPCAxlDisguiseJson(
					id, disguise.targetName, dnaCore.charNum,
					dnaCore.loadout, dnaNetId, extraData
				)
			);
			Global.serverClient?.rpc(RPC.axlDisguise, json);
		}
		LoadoutData oldLoadout = loadout;
		loadout = dnaCore.loadout;

		Character? retChar = null;
		if (charNum == (int)CharIds.X) {
			retChar = new MegamanX(
				this, character.pos.x, character.pos.y, character.xDir,
				true, dnaNetId, true, isWarpIn: false
			);
		} else if (charNum == (int)CharIds.Zero) {
			retChar = new Zero(
				this, character.pos.x, character.pos.y, character.xDir,
				true, dnaNetId, true, isWarpIn: false
			);
		} else if (charNum == (int)CharIds.Vile) {
			retChar = new Vile(
				this, character.pos.x, character.pos.y, character.xDir,
				true, dnaNetId, true, isWarpIn: false,
				mk2VileOverride: isVileMK2, mk5VileOverride: isVileMK5
			) {
				hasFrozenCastle = dnaCore.frozenCastle,
				hasSpeedDevil = dnaCore.speedDevil
			};
		} else if (charNum == (int)CharIds.Axl) {
			retChar = new Axl(
				this, character.pos.x, character.pos.y, character.xDir,
				true, dnaNetId, true, isWarpIn: false
			);
		} else if (charNum == (int)CharIds.Sigma) {
			if (dnaCore.loadout.sigmaLoadout.sigmaForm == 2) {
				retChar = new Doppma(
					this, character.pos.x, character.pos.y, character.xDir,
					true, dnaNetId, true, isWarpIn: false
				);
			} else if (dnaCore.loadout.sigmaLoadout.sigmaForm == 1) {
				retChar = new NeoSigma(
					this, character.pos.x, character.pos.y, character.xDir,
					true, dnaNetId, true, isWarpIn: false
				);
			} else {
				retChar = new CmdSigma(
					this, character.pos.x, character.pos.y, character.xDir,
					true, dnaNetId, true, isWarpIn: false
				);
			}
		} else if (charNum == (int)CharIds.Rock) {
			retChar = new Rock(
				this, character.pos.x, character.pos.y, character.xDir,
				true, dnaNetId, true, isWarpIn: false
			);
		} else if (charNum == (int)CharIds.BusterZero) {
			retChar = new BusterZero(
				this, character.pos.x, character.pos.y, character.xDir,
				true, dnaNetId, true, isWarpIn: false
			);
		} else if (charNum == (int)CharIds.PunchyZero) {
			retChar = new PunchyZero(
				this, character.pos.x, character.pos.y, character.xDir,
				true, dnaNetId, true, isWarpIn: false
			);
		} else if  (charNum == (int)CharIds.KaiserSigma) {
			retChar = new KaiserSigma(
				this, character.pos.x, character.pos.y, character.xDir,
				false, charNetId, ownedByLocalPlayer, isRevive: false
			);
		} else {
			throw new Exception("Error: Non-valid char ID: " + charNum);
		}
		if (retChar is Vile vile) {
			if (isVileMK5) vile.vileForm = 2;
			else if (isVileMK2) vile.vileForm = 1;
		}
		retChar.addTransformAnim();

		// Weapon configuration.
		oldWeapons = weapons;

		if (charNum == (int)CharIds.Zero) {
			retChar.weapons.Add(new ZSaber());
		}
		if (charNum == (int)CharIds.BusterZero) {
			retChar.weapons.Add(new ZeroBuster());
		}
		if (charNum == (int)CharIds.PunchyZero) {
			retChar.weapons.Add(new KKnuckleWeapon());
		}
		if (charNum == (int)CharIds.KaiserSigma) {
			retChar.weapons.Add(new SigmaMenuWeapon());
		}
		retChar.weapons.Add(new AssassinBulletChar());
		retChar.weapons.Add(new UndisguiseWeapon());

		if (isAI) {
			retChar.addAI();
		}

		retChar.xDir = character.xDir;
		//retChar.heal(maxHealth);

		// Speed and state.
		if (retChar.charId != CharIds.KaiserSigma) {
			if (character.charState.canStopJump && !character.charState.stoppedJump) {
				retChar.changeState(retChar.getJumpState(), true);
			} else {
				retChar.changeToIdleOrFall();
			}
			retChar.vel = character.vel;
			retChar.slideVel = character.slideVel;
			retChar.xFlinchPushVel = character.xFlinchPushVel;
			retChar.xIceVel = character.xIceVel;
		}
		retChar.health = character.health;
		retChar.maxHealth = character.maxHealth;
		retChar.healAmount = character.healAmount;

		// Status effects.
		retChar.burnTime = character.burnTime;
		retChar.acidTime = character.acidTime;
		retChar.oilTime = character.oilTime;
		retChar.igFreezeProgress = character.igFreezeProgress;
		retChar.virusTime = character.virusTime;

		// Hit cooldowns.
		retChar.projectileCooldown = character.projectileCooldown;
		retChar.flinchCooldown = character.flinchCooldown;

		character.cleanupBeforeTransform();
		character = retChar;
		if (weapon != null) {
			weapon.shootCooldown = 0.25f;
		}

		if (character is Zero zero) {
			zero.gigaAttack.ammo = dnaCore.rakuhouhaAmmo;

			if (dnaCore.hyperMode == DNACoreHyperMode.BlackZero) {
				zero.isBlack = true;
				zero.hyperMode = 0;
			} else if (dnaCore.hyperMode == DNACoreHyperMode.AwakenedZero) {
				zero.awakenedPhase = 1;
				zero.hyperMode = 1;
			} else if (dnaCore.hyperMode == DNACoreHyperMode.NightmareZero) {
				zero.isViral = true;
				zero.hyperMode = 2;
			}
		} else if (charNum == (int)CharIds.Axl && character is Axl axl) {
			if (dnaCore.hyperMode == DNACoreHyperMode.WhiteAxl) {
				axl.whiteAxlTime = axl.maxHyperAxlTime;
			}
			axl.axlSwapTime = 0.25f;
		}
		else if (character is CmdSigma sigma) {
			sigma.ballWeapon.ammo = dnaCore.rakuhouhaAmmo;
		}
		else if (character is NeoSigma neoSigma) {
			neoSigma.gigaAttack.ammo = dnaCore.rakuhouhaAmmo;
		}
		dnaCore.ultimateArmor = false;
		dnaCore.usedOnce = true;
		dnaCore.hyperMode = DNACoreHyperMode.None;

		// Restore old loadout and save the transformed one.
		atransLoadout = loadout;
		loadout = oldLoadout;
	}

	// If you change this method change revertToAxlDeath() too
	public void revertToAxl(ushort? backupNetId = null) {
		disguise = null;
		atransLoadout = null;
		Character oldChar = character;

		if (ownedByLocalPlayer) {
			string json = JsonConvert.SerializeObject(
				new RPCAxlDisguiseJson(id, "", -1, loadout, preTransformedAxl.netId.Value)
			);
			Global.serverClient?.rpc(RPC.axlDisguise, json);

			if (character is Zero zero) {
				lastDNACore.rakuhouhaAmmo = zero.gigaAttack.ammo;
			} else if (character is PunchyZero pzero) {
				lastDNACore.rakuhouhaAmmo = pzero.gigaAttack.ammo;
			} else if (character is CmdSigma sigma) {
				lastDNACore.rakuhouhaAmmo = sigma.ballWeapon.ammo;
			} else if (character is NeoSigma neoSigma) {
				lastDNACore.rakuhouhaAmmo = neoSigma.gigaAttack.ammo;
			}
		}
		var oldPos = character.pos;
		var oldDir = character.xDir;
		character.destroySelf();
		// For if joined late and not locally owned.
		if (!ownedByLocalPlayer && preTransformedAxl == null) {
			if (backupNetId == null) {
				throw new Exception("Error: Missing NetID on Axl trasform RPC.");
			}
			preTransformedAxl = new Axl(
				this, oldPos.x, oldPos.y, oldDir, true, backupNetId, ownedByLocalPlayer, false
			);
		} else {
			Global.level.addGameObject(preTransformedAxl);
		}
		character = preTransformedAxl;
		character.addTransformAnim();
		preTransformedAxl = null;
		charNum = 3;
		character.pos = oldPos;
		character.xDir = oldDir;
		maxHealth = getMaxHealth();
		health = Math.Min(health, maxHealth);

		if (ownedByLocalPlayer) {
			character.weaponSlot = 0;
			lastDNACore = null;
			lastDNACoreIndex = 4;

			character.weapons.AddRange(oldChar.weapons.Where(
				(Weapon w) => w is MaverickWeapon { summonedOnce: true } && !character.weapons.Contains(w)
			));

			character.grounded = oldChar.grounded;
			if (oldChar.charState.canStopJump && !oldChar.grounded) {
				character.changeState(character.getJumpState(), true);
			} else {
				character.changeToIdleOrFall();
			}
			// Speed
			character.vel = oldChar.vel;
			character.slideVel = oldChar.slideVel;
			character.xFlinchPushVel = oldChar.xFlinchPushVel;
			character.xIceVel = oldChar.xIceVel;

			// Status effects.
			character.burnTime = oldChar.burnTime;
			character.acidTime = oldChar.acidTime;
			character.oilTime = oldChar.oilTime;
			character.igFreezeProgress = oldChar.igFreezeProgress;
			character.virusTime = oldChar.virusTime;

			// Hit cooldowns.
			character.projectileCooldown = oldChar.projectileCooldown;
			character.flinchCooldown = oldChar.flinchCooldown;
		}
	}

	// If you change this method change revertToAxl() too
	public void revertToAxlDeath() {
		disguise = null;
		atransLoadout = null;
		preTransformedAxl = null;
		charNum = (int)CharIds.Axl;

		if (ownedByLocalPlayer) {
			string json = JsonConvert.SerializeObject(
				new RPCAxlDisguiseJson(id, "", -2, loadout, character.netId.Value)
			);
			Global.serverClient?.rpc(RPC.axlDisguise, json);

			if (character is Zero zero) {
				lastDNACore.rakuhouhaAmmo = zero.gigaAttack.ammo;
			} else if (character is PunchyZero pzero) {
				lastDNACore.rakuhouhaAmmo = pzero.gigaAttack.ammo;
			} else if (character is CmdSigma sigma) {
				lastDNACore.rakuhouhaAmmo = sigma.ballWeapon.ammo;
			} else if (character is NeoSigma neoSigma) {
				lastDNACore.rakuhouhaAmmo = neoSigma.gigaAttack.ammo;
			}
		}
		if (character == null) {
			return;
		}
		if (ownedByLocalPlayer) {
			maxHealth = getMaxHealth();
			health = 0;
			configureWeapons(character);
			character.weaponSlot = 0;
			lastDNACore = null;
			lastDNACoreIndex = 4;
		}
		character.addTransformAnim();
	}

	public bool isMainPlayer {
		get { return Global.level.mainPlayer == this; }
	}

	public bool isCamPlayer {
		get { return this == Global.level.camPlayer; }
	}

	public bool hasArmor() {
		return bodyArmorNum > 0 || legArmorNum > 0 || armArmorNum > 0 || helmetArmorNum > 0;
	}

	public bool hasArmor(int version) {
		return bodyArmorNum == version || legArmorNum == version || armArmorNum == version || helmetArmorNum == version;
	}

	public bool hasAllArmor() {
		return bodyArmorNum > 0 && legArmorNum > 0 && armArmorNum > 0 && helmetArmorNum > 0;
	}

	public bool hasAllX3Armor() {
		return bodyArmorNum >= 3 && legArmorNum >= 3 && armArmorNum >= 3 && helmetArmorNum >= 3;
	}

	public void destroy() {
		character?.destroySelf();
		character = null;
		removeOwnedActors();
	}

	public void removeOwnedActors() {
		foreach (var go in Global.level.gameObjects) {
			if (go is Actor actor && actor.netOwner == this && actor.cleanUpOnPlayerLeave()) {
				actor.destroySelf();
			}
		}
	}

	public void removeOwnedGrenades() {
		for (int i = grenades.Count - 1; i >= 0; i--) {
			grenades[i].destroySelf();
		}
		grenades.Clear();
	}

	public void removeOwnedIceStatues() {
		for (int i = iceStatues.Count - 1; i >= 0; i--) {
			iceStatues[i].destroySelf();
		}
		iceStatues.Clear();
	}

	public void removeOwnedSeeds() {
		for (int i = seeds.Count - 1; i >= 0; i--) {
			seeds[i].destroySelf();
		}
		seeds.Clear();
	}

	public void removeOwnedMechaniloids() {
		for (int i = mechaniloids.Count - 1; i >= 0; i--) {
			mechaniloids[i].destroySelf();
		}
	}

	public int tankMechaniloidCount() {
		return mechaniloids.Count(m => m is Mechaniloid ml && ml.type == MechaniloidType.Tank);
	}

	public int hopperMechaniloidCount() {
		return mechaniloids.Count(m => m is Mechaniloid ml && ml.type == MechaniloidType.Hopper);
	}

	public int birdMechaniloidCount() {
		return mechaniloids.Count(m => m is BirdMechaniloidProj);
	}

	public int fishMechaniloidCount() {
		return mechaniloids.Count(m => m is Mechaniloid ml && ml.type == MechaniloidType.Fish);
	}

	public bool canControl {
		get {
			if (Global.level.gameMode.isOver) {
				return false;
			}
			if (!isAI && Menu.inChat) {
				return false;
			}
			if (!isAI && Menu.inMenu) {
				return false;
			}
			if (character != null && currentMaverick == null) {
				InRideArmor inRideArmor = character.charState as InRideArmor;
				if (inRideArmor != null &&
					(inRideArmor.frozenTime > 0 || inRideArmor.stunTime > 0 || inRideArmor.crystalizeTime > 0)
				) {
					return false;
				}
				if (character.charState is GenericStun) {
					return false;
				}
				if (character.charState is SniperAimAxl) {
					return false;
				}
				if (character.rideArmor?.rideArmorState is RADropIn) {
					return false;
				}
			}
			if (character is BaseSigma sigma && sigma.tagTeamSwapProgress > 0) {
				return false;
			}
			if (isPossessed()) {
				return false;
			}
			/*
			if (character?.charState?.isGrabbedState == true)
			{
				return false;
			}
			*/
			return true;
		}
	}

	public void awardCurrency() {
		// Cannot gain scrap or ST.
		if (Global.level.is1v1()) return;
		if (axlBulletType == (int)AxlBulletWeaponType.AncientGun && isAxl) return;

		// First we fill ST.
		if (Global.level?.server?.customMatchSettings != null) {
			fillSubtank(Global.level.server.customMatchSettings.SubtankGain);
		} else {
			if (isVile) {
				fillSubtank(2);
			} else if (isAxl) {
				fillSubtank(3);
			} else {
				fillSubtank(4);
			}
		}
		if (character is Zero zero && zero.isViral) {
			zero.freeBusterShots++;
			return;
		}
		if (character is PunchyZero pzero && pzero.isViral) {
			pzero.freeBusterShots++;
			return;
		}
		// Check for stuff that cannot gain scraps.
		if (character is RagingChargeX or KaiserSigma or ViralSigma or WolfSigma) return;
		if (character?.rideArmor?.raNum == 4 && character.charState is InRideArmor) return;
		if (character is MegamanX mmx && mmx.hasUltimateArmor) {
			return;
		}
		
		if (Global.level?.server?.customMatchSettings != null) {
			currency += Global.level.server.customMatchSettings.currencyGain;
		} else currency++;
	}

	public int getStartCurrency() {
		if (Global.level.levelData.isTraining() || Global.anyQuickStart) {
			return 999;
		}
		if (Global.level?.server?.customMatchSettings != null) {
			return Global.level.server.customMatchSettings.startCurrency;
		}
		return 3;
	}

	public int getRespawnTime() {
		if (Global.level.isTraining() || Global.level.isRace()) {
			return 2;
		}
		if (Global.level?.server?.customMatchSettings != null) {
			return Global.level.server.customMatchSettings.respawnTime;
		} else {
			if (Global.level?.gameMode is ControlPoints && alliance == GameMode.redAlliance) {
				return 7;
			}
			if (Global.level?.gameMode is KingOfTheHill) {
				return 7;
			}
		}
		return 5;
	}

	public bool canReviveVile() {
		if (Global.level.isElimination() ||
			!lastDeathCanRevive ||
			newCharNum != (int)CharIds.Vile ||
			currency < reviveVileCost
		) {
			return false;
		}
		if (character?.charState is not Die) {
			return false;
		}
		if (character is Vile vile2 && vile2.isVileMK5) {
			return false;
		}
		if (character is not Vile vile || vile.summonedGoliath) {
			return false;
		}
		return true;
	}

	public bool canReviveSigma(out Point spawnPoint, int sigmaHypermode) {
		spawnPoint = Point.zero;
		if (character?.charState is not Die) {
			return false;
		}
		spawnPoint = character.pos;
		if (Global.level.isHyper1v1() &&
			!lastDeathWasSigmaHyper &&
			character is BaseSigma &&
			newCharNum == (int)CharIds.Sigma
		) {
			return true;
		}
		if (character == null ||
			!lastDeathCanRevive ||
			!isSigma ||
			newCharNum != (int)CharIds.Sigma ||
			currency < reviveSigmaCost ||
			lastDeathWasSigmaHyper
		) {
			return false;
		}

		if (sigmaHypermode == 0) {
			Point deathPos = character.pos;

			// Get ground snapping pos
			var rect = new Rect(deathPos.addxy(-7, 0), deathPos.addxy(7, 112));
			var hits = Global.level.checkCollisionsShape(rect.getShape(), null);
			Point? closestHitPoint = Helpers.getClosestHitPoint(hits, deathPos, typeof(Wall));
			if (closestHitPoint != null) {
				deathPos = new Point(deathPos.x, closestHitPoint.Value.y);
			}

			// Check if ample space to revive in
			int w = 10;
			int h = 120;
			rect = new Rect(
				new Point(deathPos.x - w / 2, deathPos.y - h),
				new Point(deathPos.x + w / 2, deathPos.y - 25)
			);
			hits = Global.level.checkCollisionsShape(rect.getShape(), null);
			if (hits.Any(h => h.gameObject is Wall)) {
				return false;
			}

			if (deathPos.x - 100 < 0 || deathPos.x + 100 > Global.level.width) {
				return false;
			}
			foreach (var player in Global.level.players) {
				if (player.character is WolfSigma &&
					player.character.pos.distanceTo(deathPos) < Global.screenW
				) {
					return false;
				}
			}
		} else if (sigmaHypermode == 2) {
			return character != null && KaiserSigma.canKaiserSpawn(character, out spawnPoint);
		}
		return true;
	}

	public bool canReviveX() {
		return (
			character is MegamanX mmx &&
			!mmx.hasAnyArmor &&
			mmx.charState is Die &&
			lastDeathCanRevive &&
			newCharNum == 0 &&
			currency >= reviveXCost
		);
	}

	public void reviveVile(bool toMK5) {
		currency -= reviveVileCost;
		if (character is not Vile vile) {
			return;
		}

		if (toMK5) {
			vileFormToRespawnAs = 2;
		} else if (vile.vileForm == 0) {
			vileFormToRespawnAs = 1;
		} else if (vile.vileForm == 1) {
			vileFormToRespawnAs = 2;
		}

		respawnTime = 0;
		vile.alreadySummonedNewMech = false;
		character.visible = true;
		if (explodeDieEffect != null) {
			explodeDieEffect.destroySelf();
			explodeDieEffect = null;
		}
		vile.rideMenuWeapon = new MechMenuWeapon(VileMechMenuType.All);
		character.changeState(new VileRevive(vileFormToRespawnAs == 2), true);
		RPC.playerToggle.sendRpc(id, vileFormToRespawnAs == 2 ? RPCToggleType.ReviveVileTo5 : RPCToggleType.ReviveVileTo2);
	}

	public void reviveVileNonOwner(bool toMK5) {
		if (character is not Vile vile) {
			return;
		}
		if (toMK5) {
			vile.vileForm = 2;
		} else {
			vile.vileForm = 1;
		}
	}

	public void reviveSigma(int form, Point spawnPoint) {
		currency -= reviveSigmaCost;
		hyperSigmaRespawn = true;
		respawnTime = 0;
		if (character is not BaseSigma sigma) {
			return;
		}
		if (character?.destroyed == false) {
			destroyCharacter(true);
		}
		explodeDieEnd();
		ushort newNetId = getNextATransNetId();
		if (form == 0) {
			WolfSigma wolfSigma = new WolfSigma(
				this, spawnPoint.x, spawnPoint.y, sigma.xDir, true,
				newNetId, true
			);
			character = wolfSigma;
		} else if (form == 1) {
			ViralSigma viralSigma = new ViralSigma(
				this, spawnPoint.x, spawnPoint.y, sigma.xDir, true,
				newNetId, true
			);
			character = viralSigma;
		} else {
			KaiserSigma kaiserSigma = new KaiserSigma(
				this, spawnPoint.x, spawnPoint.y, sigma.xDir, true,
				newNetId, true
			);
			character = kaiserSigma;
		}
		RPC.reviveSigma.sendRpc(form, spawnPoint, id, newNetId);
	}

	public void reviveSigmaNonOwner(int form, Point spawnPoint, ushort sigmaNetId) {
		if (form >= 2) {
			KaiserSigma kaiserSigma = new KaiserSigma(
				this, spawnPoint.x, spawnPoint.y, character?.xDir ?? 1, true,
				sigmaNetId, false
			);
			character = kaiserSigma;

			character.changeSprite("kaisersigma_enter", true);
		}
	}

	public void reviveX() {
		if (character == null) {
			return;
		}
		currency -= reviveXCost;
		respawnTime = 0;
		// Save old char to variable and nuke him.
		Character oldChar = character;
		destroyCharacter(oldChar, true);
		// Spawn RCX in the old position in the same frame.
		character = spawnCharAtPoint(
			(int)CharIds.RagingChargeX, [],
			oldChar.pos, oldChar.xDir,
			getNextATransNetId(), true,
			forceSpawn: true
		) ?? throw new Exception("Error spawning RCX.");
		// Set the inital state.
		character.health = 0;
		character.changeState(new XRevive(), true);
	}

	public void explodeDieStart() {
		if (character == null) {
			return;
		}
		explodeDieEffect = ExplodeDieEffect.createFromActor(this, character, 20, 1.5f, false, doExplosion: false);
		Global.level.addEffect(explodeDieEffect);
	}

	public void explodeDieEnd() {
		explodeDieEffect = null;
	}

	public void destroyCharacter(bool sendRpc = false) {
		if (character == null) {
			return;
		}
		character.destroySelf();
		onCharacterDeath();
		if (sendRpc) {
			RPC.destroyCharacter.sendRpc(this, character);
		}
		character = null;
	}

	public void destroyCharacter(Character? targetChar, bool sendRpc = false) {
		if (targetChar == null) {
			return;
		}
		targetChar.destroySelf();
		if (sendRpc) {
			RPC.destroyCharacter.sendRpc(this, targetChar);
		}
		if (character == targetChar) {
			onCharacterDeath();
			character = null;
		}
	}

	public bool destroyCharacter(ushort netId) {
		if (character?.netId != netId) {
			return false;
		}
		character.destroySelf();
		onCharacterDeath();
		character = null;

		return true;
	}

	// Must be called on any character death
	public void onCharacterDeath() {
		delayedNewCharNum = null;
		suicided = false;
		unpossess();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (delayedNewCharNum != null &&
			Global.level.mainPlayer.charNum != delayedNewCharNum.Value
		) {
			Global.level.mainPlayer.newCharNum = delayedNewCharNum.Value;
			Global.serverClient?.rpc(
				RPC.switchCharacter, (byte)Global.level.mainPlayer.id, (byte)delayedNewCharNum.Value
			);
		}
		if (character == null) {
			return;
		}
		foreach (Weapon weapon in character.weapons) {
			if (weapon is MaverickWeapon mw && mw.maverick != null) {
				mw.maverick.changeState(new MExit(mw.maverick.pos, true), true);
			}
		}
	}

	public void maverick1v1Kill() {
		character?.applyDamage(Damager.forceKillDamage, null, null, null, null);
		character?.destroySelf();
		character = null;
		respawnTime = getRespawnTime() * (suicided ? 2 : 1);
		suicided = false;
		randomTip = Tips.getRandomTip(charNum);
		maverick1v1Spawned = false;
	}

	public void forceKill() {
		if (maverick1v1 != null && Global.level.is1v1()) {
			//character?.applyDamage(null, null, 1000, null);
			currentMaverick?.applyDamage(Damager.forceKillDamage, this, character, null, null);
			return;
		}

		if (currentMaverick != null && currentMaverick.controlMode == MaverickMode.TagTeam) {
			destroyCharacter(true);
		} else {
			character?.applyDamage(Damager.forceKillDamage, this, character, null, null);
		}
		foreach (var maverick in mavericks) {
			maverick.applyDamage(Damager.forceKillDamage, this, character, null, null);
		}
	}

	public bool isGridModeEnabled() {
		if (isAxl || isDisguisedAxl) {
			if (Options.main.useMouseAim) return false;
			if (Global.level.is1v1()) return Options.main.gridModeAxl > 0;
			return Options.main.gridModeAxl > 1;
		} else if (isX) {
			if (Global.level.is1v1()) return Options.main.gridModeX > 0;
			return Options.main.gridModeX > 1;
		}

		return false;
	}

	public Point[] gridModePoints() {
		if (weapons.Count < 2) return null;
		if (weapons.Count == 2) return new Point[] { new Point(0, 0), new Point(-1, 0) };
		if (weapons.Count == 3) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0) };
		if (weapons.Count == 4) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1) };
		if (weapons.Count == 5) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1) };
		if (weapons.Count == 6) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1), new Point(-1, -1) };
		if (weapons.Count == 7) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1), new Point(-1, -1), new Point(1, -1) };
		if (weapons.Count == 8) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1), new Point(-1, -1), new Point(1, -1), new Point(-1, 1) };
		if (weapons.Count == 9) return new Point[] { new Point(0, 0), new Point(-1, -1), new Point(0, -1), new Point(1, -1), new Point(-1, 0), new Point(1, 0), new Point(-1, 1), new Point(0, 1), new Point(1, 1) };
		if (weapons.Count >= 10) return new Point[] { new Point(0, 0), new Point(-1, -1), new Point(0, -1), new Point(1, -1), new Point(-1, 0), new Point(1, 0), new Point(-1, 1), new Point(0, 1), new Point(1, 1), new Point(2, 1) };
		return null;
	}

	// 0000 0000 0000 0000 [boots][body][helmet][arm]
	// 0000 = none, 0001 = x1, 0010 = x2, 0011 = x3, 1111 = chip
	public static int getArmorNum(int armorFlag, int armorIndex, bool isChipCheck) {
		List<string> bits = Convert.ToString(armorFlag, 2).Select(s => s.ToString()).ToList();
		while (bits.Count < 16) {
			bits.Insert(0, "0");
		}

		string bitStr = "";
		if (armorIndex == 0) bitStr = bits[0] + bits[1] + bits[2] + bits[3];
		if (armorIndex == 1) bitStr = bits[4] + bits[5] + bits[6] + bits[7];
		if (armorIndex == 2) bitStr = bits[8] + bits[9] + bits[10] + bits[11];
		if (armorIndex == 3) bitStr = bits[12] + bits[13] + bits[14] + bits[15];

		int retVal = Convert.ToInt32(bitStr, 2);
		if (retVal > 3 && !isChipCheck) retVal = 3;
		return retVal;
	}

	public void setArmorNum(int armorIndex, int val) {
		List<string> bits = Convert.ToString(armorFlag, 2).Select(s => s.ToString()).ToList();
		while (bits.Count < 16) {
			bits.Insert(0, "0");
		}

		List<string> valBits = Convert.ToString(val, 2).Select(s => s.ToString()).ToList();
		while (valBits.Count < 4) {
			valBits.Insert(0, "0");
		}

		int i = armorIndex * 4;
		bits[i] = valBits[0];
		bits[i + 1] = valBits[1];
		bits[i + 2] = valBits[2];
		bits[i + 3] = valBits[3];

		armorFlag = Convert.ToUInt16(string.Join("", bits), 2);
	}

	public int legArmorNum {
		get { return getArmorNum(armorFlag, 0, false); }
		set { setArmorNum(0, value); }
	}
	public int bodyArmorNum {
		get { return getArmorNum(armorFlag, 1, false); }
		set { setArmorNum(1, value); }
	}
	public int helmetArmorNum {
		get { return getArmorNum(armorFlag, 2, false); }
		set { setArmorNum(2, value); }
	}
	public int armArmorNum {
		get { return getArmorNum(armorFlag, 3, false); }
		set { setArmorNum(3, value); }
	}

	public bool hasHelmetArmor(ArmorId armorId) { return helmetArmorNum == (int)armorId; }
	public bool hasArmArmor(ArmorId armorId) { return armArmorNum == (int)armorId; }
	public bool hasBootsArmor(int xGame) { return legArmorNum == xGame; }
	public bool hasBodyArmor(int xGame) { return bodyArmorNum == xGame; }
	public bool hasHelmetArmor(int xGame) { return helmetArmorNum == xGame; }
	public bool hasArmArmor(int xGame) { return armArmorNum == xGame; }

	public bool isHeadArmorPurchased(int xGame) { return headArmorsPurchased[xGame - 1]; }
	public bool isBodyArmorPurchased(int xGame) { return bodyArmorsPurchased[xGame - 1]; }
	public bool isArmArmorPurchased(int xGame) { return armArmorsPurchased[xGame - 1]; }
	public bool isBootsArmorPurchased(int xGame) { return bootsArmorsPurchased[xGame - 1]; }

	public void setHeadArmorPurchased(int xGame) { headArmorsPurchased[xGame - 1] = true; }
	public void setBodyArmorPurchased(int xGame) { bodyArmorsPurchased[xGame - 1] = true; }
	public void setArmArmorPurchased(int xGame) { armArmorsPurchased[xGame - 1] = true; }
	public void setBootsArmorPurchased(int xGame) { bootsArmorsPurchased[xGame - 1] = true; }

	public bool hasAllArmorsPurchased() {
		for (int i = 0; i < 3; i++) {
			if (!headArmorsPurchased[i]) return false;
			if (!bodyArmorsPurchased[i]) return false;
			if (!armArmorsPurchased[i]) return false;
			if (!bootsArmorsPurchased[i]) return false;
		}
		return true;
	}

	public bool hasAnyArmorPurchased() {
		for (int i = 0; i < 3; i++) {
			if (headArmorsPurchased[i]) return true;
			if (bodyArmorsPurchased[i]) return true;
			if (armArmorsPurchased[i]) return true;
			if (bootsArmorsPurchased[i]) return true;
		}
		return false;
	}

	public bool hasAnyArmor() {
		return legArmorNum > 0 || armArmorNum > 0 || bodyArmorNum > 0 || helmetArmorNum > 0;
	}

	public void press(string inputMapping) {
		string keyboard = "keyboard";
		int? control = Control.controllerNameToMapping[keyboard].GetValueOrDefault(inputMapping);
		if (control == null) return;
		Key key = (Key)control;
		input.keyPressed[key] = !input.keyHeld.GetValueOrDefault(key);
		input.keyHeld[key] = true;
	}

	public void release(string inputMapping) {
		string keyboard = "keyboard";
		int? control = Control.controllerNameToMapping[keyboard].GetValueOrDefault(inputMapping);
		if (control == null) return;
		Key key = (Key)control;
		input.keyHeld[key] = false;
		input.keyPressed[key] = false;
	}

	public void clearAiInput() {
		input.keyHeld.Clear();
		input.keyPressed.Clear();
		if (character != null && character.ai.framesChargeHeld > 0) {
			press("shoot");
		}
		if (character != null) {
			if (character.ai.jumpTime > 0) {
				press("jump");
			} else {
				release("jump");
			}
		}
	}

	public bool dashPressed(out string dashControl) {
		dashControl = "";
		if (input.isPressed(Control.Dash, this)) {
			dashControl = Control.Dash;
			return true;
		} else if (!Options.main.disableDoubleDash) {
			if (input.isPressed(Control.Left, this) && input.checkDoubleTap(Control.Left)) {
				dashControl = Control.Left;
				return true;
			} else if (input.isPressed(Control.Right, this) && input.checkDoubleTap(Control.Right)) {
				dashControl = Control.Right;
				return true;
			}
		}
		return false;
	}

	public void promoteToHost() {
		if (this == Global.level.mainPlayer) {
			Global.serverClient.isHost = true;
			if (Global.level?.gameMode != null) {
				Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry("You were promoted to host.", null, null, true));
			}
			if (Global.level?.redFlag != null) {
				Global.level.redFlag.takeOwnership();
				Global.level.redFlag.pedestal?.takeOwnership();
			}
			if (Global.level?.blueFlag != null) {
				Global.level.blueFlag.takeOwnership();
				Global.level.blueFlag.pedestal?.takeOwnership();
			}
			foreach (var cp in Global.level.controlPoints) {
				cp.takeOwnership();
			}
			Global.level?.hill?.takeOwnership();

			foreach (var player in Global.level.players) {
				if (player.serverPlayer.isBot) {
					player.ownedByLocalPlayer = true;
					player.isAI = true;
					player.character?.addAI();
					player.character?.takeOwnership();
				}
			}
		} else {
			Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(name + " promoted to host.", null, null, true));
		}
	}

	public void addKill() {
		if (Global.serverClient == null) {
			kills++;
		} else if (Global.canControlKillscore) {
			kills++;
			if (!charNumToKills.ContainsKey(realCharNum)) {
				charNumToKills[realCharNum] = 0;
			}
			charNumToKills[realCharNum]++;
			RPC.updatePlayer.sendRpc(id, kills, deaths);
		}
	}

	public void addAssist() {
		assists++;
	}

	public void addDeath() {
		if (isSigma && maverick1v1 == null && Global.level.isHyper1v1() && !lastDeathWasSigmaHyper) {
			return;
		}

		if (Global.serverClient == null) {
			deaths++;
		} else if (Global.canControlKillscore) {
			deaths++;
			RPC.updatePlayer.sendRpc(id, kills, deaths);
		}
	}

	public float mashValue() {
		int mashCount = input.mashCount;
		if (isAI && character?.ai != null) {
			if (character.ai.mashType == 1) {
				mashCount = Helpers.randomRange(0, 6) == 0 ? 1 : 0;
			} else if (character.ai.mashType == 2) {
				mashCount = Helpers.randomRange(0, 3) == 0 ? 1 : 0;
			}
		}

		float healthPercent = 0.3333f + ((health / maxHealth) * 0.6666f);
		float mashAmount = (healthPercent * mashCount * 0.25f);

		if (Global.frameCount - lastMashAmountSetFrame > 10) {
			lastMashAmount = 0;
		}

		float prevLastMashAmount = lastMashAmount;
		lastMashAmount += mashAmount;
		if (mashAmount > 0 && prevLastMashAmount == 0) {
			lastMashAmountSetFrame = Global.frameCount;
		}

		return (Global.spf + mashAmount);
	}

	// Sigma helper functions
	public bool isSigma2() {
		return loadout?.sigmaLoadout?.sigmaForm == 1;
	}

	public bool isSigma3() {
		return loadout?.sigmaLoadout?.sigmaForm == 2;
	}

	public bool isKaiserSigma() {
		return character is KaiserSigma;
	}

	public bool isKaiserViralSigma() {
		return character != null && character.sprite.name.StartsWith("kaisersigma_virus");
	}

	public bool isKaiserNonViralSigma() {
		return isKaiserSigma() && !isKaiserViralSigma();
	}

	public bool isAlivePuppeteer() {
		return (
			!isAI && loadout?.sigmaLoadout != null &&
			loadout.sigmaLoadout.commandMode == (int)MaverickMode.Puppeteer &&
			character?.alive == true
		);
	}

	public bool hasSubtankCapacity() {
		var subtanks = this.subtanks;
		for (int i = 0; i < subtanks.Count; i++) {
			if (subtanks[i].health < SubTank.maxHealth) {
				return true;
			}
		}
		return false;
	}

	public bool canUseSubtank(SubTank subtank) {
		if (isDead) return false;
		if (character == null) return false;
		if (character.healAmount > 0) return false;
		if (health <= 0 || health >= maxHealth) return false;
		if (subtank.health <= 0) return false;
		if (character.charState is WarpOut) return false;
		if (character.charState.invincible) return false;
		if (character is MegamanX { stingActiveTime: >0 }) return false;
		// TODO: Add Wolf Check here.
		//if (character.isHyperSigmaBS.getValue()) return false;

		return true;
	}

	public void fillSubtank(float amount) {
		if (character?.healAmount > 0) return;
		var subtanks = this.subtanks;
		for (int i = 0; i < subtanks.Count; i++) {
			if (subtanks[i].health < SubTank.maxHealth) {
				subtanks[i].health += amount;
				if (subtanks[i].health >= SubTank.maxHealth) {
					subtanks[i].health = SubTank.maxHealth;
					if (isMainPlayer && character != null)  {
						character.playAltSound("subtankFull", altParams: "harmor");
					}
				} else {
					if (isMainPlayer && character != null) {
						character.playAltSound("subtankFill", altParams: "harmor");
					}
				}
				break;
			}
		}
	}

	public bool isUsingSubTank() {
		return character?.usedSubtank != null;
	}

	public int getSpawnIndex(int spawnPointCount) {
		var nonSpecPlayers = Global.level.nonSpecPlayers();
		nonSpecPlayers = nonSpecPlayers.OrderBy(p => p.id).ToList();
		int index = nonSpecPlayers.IndexOf(this) % spawnPointCount;
		if (index < 0) {
			index = 0;
		}
		return index;
	}

	public void delaySubtank() {
		if (isMainPlayer) {
			UpgradeMenu.subtankDelay = UpgradeMenu.maxSubtankDelay;
		}
	}
}

[ProtoContract]
public class Disguise {
	[ProtoMember(1)]
	public string targetName { get; set; }

	public Disguise() { }

	public Disguise(string name) {
		targetName = name;
	}
}
