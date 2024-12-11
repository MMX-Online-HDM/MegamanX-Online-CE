
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SFML.Graphics;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public partial class Character : Actor, IDamagable {
	public static string[] charDisplayNames = {
		"X",
		"Zero",
		"Vile",
		"Axl",
		"Sigma"
	};

	// Health.
	public decimal health;
	public decimal maxHealth;
	
	// Player linked data.
	public Player player;
	public int currency;

	public List<Weapon> weapons = new();
	public int weaponSlot;
	public Weapon? currentWeapon { get {
		if (weaponSlot < 0 || weaponSlot >= weapons.Count) {
			return null;
		}
		return weapons[weaponSlot];
	}}
	

	// Statemachine stuff.
	public CharState charState;
	public bool isDashing;

	// Movement and charge.
	public bool changedStateInFrame;
	public bool pushedByTornadoInFrame;

	public float chargeTime;
	public float charge1Time = 30;
	public float charge2Time = 105;
	public float charge3Time = 180;
	public float charge4Time = 255;
	public float hyperProgress;

	public ChargeEffect chargeEffect;
	public const float DefaultShootAnimTime = 18;
	public float shootAnimTime = 0;
	public AI? ai;

	public int dashedInAir = 0;
	public float healAmount = 0;
	public SubTank? usedSubtank;
	public float netSubtankHealAmount;
	public bool playHealSound;
	public float healTime = 0;
	public float weaponHealAmount = 0;
	public float weaponHealTime = 0;
	public float healthBarInnerWidth;
	public float slideVel = 0;
	public Flag? flag;
	public bool isCrystalized;
	public bool insideCharacter;
	public float invulnTime = 0;

	public List<Trail> lastFiveTrailDraws = new List<Trail>();
	public LoopingSound chargeSound;

	public readonly float headshotRadius = 6;

	public decimal damageSavings = 0;
	public decimal damageDebt = 0;

	public bool stopCamUpdate = false;
	public Anim? warpBeam;
	public float flattenedTime;

	public const float maxLastAttackerTime = 5;

	public float igFreezeProgress;
	public float freezeInvulnTime;
	public float stunInvulnTime;
	public float crystalizeInvulnTime;
	public float grabInvulnTime;
	public float darkHoldInvulnTime;
	public bool isDarkHoldState;
	public bool isStrikeChainState;

	public float limboRACheckCooldown;
	public RideArmor? rideArmor;
	public RideChaser? rideChaser;
	public Player lastGravityWellDamager;

	// Some things previously in other char files used by multiple characters.
	public RideArmor? linkedRideArmor;
	public RideArmor? rideArmorPlatform;
	public bool alreadySummonedNewMech;

	// Was on Axl.cs before
	public Anim? transformAnim;
	float transformSmokeTime;
	public int fakeAlliance;

	// For states with special propieties.
	// For doublejump.
	public float lastJumpPressedTime;

	// For wallkick.
	public float wallKickTimer;
	public int wallKickDir;
	public float maxWallKickTime = 12;

	// Char Ids.
	public CharIds charId;

	// Random stuff.
	public float dropFlagProgress;
	public float dropFlagCooldown;
	public bool dropFlagUnlocked;

	// Status effects.
	// Acid
	public Damager? acidDamager;
	public float acidTime;
	public float acidHurtCooldown;
	// Infected
	public float virusTime;
	// Oil
	public Damager? oilDamager;
	public float oilTime;
	// Burn
	public Damager? burnDamager;
	public Weapon burnWeapon = FireWave.netWeapon;
	public float burnTime;
	public float burnEffectTime;
	public float burnHurtCooldown;
	// Ice
	public float slowdownTime;
	// Parasite.
	public Damager? parasiteDamager;
	public ParasiteAnim? parasiteAnim;
	public bool hasParasite { get { return parasiteTime > 0; } }
	public float parasiteTime;
	public float parasiteMashTime;
	
	// Disables status.
	public float paralyzedTime;
	public float paralyzedMaxTime;
	public float frozenTime;
	public float frozenMaxTime;
	public float crystalizedTime;
	public float crystalizedMaxTime;
	
	// Buffs.
	public float vaccineTime;
	public float vaccineHurtCooldown;

	// Ctrl data
	public int altCtrlsLength = 1;

	// Etc.
	public int camOffsetX;

	// Main character class starts here.
	public Character(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, bool mk2VileOverride = false, bool mk5VileOverride = false
	) : base(
		null!, new Point(x, y), netId, ownedByLocalPlayer, dontAddToLevel: true
	) {
		this.player = player;
		this.xDir = xDir;

		isDashing = false;
		splashable = true;
		// Intialize state as soon as posible.
		charState = new NetLimbo();
		charState.character = this;

		// Starting state.
		CharState initialCharState;

		if (ownedByLocalPlayer) {
			if (isWarpIn) { initialCharState = new WarpIn(); }
			else { initialCharState = new Idle(); }
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

		changeState(initialCharState, true);
		visible = isVisible;

		chargeTime = 0;
		useFrameProjs = true;

		chargeSound = new LoopingSound("charge_start", "charge_loop", this);

		if (this.player != Global.level.mainPlayer) {
			zIndex = ++Global.level.autoIncCharZIndex;
		} else {
			zIndex = ZIndex.MainPlayer;
		}

		Global.level.addGameObject(this);

		chargeEffect = new ChargeEffect();
		lastGravityWellDamager = player;
		maxHealth = (decimal)player.getMaxHealth();
		health = 1;
		healAmount = (float)maxHealth - 1;
	}

	public override void onStart() {
		base.onStart();
	}

	public void addVaccineTime(float time) {
		if (!ownedByLocalPlayer) return;
		vaccineTime += time;
		if (vaccineTime > 8) vaccineTime = 8;
		if (charState is GenericStun) {
			changeToIdleOrFall();
		}
		burnTime = 0;
		acidTime = 0;
		oilTime = 0;
		player.possessedTime = 0;
	}
	public bool isVaccinated() { return vaccineTime > 0; }

	public void addVirusTime(Player attacker, float time) {
		if (!ownedByLocalPlayer) return;
		if (isInvulnerable()) return;
		if (isVaccinated()) return;
		if (charState.invincible) return;

		Damager damager = new Damager(attacker, 0, 0, 0);
		virusTime += time;
		if (virusTime > 8) virusTime = 8;
	}

	public void addDarkHoldTime(float darkHoldTime, Player attacker) {
		if (!ownedByLocalPlayer) return;
		if (isInvulnerable()) return;
		if (isVaccinated()) return;
		if (charState.invincible) return;

		changeState(new DarkHoldState(this, darkHoldTime), true);
	}

	public void addAcidTime(Player attacker, float time) {
		if (!ownedByLocalPlayer ||
			isDotImmune() ||
			isInvulnerable() ||
			isVaccinated() || charState.invincible
		) {
			return;
		}
		// If attacker is null use the same, else use self.
		Player newAttacker = attacker ?? burnDamager?.owner ?? player;
		if (acidDamager == null) {
			acidDamager = new Damager(newAttacker, 0, 0, 0);
		} else {
			acidDamager.owner = newAttacker;
		}
		// Reset timer if it's 0.
		if (acidTime == 0) {
			acidHurtCooldown = 0;
		}
		// Apply time if we do not go over 8.
		if (acidTime + time <= 8) {
			acidTime += time;
		}
	}

	public void addOilTime(Player attacker, float time) {
		if (!ownedByLocalPlayer ||
			isDebuffImmune() ||
			isInvulnerable() ||
			isVaccinated() || charState.invincible
		) {
			return;
		}
		// If attacker is null use the same, else use self.
		Player newAttacker = attacker ?? burnDamager?.owner ?? player;
		if (oilDamager == null) {
			oilDamager = new Damager(newAttacker, 0, 0, 0);
		} else {
			oilDamager.owner = newAttacker;
		}
		// Apply time and limit to 8.
		oilTime += time;
		if (oilTime >= 8) {
			oilTime = 8;
		}
		// Activate burn if burning.
		if (burnTime > 0) {
			addBurnTime(attacker, new FlameMOilWeapon(), 2);
			return;
		}
	}

	public void addBurnTime(Player? attacker, Weapon weapon, float time) {
		if (!ownedByLocalPlayer ||
			isDotImmune() ||
			isInvulnerable() ||
			isVaccinated() || charState.invincible
		) {
			return;
		}
		// If attacker is null use the same, else use self.
		Player newAttacker = attacker ?? burnDamager?.owner ?? player;
		if (burnDamager == null) {
			burnDamager = new Damager(newAttacker, 0, 0, 0);
			burnWeapon = weapon;
		} else {
			burnDamager.owner = newAttacker;
			burnWeapon = weapon;
		}
		// Reset timer if it's 0.
		if (burnTime == 0) {
			burnHurtCooldown = 0;
		}
		// Apply time if we do not go over 8.
		if (burnTime + time <= 8) {
			burnTime += time;
		}
		// Oil explosion.
		if (oilTime > 0) {
			playSound("flamemOilBurn", sendRpc: true);
			burnDamager.applyDamage(
				this, false, weapon, this, (int)ProjIds.Burn,
				overrideDamage: 2, overrideFlinch: Global.defFlinch
			);
			// Apply burn damage instantly.
			burnTime += oilTime;
			oilTime = 0;
			burnHurtCooldown = 0;
			// Double check again in case oil increased over 8.
			if (burnTime >= 8) {
				burnTime = 8;
			}
		}
	}

	float igFreezeRecoveryCooldown = 0;
	public void addIgFreezeProgress(float amount, int freezeTime = 120) {
		if (freezeInvulnTime > 0) return;
		if (frozenTime > 0) return;
		if (isStatusImmune()) return;
		if (isInvulnerable()) return;
		if (isVaccinated()) return;
		if (charState.invincible) return;

		igFreezeProgress += amount;
		igFreezeRecoveryCooldown = 0;
		if (igFreezeProgress >= 4) {
			igFreezeProgress = 4;
		}
		if (igFreezeProgress >= 4 && canFreeze()) {
			igFreezeProgress = 0;
			freeze(freezeTime);
		}
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = new();

		if (player.isPossessed() && player.possessedShader != null) {
			player.possessedShader.SetUniform("palette", 1);
			player.possessedShader.SetUniform("paletteTexture", Global.textures["palettePossessed"]);
			shaders.Add(player.possessedShader);
		}

		if (isDarkHoldState && player.darkHoldShader != null) {
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
			player.acidShader.SetUniform("acidFactor", 0.25f + (acidTime / 8f) * 0.75f);
			shaders.Add(player.acidShader);
		}
		if (oilTime > 0 && player.oilShader != null) {
			player.oilShader.SetUniform("oilFactor", 0.25f + (oilTime / 8f) * 0.75f);
			shaders.Add(player.oilShader);
		}
		if (vaccineTime > 0 && player.vaccineShader != null) {
			player.vaccineShader.SetUniform("vaccineFactor", vaccineTime / 8f);
			//vaccineShader?.SetUniform("vaccineFactor", 1f);
			shaders.Add(player.vaccineShader);
		}
		if (igFreezeProgress > 0 && !sprite.name.Contains("frozen") && player.igShader != null) {
			player.igShader.SetUniform("igFreezeProgress", igFreezeProgress / 4);
			shaders.Add(player.igShader);
		}
		if (virusTime > 0 && player.infectedShader != null) {
			player.infectedShader.SetUniform("infectedFactor", virusTime / 8f);
			shaders.Add(player.infectedShader);
		}
		return shaders;
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
		return false;
	}

	public virtual bool canTurn() {
		if (rideArmorPlatform != null) {
			return false;
		}
		return true;
	}

	public virtual bool canMove() {
		if (rideArmorPlatform != null) {
			return false;
		}
		return true;
	}

	public virtual bool canDash() {
		if (player.isAI && charState is Dash) return false;
		if (rideArmorPlatform != null) return false;
		if (charState is WallKick wallKick && wallKick.stateTime < 0.25f) return false;
		if (isSoftLocked()) return false;
		return flag == null;
	}

	public virtual bool canJump() {
		if (rideArmorPlatform != null) return false;
		if (isSoftLocked()) return false;
		return true;
	}

	public virtual bool canCrouch() {
		if (isSoftLocked() || isDashing) {
			return false;
		}
		return true;
	}

	public virtual bool canAirDash() {
		return dashedInAir == 0;
	}

	public virtual bool canAirJump() {
		return false;
	}

	public virtual bool canWallClimb() {
		if (rideArmorPlatform != null) return false;
		if (isSoftLocked()) return false;
		return true;
	}

	public virtual bool canUseLadder() {
		if (!charState.normalCtrl) return false;
		if (rideArmorPlatform != null) return false;
		if (isSoftLocked()) return false;
		return true;
	}

	public bool canStartClimbLadder() {
		if (!charState.normalCtrl) return false;
		return true;
	}

	public virtual bool canClimbLadder() {
		if (rideArmorPlatform != null) {
			return false;
		}
		if (shootAnimTime > 0 || isSoftLocked()) {
			return false;
		}
		return true;
	}

	public virtual bool canCharge() {
		return true;
	}

	public virtual bool canShoot() {
		return charState.attackCtrl;
	}

	public virtual bool canChangeWeapons() {
		if (player.weapon is AssassinBullet && chargeTime > 0) return false;
		if (charState is ViralSigmaPossess) return false;
		if (charState is InRideChaser) return false;

		return true;
	}

	public virtual bool canPickupFlag() {
		if (player.isPossessed()) return false;
		if (dropFlagCooldown > 0) return false;
		if (isInvulnerable()) return false;
		if (player.isDisguisedAxl) return false;
		if (isCCImmuneHyperMode()) return false;
		if (charState is Die || charState is VileRevive || charState is XReviveStart || charState is XRevive) return false;
		if (player.currentMaverick != null && player.isTagTeam()) return false;
		if (isWarpOut()) return false;
		if (Global.serverClient != null) {
			if (Global.serverClient.isLagging() == true) return false;
			if (player.serverPlayer.connection?.AverageRoundtripTime >= 1000) return false;
		}
		if (charState is KaiserSigmaRevive || charState is WolfSigmaRevive || charState is ViralSigmaRevive) return false;
		return true;
	}

	public virtual bool canKeepFlag() {
		if (player.isPossessed()) return false;
		if (health <= 0) return false;
		if (isInvulnerable()) return false;
		if (isCCImmuneHyperMode()) return false;
		if (charState is Die) return false;
		if (isWarpOut()) return false;
		if (Global.serverClient != null) {
			if (Global.serverClient.isLagging() == true) return false;
			if (player.serverPlayer.connection?.AverageRoundtripTime >= 1000) return false;
		}
		return true;
	}

	public virtual bool isSoundCentered() {
		if (charState is WarpOut) {
			return false;
		}
		return true;
	}

	public virtual float getRunSpeed() {
		return Physics.WalkSpeed * getRunDebuffs();
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
		bool isChargedStrikeChain = strikeChainProj is StrikeChainProjCharged;
		bool flinch = (isChargedStrikeChain || strikeChainProj is WSpongeSideChainProj);
		changeState(new StrikeChainHooked(strikeChainProj, flinch), true);
	}

	// For terrain collision.
	public override Collider? getTerrainCollider() {
		Collider? overrideGlobalCollider = null;
		if (spriteToColliderMatch(sprite.name, out overrideGlobalCollider)) {
			return overrideGlobalCollider;
		}
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

	public override Collider? getGlobalCollider() {
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

	public virtual Collider getBlockCollider() {
		var rect = new Rect(0, 0, 18, 34);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override void preUpdate() {
		base.preUpdate();
		insideCharacter = false;
		changedStateInFrame = false;
		pushedByTornadoInFrame = false;
		if (grounded && !isDashing) {
			dashedInAir = 0;
		}
	}

	public override void onCollision(CollideData other) {
		if (charState is SaberParryStartState punchParry &&
			other.gameObject is Projectile proj &&
			punchParry.canParry(proj) &&
			proj.owner.alliance != player.alliance &&
			proj.damager?.damage > 0 &&
			!Damager.isDot(proj.projId)
		) {
			punchParry.counterAttack(proj.owner, proj, proj.damager.damage);
			return;
		}
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
			if (!killZone.killInvuln && this is MegamanX { stingActiveTime: >0 } ) return;
			if (!killZone.killInvuln && this is Axl { stealthActive: true} ) return;
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

	public override void update() {
		if (charState is not InRideChaser) {
			camOffsetX = MathInt.Round(Helpers.lerp(camOffsetX, 0, 10));
		}

		Helpers.decrementTime(ref limboRACheckCooldown);
		Helpers.decrementTime(ref dropFlagCooldown);

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

		if (Global.level.gameMode.isTeamMode && Global.level.mainPlayer != player) {
			int alliance = player.alliance;
			// If this is an enemy disguised Axl, change the alliance
			if (player.alliance != Global.level.mainPlayer.alliance && player.isDisguisedAxl && !disguiseCoverBlown) {
				alliance = fakeAlliance;
			}
			RenderEffectType? allianceEffect = alliance switch {
				0 => RenderEffectType.BlueShadow,
				1 => RenderEffectType.RedShadow,
				2 => RenderEffectType.GreenShadow,
				3 => RenderEffectType.PurpleShadow,
				4 => RenderEffectType.YellowShadow,
				5 => RenderEffectType.OrangeShadow,
				_ => null
			};
			if (allianceEffect != null) {
				addRenderEffect(allianceEffect.Value);
			}
		}

		if (Global.level.mainPlayer.readyTextOver) {
			Helpers.decrementTime(ref invulnTime);
		}

		if (vaccineTime > 0) {
			oilTime = 0;
			burnTime = 0;
			acidTime = 0;
			virusTime = 0;
			vaccineTime -= Global.spf;
			if (vaccineTime <= 0) {
				vaccineTime = 0;
			}
		}

		if (virusTime > 0) {
			virusTime -= Global.spf;
			if (virusTime <= 0) {
				virusTime = 0;
			}
		}

		if (oilTime > 0) {
			oilTime -= Global.spf;
			if (isUnderwater() || charState.invincible || isStatusImmune()) {
				oilTime = 0;
			}
			if (oilTime <= 0) {
				oilTime = 0;
			}
		}

		if (acidTime > 0) {
			acidTime -= Global.spf;
			acidHurtCooldown += Global.speedMul;
			if (acidHurtCooldown >= 60) {
				acidHurtCooldown -= 60;
				if (acidHurtCooldown <= 0) {
					acidHurtCooldown = 0;
				}
				acidDamager?.applyDamage(
					this, player.weapon is TornadoFang,
					new AcidBurst(), this, (int)ProjIds.AcidBurstPoison,
					overrideDamage: 1f
				);
				new Anim(
					getCenterPos().addxy(Helpers.randomRange(-6, 6), -20),
					"torpedo_smoke", 1, null, true) {
						vel = new Point(0, -50)
					};
			}
			if (isUnderwater() || charState.invincible || isStatusImmune()) {
				acidTime = 0;
			}
			if (acidTime <= 0) {
				removeAcid();
			}
		}

		if (burnTime > 0) {
			burnTime -= Global.spf;
			burnHurtCooldown += Global.speedMul;
			burnEffectTime += Global.speedMul;
			if (burnEffectTime >= 6) {
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
			if (burnHurtCooldown >= 60) {
				burnHurtCooldown -= 60;
				if (burnHurtCooldown <= 0) {
					burnHurtCooldown = 0;
				}
				burnDamager?.applyDamage(this, false, burnWeapon, this, (int)ProjIds.Burn, overrideDamage: 1f);
			}
			if (isUnderwater() || charState.invincible || isStatusImmune()) {
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
		Helpers.decrementTime(ref slowdownTime);

		if (!ownedByLocalPlayer) {
			if (isCharging()) {
				chargeGfx();
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

		if (linkedRideArmor != null && !Global.level.hasGameObject(linkedRideArmor)) {
			linkedRideArmor = null;
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
		// Cutoff point for things that run but aren't owned by the player
		if (!ownedByLocalPlayer) {
			base.update();
			return;
		}
		updateParasite();

		if (pos.y > Global.level.killY && !isWarpIn() && charState is not WarpOut) {
			if (charState is WolfSigmaRevive wsr) {
				stopMoving();
				useGravity = false;
				wsr.groundStart = true;
			} else {
				if (charState is not Die) {
					incPos(new Point(0, 25));
				}
				applyDamage(Damager.envKillDamage, player, this, null, null);
			}
		}

		if (health >= maxHealth) {
			healAmount = 0;
			usedSubtank = null;
		}
		if (healAmount > 0 && health > 0) {
			healTime += Global.spf;
			if (healTime > 0.05) {
				healTime = 0;
				healAmount--;
				if (usedSubtank != null) {
					usedSubtank.health--;
				}
				health = Helpers.clampMax(health + 1, maxHealth);
				if (acidTime > 0) {
					acidTime--;
					if (acidTime < 0) removeAcid();
				}
				if (player == Global.level.mainPlayer || playHealSound) {
					if (this is MegamanX { hyperHelmetActive: true, helmetArmor: ArmorId.Max }) {
						playSound("goldenHelmetHP", forcePlay: true, sendRpc: true);
					} else {
						playSound("heal", forcePlay: true, sendRpc: true);
					}
				}
			}
		} else {
			playHealSound = false;
		}

		if (usedSubtank != null && usedSubtank.health <= 0) {
			usedSubtank = null;
		}

		if (ai != null ) {
			ai.update();
		}

		if (slideVel != 0) {
			slideVel = Helpers.toZero(slideVel, Global.spf * 350, Math.Sign(slideVel));
			move(new Point(slideVel, 0), true);
		}
		base.update();

		// For G. Well damage.
		// This is calculated after the base update to prevent acidental double damage.
		if (vel.y < 0 && Global.level.checkTerrainCollisionOnce(this, 0, -1) != null) {
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
		if (rideArmorPlatform != null) {
			changePos(rideArmorPlatform.getMK5Pos().addxy(0, 1));
			xDir = rideArmorPlatform.xDir;
			grounded = true;

			if (rideArmorPlatform.destroyed) {
				rideArmorPlatform = null;
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
		charState.stateFrames += 1f * Global.speedMul;
	}

	public virtual bool updateCtrl() {
		if (!ownedByLocalPlayer) {
			return false;
		}
		if (charState.exitOnLanding && grounded) {
			landingCode();
		}
		if (charState.exitOnAirborne && !grounded) {
			changeState(new Fall());
		}
		if (canWallClimb() &&
			!grounded && vel.y >= 0 &&
			wallKickTimer <= 0 && (
				charState.normalCtrl || charState.airMove ||
				charState is WallSlide || charState is WallSlideAttack { canCancel: true }
			) &&
			player.input.isPressed(Control.Jump, player) &&
			(charState.wallKickLeftWall != null || charState.wallKickRightWall != null)
		) {
			dashedInAir = 0;
			if (player.input.isHeld(Control.Dash, player) &&
				(charState.useDashJumpSpeed || charState is WallSlide or WallSlideAttack)
			) {
				isDashing = true;
				dashedInAir++;
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
			wallKickTimer = maxWallKickTime;
			if (charState.normalCtrl || charState is WallSlide or WallSlideAttack) {
				changeState(new WallKick(), true);
				if (wallKickDir != 0) {
					xDir = -wallKickDir;
				}
			} else {
				playSound("jump", sendRpc: true);
			}
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
		if (charState.canJump && (grounded || canAirJump() && flag == null)) {
			if (player.input.isPressed(Control.Jump, player)) {
				if (!grounded) {
					dashedInAir++;
				} else {
					grounded = false;
				}
				vel.y = -getJumpPower();
				playSound("jump", sendRpc: true);
				if (charState.airSprite != "") {
					changeSpriteFromName(charState.airSprite, false);
				}
			}
		}
		if (charState.normalCtrl) {
			normalCtrl();
		}
		if (charState.attackCtrl && invulnTime <= 0) {
			return attackCtrl();
		}
		if (charState.altCtrls.Any(b => b)) {
			return altCtrl(charState.altCtrls);
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
				if (isDashing) {
					dashedInAir++;
				}
				changeState(new Jump());
				return true;
			} else if (player.dashPressed(out string dashControl) && canDash() && charState is not Dash) {
				changeState(new Dash(dashControl), true);
				return true;
			} else if (
				rideArmorPlatform != null &&
				player.input.isPressed(Control.Jump, player) &&
				player.input.isHeld(Control.Up, player) &&
				canEjectFromRideArmor()
			  ) {
				getOffMK5Platform();
				changeState(new Jump());
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
			if (player.dashPressed(out string dashControl) && canAirDash() && canDash() && flag == null) {
				changeState(new AirDash(dashControl));
				return true;
			}
			if (canAirJump() && flag == null) {
				if (player.input.isPressed(Control.Jump, player) && canJump()) {
					lastJumpPressedTime = Global.time;
				}
				if ((player.input.isPressed(Control.Jump, player) ||
					Global.time - lastJumpPressedTime < 0.1f) &&
					wallKickTimer <= 0 && flag == null &&
					!sprite.name.Contains("kick_air")
				) {
					lastJumpPressedTime = 0;
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

				if (dpadXDir == -1 && velYRequirementMet && charState.lastLeftWall != null
					&& charState.lastLeftWallCollider != null
				) {
					changeState(new WallSlide(-1, charState.lastLeftWallCollider));
					return true;
				}
				if (dpadXDir == 1 && velYRequirementMet && charState.lastRightWall != null
					&& charState.lastRightWallCollider != null
				) {
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

	public virtual bool altCtrl(bool[] ctrls) {
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
		return sprite.name.Contains("warp_in") || !visible;
	}

	public Point getCharRideArmorPos() {
		if (rideArmor == null || rideArmor.currentFrame.POIs.Length == 0) {
			return new Point();
		}
		var charPos = rideArmor.currentFrame.POIs[0];
		charPos.x *= xDir;
		return charPos;
	}

	public Point getMK5RideArmorPos() {
		if (rideArmorPlatform == null || rideArmorPlatform.currentFrame.POIs.Length == 0) {
			return new Point();
		}
		var charPos = rideArmorPlatform.currentFrame.POIs[0];
		charPos.x *= xDir;
		return charPos;
	}

	public bool isSpriteDash(string spriteName) {
		return spriteName.Contains("dash") && !spriteName.Contains("up_dash");
	}

	public override void changeSprite(string spriteName, bool resetFrame) {
		List<Trail>? trails = sprite?.lastFiveTrailDraws;
		base.changeSprite(spriteName, resetFrame);
		if (trails != null && sprite != null) {
			sprite.lastFiveTrailDraws = trails;
		}
	}

	public bool isHeadbuttSprite(string sprite) {
		return sprite.EndsWith("jump") || sprite.EndsWith("up_dash") || sprite.EndsWith("wall_kick");
	}

	public void freeze(int timeToFreeze = 120) {
		if (!ownedByLocalPlayer) {
			return;
		}
		frozenMaxTime = timeToFreeze;
		frozenTime = timeToFreeze;
		if (charState is not GenericStun) {
			changeState(new GenericStun(), true);
		}
	}

	public bool canFreeze() {
		return (
			charState is not Die &&
			!isStunImmune() &&
			!charState.stunResistant &&
			!charState.invincible &&
			invulnTime == 0 &&
			freezeInvulnTime <= 0 &&
			!isInvulnerable() &&
			!isVaccinated() &&
			!isStatusImmune()
		);
	}

	public void paralize(float timeToParalize = 120) {
		if (!ownedByLocalPlayer ||
			isInvulnerable() ||
			isVaccinated() ||
			isStatusImmune() ||
			charState.invincible ||
			charState.stunResistant ||
			(charState is Die or VileMK2Grabbed) ||
			isStunImmune() ||
		 	stunInvulnTime > 0
		) {
			return;
		}
		paralyzedMaxTime = timeToParalize;
		paralyzedTime = timeToParalize;
		if (charState is not GenericStun) {
			changeState(new GenericStun(), true);
		}
	}

	public void crystalize(float timeToCrystalize = 120) {
		if (!ownedByLocalPlayer ||
			isInvulnerable() ||
			isVaccinated() ||
			isStatusImmune() ||
			charState.invincible ||
			charState.stunResistant ||
			isCrystalized ||
			(charState is Die) ||
			isStunImmune() ||
			crystalizeInvulnTime > 0
		) {
			return;
		}
		vel.y = 0;
		crystalizedMaxTime = timeToCrystalize;
		crystalizedTime = timeToCrystalize;
		if (charState is not GenericStun) {
			changeState(new GenericStun(), true);
		}
	}

	public virtual void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			int chargeType = 0;
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
				addRenderEffect(renderGfx, 2, 6);
			}
			chargeEffect.update(getChargeLevel(), chargeType);
		}
	}

	public virtual bool isDebuffImmune() {
		return isStatusImmune();
	}

	public virtual bool isDotImmune() {
		return isStatusImmune();
	}

	public virtual bool isSlowImmune() {
		return isStatusImmune();
	}

	public virtual bool isStunImmune() {
		return isStatusImmune();
	}

	public virtual bool isPushImmune() {
		return isTrueStatusImmune();
	}

	public virtual bool isStatusImmune() {
		return isTrueStatusImmune() || isInvulnerable() || isVaccinated() || charState.invincible;
	}

	public virtual bool isTrueStatusImmune() {
		return false;
	}

	public virtual bool isCCImmuneHyperMode() {
		return false;
	}

	public virtual bool isToughGuyHyperMode() {
		return false;
	}

	public bool isImmuneToKnockback() {
		return charState?.immuneToWind == true || immuneToKnockback || isStatusImmune();
	}

	// If factorHyperMode = true, then invuln frames in a hyper mode won't count as "invulnerable".
	// This is to allow the hyper mode start invulnerability to still be able to do things without being impeded
	// and should be set only by code that is checking to see if such things can be done.
	public virtual bool isInvulnerable(bool ignoreRideArmorHide = false, bool factorHyperMode = false) {
		if (isWarpIn()) return true;
		if (invulnTime > 0) return true;
		if (!ignoreRideArmorHide && charState is InRideArmor && (charState as InRideArmor)?.isHiding == true) {
			return true;
		}
		if (!ignoreRideArmorHide && !string.IsNullOrEmpty(sprite?.name) && sprite.name.Contains("ra_hide")) {
			return true;
		}
		if (charState.specialId == SpecialStateIds.AxlRoll || charState.specialId == SpecialStateIds.XTeleport) {
			return true;
		}
		if (sprite != null && sprite.name.Contains("viral_exit")) {
			return true;
		}
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

	public bool canBeGrabbed() {
		return (
			grabInvulnTime == 0 && !charState.invincible &&
			!isInvulnerable() && !isStatusImmune() && !isDarkHoldState
		);
	}

	public bool isDeathOrReviveSprite() {
		if (sprite.name == "sigma_head_intro") return true;
		if (sprite.name.EndsWith("die")) return true;
		if (sprite.name.Contains("revive")) return true;
		return false;
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

	public virtual bool isStealthy(int alliance) {
		return false;
	}

	public Point getDashSparkEffectPos(int xDir) {
		return getDashDustEffectPos(xDir).addxy(6 * xDir, 4);
	}

	public virtual Point getDashDustEffectPos(int xDir) {
		float dashXPos = -24;
		return pos.addxy(dashXPos * xDir + (5 * xDir), -4);
	}

	public override Point getCenterPos() {
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

	public virtual Point getParasitePos() {
		if (sprite.name.Contains("_ra_")) {
			return pos.addxy(0, -6);
		}
		return getCenterPos();
	}

	public virtual Actor getFollowActor() {
		if (rideArmorPlatform != null) {
			return rideArmorPlatform;
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
		if (rideArmorPlatform != null) {
			return rideArmorPlatform.pos.round().addxy(0, -70);
		}
		if (rideArmor != null) {
			if (ownedByLocalPlayer && rideArmor.rideArmorState is RADropIn rADropInState) {
				return rADropInState.spawnPos.addxy(0, -24);
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
		Point headPos = getHeadPos() ?? pos;
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

	public virtual Point getShootPos() {
		var busterOffset = currentFrame.getBusterOffset();
		if (busterOffset == null) {
			return getCenterPos();
		}
		return pos.addxy(busterOffset.Value.x * xDir, busterOffset.Value.y);
	}

	public void stopCharge() {
		if (chargeEffect == null) return;
		chargeEffect.reset();
		chargeTime = 0;
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

	public virtual int getMaxChargeLevel() {
		return 3;
	}

	public virtual int getChargeLevel() {
		int chargeLevel = 0;
		int maxCharge = getMaxChargeLevel();

		if (chargeTime < charge1Time) {
			chargeLevel = 0;
		} else if (chargeTime >= charge1Time && chargeTime < charge2Time) {
			chargeLevel = 1;
		} else if (chargeTime >= charge2Time && chargeTime < charge3Time) {
			chargeLevel = 2;
		} else if (chargeTime >= charge3Time && chargeTime < charge4Time) {
			chargeLevel = 3;
		} else if (chargeTime >= charge4Time) {
			chargeLevel = 4;
		}
		return Helpers.clampMax(chargeLevel, maxCharge);
	}

	public virtual void changeToIdleOrFall(string transitionSprite = "") {
		if (grounded) {
			changeState(new Idle(transitionSprite), true);
		} else {
			changeState(new Fall(), true);
		}
	}

	public virtual void changeToLandingOrFall(bool useSound = true) {
		if (grounded) {
			landingCode(useSound);
		} else {
			changeState(new Fall(), true);
		}
	}

	public virtual void changeToCrouchOrFall() {
		if (grounded) {
			changeState(new Crouch(), true);
		} else {
			changeState(new Fall(), true);
		}
	}

	public virtual void landingCode(bool useSound = true) {
		if (useSound) {
			playSound("land", sendRpc: true);
		}
		dashedInAir = 0;
		changeState(new Idle("land"), true);
	}

	public virtual bool changeState(CharState newState, bool forceChange = false) {
		// Set the character as soon as posible.
		newState.character = this;
		newState.altCtrls = new bool[altCtrlsLength];

		// Check if we can change.
		if (!forceChange &&
			(charState.GetType() == newState.GetType() || changedStateInFrame)
		) {
			return false;
		}
		// For Ride Armor stuns.
		if (charState is InRideArmor inRideArmor) {
			if (newState is GenericStun) {
				if (crystalizedTime > 0) {
					inRideArmor.crystalize(crystalizedTime / 60f);
				}
				if (frozenTime > 0) {
					inRideArmor.freeze(frozenTime / 60f);
				}
				if (paralyzedTime > 0) {
					inRideArmor.stun(paralyzedTime / 60f);
				}
				return false;
			}
		}
		if (!charState.canExit(this, newState)) {
			return false; 
		}
		if (!newState.canEnter(this)) {
			return false;
		}
		changedStateInFrame = true;
		bool hasShootAnim = newState.canUseShootAnim();
		if (shootAnimTime > 0 && hasShootAnim) {
			changeSprite(getSprite(newState.shootSprite), true);
		} else {
			string spriteName = sprite.name;
			changeSprite(getSprite(newState.sprite), true);
			if (spriteName == sprite.name) {
				sprite.frameIndex = 0;
				sprite.frameTime = 0;
				sprite.time = 0;
				sprite.frameSpeed = 1;
				sprite.loopCount = 0;
				sprite.visible = true;
			}
		}
		CharState oldState = charState;
		oldState.onExit(newState);

		charState = newState;
		newState.onEnter(oldState);

		//if (!newState.canShoot()) {
		//	this.shootTime = 0;
		//	this.shootAnimTime = 0;
		//}
		return true;
	}

	// Get dist from y pos to pos at which to draw the first label
	public virtual float getLabelOffY() {
		if (sprite.name.Contains("_ra_")) {
			return 25;
		}
		return 42;
	}

	public override bool shouldDraw() {
		if (invulnTime > 0) {
			if (Global.level.frameCount % 4 < 2) { return false; }
		}
		return base.shouldDraw();
	}

	public override void render(float x, float y) {
		if (!shouldRender(x, y)) {
			return;
		}
		currentLabelY = -getLabelOffY();

		
		if (rideArmor == null && rideChaser == null && rideArmorPlatform == null) {
			base.render(x, y);
		} else if (rideArmorPlatform != null) {
			var rideArmorPos = rideArmorPlatform.pos;
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
		);

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
		string overrideName = "";
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
			else if (player.isMainPlayer && player.isDisguisedAxl && 
				Global.level.gameMode.isTeamMode && player.disguise != null
			) {
				overrideName = player.disguise.targetName;
				shouldDrawName = true;
			}
			// Special case: labeling an enemy player's disguised Axl
			else if (
				!player.isMainPlayer && player.isDisguisedAxl &&
				Global.level.gameMode.isTeamMode &&
				player.alliance != Global.level.mainPlayer.alliance &&
				player.disguise != null
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
		if (shouldDrawName || Global.overrideDrawName && overrideName != "") {
			drawName(overrideName, overrideColor);
		}

		if (!hideNoShaderIcon()) {
			float dummy = 0;
			getHealthNameOffsets(out bool shieldDrawn, ref dummy);
		}

		bool drewSubtankHealing = drawSubtankHealing();
		if (!player.isDead) {
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

		/*if (Global.showHitboxes) {
			Point? headPos = getHeadPos();
			if (headPos != null) {
				//DrawWrappers.DrawCircle(headPos.Value.x, headPos.Value.y, headshotRadius, true, new Color(255, 0, 255, 128), 1, ZIndex.HUD);
				var headRect = getHeadRect();
				DrawWrappers.DrawRect(
					headRect.x1 + 1,
					headRect.y1 + 1,
					headRect.x2 - 1,
					headRect.y2 - 1,
					true, new Color(255, 0, 0, 50), 1, ZIndex.HUD, true,
					new Color(255, 0, 0, 128)
				);
			}
		}*/
	}

	public void drawSpinner(float progress) {
		float cx = pos.x;
		float cy = pos.y - 50;
		float ang = -90;
		float radius = 4f;
		float thickness = 1.5f;
		int count = Options.main.lowQualityParticles() ? 16 : 40;

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
		if (!Options.main.showMashProgress || Global.level.mainPlayer.character != this) {
			return false;
		}

		int statusIndex = 0;
		float statusProgress = 0;
		float totalMashTime = 1;
		float healthBarInnerWidth = 30;

		if (charState is GenericStun gst) {
			bool hasDrawn = false;
			List<int> iconsToDraw = new();
			if (crystalizedTime > 0) {
				drawStatusBar(crystalizedTime, gst.getTimerFalloff(), crystalizedMaxTime, new Color(247, 206, 247));
				deductLabelY(5);
				iconsToDraw.Add(1);
				hasDrawn = true;
			}
			if (frozenTime > 0) {
				drawStatusBar(frozenTime, gst.getTimerFalloff(), frozenMaxTime, new Color(123, 206, 255));
				deductLabelY(5);
				iconsToDraw.Add(0);
				hasDrawn = true;
			}
			if (paralyzedTime > 0) {
				drawStatusBar(paralyzedTime, gst.getTimerFalloff(), paralyzedMaxTime, new Color(255, 231, 123));
				deductLabelY(5);
				iconsToDraw.Add(2);
				hasDrawn = true;
			}
			if (hasDrawn) {
				for(int i = 0; i < iconsToDraw.Count; i++) {
					Global.sprites["hud_status_icon"].draw(
						iconsToDraw[i],
						pos.x - (iconsToDraw.Count - 1) * 6 + i * 12,
						pos.y - 7 + currentLabelY,
						1, 1, null, 1, 1, 1, ZIndex.HUD
					);
				}
				deductLabelY(15);
				return true;
			}
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

		float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * statusProgress), healthBarInnerWidth);
		float mashWidth = healthBarInnerWidth * (player.lastMashAmount / totalMashTime);

		getHealthNameOffsets(out bool shieldDrawn, ref statusProgress);

		Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
		Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);
		Global.sprites["hud_status_icon"].draw(statusIndex, pos.x, topLeft.y - 7, 1, 1, null, 1, 1, 1, ZIndex.HUD);

		DrawWrappers.DrawRect(
			topLeft.x, topLeft.y, botRight.x, botRight.y, true,
			Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White
		);
		DrawWrappers.DrawRect(
			topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width,
			botRight.y - 1, true, Color.Yellow, 0, ZIndex.HUD - 1
		);
		DrawWrappers.DrawRect(
			topLeft.x + 1 + width, topLeft.y + 1,
			Math.Min(topLeft.x + 1 + width + mashWidth, botRight.x - 1),
			botRight.y - 1, true, Color.Red, 0, ZIndex.HUD - 1
		);
		deductLabelY(labelStatusOffY);

		return true;
	}

	public void drawStatusBar(float time, float mash, float maxTime, Color color) {
		float healthBarInnerWidth = 30;
		float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * (time / maxTime)), healthBarInnerWidth);
		float mashWidth = mash / maxTime;

		Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
		Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);
		DrawWrappers.DrawRect(
			topLeft.x, topLeft.y, botRight.x, botRight.y,
			true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White
		);
		DrawWrappers.DrawRect(
			topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width,
			botRight.y - 1, true, color, 0, ZIndex.HUD - 1
		);
		DrawWrappers.DrawRect(
			topLeft.x + 1 + width, topLeft.y + 1,
			Math.Min(topLeft.x + 1 + width + mashWidth, botRight.x - 1), botRight.y - 1,
			true, Color.Red, 0, ZIndex.HUD - 1
		);
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
		return false;
	}

	public virtual void getHealthNameOffsets(out bool shieldDrawn, ref float healthPct) {
		shieldDrawn = false;
		if (rideArmor != null) {
			shieldDrawn = true;
			healthPct = rideArmor.health / rideArmor.maxHealth;
		}
	}

	public void drawHealthBar() {
		float healthBarInnerWidth = 30;
		Color color = new Color();

		float healthPct = (float)(health / maxHealth);
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

	public virtual bool canBeHealed(int healerAlliance) {
		return player.alliance == healerAlliance && health > 0 && health < maxHealth;
	}

	public virtual void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
		if (!allowStacking && this.healAmount > 0) return;
		if (health < maxHealth) {
			playHealSound = true;
		}
		commonHealLogic(healer, (decimal)healAmount, health, maxHealth, drawHealText);
		addHealth(healAmount, fillSubtank: false);
	}

	
	public virtual bool isInvincible(Player attacker, int? projId) {
		if (ownedByLocalPlayer) {
			return charState.invincible || genmuImmune(attacker);
		} else {
			return isSpriteInvulnerable() || genmuImmune(attacker);
		}
	}

	public virtual bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		if (isInvulnerable()) return false;
		if (isDeathOrReviveSprite()) return false;
		if (Global.level.gameMode.setupTime > 0) return false;
		if (Global.level.isRace()) {
			bool isAxlSelfDamage = player.isAxl && damagerAlliance == player.alliance;
			if (!isAxlSelfDamage) return false;
		}

		// Self damaging projIds can go thru alliance check
		bool isSelfDamaging =
			projId == (int)ProjIds.BlastLauncherGrenadeSplash ||
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

	public virtual void applyDamage(float fDamage, Player? attacker, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;
		decimal damage = decimal.Parse(fDamage.ToString());
		decimal originalDamage = damage;
		decimal originalHP = health;
		Axl? axl = this as Axl;
		MegamanX? mmx = this as MegamanX;

		// For Dark Hold break.
		if (damage > 0 && charState is DarkHoldState dhs && dhs.stateFrames > 10 && !Damager.isDot(projId)) {
			changeToIdleOrFall();
		}
		//this made Axl immortal to pits, suicide button, etc
		/*
		if (attacker == player && axl?.isWhiteAxl() == true) {
			damage = 0;
		} */
		if (Global.level.isRace() &&
			damage != (decimal)Damager.envKillDamage &&
			damage != (decimal)Damager.switchKillDamage &&
			attacker != player
		) {
			damage = 0;
		}

		bool isArmorPiercing = Damager.isArmorPiercing(projId);

		if (projId == (int)ProjIds.CrystalHunterDash &&
			charState is GenericStun && crystalizedTime > 0 &&
			damage > 0
		) {
			crystalizedTime = 0; // Dash to destroy crystal
		}

		var inRideArmor = charState as InRideArmor;
		if (inRideArmor != null && inRideArmor.crystalizeTime > 0) {
			if (weaponIndex == 20 && damage > 0) inRideArmor.crystalizeTime = 0;   //Dash to destroy crystal
			inRideArmor.checkCrystalizeTime();
		}

		// For fractional damage shenanigans.
		if (damage % 1 != 0) {
			decimal decDamage = damage % 1;
			damage = Math.Floor(damage);
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
				damageSavings += (originalDamage * 0.25m);
			}
			if (charState is SigmaBlock) {
				damageSavings += (originalDamage * 0.5m);
			}
			if (acidTime > 0) {
				decimal extraDamage = 0.25m + (0.25m * ((decimal)acidTime / 8.0m));
				damageDebt += (originalDamage * extraDamage);
			}
			if (mmx != null) {
				if (mmx.barrierActiveTime > 0) {
					if (mmx.hyperChestArmor == ArmorId.Max) {
						damageSavings += (originalDamage * 0.5m);
					} else {
						damageSavings += (originalDamage * 0.25m);
					}
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
		// This is to defend from overkill damage.
		// Or at least attempt to.
		if (damageSavings > 0 &&
			health - damage <= 0 &&
			(health + damageSavings) - damage > 0
		) {
			// Apply in the normal way.
			while (damageSavings >= 1) {
				damageSavings -= 1;
				damage -= 1;
			}
			// Decimal protection scenario.
			if (damage > 0 && damageSavings > 0 && damageSavings + (1m/8m) >= damage) {
				damage = 0;
				damageSavings = damageSavings - damage;
				if (damageSavings <= 0) {
					damageSavings = 0;
				}
			} 
		}

		// If somehow the damage is negative.
		// Heals are not really applied here.
		if (damage < 0) { damage = 0; }
		health -= damage;
		// Clamp to 0. We do not want to go into the negatives here.
		if (health < 0) {
			health = 0;
		}

		if (player.showTrainingDps && health > 0 && originalDamage > 0) {
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
			addDamageTextHelper(attacker, (float)damage, (float)maxHealth, true);
		}
		if (health > 0 && (originalDamage > 0 || damage > 0) && ownedByLocalPlayer) {
			decimal modifier = (32 / maxHealth);
			decimal gigaDamage = damage;
			if (originalDamage > damage) {
				gigaDamage = originalDamage;
			}
			float gigaAmmoToAdd = (float)(gigaDamage * modifier);
	
			if (this is Zero zero) {
				float currentAmmo = zero.gigaAttack.ammo;
				zero.gigaAttack.addAmmo(gigaAmmoToAdd, player);
				if (player.isMainPlayer) {
					Weapon.gigaAttackSoundLogic(
						this, currentAmmo, zero.gigaAttack.ammo,
						zero.gigaAttack.getAmmoUsage(0), zero.gigaAttack.maxAmmo
					);
				}
			}
			if (this is PunchyZero punchyZero) {
				float currentAmmo = punchyZero.gigaAttack.ammo;
				punchyZero.gigaAttack.addAmmo(gigaAmmoToAdd, player);
				if (player.isMainPlayer) {
					Weapon.gigaAttackSoundLogic(
						this, currentAmmo, punchyZero.gigaAttack.ammo,
						punchyZero.gigaAttack.getAmmoUsage(0), punchyZero.gigaAttack.maxAmmo
					);
				}
			}
			if (this is MegamanX) {
				var gigaCrush = player.weapons.FirstOrDefault(w => w is GigaCrush);
				if (gigaCrush != null) {
					float currentAmmo = gigaCrush.ammo;
					gigaCrush.addAmmo(gigaAmmoToAdd, player);
					if (player.isMainPlayer) {
						Weapon.gigaAttackSoundLogic(
							this, currentAmmo, gigaCrush.ammo,
							gigaCrush.getAmmoUsage(0), gigaCrush.maxAmmo
						);
					}
				}
				var hyperBuster = player.weapons.FirstOrDefault(w => w is HyperCharge);
				if (hyperBuster != null) {
					float currentAmmo = hyperBuster.ammo;
					hyperBuster.addAmmo(gigaAmmoToAdd, player);
					if (player.isMainPlayer) {
						Weapon.gigaAttackSoundLogic(
							this, currentAmmo, hyperBuster.ammo,
							hyperBuster.getAmmoUsage(0), hyperBuster.maxAmmo,
							"hyperchargeRecharge", "hyperchargeFull"
						);
					}
				}
				var novaStrike = player.weapons.FirstOrDefault(w => w is HyperNovaStrike);
				if (novaStrike != null) {
					float currentAmmo = novaStrike.ammo;
					novaStrike.addAmmo(gigaAmmoToAdd, player);
					if (player.isMainPlayer) {
						Weapon.gigaAttackSoundLogic(
							this, currentAmmo, novaStrike.ammo,
							novaStrike.getAmmoUsage(0), novaStrike.maxAmmo
						);
					}
				}
			}
			if (this is NeoSigma) {
				player.sigmaAmmo = Helpers.clampMax(player.sigmaAmmo + gigaAmmoToAdd, player.sigmaMaxAmmo);
			}
		}

		if ((damage > 0 || Damager.alwaysAssist(projId)) && attacker != null && weaponIndex != null) {
			damageHistory.Add(new DamageEvent(attacker, weaponIndex.Value, projId, false, Global.time));
		}

		if (health <= 0) {
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
				mmx.activateMaxBarrier(charState is Hurt or GenericStun);
			}
		}
	}

	public void killPlayer(Player? killer, Player? assister, int? weaponIndex, int? projId) {
		health = 0;
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
		if (health >= maxHealth && fillSubtank) {
			player.fillSubtank(amount);
		}
		healAmount += amount;
	}

	public void fillHealthToMax() {
		healAmount += (float)Math.Ceiling(maxHealth);
	}

	public virtual void addAmmo(float amount) {
		player.weapon?.addAmmoHeal(amount);
	}

	public virtual void addPercentAmmo(float amount) {
		player.weapon?.addAmmoPercentHeal(amount);
	}

	public virtual bool canAddAmmo() {
		return false;
	}

	public virtual void increaseCharge() {
		chargeTime += Global.speedMul;
	}

	public void dropFlag() {
		if (flag != null) {
			flag.dropFlag();
			flag = null;
		}
	}

	public virtual void onFlagPickup(Flag flag) {
		if (flag == null) {
			return;
		}
		dropFlagProgress = 0;
		this.flag = flag;

		if (player.isDisguisedAxl && player.ownedByLocalPlayer) {
			player.revertToAxl();
		}
	}

	public void setHurt(int dir, int flinchFrames, bool spiked) {
		if (!ownedByLocalPlayer) {
			return;
		}
		// Tough Guy.
		if (player.isSigma || isToughGuyHyperMode()) {
			flinchFrames = 6;
		}
		if (charState is GenericStun genericStunState) {
			genericStunState.activateFlinch(flinchFrames, dir);
			return;
		}
		if (charState is Hurt hurtState) {
			if (flinchFrames >= hurtState.flinchLeft) {
				// You can probably add a check here that sets "hurtState.yStartPos" to null if you.
				// Want to add a flinch attack that pushes up on chain-flinch.
				changeState(new Hurt(dir, flinchFrames, false, hurtState.flinchYPos), true);
				return;
			}
			return;
		}
		if (charState is GenericStun stunState) {
			// We disable the jump as we mid-flinch movement.
			changeState(new Hurt(dir, flinchFrames, true, stunState.flinchYPos), true);
			return;
		}
		if (charState is not Die and not InRideArmor and not InRideChaser) {
			changeState(new Hurt(dir, flinchFrames, spiked), true);
			return;
		}
	}

	public override void destroySelf(
		string spriteName = "", string fadeSound = "",
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
		burnTime = 0;
		acidTime = 0;
		oilTime = 0;
		player.possessedTime = 0;
		igFreezeProgress = 0;
		virusTime = 0;
	}

	

	public void crystalizeStart() {
		isCrystalized = true;
		if (globalCollider != null) globalCollider.isClimbable = true;
		new Anim(getCenterPos(), "crystalhunter_activate", 1, null, true);
		playSound("crystalize");
	}

	public void crystalizeEnd() {
		isCrystalized = false;
		playSound("crystalizeDashingX2");
		for (int i = 0; i < 8; i++) {
			var anim = new Anim(getCenterPos().addxy(Helpers.randomRange(-20, 20), Helpers.randomRange(-20, 20)), "crystalhunter_piece", Helpers.randomRange(0, 1) == 0 ? -1 : 1, null, false);
			anim.frameIndex = Helpers.randomRange(0, 1);
			anim.frameSpeed = 0;
			anim.useGravity = true;
			anim.vel = new Point(Helpers.randomRange(-150, 150), Helpers.randomRange(-300, 25));
		}
	}

	// PARASITE SECTION
	public void addParasite(Player attacker) {
		if (!ownedByLocalPlayer) return;
		if (charState.invincible || isInvulnerable()) return;
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
				if (otherPlayer == parasiteDamager?.owner) continue;
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

	public float getKaiserStompDamage() {
		float damagePercent = 0.25f;
		if (deltaPos.y > 150 * Global.spf) damagePercent = 0.5f;
		if (deltaPos.y > 210 * Global.spf) damagePercent = 0.75f;
		if (deltaPos.y > 300 * Global.spf) damagePercent = 1;
		return damagePercent;
	}

	public void releaseGrab(Actor grabber, bool sendRpc = false) {
		charState.releaseGrab();
		if (!ownedByLocalPlayer) {
			RPC.commandGrabPlayer.sendRpc(
				grabber.netId, netId, CommandGrabScenario.Release, grabber.isDefenderFavored()
			);
			changeState(new NetLimbo());
		}
	}

	public bool isAlwaysHeadshot() {
		return sprite?.name?.Contains("_ra_") == true || sprite?.name?.Contains("_rc_") == true;
	}

	public bool canEjectFromRideArmor() {
		if (rideArmor == null) {
			return true;
		}
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

	public bool canLandOnRideArmor() {
		if (charState is Fall) return true;
		if (charState is VileHover vh && vh.fallY > 0) return true;
		return false;
	}

	public void getOffMK5Platform() {
		if (rideArmorPlatform != null) {
			if (rideArmorPlatform != linkedRideArmor) {
				rideArmorPlatform.character = null;
				rideArmorPlatform.changeState(new RADeactive(), true);
			}
			rideArmorPlatform = null;
		}
	}

	public bool canAffordRideArmor() {
		if (Global.level.is1v1()) {
			return health > Math.Floor(maxHealth / 2);
		}
		return player.currency >= Vile.callNewMechCost;
	}

	public void buyRideArmor() {
		if (Global.level.is1v1()) {
			health -= Math.Floor(maxHealth / 2);
			return;
		}
		player.currency -= Vile.callNewMechCost * (player.selectedRAIndex >= 4 ? 2 : 1);
	}

	public virtual void onMechSlotSelect(MechMenuWeapon mmw) {
		if (linkedRideArmor == null) {
			if (!mmw.isMenuOpened) {
				mmw.isMenuOpened = true;
				return;
			}
		}

		if (linkedRideArmor == null) {
			if (alreadySummonedNewMech) {
				Global.level.gameMode.setHUDErrorMessage(player, "Can only summon a mech once per life");
			} else if (canAffordRideArmor()) {
				if (!(charState is Idle || charState is Run || charState is Crouch)) return;
				alreadySummonedNewMech = true;
				if (linkedRideArmor != null) linkedRideArmor.selfDestructTime = 1000;
				buyRideArmor();
				mmw.isMenuOpened = false;
				int raIndex = player.selectedRAIndex;
				linkedRideArmor = new RideArmor(
					player, pos, raIndex, 0, player.getNextActorNetId(), true, sendRpc: true
				);
			}
		} else {
			rideArmor?.changeState(new RACalldown(pos, false), true);
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
		*/

		if (this is Zero or PunchyZero or BusterZero or Vile) {
			player.changeWeaponControls();
		}

		if (player.weapon is UndisguiseWeapon) {
			bool shootPressed = player.input.isPressed(Control.Shoot, player);
			bool altShootPressed = player.input.isPressed(Control.Special1, player);
			if ((shootPressed || altShootPressed)) {
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

		if (player.weapon is AssassinBullet && (this is Vile or BaseSigma)) {
			if (player.input.isHeld(Control.Shoot, player)) {
				increaseCharge();
			} else {
				if (isCharging()) {
					shootAssassinShot();
				}
				stopCharge();
			}
			chargeGfx();
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
		transformAnim = new Anim(pos, "axl_transform", xDir, player.getNextActorNetId(), true);
		playSound("transform", sendRpc: true);
	}

	public virtual void onFlinchOrStun(CharState state) {

	}

	public virtual void onWeaponChange(Weapon oldWeapon, Weapon newWeapon) {}

	public virtual void onExitState(CharState oldState, CharState newState) {

	}

	public virtual bool chargeButtonHeld() {
		return false;
	}


	public virtual void chargeLogic(Action<int> shootFunct) {
		// Charge shoot logic.
		// We test if we are holding charge and not inside a vehicle.
		if (chargeButtonHeld() && flag == null && rideChaser == null && rideArmor == null) {
			// If we are holding but we cannot charge we do not release.
			if (canCharge()) {
				increaseCharge();
			}
		}
		// Release charge only if not holding and we can attack.
		// This to prevent from losing charge.
		else if (canShoot()) {
			int chargeLevel = getChargeLevel();
			if (isCharging()) {
				if (chargeLevel >= 1) {
					shootFunct(chargeLevel);
				}
			}
			stopCharge();
		}
		chargeGfx();
	}

	public virtual void aiUpdate(Actor? target) { }

	public virtual void aiAttack(Actor target) { }

	public virtual void aiDodge(Actor? target) { }

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();

		// For keeping track how long the normal args are.
		// We need to edit this later.
		customData.Add(0);

		// Always on values.
		customData.Add((byte)Math.Ceiling(health));
		customData.Add((byte)Math.Ceiling(maxHealth));
		customData.Add((byte)player.alliance);
		customData.Add((byte)player.currency);

		// Bool variables. Packed in a single byte.
		customData.Add(Helpers.boolArrayToByte([
			player.isDefenderFavored,
			invulnTime > 0,
			isDarkHoldState,
			isStrikeChainState,
			charState.immuneToWind
		]));

		// Bool mask. Pos 5.
		// For things not always enabled.
		// We also edit this later.
		int boolMaskPos = customData.Count();
		bool[] boolMask = new bool[8];
		customData.Add(0);

		// Add each status effect and enabled their respective flag.
		if (acidTime > 0) {
			customData.Add((byte)MathF.Ceiling(acidTime * 20));
			boolMask[0] = true;
		}
		if (burnTime > 0) {
			customData.Add((byte)MathF.Ceiling(burnTime * 30));
			boolMask[1] = true;
		}
		if (chargeTime > 0) {
			customData.Add((byte)MathF.Ceiling(chargeTime / 3));
			boolMask[2] = true;
		}
		if (igFreezeProgress > 0) {
			customData.Add((byte)MathF.Ceiling(igFreezeProgress * 30));
			boolMask[3] = true;
		}
		if (oilTime > 0) {
			customData.Add((byte)MathF.Ceiling(oilTime * 30));
			boolMask[4] = true;
		}
		if (virusTime > 0) {
			customData.Add((byte)MathF.Ceiling(virusTime * 30));
			boolMask[5] = true;
		}
		if (vaccineTime > 0) {
			customData.Add((byte)MathF.Ceiling(vaccineTime * 30));
			boolMask[6] = true;
		}
		if (rideArmor?.netId != null && rideArmor.netId != 0 ||
			rideChaser?.netId != null && rideChaser.netId != 0
		) {
			if (rideArmor != null) {
				customData.AddRange(BitConverter.GetBytes(rideArmor?.netId ?? ushort.MaxValue));
			} else {
				customData.AddRange(BitConverter.GetBytes(rideChaser?.netId ?? ushort.MaxValue));
			}
			boolMask[7] = true;
		}
		// Add the final value of the bool mask.
		customData[boolMaskPos] = Helpers.boolArrayToByte(boolMask);

		// Add the total arguments size.
		customData[0] = (byte)customData.Count;

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Always on values.
		health = data[1];
		maxHealth = data[2];
		player.alliance = data[3];
		currency = data[4];

		// Bool variables.
		bool[] boolData = Helpers.byteToBoolArray(data[5]);

		player.isDefenderFavoredNonOwner = boolData[0];
		invulnTime = (boolData[1] ? 1 : 0);
		isDarkHoldState = boolData[2];
		isStrikeChainState = boolData[3];
		charState.immuneToWind = boolData[4];

		// Optional statuses.
		bool[] boolMask = Helpers.byteToBoolArray(data[6]);
		int pos = 7;
		// Update and increase pos as we go.
		if (boolMask[0]) {
			acidTime = data[pos] / 30f;
			pos++;
		}
		if (boolMask[1]) {
			burnTime = data[pos] / 30f;
			pos++;
		}
		if (boolMask[2]) {
			chargeTime = data[pos] * 3;
			pos++;
		}
		if (boolMask[3]) {
			igFreezeProgress = data[pos] / 30f;
			pos++;
		}
		if (boolMask[4]) {
			oilTime = data[pos] / 30f;
			pos++;
		}
		if (boolMask[5]) {
			virusTime = data[pos] / 30f;
			pos++;
		}
		if (boolMask[6]) {
			vaccineTime = data[pos] / 30f;
			pos++;
		}
		if (boolMask[7]) {
			Actor? vehicleActor = Global.level.getActorByNetId(BitConverter.ToUInt16(data[pos..(pos+2)]));
			if (vehicleActor is RideArmor rav) {
				rideArmor = rav;
				rav.zIndex = zIndex - 10;
			}
			else if (vehicleActor is RideChaser rcv) {
				rideChaser = rcv;
				rcv.zIndex = zIndex - 10;
			}
			pos += 2;
		} else {
			rideArmor = null;
			rideChaser = null;
		}
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
