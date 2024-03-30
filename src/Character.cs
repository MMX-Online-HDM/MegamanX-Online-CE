
using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public partial class Character : Actor, IDamagable {
	public static string[] charDisplayNames = {
		"X",
		"Zero",
		"Vile",
		"Axl",
		"Sigma"
	};
	public CharState charState;
	public Player player;
	public bool isDashing;
	public float shootTime {
		get { return player.weapon.shootTime; }
		set { player.weapon.shootTime = value; }
	}
	public bool changedStateInFrame;
	public bool pushedByTornadoInFrame;
	public float chargeTime;
	public const float charge1Time = 0.5f;
	public const float charge2Time = 1.75f;
	public const float charge3Time = 3f;
	public const float charge4Time = 4.25f;
	public float hyperProgress;

	public Point? sigmaHeadGroundCamCenterPos;
	public float chargeFlashTime;
	public ChargeEffect chargeEffect;
	public float shootAnimTime = 0;
	public AI ai;
	public bool slowdown;
	public bool boughtUltimateArmorOnce;
	public bool boughtGoldenArmorOnce;

	public float headbuttAirTime = 0;
	public int dashedInAir = 0;
	public bool lastAirDashWasSide;
	public float healAmount = 0;
	public SubTank usedSubtank;
	public float netSubtankHealAmount;
	public bool playHealSound;
	public float healTime = 0;
	public float weaponHealAmount = 0;
	public float weaponHealTime = 0;
	public float healthBarInnerWidth;
	public float slideVel = 0;
	public Flag? flag;
	public float stingChargeTime;
	public bool isCrystalized;
	public bool insideCharacter;
	public float invulnTime = 0;
	public float parryCooldown;
	public float maxParryCooldown = 0.5f;

	public bool stockedCharge;
	public void stockCharge(bool stockOrUnstock) {
		stockedCharge = stockOrUnstock;
		if (ownedByLocalPlayer) {
			RPC.playerToggle.sendRpc(player.id, stockOrUnstock ? RPCToggleType.StockCharge : RPCToggleType.UnstockCharge);
		}
	}

	public bool stockedXSaber;
	public void stockSaber(bool stockOrUnstock) {
		stockedXSaber = stockOrUnstock;
		if (ownedByLocalPlayer) {
			RPC.playerToggle.sendRpc(player.id, stockOrUnstock ? RPCToggleType.StockSaber : RPCToggleType.UnstockSaber);
		}
	}

	public bool stockedX3Buster;

	public float xSaberCooldown;
	public float stockedChargeFlashTime;

	public List<Trail> lastFiveTrailDraws = new List<Trail>();
	public LoopingSound chargeSound;

	//public ShaderWrapper possessedShader;
	//public ShaderWrapper acidShader;
	//public ShaderWrapper igShader;
	//public ShaderWrapper oilShader;
	//public ShaderWrapper infectedShader;
	//public ShaderWrapper frozenCastleShader;
	//public ShaderWrapper vaccineShader;
	//public ShaderWrapper darkHoldShader;

	public float headshotRadius {
		get {
			return 6f;
		}
	}

	public decimal damageSavings = 0;
	public decimal damageDebt = 0;

	public bool stopCamUpdate = false;
	public Anim warpBeam;
	public float flattenedTime;
	public float saberCooldown;

	public const float maxLastAttackerTime = 5;

	public float igFreezeProgress;
	public float freezeInvulnTime;
	public float stunInvulnTime;
	public float crystalizeInvulnTime;
	public float grabInvulnTime;
	public float darkHoldInvulnTime;

	public float limboRACheckCooldown;
	public RideArmor rideArmor;
	public RideChaser rideChaser;
	public Player lastGravityWellDamager;

	// Some things previously in other char files used by multiple characters.
	public int lastShootPressed;
	public int lastShootReleased;
	public int lastAttackFrame = -100;
	public int framesSinceLastAttack = 1000;
	public float grabCooldown;

	public RideArmor vileStartRideArmor;
	public RideArmor mk5RideArmorPlatform;
	public float calldownMechCooldown;
	public const float maxCalldownMechCooldown = 2;
	public bool alreadySummonedNewMech;

	// Was on Axl.cs before
	public int lastXDir;
	public Anim transformAnim;
	float transformSmokeTime;

	// For states with special propieties.
	public int specialState = 0;
	// For doublejump.
	public float lastJumpPressedTime;

	// For wallkick.
	public float wallKickTimer;
	public int wallKickDir;
	public float maxWallKickTime = 12;

	// Main character class starts here.
	public Character(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, bool mk2VileOverride = false, bool mk5VileOverride = false
	) : base(
		null, new Point(x, y), netId, ownedByLocalPlayer, dontAddToLevel: true
	) {
		this.player = player;
		this.xDir = xDir;
		initNetCharState1();
		initNetCharState2();

		isDashing = false;
		splashable = true;

		CharState initialCharState;

		if (ownedByLocalPlayer) {
			if (isWarpIn) initialCharState = new WarpIn();
			else initialCharState = new Idle();
		} else {
			initialCharState = new NetLimbo();
			useGravity = false;
		}

		spriteToCollider["roll"] = getDashingCollider();
		spriteToCollider["dash*"] = getDashingCollider();
		spriteToCollider["crouch*"] = getCrouchingCollider();
		spriteToCollider["ra_*"] = getRaCollider();
		spriteToCollider["rc_*"] = getRcCollider();
		spriteToCollider["warp_beam"] = null;
		spriteToCollider["warp_out"] = null;
		spriteToCollider["warp_in"] = null;
		spriteToCollider["revive"] = null;
		spriteToCollider["revive_to5"] = null;
		spriteToCollider["die"] = null;
		spriteToCollider["block"] = getBlockCollider();

		changeState(initialCharState);

		visible = isVisible;

		chargeTime = 0;
		chargeFlashTime = 0;
		useFrameProjs = true;

		chargeSound = new LoopingSound("charge_start", "charge_loop", this);

		if (this.player != Global.level.mainPlayer) {
			zIndex = ++Global.level.autoIncCharZIndex;
		} else {
			zIndex = ZIndex.MainPlayer;
		}

		Global.level.addGameObject(this);

		chargeEffect = new ChargeEffect();
	}

	public override void onStart() {
		base.onStart();
	}

	public float vaccineTime;
	public float vaccineHurtCooldown;
	public void addVaccineTime(float time) {
		if (!ownedByLocalPlayer) return;
		vaccineTime += time;
		if (vaccineTime > 8) vaccineTime = 8;
		if (charState is Frozen || charState is Crystalized || charState is Stunned) {
			changeToIdleOrFall();
		}
		burnTime = 0;
		acidTime = 0;
		oilTime = 0;
		player.possessedTime = 0;
	}
	public bool isVaccinated() { return vaccineTime > 0; }

	public float infectedTime;
	public Damager infectedDamager;
	public void addInfectedTime(Player attacker, float time) {
		if (!ownedByLocalPlayer) return;
		if (isInvulnerable()) return;
		if (isVaccinated()) return;

		Damager damager = new Damager(attacker, 0, 0, 0);
		if (infectedTime == 0 || infectedDamager == null) {
			infectedDamager = damager;
		} else if (infectedDamager.owner != damager.owner) return;
		infectedTime += time;
		if (infectedTime > 8) infectedTime = 8;
	}

	public void addDarkHoldTime(Player attacker, float time) {
		if (!ownedByLocalPlayer) return;
		if (isInvulnerable()) return;
		if (isVaccinated()) return;

		changeState(new DarkHoldState(this), true);
	}

	public float acidTime;
	public float acidHurtCooldown;
	public Damager acidDamager;
	public void addAcidTime(Player attacker, float time) {
		if (!ownedByLocalPlayer) return;
		if ((this as MegamanX)?.chargedRollingShieldProj != null) return;
		if (isInvulnerable()) return;
		if (isVaccinated()) return;

		Damager damager = new Damager(attacker, 0, 0, 0);
		if (acidTime == 0 || acidDamager == null) {
			acidDamager = damager;
		} else if (acidDamager.owner != damager.owner) return;
		acidHurtCooldown = 0.5f;
		acidTime += time;
		oilTime = 0;
		if (acidTime > 8) acidTime = 8;
	}

	public float oilTime;
	public Damager oilDamager;
	public void addOilTime(Player attacker, float time) {
		if (!ownedByLocalPlayer) return;
		if ((this as MegamanX)?.chargedRollingShieldProj != null) return;
		if (isInvulnerable()) return;
		if (isVaccinated()) return;

		Damager damager = new Damager(attacker, 0, 0, 0);
		if (oilTime == 0 || oilDamager == null) {
			oilDamager = damager;
		} else if (oilDamager.owner != damager.owner) return;
		oilTime += time;
		acidTime = 0;
		if (oilTime > 8) oilTime = 8;

		if (burnTime > 0) {
			float oldBurnTime = burnTime;
			burnTime = 0;
			addBurnTime(attacker, new FlameMOilWeapon(), oldBurnTime + 2);
			return;
		}
	}

	public float burnTime;
	public float burnEffectTime;
	public float burnHurtCooldown;
	public Damager burnDamager;
	public Weapon burnWeapon;
	public void addBurnTime(Player attacker, Weapon weapon, float time) {
		if (!ownedByLocalPlayer) return;
		if ((this as MegamanX)?.chargedRollingShieldProj != null) return;
		if (isInvulnerable()) return;
		if (isVaccinated()) return;

		Damager damager = new Damager(attacker, 0, 0, 0);
		if (burnTime == 0 || burnDamager == null) {
			burnDamager = damager;
			burnWeapon = weapon;
		} else if (burnDamager.owner != damager.owner) return;
		burnHurtCooldown = 0.5f;
		burnTime += time;
		if (oilTime > 0) {
			playSound("flamemOilBurn", sendRpc: true);
			damager.applyDamage(this, false, weapon, this, (int)ProjIds.Burn, overrideDamage: 2, overrideFlinch: Global.defFlinch);
			burnTime += oilTime;
			oilTime = 0;
		}
		if (burnTime > 8) burnTime = 8;
	}

	float igFreezeRecoveryCooldown = 0;
	public void addIgFreezeProgress(float amount, int freezeTime) {
		if (freezeInvulnTime > 0) return;
		if (charState is Frozen) return;
		if (isCCImmune()) return;
		if (isInvulnerable()) return;
		if (isVaccinated()) return;

		igFreezeProgress += amount;
		igFreezeRecoveryCooldown = 0;
		if (igFreezeProgress >= 4) {
			igFreezeProgress = 0;
			freeze(freezeTime);
		}
	}

	public bool isCStingInvisible() {
		if (!player.isX) return false;
		if (isInvisibleBS.getValue() == false) return false;
		return true;
	}

	public bool isCStingInvisibleGraphics() {
		if (!player.isX) return false;
		if (hasUltimateArmorBS.getValue() == true) return false;
		if (isInvisibleBS.getValue() == false) return false;
		return true;
	}

	public override List<ShaderWrapper> getShaders() {
		var shaders = new List<ShaderWrapper>();
		ShaderWrapper palette = null;

		// TODO: Send this to the respective classes.
		if (player.isX) {
			int index = player.weapon.index;
			if (index == (int)WeaponIds.GigaCrush || index == (int)WeaponIds.ItemTracer || index == (int)WeaponIds.AssassinBullet || index == (int)WeaponIds.Undisguise || index == (int)WeaponIds.UPParry) index = 0;
			if (index == (int)WeaponIds.HyperBuster && ownedByLocalPlayer) {
				index = player.weapons[player.hyperChargeSlot].index;
			}
			if (player.hasGoldenArmor()) index = 25;
			if (hasUltimateArmorBS.getValue()) index = 0;
			palette = player.xPaletteShader;

			if (!isCStingInvisibleGraphics()) {
				palette?.SetUniform("palette", index);
				palette?.SetUniform("paletteTexture", Global.textures["paletteTexture"]);
			} else {
				palette?.SetUniform("palette", (this as MegamanX).cStingPaletteIndex % 9);
				palette?.SetUniform("paletteTexture", Global.textures["cStingPalette"]);
			}
		} else if (this is Zero zero) {
			int paletteNum = 0;
			if (zero.blackZeroTime > 3) paletteNum = 1;
			else if (zero.blackZeroTime > 0) {
				int mod = MathInt.Ceiling(zero.blackZeroTime) * 2;
				paletteNum = (Global.frameCount % (mod * 2)) < mod ? 0 : 1;
			}
			palette = player.zeroPaletteShader;
			palette?.SetUniform("palette", paletteNum);
			if (!player.isZBusterZero()) {
				palette?.SetUniform("paletteTexture", Global.textures["hyperZeroPalette"]);
			} else {
				palette?.SetUniform("paletteTexture", Global.textures["hyperBusterZeroPalette"]);
			}
			if (isNightmareZeroBS.getValue()) {
				palette = player.nightmareZeroShader;
			}
		} else if (player.isAxl) {

		} else if (player.isViralSigma()) {
			int paletteNum = 6 - MathInt.Ceiling((player.health / player.maxHealth) * 6);
			if (sprite.name.Contains("_enter")) paletteNum = 0;
			palette = player.viralSigmaShader;
			palette?.SetUniform("palette", paletteNum);
			palette?.SetUniform("paletteTexture", Global.textures["paletteViralSigma"]);
		} else if (player.isSigma3()) {
			if (Global.isOnFrameCycle(8)) palette = player.sigmaShieldShader;
		}

		if (palette != null) shaders.Add(palette);

		if (player.isPossessed()) {
			player.possessedShader?.SetUniform("palette", 1);
			player.possessedShader?.SetUniform("paletteTexture", Global.textures["palettePossessed"]);
			shaders.Add(player.possessedShader);
		}

		if (isDarkHoldBS.getValue() && player.darkHoldShader != null) {
			// If we are not already being affected by a dark hold shader, apply it. Otherwise for a brief period,
			// victims will be double color inverted, appearing normal
			if (!Global.level.darkHoldProjs.Any(dhp => dhp.screenShader != null && dhp.inRange(this))) {
				shaders.Add(player.darkHoldShader);
			}
		}

		if (player.darkHoldShader != null) {
			// Invert the zero who used a dark hold so he appears to be drawn normally on top of it
			var myDarkHold = Global.level.darkHoldProjs.FirstOrDefault(dhp => dhp.owner == player);
			if (myDarkHold != null && myDarkHold.inRange(this)) {
				shaders.Add(player.darkHoldShader);
			}
		}

		if (acidTime > 0 && player.acidShader != null) {
			player.acidShader?.SetUniform("acidFactor", 0.25f + (acidTime / 8f) * 0.75f);
			shaders.Add(player.acidShader);
		}
		if (oilTime > 0 && player.oilShader != null) {
			player.oilShader?.SetUniform("oilFactor", 0.25f + (oilTime / 8f) * 0.75f);
			shaders.Add(player.oilShader);
		}
		if (vaccineTime > 0 && player.vaccineShader != null) {
			player.vaccineShader?.SetUniform("vaccineFactor", vaccineTime / 8f);
			//vaccineShader?.SetUniform("vaccineFactor", 1f);
			shaders.Add(player.vaccineShader);
		}
		if (igFreezeProgress > 0 && !sprite.name.Contains("frozen") && player.igShader != null) {
			player.igShader?.SetUniform("igFreezeProgress", igFreezeProgress / 4);
			shaders.Add(player.igShader);
		}
		if (infectedTime > 0 && player.infectedShader != null) {
			player.infectedShader?.SetUniform("infectedFactor", infectedTime / 8f);
			shaders.Add(player.infectedShader);
		} else if (player.isVile && isFrozenCastleActiveBS.getValue()) {
			shaders.Add(player.frozenCastleShader);
		}

		if (!isCStingInvisibleGraphics()) {
			if (renderEffects.ContainsKey(RenderEffectType.Invisible) && alpha == 1) {
				player.invisibleShader?.SetUniform("alpha", 0.33f);
				shaders.Add(player.invisibleShader);
			}
			// alpha float doesn't work if one or more shaders exist. So need to use the invisible shader instead
			else if (alpha < 1 && shaders.Count > 0) {
				player.invisibleShader?.SetUniform("alpha", alpha);
				shaders.Add(player.invisibleShader);
			}
		}

		return shaders;
	}

	public bool isInvisibleEnemy() {
		return player.alliance != Global.level.mainPlayer.alliance;
	}

	public void splashLaserKnockback(Point splashDeltaPos) {
		if (charState.invincible) return;
		if (isImmuneToKnockback()) return;

		if (isClimbingLadder()) {
			setFall();
		} else {
			float modifier = 1;
			if (grounded) modifier = 0.5f;
			if (charState is Crouch) modifier = 0.25f;
			var pushVel = splashDeltaPos.normalize().times(200 * modifier);
			xPushVel = pushVel.x;
			vel.y = pushVel.y;
		}
	}

	// Stuck in place and can't do any action but still can activate controls, etc.
	public virtual bool isSoftLocked() {
		if (charState is WarpOut) return true;
		if (player.currentMaverick != null) return true;
		//if (player.weapon is MaverickWeapon mw && mw.isMenuOpened) return true;
		return false;
	}

	public bool canTurn() {
		if (mk5RideArmorPlatform != null) {
			return false;
		}
		if (this is Vile vile && vile.isShootingLongshotGizmo) {
			return false;
		}
		return true;
	}

	public virtual bool canMove() {
		if (mk5RideArmorPlatform != null) {
			return false;
		}
		// TODO: Move this to axl.cs
		if (isAimLocked()) {
			return false;
		}
		if (isSoftLocked()) {
			return false;
		}
		return true;
	}

	public virtual bool canDash() {
		if (player.isAI && charState is Dash) return false;
		if (mk5RideArmorPlatform != null) return false;
		if (charState is WallKick wallKick && wallKick.stateTime < 0.25f) return false;
		if (isSoftLocked()) return false;
		if (isAttacking()) return false;
		return flag == null;
	}

	public virtual bool canJump() {
		if (mk5RideArmorPlatform != null) return false;
		if (isSoftLocked()) return false;
		return true;
	}

	public virtual bool canCrouch() {
		if (isSoftLocked() || isDashing) {
			return false;
		}
		return true;
	}

	public bool canAirDash() {
		return dashedInAir == 0 || (dashedInAir == 1 && player.isX && player.hasChip(0));
	}

	public bool canAirJump() {
		if (this is not Zero zero) {
			return false;
		}
		return dashedInAir == 0 || (dashedInAir == 1 && zero.isBlackZero2());
	}

	public virtual bool canWallClimb() {
		if (charState is ZSaberProjSwingState || charState is ZeroDoubleBuster) return false;
		if (mk5RideArmorPlatform != null) return false;
		if (isSoftLocked()) return false;
		if (charState is VileHover) {
			return !player.input.isHeld(Control.Jump, player);
		}
		return true;
	}

	public virtual bool canUseLadder() {
		if (charState is ZSaberProjSwingState || charState is ZeroDoubleBuster) return false;
		if (mk5RideArmorPlatform != null) return false;
		if (isSoftLocked()) return false;
		if (charState is VileHover) {
			return !player.input.isHeld(Control.Jump, player);
		}
		return true;
	}

	public bool canStartClimbLadder() {
		if (charState is ZSaberProjSwingState || charState is ZeroDoubleBuster) return false;
		return true;
	}

	public virtual bool canClimbLadder() {
		if (mk5RideArmorPlatform != null) {
			return false;
		}
		if (shootAnimTime > 0 ||
			isAttacking() ||
			isSoftLocked()
		) {
			return false;
		}
		return true;
	}

	public virtual bool canCharge() {
		return true;
	}

	public virtual bool canShoot() {
		if (isInvulnerableAttack()) {
			return false;
		}
		return true;
	}

	public virtual bool canChangeWeapons() {
		if (player.weapon is AssassinBullet && chargeTime > 0) return false;
		if (charState is ViralSigmaPossess) return false;
		if (charState is InRideChaser) return false;

		return true;
	}

	public bool canPickupFlag() {
		if (player.isPossessed()) return false;
		if (dropFlagCooldown > 0) return false;
		if (isInvulnerable()) return false;
		if (player.isDisguisedAxl) return false;
		if (isCCImmuneHyperMode()) return false;
		if (charState is Die || charState is VileRevive || charState is XReviveStart || charState is XRevive) return false;
		if (charState is WolfSigmaRevive || charState is WolfSigma || sprite.name.StartsWith("sigma_head")) return false;
		if (charState is ViralSigmaRevive || charState is ViralSigmaIdle || sprite.name.StartsWith("sigma2_viral")) return false;
		if (charState is KaiserSigmaRevive || Helpers.isOfClass(charState, typeof(KaiserSigmaBaseState)) || sprite.name.StartsWith("sigma3_kaiser")) return false;
		if (player.currentMaverick != null && player.isTagTeam()) return false;
		if (isWarpOut()) return false;
		return true;
	}

	public virtual bool isSoundCentered() {
		if (charState is WarpOut) {
			return false;
		}
		return true;
	}

	public bool isAimLocked() {
		if (!player.isAxl) return false;
		if (player.input.isPositionLocked(player) && Options.main.axlAimMode == 0) {
			return true;
		}
		if (Options.main.axlAimMode == 0 && !Options.main.moveInDiagAim && !isDashing &&
			(grounded || charState is Hover || player.input.isHeld(Control.Shoot, player) || player.input.isHeld(Control.Special1, player)) &&
			(player.input.isHeld(Control.Up, player) || player.input.isHeld(Control.Down, player))) {
			return true;
		}
		return false;
	}

	public virtual float getRunSpeed() {
		float runSpeed = Physics.WalkSpeed;
		if (player.isX) {
			if (charState is XHover) {
				runSpeed = Physics.WalkSpeed;
			}
		} else if (player.isVile && player.speedDevil) {
			runSpeed *= 1.1f;
		}
		return runSpeed * getRunDebuffs();
	}

	public float getRunDebuffs() {
		float runSpeed = 1;
		if (slowdownTime > 0) runSpeed *= 0.75f;
		if (igFreezeProgress >= 3) runSpeed *= 0.25f;
		else if (igFreezeProgress >= 2) runSpeed *= 0.75f;
		else if (igFreezeProgress >= 1) runSpeed *= 0.5f;
		return runSpeed;
	}

	public virtual float getDashSpeed() {
		if (flag != null || !isDashing) {
			return getRunSpeed();
		}
		float dashSpeed = 3.45f * 60f;

		if (charState is XHover) {
			dashSpeed *= 1.25f;
		} else if (player.isVile && player.speedDevil) {
			dashSpeed *= 1.1f;
		}
		return dashSpeed * getRunDebuffs();
	}

	public virtual float getJumpPower() {
		float jp = Physics.JumpSpeed;

		return jp * getJumpModifier();
	}

	public virtual float getJumpModifier() {
		float jp = 1;

		if (slowdownTime > 0) jp *= 0.75f;
		if (igFreezeProgress == 1) jp *= 0.75f;
		if (igFreezeProgress == 2) jp *= 0.5f;
		if (igFreezeProgress == 3) jp *= 0.25f;

		return jp;
	}

	public void hook(Projectile strikeChainProj) {
		bool isChargedStrikeChain = strikeChainProj is StrikeChainProj scp && scp.isCharged;
		bool flinch = (isChargedStrikeChain || strikeChainProj is WSpongeSideChainProj);
		changeState(new StrikeChainHooked(strikeChainProj, flinch), true);
	}

	// For terrain collision.
	public override Collider? getTerrainCollider() {
		if (physicsCollider == null) {
			return null;
		}
		float hSize = 30;
		if (sprite.name.Contains("crouch")) {
			hSize = 22;
		}
		if (sprite.name.Contains("dash")) {
			hSize = 22;
		}
		if (sprite.name.Contains("_ra_")) {
			hSize = 20;
		}
		return new Collider(
			new Rect(0f, 0f, 18, hSize).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, new Point(0, 0)
		);
	}

	public override Collider getGlobalCollider() {
		var rect = new Rect(0, 0, 18, 34);
		if (sprite.name.Contains("_ra_")) {
			rect.y2 = 20;
		}
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public virtual Collider getDashingCollider() {
		var rect = new Rect(0, 0, 18, 22);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public virtual Collider getCrouchingCollider() {
		var rect = new Rect(0, 0, 18, 22);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public virtual Collider getRaCollider() {
		var rect = new Rect(0, 0, 18, 15);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public virtual Collider getRcCollider() {
		var rect = new Rect(0, -20, 18, 0);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public Collider getSigmaHeadCollider() {
		var rect = new Rect(0, 0, 14, 20);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public virtual Collider getBlockCollider() {
		var rect = new Rect(0, 0, 18, 34);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override void preUpdate() {
		base.preUpdate();
		insideCharacter = false;
		changedStateInFrame = false;
		pushedByTornadoInFrame = false;
		lastXDir = xDir;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (other.myCollider?.flag == (int)HitboxFlag.Hitbox || other.myCollider?.flag == (int)HitboxFlag.None) return;

		var killZone = other.gameObject as KillZone;
		if (killZone != null) {
			if (charState is WolfSigmaRevive wsr) {
				stopMoving();
				useGravity = false;
				wsr.groundStart = true;
				return;
			}
			if (!killZone.killInvuln && player.isKaiserSigma()) return;
			if (!killZone.killInvuln && isInvisibleBS.getValue()) return;
			if (rideArmor != null && rideArmor.rideArmorState is RADropIn) return;
			killZone.applyDamage(this);
		}

		var character = other.gameObject as Character;
		if ((charState is Dash || charState is AirDash) &&
			character != null && character.isCrystalized &&
			character.player.alliance != player.alliance
		) {
			Damager.applyDamage(
				player, 3, 1f, Global.defFlinch, character, false,
				(int)WeaponIds.CrystalHunter, 20, this,
				(int)ProjIds.CrystalHunterDash
			);
		}

		// Move zone movement.
		if (other.gameObject is MoveZone moveZone) {
			if (moveZone.moveVel.x != 0) {
				xPushVel = moveZone.moveVel.x;
			}
			if (moveZone.moveVel.y != 0) {
				yPushVel = moveZone.moveVel.y;
			}
		}

		if (ownedByLocalPlayer && other.gameObject is Flag flag && flag.alliance != player.alliance) {
			if (!Global.level.players.Any(p => p != player && p.character?.flag == flag)) {
				if (charState is SpeedBurnerCharState || charState is SigmaWallDashState || charState is FSplasherState) {
					changeState(new Fall(), true);
				}
			}
		}
	}

	public List<Tuple<string, int>> lastDTInputs = new List<Tuple<string, int>>();
	public string holdingDTDash;
	const int doubleDashFrames = 20;
	public float slowdownTime;
	public float dropFlagProgress;
	public float dropFlagCooldown;
	public bool dropFlagUnlocked;
	long originalZIndex;
	bool viralOnce;

	public override void update() {
		if (charState is not InRideChaser) {
			camOffsetX = MathInt.Round(Helpers.lerp(camOffsetX, 0, 10));
		}

		Helpers.decrementTime(ref limboRACheckCooldown);
		Helpers.decrementTime(ref dropFlagCooldown);
		Helpers.decrementTime(ref parryCooldown);

		if (ownedByLocalPlayer && player.possessedTime > 0) {
			player.possesseeUpdate();
		}

		if (flag != null) {
			if (MathF.Abs(xPushVel) > 75) xPushVel = 75 * MathF.Sign(xPushVel);
			if (MathF.Abs(xSwingVel) > 75) xSwingVel = 75 * MathF.Sign(xSwingVel);
			if (vel.y < -350) vel.y = -350;

			// Used to prevent holding dash before taking flag from activating which is bad player experience
			if (!player.input.isHeld(Control.Dash, player)) {
				dropFlagUnlocked = true;
			}

			if (!canPickupFlag()) {
				if (Global.isHost || Global.serverClient == null) {
					dropFlag();
				}
			} else if (dropFlagUnlocked && dropFlagCooldown == 0 && player.input.isHeld(Control.Dash, player)) {
				dropFlagProgress += Global.spf;
				if (dropFlagProgress > 1) {
					dropFlagProgress = 0;
					dropFlagCooldown = 1;
					if (Global.isHost || Global.serverClient == null) {
						dropFlag();
					}
					RPC.actorToggle.sendRpc(netId, RPCActorToggleType.DropFlagManual);
				}
			} else {
				dropFlagProgress = 0;
			}
		} else {
			dropFlagProgress = 0;
			dropFlagUnlocked = false;
		}

		if (Global.level.gameMode.isTeamMode) {
			int alliance = player.alliance;
			// If this is an enemy disguised Axl, change the alliance
			if (player.alliance != Global.level.mainPlayer.alliance && player.isDisguisedAxl) {
				alliance = Global.level.mainPlayer.alliance;
			}

			removeRenderEffect(RenderEffectType.BlueShadow);
			removeRenderEffect(RenderEffectType.RedShadow);

			if (Global.level.teamNum == 2) {
				if (alliance == GameMode.blueAlliance) {
					addRenderEffect(RenderEffectType.BlueShadow);
				} else {
					addRenderEffect(RenderEffectType.RedShadow);
				}
			} else if (!player.isMainPlayer && alliance == Global.level.mainPlayer.alliance) {
				addRenderEffect(RenderEffectType.GreenShadow);
			}
		}

		if (isInvisibleBS.getValue() == true) {
			if (player.isAxl && Global.shaderWrappers.ContainsKey("stealthmode_blue") && Global.shaderWrappers.ContainsKey("stealthmode_red")) {
				if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance) addRenderEffect(RenderEffectType.StealthModeRed);
				else addRenderEffect(RenderEffectType.StealthModeBlue);
				removeRenderEffect(RenderEffectType.BlueShadow);
				removeRenderEffect(RenderEffectType.RedShadow);
			} else {
				addRenderEffect(RenderEffectType.Invisible);
			}
		} else {
			if (player.isAxl && Global.shaderWrappers.ContainsKey("stealthmode_blue") && Global.shaderWrappers.ContainsKey("stealthmode_red")) {
				if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance) removeRenderEffect(RenderEffectType.StealthModeRed);
				else removeRenderEffect(RenderEffectType.StealthModeBlue);
			} else {
				removeRenderEffect(RenderEffectType.Invisible);
			}
		}

		if (Global.level.mainPlayer.readyTextOver) {
			Helpers.decrementTime(ref invulnTime);
		}

		if (vaccineTime > 0) {
			oilTime = 0;
			burnTime = 0;
			acidTime = 0;
			infectedTime = 0;
			vaccineTime -= Global.spf;
			if (vaccineTime <= 0) {
				vaccineTime = 0;
			}
		}

		if (infectedTime > 0) {
			infectedTime -= Global.spf;
			if (infectedTime <= 0) {
				infectedTime = 0;
			}
		}

		if (oilTime > 0) {
			oilTime -= Global.spf;
			if (isUnderwater() || charState.invincible || isCCImmune()) {
				oilTime = 0;
			}
			if (oilTime <= 0) {
				oilTime = 0;
			}
		}

		if (acidTime > 0) {
			acidTime -= Global.spf;
			acidHurtCooldown += Global.spf;
			if (acidHurtCooldown > 1) {
				acidHurtCooldown = 0;
				acidDamager?.applyDamage(this, player.weapon is TunnelFang, new AcidBurst(), this, (int)ProjIds.AcidBurstPoison, overrideDamage: 1f);
				new Anim(getCenterPos().addxy(0, -20), "torpedo_smoke", 1, null, true) { vel = new Point(0, -50) };
			}
			if (isUnderwater() || charState.invincible || isCCImmune()) {
				acidTime = 0;
			}
			if (acidTime <= 0) {
				removeAcid();
			}
		}

		if (burnTime > 0) {
			burnTime -= Global.spf;
			burnHurtCooldown += Global.spf;
			burnEffectTime += Global.spf;
			if (burnEffectTime > 0.1f) {
				burnEffectTime = 0;

				Point burnPos = pos.addxy(0, -10);
				bool hiding = false;
				if (charState is InRideArmor inRideArmor) {
					if (inRideArmor.isHiding) {
						burnPos = pos.addxy(0, 0);
						hiding = true;
					}
				}

				var f1 = new Anim(burnPos.addRand(5, 10), "burn_flame", 1, null, true, host: this);
				if (hiding) f1.setzIndex(zIndex - 100);

				if (burnTime > 2) {
					var f2 = new Anim(burnPos.addRand(5, 10), "burn_flame", 1, null, true, host: this);
					if (hiding) f2.setzIndex(zIndex - 100);
				}
				if (burnTime > 4) {
					var f3 = new Anim(burnPos.addRand(5, 10), "burn_flame", 1, null, true, host: this);
					if (hiding) f3.setzIndex(zIndex - 100);
				}
				if (burnTime > 6) {
					var f4 = new Anim(burnPos.addRand(5, 10), "burn_flame", 1, null, true, host: this);
					if (hiding) f4.setzIndex(zIndex - 100);
				}
			}
			if (burnHurtCooldown > 1) {
				burnHurtCooldown = 0;
				burnDamager?.applyDamage(this, false, burnWeapon, this, (int)ProjIds.Burn, overrideDamage: 1f);
			}
			if (isUnderwater() || charState.invincible || isCCImmune()) {
				burnTime = 0;
			}
			if (charState is Frozen) {
				burnTime = 0;
			}
			if (burnTime <= 0) {
				removeBurn();
			}
		}

		if (flattenedTime > 0 && !(charState is Die)) {
			flattenedTime -= Global.spf;
			if (flattenedTime < 0) flattenedTime = 0;
		}

		if (isHyperSigmaBS.getValue() || isHyperXBS.getValue()) {
			flattenedTime = 0;
		}
		Helpers.decrementTime(ref slowdownTime);

		if (!ownedByLocalPlayer) {
			if (isCharging()) {
				chargeLogic();
			} else {
				stopCharge();
			}
		}

		updateProjectileCooldown();

		igFreezeRecoveryCooldown += Global.spf;
		if (igFreezeRecoveryCooldown > 0.2f) {
			igFreezeRecoveryCooldown = 0;
			igFreezeProgress--;
			if (igFreezeProgress < 0) igFreezeProgress = 0;
		}
		Helpers.decrementTime(ref freezeInvulnTime);
		Helpers.decrementTime(ref stunInvulnTime);
		Helpers.decrementTime(ref crystalizeInvulnTime);
		Helpers.decrementTime(ref grabInvulnTime);
		Helpers.decrementTime(ref darkHoldInvulnTime);

		if (flag != null && flag.ownedByLocalPlayer) {
			flag.changePos(getCenterPos());
		}

		if (!Global.level.hasGameObject(vileStartRideArmor)) {
			vileStartRideArmor = null;
		}

		if (transformAnim != null) {
			transformSmokeTime += Global.spf;
			if (transformSmokeTime > 0) {
				int width = 15;
				int height = 25;
				transformSmokeTime = 0;
				new Anim(getCenterPos().addxy(Helpers.randomRange(-width, width), Helpers.randomRange(-height, height)), "torpedo_smoke", xDir, null, true);
				new Anim(getCenterPos().addxy(Helpers.randomRange(-width, width), Helpers.randomRange(-height, height)), "torpedo_smoke", xDir, null, true);
				new Anim(getCenterPos().addxy(Helpers.randomRange(-width, width), Helpers.randomRange(-height, height)), "torpedo_smoke", xDir, null, true);
				new Anim(getCenterPos().addxy(Helpers.randomRange(-width, width), Helpers.randomRange(-height, height)), "torpedo_smoke", xDir, null, true);
			}

			transformAnim.changePos(pos);
			if (transformAnim.destroyed) {
				transformAnim = null;
			}
		}

		if (stockedCharge) {
			addRenderEffect(RenderEffectType.ChargePink, 0.033333f, 0.1f);
		}
		if (stockedXSaber) {
			addRenderEffect(RenderEffectType.ChargeGreen, 0.05f, 0.1f);
		}
		if (stockedX3Buster) {
			if (player.weapon is not Buster) {
				stockedX3Buster = false;
			} else {
				addRenderEffect(RenderEffectType.ChargeOrange, 0.05f, 0.1f);
			}
		}

		/*
		if (!ownedByLocalPlayer || player.isAI)
		{
			if (isInvisibleBS.getValue() && player.alliance != Global.level.mainPlayer.alliance)
			{
				alpha -= Global.spf * 4;
				if (alpha < 0) alpha = 0;
				removeRenderEffect(RenderEffectType.StockedCharge);
				removeRenderEffect(RenderEffectType.StockedSaber);
			}
			else
			{
				alpha += Global.spf * 4;
				if (alpha > 1) alpha = 1;
			}
		}
		*/
		// Cutoff point for things that run but aren't owned by the player
		if (!ownedByLocalPlayer) {
			base.update();

			if (sprite.name.Contains("sigma2_viral")) {
				if (!viralOnce) {
					viralOnce = true;
					xScale = 0;
					yScale = 0;
					originalZIndex = zIndex;
				}

				if (sprite.name.Contains("sigma2_viral_possess")) {
					setzIndex(ZIndex.Actor);
				} else {
					setzIndex(originalZIndex);
				}
			}

			return;
		}

		if (player.isVile && !player.isAI && !player.isDisguisedAxl && player.getVileWeightActive() > VileLoadout.maxWeight && charState is not WarpIn && charState is not Die) {
			applyDamage(null, null, Damager.envKillDamage, null);
			return;
		}

		updateParasite();

		if (beeSwarm != null) {
			beeSwarm.update();
		}

		if (stingChargeTime > 0) {
			if (player.isX) {
				stingChargeTime -= Global.spf;

				player.weapon.ammo -= (Global.spf * 3 * (player.hasChip(3) ? 0.5f : 1));
				if (player.weapon.ammo < 0) player.weapon.ammo = 0;
				stingChargeTime = player.weapon.ammo;
			} else {
				stingChargeTime -= Global.spf;
			}
			if (stingChargeTime <= 0) {
				player.delaySubtank();
				stingChargeTime = 0;
			}
		}

		if (pos.y > Global.level.killY && !isWarpIn() && charState is not WarpOut) {
			if (charState is WolfSigmaRevive wsr) {
				stopMoving();
				useGravity = false;
				wsr.groundStart = true;
			} else {
				if (charState is not Die) {
					incPos(new Point(0, 25));
				}
				applyDamage(null, null, Damager.envKillDamage, null);
			}
		}

		if (player.health >= player.maxHealth) {
			healAmount = 0;
			usedSubtank = null;
		}
		if (healAmount > 0 && player.health > 0) {
			healTime += Global.spf;
			if (healTime > 0.05) {
				healTime = 0;
				healAmount--;
				if (usedSubtank != null) {
					usedSubtank.health--;
				}
				player.health = Helpers.clampMax(player.health + player.getHealthModifier(), player.maxHealth);
				if (acidTime > 0) {
					acidTime--;
					if (acidTime < 0) removeAcid();
				}
				if (player == Global.level.mainPlayer || playHealSound) {
					playSound("heal", forcePlay: true, sendRpc: true);
				}
			}
		} else {
			playHealSound = false;
		}

		if (usedSubtank != null && usedSubtank.health <= 0) {
			usedSubtank = null;
		}

		if (ai != null) {
			ai.update();
		}

		if (slideVel != 0) {
			slideVel = Helpers.toZero(slideVel, Global.spf * 350, Math.Sign(slideVel));
			move(new Point(slideVel, 0), true);
		}
		base.update();

		// For G. Well damage.
		// This is calculated after the base update to prevent acidental double damage.
		if (vel.y < 0 && Global.level.checkCollisionActor(this, 0, -1) != null) {
			if (gravityWellModifier < 0 && vel.y < -300) {
				Damager.applyDamage(
					lastGravityWellDamager,
					4, 0.5f, Global.halfFlinch, this,
					false, (int)WeaponIds.GravityWell, 45, this,
					(int)ProjIds.GravityWellCharged
				);
			}
			vel.y = 0;
		}

		// This overrides the ground checks made by Actor.update();
		if (mk5RideArmorPlatform != null) {
			changePos(mk5RideArmorPlatform.getMK5Pos().addxy(0, 1));
			xDir = mk5RideArmorPlatform.xDir;
			grounded = true;

			if (mk5RideArmorPlatform.destroyed) {
				mk5RideArmorPlatform = null;
			}
		}

		if (player.isDisguisedAxl) {
			updateDisguisedAxl();
		}

		updateCtrl();
	}

	public override void stateUpdate() {
		charState.update();
	}

	public override void statePostUpdate() {
		base.statePostUpdate();
		charState.frameTime += 1f * Global.speedMul;
	}

	public virtual bool updateCtrl() {
		if (!ownedByLocalPlayer) {
			return false;
		}
		if (charState.exitOnLanding && grounded) {
			charState.landingCode();
		}
		if (charState.exitOnAirborne && !grounded) {
			changeState(new Fall());
		}
		if (canWallClimb() && !grounded &&
			(charState.airMove && vel.y > 0 || charState is WallSlide) &&
			wallKickTimer <= 0 &&
			player.input.isPressed(Control.Jump, player) &&
			(charState.wallKickLeftWall != null || charState.wallKickRightWall != null)
		) {
			if (player.input.isHeld(Control.Dash, player) &&
				(charState.useDashJumpSpeed || charState is WallSlide)
			) {
				isDashing = true;
			}
			vel.y = -getJumpPower();
			wallKickDir = 0;
			if (charState.wallKickLeftWall != null) {
				wallKickDir += 1;
			}
			if (charState.wallKickRightWall != null) {
				wallKickDir -= 1;
			}
			if (wallKickDir == 0) {
				if (charState.lastLeftWall != null) {
					wallKickDir += 1;
				}
				if (charState.lastRightWall != null) {
					wallKickDir -= 1;
				}
			}
			if (wallKickDir != 0) {
				xDir = -wallKickDir;
			}
			wallKickTimer = maxWallKickTime;
			changeState(new WallKick(), true);
			var wallSparkPoint = pos.addxy(12 * xDir, 0);
			var rect = new Rect(wallSparkPoint.addxy(-2, -2), wallSparkPoint.addxy(2, 2));
			if (Global.level.checkCollisionShape(rect.getShape(), null) != null) {
				new Anim(wallSparkPoint, "wall_sparks", xDir,
					player.getNextActorNetId(), true, sendRpc: true
				);
			}
			return true;
		}
		if (charState.canStopJump &&
			!grounded && vel.y < 0 &&
			!player.input.isHeld(Control.Jump, player)
		) {
			vel.y = 0;
		}
		if (charState.airMove && !grounded) {
			airMove();
		}
		if (charState.normalCtrl) {
			normalCtrl();
		}
		if (charState.attackCtrl) {
			return attackCtrl();
		}
		return false;
	}

	// For trastion between the normal states.
	public virtual bool normalCtrl() {
		// Ladder check.
		if (canUseLadder() && canStartClimbLadder()) {
			charState.checkLadder(grounded);
			if (charState is LadderClimb) {
				return true;
			}
		}
		// Ground normal states.
		if (grounded) {
			if (player.input.isPressed(Control.Jump, player) && canJump()) {
				vel.y = -getJumpPower();
				isDashing = (
					isDashing || player.dashPressed(out string dashControl) && canDash()
				);
				changeState(new Jump());
				return true;
			} else if (player.dashPressed(out string dashControl) && canDash() && charState is not Dash) {
				changeState(new Dash(dashControl), true);
				return true;
			} else if (mk5RideArmorPlatform != null &&
				  player.input.isPressed(Control.Jump, player) &&
				  player.input.isHeld(Control.Up, player) &&
				  canEjectFromRideArmor()
			  ) {
				getOffMK5Platform();
				return true;
			}
			if (player.isCrouchHeld() && canCrouch() && charState is not Crouch) {
				changeState(new Crouch());
				return true;
			}
			if (player.input.isPressed(Control.Taunt, player)) {
				changeState(new Taunt());
				return true;
			}
		}
		// Air normal states.
		else {
			if (player.dashPressed(out string dashControl) && canAirDash() && canDash()) {
				if (!isDashing) {
					changeState(new AirDash(dashControl));
					return true;
				}
			}
			if (canAirJump()) {
				if (player.input.isPressed(Control.Jump, player) && canJump()) {
					lastJumpPressedTime = Global.time;
				}
				if ((player.input.isPressed(Control.Jump, player) ||
					Global.time - lastJumpPressedTime < 0.1f) &&
					!isDashing && wallKickTimer <= 0 && flag == null &&
					!sprite.name.Contains("kick_air")
				) {
					dashedInAir++;
					vel.y = -getJumpPower();
					changeState(new Jump(), true);
					return true;
				}
			} else {
				lastJumpPressedTime = 0;
			}
			// Wallclimb code.
			if (canWallClimb() && charState is not WallSlide && wallKickTimer <= 0) {
				bool velYRequirementMet = vel.y > 0 || (charState is VileHover vh && vh.fallY > 0);
				// This logic can be abit confusing,
				// but we are trying to mirror the actual Mega man X wall climb physics.
				// In the actual game, X will not initiate a climb
				// if you directly hugging a wall, jump and push in its direction
				// UNTIL you start falling OR you move away and jump into it
				int dpadXDir = player.input.getXDir(player);

				if (dpadXDir == -1 && velYRequirementMet && charState.lastLeftWall != null) {
					changeState(new WallSlide(-1, charState.lastLeftWallCollider));
					return true;
				}
				if (dpadXDir == 1 && velYRequirementMet && charState.lastRightWall != null) {
					changeState(new WallSlide(1, charState.lastRightWallCollider));
					return true;
				}
			}
		}
		return false;
	}

	public virtual void airMove() {
		int xDpadDir = player.input.getXDir(player);
		bool wallKickMove = (wallKickTimer > 0);
		if (wallKickMove) {
			if (wallKickDir == xDpadDir || vel.y > 0) {
				wallKickMove = false;
				wallKickTimer = 0;
			} else {
				float kickSpeed;
				if (isDashing) {
					kickSpeed = 200 * (wallKickTimer / 12);
				} else {
					kickSpeed = 150 * (wallKickTimer / 12);
				}
				move(new Point(kickSpeed * wallKickDir, 0));
			}
			wallKickTimer -= 1 * Global.speedMul;
		}
		if (!wallKickMove && xDpadDir != 0) {
			Point moveSpeed = new Point();
			if (canMove()) { moveSpeed.x = getDashSpeed() * xDpadDir; }
			if (canTurn()) { xDir = xDpadDir; }
			if (moveSpeed.magnitude > 0) { move(moveSpeed); }
		}
	}

	public virtual bool attackCtrl() {
		return false;
	}

	public void removeAcid() {
		acidTime = 0;
		acidHurtCooldown = 0;
	}

	public void removeBurn() {
		burnTime = 0;
		burnHurtCooldown = 0;
	}

	public virtual bool canEnterRideArmor() {
		if (rideArmor != null || rideChaser != null) {
			return false;
		}
		if (charState is not Fall fall || fall.limboVehicleCheckTime > 0) {
			return false;
		}
		return true;
	}

	public bool canEnterRideChaser() {
		return charState is Fall fall && rideArmor == null && rideChaser == null && fall.limboVehicleCheckTime == 0;
	}

	public bool isSpawning() {
		return sprite.name.Contains("warp_in") || !visible || (player.isVile && isInvulnBS.getValue());
	}

	public Point getCharRideArmorPos() {
		if (rideArmor.currentFrame.POIs.Count == 0) return new Point();
		var charPos = rideArmor.currentFrame.POIs[0];
		charPos.x *= xDir;
		return charPos;
	}

	public Point getMK5RideArmorPos() {
		if (mk5RideArmorPlatform.currentFrame.POIs.Count == 0) return new Point();
		var charPos = mk5RideArmorPlatform.currentFrame.POIs[0];
		charPos.x *= xDir;
		return charPos;
	}

	public bool isSpriteDash(string spriteName) {
		return spriteName.Contains("dash") && !spriteName.Contains("up_dash");
	}

	public override void changeSprite(string spriteName, bool resetFrame) {
		if (!isHeadbuttSprite(sprite?.name) && isHeadbuttSprite(spriteName)) {
			headbuttAirTime = Global.spf;
		}
		if (isHeadbuttSprite(sprite?.name) && !isHeadbuttSprite(spriteName)) {
			headbuttAirTime = 0;
		}
		base.changeSprite(spriteName, resetFrame);
	}

	public bool isHeadbuttSprite(string sprite) {
		if (sprite == null) return false;
		return sprite.EndsWith("jump") || sprite.EndsWith("up_dash") || sprite.EndsWith("wall_kick");
	}

	public void unfreezeIfFrozen() {
		if (charState is Frozen) {
			changeState(new Idle(), true);
		}
	}

	public void freeze(int timeToFreeze = 5) {
		if ((this as MegamanX)?.chargedRollingShieldProj != null) return;
		if (charState.stunResistant) return;
		if (charState is Frozen) return;

		changeState(new Frozen(timeToFreeze), true);
	}

	public bool canCrystalize() {
		if ((this as MegamanX)?.chargedRollingShieldProj != null) return false;
		if (charState.stunResistant) return false;
		if (isCrystalized) return false;
		return true;
	}

	public void chargeLogic() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			int chargeType = 0;
			if (this is BusterZero) {
				chargeType = 1;
			} else if (player.isX && player.hasArmArmor(3)) {
				if (player.hasGoldenArmor()) {
					chargeType = 2;
				}
			}
			if (!sprite.name.Contains("ra_hide")) {
				int level = getChargeLevel();
				var renderGfx = RenderEffectType.ChargeBlue;
				renderGfx = level switch {
					1 => RenderEffectType.ChargeBlue,
					2 => RenderEffectType.ChargeYellow,
					3 when (chargeType == 2) => RenderEffectType.ChargeOrange,
					3 => RenderEffectType.ChargePink,
					_ when (chargeType == 1) => RenderEffectType.ChargeGreen,
					_ => RenderEffectType.ChargeOrange
				};
				addRenderEffect(renderGfx, 0.033333f, 0.1f);
			}
			chargeEffect.update(getChargeLevel(), chargeType);
		}
	}

	public bool isCCImmune() {
		//if (isAwakenedZeroBS.getValue() && isAwakenedGenmuZeroBS.getValue()) return true;
		//if (isHyperSigmaBS.getValue()) return true;
		//return false;
		return isCCImmuneHyperMode();
	}

	public bool isCCImmuneHyperMode() {
		// The former two hyper modes rely on a float time value sync.
		// The latter two hyper modes are boolean states so use the BoolState ("BS") system.
		return isAwakenedGenmuZeroBS.getValue() || (isInvisibleBS.getValue() && player.isAxl) || isHyperSigmaBS.getValue() || isHyperXBS.getValue();
	}

	public virtual bool isToughGuyHyperMode() {
		return false;
	}

	public bool isImmuneToKnockback() {
		return charState?.immuneToWind == true || immuneToKnockback || isCCImmune();
	}

	public bool isNonCCImmuneHyperMode() {
		return sprite.name.Contains("vilemk2") || player.hasGoldenArmor() || hasUltimateArmorBS.getValue();
	}

	// If factorHyperMode = true, then invuln frames in a hyper mode won't count as "invulnerable".
	// This is to allow the hyper mode start invulnerability to still be able to do things without being impeded
	// and should be set only by code that is checking to see if such things can be done.
	public bool isInvulnerable(bool ignoreRideArmorHide = false, bool factorHyperMode = false) {
		if (isWarpIn()) return true;
		if (!factorHyperMode && isInvulnBS.getValue()) return true;
		if (factorHyperMode && isInvulnBS.getValue() && !isCCImmuneHyperMode()) return true;
		if (!ignoreRideArmorHide && charState is InRideArmor && (charState as InRideArmor).isHiding) return true;
		if (!ignoreRideArmorHide && !string.IsNullOrEmpty(sprite?.name) && sprite.name.Contains("ra_hide")) return true;
		if (specialState == (int)SpecialStateIds.AxlRoll ||
			specialState == (int)SpecialStateIds.XTeleport
		) {
			return true;
		}
		if (sprite.name.Contains("viral_exit")) return true;
		if (charState is WarpOut) return true;
		if (charState is WolfSigmaRevive || charState is ViralSigmaRevive || charState is KaiserSigmaRevive) return true;
		return false;
	}

	public bool isWarpIn() {
		return charState is WarpIn || sprite.name.EndsWith("warp_in");
	}

	public bool isWarpOut() {
		return charState is WarpOut || sprite.name.EndsWith("warp_beam");
	}

	public bool isInvulnerableAttack() {
		return isInvulnerable(factorHyperMode: true);
	}

	public bool isSpriteInvulnerable() {
		return sprite.name == "mmx_gigacrush" || sprite.name == "zero_hyper_start" || sprite.name == "axl_hyper_start" || sprite.name == "zero_rakuhouha" ||
			sprite.name == "zero_rekkoha" || sprite.name == "zero_cflasher" || sprite.name.Contains("vile_revive") || sprite.name.Contains("warp_out") || sprite.name.Contains("nova_strike");
	}

	public bool isCStingVulnerable(int projId) {
		return isInvisibleBS.getValue() && player.isX && Damager.isBoomerang(projId);
	}

	public bool canBeGrabbed() {
		return grabInvulnTime == 0 && !isCCImmune() && charState is not DarkHoldState;
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		if (isInvulnerable()) return false;
		if (isDeathOrReviveSprite()) return false;
		if (Global.level.gameMode.setupTime > 0) return false;
		if (Global.level.isRace()) {
			bool isAxlSelfDamage = player.isAxl && damagerAlliance == player.alliance;
			if (!isAxlSelfDamage) return false;
		}

		// Bommerang can go thru invisibility check
		if (player.alliance != damagerAlliance && projId != null && isCStingVulnerable(projId.Value)) {
			return true;
		}

		if (isInvisibleBS.getValue()) return false;

		// Self damaging projIds can go thru alliance check
		bool isSelfDamaging =
			projId == (int)ProjIds.BlastLauncherSplash ||
			projId == (int)ProjIds.GreenSpinnerSplash ||
			projId == (int)ProjIds.NecroBurst ||
			projId == (int)ProjIds.SniperMissileBlast ||
			projId == (int)ProjIds.SpeedBurnerRecoil;

		if (isSelfDamaging && damagerPlayerId == player.id) {
			return true;
		}

		if (player.alliance == damagerAlliance) return false;

		return true;
	}

	public bool isDeathOrReviveSprite() {
		if (sprite.name == "sigma_head_intro") return true;
		if (sprite.name.EndsWith("die")) return true;
		if (sprite.name.Contains("revive")) return true;
		return false;
	}

	public bool isInvincible(Player attacker, int? projId) {
		if (ownedByLocalPlayer) {
			return charState.invincible || genmuImmune(attacker);
		} else {
			return isSpriteInvulnerable() || genmuImmune(attacker);
		}
	}

	public int getShootXDirSynced() {
		int xDir = this.xDir;
		if (sprite.name.Contains("_wall_slide")) xDir *= -1;
		return xDir;
	}

	public int getShootXDir() {
		int xDir = this.xDir;
		if (charState is WallSlide) xDir *= -1;
		return xDir;
	}

	public bool isStealthy(int alliance) {
		if (player.alliance == alliance) return false;
		if (isInvisibleBS.getValue()) return true;
		if (player.isDisguisedAxl) return true;
		return false;
	}


	public Point getDashSparkEffectPos(int xDir) {
		return getDashDustEffectPos(xDir).addxy(6 * xDir, 4);
	}

	public Point getDashDustEffectPos(int xDir) {
		float dashXPos = -24;
		if (player.isVile) dashXPos = -30;
		if (player.isSigma1AndSigma()) dashXPos = -35;
		if (player.isSigma2AndSigma()) dashXPos = -35;
		if (player.isSigma3AndSigma()) dashXPos = -35;
		return pos.addxy(dashXPos * xDir + (5 * xDir), -4);
	}

	public override Point getCenterPos() {
		if (player.isSigma) {
			if (player.isWolfSigma()) return pos.addxy(0, -7);
			else if (player.isViralSigma()) return pos.addxy(0, 0);
			return pos.addxy(0, -32);
		}
		return pos.addxy(0, -18);
	}

	public virtual Point getAimCenterPos() {
		if (sprite.name.Contains("_ra_")) {
			return pos.addxy(0, -10);
		}
		if (player.isSigma) {
			if (player.isKaiserSigma() && !player.isKaiserViralSigma()) {
				return pos.addxy(13 * xDir, -95);
			}
			return getCenterPos();
		}
		return pos.addxy(0, -18);
	}

	public Point getParasitePos() {
		float yOff = -18;
		if (sprite.name.Contains("_ra_")) {
			float hideY = 0;
			if (sprite.name.Contains("_ra_hide")) {
				hideY = 22 * ((float)sprite.frameIndex / sprite.frames.Count);
			}
			yOff = -6 + hideY;
		} else if (player.isZero) yOff = -20;
		else if (player.isVile) yOff = -24;
		if (player.isSigma) {
			return getCenterPos();
		} else if (player.isAxl) yOff = -18;

		return pos.addxy(0, yOff);
	}

	public int camOffsetX;


	public virtual Actor getFollowActor() {
		if (mk5RideArmorPlatform != null) {
			return mk5RideArmorPlatform;
		}
		if (player.currentMaverick != null && player.isTagTeam()) {
			return player.currentMaverick;
		}
		if (rideArmor != null) {
			return rideArmor;
		}
		return this;
	}

	public virtual Point getCamCenterPos(bool ignoreZoom = false) {
		if (mk5RideArmorPlatform != null) {
			return mk5RideArmorPlatform.pos.round().addxy(0, -70);
		}
		if (player.isSigma) {
			var maverick = player.currentMaverick;
			if (maverick != null && player.isTagTeam()) {
				if (maverick.state is MEnter me) {
					return me.getDestPos().round().addxy(camOffsetX, -24);
				}
				if (maverick.state is MorphMCHangState hangState) {
					return maverick.pos.addxy(camOffsetX, -24 + 17);
				}
				return maverick.pos.round().addxy(camOffsetX, -24);
			}

			if (player.isViralSigma()) {
				return pos.round().addxy(camOffsetX, 25);
			}

			if (player.isKaiserSigma()) {
				if (sprite.name.StartsWith("sigma3_kaiser_virus")) return pos.addxy(camOffsetX, -12);
				return pos.round().addxy(camOffsetX, -55);
			}

			if (player.weapon is WolfSigmaHandWeapon handWeapon && handWeapon.hand.isControlling) {
				var hand = handWeapon.hand;
				Point camCenter = sigmaHeadGroundCamCenterPos.Value;
				if (hand.pos.x > camCenter.x + Global.halfScreenW || hand.pos.x < camCenter.x - Global.halfScreenW || hand.pos.y > camCenter.y + Global.halfScreenH || hand.pos.y < camCenter.y - Global.halfScreenH) {
					float overFactorX = MathF.Abs(hand.pos.x - camCenter.x) - Global.halfScreenW;
					if (overFactorX > 0) {
						float remainder = overFactorX - Global.halfScreenW;
						int sign = MathF.Sign(hand.pos.x - camCenter.x);
						camCenter.x += Math.Min(overFactorX, Global.halfScreenW) * sign * 2;
						camCenter.x += Math.Max(remainder, 0) * sign;
					}

					float overFactorY = MathF.Abs(hand.pos.y - camCenter.y) - Global.halfScreenH;
					if (overFactorY > 0) {
						float remainder = overFactorY - Global.halfScreenH;
						int sign = MathF.Sign(hand.pos.y - camCenter.y);
						camCenter.y += Math.Min(overFactorY, Global.halfScreenH) * sign * 2;
						camCenter.y += Math.Max(remainder, 0) * sign;
					}

					return camCenter.round();
				}
			}

			if (sigmaHeadGroundCamCenterPos != null) {
				return sigmaHeadGroundCamCenterPos.Value;
			}
		}
		if (rideArmor != null) {
			if (ownedByLocalPlayer && rideArmor.rideArmorState is RADropIn) {
				return (rideArmor.rideArmorState as RADropIn).spawnPos.addxy(0, -24);
			}
			return rideArmor.pos.round().addxy(camOffsetX, -24);
		}
		return pos.round().addxy(camOffsetX, -30);
	}

	public Point? getHeadPos() {
		if (currentFrame?.headPos == null) return null;
		return pos.addxy(currentFrame.headPos.Value.x * xDir, currentFrame.headPos.Value.y - 2);
	}

	public Rect getHeadRect() {
		Point headPos = getHeadPos().Value;
		float topY = float.MaxValue;
		float leftX = float.MaxValue;
		float rightX = float.MinValue;
		if (standartCollider != null) {
			topY = standartCollider.shape.getRect().y1 - 1;
			//leftX = standartCollider.shape.getRect().x1 - 1;
			//rightX = standartCollider.shape.getRect().x2 + 1;
		}

		return new Rect(
			Math.Min(leftX, headPos.x - headshotRadius),
			Math.Min(topY, headPos.y - headshotRadius),
			Math.Max(rightX, headPos.x + headshotRadius),
			headPos.y + headshotRadius
		);
	}

	public Actor abstractedActor() {
		if (rideArmor != null) return rideArmor;
		return this;
	}

	public void setFall() {
		changeState(new Fall());
	}

	public bool isClimbingLadder() {
		return charState is LadderClimb;
	}

	public void addAI() {
		ai = new AI(this);
	}

	public void drawCharge() {
	}

	public bool isCharging() {
		return chargeTime >= charge1Time;
	}

	public Point getShootPos() {
		var busterOffsetPos = currentFrame.getBusterOffset();
		if (busterOffsetPos == null) {
			return getCenterPos();
		}
		var busterOffset = (Point)busterOffsetPos;
		if (player.isX && player.armArmorNum == 3 && sprite.needsX3BusterCorrection()) {
			if (busterOffset.x > 0) busterOffset.x += 4;
			else if (busterOffset.x < 0) busterOffset.x -= 4;
		}
		busterOffset.x *= xDir;
		if (player.weapon is RollingShield && charState is Dash) {
			busterOffset.y -= 2;
		}
		return pos.add(busterOffset);
	}

	public void stopCharge() {
		if (chargeEffect == null) return;
		chargeEffect.reset();
		chargeTime = 0;
		chargeFlashTime = 0;
		chargeSound.stop();
		chargeSound.reset();
		chargeEffect.stop();
	}

	public virtual string getSprite(string spriteName) {
		return spriteName;
	}

	public void changeSpriteFromName(string spriteName, bool resetFrame) {
		changeSprite(getSprite(spriteName), resetFrame);
	}

	public void changeSpriteFromNameIfDifferent(string spriteName, bool resetFrame) {
		string realSpriteName = getSprite(spriteName);
		if (sprite?.name != realSpriteName) {
			changeSprite(realSpriteName, resetFrame);
		}
	}

	public int getChargeLevel() {
		bool clampTo3 = true;
		switch (this) {
			case MegamanX mmx:
				clampTo3 = !mmx.isHyperX;
				break;
			case Zero zero:
				clampTo3 = !zero.canUseDoubleBusterCombo();
				break;
			case Vile vile:
				clampTo3 = !vile.isVileMK5;
				break;
			case BusterZero:
				clampTo3 = false;
				break;
		}
		if (chargeTime < charge1Time) {
			return 0;
		} else if (chargeTime >= charge1Time && chargeTime < charge2Time) {
			return 1;
		} else if (chargeTime >= charge2Time && chargeTime < charge3Time) {
			return 2;
		} else if (chargeTime >= charge3Time && chargeTime < charge4Time) {
			return 3;
		} else if (chargeTime >= charge4Time) {
			return clampTo3 ? 3 : 4;
		}
		return -1;
	}

	public virtual void changeToIdleOrFall() {
		if (grounded) {
			changeState(new Idle(), true);
		} else {
			changeState(new Fall(), true);
		}
	}

	public virtual void changeState(CharState newState, bool forceChange = false) {
		if (!forceChange && charState != null && newState != null &&
			charState.GetType() == newState.GetType() ||
			!forceChange && changedStateInFrame
		) {
			return;
		}
		if (charState is InRideArmor && newState is Frozen) {
			(charState as InRideArmor).freeze((newState as Frozen).freezeTime);
			return;
		} else if (charState is InRideArmor && newState is Stunned) {
			(charState as InRideArmor).stun((newState as Stunned).stunTime);
			return;
		} else if (charState is InRideArmor && newState is Crystalized) {
			(charState as InRideArmor).crystalize((newState as Crystalized).crystalizedTime);
			return;
		}

		if (charState != null && !charState.canExit(this, newState)) {
			return;
		}
		if (newState != null && !newState.canEnter(this)) {
			return;
		}
		changedStateInFrame = true;
		newState.character = this;

		if (shootAnimTime == 0 || !newState.canShoot()) {
			changeSprite(getSprite(newState.sprite), true);
		} else {
			changeSprite(getSprite(newState.shootSprite), true);
		}
		var oldState = charState;
		if (oldState != null) {
			oldState.onExit(newState);
		}
		charState = newState;
		newState.onEnter(oldState);

		//if (!newState.canShoot()) {
		//this.shootTime = 0;
		//this.shootAnimTime = 0;
		//}
	}

	// Get dist from y pos to pos at which to draw the first label
	public float getLabelOffY() {
		float offY = 42;
		if (player.isZero) offY = 45;
		if (player.isVile) offY = 50;
		if (player.isSigma) offY = 62;
		if (sprite.name.Contains("_ra_")) offY = 25;
		if (player.isMainPlayer && player.isTagTeam() && player.currentMaverick != null) {
			offY = player.currentMaverick.getLabelOffY();
		}
		if (player.isWolfSigma()) offY = 25;
		if (player.isViralSigma()) offY = 43;
		if (player.isKaiserSigma()) offY = 125;
		if (player.isKaiserViralSigma()) offY = 60;

		return offY;
	}

	public override void render(float x, float y) {
		if (!shouldRender(x, y)) {
			return;
		}
		currentLabelY = -getLabelOffY();

		if (player.isSigma && visible) {
			string kaiserBodySprite = "";
			if (sprite.name.EndsWith("kaiser_idle")) kaiserBodySprite = sprite.name + "_body";
			if (sprite.name.EndsWith("kaiser_hover")) kaiserBodySprite = sprite.name + "_body";
			if (sprite.name.EndsWith("kaiser_fall")) kaiserBodySprite = sprite.name + "_body";
			if (sprite.name.EndsWith("kaiser_shoot")) kaiserBodySprite = sprite.name + "_body";
			if (sprite.name.EndsWith("kaiser_shoot2")) kaiserBodySprite = sprite.name + "_body";
			if (sprite.name.EndsWith("kaiser_taunt")) kaiserBodySprite = sprite.name + "_body";
			if (kaiserBodySprite != "") {
				Global.sprites[kaiserBodySprite].draw(
					0, pos.x + x, pos.y + y, xDir, 1, null, 1, 1, 1, zIndex - 10
				);
			}
		}

		if (rideArmor == null && rideChaser == null && mk5RideArmorPlatform == null) {
			base.render(x, y);
		} else if (mk5RideArmorPlatform != null) {
			var rideArmorPos = mk5RideArmorPlatform.pos;
			var charPos = getMK5RideArmorPos().addxy(0, 1);
			base.render(rideArmorPos.x + charPos.x - pos.x, rideArmorPos.y + charPos.y - pos.y);
		} else if (rideArmor != null) {
			var rideArmorPos = rideArmor.pos;
			var charPos = getCharRideArmorPos();
			base.render(rideArmorPos.x + charPos.x - pos.x, rideArmorPos.y + charPos.y - pos.y);
		} else if (rideChaser != null) {
			var rideChaserPos = rideChaser.pos;
			base.render(rideChaserPos.x - pos.x, rideChaserPos.y - pos.y);
		}

		if (charState != null) {
			charState.render(x, y);
		}

		if (chargeEffect != null) {
			chargeEffect.render(getParasitePos().add(new Point(x, y)));
		}

		if (player.isX && sprite.name.Contains("frozen")) {
			Global.sprites["frozen_block"].draw(
				0, pos.x + x - (xDir * 2), pos.y + y + 1, xDir, 1, null, 1, 1, 1, zIndex + 1
			);
		}

		if (isCrystalized) {
			float yOff = 0;
			if (sprite.name.Contains("ra_idle")) yOff = 12;
			if (player.isSigma) yOff = -7;
			Global.sprites["crystalhunter_crystal"].draw(
				0, pos.x + x, pos.y + y + yOff, xDir, 1, null, 1, 1, 1, zIndex + 1
			);
		}
		List<Player> nonSpecPlayers = Global.level.nonSpecPlayers();
		bool drawCursorChar = player.isMainPlayer && (
			Global.level.is1v1() || Global.level.server.fixedCamera
		) && !isHyperSigmaBS.getValue();
		if (Global.level.mainPlayer.isSpectator && player == Global.level.specPlayer) {
			drawCursorChar = true;
		}
		if (Global.overrideDrawCursorChar) drawCursorChar = true;

		if (!isWarpIn() && drawCursorChar && player.currentMaverick == null) {
			Global.sprites["cursorchar"].draw(
				0, pos.x + x, pos.y + y + currentLabelY, 1, 1, null, 1, 1, 1, zIndex + 1
			);
			deductLabelY(labelCursorOffY);
		}

		bool shouldDrawName = false;
		bool shouldDrawHealthBar = false;
		string overrideName = null;
		FontType? overrideColor = null;

		if (!hideHealthAndName()) {
			if (Global.level.mainPlayer.isSpectator) {
				shouldDrawName = true;
				shouldDrawHealthBar = true;
			} else if (Global.level.is1v1()) {
				if (!player.isMainPlayer && player.alliance == Global.level.mainPlayer.alliance) {
					shouldDrawName = true;
				}
			}
			// Special case: labeling the own player's disguised Axl
			else if (player.isMainPlayer && player.isDisguisedAxl && Global.level.gameMode.isTeamMode) {
				overrideName = player.disguise.targetName;
				shouldDrawName = true;
			}
			// Special case: labeling an enemy player's disguised Axl
			else if (
				!player.isMainPlayer && player.isDisguisedAxl &&
				Global.level.gameMode.isTeamMode &&
				player.alliance != Global.level.mainPlayer.alliance
			) {
				overrideName = player.disguise.targetName;
				overrideColor = Global.level.gameMode.teamFonts[Global.level.mainPlayer.alliance];
				shouldDrawName = true;
				shouldDrawHealthBar = true;
			}
			// Special case: drawing enemy team name/health as disguised Axl
			else if (!player.isMainPlayer && Global.level.mainPlayer.isDisguisedAxl &&
				Global.level.gameMode.isTeamMode &&
				player.alliance != Global.level.mainPlayer.alliance &&
				!isStealthy(Global.level.mainPlayer.alliance)
			) {
				overrideColor = FontType.Grey;
				shouldDrawName = true;
				shouldDrawHealthBar = true;
			}
			// Basic case, drawing alliance of teammates in team modes
			else if (
				!player.isMainPlayer && player.alliance == Global.level.mainPlayer.alliance &&
				Global.level.gameMode.isTeamMode
			) {
				shouldDrawName = true;
				shouldDrawHealthBar = true;
			}
			// X with scan
			else if (!player.isMainPlayer && Global.level.mainPlayer.isX &&
			  	Global.level.mainPlayer.hasHelmetArmor(2) && player.scanned &&
				!isStealthy(Global.level.mainPlayer.alliance)
			) {
				shouldDrawName = true;
				shouldDrawHealthBar = true;
			}
			// Axl target
			else if (
				!player.isMainPlayer &&
				Global.level.mainPlayer.character is Axl axl &&
				axl.axlCursorTarget == this &&
				!isStealthy(Global.level.mainPlayer.alliance)
			) {
				shouldDrawHealthBar = true;
			}
		}

		if (shouldDrawHealthBar || Global.overrideDrawHealth) {
			drawHealthBar();
		}
		if (shouldDrawName || Global.overrideDrawName) {
			drawName(overrideName, overrideColor);
		}

		if (!hideNoShaderIcon()) {
			float dummy = 0;
			getHealthNameOffsets(out bool shieldDrawn, ref dummy);
			if (player.isX && !Global.shaderWrappers.ContainsKey("palette") && player != Global.level.mainPlayer && !isWarpIn() && !(charState is Die) && player.weapon.index != 0) {
				int overrideIndex = player.weapon.index;
				if (player.weapon is NovaStrike) {
					overrideIndex = 95;
				}
				Global.sprites["hud_weapon_icon"].draw(overrideIndex, pos.x, pos.y - 8 + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD);
				deductLabelY(labelWeaponIconOffY);
			}
		}

		bool drewSubtankHealing = drawSubtankHealing();
		if (player.isMainPlayer && !player.isDead) {
			bool drewStatusProgress = drawStatusProgress();

			if (!drewStatusProgress && !drewSubtankHealing && dropFlagProgress > 0) {
				float healthBarInnerWidth = 30;

				float progress = (dropFlagProgress);
				float width = progress * healthBarInnerWidth;

				getHealthNameOffsets(out bool shieldDrawn, ref progress);

				Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
				Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);

				DrawWrappers.DrawRect(topLeft.x, topLeft.y, botRight.x, botRight.y, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
				DrawWrappers.DrawRect(topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1, true, Color.Yellow, 0, ZIndex.HUD - 1);

				Fonts.drawText(
					FontType.Grey, "Dropping...",
					pos.x + 5, pos.y - 15 + currentLabelY,
					Alignment.Center, true, depth: ZIndex.HUD
				);
				deductLabelY(labelCooldownOffY);
			}

			if (!drewStatusProgress && !drewSubtankHealing && hyperProgress > 0) {
				float healthBarInnerWidth = 30;

				float progress = (hyperProgress);
				float width = progress * healthBarInnerWidth;

				getHealthNameOffsets(out bool shieldDrawn, ref progress);

				Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY - 2.5f);
				Point botRight = new Point(pos.x + 16, pos.y + currentLabelY - 2.5f);

				DrawWrappers.DrawRect(
					topLeft.x, topLeft.y, botRight.x, botRight.y,
					true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White
				);
				DrawWrappers.DrawRect(
					topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1,
					true, Color.Yellow, 0, ZIndex.HUD - 1
				);

				int label = 125;
				if (player.isAxl || player.isDisguisedAxl) {
					label = 123;
				}
				Global.sprites["hud_killfeed_weapon"].draw(
					label, pos.x, pos.y - 6 + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD
				);
				deductLabelY(labelCooldownOffY);
			}
		}

		if (player.isKaiserSigma() && !player.isKaiserViralSigma()) {
			renderDamageText(100);
		} else {
			renderDamageText(35);
		}

		if (Global.showAIDebug) {
			float textPosX = pos.x;// (pos.x - Global.level.camX) / Global.viewSize;
			float textPosY = pos.y - 50;// (pos.y - 50 - Global.level.camY) / Global.viewSize;
			Color outlineColor = player.alliance == GameMode.blueAlliance ? Helpers.DarkBlue : Helpers.DarkRed;

			//DrawWrappers.DrawText(
			//	"Possessing...", pos.x, pos.y - 15 + currentLabelY,
			//	Alignment.Center, true, 0.75f, Color.White, Helpers.getAllianceColor(),
			//	Text.Styles.Regular, 1, true, ZIndex.HUD
			//);

			Fonts.drawText(
				FontType.Grey, player.name, textPosX, textPosY,
				Alignment.Center, true, depth: ZIndex.HUD
			);
			if (ai != null) {
				//DrawWrappers.DrawText(
				//	"state:" + ai.aiState.GetType().Name, textPosX, textPosY -= 10,
				//	Alignment.Center, fontSize: fontSize, outlineColor: outlineColor
				//);
				var charTarget = ai.target as Character;
				Fonts.drawText(
					FontType.Grey, "dest:" + ai.aiState.getDestNodeName(),
					textPosX, textPosY -= 10, Alignment.Center, true, depth: ZIndex.HUD
				);
				Fonts.drawText(
					FontType.Grey, "next:" + ai.aiState.getNextNodeName(), textPosX, textPosY -= 10,
					Alignment.Center, true, depth: ZIndex.HUD
				);
				Fonts.drawText(
					FontType.Grey, "prev:" + ai.aiState.getPrevNodeName(), textPosX, textPosY -= 10,
					Alignment.Center, true, depth: ZIndex.HUD
				);
				if (charTarget != null) {
					Fonts.drawText(
						FontType.Grey, "target:" + charTarget?.name, textPosX, textPosY -= 10,
						Alignment.Center, true, depth: ZIndex.HUD
					);
					if (ai.aiState is FindPlayer fp) {
						Fonts.drawText(
							FontType.Grey, "stuck:" + fp.stuckTime, textPosX, textPosY -= 10,
							Alignment.Center, true, depth: ZIndex.HUD
						);
					}
				}
			}
		}

		if (Global.showHitboxes) {
			Point? headPos = getHeadPos();
			if (headPos != null) {
				//DrawWrappers.DrawCircle(headPos.Value.x, headPos.Value.y, headshotRadius, true, new Color(255, 0, 255, 128), 1, ZIndex.HUD);
				var headRect = getHeadRect();
				DrawWrappers.DrawRect(headRect.x1, headRect.y1, headRect.x2, headRect.y2, true, new Color(255, 0, 0, 128), 1, ZIndex.HUD);
			}
		}
	}

	public void drawSpinner(float progress) {
		float cx = pos.x;
		float cy = pos.y - 50;
		float ang = -90;
		float radius = 4f;
		float thickness = 1.5f;
		int count = Options.main.lowQualityParticles() ? 8 : 40;

		for (int i = 0; i < count; i++) {
			float angCopy = ang;
			DrawWrappers.deferredTextDraws.Add(() => DrawWrappers.DrawCircle(
				(-Global.level.camX + cx + Helpers.cosd(angCopy) * radius) / Global.viewSize,
				(-Global.level.camY + cy + Helpers.sind(angCopy) * radius) / Global.viewSize,
				thickness / Global.viewSize, true, Color.Black, 1, ZIndex.HUD, isWorldPos: false));
			ang += (360f / count);
		}

		for (int i = 0; i < count * progress; i++) {
			float angCopy = ang;
			DrawWrappers.deferredTextDraws.Add(() => DrawWrappers.DrawCircle(
				(-Global.level.camX + cx + Helpers.cosd(angCopy) * radius) / Global.viewSize,
				(-Global.level.camY + cy + Helpers.sind(angCopy) * radius) / Global.viewSize,
				(thickness - 0.5f) / Global.viewSize, true, Color.Yellow, 1, ZIndex.HUD, isWorldPos: false));
			ang += (360f / count);
		}
	}

	public bool drawSubtankHealing() {
		if (ownedByLocalPlayer) {
			if (usedSubtank != null) {
				drawSubtankHealingInner(usedSubtank.health);
				return true;
			}
		} else {
			if (netSubtankHealAmount > 0) {
				drawSubtankHealingInner(netSubtankHealAmount);
				netSubtankHealAmount -= Global.spf * 20;
				if (netSubtankHealAmount <= 0) netSubtankHealAmount = 0;
				return true;
			}
		}

		return false;
	}

	public void drawSubtankHealingInner(float health) {
		Point topLeft = new Point(pos.x - 8, pos.y - 15 + currentLabelY);
		Point topLeftBar = new Point(pos.x - 2, topLeft.y + 1);
		Point botRightBar = new Point(pos.x + 2, topLeft.y + 15);

		Global.sprites["menu_subtank"].draw(1, topLeft.x, topLeft.y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
		Global.sprites["menu_subtank_bar"].draw(0, topLeftBar.x, topLeftBar.y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
		float yPos = 14 * (health / SubTank.maxHealth);
		DrawWrappers.DrawRect(topLeftBar.x, topLeftBar.y, botRightBar.x, botRightBar.y - yPos, true, Color.Black, 1, ZIndex.HUD);

		deductLabelY(labelSubtankOffY);
	}

	public bool drawStatusProgress() {
		if (!Options.main.showMashProgress) {
			return false;
		}

		int statusIndex = 0;
		float statusProgress = 0;
		float totalMashTime = 1;

		if (charState is Frozen frozen) {
			statusIndex = 0;
			totalMashTime = frozen.startFreezeTime;
			statusProgress = frozen.freezeTime / totalMashTime;
		} else if (charState is Crystalized crystalized) {
			statusIndex = 1;
			totalMashTime = 2;
			statusProgress = crystalized.crystalizedTime / totalMashTime;
		} else if (charState is Stunned stunned) {
			statusIndex = 2;
			totalMashTime = 2;
			statusProgress = stunned.stunTime / totalMashTime;
		} else if (charState is VileMK2Grabbed grabbed) {
			statusIndex = 3;
			totalMashTime = VileMK2Grabbed.maxGrabTime;
			statusProgress = grabbed.grabTime / totalMashTime;
		} else if (parasiteTime > 0) {
			statusIndex = 4;
			totalMashTime = 2;
			statusProgress = 1 - (parasiteMashTime / 5);
		} else if (charState is UPGrabbed upGrabbed) {
			statusIndex = 5;
			totalMashTime = UPGrabbed.maxGrabTime;
			statusProgress = upGrabbed.grabTime / totalMashTime;
		} else if (charState is WhirlpoolGrabbed drained) {
			statusIndex = 6;
			totalMashTime = WhirlpoolGrabbed.maxGrabTime;
			statusProgress = drained.grabTime / totalMashTime;
		} else if (player.isPossessed()) {
			statusIndex = 7;
			totalMashTime = Player.maxPossessedTime;
			statusProgress = player.possessedTime / totalMashTime;
		} else if (charState is WheelGGrabbed wheelgGrabbed) {
			statusIndex = 8;
			totalMashTime = WheelGGrabbed.maxGrabTime;
			statusProgress = wheelgGrabbed.grabTime / totalMashTime;
		} else if (charState is FStagGrabbed fstagGrabbed) {
			statusIndex = 9;
			totalMashTime = FStagGrabbed.maxGrabTime;
			statusProgress = fstagGrabbed.grabTime / totalMashTime;
		} else if (charState is MagnaCDrainGrabbed magnacGrabbed) {
			statusIndex = 10;
			totalMashTime = MagnaCDrainGrabbed.maxGrabTime;
			statusProgress = magnacGrabbed.grabTime / totalMashTime;
		} else if (charState is CrushCGrabbed crushcGrabbed) {
			statusIndex = 11;
			totalMashTime = CrushCGrabbed.maxGrabTime;
			statusProgress = crushcGrabbed.grabTime / totalMashTime;
		} else if (charState is BBuffaloDragged bbuffaloDragged) {
			statusIndex = 12;
			totalMashTime = BBuffaloDragged.maxGrabTime;
			statusProgress = bbuffaloDragged.grabTime / totalMashTime;
		} else if (charState is DarkHoldState darkHoldState) {
			statusIndex = 13;
			totalMashTime = DarkHoldState.totalStunTime;
			statusProgress = darkHoldState.stunTime / totalMashTime;
		} else {
			player.lastMashAmount = 0;
			return false;
		}

		float healthBarInnerWidth = 30;

		float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * statusProgress), healthBarInnerWidth);
		float mashWidth = healthBarInnerWidth * (player.lastMashAmount / totalMashTime);

		getHealthNameOffsets(out bool shieldDrawn, ref statusProgress);

		Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
		Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);
		Global.sprites["hud_status_icon"].draw(statusIndex, pos.x, topLeft.y - 7, 1, 1, null, 1, 1, 1, ZIndex.HUD);

		DrawWrappers.DrawRect(topLeft.x, topLeft.y, botRight.x, botRight.y, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
		DrawWrappers.DrawRect(topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1, true, Color.Yellow, 0, ZIndex.HUD - 1);
		DrawWrappers.DrawRect(topLeft.x + 1 + width, topLeft.y + 1, Math.Min(topLeft.x + 1 + width + mashWidth, botRight.x - 1), botRight.y - 1, true, Color.Red, 0, ZIndex.HUD - 1);

		deductLabelY(labelStatusOffY);

		return true;
	}

	public bool hideHealthAndName() {
		if (isWarpIn()) return true;
		if (sprite.name.EndsWith("warp_beam")) return true;
		if (!player.readyTextOver) return true;
		if (isDeathOrReviveSprite()) return true;
		if (Global.level.is1v1() && !Global.level.gameMode.isTeamMode && !Global.level.mainPlayer.isSpectator) return true;
		if (Global.showAIDebug) return true;
		return false;
	}

	// Used to show weapon icons, WA/BZ, etc for non-shader enabled players
	public bool hideNoShaderIcon() {
		if (isWarpIn()) return true;
		if (!player.readyTextOver) return true;
		if (isDeathOrReviveSprite()) return true;
		if (Global.showAIDebug) return true;
		if (isInvisibleBS.getValue()) return true;
		return false;
	}

	public void getHealthNameOffsets(out bool shieldDrawn, ref float healthPct) {
		shieldDrawn = false;
		if (rideArmor != null) {
			shieldDrawn = true;
			healthPct = rideArmor.health / rideArmor.maxHealth;
		} else if ((this as MegamanX)?.chargedRollingShieldProj != null) {
			shieldDrawn = true;
			healthPct = player.weapon.ammo / player.weapon.maxAmmo;
		}
		/*
		else if (player.scanned && !player.isVile)
		{
			shieldDrawn = true;
			if (player.isZero) healthPct = player.rakuhouhaWeapon.ammo / player.rakuhouhaWeapon.maxAmmo;
			else healthPct = player.weapon.ammo / player.weapon.maxAmmo;
		}
		*/
	}

	public void drawHealthBar() {
		float healthBarInnerWidth = 30;
		Color color = new Color();

		float healthPct = player.health / player.maxHealth;
		float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
		if (healthPct > 0.66) color = Color.Green;
		else if (healthPct <= 0.66 && healthPct >= 0.33) color = Color.Yellow;
		else if (healthPct < 0.33) color = Color.Red;

		getHealthNameOffsets(out bool shieldDrawn, ref healthPct);

		float botY = pos.y + currentLabelY;
		DrawWrappers.DrawRect(pos.x - 16, botY - 5, pos.x + 16, botY, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
		DrawWrappers.DrawRect(pos.x - 15, botY - 4, pos.x - 15 + width, botY - 1, true, color, 0, ZIndex.HUD - 1);

		// Shield
		if (shieldDrawn) {
			width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
			float shieldOffY = 4f;
			DrawWrappers.DrawRect(pos.x - 16, botY - 5 - shieldOffY, pos.x + 16, botY - shieldOffY, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
			DrawWrappers.DrawRect(pos.x - 15, botY - 4 - shieldOffY, pos.x - 15 + width, botY - 1 - shieldOffY, true, Color.Blue, 0, ZIndex.HUD - 1);
			deductLabelY(labelHealthOffY + shieldOffY);
		} else {
			deductLabelY(labelHealthOffY);
		}
	}

	public void drawName(string overrideName = "", FontType? overrideColor = null) {
		float healthPct = 0;
		getHealthNameOffsets(out bool shieldDrawn, ref healthPct);

		string playerName = player.name;
		FontType playerColor = FontType.Grey;
		if (Global.level.gameMode.isTeamMode && player.alliance < Global.level.teamNum) {
			playerColor = Global.level.gameMode.teamFonts[player.alliance];
		}

		if (!string.IsNullOrEmpty(overrideName)) playerName = overrideName;
		if (overrideColor != null) {
			playerColor = overrideColor.Value;
		}
		float textPosX = pos.x;
		float textPosY = pos.y + currentLabelY - 8;

		Fonts.drawText(
			FontType.Grey, playerName, textPosX, textPosY,
			Alignment.Center, true, depth: ZIndex.HUD
		);

		deductLabelY(labelNameOffY);
	}

	public void applyDamage(Player attacker, int? weaponIndex, float fDamage, int? projId) {
		if (!ownedByLocalPlayer) return;
		decimal damage = (decimal)fDamage;
		decimal originalDamage = damage;
		decimal originalHP = (decimal)player.health;
		decimal decimalHP = (decimal)player.health;
		Axl axl = this as Axl;
		MegamanX mmx = this as MegamanX;

		if (attacker == player && axl?.isWhiteAxl() == true) {
			damage = 0;
		}
		if (Global.level.isRace() &&
			damage != (decimal)Damager.envKillDamage &&
			damage != (decimal)Damager.switchKillDamage &&
			attacker != player
		) {
			damage = 0;
		}

		bool isArmorPiercing = Damager.isArmorPiercing(projId);

		if (projId == (int)ProjIds.CrystalHunterDash &&
			charState is Crystalized crystalizedState &&
			damage > 0
		) {
			crystalizedState.crystalizedTime = 0; //Dash to destroy crystal
		}

		var inRideArmor = charState as InRideArmor;
		if (inRideArmor != null && inRideArmor.crystalizeTime > 0) {
			if (weaponIndex == 20 && damage > 0) inRideArmor.crystalizeTime = 0;   //Dash to destroy crystal
			inRideArmor.checkCrystalizeTime();
		}

		// For fractional damage shenanigans.
		if (damage % 1 != 0) {
			decimal decDamage = damage % 1;
			damage = Math.Floor(decDamage);
			// Fully nullyfy decimal using damage savings if posible.
			if (damageSavings >= decDamage) {
				damageSavings -= decDamage;
			}
			// If damage is over one we add it to damagedebt.
			else if (damage >= 1) {
				damageDebt += decDamage;
			}
			// If is under 1 we just apply it as is.
			else {
				damage += decDamage;
			}
		}
		// First we apply debt then savings.
		// This is done before defense calculation to allow to defend from debt.
		while (damageDebt >= 1) {
			damageDebt -= 1;
			damage += 1;
		}
		while (damageSavings >= 1 && damage >= 1) {
			damageSavings -= 1;
			damage -= 1;
		}

		// Damage increase/reduction section
		if (!isArmorPiercing) {
			if (charState is SwordBlock) {
				if (player.isSigma) {
					if (player.isPuppeteer()) {
						damageSavings += (originalDamage * 0.25m);
					} else {
						damageSavings += (originalDamage * 0.5m);
					}
				} else {
					damageSavings += (originalDamage * 0.25m);
				}
			}
			if (acidTime > 0) {
				decimal extraDamage = 0.25m + (0.25m * ((decimal)acidTime / 8.0m));
				damageDebt += (originalDamage * extraDamage);
			}
			if (mmx != null) {
				if (mmx.hasBarrier(false)) {
					damageSavings += (originalDamage * 0.25m);
				} else if (mmx.hasBarrier(true)) {
					damageSavings += (originalDamage * 0.5m);
				}
				if (player.isX && player.hasBodyArmor(1)) {
					damageSavings += originalDamage / 8m;
				}
				if (player.isX && player.hasBodyArmor(2)) {
					damageSavings += originalDamage / 8m;
				}
			}
			if (this is Vile vile && vile.hasFrozenCastleBarrier()) {
				damageSavings += originalDamage * Vile.frozenCastlePercent;
			}
		}
		// Special conditions for decimal damage.
		if (damage % 1 != 0) {
			decimal damageDec = (decimal)damage % 1m;

		}
		// This is to defend from overkill damage.
		// Or at least attempt to.
		if (damageSavings > 0 &&
			decimalHP - damage <= 0 &&
			(decimalHP + damageSavings) - damage > 0
		) {
			while (damageSavings >= 1) {
				damageSavings -= 1;
				damage -= 1;
			}
		}

		// If somehow the damage is negative.
		// Heals are not really applied here.
		if (damage < 0) { damage = 0; }

		player.health -= (float)damage;
		decimalHP = (decimal)player.health;

		if (player.showTrainingDps && player.health > 0 && originalDamage > 0) {
			if (player.trainingDpsStartTime == 0) {
				player.trainingDpsStartTime = Global.time;
				Global.level.gameMode.dpsString = "";
			}
			player.trainingDpsTotalDamage += (float)damage;
		}

		if (damage > 0 && mmx != null) {
			mmx.noDamageTime = 0;
			mmx.rechargeHealthTime = 0;
		}

		if (damage > 0 && attacker != null) {
			if (projId != (int)ProjIds.Burn && projId != (int)ProjIds.AcidBurstPoison) {
				player.delaySubtank();
			}
		}
		if (originalHP > 0 && (originalDamage > 0 || damage > 0)) {
			addDamageTextHelper(attacker, (float)damage, player.maxHealth, true);
		}
		if (player.health > 0 && damage > 0 && ownedByLocalPlayer) {
			decimal modifier = (decimal)player.maxHealth > 0 ? (16 / (decimal)player.maxHealth) : 1;
			float gigaAmmoToAdd = (float)(1 + (damage * 2 * modifier));
			if (this is Zero zero) {
				zero.zeroGigaAttackWeapon.addAmmo(gigaAmmoToAdd, player);
			}
			if (this is MegamanX) {
				var gigaCrush = player.weapons.FirstOrDefault(w => w is GigaCrush);
				if (gigaCrush != null) {
					gigaCrush.addAmmo(gigaAmmoToAdd, player);
				}
				var hyperBuster = player.weapons.FirstOrDefault(w => w is HyperBuster);
				if (hyperBuster != null) {
					hyperBuster.addAmmo(gigaAmmoToAdd, player);
				}
				var novaStrike = player.weapons.FirstOrDefault(w => w is NovaStrike);
				if (novaStrike != null) {
					novaStrike.addAmmo(gigaAmmoToAdd, player);
				}
				//fgMoveAmmo += gigaAmmoToAdd;
				//if (fgMoveAmmo > 32) fgMoveAmmo = 32;
			}
			if (this is NeoSigma) {
				player.sigmaAmmo = Helpers.clampMax(player.sigmaAmmo + gigaAmmoToAdd, player.sigmaMaxAmmo);
			}
		}

		if (attacker != null && weaponIndex != null && damage > 0) {
			damageHistory.Add(new DamageEvent(attacker, weaponIndex.Value, projId, false, Global.time));
		}

		if (player.health <= 0) {
			if (player.showTrainingDps && player.trainingDpsStartTime > 0) {
				float timeToKill = Global.time - player.trainingDpsStartTime;
				float dps = player.trainingDpsTotalDamage / timeToKill;
				Global.level.gameMode.dpsString = "DPS: " + dps.ToString("0.0");

				player.trainingDpsTotalDamage = 0;
				player.trainingDpsStartTime = 0;
			}
			killPlayer(attacker, null, weaponIndex, projId);
		} else {
			if (mmx != null && player.hasBodyArmor(3) && damage > 0) {
				mmx.addBarrier(charState is Hurt);
			}
		}
	}

	public void killPlayer(Player killer, Player assister, int? weaponIndex, int? projId) {
		player.health = 0;
		int? assisterProjId = null;
		int? assisterWeaponId = null;
		if (charState is not Die || !ownedByLocalPlayer) {
			player.lastDeathCanRevive = Global.anyQuickStart || Global.debug || Global.level.isTraining() || killer != null;
			changeState(new Die(), true);

			if (ownedByLocalPlayer) {
				getKillerAndAssister(player, ref killer, ref assister, ref weaponIndex, ref assisterProjId, ref assisterWeaponId);
			}

			if (killer != null && killer != player) {
				killer.addKill();
				if (Global.level.gameMode is TeamDeathMatch) {
					if (Global.isHost) {
						if (killer.alliance != player.alliance) {
							Global.level.gameMode.teamPoints[killer.alliance]++;
							Global.level.gameMode.syncTeamScores();
						}
					}
				}

				killer.awardCurrency();
			} else if (Global.level.gameMode.level.is1v1()) {
				// In 1v1 the other player should always be considered a killer to prevent suicide
				var otherPlayer = Global.level.nonSpecPlayers().Find(p => p.id != player.id);
				if (otherPlayer != null) {
					otherPlayer.addKill();
				}
			}

			if (assister != null && assister != player) {
				assister.addAssist();
				assister.addKill();

				assister.awardCurrency();
			}
			//bool isSuicide = killer == null || killer == player;
			player.addDeath(false);
			/*
			if (isSuicide && Global.isHost && Global.level.gameMode is TeamDeathMatch)
			{
				if (player.alliance == GameMode.redAlliance) Global.level.gameMode.redPoints--;
				if (player.alliance == GameMode.blueAlliance) Global.level.gameMode.bluePoints--;
				if (Global.level.gameMode.bluePoints < 0) Global.level.gameMode.bluePoints = 0;
				if (Global.level.gameMode.redPoints < 0) Global.level.gameMode.redPoints = 0;
				Global.level.gameMode.syncTeamScores();
			}
			*/

			Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(killer, assister, player, weaponIndex));
			if (ownedByLocalPlayer && Global.level.isNon1v1Elimination() && player.deaths >= Global.level.gameMode.playingTo) {
				Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(player.name + " was eliminated.", GameMode.blueAlliance), sendRpc: true);
			}

			if (killer?.ownedByLocalPlayer == true)
				if (killer.character is Axl axl && killer.copyShotDamageEvents.Any(c => c.character == this)) {
					axl.addDNACore(this);
				}

			if (assister?.ownedByLocalPlayer == true) {
				if (assister.character is Axl axl && assister.copyShotDamageEvents.Any(c => c.character == this)) {
					axl.addDNACore(this);
				}
			}

			if (ownedByLocalPlayer) {
				var victimPlayerIdBytes = BitConverter.GetBytes((ushort)player.id);

				if (weaponIndex != null && killer != null) {
					var bytes = new List<byte>()
					{
							1,
							(byte)killer.id,
							assister == null ? (byte)killer.id : (byte)assister.id,
							victimPlayerIdBytes[0],
							victimPlayerIdBytes[1],
							(byte)weaponIndex
						};

					if (projId != null) {
						byte[] projIdBytes = BitConverter.GetBytes((ushort)projId.Value);
						bytes.Add(projIdBytes[0]);
						bytes.Add(projIdBytes[1]);
					}

					Global.serverClient?.rpc(RPC.killPlayer, bytes.ToArray());
				} else {
					Global.serverClient?.rpc(RPC.killPlayer, 0, 0, 0, victimPlayerIdBytes[0], victimPlayerIdBytes[1]);
				}
			}
		}
	}

	public void addHealth(float amount, bool fillSubtank = true) {
		if (player.health >= player.maxHealth && fillSubtank) {
			player.fillSubtank(amount);
		}
		healAmount += amount;
	}

	public void fillHealthToMax() {
		healAmount += player.maxHealth;
	}

	public void addAmmo(float amount) {
		if (player.isX && player.weapon.ammo >= player.weapon.maxAmmo) {
			foreach (var weapon in player.weapons) {
				if (weapon == player.weapon) continue;
				if (weapon.ammo == weapon.maxAmmo) continue;
				weapon.ammo = Math.Clamp(weapon.ammo + amount, 0, weapon.maxAmmo);
				break;
			}
			return;
		}

		weaponHealAmount += amount;
	}

	public virtual void increaseCharge() {
		float factor = 1;
		if (player.isX && player.hasArmArmor(1)) factor = 1.5f;
		//if (player.isX && isHyperX) factor = 1.5f;
		//if (player.isZero && isAttacking()) factor = 0f;
		chargeTime += Global.spf * factor;
	}

	public void dropFlag() {
		if (flag != null) {
			flag.dropFlag();
			flag = null;
		}
	}

	public void onFlagPickup(Flag flag) {
		if (isCharging()) {
			stopCharge();
		}
		dropFlagProgress = 0;
		stockedCharge = false;
		this.flag = flag;
		stingChargeTime = 0;
		if (beeSwarm != null) {
			beeSwarm.destroy();
		}
		if (this is MegamanX mmx) {
			if (mmx.chargedRollingShieldProj != null) {
				mmx.chargedRollingShieldProj.destroySelf();
			}
			mmx.popAllBubbles();
		}
		if (player.isDisguisedAxl && player.ownedByLocalPlayer) {
			player.revertToAxl();
		}
	}

	public void setHurt(int dir, int flinchFrames, float miniFlinchTime, bool spiked) {
		if (!ownedByLocalPlayer) {
			return;
		}
		// Tough Guy.
		if (player.isSigma || isToughGuyHyperMode()) {
			if (miniFlinchTime > 0) return;
			else {
				flinchFrames = 0;
				miniFlinchTime = 0.1f;
			}
		}
		if (!(charState is Die) && !(charState is InRideArmor) && !(charState is InRideChaser)) {
			changeState(new Hurt(dir, flinchFrames, miniFlinchTime, spiked), true);
		}
	}

	public override void destroySelf(
		string spriteName = null, string fadeSound = null,
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {
		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned);

		player.removeOwnedGrenades();
		player.removeOwnedIceStatues();
		player.removeOwnedMechaniloids();
		player.removeOwnedSeeds();

		chargeEffect?.destroy();
		chargeSound?.destroy();
		parasiteAnim?.destroySelf();

		// This ensures that the "onExit" charState function
		// Can do any cleanup it needs to do without having to copy-paste that code here too.
		charState?.onExit(null);
	}

	public void cleanupBeforeTransform() {
		parasiteAnim?.destroySelf();
		parasiteTime = 0;
		parasiteMashTime = 0;
		parasiteDamager = null;
		removeAcid();
		removeBurn();
	}

	public bool canBeHealed(int healerAlliance) {
		if (isHyperSigmaBS.getValue()) return false;
		return player.alliance == healerAlliance && player.health > 0 && player.health < player.maxHealth;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
		if (!allowStacking && this.healAmount > 0) return;
		if (player.health < player.maxHealth) {
			playHealSound = true;
		}
		commonHealLogic(healer, healAmount, player.health, player.maxHealth, drawHealText);
		addHealth(healAmount, fillSubtank: false);
	}

	public void crystalizeStart() {
		isCrystalized = true;
		if (globalCollider != null) globalCollider.isClimbable = true;
		new Anim(getCenterPos(), "crystalhunter_activate", 1, null, true);
		playSound("crystalize");
	}

	public void crystalizeEnd() {
		isCrystalized = false;
		playSound("CrystalizeDashingX2");
		for (int i = 0; i < 8; i++) {
			var anim = new Anim(getCenterPos().addxy(Helpers.randomRange(-20, 20), Helpers.randomRange(-20, 20)), "crystalhunter_piece", Helpers.randomRange(0, 1) == 0 ? -1 : 1, null, false);
			anim.frameIndex = Helpers.randomRange(0, 1);
			anim.frameSpeed = 0;
			anim.useGravity = true;
			anim.vel = new Point(Helpers.randomRange(-150, 150), Helpers.randomRange(-300, 25));
		}
	}

	// PARASITE SECTION

	public ParasiteAnim parasiteAnim;
	public bool hasParasite { get { return parasiteTime > 0; } }
	public float parasiteTime;
	public float parasiteMashTime;
	public Damager parasiteDamager;
	public BeeSwarm beeSwarm;

	public void addParasite(Player attacker) {
		if (!ownedByLocalPlayer) return;

		Damager damager = new Damager(attacker, 4, Global.defFlinch, 0);
		parasiteTime = Global.spf;
		parasiteDamager = damager;
		parasiteAnim = new ParasiteAnim(getCenterPos(), "parasitebomb_latch_start", player.getNextActorNetId(), true, true);
	}

	public void updateParasite() {
		if (parasiteTime <= 0) return;
		slowdownTime = Math.Max(slowdownTime, 0.05f);

		if (!(charState is ParasiteCarry) && parasiteTime > 1.5f) {
			foreach (var otherPlayer in Global.level.players) {
				if (otherPlayer.character == null) continue;
				if (otherPlayer == player) continue;
				if (otherPlayer == parasiteDamager.owner) continue;
				if (otherPlayer.character.isInvulnerable()) continue;
				if (Global.level.gameMode.isTeamMode && otherPlayer.alliance != player.alliance) continue;
				if (otherPlayer.character.getCenterPos().distanceTo(getCenterPos()) > ParasiticBomb.carryRange) continue;
				Character target = otherPlayer.character;
				changeState(new ParasiteCarry(target, true));
				break;
			}
		}

		if (parasiteAnim != null) {
			if (parasiteAnim.sprite.name == "parasitebomb_latch_start" && parasiteAnim.isAnimOver()) {
				parasiteAnim.changeSprite("parasitebomb_latch", true);
			}
			parasiteAnim.changePos(getParasitePos());
		}

		parasiteTime += Global.spf;
		float mashValue = player.mashValue();
		if (mashValue > Global.spf) {
			parasiteMashTime += mashValue;
		}
		if (parasiteMashTime > 5) {
			removeParasite(true, false);
		} else if (parasiteTime > 2 && !(charState is ParasiteCarry)) {
			removeParasite(false, false);
		}
	}

	public void removeParasite(bool ejected, bool carried) {
		if (!ownedByLocalPlayer) return;
		if (parasiteDamager == null) return;

		parasiteAnim?.destroySelf();
		if (ejected) {
			new Anim(getCenterPos(), "parasitebomb_latch", 1, player.getNextActorNetId(), true, sendRpc: true, ownedByLocalPlayer) {
				vel = new Point(50 * xDir, -50),
				useGravity = true
			};
		} else {
			new Anim(getCenterPos(), "explosion", 1, player.getNextActorNetId(), true, sendRpc: true, ownedByLocalPlayer);
			playSound("explosion", sendRpc: true);
			if (!carried) parasiteDamager.applyDamage(this, player.weapon is FrostShield, new ParasiticBomb(), this, (int)ProjIds.ParasiticBombExplode, overrideDamage: 4, overrideFlinch: Global.defFlinch);
		}

		parasiteTime = 0;
		parasiteMashTime = 0;
		parasiteDamager = null;
	}

	public virtual bool isInvisible() {
		return stingChargeTime > 0 && player.isX;
	}

	public bool genmuImmune(Player owner) {
		return false;
	}

	public override Dictionary<int, Func<Projectile>> getGlobalProjs() {
		var retProjs = new Dictionary<int, Func<Projectile>>();

		// TODO: Move this to viral Sigma class.
		if (sprite.name.Contains("viral_tackle") && sprite.time > 0.15f) {
			retProjs[(int)ProjIds.Sigma2ViralTackle] = () => {
				var damageCollider = getAllColliders().FirstOrDefault(c => c.isAttack());
				Point centerPoint = damageCollider.shape.getRect().center();
				Projectile proj = new GenericMeleeProj(
					new ViralSigmaTackleWeapon(player), centerPoint, ProjIds.Sigma2ViralTackle, player
				);
				proj.globalCollider = damageCollider.clone();
				return proj;
			};
		}
		return retProjs;
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.Sigma3KaiserStomp) {
			float damagePercent = getKaiserStompDamage();
			if (damagePercent > 0) {
				proj.damager.damage = 12 * damagePercent;
			}
		} else if (proj.projId == (int)ProjIds.AwakenedAura) {
			if (isAwakenedGenmuZeroBS.getValue()) {
				proj.damager.damage = 4;
				proj.damager.flinch = Global.defFlinch;
			}
		}
	}

	public float getKaiserStompDamage() {
		float damagePercent = 0.25f;
		if (deltaPos.y > 150 * Global.spf) damagePercent = 0.5f;
		if (deltaPos.y > 210 * Global.spf) damagePercent = 0.75f;
		if (deltaPos.y > 300 * Global.spf) damagePercent = 1;
		return damagePercent;
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		return base.getProjFromHitbox(hitbox, centerPoint);
	}

	public void releaseGrab(Actor grabber, bool sendRpc = false) {
		charState?.releaseGrab();
		if (!ownedByLocalPlayer) {
			RPC.commandGrabPlayer.sendRpc(
				grabber.netId, netId, CommandGrabScenario.Release, grabber.isDefenderFavored()
			);
		}
	}

	public bool isAlwaysHeadshot() {
		return sprite?.name?.Contains("_ra_") == true || sprite?.name?.Contains("_rc_") == true;
	}

	public bool canEjectFromRideArmor() {
		var shape = globalCollider.shape;
		if (shape.minY < 0 && shape.maxY < 0) {
			shape = shape.clone(0, MathF.Abs(shape.maxY) + 1);
		}

		var collision = Global.level.checkCollisionShape(shape, new List<GameObject>() { rideArmor });
		if (collision?.gameObject is not Wall) {
			return true;
		}

		return false;
	}

	public virtual bool isAttacking() {
		return sprite.name.Contains("attack");
	}

	public bool canLandOnRideArmor() {
		if (charState is Fall) return true;
		if (charState is VileHover vh && vh.fallY > 0) return true;
		return false;
	}

	public void getOffMK5Platform() {
		if (mk5RideArmorPlatform != null) {
			if (mk5RideArmorPlatform != vileStartRideArmor) {
				mk5RideArmorPlatform.character = null;
				mk5RideArmorPlatform.changeState(new RADeactive(), true);
			}
			mk5RideArmorPlatform = null;
		}
	}

	public bool canAffordRideArmor() {
		if (Global.level.is1v1()) return player.health > (player.maxHealth / 2);
		return player.currency >= Vile.callNewMechCost;
	}

	public void buyRideArmor() {
		if (Global.level.is1v1()) player.health -= (player.maxHealth / 2);
		else player.currency -= Vile.callNewMechCost * (player.selectedRAIndex >= 4 ? 2 : 1);
	}

	public virtual void onMechSlotSelect(MechMenuWeapon mmw) {
		if (vileStartRideArmor == null) {
			if (!mmw.isMenuOpened) {
				mmw.isMenuOpened = true;
				return;
			}
		}

		if (player.isAI) {
			calldownMechCooldown = maxCalldownMechCooldown;
		}
		if (vileStartRideArmor == null) {
			if (alreadySummonedNewMech) {
				Global.level.gameMode.setHUDErrorMessage(player, "Can only summon a mech once per life");
			} else if (canAffordRideArmor()) {
				if (!(charState is Idle || charState is Run || charState is Crouch)) return;
				alreadySummonedNewMech = true;
				if (vileStartRideArmor != null) vileStartRideArmor.selfDestructTime = 1000;
				buyRideArmor();
				mmw.isMenuOpened = false;
				int raIndex = player.selectedRAIndex;
				vileStartRideArmor = new RideArmor(
					player, pos, raIndex, 0, player.getNextActorNetId(), true, sendRpc: true
				);
			}
		} else {
			rideArmor.changeState(new RACalldown(pos, false), true);
		}
	}

	// Axl DNA shenanigans.
	public void updateDisguisedAxl() {
		if (player.weapon is AssassinBullet) {
			// URGENT TODO
			// player.assassinHitPos = player.character.getFirstHitPos(AssassinBulletProj.range);
		}
		/*
		// TODO: Check if this has any impact.
		if (!player.isAxl) {
			if (Options.main.axlAimMode == 2) {
				updateAxlCursorPos();
			} else {
				updateAxlDirectionalAim();
			}
		}
		*/

		if (this is Zero || this is Rock) {
			player.changeWeaponControls();
		}

		if (player.weapon is UndisguiseWeapon) {
			bool shootPressed = player.input.isPressed(Control.Shoot, player);
			bool altShootPressed = player.input.isPressed(Control.Special1, player);
			if ((shootPressed || altShootPressed) && !isCCImmuneHyperMode()) {
				undisguiseTime = 0.33f;
				DNACore lastDNA = player.lastDNACore;
				int lastDNAIndex = player.lastDNACoreIndex;
				player.revertToAxl();
				player.character.undisguiseTime = 0.33f;
				// To keep DNA.
				if (altShootPressed && player.currency >= 1) {
					player.currency -= 1;
					lastDNA.hyperMode = DNACoreHyperMode.None;
					// Turn ultimate and golden armor into naked X
					if (lastDNA.armorFlag >= byte.MaxValue - 1) {
						lastDNA.armorFlag = 0;
					}
					// Turn ancient gun into regular axl bullet
					if (lastDNA.weapons.Count > 0 &&
						lastDNA.weapons[0] is AxlBullet ab &&
						ab.type == (int)AxlBulletWeaponType.AncientGun
					) {
						lastDNA.weapons[0] = player.getAxlBulletWeapon(0);
					}
					player.weapons.Insert(lastDNAIndex, lastDNA);
				}
				return;
			}
		}

		if (player.weapon is AssassinBullet) {
			if (player.input.isPressed(Control.Special1, player) && !isCharging()) {
				if (player.currency >= 2) {
					player.currency -= 2;
					shootAssassinShot(isAltFire: true);
					return;
				} else {
					Global.level.gameMode.setHUDErrorMessage(
						player, $"Quick assassinate requires 2 {Global.nameCoins}"
					);
				}
			}
		}

		if (player.weapon is AssassinBullet && (player.isVile || player.isSigma)) {
			if (player.input.isHeld(Control.Shoot, player)) {
				increaseCharge();
			} else {
				if (isCharging()) {
					shootAssassinShot();
				}
				stopCharge();
			}
			chargeLogic();
		}

		/*
		if (player.weapon is AssassinBullet && chargeTime > 7)
		{
			shootAssassinShot();
			stopCharge();
		}
		*/
	}

	public void shootAssassinShot(bool isAltFire = false) {
		if (getChargeLevel() >= 3 || isAltFire) {
			player.revertToAxl();
			assassinTime = 0.5f;
			assassinTime = 0.5f;
			player.character.useGravity = false;
			player.character.vel = new Point();
			player.character.isQuickAssassinate = isAltFire;
			player.character.changeState(new Assassinate(grounded), true);
		} else {
			stopCharge();
		}
	}

	public float assassinTime;
	public bool isQuickAssassinate;
	public float undisguiseTime;
	public bool disguiseCoverBlown;

	public void addTransformAnim() {
		transformAnim = new Anim(pos, "axl_transform", xDir, null, true);
		playSound("transform");
		if (ownedByLocalPlayer) {
			Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (byte)RPCToggleType.AddTransformEffect);
		}
	}

	public virtual void onFlinchOrStun(CharState state) {

	}

	public virtual void onExitState(CharState oldState, CharState newState) {

	}

	public virtual bool chargeButtonHeld() {
		return false;
	}
}

public struct DamageEvent {
	public Player attacker;
	public int weapon;
	public int? projId;
	public float time;
	public bool envKillOnly;

	public DamageEvent(Player attacker, int weapon, int? projId, bool envKillOnly, float time) {
		this.attacker = attacker;
		this.weapon = weapon;
		this.projId = projId;
		this.envKillOnly = envKillOnly;
		this.time = time;
	}
}
