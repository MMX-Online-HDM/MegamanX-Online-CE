﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class MegamanX : Character {
	// Shoot variables.
	public float shootCooldown;
	public int lastShootPressed;
	public bool bufferedShotPressed => lastShootPressed < 6 || player.input.isPressed(Control.Shoot, player);
	public float specialSaberCooldown;
	public XBuster specialBuster;
	public int specialButtonMode;

	// Armor variables.
	public ArmorId chestArmor;
	public ArmorId armArmor;
	public ArmorId legArmor;
	public ArmorId helmetArmor;
	
	public ArmorId hyperChestArmor => (hyperChestActive ? helmetArmor : ArmorId.None);
	public ArmorId hyperArmArmor => (hyperArmActive ? armArmor : ArmorId.None);
	public ArmorId hyperLegArmor => (hyperLegActive ? legArmor : ArmorId.None);
	public ArmorId hyperHelmetArmor => (hyperHelmetActive ? helmetArmor : ArmorId.None);
	
	public ArmorId fullArmor => (
		chestArmor == armArmor &&
		armArmor == legArmor &&
		legArmor == helmetArmor
		? chestArmor
		: 0
	);

	public bool hyperChestActive;
	public bool hyperArmActive;
	public bool hyperLegActive;
	public bool hyperHelmetActive;

	public const int headArmorCost = 2;
	public const int chestArmorCost = 3;
	public const int armArmorCost = 3;
	public const int bootsArmorCost = 2;

	public float headbuttAirTime = 0;
	public int hyperChargeTarget;
	public float noDamageTime;
	public float rechargeHealthTime;

	// Shoto moves.
	public float hadoukenCooldownTime;
	public float maxHadoukenCooldownTime = 60;
	public float shoryukenCooldownTime;
	public float maxShoryukenCooldownTime = 60;

	// HyperX stuff.
	public bool hasUltimateArmor;
	public bool hasFullHyperMaxArmor => (
		hyperChestArmor == ArmorId.Max &&
		hyperArmArmor == ArmorId.Max &&
		hyperLegArmor == ArmorId.Max &&
		hyperHelmetArmor == ArmorId.Max
	);
	public bool hasAnyArmor => (
		chestArmor != 0 ||
		armArmor != 0 ||
		legArmor != 0 ||
		helmetArmor != 0
	);
	public bool hasAnyHyperArmor => (
		hyperChestActive ||
		hyperArmActive ||
		hyperLegActive ||
		hyperHelmetActive
	);
	public bool usedChips;

	// Giga-attacks and armor weapons.
	public Weapon? gigaWeapon;
	public HyperNovaStrike? hyperNovaStrike;
	public ItemTracer itemTracer = new();
	public float barrierCooldown;
	public float barrierActiveTime;
	public Sprite barrierAnim = new Sprite("barrier_start");
	public bool stockedSaber;
	public bool hyperChargeActive;
	public bool stockedBuster;
	public bool stockedMaxBuster;

	// Weapon-specific.
	public RollingShieldProjCharged? chargedRollingShieldProj;
	public List<BubbleSplashProjCharged> chargedBubbles = new();
	public Projectile? strikeChainProj;
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
	public int stingPaletteIndex;
	public float stingPaletteTime;
	public float chargePalleteTime;
	// Other.
	public float weaknessCooldown;
	public float aiAttackCooldown;
	public float stockedTime;
	public XLoadout loadout;

	// Creation code.
	public MegamanX(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, XLoadout? xLoadout = null
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.X;
		// Configure loadout.
		if (xLoadout == null) {
			// Copy if null;
			XLoadout playerLoadout = player.loadout.xLoadout;
			xLoadout = new XLoadout();
			xLoadout.weapon1 = playerLoadout.weapon1;
			xLoadout.weapon2 = playerLoadout.weapon2;
			xLoadout.weapon3 = playerLoadout.weapon3;
			xLoadout.melee = playerLoadout.melee;
		}
		// Set up final loadout.
		weapons = XLoadoutSetup.getLoadout(player, xLoadout);
		specialButtonMode = xLoadout.melee;
		// Link X-Buster or create one.
		XBuster? tempBuster = weapons.Find((Weapon w) => w is XBuster) as XBuster;
		if (tempBuster != null) {
			specialBuster = tempBuster;
		} else {
			specialBuster = new XBuster();
		}
		// Armor shenanigas.
		chestArmor = (ArmorId)player.bodyArmorNum;
		armArmor = (ArmorId)player.armArmorNum;
		legArmor = (ArmorId)player.legArmorNum;
		helmetArmor = (ArmorId)player.helmetArmorNum;
	}

	// Updates at the start of the frame.
	public override void preUpdate() {
		base.preUpdate();
		Helpers.decrementFrames(ref barrierActiveTime);

		// Max armor barrier sprite.
		if (barrierActiveTime > 0) {
			barrierAnim.update();
			if (barrierAnim.name == "barrier_start" && barrierAnim.isAnimOver()) {
				if (hyperChestArmor == ArmorId.Max) {
					barrierAnim = new Sprite("barrier2");
				} else {
					barrierAnim = new Sprite("barrier");
				}
			}
		}

		Helpers.decrementFrames(ref weaknessCooldown);
		if (!ownedByLocalPlayer) {
			return;
		}
		Helpers.decrementFrames(ref shootCooldown);
		Helpers.decrementFrames(ref barrierCooldown);
		Helpers.decrementFrames(ref specialSaberCooldown);
		Helpers.decrementFrames(ref hadoukenCooldownTime);
		Helpers.decrementFrames(ref shoryukenCooldownTime);

		if (lastShootPressed < 100) {
			lastShootPressed++;
		}
		player.hadoukenAmmo += Global.speedMul;
		if (player.hadoukenAmmo > player.fgMoveMaxAmmo) player.hadoukenAmmo = player.fgMoveMaxAmmo;
		player.shoryukenAmmo += Global.speedMul;
		if (player.shoryukenAmmo > player.fgMoveMaxAmmo) player.shoryukenAmmo = player.fgMoveMaxAmmo;

		Helpers.decrementFrames(ref aiAttackCooldown);
		// Strike chain extending the shoot anim.
		if (hasLastingProj() && shootAnimTime > 0) {
			shootAnimTime = 4;
		}
		// For the shooting animation.
		if (shootAnimTime > 0 && sprite.name == getSprite(charState.shootSpriteEx)) {
			shootAnimTime -= speedMul;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				if (sprite.name == getSprite(charState.shootSpriteEx)) {
					changeSpriteFromName(charState.defaultSprite, false);
					if (charState is WallSlide) {
						frameIndex = sprite.totalFrameNum - 1;
					}
				}
			}
		}
		if (stockedSaber || stockedMaxBuster || stockedBuster) {
			stockedTime += Global.spf;
			if (stockedTime >= 61f/60f) {
				stockedTime = 0;
				playSound("stockedSaber");
			}
		}
	}

	// General update.
	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) {
			return;
		}
		if (player.input.isPressed(Control.Shoot, player)) {
			lastShootPressed = 6;
		}
		chargedParasiticBomb?.update();
		gigaWeapon?.update();
		hyperNovaStrike?.update();
		itemTracer?.update();
		shootingRaySplasher?.burstLogic(this);

		// Charge and release charge logic.
		chargeLogic(shoot);
		player.changeWeaponControls();
	}

	// Late updates. Before render.
	public override void postUpdate() {
		base.postUpdate();

		if (!charState.normalCtrl) {
			lastShootPressed = 100;	
		}
	}

	public override bool normalCtrl() {
		quickArmorUpgrade();
		if (grounded) {
			if (legArmor == ArmorId.Max &&
				player.input.isPressed(Control.Dash, player) &&
				player.input.isHeld(Control.Up, player) &&
				canDash() && flag == null
			) {
				changeState(new UpDash(Control.Dash));
				return true;
			}
			if (legArmor == ArmorId.Light && grounded &&
				player.dashPressed(out string dashControlL) &&
				canDash()
			) {
				changeState(new LightDash(dashControlL), true);
				return true;
			}
		} else if (!grounded) {
			if (legArmor == ArmorId.Max &&
				player.input.isPressed(Control.Dash, player) &&
				player.input.isHeld(Control.Up, player) &&
				canAirDash() && canDash() && flag == null
			) {
				changeState(new UpDash(Control.Dash));
				return true;
			}
			if (legArmor == ArmorId.Giga && !grounded &&
				player.dashPressed(out string dashControlG) &&
				canAirDash() && canDash()
			) {
				changeState(new GigaAirDash(dashControlG), true);
				return true;
			}
			if (!player.isAI && hasUltimateArmor &&
				player.input.isPressed(Control.Jump, player) &&
				canJump() && !isDashing && canAirDash() && flag == null
			) {
				dashedInAir++;
				changeState(new XHover(), true);
				return true;
			}
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		if (stockedSaber && player.input.isPressed(Control.Special1, player)) {
			changeState(new XMaxWaveSaberState(), true);
			return true;
		}
		if (player.input.isPressed(Control.Special1, player) && !hasAnyArmor) {
			if (specialButtonMode == 1) {
				changeState(new X6SaberState(grounded), true);
				return true;
			} else {
				shoot(0, specialBuster, false);
				return true;
			}
		}
		if (player.input.isPressed(Control.Special1, player) && helmetArmor == ArmorId.Giga &&
			itemTracer.shootCooldown == 0
		) {
			itemTracer.shoot(this, [0, hyperHelmetArmor == ArmorId.Giga ? 1 : 0]);
			itemTracer.shootCooldown = itemTracer.fireRate;
		}
		if (bufferedShotPressed && stockedMaxBuster) {
			shoot(1, specialBuster, false);
			return true;
		}
		if (bufferedShotPressed && stockedBuster) {
			shoot(1, currentWeapon ?? specialBuster, true);
			return true;
		}
		bool inputCheckH = false;
		bool inputCheckS = false;
		if (hasHadoukenEquipped()) {
			inputCheckH = player.input.checkHadoken(player, xDir, Control.Shoot);
		}
		if (hasShoryukenEquipped()) {
			inputCheckS = player.input.checkShoryuken(player, xDir, Control.Shoot);
		}
		if (inputCheckH && canUseFgMove() && grounded &&
			player.hadoukenAmmo >= player.fgMoveMaxAmmo && 
			hadoukenCooldownTime == 0
		) {
			if (!player.hasAllItems()) player.currency -= 3;
			player.hadoukenAmmo = 0;
			changeState(new Hadouken(), true);
			return true;
		}
		if (inputCheckS && canUseFgMove() && grounded &&
			player.shoryukenAmmo >= player.fgMoveMaxAmmo && 
			shoryukenCooldownTime == 0
		) {
			if (!player.hasAllItems()) player.currency -= 3;
			player.shoryukenAmmo = 0;
			changeState(new Shoryuken(isUnderwater()), true);
			return true;
		}
		if (currentWeapon != null && canShoot() && (
				player.input.isPressed(Control.Shoot, player) && !isCharging() ||
				currentWeapon.isStream && getChargeLevel() < 2 &&
				player.input.isHeld(Control.Shoot, player)
			)
		) {
			if (currentWeapon.shootCooldown <= 0) {
				shoot(0);
				return true;
			}
		}
		return base.attackCtrl();
	}

	// Shoots stuff.
	public void shoot(int chargeLevel) {
		shoot(chargeLevel, currentWeapon ?? specialBuster, false);
	}

	public void shoot(int chargeLevel, Weapon weapon, bool busterStock) {
		lastShootPressed = 100;
		// Check if can shoot.
		if (shootCooldown > 0 ||
			weapon == null ||
			!weapon.canShoot(chargeLevel, player) ||
			weapon.shootCooldown > 0
		) {
			return;
		}
		// Calls the weapon shoot function.
		bool useCrossShotAnim = false;
		if (chargeLevel >= 3 && armArmor == ArmorId.Giga || busterStock) {
			if (!busterStock) {
				stockedBuster = true;
			} else if (chargeLevel < 3) {
				stockedBuster = false;
			}
			if (!weapon.hasCustomChargeAnim && charState.normalCtrl && charState.attackCtrl) {
				useCrossShotAnim = true;
			} else {
				chargeLevel = 3;
			}
		}
		if (!busterStock && chargeLevel >= 3 && hasFullHyperMaxArmor) {
			stockedSaber = true;
		}
		// Changes to shoot animation and gets sound.
		if (chargeLevel < 3 || !weapon.hasCustomChargeAnim) {
			setShootAnim();
			shootAnimTime = DefaultShootAnimTime;
		}
		string shootSound = weapon.shootSounds[chargeLevel];
		// Shoot.
		if (useCrossShotAnim) {
			changeState(new X2ChargeShot(null, busterStock ? 1 : 0), true);
			stopCharge();
			return;
		} else {
			weapon.shoot(this, [chargeLevel, busterStock ? 1 : 0]);
		}
		// Sets up global shoot cooldown to the weapon shootCooldown.
		float baseCooldown = weapon.getFireRate(this, chargeLevel, [busterStock ? 1 : 0]);
		if (!stockedBuster || busterStock || weapon.fireRate <= 10) {
			weapon.shootCooldown = baseCooldown;
		} else {
			weapon.shootCooldown = 10;
		}
		shootCooldown = weapon.shootCooldown;
		// Add ammo.
		weapon.addAmmo(-weapon.getAmmoUsageEX(chargeLevel, this), player);
		// Play sound if any.
		if (shootSound != "") {
			playSound(shootSound, sendRpc: true);
		}
		// Stop charge if this was a charge shot.
		if (chargeLevel >= 1) {
			stopCharge();
		}
	}

	public void quickArmorUpgrade() {
		if (!player.input.isHeld(Control.Special2, player)) {
			hyperProgress = 0;
			return;
		}
		if (player.health <= 0 || hasUltimateArmor) {
			hyperProgress = 0;
			return;
		}
		if (fullArmor == ArmorId.Max && !hasFullHyperMaxArmor) {
			if (player.currency < Player.goldenArmorCost || hasAnyHyperArmor) {
				hyperProgress = 0;
				return;
			}
		}
		else if (player.currency < Player.ultimateArmorCost) {
			hyperProgress = 0;
			return;
		}
		if (charState is not WarpIn && fullArmor != ArmorId.None) {
			hyperProgress += Global.spf;
		}
		if (hyperProgress < 1) {
			return;
		}
		hyperProgress = 0;
		if (fullArmor == ArmorId.Max && !hasFullHyperMaxArmor) {
			player.currency -= Player.goldenArmorCost;
			hyperChestActive = true;
			hyperArmActive = true;
			hyperLegActive = true;
			hyperHelmetActive = true;
			Global.playSound("ching");
			return;
		}
		// Ultimate or Seraph armor.
		player.currency -= Player.ultimateArmorCost;
		hasUltimateArmor = true;
		Global.playSound("chingX4");
		return;
	}

	// Movement related stuff.
	public override float getRunSpeed() {
		if (charState is XHover) {
			return 2 * 60 * getRunDebuffs();;
		}
		return base.getRunSpeed();
	}

	public override float getDashSpeed() {
		if (flag != null || !isDashing) {
			return getRunSpeed();
		}
		float dashSpeed = 3.5f * 60;
		return dashSpeed * getRunDebuffs();
	}

	public override bool canAirDash() {
		return dashedInAir == 0 || (dashedInAir < 2 && hyperLegArmor == ArmorId.Max);
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
		string shootSprite = getSprite(charState.shootSpriteEx);
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

	public override Point? getNullableShootPos() {
		Point? busterOffsetPos = currentFrame.getBusterOffset();
		if (busterOffsetPos == null) {
			return null;
		}
		Point busterOffset = busterOffsetPos.Value;
		if (armArmor == ArmorId.Max && sprite.needsX3BusterCorrection()) {
			if (busterOffset.x > 0) { busterOffset.x += 4; }
			else if (busterOffset.x < 0) { busterOffset.x -= 4; }
		}
		busterOffset.x *= xDir;
		if (currentWeapon is RollingShield && charState is Dash) {
			busterOffset.y -= 2;
		}
		return pos.add(busterOffset);
	}
	
	public override bool canShoot() {
		if (isInvulnerableAttack() ||
			hasLastingProj() && !stockedBuster && !stockedMaxBuster && !stockedSaber ||
			hasLockingProj() ||
			shootCooldown > 0 ||
			invulnTime > 0 ||
			linkedTriadThunder != null
		) {
			return false;
		}
		return charState.attackCtrl;
	}

	public override bool canCharge() {
		return !isInvulnerableAttack() && !hasLockingProj();
	}

	public override void increaseCharge() {
		if (armArmor == ArmorId.Light) {
			chargeTime += speedMul * 1.5f;
			return;
		}
		chargeTime += speedMul;
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public override bool canChangeWeapons() {
		return base.canChangeWeapons();
	}

	public override void onWeaponChange(Weapon oldWeapon, Weapon newWeapon) {
		if (newWeapon is not ChameleonSting &&
			(newWeapon is not HyperCharge || weapons[hyperChargeTarget] is not ChameleonSting)
		) {
			stingActiveTime = 0;
		}
	}

	public override bool canAddAmmo() {
		if (weapons.Count == 0) { return false; }
		bool hasEmptyAmmo = false;
		foreach (Weapon weapon in weapons) {
			if (weapon.canHealAmmo && weapon.ammo < weapon.maxAmmo) {
				return true;
			}
		}
		return hasEmptyAmmo;
	}

	public override void addAmmo(float amount) {
		getRefillTargetWeapon()?.addAmmoHeal(amount);
	}

	public override void addPercentAmmo(float amount) {
		getRefillTargetWeapon()?.addAmmoPercentHeal(amount);
	}

	public Weapon? getRefillTargetWeapon() {
		if (currentWeapon != null && currentWeapon.canHealAmmo && currentWeapon.ammo < currentWeapon.maxAmmo) {
			return currentWeapon;
		}
		foreach (Weapon weapon in weapons) {
			if (weapon is GigaCrush or HyperNovaStrike or HyperCharge && weapon.ammo < weapon.maxAmmo) {
				return weapon;
			}
		}
		Weapon? targetWeapon = null;
		float targetAmmo = Int32.MaxValue;
		foreach (Weapon weapon in weapons) {
			if (!weapon.canHealAmmo) {
				continue;
			}
			if (weapon != currentWeapon &&
				weapon.ammo < weapon.maxAmmo &&
				weapon.ammo < targetAmmo
			) {
				targetWeapon = weapon;
				targetAmmo = targetWeapon.ammo;
			}
		}
		return targetWeapon;
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

	public override bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		bool damaged = base.canBeDamaged(damagerAlliance, damagerPlayerId, projId);

		// Bommerang can go thru invisibility check
		if (stingActiveTime > 0) {
			if (player.alliance != damagerAlliance && projId != null && Damager.isBoomerang(projId)) {
				return damaged;
			}
			return false;
		}
		return damaged;
	}

	public bool hasHadoukenEquipped() {
		return !Global.level.is1v1() && fullArmor == ArmorId.Light;
	}

	public bool hasShoryukenEquipped() {
		return !Global.level.is1v1() && fullArmor == ArmorId.Giga;
	}

	public bool hasFgMoveEquipped() {
		return hasHadoukenEquipped() || hasShoryukenEquipped();
	}

	public bool canAffordFgMove() {
		return player.currency >= 3 || player.hasAllItems();
	}

	public bool canUseFgMove() {
		return (
			!isInvulnerableAttack() && 
			chargedRollingShieldProj == null && 
			stingActiveTime == 0 && canAffordFgMove()
		);
	}

	public bool hasLastingProj() {
		return (
			chargedSpinningBlade?.destroyed == false ||
			chargedFrostShield?.destroyed == false ||
			chargedTornadoFang?.destroyed == false ||
			strikeChainProj?.destroyed == false
		);
	}

	public bool hasLockingProj() {
		return (
			chargedFrostShield?.destroyed == false ||
			chargedTornadoFang?.destroyed == false ||
			chargedSpinningBlade?.destroyed == false ||
			linkedTriadThunder?.destroyed == false
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
			// Light  Helmet.
			"mmx_jump" or "mmx_jump_shoot" or "mmx_wall_kick" or "mmx_wall_kick_shoot"
			when helmetArmor == ArmorId.Light && stingActiveTime == 0 => MeleeIds.LightHeadbutt,
			// Light Helmet when it up-dashes.
			"mmx_up_dash" or "mmx_up_dash_shoot"
			when helmetArmor == ArmorId.Light && stingActiveTime == 0 => MeleeIds.LightHeadbuttEX,
			// Nothing.
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		return id switch {
			(int)MeleeIds.SpeedBurnerCharged => new GenericMeleeProj(
				SpeedBurner.netWeapon, projPos, ProjIds.SpeedBurnerCharged, player,
				4, Global.defFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.LightHeadbutt => new GenericMeleeProj(
				LhHeadbutt.netWeapon, projPos, ProjIds.Headbutt, player,
				2, Global.halfFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.LightHeadbuttEX => new GenericMeleeProj(
				LhHeadbutt.netWeapon, projPos, ProjIds.Headbutt, player,
				4, Global.defFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.Shoryuken => new GenericMeleeProj(
				ShoryukenWeapon.netWeapon, projPos, ProjIds.Shoryuken, player,
				Damager.ohkoDamage, Global.defFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.MaxZSaber => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.XSaber, player,
				4, Global.defFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.ZSaber => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.X6Saber, player,
				3, 0, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.ZSaberAir => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.X6Saber, player,
				2, 0, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.NovaStrike => new GenericMeleeProj(
				HyperNovaStrike.netWeapon, projPos, ProjIds.NovaStrike, player,
				4, Global.defFlinch, 30, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public enum MeleeIds {
		None = -1,
		SpeedBurnerCharged,
		LightHeadbutt,
		LightHeadbuttEX,
		Shoryuken,
		MaxZSaber,
		ZSaber,
		ZSaberAir,
		NovaStrike,
	}

	// Other overrides.
	public override void onFlinchOrStun(CharState newState) {
		strikeChainProj?.destroySelf();
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
		return true;
	}

	public override void getHealthNameOffsets(out bool shieldDrawn, ref float healthPct) {
		shieldDrawn = false;
		if (rideArmor != null) {
			shieldDrawn = true;
			healthPct = rideArmor.health / rideArmor.maxHealth;
		}
		else if (chargedRollingShieldProj != null && currentWeapon != null) {
			shieldDrawn = true;
			healthPct = currentWeapon.ammo / currentWeapon.maxAmmo;
		}
	}

	public override void destroySelf(
		string spriteName = "", string fadeSound = "",
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {
		removeLastingProjs();
		chargedRollingShieldProj?.destroySelfNoEffect();
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
		if (sprite.name == "mmx_frozen") {
			Global.sprites["frozen_block"].draw(
				0, pos.x + x - (xDir * 2), pos.y + y + 1, xDir, 1, null, 1, 1, 1, zIndex + 1
			);
		}
		if (getChargeShaders().Count != 0) {
			chargePalleteTime += Global.gameSpeed;
		} else {
			chargePalleteTime = 0;
		}
		float backupAlpha = alpha;
		if (stingActiveTime > 0) {
			if (stingPaletteTime > 6) {
				stingPaletteTime = 0;
				stingPaletteIndex++;
				if (stingPaletteIndex >= 9) {
					stingPaletteIndex = 0;
				}
			} else {
				stingPaletteTime++;
			}
			if (stingPaletteTime % 4 <= 1) {
				alpha *= 0.3f;
			}
		}
		base.render(x, y);
		alpha = backupAlpha;
	}

	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			int chargeType = 0;
			if (hasFullHyperMaxArmor) {
				chargeType = 3;
			}
			else if (armArmor == ArmorId.Max) {
				chargeType = 1;
			}
			chargeEffect.update(getChargeLevel(), chargeType);
		}
	}

	public override string getAltSound(string sound, string options = "") {
		int gameSound = options.ToLower() switch {
			"larmor" => (int)legArmor,
			"aarmor" => (int)armArmor,
			"carmor" => (int)chestArmor,
			"harmor" => (int)helmetArmor,
			_ => 0
		};
		string apendix = gameSound switch {
			1 => "x1",
			2 => "x2",
			3 => "x3",
			_ => ""
		};
		if (apendix != "" && Global.soundBuffers.ContainsKey(sound.ToLower()+apendix)) {
			return sound+apendix;
		}
		return sound;
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;
		int index = currentWeapon?.index ?? 0;

		if (stingActiveTime > 0 && stingPaletteIndex != 0){
			palette = player.xStingPaletteShader;
			palette.SetUniform("palette", stingPaletteIndex);

			shaders.Add(palette);
			shaders.AddRange(baseShaders);
			return shaders;
		}
		if (index >= (int)WeaponIds.GigaCrush) {
			index = 0;
		}
		if (index == (int)WeaponIds.HyperCharge && ownedByLocalPlayer) {
			index = player.weapons[player.hyperChargeSlot].index;
		}
		if (hasFullHyperMaxArmor) {
			index = 25;
		}
		if (hasUltimateArmor && index == 0) {
			index = 30;
		}
		palette = player.xPaletteShader;

		palette?.SetUniform("palette", index);

		List<ShaderWrapper?> chargePalletes = getChargeShaders();
		if (chargePalletes.Count > 0) {
			if (chargePalletes.Count == 1) {
				chargePalletes.Add(null);
			}
			ShaderWrapper? targetChargePallete = chargePalletes[MathInt.Floor(
				(chargePalleteTime % (chargePalletes.Count * 2)) / 2f
			)];
			if (targetChargePallete != null) {
				palette = targetChargePallete;
			}
		}

		if (charState is SpeedBurnerCharState) {
			palette = player.speedBurnerOrange;
			if (Global.isOnFrameCycle(8)) {
				palette = player.speedBurnerGrey;
			}
		}
		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}
		shaders.AddRange(baseShaders);
		return shaders;
	}

	public List<ShaderWrapper> getChargeShaders() {
		List<ShaderWrapper> chargePalletes = new();
		ShaderWrapper? defaultChargePallete = null;
		int chargeLevel = getChargeLevel();
		if (chargeLevel > 0) {
			defaultChargePallete = getChargeLevel() switch {
				1 => Player.XBlueC,
				2 => Player.XYellowC,
				3 when hasFullHyperMaxArmor => Player.XGreenC,
				3 when armArmor == ArmorId.Max => Player.XOrangeC,
				_ => Player.XPinkC,
			};
			chargePalletes.Add(defaultChargePallete);
		}
		if (stockedMaxBuster && defaultChargePallete != Player.XOrangeC) {
			chargePalletes.Add(Player.XOrangeC);
		}
		if (stockedBuster && defaultChargePallete != Player.XPinkC) {
			chargePalletes.Add(Player.XPinkC);
		}
		if (stockedSaber && defaultChargePallete != Player.XGreenC) {
			chargePalletes.Add(Player.XGreenC);
		}
		if (currentWeapon is HyperCharge) {
			if (!hasFullHyperMaxArmor && !stockedMaxBuster && defaultChargePallete != Player.XOrangeC) {
				chargePalletes.Add(Player.XOrangeC);
			}
			else if (!stockedSaber && defaultChargePallete != Player.XGreenC) {
				chargePalletes.Add(Player.XGreenC);
			}
		}
		return chargePalletes;
	}

	public int getArmorByte() {
		int armorByte = (byte)chestArmor;
		armorByte += (byte)armArmor << 4;
		armorByte += (byte)legArmor << 8;
		armorByte += (byte)helmetArmor << 12;

		return armorByte;
	}

	public void setArmorByte(int armorByte) {
		int[] values = new int[4];
		for (int i = values.Length - 1; i >= 0; i--) {
			int offF = (i + 1) * 4;
			int offB = i * 4;
			values[i] = ((armorByte >> offF << offF) ^ armorByte) >> offB;
		}
		chestArmor = (ArmorId)values[0];
		armArmor = (ArmorId)values[1];
		legArmor = (ArmorId)values[2];
		helmetArmor = (ArmorId)values[3];
	}

	public static int[] getArmorVals(int armorByte) {
		int[] values = new int[4];
		for (int i = values.Length - 1; i >= 0; i--) {
			int offF = (i + 1) * 4;
			int offB = i * 4;
			values[i] = ((armorByte >> offF << offF) ^ armorByte) >> offB;
		}
		return [
			values[0],
			values[1],
			values[2],
			values[3]
		];
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();

		int weaponIndex = currentWeapon?.index ?? 255;
		byte ammo = (byte)MathF.Ceiling(currentWeapon?.ammo ?? 0);
		if (weaponIndex == (int)WeaponIds.HyperCharge) {
			weaponIndex = weapons[player.hyperChargeSlot].index;
			ammo = (byte)MathF.Ceiling(weapons[player.hyperChargeSlot].ammo);
		}
		customData.Add((byte)weaponIndex);
		customData.Add(ammo);
		customData.AddRange(BitConverter.GetBytes(getArmorByte()));

		// Stocked charge flags.
		customData.Add(Helpers.boolArrayToByte([
			stingActiveTime > 0,
			stockedBuster,
			stockedMaxBuster,
			stockedSaber,
			hyperChargeActive,
		]));

		// Hyper Armor Flags.
		customData.Add(Helpers.boolArrayToByte([
			hyperChestActive,
			hyperArmActive,
			hyperLegActive,
			hyperHelmetActive,
			hasUltimateArmor
		]));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-player data.
		Weapon? targetWeapon = weapons.Find(w => w.index == data[0]);
		if (targetWeapon != null) {
			weaponSlot = weapons.IndexOf(targetWeapon);
			targetWeapon.ammo = data[1];
		}
		setArmorByte(BitConverter.ToUInt16(data[2..4]));

		// Stocked charge and weapon flags.
		bool[] boolData = Helpers.byteToBoolArray(data[4]);
		stingActiveTime = boolData[0] ? 20 : 0;
		stockedBuster = boolData[1];
		stockedMaxBuster = boolData[2];
		stockedSaber = boolData[3];
		hyperChargeActive = boolData[4];

		// Hyper Armor Flags.
		bool[] armorBoolData = Helpers.byteToBoolArray(data[5]);
		hyperChestActive = armorBoolData[0];
		hyperArmActive = armorBoolData[1];
		hyperLegActive = armorBoolData[2];
		hyperHelmetActive = armorBoolData[3];
		hasUltimateArmor = armorBoolData[4];
	}

	public override void aiAttack(Actor? target) {
		bool isTargetInAir = pos.y < target?.pos.y - 20;
		bool isTargetClose = pos.x < target?.pos.x - 10;
		bool isFacingTarget = (pos.x < target?.pos.x && xDir == 1) || (pos.x >= target?.pos.x && xDir == -1);
		int Xattack = Helpers.randomRange(0, 7);
		if (charState.normalCtrl) {
			player.press(Control.Shoot);
		} 
		if (canShoot() && canChangeWeapons() && player != null &&
		 charState is not LadderClimb && player.weapon != null && aiAttackCooldown <= 0) {
			switch (Xattack) {
				case 0 when getMaxChargeLevel() >= 3 && isFacingTarget:
					player.release(Control.Shoot);		
					break;
				case 1 when target is MegamanX or Axl or Vile or NeoSigma && hasHadoukenEquipped()
					&& canUseFgMove() && isTargetClose: 
					player.currency -= 3;
					changeState(new Hadouken(), true);
					break;
				case 2 when isTargetInAir && isTargetClose && hasShoryukenEquipped() && canUseFgMove():
					player.currency -= 3;
					changeState(new Shoryuken(isUnderwater()), true);
					break;
				case 3 when !hasAnyArmor && specialSaberCooldown <= 0:
					specialSaberCooldown = 60;
					changeState(new X6SaberState(grounded), true);
					break;
				case 4 when hasUltimateArmor:
					int novaStrikeSlot = player.weapons.FindIndex(w => w is HyperNovaStrike);
					player.changeWeaponSlot(novaStrikeSlot);
					if (player.weapon.ammo >= 16) {
						player.press(Control.Shoot);
					} else { player.changeWeaponSlot(getRandomWeaponIndex()); }
					break;
				case 5 when player.hasBodyArmor(2):
					int gCrushSlot = player.weapons.FindIndex(w => w is GigaCrush);
					player.changeWeaponSlot(gCrushSlot);
					if (player.weapon.ammo >= 28)
						player.press(Control.Shoot);
					else {
						player.changeWeaponSlot(getRandomWeaponIndex());
					}
					break;
				case 6 when armArmor == ArmorId.Max:
					int hyperbuster = player.weapons.FindIndex(w => w is HyperCharge);
					player.changeWeaponSlot(hyperbuster);
					if (player.weapon.ammo >= 16) {
						player.press(Control.Shoot);
						player.release(Control.Shoot);
					} else { player.changeWeaponSlot(getRandomWeaponIndex()); }
					break;
				case 7 when stockedBuster || stockedMaxBuster || stockedSaber:
					player.press(Control.Shoot);
					player.release(Control.Shoot);
					break;
				default:
					player.release(Control.Shoot);
					break;
			}
			aiAttackCooldown = 10;
		}
		base.aiAttack(target);
	}
	public override void aiDodge(Actor? target) {
		int RollingShield = player.weapons.FindIndex(w => w is RollingShield);
		int novaStrikeSlot = player.weapons.FindIndex(w => w is HyperNovaStrike);
		foreach (GameObject gameObject in getCloseActors(64, true, false, false)) {
			if (player != null && player.weapon != null &&
			aiAttackCooldown <= 0 && gameObject is Projectile proj && 
			proj.damager.owner.alliance != player.alliance) {
				if (player.weapon.ammo > 0) {
					player.changeWeaponSlot(RollingShield);
					player.press(Control.Shoot);
					player.release(Control.Shoot);
				} else if (hasUltimateArmor) {
					player.changeWeaponSlot(novaStrikeSlot);
					if (player.weapon.ammo >= 16) {
						player.press(Control.Shoot);
					} else { player.changeWeaponSlot(getRandomWeaponIndex()); }
				}
			}
		}
		base.aiDodge(target);
	}

	public override void aiUpdate() {
		//Buying UAX and Golden Armor goes here
		base.aiUpdate();
	}
}
