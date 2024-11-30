using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class MegamanX : Character {
	// Shoot variables.
	public float shootCooldown;
	public int lastShootPressed;
	public float xSaberCooldown;
	public XBuster specialBuster;
	public int specialButtonMode;

	// Armor variables.
	public ArmorId bodyArmor;
	public ArmorId armArmor;
	public ArmorId legArmor;
	public ArmorId helmetArmor;
	
	public ArmorId hyperBodyActive;
	public ArmorId hyperArmActive;
	public ArmorId hyperLegActive;
	public ArmorId hyperHelmetActive;

	public ArmorId hyperBodyArmor;
	public ArmorId hyperArmArmor;
	public ArmorId hyperLegArmor;
	public ArmorId hyperHelmetArmor;

	public const int headArmorCost = 2;
	public const int bodyArmorCost = 3;
	public const int armArmorCost = 3;
	public const int bootsArmorCost = 2;

	public float headbuttAirTime = 0;
	public int hyperChargeTarget;
	public bool stockedBuster;
	public bool stockedMaxBuster;
	public float noDamageTime;
	public float rechargeHealthTime;

	// Shoto moves.
	public float hadoukenCooldownTime;
	public float maxHadoukenCooldownTime = 60;
	public float shoryukenCooldownTime;
	public float maxShoryukenCooldownTime = 60;

	// HyperX stuff.
	public bool hasSeraphArmor;
	public bool hasFullHyperMaxArmor => (
		hyperBodyArmor == ArmorId.Max &&
		hyperArmArmor == ArmorId.Max &&
		hyperLegActive == ArmorId.Max &&
		hyperHelmetArmor == ArmorId.Max
	);
	public bool usedChips;

	// Giga-attacks and armor weapons.
	public Weapon? gigaWeapon;
	public HyperNovaStrike? seraphNovaStrike;
	public ItemTracer? itemTracer;
	public float barrierCooldown;
	public float barrierActiveTime;
	public Sprite barrierAnim = new Sprite("barrier_start");
	public bool stockedCharge;
	public bool stockedX3Charge;
	public bool stockedSaber;

	// Weapon-specific.
	public RollingShieldProjCharged? chargedRollingShieldProj;
	public List<BubbleSplashProjCharged> chargedBubbles = new();
	public StrikeChainProj? strikeChainProj;
	public StrikeChainProjCharged? strikeChainChargedProj;
	public GravityWellProjCharged? chargedGravityWell;
	public SpinningBladeProjCharged? chargedSpinningBlade;
	public FrostShieldProjCharged? chargedFrostShield;
	public TornadoFangProjCharged? chargedTornadoFang;
	public GravityWellProj? linkedGravityWell;
	public TriadThunderProj? linkedTriadThunder;
	public BeeSwarm? chargedParasiticBomb;
	public List<MagnetMineProj> magnetMines = new();
	public List<RaySplasherTurret> rayTurrets = new();
	public RaySplasher? shootingRaySplasher = new();

	// Chamaleon Sting.
	public float stingActiveTime;
	public int cStingPaletteIndex;
	public float cStingPaletteTime;
	// Other.
	public float WeaknessCooldown;

	// Creation code.
	public MegamanX(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.X;
		specialBuster = new XBuster();
	}

	// Updates at the start of the frame.
	public override void preUpdate() {
		base.preUpdate();
		Helpers.decrementFrames(ref barrierActiveTime);

		// Max armor barrier sprite.
		if (barrierActiveTime > 0) {
			barrierAnim.update();
			if (barrierAnim.name == "barrier_start" && barrierAnim.isAnimOver()) {
				if (player.hasChip(1)) {
					barrierAnim = new Sprite("barrier2");
				} else {
					barrierAnim = new Sprite("barrier");
				}
			}
		}

		Helpers.decrementFrames(ref WeaknessCooldown);
		if (!ownedByLocalPlayer) {
			return;
		}
		Helpers.decrementFrames(ref shootCooldown);
		Helpers.decrementFrames(ref barrierCooldown);
		Helpers.decrementFrames(ref xSaberCooldown);

		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= Global.speedMul;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				changeSpriteFromName(charState.defaultSprite, false);
				if (charState is WallSlide) {
					frameIndex = sprite.totalFrameNum - 1;
				}
			}
		}
	}

	// General update.
	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) {
			return;
		}
		gigaWeapon?.update();
		seraphNovaStrike?.update();
		itemTracer?.update();
		shootingRaySplasher?.burstLogic(this);
	}

	// Late updates. Before render.
	public override void postUpdate() {
		base.postUpdate();

		if (stockedSaber) {
			addRenderEffect(RenderEffectType.ChargeGreen, 2, 6);
		}
		else if (stockedX3Charge) {
			addRenderEffect(RenderEffectType.ChargeOrange, 2, 6);
		}
		else if (stockedBuster) {
			addRenderEffect(RenderEffectType.ChargePink, 2, 6);
		}
	}

	// Shoots stuff.
	public void shoot(int chargeLevel) {
		// Changes to shoot animation.
		setShootAnim();
		shootAnimTime = DefaultShootAnimTime;

		// Calls the weapon shoot function.
		player.weapon.shoot(this, [chargeLevel]);
		// Sets up global shoot cooldown to the weapon shootCooldown.
		shootCooldown = player.weapon.shootCooldown;

		// Stop charge if this was a charge shot.
		if (chargeLevel >= 1) {
			stopCharge();
		}
	}

	// Movement related stuff.
	public override float getDashSpeed() {
		if (flag != null || !isDashing) {
			return getRunSpeed();
		}
		float dashSpeed = 3.5f * 60;
		if (legArmor == ArmorId.Light && charState is Dash) {
			dashSpeed *= 1.15f;
		} else if (legArmor == ArmorId.Giga && charState is AirDash) {
			dashSpeed *= 1.15f;
		}
		return dashSpeed * getRunDebuffs();
	}

	public override bool canAirDash() {
		return dashedInAir == 0 || (dashedInAir == 1 && hyperLegArmor == ArmorId.Max);
	}

	// Handles Bubble Splash Charged jump height
	public override float getJumpPower() {
		float jumpModifier = 0;
		jumpModifier += (chargedBubbles.Count / 6.0f) * 50;

		return jumpModifier + base.getJumpPower();
	}

	
	public override float getGravity() {
			float modifier = 1;
		if (chargedBubbles.Count > 0) {
			if (isUnderwater()) {
				modifier = 1 - (0.01f * chargedBubbles.Count);
			} else {
				modifier = 1 - (0.05f * chargedBubbles.Count);
			}
		}
		return base.getGravity() * modifier;
	}

	// Attack related stuff.
	public void setShootAnim() {
		string shootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) { shootSprite = getSprite("shoot"); } else { shootSprite = getSprite("fall_shoot"); }
		}
		if (shootAnimTime == 0) {
			changeSprite(shootSprite, false);
		} else if (charState is Idle) {
			frameIndex = 0;
			frameTime = 0;
		}
	}
	
	public override bool canShoot() {
		if (isInvulnerableAttack() ||
			hasLastingProj() ||
			shootCooldown > 0 ||
			invulnTime > 0 ||
			linkedTriadThunder != null
		) {
			return false;
		}
		return charState.attackCtrl;
	}

	public override bool canCharge() {
		return !isInvulnerableAttack() || hasLastingProj();
	}

	public override bool canChangeWeapons() {
		if (hasLastingProj()) {
			return false;
		}
		return base.canChangeWeapons();
	}

	public override void onWeaponChange(Weapon oldWeapon, Weapon newWeapon) {
		stingActiveTime = 0;
	}

	public void activateMaxBarrier(bool isFlinchOrStun) {
		if (!ownedByLocalPlayer ||
			barrierActiveTime > 0 ||
			barrierCooldown > 0
		) {
			return;
		}
		if (isFlinchOrStun) {
			barrierActiveTime = 90;
		} else {
			barrierActiveTime = 45;
		}
	}

	public bool hasHadoukenEquipped() {
		return !Global.level.is1v1() &&
		player.hasArmArmor(1) && player.hasBootsArmor(1) &&
		player.hasHelmetArmor(1) && player.hasBodyArmor(1);
	}

	public bool hasShoryukenEquipped() {
		return !Global.level.is1v1() &&
		player.hasArmArmor(2) && player.hasBootsArmor(2) &&
		player.hasHelmetArmor(2) && player.hasBodyArmor(2);
	}

	public bool hasFgMoveEquipped() {
		return hasHadoukenEquipped() || hasShoryukenEquipped();
	}

	public bool canAffordFgMove() {
		return player.currency >= 3 || player.hasAllItems();
	}

	public bool canUseFgMove() {
		return 
			!isInvulnerableAttack() && 
			chargedRollingShieldProj == null && 
			stingActiveTime == 0 && canAffordFgMove() && 
			hadoukenCooldownTime == 0 && player.weapon is XBuster && 
			player.fgMoveAmmo >= player.fgMoveMaxAmmo && grounded;
	}

	public bool hasLastingProj() {
		return (
			chargedSpinningBlade != null ||
			chargedFrostShield != null ||
			chargedTornadoFang != null ||
			strikeChainProj != null ||
			strikeChainChargedProj != null
		);
	}

	public void removeLastingProjs() {
		chargedSpinningBlade?.destroySelf();
		chargedSpinningBlade = null;
		chargedFrostShield?.destroySelf();
		chargedFrostShield = null;
		chargedTornadoFang?.destroySelf();
		chargedTornadoFang = null;
		strikeChainProj?.destroySelf();
		strikeChainProj = null;
		strikeChainChargedProj?.destroySelf();
		strikeChainChargedProj = null;
	}
	
	public void popAllBubbles() {
		for (int i = chargedBubbles.Count - 1; i >= 0; i--) {
			chargedBubbles[i].destroySelf();
		}
		chargedBubbles.Clear();
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"mmx_speedburner" => MeleeIds.SpeedBurnerCharged,
			"mmx_shoryuken" => MeleeIds.Shoryuken,
			"mmx_beam_saber" or "mmx_beam_saber_air" => MeleeIds.MaxZSaber,
			"mmx_beam_saber2" => MeleeIds.ZSaber,
			"mmx_beam_saber_air2" => MeleeIds.ZSaberAir,
			"mmx_nova_strike" or "mmx_nova_strike_down" or "mmx_nova_strike_up" => MeleeIds.NovaStrike,
			// Light Helmet.
			"mmx_jump" or "mmx_jump_shoot" or "mmx_wall_kick" or "mmx_wall_kick_shoot"
			when helmetArmor == ArmorId.Light && stingActiveTime == 0 => MeleeIds.LigthHeadbutt,
			// Light Helmet when it up-dashes.
			"mmx_up_dash" or "mmx_up_dash_shoot"
			when helmetArmor == ArmorId.Light && stingActiveTime == 0 => MeleeIds.LigthHeadbuttEX,
			// Nothing.
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		return id switch {
			(int)MeleeIds.SpeedBurnerCharged => new GenericMeleeProj(
				SpeedBurner.netWeapon, projPos, ProjIds.SpeedBurnerCharged, player,
				4, Global.defFlinch, 0.5f
			),
			(int)MeleeIds.LigthHeadbutt => new GenericMeleeProj(
				LhHeadbutt.netWeapon, projPos, ProjIds.Headbutt, player,
				2, Global.halfFlinch, 0.5f, addToLevel: addToLevel
			),
			(int)MeleeIds.LigthHeadbuttEX => new GenericMeleeProj(
				LhHeadbutt.netWeapon, projPos, ProjIds.Headbutt, player,
				4, Global.defFlinch, 0.5f, addToLevel: addToLevel
			),
			(int)MeleeIds.Shoryuken => new GenericMeleeProj(
				ShoryukenWeapon.netWeapon, projPos, ProjIds.Shoryuken, player,
				Damager.ohkoDamage, Global.defFlinch, 0.5f, addToLevel: addToLevel
			),
			(int)MeleeIds.MaxZSaber => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.XSaber, player,
				4, Global.defFlinch, 0.5f, addToLevel: addToLevel
			),
			(int)MeleeIds.ZSaber => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.X6Saber, player,
				3, 0, 0.5f, addToLevel: addToLevel
			),
			(int)MeleeIds.ZSaberAir => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.X6Saber, player,
				2, 0, 0.5f, addToLevel: addToLevel
			),
			(int)MeleeIds.NovaStrike => new GenericMeleeProj(
				HyperNovaStrike.netWeapon, projPos, ProjIds.NovaStrike, player,
				4, Global.defFlinch, 0.5f, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public enum MeleeIds {
		None = -1,
		SpeedBurnerCharged,
		LigthHeadbutt,
		LigthHeadbuttEX,
		Shoryuken,
		MaxZSaber,
		ZSaber,
		ZSaberAir,
		NovaStrike,
	}

	// Other overrides.
	public override void onFlinchOrStun(CharState newState) {
		strikeChainProj?.destroySelf();
		strikeChainChargedProj?.destroySelf();
		// Remove all linked stuff on stun.
		if (newState is not Hurt hurtState) {
			removeLastingProjs();
		}
		// Reset P-Bomb on flinch.
		else {
			chargedParasiticBomb?.reset(hurtState.isMiniFlinch());
		}
		base.onFlinchOrStun(newState);
	}

	public override bool changeState(CharState newState, bool forceChange = false) {
		bool hasChanged = base.changeState(newState, forceChange);
		if (!hasChanged || !ownedByLocalPlayer) {
			return hasChanged;
		}
		if (!newState.canUseShootAnim() && charState is not Hurt) {
			removeLastingProjs();
		}
		return true;
	}

	public override void getHealthNameOffsets(out bool shieldDrawn, ref float healthPct) {
		shieldDrawn = false;
		if (rideArmor != null) {
			shieldDrawn = true;
			healthPct = rideArmor.health / rideArmor.maxHealth;
		}
		else if (chargedRollingShieldProj != null) {
			shieldDrawn = true;
			healthPct = player.weapon.ammo / player.weapon.maxAmmo;
		}
	}

	public override void destroySelf(
		string spriteName = "", string fadeSound = "",
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {

		removeLastingProjs();
		chargedRollingShieldProj?.destroySelfNoEffect();
		strikeChainProj?.destroySelf();
		strikeChainChargedProj?.destroySelf();
		chargedParasiticBomb?.destroy();

		for (int i = magnetMines.Count - 1; i >= 0; i--) {
			magnetMines[i].destroySelf();
		}
		magnetMines.Clear();
		for (int i = rayTurrets.Count - 1; i >= 0; i--) {
			rayTurrets[i].destroySelf();
		}
		rayTurrets.Clear();

		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy);
	}

	public override string getSprite(string spriteName) {
		return "mmx_" + spriteName;
	}

	public override void render(float x, float y) {
		if (!shouldRender(x, y)) {
			return;
		}
		base.render(x, y);
	}
}
