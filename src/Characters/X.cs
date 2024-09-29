using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public partial class MegamanX : Character {
	public float shotgunIceChargeTime = 0;
	public float shotgunIceChargeCooldown = 0;
	public int shotgunIceChargeMod = 0;
	public bool fgMotion;
	public bool isHyperX;
	public bool hasUltimateArmor;

	public const int headArmorCost = 2;
	public const int bodyArmorCost = 3;
	public const int armArmorCost = 3;
	public const int bootsArmorCost = 2;

	public RollingShieldProjCharged? chargedRollingShieldProj;
	public List<BubbleSplashProjCharged> chargedBubbles = new List<BubbleSplashProjCharged>();
	public StrikeChainProj? strikeChainProj;
	public GravityWellProjCharged? chargedGravityWell;
	public SpinningBladeProjCharged? chargedSpinningBlade;
	public FrostShieldProjCharged? chargedFrostShield;
	public TunnelFangProjCharged? chargedTunnelFang;
	public GravityWellProj? gravityWell;
	public int totalChipHealAmount;
	public const int maxTotalChipHealAmount = 32;
	public int unpoShotCount {
		get {
			if (player.weapon is not Buster { isUnpoBuster: true }) {
				return 0;
			}
			return MathInt.Floor(player.weapon.ammo / player.weapon.getAmmoUsage(0));
		}
	}

	public float hadoukenCooldownTime;
	public float maxHadoukenCooldownTime = 1f;
	public float shoryukenCooldownTime;
	public float maxShoryukenCooldownTime = 1f;
	//public ShaderWrapper xPaletteShader;

	public float streamCooldown;
	public float noDamageTime;
	public float rechargeHealthTime;
	public float scannerCooldown;
	float UPDamageCooldown;
	public float unpoDamageMaxCooldown = 2;
	float unpoTime;

	public int cStingPaletteIndex;
	public float cStingPaletteTime;
	bool lastFrameSpecialHeld;
	bool lastShotWasSpecialBuster;
	public float upPunchCooldown;
	public Projectile? unpoAbsorbedProj;

	float hyperChargeAnimTime;
	float hyperChargeAnimTime2 = 0.125f;
	const float maxHyperChargeAnimTime = 0.25f;

	public bool boughtUltimateArmorOnce;
	public bool boughtGoldenArmorOnce;

	public bool stockedCharge;
	public bool stockedXSaber;

	public bool stockedX3Buster;

	public float xSaberCooldown;
	public float stockedChargeFlashTime;

	public BeeSwarm? beeSwarm;

	public float parryCooldown;
	public float maxParryCooldown = 0.5f;

	public bool stingActive;
	public bool isHyperChargeActive;

	public Sprite hyperChargePartSprite =  new Sprite("hypercharge_part_1");
	public Sprite hyperChargePart2Sprite =  new Sprite("hypercharge_part_1");

	public bool isShootingSpecialBuster;

	public Buster staticBusterWeapon = new();
	public Buster specialBuster => (player.weapons.OfType<Buster>().FirstOrDefault() ?? staticBusterWeapon);

	public MegamanX(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.X;
	}

	public bool canShootSpecialBuster() {
		if (isHyperX && (charState is Dash || charState is AirDash)) {
			return false;
		}
		return isSpecialBuster() &&
			!Buster.isNormalBuster(player.weapon) &&
			!stingActive &&
			player.armorFlag == 0 &&
			streamCooldown == 0;
	}

	public bool canShootSpecialBusterOnBuster() {
		return isSpecialBuster() && !stingActive && player.armorFlag == 0;
	}

	public void refillUnpoBuster() {
		if (player.weapons.Count > 0) player.weapons[0].ammo = player.weapons[0].maxAmmo;
	}

	public override void update() {
		fgMotion = false;
		base.update();

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

		Helpers.decrementTime(ref barrierCooldown);

		if (stingActive) {
			addRenderEffect(RenderEffectType.Invisible);
		} else {
			removeRenderEffect(RenderEffectType.Invisible);
		}

		if (cStingPaletteTime > 5) {
			cStingPaletteTime = 0;
			cStingPaletteIndex++;
		}
		cStingPaletteTime++;

		if (headbuttAirTime > 0) {
			headbuttAirTime += Global.spf;
		}

		if (isHyperX) {
			if (musicSource == null) {
				addMusicSource("introStageBreisX4_JX", getCenterPos(), true);
			}
		} else {
			destroyMusicSource();
		}

		if (!ownedByLocalPlayer) {
			Helpers.decrementTime(ref barrierTime);
			return;
		}
		Helpers.decrementTime(ref parryCooldown);
		isHyperChargeActive = shouldShowHyperBusterCharge();

		if (beeSwarm != null) {
			beeSwarm.update();
		}

		updateBarrier();

		if (hasFgMoveEquipped()) {
			player.fgMoveAmmo += Global.spf;
			if (player.fgMoveAmmo > 32) player.fgMoveAmmo = 32;
		}

		if (stingChargeTime > 0) {
			hadoukenCooldownTime = maxHadoukenCooldownTime;
			shoryukenCooldownTime = maxShoryukenCooldownTime;
		}

		Helpers.decrementTime(ref xSaberCooldown);
		Helpers.decrementTime(ref scannerCooldown);
		Helpers.decrementTime(ref hadoukenCooldownTime);
		Helpers.decrementTime(ref shoryukenCooldownTime);
		Helpers.decrementTime(ref streamCooldown);

		if (player.weapon.ammo >= player.weapon.maxAmmo) {
			weaponHealAmount = 0;
		}

		if (weaponHealAmount > 0 && player.health > 0) {
			weaponHealTime += Global.spf;
			if (weaponHealTime > 0.05) {
				weaponHealTime = 0;
				weaponHealAmount--;
				player.weapon.ammo = Helpers.clampMax(player.weapon.ammo + 1, player.weapon.maxAmmo);
				if (!player.hasArmArmor(3)) {
					playSound("heal", forcePlay: true);
				} else {
					playSound("healX3", forcePlay: true);
				}
			}
		}

		if (shootAnimTime > 0 && strikeChainProj == null && !charState.isGrabbing) {
			shootAnimTime -= Global.spf;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				changeSpriteFromName(charState.sprite, false);
				if (charState is WallSlide) {
					frameIndex = sprite.totalFrameNum - 1;
				}
			}
		}

		if (player.hasChip(2) && !isInvisible() && totalChipHealAmount < maxTotalChipHealAmount) {
			noDamageTime += Global.spf;
			if ((player.health < player.maxHealth || player.hasSubtankCapacity()) && noDamageTime > 4) {
				rechargeHealthTime -= Global.spf;
				if (rechargeHealthTime <= 0) {
					rechargeHealthTime = 1;
					addHealth(1);
					totalChipHealAmount++;
				}
			}
		}

		Point inputDir = player.input.getInputDir(player);

		if (!isHyperX && canShoot() &&
			charState is not Die &&
			charState is not Hurt &&
			charState.canShoot() == true
		) {
			if (Global.level.is1v1() && player.weapons.Count == 10) {
				if (player.weaponSlot != 9) {
					player.weapons[9].update();
				}

				if (player.input.isPressed(Control.Special1, player) && chargeTime == 0) {
					int oldSlot = player.weaponSlot;
					player.changeWeaponSlot(9);
					if (shootTime <= 0) {
						shoot(false);
					}
					player.weaponSlot = oldSlot;
					player.changeWeaponSlot(oldSlot);
				}
			}

			if (Options.main.gigaCrushSpecial &&
				player.input.isPressed(Control.Special1, player) &&
				player.input.isHeld(Control.Down, player) &&
				player.weapons.Any(w => w is GigaCrush)
			) {
				int oldSlot = player.weaponSlot;
				int gCrushSlot = player.weapons.FindIndex(w => w is GigaCrush);
				player.changeWeaponSlot(gCrushSlot);
				shoot(false);
				player.weaponSlot = oldSlot;
				player.changeWeaponSlot(oldSlot);
			} else if (
				  Options.main.novaStrikeSpecial &&
				  player.input.isPressed(Control.Special1, player) &&
				  player.weapons.Any(w => w is NovaStrike) &&
				  !inputDir.isZero()
			  ) {
				int oldSlot = player.weaponSlot;
				int novaStrikeSlot = player.weapons.FindIndex(w => w is NovaStrike);
				player.changeWeaponSlot(novaStrikeSlot);
				if (player.weapon.shootTime <= 0) {
					shoot(false);
				}
				player.weaponSlot = oldSlot;
				player.changeWeaponSlot(oldSlot);
			}
		}
		// Fast Hyper Activation.
		quickArmorUpgrade();

		if (charState is not Die &&
			player.input.isPressed(Control.Special1, player) &&
			player.hasAllX3Armor() && !player.hasGoldenArmor()) {
			if (player.input.isHeld(Control.Down, player)) {
				player.setChipNum(0, false);
				Global.level.gameMode.setHUDErrorMessage(
					player, "Equipped foot chip.", playSound: false, resetCooldown: true
				);
			} else if (player.input.isHeld(Control.Up, player)) {
				player.setChipNum(2, false);
				Global.level.gameMode.setHUDErrorMessage(
					player, "Equipped head chip.", playSound: false, resetCooldown: true
				);
			} else if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
				player.setChipNum(3, false);
				Global.level.gameMode.setHUDErrorMessage(
					player, "Equipped arm chip.", playSound: false, resetCooldown: true
				);
			} else {
				player.setChipNum(1, false);
				Global.level.gameMode.setHUDErrorMessage(
					player, "Equipped body chip.", playSound: false, resetCooldown: true
				);
			}
		}

		Helpers.decrementTime(ref upPunchCooldown);

		if (isHyperX && !isInvulnerableAttack()) {
			if (charState.attackCtrl && player.input.isPressed(Control.Shoot, player)) {
				if (unpoShotCount <= 0) {
					upPunchCooldown = 0.5f;
					changeState(new XUPPunchState(grounded), true);
					return;
				}
			} else if (player.input.isPressed(Control.Special1, player) && !isInvisible() &&
				  (charState is Dash || charState is AirDash)) {
				charState.isGrabbing = true;
				changeSpriteFromName("unpo_grab_dash", true);
			} else if
			  (
				  player.input.isWeaponLeftOrRightPressed(player) && parryCooldown == 0 &&
				  (charState is Idle || charState is Run || charState is Fall || charState is Jump || charState is XUPPunchState || charState is XUPGrabState)
			  ) {
				if (unpoAbsorbedProj != null) {
					changeState(new XUPParryProjState(unpoAbsorbedProj, true, false), true);
					unpoAbsorbedProj = null;
					return;
				} else {
					changeState(new XUPParryStartState(), true);
				}
			}
		}

		if (charState.attackCtrl &&
			(isSpecialSaber() || isHyperX) && canShoot() &&
			canChangeWeapons() && player.armorFlag == 0 &&
			player.input.isPressed(Control.Special1, player) &&
			!isAttacking() && !isInvisible() &&
			!charState.isGrabbing
		) {
			if (xSaberCooldown == 0) {
				xSaberCooldown = 1f;
				changeState(new X6SaberState(grounded), true);
				return;
			}
		}

		if (isHyperX) {
			//if (unpoTime > 12) unpoDamageMaxCooldown = 1;
			//if (unpoTime > 24) unpoDamageMaxCooldown = 0.75f;
			//if (unpoTime > 36) unpoDamageMaxCooldown = 0.5f;
			//if (unpoTime > 48) unpoDamageMaxCooldown = 0.25f;

			if (charState is not XUPGrabState
				and not XUPParryMeleeState
				and not XUPParryProjState
				and not Hurt
				and not GenericStun
				and not VileMK2Grabbed
				and not GenericGrabbedState
			) {
				unpoTime += Global.spf;
				UPDamageCooldown += Global.spf;
				if (UPDamageCooldown > unpoDamageMaxCooldown) {
					UPDamageCooldown = 0;
					applyDamage(1, player, this, null, null);
				}
			}
		}

		if (charState.attackCtrl && player.hasHelmetArmor(2) && scannerCooldown == 0 && canScan()) {
			Point scanPos;
			Point? headPos = getHeadPos();
			if (headPos != null) {
				scanPos = headPos.Value.addxy(xDir * 10, 0);
			} else {
				scanPos = getCenterPos().addxy(0, -10);
			}
			CollideData hit = Global.level.raycast(
				scanPos, scanPos.addxy(getShootXDir() * 150, 0), new List<Type>() { typeof(Actor) }
			);
			if (hit?.gameObject is Character chr &&
				chr.player.alliance != player.alliance &&
				!chr.player.scanned &&
				!chr.isStealthy(player.alliance)
			) {
				new ItemTracer().getProjectile(scanPos, getShootXDir(), player, 0, player.getNextActorNetId());
			} else if (player.input.isPressed(Control.Special1, player)) {
				new ItemTracer().getProjectile(scanPos, getShootXDir(), player, 0, player.getNextActorNetId());
			}
		}

		staticBusterWeapon.update();

		var oldWeapon = player.weapon;
		if (canShootSpecialBuster()) {
			if (player.input.isHeld(Control.Special1, player)) {
				if (!lastFrameSpecialHeld) {
					lastFrameSpecialHeld = true;
					if (isCharging()) {
						stopCharge();
					}
				}
			} else {
				if (lastFrameSpecialHeld) {
					isShootingSpecialBuster = true;
				}
				lastFrameSpecialHeld = false;
			}
		} else {
			lastFrameSpecialHeld = false;
			lastShotWasSpecialBuster = false;
			isShootingSpecialBuster = false;
		}

		if (charState.canShoot()) {
			bool specialBusterOnBuster = (
				Buster.isNormalBuster(player.weapon) &&
				player.input.isPressed(Control.Special1, player) &&
				canShootSpecialBusterOnBuster()
			);
			var shootPressed = player.input.isPressed(Control.Shoot, player) || specialBusterOnBuster;
			if (shootPressed) {
				lastShotWasSpecialBuster = false;
				if (lastFrameSpecialHeld) {
					lastFrameSpecialHeld = false;
					stopCharge();
				}
			} else {
				if (canShootSpecialBuster()) {
					shootPressed = player.input.isPressed(Control.Special1, player);
					if (shootPressed) {
						lastShotWasSpecialBuster = true;
					}
				}
			}

			if (shootPressed) {
				lastShootPressed = Global.frameCount;
			}

			int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
			int framesSinceLastShootReleased = Global.frameCount - lastShootReleased;
			var shootHeld = player.input.isHeld(Control.Shoot, player);

			if (lastShotWasSpecialBuster) isShootingSpecialBuster = true;

			bool offCooldown = oldWeapon.shootTime == 0 && shootTime == 0;
			if (isShootingSpecialBuster) {
				offCooldown = oldWeapon.shootTime < oldWeapon.rateOfFire * 0.5f && shootTime == 0;
			}

			bool shootCondition = (
				shootPressed ||
				(framesSinceLastShootPressed < Global.normalizeFrames(6) &&
				framesSinceLastShootReleased > Global.normalizeFrames(30)) ||
				(shootHeld && player.weapon.isStream && chargeTime < charge2Time)
			);
			if (!fgMotion && offCooldown && shootCondition) {
				shoot(false);
			}

			if (!isHyperX) {
				chargeControls();
			} else {
				unpoChargeControls();
			}
		} else if (
			charState is Dash || charState is AirDash ||
			charState is XUPParryMeleeState || charState is XUPParryProjState ||
			charState is XUPParryStartState || charState is XUPGrabState
		) {
			if (isHyperX) {
				unpoChargeControls();
			}
		}

		isShootingSpecialBuster = false;

		if (chargedSpinningBlade != null || chargedFrostShield != null || chargedTunnelFang != null) {
			changeSprite("mmx_" + charState.shootSprite, true);
		}

		if (!isHyperX) {
			player.changeWeaponControls();
		}

		chargeGfx();

		if (charState is Hurt || charState is Die) {
			shotgunIceChargeTime = 0;
		}

		if (string.IsNullOrEmpty(charState.shootSprite)) {
			shotgunIceChargeTime = 0;
		}

		if (shotgunIceChargeTime > 0 && ownedByLocalPlayer) {
			changeSprite("mmx_" + charState.shootSprite, true);
			shotgunIceChargeTime -= Global.spf;
			var busterPos = getShootPos().addxy(xDir * 10, 0);
			if (shotgunIceChargeCooldown == 0) {
				new ShotgunIceProjCharged(
					player.weapon, busterPos, xDir,
					player, shotgunIceChargeMod % 2, false,
					player.getNextActorNetId(), rpc: true
				);
				shotgunIceChargeMod++;
			}
			shotgunIceChargeCooldown += Global.spf;
			if (shotgunIceChargeCooldown > 0.1) {
				shotgunIceChargeCooldown = 0;
			}
			if (shotgunIceChargeTime < 0) {
				shotgunIceChargeTime = 0;
				shotgunIceChargeCooldown = 0;
				changeSprite("mmx_" + charState.defaultSprite, true);
			}
		}

		if (isShootingRaySplasher && ownedByLocalPlayer) {
			changeSprite("mmx_" + charState.shootSprite, true);

			if (raySplasherCooldown > 0) {
				raySplasherCooldown += Global.spf;
				if (raySplasherCooldown >= 0.03f) {
					raySplasherCooldown = 0;
				}
			} else {
				var busterPos = getShootPos();
				if (raySplasherCooldown2 == 0) {
					player.weapon.addAmmo(-0.15f, player);
					raySplasherCooldown2 = 0.03f;
					new RaySplasherProj(
						player.weapon, busterPos,
						getShootXDir(), raySplasherMod % 3, (raySplasherMod / 3) % 3,
						false, player, player.getNextActorNetId(), rpc: true
					);
					raySplasherMod++;
					if (raySplasherMod % 3 == 0) {
						if (raySplasherMod >= 21) {
							setShootRaySplasher(false);
							changeSprite("mmx_" + charState.defaultSprite, true);
						} else {
							raySplasherCooldown = Global.spf;
						}
					}
				} else {
					raySplasherCooldown2 -= Global.spf;
					if (raySplasherCooldown2 <= 0) {
						raySplasherCooldown2 = 0;
					}
				}
			}
		}
	}

	public override bool normalCtrl() {
		if (!grounded) {
			if (player.dashPressed(out string dashControl) && canAirDash() && canDash() && flag == null) {
				CharState dashState;
				if (player.input.isHeld(Control.Up, player) && player.hasBootsArmor(3)) {
					dashState = new UpDash(Control.Dash);
				} else {
					dashState = new AirDash(dashControl);
				}
				if (!isDashing) {
					changeState(dashState);
					return true;
				} else if (player.hasChip(0)) {
					changeState(dashState);
					return true;
				}
			}
			if (player.input.isPressed(Control.Jump, player) &&
				canJump() && isUnderwater() &&
				chargedBubbles.Count > 0 && flag == null
			) {
				vel.y = -getJumpPower();
				changeState(new Jump());
				return true;
			}
			if (!player.isAI && player.hasUltimateArmor() &&
				player.input.isPressed(Control.Jump, player) &&
				canJump() && !isDashing && canAirDash() && flag == null
			) {
				dashedInAir++;
				changeState(new XHover(), true);
			}
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		if (!grounded) {
			return false;
		}
		bool hadokenCheck = false;
		bool shoryukenCheck = false;
		if (hasHadoukenEquipped()) {
			hadokenCheck = player.input.checkHadoken(player, xDir, Control.Shoot);
		}
		if (hasShoryukenEquipped()) {
			shoryukenCheck = player.input.checkShoryuken(player, xDir, Control.Shoot);
		}
		if (player.isX && hadokenCheck && canUseFgMove()) {
			if (!player.hasAllItems()) player.currency -= 3;
			player.fgMoveAmmo = 0;
			changeState(new Hadouken(), true);
			return true;
		}
		if (player.isX && shoryukenCheck && canUseFgMove()) {
			if (!player.hasAllItems()) player.currency -= 3;
			player.fgMoveAmmo = 0;
			changeState(new Shoryuken(isUnderwater()), true);
			return true;
		}
		return false;
	}

	public void chargeControls() {
		if (chargeButtonHeld() && canCharge()) {
			increaseCharge();

			if (player.weapon is ParasiticBomb && getChargeLevel() == 3) {
				shoot(true);
			}
		} else {
			if (isCharging()) {
				if (player.weapon is AssassinBullet) {
					shootAssassinShot();
				} else {
					if (shootTime == 0) {
						shoot(true);
					}
					stopCharge();
					lastShootReleased = Global.frameCount;
				}
			} else if (!(charState is Hurt)) {
				stopCharge();
			}
		}
	}

	public void stockCharge(bool stockOrUnstock) {
		stockedCharge = stockOrUnstock;
		if (ownedByLocalPlayer) {
			RPC.playerToggle.sendRpc(
				player.id, stockOrUnstock ? RPCToggleType.StockCharge : RPCToggleType.UnstockCharge
			);
		}
	}

	public void stockSaber(bool stockOrUnstock) {
		stockedXSaber = stockOrUnstock;
		if (ownedByLocalPlayer) {
			RPC.playerToggle.sendRpc(
				player.id, stockOrUnstock ? RPCToggleType.StockSaber : RPCToggleType.UnstockSaber
			);
		}
	}

	public void unpoChargeControls() {
		if (chargeButtonHeld() && canCharge()) {
			//increaseCharge();
			player.weapon.addAmmo(player.weapon.getAmmoUsage(0) * 0.625f * Global.spf, player);
			chargeTime = unpoShotCount switch {
				0 => charge1Time,
				1 => charge2Time,
				3 => charge3Time,
				_ => charge3Time
			};
			if (sprite.name.EndsWith("shoot")) {
				chargeTime = 0;
			}
		} else {
			if (isCharging()) {
				stopCharge();
			} else if (!(charState is Hurt)) {
				stopCharge();
			}
		}
	}

	public void shoot(bool doCharge) {
		int chargeLevel = getChargeLevel();
		if (!doCharge && chargeLevel >= 3) return;

		if (isHyperX && unpoShotCount <= 0) return;

		if (!player.weapon.canShoot(chargeLevel, player)) {
			return;
		}
		if (player.weapon is AssassinBullet || player.weapon is UndisguiseWeapon) {
			return;
		}
		if (player.weapon is HyperBuster hb) {
			var hyperChargeWeapon = player.weapons[player.hyperChargeSlot];
			shootTime = hb.getRateOfFire(player);
			if (hyperChargeWeapon is Sting || hyperChargeWeapon is RollingShield || hyperChargeWeapon is BubbleSplash || hyperChargeWeapon is ParasiticBomb) {
				doCharge = true;
				chargeLevel = 3;
				hb.shootTime = hb.getRateOfFire(player);
				hb.addAmmo(-hb.getAmmoUsage(3), player);
				player.changeWeaponSlot(player.hyperChargeSlot);
				if (hyperChargeWeapon is BubbleSplash bs) {
					bs.hyperChargeDelay = 0.25f;
				}
			}
		} else {
			shootTime = player.weapon.rateOfFire;
		}

		if (chargeLevel == 2 || chargeLevel >= 3) {
			var hbWep = player.weapons.FirstOrDefault(w => w is HyperBuster) as HyperBuster;
			if (hbWep != null) {
				hbWep.shootTime = hbWep.getRateOfFire(player);
			}
		}

		if (stockedXSaber) {
			if (xSaberCooldown == 0) {
				stockSaber(false);
				changeState(new XSaberState(grounded), true);
			}
			return;
		}

		bool hasShootSprite = !string.IsNullOrEmpty(charState.shootSprite);
		if (shootAnimTime == 0) {
			if (hasShootSprite) changeSprite(getSprite(charState.shootSprite), false);
		} else if (charState is Idle) {
			frameIndex = 0;
			frameTime = 0;
		}
		if (charState is LadderClimb) {
			if (player.input.isHeld(Control.Left, player)) {
				this.xDir = -1;
			} else if (player.input.isHeld(Control.Right, player)) {
				this.xDir = 1;
			}
		}
		if (charState is XUPGrabState) {
			changeToIdleOrFall();
		}

		//Sometimes transitions cause the shoot sprite not to be played immediately, so force it here
		if (currentFrame.getBusterOffset() == null) {
			if (hasShootSprite) changeSprite(getSprite(charState.shootSprite), false);
		}

		if (hasShootSprite) shootAnimTime = 0.3f;
		int xDir = getShootXDir();

		int cl = doCharge ? chargeLevel : 0;
		if (player.weapon is GigaCrush) {
			if (player.weapon.ammo < 16) cl = 0;
			else if (player.weapon.ammo >= 16 && player.weapon.ammo < 24) cl = 1;
			else if (player.weapon.ammo >= 24 && player.weapon.ammo < 32) cl = 2;
			else cl = 3;
		}
		if (Buster.isWeaponUnpoBuster(player.weapon)) {
			cl = 2;
		}

		shootRpc(getShootPos(), player.weapon.index, xDir, cl, player.getNextActorNetId(), true);

		if (chargeLevel >= 3 && player.hasGoldenArmor() && player.weapon is Buster) {
			stockSaber(true);
			xSaberCooldown = 0.66f;
		}

		if (chargeLevel >= 3 && player.hasArmArmor(2)) {
			stockedCharge = true;
			if (player.weapon is Buster) {
				shootTime = hasUltimateArmor ? 0.5f : 0.25f;
			} else shootTime = 0.5f;
			Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (int)RPCToggleType.StockCharge);
		} else if (stockedCharge) {
			stockedCharge = false;
			Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (int)RPCToggleType.UnstockCharge);
		}

		if (!player.weapon.isStream) {
			chargeTime = 0;
		} else {
			streamCooldown = 0.25f;
		}

		/*if (isHyperX) {
			unpoShotCount--;
			if (unpoShotCount < 0) unpoShotCount = 0;
		}*/
	}

	public void shootRpc(Point pos, int weaponIndex, int xDir, int chargeLevel, ushort netProjId, bool sendRpc) {
		// Right before we shoot, change to the current weapon.
		// This ensures that the shoot RPC sent reflects the current weapon used
		if (!player.isAI) {
			player.changeWeaponFromWi(weaponIndex);
		}

		Weapon weapon = player.weapon;
		float oldWeaponAmmo = 0;
		bool nonBusterHyperCharge = false;
		if (ownedByLocalPlayer && weaponIndex == (int)WeaponIds.HyperBuster) {
			Weapon hyperChargeWeapon = player.weapons[player.hyperChargeSlot];
			if (hyperChargeWeapon is not Buster || player.hasUltimateArmor()) {
				player.weapon.addAmmo(-HyperBuster.ammoUsage, player);
				weapon = hyperChargeWeapon;
				if (hyperChargeWeapon is not Buster) {
					nonBusterHyperCharge = true;
					weapon.addAmmo(-HyperBuster.weaponAmmoUsage, player);
					oldWeaponAmmo = weapon.ammo;
				}
			}

			chargeLevel = 3;
		}

		shoot(weapon, pos, xDir, player, chargeLevel, netProjId);

		if (ownedByLocalPlayer && nonBusterHyperCharge) {
			weapon.ammo = oldWeaponAmmo;
		}

		if (ownedByLocalPlayer && sendRpc) {
			var playerIdByte = (byte)player.id;
			var xDirByte = (byte)(xDir + 128);
			var chargeLevelByte = (byte)chargeLevel;
			var netProjIdBytes = BitConverter.GetBytes(netProjId);
			var xBytes = BitConverter.GetBytes((short)pos.x);
			var yBytes = BitConverter.GetBytes((short)pos.y);
			var weaponIndexByte = (byte)weapon.index;

			RPC shootRpc = RPC.shoot;
			if (weapon is FireWave) {
				// Optimize firewave shoot RPCs since they churn out at a fast rate
				shootRpc = RPC.shootFast;
			}

			Global.serverClient?.rpc(
				shootRpc, playerIdByte,
				xBytes[0], xBytes[1], yBytes[0], yBytes[1], xDirByte,
				chargeLevelByte, netProjIdBytes[0], netProjIdBytes[1], weaponIndexByte
			);
		}
	}

	public void shoot(Weapon weapon, Point pos, int xDir, Player player, int chargeLevel, ushort netProjId) {
		if (stockedCharge) {
			chargeLevel = 3;
		}

		weapon.getProjectile(pos, xDir, player, chargeLevel, netProjId);

		if (weapon.soundTime == 0) {
			if (weapon.shootSounds != null && weapon.shootSounds.Length > 0) {
				int shootSoundIndex = chargeLevel;
				if (shootSoundIndex >= weapon.shootSounds.Length) {
					shootSoundIndex = weapon.shootSounds.Length - 1;
				}
				if (weapon.shootSounds[chargeLevel] != "") {
					player.character.playSound(weapon.shootSounds[chargeLevel]);
				}
			}
			if (weapon is FireWave) {
				weapon.soundTime = 0.25f;
			}
		}

		// Only deduct ammo if owned by local player
		if (ownedByLocalPlayer) {
			float ammoUsage;
			if ((player.character as MegamanX)?.stingActive == true && chargeLevel < 3) {
				ammoUsage = 4;
			} else if (weapon is FireWave) {
				if (chargeLevel < 3) {
					float chargeTime = player.character.chargeTime;
					if (chargeTime < 1) {
						ammoUsage = Global.spf * 10;
					} else {
						ammoUsage = Global.spf * 20;
					}
				} else {
					ammoUsage = 8;
				}
			} else {
				ammoUsage = weapon.getAmmoUsage(chargeLevel);
			}
			weapon.addAmmo(-ammoUsage, player);

			/*
			if (weapon.ammo <= 0 && isHyperX == true) {
				player.weapons.Remove(this);
				player.weaponSlot--;
				if (player.weaponSlot < 0) {
					player.weaponSlot = 0;
				}
			}
			*/
		}
	}

	// Fast upgrading via command key.
	public void quickArmorUpgrade() {
		if (!player.input.isHeld(Control.Special2, player)) {
			hyperProgress = 0;
			return;
		}
		if (player.health <= 0) {
			hyperProgress = 0;
			return;
		}
		if (!(charState is WarpIn) && (player.canUpgradeGoldenX() || player.canUpgradeUltimateX())) {
			hyperProgress += Global.spf;
		}
		if (hyperProgress < 1) {
			return;
		}
		hyperProgress = 0;
		if (player.canUpgradeGoldenX()) {
			if (!boughtGoldenArmorOnce) {
				player.currency -= Player.goldenArmorCost;
				boughtGoldenArmorOnce = true;
			}
			player.setGoldenArmor(true);
			Global.playSound("ching");
			return;
		}
		if (player.canUpgradeUltimateX()) {
			if (!boughtUltimateArmorOnce) {
				player.currency -= Player.ultimateArmorCost;
				boughtUltimateArmorOnce = true;
			}
			player.setUltimateArmor(true);
			Global.playSound("chingX4");
			return;
		}
	}

	public bool canScan() {
		if (player.isAI) return false;
		if (invulnTime > 0) return false;
		if (!(charState is Idle || charState is Run || charState is Jump || charState is Fall || charState is WallKick || charState is WallSlide || charState is Dash || charState is Crouch || charState is AirDash)) return false;
		if (Global.level.is1v1()) return false;
		return true;
	}

	public bool isHyperXOrReviving() {
		return (isHyperX || sprite.name.EndsWith("revive"));
	}

	// BARRIER SECTION

	public Anim? barrierAnim;
	public float barrierTime;
	public bool barrierFlinch;
	public float barrierDuration { get { return barrierFlinch ? 1.5f : 0.75f; } }
	public float barrierCooldown;
	public float maxBarrierCooldown { get { return 0; } }
	public bool hasBarrier(bool isOrange) {
		bool hasBarrier = barrierTime > 0 && barrierTime < barrierDuration - 0.12f;
		if (!isOrange) return hasBarrier && !player.hasChip(1);
		else return hasBarrier && player.hasChip(1);
	}
	public void updateBarrier() {
		if (barrierTime <= 0) return;

		if (barrierAnim != null) {
			if (barrierAnim.sprite.name == "barrier_start" && barrierAnim.isAnimOver()) {
				string spriteName = player.hasChip(1) ? "barrier2" : "barrier";
				barrierAnim.changeSprite(spriteName, true);
			}
			barrierAnim.changePos(getCenterPos());
		}

		barrierTime -= Global.spf;
		if (barrierTime <= 0) {
			removeBarrier();
			barrierCooldown = maxBarrierCooldown;
		}
	}
	public void addBarrier(bool isFlinch) {
		if (!ownedByLocalPlayer) return;
		if (barrierAnim != null) return;
		if (barrierTime > 0) return;
		if (barrierCooldown > 0) return;
		barrierFlinch = isFlinch;
		barrierAnim = new Anim(getCenterPos(), "barrier_start", 1, player.getNextActorNetId(), false, true, true);
		barrierTime = barrierDuration;
		RPC.playerToggle.sendRpc(player.id, RPCToggleType.StartBarrier);
	}
	public void removeBarrier() {
		if (!ownedByLocalPlayer) return;
		barrierTime = 0;
		barrierAnim?.destroySelf();
		barrierAnim = null;
		RPC.playerToggle.sendRpc(player.id, RPCToggleType.StopBarrier);
	}

	// RAY SPLASHER SECTION

	public bool isShootingRaySplasher;
	public float raySplasherCooldown;
	public int raySplasherMod;
	public float raySplasherCooldown2;

	public void setShootRaySplasher(bool isShooting) {
		isShootingRaySplasher = isShooting;
		if (!isShooting) {
			raySplasherCooldown = 0;
			raySplasherMod = 0;
			raySplasherCooldown2 = 0;
			if (ownedByLocalPlayer) RPC.playerToggle.sendRpc(player.id, RPCToggleType.StopRaySplasher);
		}
	}

	public void removeBusterProjs() {
		chargedSpinningBlade = null;
		chargedFrostShield = null;
		chargedTunnelFang = null;
		changeSprite("mmx_" + charState.sprite, true);
	}

	public bool hasBusterProj() {
		return chargedSpinningBlade != null || chargedFrostShield != null || chargedTunnelFang != null;
	}

	public void destroyBusterProjs() {
		chargedSpinningBlade?.destroySelf();
		chargedFrostShield?.destroySelf();
		chargedTunnelFang?.destroySelf();
	}

	public bool checkMaverickWeakness(ProjIds projId) {
		switch (player.weapon) {
			case Torpedo:
				return projId == ProjIds.ArmoredARoll;
			case Sting:
				return projId == ProjIds.BoomerangKBoomerang;
			case RollingShield:
				return projId == ProjIds.SparkMSpark;
			case FireWave:
				return projId == ProjIds.StormETornado;
			case Tornado:
				return projId == ProjIds.StingCSting;
			case ElectricSpark:
				return projId == ProjIds.ChillPIcePenguin || projId == ProjIds.ChillPIceShot;
			case Boomerang:
				return projId == ProjIds.LaunchOMissle || projId == ProjIds.LaunchOTorpedo;
			case ShotgunIce:
				return projId == ProjIds.FlameMFireball || projId == ProjIds.FlameMOilFire;
			case CrystalHunter:
				return projId == ProjIds.MagnaCMagnetMine;
			case BubbleSplash:
				return projId == ProjIds.WheelGSpinWheel;
			case SilkShot:
				return projId == ProjIds.FStagFireball || projId == ProjIds.FStagDash ||
					   projId == ProjIds.FStagDashCharge || projId == ProjIds.FStagDashTrail;
			case SpinWheel:
				return projId == ProjIds.WSpongeChain || projId == ProjIds.WSpongeUpChain;
			case SonicSlicer:
				return projId == ProjIds.CSnailCrystalHunter;
			case StrikeChain:
				return projId == ProjIds.OverdriveOSonicSlicer || projId == ProjIds.OverdriveOSonicSlicerUp;
			case MagnetMine:
				return projId == ProjIds.MorphMCScrap || projId == ProjIds.MorphMBeam;
			case SpeedBurner:
				return projId == ProjIds.BCrabBubbleSplash;
			case AcidBurst:
				return projId == ProjIds.BBuffaloIceProj || projId == ProjIds.BBuffaloIceProjGround;
			case ParasiticBomb:
				return projId == ProjIds.GBeetleGravityWell || projId == ProjIds.GBeetleBall;
			case TriadThunder:
				return projId == ProjIds.TunnelRTornadoFang || projId == ProjIds.TunnelRTornadoFang2 || projId == ProjIds.TunnelRTornadoFangDiag;
			case SpinningBlade:
				return projId == ProjIds.VoltCBall || projId == ProjIds.VoltCTriadThunder || projId == ProjIds.VoltCUpBeam || projId == ProjIds.VoltCUpBeam2;
			case RaySplasher:
				return projId == ProjIds.CrushCArmProj;
			case GravityWell:
				return projId == ProjIds.NeonTRaySplasher;
			case FrostShield:
				return projId == ProjIds.BHornetBee || projId == ProjIds.BHornetHomingBee;
			case TunnelFang:
				return projId == ProjIds.TSeahorseAcid1 || projId == ProjIds.TSeahorseAcid2;
		}

		return false;
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		Projectile? proj = null;

		if (sprite.name.Contains("beam_saber") && sprite.name.Contains("2")) {
			float overrideDamage = 3;
			if (!grounded) overrideDamage = 2;
			proj = new GenericMeleeProj(new XSaber(player), centerPoint, ProjIds.X6Saber, player, damage: overrideDamage, flinch: 0);
		} else if (sprite.name.Contains("beam_saber")) {
			proj = new GenericMeleeProj(new XSaber(player), centerPoint, ProjIds.XSaber, player);
		} else if (sprite.name.Contains("nova_strike")) {
			proj = new GenericMeleeProj(new NovaStrike(player), centerPoint, ProjIds.NovaStrike, player);
		} else if (sprite.name.Contains("speedburner")) {
			proj = new GenericMeleeProj(new SpeedBurner(player), centerPoint, ProjIds.SpeedBurnerCharged, player);
		} else if (sprite.name.Contains("shoryuken")) {
			proj = new GenericMeleeProj(new ShoryukenWeapon(player), centerPoint, ProjIds.Shoryuken, player);
		} else if (sprite.name.Contains("unpo_grab_dash")) {
			proj = new GenericMeleeProj(new XUPGrab(), centerPoint, ProjIds.UPGrab, player, 0, 0, 0);
		} else if (sprite.name.Contains("unpo_punch")) {
			proj = new GenericMeleeProj(new XUPPunch(player), centerPoint, ProjIds.UPPunch, player);
		} else if (sprite.name.Contains("unpo_air_punch")) {
			proj = new GenericMeleeProj(new XUPPunch(player), centerPoint, ProjIds.UPPunch, player, damage: 3, flinch: Global.halfFlinch);
		} else if (sprite.name.Contains("unpo_parry_start")) {
			proj = new GenericMeleeProj(new XUPParry(), centerPoint, ProjIds.UPParryBlock, player, 0, 0, 1);
		}

		return proj;
	}

	public void popAllBubbles() {
		for (int i = chargedBubbles.Count - 1; i >= 0; i--) {
			chargedBubbles[i].destroySelf();
		}
	}

	public override bool canClimbLadder() {
		if (hasBusterProj() || isShootingRaySplasher) {
			return false;
		}
		return base.canClimbLadder();
	}

	public override bool canCharge() {
		if (beeSwarm != null) return false;
		Weapon weapon = player.weapon;
		if (weapon is RollingShield && chargedRollingShieldProj != null) return false;
		if (stingActive) return false;
		if (flag != null) return false;
		if (player.weapons.Count == 0) return false;
		if (weapon is AbsorbWeapon) return false;

		return true;
	}

	public override bool canShoot() {
		if (isInvulnerableAttack()) return false;
		if (chargedSpinningBlade != null) return false;
		if (isShootingRaySplasher) return false;
		if (chargedFrostShield != null) return false;
		if (chargedTunnelFang != null) return false;
		if (invulnTime > 0) return false;

		return base.canShoot();
	}

	public override bool canChangeWeapons() {
		if (strikeChainProj != null) return false;
		if (isShootingRaySplasher) return false;
		if (chargedSpinningBlade != null) return false;
		if (chargedFrostShield != null) return false;
		if (charState is GravityWellChargedState) return false;
		if (player.weapon is TriadThunder triadThunder && triadThunder.shootTime > 0.75f) return false;
		if (charState is XRevive || charState is XReviveStart) return false;

		return base.canChangeWeapons();
	}

	// Handles Bubble Splash Charged jump height

	public override float getJumpPower() {
		float jumpModifier = 0;
		jumpModifier += (chargedBubbles.Count / 6.0f) * 50;

		return jumpModifier + base.getJumpPower();
	}

	// Jack's version of the fix above, documented.

	/* public override float getJumpModifier() {
		float jumpModifier = 1;
		jumpModifier += (chargedBubbles.Count / 6.0f) * 5;

		return jumpModifier * base.getJumpModifier();
	} */

	public override bool changeState(CharState newState, bool forceChange = false) {
		bool hasChanged = base.changeState(newState, forceChange);
		if (!hasChanged) {
			return false;
		}
		if (hasBusterProj() && string.IsNullOrEmpty(newState.shootSprite) && newState is not Hurt) {
			destroyBusterProjs();
		}
		return true;
	}

	public void drawHyperCharge(float x, float y) {
		addRenderEffect(RenderEffectType.ChargeOrange, 0.05f, 0.1f);
		hyperChargeAnimTime += Global.spf;
		if (hyperChargeAnimTime >= maxHyperChargeAnimTime) hyperChargeAnimTime = 0;
		float sx = pos.x + x;
		float sy = pos.y + y - 18;

		var sprite1 = hyperChargePartSprite;
		float distFromCenter = 12;
		float posOffset = hyperChargeAnimTime * 50;
		int hyperChargeAnimFrame = MathInt.Floor((hyperChargeAnimTime / maxHyperChargeAnimTime) * sprite1.totalFrameNum);
		sprite1.draw(hyperChargeAnimFrame, sx + distFromCenter + posOffset, sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx - distFromCenter - posOffset, sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx, sy + distFromCenter + posOffset, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx, sy - distFromCenter - posOffset, 1, 1, null, 1, 1, 1, zIndex + 1);

		hyperChargeAnimTime2 += Global.spf;
		if (hyperChargeAnimTime2 >= maxHyperChargeAnimTime) hyperChargeAnimTime2 = 0;
		var sprite2 = hyperChargePart2Sprite;
		float distFromCenter2 = 12;
		float posOffset2 = hyperChargeAnimTime2 * 50;
		int hyperChargeAnimFrame2 = MathInt.Floor(
			(hyperChargeAnimTime2 / maxHyperChargeAnimTime) * sprite2.totalFrameNum
		);
		float xOff = Helpers.cosd(45) * (distFromCenter2 + posOffset2);
		float yOff = Helpers.sind(45) * (distFromCenter2 + posOffset2);
		sprite2.draw(hyperChargeAnimFrame2, sx - xOff, sy + yOff, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite2.draw(hyperChargeAnimFrame2, sx + xOff, sy - yOff, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite2.draw(hyperChargeAnimFrame2, sx + xOff, sy + yOff, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite2.draw(hyperChargeAnimFrame2, sx - xOff, sy - yOff, 1, 1, null, 1, 1, 1, zIndex + 1);
	}

	public override void render(float x, float y) {
		addRenderEffect(RenderEffectType.TrailX);
		if (!shouldRender(x, y)) {
			return;
		}
		if (isShootingRaySplasher) {
			var shootPos = getShootPos();
			var muzzleFrameCount = Global.sprites["raysplasher_muzzle"].frames.Length;
			Global.sprites["raysplasher_muzzle"].draw(
				Global.frameCount % muzzleFrameCount,
				shootPos.x + x + (3 * xDir), shootPos.y + y, 1, 1, null, 1, 1, 1, zIndex
			);
		}
		if (isHyperChargeActive && visible) {
			drawHyperCharge(x, y);
		}
		base.render(x, y);
	}

	public override void destroySelf(
		string spriteName = "", string fadeSound = "",
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {
		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy);

		chargedRollingShieldProj?.destroySelfNoEffect();
		strikeChainProj?.destroySelf();
		barrierAnim?.destroySelf();
		beeSwarm?.destroy();
		destroyBusterProjs();
		setShootRaySplasher(false);

		player.removeOwnedMines();
		player.removeOwnedTurrets();

		player.usedChipOnce = false;

		if (player.hasUltimateArmor()) {
			player.setUltimateArmor(false);
		}
		if (player.hasGoldenArmor()) {
			player.setGoldenArmor(false);
		}
	}


	public bool canHeadbutt() {
		if (!player.hasHelmetArmor(1)) return false;
		if (stingActive) return false;
		if (isInvulnerableAttack()) return false;
		if (headbuttAirTime < 0.04f) return false;
		if (sprite.name.Contains("jump") && deltaPos.y < -100 * Global.spf) return true;
		if (sprite.name.Contains("up_dash") || sprite.name.Contains("wall_kick")) return true;
		if (charState is StrikeChainPullToWall scptw && scptw.isUp) return true;
		return false;
	}

	public bool hasHadoukenEquipped() {
		return !Global.level.is1v1() && player.hasArmArmor(1) && player.hasBootsArmor(1) && player.hasHelmetArmor(1) && player.hasBodyArmor(1) && player.weapons.Any(w => w is Buster);
	}

	public bool hasShoryukenEquipped() {
		return !Global.level.is1v1() && player.hasArmArmor(2) && player.hasBootsArmor(2) && player.hasHelmetArmor(2) && player.hasBodyArmor(2) && player.weapons.Any(w => w is Buster);
	}

	public bool hasFgMoveEquipped() {
		return hasHadoukenEquipped() || hasShoryukenEquipped();
	}

	public bool canAffordFgMove() {
		return player.currency >= 3 || player.hasAllItems();
	}

	public bool canUseFgMove() {
		return !isInvulnerableAttack() && chargedRollingShieldProj == null && !stingActive && canAffordFgMove() && hadoukenCooldownTime == 0 && player.weapon is Buster && player.fgMoveAmmo >= 32;
	}

	public bool shouldDrawFgCooldown() {
		return !isInvulnerableAttack() && chargedRollingShieldProj == null && !stingActive && canAffordFgMove() && hadoukenCooldownTime == 0;
	}

	public override Dictionary<int, Func<Projectile>> getGlobalProjs() {
		var retProjs = new Dictionary<int, Func<Projectile>>();

		if (canHeadbutt() && getHeadPos() != null) {
			retProjs[(int)ProjIds.Headbutt] = () => {
				Point centerPoint = getHeadPos()!.Value.addxy(0, -6);
				float damage = 2;
				int flinch = Global.halfFlinch;
				if (sprite.name.Contains("up_dash")) {
					damage = 4;
					flinch = Global.defFlinch;
				}
				Projectile proj = new GenericMeleeProj(
					Headbutt.netWeapon, centerPoint, ProjIds.Headbutt, player,
					damage, flinch, 0.5f
				);
				var rect = new Rect(0, 0, 14, 4);
				proj.globalCollider = new Collider(rect.getPoints(), false, proj, false, false, 0, Point.zero);
				return proj;
			};
		}
		return retProjs;
	}

	public override void onFlinchOrStun(CharState newState) {
		strikeChainProj?.destroySelf();
		if (newState is not Hurt hurtState) {
			beeSwarm?.destroy();
		} else {
			beeSwarm?.reset(hurtState.isMiniFlinch());
		}
		base.onFlinchOrStun(newState);
	}

	public override void onExitState(CharState oldState, CharState newState) {
		if (string.IsNullOrEmpty(newState?.shootSprite)) {
			setShootRaySplasher(false);
		}
	}

	public bool isSpecialBuster() {
		return player.loadout.xLoadout.melee == 0 && !isHyperX;
	}

	public bool isSpecialSaber() {
		return isHyperX || player.loadout.xLoadout.melee == 1;
	}

	public override bool chargeButtonHeld() {
		if (isSpecialBuster() && player.input.isHeld(Control.Special1, player)) {
			return true;
		}
		return player.input.isHeld(Control.Shoot, player);
	}

	public override bool canAirDash() {
		return dashedInAir == 0 || (dashedInAir == 1 && player.hasChip(0));
	}

	public override string getSprite(string spriteName) {
		return "mmx_" + spriteName;
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;
		int index = player.weapon.index;

		if (index == (int)WeaponIds.GigaCrush ||
			index == (int)WeaponIds.ItemTracer ||
			index == (int)WeaponIds.AssassinBullet ||
			index == (int)WeaponIds.Undisguise ||
			index == (int)WeaponIds.UPParry
		) {
			index = 0;
		}
		if (index == (int)WeaponIds.HyperBuster && ownedByLocalPlayer) {
			index = player.weapons[player.hyperChargeSlot].index;
		}
		if (player.hasGoldenArmor()) {
			index = 25;
		}
		if (hasUltimateArmor) {
			index = 0;
		}
		palette = player.xPaletteShader;

		if (!isCStingInvisibleGraphics()) {
			palette?.SetUniform("palette", index);
			palette?.SetUniform("paletteTexture", Global.textures["paletteTexture"]);
		} else {
			palette?.SetUniform("palette", this.cStingPaletteIndex % 9);
			palette?.SetUniform("paletteTexture", Global.textures["cStingPalette"]);
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

	public override bool canAddAmmo() {
		if (player.weapon == null) { return false; }
		bool hasEmptyAmmo = false;
		foreach (Weapon weapon in player.weapons) {
			if (weapon.canHealAmmo && weapon.ammo < weapon.maxAmmo) {
				hasEmptyAmmo = true;
				break;
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
		if (player.weapon.canHealAmmo && player.weapon.ammo < player.weapon.maxAmmo) {
			return player.weapon;
		}
		foreach (Weapon weapon in player.weapons) {
			if (weapon is GigaCrush or NovaStrike or HyperBuster &&
			player.weapon.ammo < player.weapon.maxAmmo
			) {
				return weapon;
			}
		}
		Weapon? targetWeapon = null;
		float targetAmmo = Int32.MaxValue;
		foreach (Weapon weapon in player.weapons) {
			if (!weapon.canHealAmmo) {
				continue;
			}
			if (weapon != player.weapon &&
				weapon.ammo < weapon.maxAmmo &&
				weapon.ammo < targetAmmo
			) {
				targetWeapon = weapon;
				targetAmmo = targetWeapon.ammo;
			}
		}
		return targetWeapon;
	}

	public override void increaseCharge() {
		float factor = 1;
		if (player.hasArmArmor(1)) { factor = 1.5f; }
		chargeTime += Global.speedMul * factor;
	}


	public override bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		bool damaged = base.canBeDamaged(damagerAlliance, damagerPlayerId, projId);

		// Bommerang can go thru invisibility check
		if (player.alliance != damagerAlliance && projId != null && Damager.isBoomerang(projId)) {
			return damaged;
		}
		if (stingActive) {
			return false;
		}
		return damaged;
	}

	public override bool isStealthy(int alliance) {
		return (player.alliance != alliance && stingActive);
	}

	public override bool isCCImmuneHyperMode() {
		return isHyperX;
	}

	public bool shouldShowHyperBusterCharge() {
		return player.weapon is HyperBuster hb && hb.canShootIncludeCooldown(player) || flag != null;
	}

	public override bool isInvulnerable(bool ignoreRideArmorHide = false, bool factorHyperMode = false) {
		bool invul = base.isInvulnerable(ignoreRideArmorHide, factorHyperMode);
		if (stingActive) {
			return true;
		}
		return invul;
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();

		int weaponIndex = player.weapon.index;
		if (weaponIndex == (int)WeaponIds.HyperBuster) {
			weaponIndex = player.weapons[player.hyperChargeSlot].index;
		}
		customData.Add((byte)weaponIndex);
		customData.Add((byte)MathF.Ceiling(player.weapon?.ammo ?? 0));

		customData.AddRange(BitConverter.GetBytes(player.armorFlag));

		customData.Add(Helpers.boolArrayToByte([
			stingActive,
			isHyperX,
			isHyperChargeActive,
			hasUltimateArmor
		]));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-player data.
		player.changeWeaponFromWi(data[0]);
		if (player.weapon != null) {
			player.weapon.ammo = data[1];
		}
		player.armorFlag = BitConverter.ToUInt16(data[2..4]);

		bool[] boolData = Helpers.byteToBoolArray(data[4]);
		stingActive = boolData[0];
		isHyperX = boolData[1];
		isHyperChargeActive = boolData[2];
		hasUltimateArmor = boolData[3];
	}
}
