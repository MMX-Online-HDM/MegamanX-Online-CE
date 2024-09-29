using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ProtoBuf;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public partial class Player {
	public Input input;
	public Character character;
	public Character lastCharacter;
	public bool ownedByLocalPlayer;
	public int? awakenedCurrencyEnd;
	public float fgMoveAmmo = 32;
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
	public Point axlCursorPos;
	public Point? assassinCursorPos;
	public Point axlCursorWorldPos { get { return axlCursorPos.addxy(Global.level.camX, Global.level.camY); } }
	public Point axlScopeCursorWorldPos;
	public Point axlScopeCursorWorldLerpPos;
	public Point axlZoomOutCursorDestPos;
	public Point axlLockOnCursorPos;
	public Point axlGenericCursorWorldPos {
		get {
			if (character is not Axl axl || !axl.isZooming() || axl.isZoomingIn || axl.isZoomOutPhase1Done) {
				return axlCursorWorldPos;
			}
			return axlScopeCursorWorldPos;
		}
	}
	public float zoomRange {
		get {
			if (character is Axl axl && (axl.isWhiteAxl() || axl.hyperAxlStillZoomed)) return 100000;
			return Global.viewScreenW * 2.5f;
		}
	}
	public RaycastHitData assassinHitPos;

	public bool canUpgradeXArmor() {
		return (
			realCharNum == 0 && (
				character is not MegamanX mmx ||
				mmx.isHyperX != true &&
				mmx.charState is not XRevive &&
				mmx.charState is not XReviveStart
			)
		);
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
	public bool lastDeathWasVileMK5;
	public bool lastDeathWasSigmaHyper;
	public bool lastDeathWasXHyper;
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
	public bool hyperXRespawn;
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

	public float health;
	public float maxHealth;
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
	public bool usedChipOnce;

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
	public Dictionary<int, List<SubTank>> charSubTanks = new Dictionary<int, List<SubTank>>() {
		{ (int)CharIds.X, new List<SubTank>() },
		{ (int)CharIds.Zero, new List<SubTank>() },
		{ (int)CharIds.Vile, new List<SubTank>() },
		{ (int)CharIds.Axl, new List<SubTank>() },
		{ (int)CharIds.Sigma, new List<SubTank>() },
		{ (int)CharIds.PunchyZero, new List<SubTank>() },
		{ (int)CharIds.BusterZero, new List<SubTank>() },
		{ (int)CharIds.Rock, new List<SubTank>() },
	};
	// Heart tanks
	public Dictionary<int, int> charHeartTanks = new Dictionary<int, int>(){
		{ (int)CharIds.X, 0 },
		{ (int)CharIds.Zero, 0 },
		{ (int)CharIds.Vile, 0 },
		{ (int)CharIds.Axl, 0 },
		{ (int)CharIds.Sigma, 0 },
		{ (int)CharIds.PunchyZero, 0 },
		{ (int)CharIds.BusterZero, 0 },
		{ (int)CharIds.Rock, 0 },
	};
	// Getter functions.
	public List<SubTank> subtanks {
		get { return charSubTanks[isDisguisedAxl ? 3 : charNum]; }
		set { charSubTanks[isDisguisedAxl ? 3 : charNum] = value; }
	}
	public int heartTanks {
		get { return charHeartTanks[isDisguisedAxl ? 3 : charNum]; }
		set { charHeartTanks[isDisguisedAxl ? 3 : charNum] = value; }
	}

	// Currency
	public const int maxCharCurrencyId = 10;
	public static int curMul = Helpers.randomRange(2, 8);
	public int[] charCurrencyBackup = new int[maxCharCurrencyId];
	public int[] charCurrency = new int[maxCharCurrencyId];
	public int currency {
		get {
			if (!ownedByLocalPlayer) {
				return charCurrency[isDisguisedAxl ? 3 : charNum];
			}
			if (charCurrencyBackup[isDisguisedAxl ? 3 : charNum] / curMul
				!=
				charCurrency[isDisguisedAxl ? 3 : charNum]
			) {
				throw new Exception("Error, corrupted currency value");
			}
			return charCurrency[isDisguisedAxl ? 3 : charNum];
		}
		set {
			charCurrency[isDisguisedAxl ? 3 : charNum] = value;
			charCurrencyBackup[isDisguisedAxl ? 3 : charNum] = value * curMul;
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
	public LoadoutData oldAxlLoadout;
	public AxlLoadout axlLoadout { get { return loadout.axlLoadout; } }

	public bool frozenCastlePurchased;
	public bool speedDevilPurchased;

	// Note: Every time you add an armor, add an "old" version and update DNA Core code appropriately
	public ushort armorFlag;
	public ushort oldArmorFlag;
	public bool frozenCastle;
	public bool oldFrozenCastle;
	public bool speedDevil;
	public bool oldSpeedDevil;

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

		if (!ownedByLocalPlayer) {
			kills = serverPlayer.kills;
			deaths = serverPlayer.deaths;
		}

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
	public ShaderWrapper xPaletteShader = Helpers.cloneShaderSafe("palette");
	public ShaderWrapper invisibleShader = Helpers.cloneShaderSafe("invisible");
	public ShaderWrapper zeroPaletteShader = Helpers.cloneGenericPaletteShader("hyperZeroPalette");
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
	public ShaderWrapper darkHoldShader = Helpers.cloneShaderSafe("darkhold");
	public ShaderWrapper speedDevilShader = Helpers.cloneShaderSafe("speedDevilTrail");
	public ShaderWrapper trailXShader = Helpers.cloneShaderSafe("trailX");
	public ShaderWrapper trailZeroShader = Helpers.cloneShaderSafe("trailZero");
	public ShaderWrapper trailAxlShader = Helpers.cloneShaderSafe("trailAxl");
	public ShaderWrapper trailSigmaShader = Helpers.cloneShaderSafe("trailSigma");

	// Maverick shaders.
	// Duplicated mavericks are not a thing so this should not be a problem.
	public ShaderWrapper catfishChargeShader = Helpers.cloneGenericPaletteShader("paletteVoltCatfishCharge");
	public ShaderWrapper gatorArmorShader = Helpers.cloneShaderSafe("wheelgEaten");
	public ShaderWrapper spongeChargeShader = Helpers.cloneShaderSafe("wspongeCharge");

	// Projectile shaders.
	public ShaderWrapper timeSlowShader = Helpers.cloneShaderSafe("timeslow");
	public ShaderWrapper darkHoldScreenShader = Helpers.cloneShaderSafe("darkHoldScreen");


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
	public float vileAmmo = 32;
	public float vileMaxAmmo = 32;
	public float sigmaAmmo = 32;
	public float sigmaMaxAmmo = 32;
	public int? maverick1v1;
	public bool maverick1v1Spawned;

	public float possessedTime;
	public const float maxPossessedTime = 12;
	public Player possesser;

	public List<MagnetMineProj> magnetMines = new List<MagnetMineProj>();
	public List<RaySplasherTurret> turrets = new List<RaySplasherTurret>();
	public List<GrenadeProj> grenades = new List<GrenadeProj>();
	public List<ChillPIceStatueProj> iceStatues = new List<ChillPIceStatueProj>();
	public List<WSpongeSpike> seeds = new List<WSpongeSpike>();
	public List<Actor> mechaniloids = new List<Actor>();

	ExplodeDieEffect explodeDieEffect;
	public Character limboChar;
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

		if (getSameCharNum() != -1) charNum = getSameCharNum();
		if (charNum >= 210) {
			if (Global.level.is1v1()) {
				maverick1v1 = charNum - 210;
				charNum = 4;
			} else {
				charNum = 4;
				playerData.charNum = 4;
			}
		}
		this.charNum = charNum;
		newCharNum = charNum;

		this.input = input;
		this.ownedByLocalPlayer = ownedByLocalPlayer;

		this.xArmor1v1 = playerData?.armorSet ?? 1;
		if (Global.level.is1v1() && isX) {
			bootsArmorNum = xArmor1v1;
			bodyArmorNum = xArmor1v1;
			helmetArmorNum = xArmor1v1;
			armArmorNum = xArmor1v1;
		}

		for (int i = 0; i < charCurrency.Length; i++) {
			charCurrency[i] = getStartCurrency();
			charCurrencyBackup[i] = getStartCurrency() * curMul;
		}
		foreach (var key in charHeartTanks.Keys) {
			int htCount = getStartHeartTanksForChar();
			int altHtCount = getStartHeartTanks();
			if (altHtCount > htCount) {
				htCount = altHtCount;
			}
			charHeartTanks[key] = htCount;
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

		configureWeapons();

		is1v1Combatant = !isSpectator;
	}

	public int getHeartTankModifier() {
		return Global.level.server?.customMatchSettings?.heartTankHp ?? 1;
	}

	public float getMaverickMaxHp() {
		if (!Global.level.is1v1() && isTagTeam()) {
			return getModifiedHealth(20) + (heartTanks * getHeartTankModifier());
		}
		return MathF.Ceiling(getModifiedHealth(24));
	}

	public bool hasAllItems() {
		return subtanks.Count >= 4 && heartTanks >= 8;
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
		if (Global.level.server.customMatchSettings != null) {
			/*if (Global.level.gameMode.isTeamMode && alliance == GameMode.redAlliance) {
				return Global.level.server.customMatchSettings.redDamageModifier;
			}*/
			return Global.level.server.customMatchSettings.damageModifier;
		}
		return 1;
	}

	public float getMaxHealth() {
		// 1v1 is the only mode without possible heart tanks/sub tanks
		if (Global.level.is1v1()) {
			return getModifiedHealth(28);
		}
		int bonus = 0;
		if (isSigma && isPuppeteer()) {
			bonus = 4;
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

	// The character net id is always the first net id of the player
	public ushort getCharActorNetId() {
		return getStartNetId();
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
		if (curATransNetId < getStartNetId() + 1) {
			curATransNetId = (ushort)(getStartNetId() + 1);
		}
		ushort retId = curATransNetId;
		curATransNetId++;
		if (curATransNetId >= getStartNetId() + 10) {
			curATransNetId = (ushort)(getStartNetId() + 1);
		}
		return retId;
	}

	// Usually, only the main player is allowed to get the next actor net id. The exception is if you call setNextActorNetId() first. The assert checks for that in debug.
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
		if (isControllingPuppet()) {
			return true;
		}
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

		if (Global.level.gameMode.isOver && aiTakeover) {
			aiTakeover = false;
			isAI = false;
			if (character != null) character.ai = null;
		}

		if (!Global.level.gameMode.isOver) {
			respawnTime -= Global.spf;
		}

		if (ownedByLocalPlayer && Global.isOnFrame(30)) {
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
		hyperXRespawn = false;

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

			if (canReviveSigma(out var spawnPoint) &&
				(input.isPressed(Control.Special2, this) ||
				Global.level.isHyper1v1() ||
				Global.shouldAiAutoRevive)
			) {
				reviveSigma(2, spawnPoint);
			}
		} else if (isX) {
			if (canReviveX() && (input.isPressed(Control.Special2, this) || Global.shouldAiAutoRevive)) {
				reviveX();
			}
		}

		// Never spawn a character if it already exists
		if (character == null) {
			bool sendRpc = ownedByLocalPlayer;
			var charNetId = getCharActorNetId();
			if (shouldRespawn()) {
				if (Global.level.gameMode is TeamDeathMatch && Global.level.teamNum > 2) {
					List<Player> spawnPoints = Global.level.players.FindAll(
						p => p.teamAlliance == teamAlliance && p.health > 0 && p.character != null
					);
					if (spawnPoints.Count != 0) {
						Character randomChar = spawnPoints[Helpers.randomRange(0, spawnPoints.Count - 1)].character;
						Point warpInPos = Global.level.getGroundPosNoKillzone(
							randomChar.pos, Global.screenH
						) ?? randomChar.pos;
						spawnCharAtPoint(warpInPos, randomChar.xDir, charNetId, sendRpc);
					} else {
						var spawnPoint = Global.level.getSpawnPoint(this, !warpedInOnce);
						int spawnPointIndex = Global.level.spawnPoints.IndexOf(spawnPoint);
						spawnCharAtSpawnIndex(spawnPointIndex, charNetId, sendRpc);
					}
				}
				else {
					var spawnPoint = Global.level.getSpawnPoint(this, !warpedInOnce);
					if (spawnPoint == null) return;
					int spawnPointIndex = Global.level.spawnPoints.IndexOf(spawnPoint);
					spawnCharAtSpawnIndex(spawnPointIndex, charNetId, sendRpc);
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

		spawnCharAtPoint(new Point(spawnPoint.pos.x, spawnPoint.getGroundY()), spawnPoint.xDir, charNetId, sendRpc);
	}

	public void spawnCharAtPoint(Point pos, int xDir, ushort charNetId, bool sendRpc) {
		if (sendRpc) {
			RPC.spawnCharacter.sendRpc(pos, xDir, id, charNetId);
		}

		if (Global.level.gameMode.isTeamMode) {
			alliance = newAlliance;
		}

		// ONRESPAWN, SPAWN, RESPAWN, ON RESPAWN, ON SPAWN LOGIC, SPAWNLOGIC
		charNum = newCharNum;
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

		configureWeapons();
		maxHealth = getMaxHealth();
		if (isSigma) {
			if (isSigma1()) {
				sigmaMaxAmmo = 20;
				sigmaAmmo = sigmaMaxAmmo;
			} else if (isSigma2()) {
				sigmaMaxAmmo = 28;
				sigmaAmmo = 0;
			}
		}
		health = maxHealth;
		assassinHitPos = null;

		if (character == null) {
			bool mk2VileOverride = false;
			// Hyper mode overrides (PRE)
			if (Global.level.isHyper1v1() && ownedByLocalPlayer) {
				if (isVile) {
					mk2VileOverride = true;
					currency = 9999;
				}
			}

			if (charNum == (int)CharIds.X) {
				character = new MegamanX(
					this, pos.x, pos.y, xDir,
					false, charNetId, ownedByLocalPlayer
				);
			} else if (charNum == (int)CharIds.Zero) {
				character = new Zero(
					this, pos.x, pos.y, xDir,
					false, charNetId, ownedByLocalPlayer
				);
			} else if (charNum == (int)CharIds.Vile) {
				character = new Vile(
					this, pos.x, pos.y, xDir, false, charNetId,
					ownedByLocalPlayer, mk2VileOverride: mk2VileOverride
				);
			} else if (charNum == (int)CharIds.Axl) {
				character = new Axl(
					this, pos.x, pos.y, xDir,
					false, charNetId, ownedByLocalPlayer
				);
			} else if (charNum == (int)CharIds.Sigma) {
				if (!ownedByLocalPlayer && !loadoutSet) {
					character = new BaseSigma(
						this, pos.x, pos.y, xDir,
						false, charNetId, ownedByLocalPlayer
					);
				} else if (isSigma3()) {
					character = new Doppma(
						this, pos.x, pos.y, xDir,
						false, charNetId, ownedByLocalPlayer
					);
				} else if (isSigma2()) {
					character = new NeoSigma(
						this, pos.x, pos.y, xDir,
						false, charNetId, ownedByLocalPlayer
					);
				} else {
					character = new CmdSigma(
						this, pos.x, pos.y, xDir,
						false, charNetId, ownedByLocalPlayer
					);
				}
			} else if (charNum == (int)CharIds.Rock) {
				character = new Rock(
					this, pos.x, pos.y, xDir,
					false, charNetId, ownedByLocalPlayer
				);
			} else if (charNum == (int)CharIds.BusterZero) {
				character = new BusterZero(
					this, pos.x, pos.y, xDir,
					false, charNetId, ownedByLocalPlayer
				);
			} else if (charNum == (int)CharIds.PunchyZero) {
				character = new PunchyZero(
					this, pos.x, pos.y, xDir,
					false, charNetId, ownedByLocalPlayer
				);
			} else {
				throw new Exception("Error: Non-valid char ID: " + charNum);
			}
			// Hyper mode overrides (POST)
			if (Global.level.isHyperMatch() && ownedByLocalPlayer) {
				if (isX) {
					setUltimateArmor(true);
				}
				if (character is Zero zero) {
					if (loadout.zeroLoadout.hyperMode == 0) {
						zero.isBlack = true;
					} else if (loadout.zeroLoadout.hyperMode == 1) {
						zero.awakenedPhase = 1;
					} else {
						zero.isViral = true;
					}
				}
				if (character is Axl axl) {
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

			lastCharacter = character;
		}

		if (isAI) {
			character.addAI();
		}

		if (character.rideArmor != null) character.rideArmor.xDir = xDir;

		if (isCamPlayer) {
			Global.level.snapCamPos(character.getCamCenterPos(), null);
			//console.log(Global.level.camX + "," + Global.level.camY);
		}
		warpedIn = true;
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
		if (character.isCCImmune()) return false;
		if (character.flag != null) return false;
		if (possessedTime > 0) return false;
		if (character.isVaccinated()) return false;
		return true;
	}

	public void transformAxlNet(RPCAxlDisguiseJson data) {
		disguise = new Disguise(data.targetName);
		charNum = data.charNum;

		Character? retChar = null;
		if (data.charNum == (int)CharIds.X) {
			retChar = new MegamanX(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, true, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.Zero) {
			retChar = new Zero(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, true, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.Vile) {
			retChar = new Vile(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, true, isWarpIn: false,
				mk2VileOverride: data.extraData[0] == 1, mk5VileOverride: data.extraData[0] == 2
			);
		} else if (data.charNum == (int)CharIds.Axl) {
			retChar = new Axl(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, true, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.Sigma) {
			if (data.extraData[0] == 2) {
				retChar = new Doppma(
					this, character.pos.x, character.pos.y, character.xDir,
					true, data.dnaNetId, true, isWarpIn: false
				);
			} else if (data.extraData[0] == 1) {
				retChar = new NeoSigma(
					this, character.pos.x, character.pos.y, character.xDir,
					true, data.dnaNetId, true, isWarpIn: false
				);
			} else {
				retChar = new CmdSigma(
					this, character.pos.x, character.pos.y, character.xDir,
					true, data.dnaNetId, true, isWarpIn: false
				);
			}
		} else if (data.charNum == (int)CharIds.Rock) {
			retChar = new Rock(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, true, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.BusterZero) {
			retChar = new BusterZero(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, true, isWarpIn: false
			);
		} else if (data.charNum == (int)CharIds.PunchyZero) {
			retChar = new PunchyZero(
				this, character.pos.x, character.pos.y, character.xDir,
				true, data.dnaNetId, true, isWarpIn: false
			);
		} else {
			throw new Exception("Error: Non-valid char ID: " + data.charNum);
		}

		// Status effects.
		retChar.burnTime = character.burnTime;
		retChar.acidTime = character.acidTime;
		retChar.oilTime = character.oilTime;
		retChar.igFreezeProgress = character.igFreezeProgress;
		retChar.infectedTime = character.infectedTime;

		// Hit cooldowns.
		retChar.grabCooldown = character.grabCooldown;
		retChar.projectileCooldown = character.projectileCooldown;
		retChar.flinchCooldown = character.flinchCooldown;

		// Change character.
		character.cleanupBeforeTransform();
		preTransformedAxl = character;
		Global.level.gameObjects.Remove(preTransformedAxl);
		character = retChar;

		// Save old flags.
		oldArmorFlag = armorFlag;
		oldSpeedDevil = speedDevil;
		oldFrozenCastle = frozenCastle;;

		// Armor flags for X.
		if (data.charNum == (int)CharIds.X) {
			armorFlag = BitConverter.ToUInt16(
				new byte[] { data.extraData[0], data.extraData[1] }
			);
			if (retChar is MegamanX retX) {
				retX.hasUltimateArmor = (data.extraData[2] == 1);
			}
		}
		// Store old weapons.
		oldWeapons = weapons;

		// Change weapons.
		// We temporally change loadout to populate it.
		if (data.loadout == null) { return; }
		LoadoutData oldLoadout = loadout;
		loadout = data.loadout;
		configureWeapons();
		configureStaticWeapons();
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

		oldArmorFlag = armorFlag;
		oldFrozenCastle = frozenCastle;
		oldSpeedDevil = speedDevil;

		armorFlag = dnaCore.armorFlag;
		frozenCastle = dnaCore.frozenCastle;
		speedDevil = dnaCore.speedDevil;

		bool isVileMK2 = charNum == 2 && dnaCore.hyperMode == DNACoreHyperMode.VileMK2;
		bool isVileMK5 = charNum == 2 && dnaCore.hyperMode == DNACoreHyperMode.VileMK5;

		if (ownedByLocalPlayer) {
			byte[]? extraData = null;
			if (dnaCore.charNum == (int)CharIds.X) {
				byte[] armorBytes = BitConverter.GetBytes(character.player.armorFlag);

				extraData = new byte[] {
					armorBytes[0],
					armorBytes[1],
					dnaCore.ultimateArmor ? (byte)1 : (byte)0
				};
			} else if (dnaCore.charNum == (int)CharIds.Vile) {
				extraData = new byte[1];
				if (isVileMK2) {
					extraData[0] = 1;
				}
				if (isVileMK5) {
					extraData[0] = 2;
				}
			} else if (dnaCore.charNum == (int)CharIds.Sigma) {
				extraData = new byte[1];
				extraData[0] = (byte)dnaCore.loadout.sigmaLoadout.sigmaForm;
			}
			string json = JsonConvert.SerializeObject(
				new RPCAxlDisguiseJson(
					id, disguise.targetName, dnaCore.charNum,
					dnaCore.loadout, dnaNetId, extraData
				)
			);
			Global.serverClient?.rpc(RPC.axlDisguise, json);
		}
		maxHealth = dnaCore.maxHealth + MathF.Ceiling(heartTanks * getHeartTankModifier());

		oldAxlLoadout = loadout;
		loadout = dnaCore.loadout;

		oldWeapons = weapons;
		weapons = new List<Weapon>(dnaCore.weapons);
		configureStaticWeapons();

		if (charNum == (int)CharIds.Zero) {
			weapons.Add(new ZSaber());
		}
		if (charNum == (int)CharIds.BusterZero) {
			weapons.Add(new KKnuckleWeapon());
		}
		if (charNum == (int)CharIds.PunchyZero) {
			weapons.Add(new ZeroBuster());
		}
		if (charNum == (int)CharIds.Sigma) {
			weapons.Add(new SigmaMenuWeapon());
		}
		//weapons.Add(new AssassinBullet());
		weapons.Add(new UndisguiseWeapon());
		weaponSlot = 0;

		sigmaAmmo = dnaCore.rakuhouhaAmmo;

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
			);
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
		} else {
			throw new Exception("Error: Non-valid char ID: " + charNum);
		}
		if (retChar is Vile vile) {
			if (isVileMK5) vile.vileForm = 2;
			else if (isVileMK2) vile.vileForm = 1;
		}
		retChar.addTransformAnim();

		if (isAI) {
			retChar.addAI();
		}

		retChar.xDir = character.xDir;
		//retChar.heal(maxHealth);

		// Speed and state.
		if (character.grounded) {
			retChar.changeState(new Idle(), true);
		} else if (character.charState is Jump) {
			retChar.changeState(new Jump(), true);
		} else {
			retChar.changeState(new Fall(), true);
		}
		retChar.vel = character.vel;
		retChar.slideVel = character.slideVel;
		retChar.xFlinchPushVel = character.xFlinchPushVel;
		retChar.xIceVel = character.xIceVel;

		// Status effects.
		retChar.burnTime = character.burnTime;
		retChar.acidTime = character.acidTime;
		retChar.oilTime = character.oilTime;
		retChar.igFreezeProgress = character.igFreezeProgress;
		retChar.infectedTime = character.infectedTime;

		// Hit cooldowns.
		retChar.grabCooldown = character.grabCooldown;
		retChar.projectileCooldown = character.projectileCooldown;
		retChar.flinchCooldown = character.flinchCooldown;

		character.cleanupBeforeTransform();
		character = retChar;
		if (weapon != null) {
			weapon.shootTime = 0.25f;
		}

		if (character is Zero zero) {
			zero.gigaAttack.ammo = dnaCore.rakuhouhaAmmo;
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

		dnaCore.usedOnce = true;
		dnaCore.hyperMode = DNACoreHyperMode.None;
	}

	// If you change this method change revertToAxlDeath() too
	public void revertToAxl() {
		disguise = null;
		Character oldChar = character;

		if (ownedByLocalPlayer) {
			string json = JsonConvert.SerializeObject(new RPCAxlDisguiseJson(id, "", -1));
			Global.serverClient?.rpc(RPC.axlDisguise, json);

			if (character is Zero zero) {
				lastDNACore.rakuhouhaAmmo = zero.gigaAttack.ammo;
			} else if (character is PunchyZero pzero) {
				lastDNACore.rakuhouhaAmmo = pzero.gigaAttack.ammo;
			} else if (isSigma) {
				lastDNACore.rakuhouhaAmmo = sigmaAmmo;
			}
		}
		var oldPos = character.pos;
		var oldDir = character.xDir;
		character.destroySelf();
		Global.level.gameObjects.Add(preTransformedAxl);
		character = preTransformedAxl;
		character.addTransformAnim();
		preTransformedAxl = null;
		charNum = 3;
		character.pos = oldPos;
		character.xDir = oldDir;
		maxHealth = getMaxHealth();
		health = Math.Min(health, maxHealth);
		loadout = oldAxlLoadout;
		weapons = oldWeapons;
		configureStaticWeapons();
		weaponSlot = 0;

		armorFlag = oldArmorFlag;
		speedDevil = oldSpeedDevil;
		frozenCastle = oldFrozenCastle;

		lastDNACore = null;
		lastDNACoreIndex = 4;
		character.ownedByLocalPlayer = ownedByLocalPlayer;

		if (ownedByLocalPlayer) {
			if (oldChar.grounded) {
				character.changeState(new Idle(), true);
			} else if (character.charState is Jump) {
				character.changeState(new Jump(), true);
			} else {
				character.changeState(new Fall(), true);
			}
			character.vel = oldChar.vel;
			character.slideVel = oldChar.slideVel;
			character.xFlinchPushVel = oldChar.xFlinchPushVel;
			character.xIceVel = oldChar.xIceVel;
		}
	}

	// If you change this method change revertToAxl() too
	public void revertToAxlDeath() {
		disguise = null;

		if (ownedByLocalPlayer) {
			string json = JsonConvert.SerializeObject(new RPCAxlDisguiseJson(id, "", -2));
			Global.serverClient?.rpc(RPC.axlDisguise, json);

			if (character is Zero zero) {
				lastDNACore.rakuhouhaAmmo = zero.gigaAttack.ammo;
			} else if (character is PunchyZero pzero) {
				lastDNACore.rakuhouhaAmmo = pzero.gigaAttack.ammo;
			} else if (isSigma) {
				lastDNACore.rakuhouhaAmmo = sigmaAmmo;
			}
		}
		preTransformedAxl = null;
		charNum = 3;
		maxHealth = getMaxHealth();;
		health = 0;
		loadout = oldAxlLoadout;
		configureWeapons();
		configureStaticWeapons();
		weaponSlot = 0;

		armorFlag = oldArmorFlag;
		speedDevil = oldSpeedDevil;
		frozenCastle = oldFrozenCastle;

		lastDNACore = null;
		lastDNACoreIndex = 4;
		character.addTransformAnim();
	}

	public bool isMainPlayer {
		get { return Global.level.mainPlayer == this; }
	}

	public bool isCamPlayer {
		get { return this == Global.level.camPlayer; }
	}

	public bool hasArmor() {
		return bodyArmorNum > 0 || bootsArmorNum > 0 || armArmorNum > 0 || helmetArmorNum > 0;
	}

	public bool hasArmor(int version) {
		return bodyArmorNum == version || bootsArmorNum == version || armArmorNum == version || helmetArmorNum == version;
	}

	public bool hasAllArmor() {
		return bodyArmorNum > 0 && bootsArmorNum > 0 && armArmorNum > 0 && helmetArmorNum > 0;
	}

	public bool hasAllX3Armor() {
		return bodyArmorNum >= 3 && bootsArmorNum >= 3 && armArmorNum >= 3 && helmetArmorNum >= 3;
	}

	public bool canUpgradeGoldenX() {
		return character != null &&
			isX && !isDisguisedAxl &&
			character.charState is not Die && !Global.level.is1v1() &&
			hasAllX3Armor() && !hasAnyChip() && !hasUltimateArmor() &&
			!hasGoldenArmor() && currency >= goldenArmorCost && !usedChipOnce;
	}

	public bool canUpgradeUltimateX() {
		return character != null &&
			isX && !isDisguisedAxl &&
			character.charState is not Die && !Global.level.is1v1() &&
			!hasUltimateArmor() && !canUpgradeGoldenX() && hasAllArmor() && currency >= ultimateArmorCost;
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

	public void removeOwnedMines() {
		for (int i = magnetMines.Count - 1; i >= 0; i--) {
			magnetMines[i].destroySelf();
		}
	}

	public void removeOwnedTurrets() {
		for (int i = turrets.Count - 1; i >= 0; i--) {
			turrets[i].destroySelf();
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
				InRideArmor inRideArmor = character?.charState as InRideArmor;
				if (inRideArmor != null &&
					(inRideArmor.frozenTime > 0 || inRideArmor.stunTime > 0 || inRideArmor.crystalizeTime > 0)
				) {
					return false;
				}
				if ((character as MegamanX)?.shotgunIceChargeTime > 0f) {
					return false;
				}
				if (character.charState is GenericStun) {
					return false;
				}
				if (character?.charState is SniperAimAxl) {
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
		if (isVile) {
			fillSubtank(2);
		} else if (isAxl) {
			fillSubtank(3);
		} else {
			fillSubtank(4);
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
		if (character?.isCCImmuneHyperMode() == true) return;
		if (character?.rideArmor?.raNum == 4 && character.charState is InRideArmor) return;
		if (isX && hasUltimateArmor()) return;

		currency++;
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
		if (Global.level.gameMode is ControlPoints && alliance == GameMode.redAlliance) {
			return 8;
		}
		if (Global.level.gameMode is KingOfTheHill) {
			return 7;
		}
		return 5;
	}

	public bool canReviveVile() {
		if (Global.level.isElimination() ||
			!lastDeathCanRevive ||
			newCharNum != 2 ||
			currency < reviveVileCost ||
			lastDeathWasVileMK5
		) {
			return false;
		}
		if (limboChar is not Vile vile || vile.summonedGoliath) {
			return false;
		}
		return true;
	}

	public bool canReviveSigma(out Point spawnPoint) {
		spawnPoint = Point.zero;
		if (Global.level.isHyper1v1() && !lastDeathWasSigmaHyper && limboChar != null && isSigma && newCharNum == 4) {
			return true;
		}

		bool basicCheck = !Global.level.isElimination() && limboChar != null && lastDeathCanRevive && isSigma && newCharNum == 4 && currency >= reviveSigmaCost && !lastDeathWasSigmaHyper;
		if (!basicCheck) return false;

		if (false) {
			Point deathPos = limboChar.pos;

			// Get ground snapping pos
			var rect = new Rect(deathPos.addxy(-7, 0), deathPos.addxy(7, 112));
			var hits = Global.level.checkCollisionsShape(rect.getShape(), null);
			Point? closestHitPoint = Helpers.getClosestHitPoint(hits, deathPos, typeof(Wall));
			if (closestHitPoint != null) {
				deathPos = new Point(deathPos.x, closestHitPoint.Value.y);
			} else {
				if (isSigma1()) {
					return false;
				}
			}

			// Check if ample space to revive in
			int w = 10;
			int h = 120;
			rect = new Rect(new Point(deathPos.x - w / 2, deathPos.y - h), new Point(deathPos.x + w / 2, deathPos.y - 25));
			hits = Global.level.checkCollisionsShape(rect.getShape(), null);
			if (hits.Any(h => h.gameObject is Wall)) {
				return false;
			}

			if (deathPos.x - 100 < 0 || deathPos.x + 100 > Global.level.width) {
				return false;
			}
			foreach (var player in Global.level.players) {
				if (player.character is WolfSigma && player.character.pos.distanceTo(deathPos) < Global.screenW) {
					return false;
				}
			}
		} else if (false) {
			return true;
		} else if (true) {
			return limboChar != null && KaiserSigma.canKaiserSpawn(limboChar, out spawnPoint);
		}

		return true;
	}

	public bool canReviveX() {
		return !Global.level.isElimination() && armorFlag == 0 && character?.charState is Die && lastDeathCanRevive && isX && newCharNum == 0 && currency >= reviveXCost && !lastDeathWasXHyper;
	}

	public void reviveVile(bool toMK5) {
		currency -= reviveVileCost;
		Vile vile = (limboChar as Vile);

		if (toMK5) {
			vileFormToRespawnAs = 2;
		} else if (vile.vileForm == 0) {
			vileFormToRespawnAs = 1;
		} else if (vile.vileForm == 1) {
			vileFormToRespawnAs = 2;
		}

		respawnTime = 0;
		character = limboChar;
		vile.alreadySummonedNewMech = false;
		character.visible = true;
		if (explodeDieEffect != null) {
			explodeDieEffect.destroySelf();
			explodeDieEffect = null;
		}
		limboChar = null;
		vile.rideMenuWeapon = new MechMenuWeapon(VileMechMenuType.All);
		character.changeState(new VileRevive(vileFormToRespawnAs == 2), true);
		RPC.playerToggle.sendRpc(id, vileFormToRespawnAs == 2 ? RPCToggleType.ReviveVileTo5 : RPCToggleType.ReviveVileTo2);
	}

	public void reviveVileNonOwner(bool toMK5) {
		Vile vile = (character as Vile);

		if (toMK5) {
			vileFormToRespawnAs = 2;
		} else {
			vileFormToRespawnAs = 1;
		}

		respawnTime = 0;
		vile.alreadySummonedNewMech = false;
		character.visible = true;
		if (explodeDieEffect != null) {
			explodeDieEffect.destroySelf();
			explodeDieEffect = null;
		}
		character.changeState(new VileRevive(toMK5), true);
	}

	public void reviveSigma(int form, Point spawnPoint) {
		currency -= reviveSigmaCost;
		hyperSigmaRespawn = true;
		respawnTime = 0;
		character = limboChar;
		limboChar = null;
		if (character.destroyed == false) {
			character.destroySelf();
		}
		clearSigmaWeapons();
		maxHealth = getModifiedHealth(32);
		ushort newNetId = getNextATransNetId();
		if (form == 0) {
			if (Global.level.is1v1()) {
				character.changePos(new Point(Global.level.width / 2, character.pos.y));
			}
			character.changeState(new WolfSigmaRevive(explodeDieEffect), true);
		} else if (form == 1) {
			explodeDieEffect.changeSprite("sigma2_revive");
			character.changeState(new ViralSigmaRevive(explodeDieEffect), true);
		} else {
			KaiserSigma kaiserSigma = new KaiserSigma(
				this, spawnPoint.x, spawnPoint.y, character.xDir, true,
				newNetId, true
			);
			character = kaiserSigma;
			character.changeSprite("kaisersigma_enter", true);
			//explodeDieEffect.changeSprite("sigma3_revive");
			if (Global.level.is1v1() && spawnPoint.isZero()) {
				var closestSpawn = Global.level.spawnPoints.OrderBy(
					s => s.pos.distanceTo(character.pos)
				).FirstOrDefault();
				spawnPoint = closestSpawn?.pos ?? new Point(Global.level.width / 2, character.pos.y);
			}
			character.changeState(new KaiserSigmaRevive(explodeDieEffect, spawnPoint), true);
		}
		RPC.reviveSigma.sendRpc(form, spawnPoint, id, newNetId);
	}

	public void reviveSigmaNonOwner(int form, Point spawnPoint, ushort sigmaNetId) {
		clearSigmaWeapons();
		maxHealth = getModifiedHealth(32);
		if (form >= 2) {
			character.destroySelf();
			KaiserSigma kaiserSigma = new KaiserSigma(
				this, spawnPoint.x, spawnPoint.y, character.xDir, true,
				sigmaNetId, false
			);
			character = kaiserSigma;

			character.changeSprite("kaisersigma_enter", true);
		}
	}

	public void reviveX() {
		currency -= reviveXCost;
		hyperXRespawn = true;
		respawnTime = 0;
		character.changeState(new XRevive(), true);
	}

	public void reviveXNonOwner() {
	}

	public void explodeDieStart() {
		respawnTime = getRespawnTime(); // * (suicided ? 2 : 1);
		randomTip = Tips.getRandomTip(charNum);

		explodeDieEffect = ExplodeDieEffect.createFromActor(character.player, character, 20, 1.5f, false);
		Global.level.addEffect(explodeDieEffect);
		limboChar = character;
		character = null;
	}

	public void explodeDieEnd() {
		if (limboChar != null) {
			limboChar.destroySelf();
			limboChar = null;
		}
		explodeDieEffect = null;
		Global.serverClient?.rpc(RPC.destroyCharacter, (byte)id);
	}

	public void destroySigmaEffect() {
		ExplodeDieEffect.createFromActor(this, character, 25, 2, false);
	}

	public void destroySigma() {
		respawnTime = getRespawnTime();// * (suicided ? 2 : 1);
		randomTip = Tips.getRandomTip(charNum);

		if (character == null) {
			return;
		}

		character.destroySelf();
		character = null;
		Global.serverClient?.rpc(RPC.destroyCharacter, (byte)id);
		onCharacterDeath();
	}

	public void destroyCharacter() {
		respawnTime = getRespawnTime();// * (suicided ? 2 : 1);
		randomTip = Tips.getRandomTip(charNum);

		if (character == null) {
			return;
		}

		if (isAxl) {
			//axlBulletTypeBought[6] = false;
			//if (axlBulletType == (int)AxlBulletWeaponType.AncientGun) axlBulletType = 0;
		}

		if (isZero && awakenedCurrencyEnd != null && currency >= awakenedCurrencyEnd) {
			currency = awakenedCurrencyEnd.Value;
			awakenedCurrencyEnd = null;
		}

		if (!character.player.isVile && !character.player.isSigma) {
			character.playSound("die");
			/*
			if (character.player == Global.level.mainPlayer)
			{
				Global.playSound("die");
			}
			else
			{
				character.playSound("die");
			}
			*/
			new DieEffect(character.getCenterPos(), charNum);
		}

		character.destroySelf();
		character = null;

		onCharacterDeath();
	}

	// Must be called on any character death
	public void onCharacterDeath() {
		if (delayedNewCharNum != null && Global.level.mainPlayer.charNum != delayedNewCharNum.Value) {
			Global.level.mainPlayer.newCharNum = delayedNewCharNum.Value;
			Global.serverClient?.rpc(RPC.switchCharacter, (byte)Global.level.mainPlayer.id, (byte)delayedNewCharNum.Value);
		}
		delayedNewCharNum = null;
		suicided = false;
		unpossess();
	}

	public void maverick1v1Kill() {
		character?.applyDamage(1000, null, null, null, null);
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
			currentMaverick?.applyDamage(1000, this, character, null, null);
			return;
		}

		if (currentMaverick != null && isTagTeam()) {
			destroyCharacter();
		} else {
			character?.applyDamage(1000, this, character, null, null);
		}
		foreach (var maverick in mavericks) {
			maverick.applyDamage(1000, this, character, null, null);
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

	public void removeArmorNum(int armorIndex) {
		setArmorNum(armorIndex, 0);
	}

	public bool hasAnyChip() {
		return hasChip(0) || hasChip(1) || hasChip(2) || hasChip(3);
	}

	public bool hasChip(int armorIndex) {
		if (!hasAllX3Armor()) return false;
		return getArmorNum(armorFlag, armorIndex, true) == 15;
	}

	public void setChipNum(int armorIndex, bool remove) {
		if (!remove) {
			usedChipOnce = true;
		}
		setArmorNum(0, 3);
		setArmorNum(1, 3);
		setArmorNum(2, 3);
		setArmorNum(3, 3);
		setArmorNum(armorIndex, remove ? 3 : 15);
	}

	public void setGoldenArmor(bool addOrRemove) {
		if (addOrRemove) {
			savedArmorFlag = armorFlag;
			armorFlag = ushort.MaxValue;
		} else {
			armorFlag = savedArmorFlag;
		}
	}

	public bool hasGoldenArmor() {
		return armorFlag == ushort.MaxValue;
	}

	public void setUltimateArmor(bool addOrRemove) {
		if (character is MegamanX mmx) {
			if (addOrRemove) {
				mmx.hasUltimateArmor = true;
				addNovaStrike();
			} else {
				mmx.hasUltimateArmor = false;
				removeNovaStrike();
			}
		}
	}

	public bool hasUltimateArmor() {
		return character is MegamanX { hasUltimateArmor: true };
	}

	public int bootsArmorNum {
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

	public bool hasBootsArmor(ArmorId armorId) { return bootsArmorNum == (int)armorId; }
	public bool hasBodyArmor(ArmorId armorId) { return bodyArmorNum == (int)armorId; }
	public bool hasHelmetArmor(ArmorId armorId) { return helmetArmorNum == (int)armorId; }
	public bool hasArmArmor(ArmorId armorId) { return armArmorNum == (int)armorId; }

	public bool hasBootsArmor(int xGame) { return bootsArmorNum == xGame; }
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
		return bootsArmorNum > 0 || armArmorNum > 0 || bodyArmorNum > 0 || helmetArmorNum > 0;
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
		} else if (ownedByLocalPlayer) {
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

	public void addDeath(bool isSuicide) {
		if (isSigma && maverick1v1 == null && Global.level.isHyper1v1() && !lastDeathWasSigmaHyper) {
			return;
		}

		suicided = isSuicide;
		if (Global.serverClient == null) {
			deaths++;
			if (isSuicide) {
				kills = Helpers.clamp(kills - 1, 0, int.MaxValue);
				currency = Helpers.clamp(currency - 1, 0, int.MaxValue);
			}
		} else if (ownedByLocalPlayer) {
			deaths++;
			if (isSuicide) {
				kills = Helpers.clamp(kills - 1, 0, int.MaxValue);
				currency = Helpers.clamp(currency - 1, 0, int.MaxValue);
			}
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

	public bool isSigma1AndSigma() {
		return isSigma1() && isSigma;
	}

	public bool isSigma2AndSigma() {
		return isSigma2() && isSigma;
	}

	public bool isSigma3AndSigma() {
		return isSigma3() && isSigma;
	}

	public bool isSigma1() {
		return loadout?.sigmaLoadout?.sigmaForm == 0;
	}

	public bool isSigma2() {
		return loadout?.sigmaLoadout?.sigmaForm == 1;
	}

	public bool isSigma3() {
		return loadout?.sigmaLoadout?.sigmaForm == 2;
	}

	public bool isSigma1Or3() {
		return isSigma1() || isSigma3();
	}

	public bool isWolfSigma() {
		return character is WolfSigma;
	}

	public bool isViralSigma() {
		return character is ViralSigma;
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

	public bool isSummoner() {
		return loadout?.sigmaLoadout != null && loadout.sigmaLoadout.commandMode == 0;
	}

	public bool isPuppeteer() {
		return loadout?.sigmaLoadout != null && loadout.sigmaLoadout.commandMode == 1;
	}

	public bool isStriker() {
		return loadout?.sigmaLoadout != null && loadout.sigmaLoadout.commandMode == 2;
	}

	public bool isTagTeam() {
		return loadout?.sigmaLoadout != null && loadout.sigmaLoadout.commandMode == 3;
	}

	public bool isRefundableMode() {
		return isSummoner() || isPuppeteer() || isTagTeam();
	}

	public bool isAlivePuppeteer() {
		return isPuppeteer() && health > 0;
	}

	public bool isControllingPuppet() {
		return isSigma && isPuppeteer() && currentMaverick != null && weapon is MaverickWeapon;
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
		if (character.healAmount > 0) return false;
		if (health <= 0 || health >= maxHealth) return false;
		if (subtank.health <= 0) return false;
		if (character.charState is WarpOut) return false;
		if (character.charState.invincible) return false;
		if (character.isCStingInvisible()) return false;
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
					if (isMainPlayer) Global.playSound("subtankFull");
				} else {
					if (isMainPlayer) Global.playSound("subtankFill");
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
