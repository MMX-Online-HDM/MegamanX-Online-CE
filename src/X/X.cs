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
	public StrikeChainProjCharged? strikeChainChargedProj;
	public GravityWellProjCharged? chargedGravityWell;
	public SpinningBladeProjCharged? chargedSpinningBlade;
	public FrostShieldProjCharged? chargedFrostShield;
	public TornadoFangProjCharged? chargedTornadoFang;
	public GravityWellProj? gravityWell;
	public int totalChipHealAmount;
	public const int maxTotalChipHealAmount = 32;
	public int unpoShotCount;

	public float shootCooldown;
	public float hyperchargeCooldown;
	public float novaStrikeCooldown; // This one is mostly used just to show its cooldown on screen.
	public float hadoukenCooldownTime;
	public float maxHadoukenCooldownTime = 60;
	public float shoryukenCooldownTime;
	public float maxShoryukenCooldownTime = 60;
	//public ShaderWrapper xPaletteShader;

	public float streamCooldown;
	public float noDamageTime;
	public float rechargeHealthTime;
	public float scannerCooldown;
	float UPDamageCooldown;
	public float unpoDamageMaxCooldown = 120;
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
	public float maxParryCooldown = 30;

	public bool stingActive;
	public bool isHyperChargeActive;

	public Sprite hyperChargePartSprite =  new Sprite("hypercharge_part_1");
	public Sprite hyperChargePart2Sprite =  new Sprite("hypercharge_part_1");

	public bool isShootingSpecialBuster;

	public XBuster staticBusterWeapon = new();
	public XBuster specialBuster;
	public float WeaknessT;

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

	public bool canShootSpecialBuster() {
		if (isHyperX && (charState is Dash || charState is AirDash)) {
			return false;
		}
		return isSpecialBuster() &&
			player.weapon is not XBuster &&
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
			if (player.weapon is not XBuster) {
				stockedX3Buster = false;
			} else {
				addRenderEffect(RenderEffectType.ChargeOrange, 0.05f, 0.1f);
			}
		}

		stingActive = stingChargeTime > 0;
		if (stingActive) {
			addRenderEffect(RenderEffectType.Invisible);
		} else {
			removeRenderEffect(RenderEffectType.Invisible);
		}

		if (isHyperX) {
			if (musicSource == null) {
				addMusicSource("introStageBreisX4_JX", getCenterPos(), true);
			} 
		} else destroyMusicSource();
		
		if (cStingPaletteTime > 5) {
			cStingPaletteTime = 0;
			cStingPaletteIndex++;
		}
		cStingPaletteTime++;

		if (headbuttAirTime > 0) {
			headbuttAirTime += Global.spf;
		}

		Helpers.decrementFrames(ref parryCooldown);
		Helpers.decrementFrames(ref shootCooldown);
		Helpers.decrementFrames(ref hyperchargeCooldown);
		Helpers.decrementFrames(ref novaStrikeCooldown);
		Helpers.decrementFrames(ref barrierCooldown);
		Helpers.decrementFrames(ref xSaberCooldown);
		Helpers.decrementFrames(ref scannerCooldown);
		Helpers.decrementFrames(ref hadoukenCooldownTime);
		Helpers.decrementFrames(ref shoryukenCooldownTime);
		Helpers.decrementFrames(ref streamCooldown);
		Helpers.decrementFrames(ref WeaknessT);
		Helpers.decrementFrames(ref upPunchCooldown);
		isHyperChargeActive = shouldShowHyperBusterCharge();

		Helpers.decrementTime(ref shootAnimTime);
		if (shootAnimTime <= 0 && charState.attackCtrl && !charState.isGrabbing) {
			if (!hasBusterProj()) {
				changeSpriteFromName(charState.defaultSprite, false);
				if (charState is WallSlide) {
					frameIndex = sprite.totalFrameNum - 1;
				}
			}		
		}

		if (lastShotWasSpecialBuster) chargeLogic(specialShoot);
		else chargeLogic(shoot);

		if (beeSwarm != null) {
			beeSwarm.update();
		}

		updateBarrier();

		player.fgMoveAmmo += Global.speedMul;
		if (player.fgMoveAmmo > player.fgMoveMaxAmmo) player.fgMoveAmmo = player.fgMoveMaxAmmo;

		if (stingChargeTime > 0) {
			hadoukenCooldownTime = maxHadoukenCooldownTime;
			shoryukenCooldownTime = maxShoryukenCooldownTime;
		}

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

		if (player.hasChip(2) && !isInvisible() && totalChipHealAmount < maxTotalChipHealAmount) {
			noDamageTime += Global.speedMul;
			if ((player.health < player.maxHealth || player.hasSubtankCapacity()) && noDamageTime > 240) {
				Helpers.decrementFrames(ref rechargeHealthTime);
				if (rechargeHealthTime <= 0) {
					rechargeHealthTime = 60;
					addHealth(1);
					totalChipHealAmount++;
				}
			}
		}

		// Fast Hyper Activation.
		quickArmorUpgrade();

		//Fast Chip Activation.
		if (charState is not Die &&
			player.input.isPressed(Control.Special1, player) &&
			player.hasAllX3Armor() && !player.hasGoldenArmor() && !player.hasUltimateArmor()) {
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

		//UPX HP Decay.
		if (isHyperX) {
			if (charState is not XUPGrabState
				and not XUPParryMeleeState
				and not XUPParryProjState
				and not Hurt
				and not GenericStun
				and not VileMK2Grabbed
				and not GenericGrabbedState
			) {
				unpoTime += Global.speedMul;
				UPDamageCooldown += Global.speedMul;
				if (UPDamageCooldown > unpoDamageMaxCooldown) {
					UPDamageCooldown = 0;
					applyDamage(1, player, this, null, null);
				}
			}

			unpoShotCount = MathInt.Floor(player.weapon.ammo / player.weapon.getAmmoUsage(0));
		}

		//Giga Helmet Scan.
		if (charState.attackCtrl && player.hasHelmetArmor(2) && scannerCooldown <= 0 && canScan()) {
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

		if (!isHyperX) {
			player.changeWeaponControls();
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
						player, player.getNextActorNetId(), rpc: true
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
	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			int chargeType = 2;
			if (player.hasArmArmor(3) && !player.hasGoldenArmor()) {
				chargeType = 1;
			}
			if (player.hasGoldenArmor()) {
				chargeType = 3;
			}
			
			int level = isHyperX ? unpoShotCount : getChargeLevel();
			var renderGfx = RenderEffectType.ChargeBlue;
			renderGfx = level switch {
				1 => RenderEffectType.ChargeBlue,
				2 => RenderEffectType.ChargeYellow,
				3 when (chargeType == 1) => RenderEffectType.ChargeOrange,
				3 when (chargeType == 3) => RenderEffectType.ChargeGreen,
				3 => RenderEffectType.ChargePink,
				_ => RenderEffectType.ChargeOrange
			};
			addRenderEffect(renderGfx, 0.033333f, 0.1f);			
			chargeEffect.update(level, chargeType);
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
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool shootHeld = player.input.isHeld(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		
		if (isHyperX) {
			if (shootPressed && upPunchCooldown <= 0 && unpoShotCount <= 0 ) {
				upPunchCooldown = 30;
				changeState(new XUPPunchState(grounded), true);
				return true;
			} 
			else if (specialPressed && charState is Dash or AirDash) {
				charState.isGrabbing = true;
				changeSpriteFromName("unpo_grab_dash", true);
				return true;
			} 
			else if ( player.input.isWeaponLeftOrRightPressed(player) && parryCooldown <= 0 ) {
				if (unpoAbsorbedProj != null) {
					changeState(new XUPParryProjState(unpoAbsorbedProj, true, false), true);
					unpoAbsorbedProj = null;
					return true;
				} else {
					changeState(new XUPParryStartState(), true);
					return true;
				}
			}
		}

		if ( (isSpecialSaber() || isHyperX) && !hasBusterProj() &&
			canChangeWeapons() && player.armorFlag == 0 &&
			specialPressed && !stingActive
		) {
			if (xSaberCooldown == 0) {
				xSaberCooldown = 60;
				changeState(new X6SaberState(grounded), true);
				return true;
			}
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

		Point inputDir = player.input.getInputDir(player);
		int oldSlot, newSlot;

		if (Global.level.is1v1() && player.weapons.Count == 10) {
			if (player.weaponSlot != 9) {
				player.weapons[9].update();
			}

			if (player.input.isPressed(Control.Special1, player) && chargeTime == 0) {
				oldSlot = player.weaponSlot;
				player.changeWeaponSlot(9);
				if (shootCooldown <= 0) {
					shoot(getChargeLevel());
				}
				player.changeWeaponSlot(oldSlot);
				return true;
			}
		}

		if (Options.main.gigaCrushSpecial &&
			player.input.isPressed(Control.Special1, player) &&
			player.input.isHeld(Control.Down, player) &&
			player.weapons.Any(w => w is GigaCrush)
		) {
			oldSlot = player.weaponSlot;
			newSlot = player.weapons.FindIndex(w => w is GigaCrush);
			player.changeWeaponSlot(newSlot);
			shoot(getChargeLevel());
			player.changeWeaponSlot(oldSlot);
			return true;
		} 
		else if (Options.main.novaStrikeSpecial &&
			player.input.isPressed(Control.Special1, player) &&
			player.weapons.Any(w => w is NovaStrike) &&
			!inputDir.isZero()
		) {
			oldSlot = player.weaponSlot;
			newSlot = player.weapons.FindIndex(w => w is NovaStrike);
			player.changeWeaponSlot(newSlot);
			if (novaStrikeCooldown <= 0) {
				shoot(getChargeLevel());
			}
			player.changeWeaponSlot(oldSlot);
			return true;
		}
		

		bool shootCondition = (
			shootPressed || specialPressed ||
			(shootHeld && player.weapon.isStream && chargeTime < charge2Time)
		);
		
		if (shootPressed || specialPressed) {
			lastShootPressed = Global.frameCount;
		}
		int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
		
		if (shootCondition) {
			if (specialPressed) {
				specialShoot(getChargeLevel());
				return true;
			} else {
				shoot(getChargeLevel());
				return true;
			}
		}

		return base.attackCtrl();
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

	public void setShootAnim() {
		string shootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) { shootSprite = "mmx_shoot"; } else { shootSprite = "mmx_fall_shoot"; }
		}
		
		changeSprite(shootSprite, false);
		if (charState is Idle) {
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
		shootAnimTime = 0.3f;
	}

	public void shoot(int chargeLevel) {
		//We check if we have stocked x3 saber first so we can use it even when having no ammo.
		if (stockedXSaber && !stockedX3Buster) {
			if (xSaberCooldown == 0) {
				stockSaber(false);
				changeState(new XSaberState(grounded), true);
			}
			return;
		}

		//We don't shoot if we have no ammo.
		if (!player.weapon.canShoot(chargeLevel, player)) return;
		//We don't use hypercharge if its cooldown is not 0.
		if (player.weapon is HyperCharge && hyperchargeCooldown > 0) return;
		if (!canShoot()) return;

		//Set charge level.
		chargeLevel = stockedCharge ? 3 : chargeLevel;

		//Change to shoot sprite
		setShootAnim();

		//Plays weapon sound.
		if (player.weapon.soundTime <= 0) {
			if (player.weapon.shootSounds != null && player.weapon.shootSounds.Length > 0) {
				int soundIndex = chargeLevel;
				if (soundIndex >= player.weapon.shootSounds.Length) {
					soundIndex = player.weapon.shootSounds.Length - 1;
				}
				if (player.weapon.shootSounds[soundIndex] != "") {
					player.character.playSound(player.weapon.shootSounds[soundIndex]);
				}
			}
			if (player.weapon is FireWave) {
				player.weapon.soundTime = 15;
			}
		}
		
		//Gets ammo usage.
		float ammoUsage = -player.weapon.getAmmoUsageEX(chargeLevel, this);
		//Triggers weapon cooldown.
		shootCooldown = player.weapon is HyperCharge hb ?
			hb.getRateOfFire(player) : player.weapon.fireRate;
		//Triggers hypercharge special cooldown if used.
		if (player.weapon is HyperCharge h) hyperchargeCooldown = h.getRateOfFire(player);
		//Triggers hypercharge special cooldown when shooting a charged shot.
		if (chargeLevel >= 2 && player.weapons.Any(w => w is HyperCharge b)) {
			var hbWep = player.weapons.FirstOrDefault(w => w is HyperCharge) as HyperCharge;
			if (hbWep != null) {
				hyperchargeCooldown = hbWep.getRateOfFire(player);
			}
		}


		//Spends ammo and spawns the projectile.
		player.weapon.addAmmo(ammoUsage, player);

		//player.weapon.shoot(this, new int[] {chargeLevel});
		shootWeapon(this, new int[] {chargeLevel}, player.weapon);

		if (!player.weapon.isStream || chargeLevel >= 3) stopCharge();
		else streamCooldown = 15;

		//Stock Chargeshots stuff
		//Giga buster.
		bool updatedStock = false;
		if (chargeLevel >= 3 && player.hasArmArmor(2)) {
			if (player.weapon is XBuster && !stockedCharge) {
				shootCooldown = hasUltimateArmor ? 30 : 15;
			} else shootCooldown = 30;
	
			stockCharge(!stockedCharge);
			updatedStock = true;
		}

		if (!updatedStock) stockCharge(false);

		//Max Buster.
		if (chargeLevel >= 3 && player.hasGoldenArmor() && player.weapon is XBuster) {
			stockSaber(true);
			xSaberCooldown = 40;
		}

		lastShotWasSpecialBuster = false;
	}

	public void specialShoot(int chargeLevel) {
		chargeLevel = stockedCharge ? 3 : chargeLevel;
		if (!specialBuster.canShoot(chargeLevel, player)) return;
		if (!canShootSpecialBuster()) return;
		if (!canShoot()) return;

		setShootAnim();

		shootCooldown = specialBuster.fireRate;
		shootWeapon(this, new int[] {chargeLevel}, specialBuster);
		stopCharge();

		lastShotWasSpecialBuster = true;
	}

	void shootWeapon(Character character, int[] args, Weapon w) {
		switch (player.armArmorNum) {
			case (int)ArmorId.Light:
				w.shootLight(character, args);
				break;
			
			case (int)ArmorId.Giga:
				w.shootSecond(character, args);
				break;

			case (int)ArmorId.Max:
				w.shootMax(character, args);
				break;

			default:
				w.shoot(character, args);
				break;
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
		chargedTornadoFang = null;
		strikeChainProj = null;
		strikeChainChargedProj = null;
		changeSprite("mmx_" + charState.sprite, true);
	}

	public bool hasBusterProj() {
		return 
			chargedSpinningBlade != null || 
			chargedFrostShield != null || 
			chargedTornadoFang != null ||
			strikeChainProj != null ||
			strikeChainChargedProj != null ||
			isShootingRaySplasher;
	}

	public void destroyBusterProjs() {
		chargedSpinningBlade?.destroySelf();
		chargedFrostShield?.destroySelf();
		chargedTornadoFang?.destroySelf();
		strikeChainProj?.destroySelf();
		strikeChainChargedProj?.destroySelf();
	}

	public bool checkMaverickWeakness(ProjIds projId) {
		switch (player.weapon) {
			case HomingTorpedo:
				return projId == ProjIds.ArmoredARoll;
			case ChameleonSting:
				return projId == ProjIds.BoomerangKBoomerang;
			case RollingShield:
				return projId == ProjIds.SparkMSpark;
			case FireWave:
				return projId == ProjIds.StormETornado;
			case StormTornado:
				return projId == ProjIds.StingCSting;
			case ElectricSpark:
				return projId == ProjIds.ChillPIcePenguin || projId == ProjIds.ChillPIceShot;
			case BoomerangCutter:
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
			case TornadoFang:
				return projId == ProjIds.TSeahorseAcid1 || projId == ProjIds.TSeahorseAcid2;
		}

		return false;
	}
	public bool checkWeakness(ProjIds projId) {
		switch (player.weapon) {
			case HomingTorpedo:
				return projId == ProjIds.RollingShield || projId == ProjIds.RollingShieldCharged;
			case ChameleonSting:
				return projId == ProjIds.Boomerang || projId == ProjIds.BoomerangCharged;
			case RollingShield:
				return projId == ProjIds.ElectricSpark || projId == ProjIds.ElectricSparkCharged ||
					   projId == ProjIds.ElectricSparkChargedStart;
			case FireWave:
				return projId == ProjIds.Tornado || projId == ProjIds.TornadoCharged;
			case StormTornado:
				return projId == ProjIds.Sting || projId == ProjIds.StingDiag;
			case ElectricSpark:
				return projId == ProjIds.ShotgunIce || projId == ProjIds.ShotgunIceSled || 
					   projId == ProjIds.ShotgunIceCharged;
			case BoomerangCutter:
				return projId == ProjIds.Torpedo || projId == ProjIds.TorpedoCharged;
			case ShotgunIce:
				return projId == ProjIds.FireWave || projId == ProjIds.FireWaveCharged ||
					   projId == ProjIds.FireWaveChargedStart;
			case CrystalHunter:
				return projId == ProjIds.MagnetMine || projId == ProjIds.MagnetMineCharged;
			case BubbleSplash:
				return projId == ProjIds.SpinWheel || projId == ProjIds.SpinWheelCharged || 
					   projId == ProjIds.SpinWheelChargedStart;
			case SilkShot:
				return projId == ProjIds.SpeedBurner || projId == ProjIds.SpeedBurnerCharged ||
					   projId == ProjIds.SpeedBurnerWater || projId == ProjIds.SpeedBurnerTrail;
			case SpinWheel:
				return projId == ProjIds.StrikeChain || projId == ProjIds.StrikeChainCharged;
			case SonicSlicer:
				return projId == ProjIds.CrystalHunter || projId == ProjIds.CrystalHunterDash;
			case StrikeChain:
				return projId == ProjIds.SonicSlicer || projId == ProjIds.SonicSlicerCharged ||
					   projId == ProjIds.SonicSlicerStart;
			case MagnetMine:
				return projId == ProjIds.SilkShot || projId == ProjIds.SilkShotCharged ||
				       projId == ProjIds.SilkShotShrapnel;
			case SpeedBurner:
				return projId == ProjIds.BubbleSplash || projId == ProjIds.BubbleSplashCharged;
			case AcidBurst:
				return projId == ProjIds.FrostShield || projId == ProjIds.FrostShieldAir ||
					   projId == ProjIds.FrostShieldCharged || projId == ProjIds.FrostShieldChargedGrounded || 
					   projId == ProjIds.FrostShieldGround || projId == ProjIds.FrostShieldPlatform;
			case ParasiticBomb:
				return projId == ProjIds.GravityWell || projId == ProjIds.GravityWellCharged;
			case TriadThunder:
				return projId == ProjIds.TornadoFang || projId == ProjIds.TornadoFang2 || 
					   projId == ProjIds.TornadoFangCharged;
			case SpinningBlade:
				return projId == ProjIds.TriadThunder || projId == ProjIds.TriadThunderBall || 
					   projId == ProjIds.TriadThunderBeam || projId == ProjIds.TriadThunderCharged ||
					   projId == ProjIds.TriadThunderQuake;
			case RaySplasher:
				return projId == ProjIds.SpinningBlade || projId == ProjIds.SpinningBladeCharged;
			case GravityWell:
				return projId == ProjIds.RaySplasher || projId == ProjIds.RaySplasherChargedProj;
			case FrostShield:
				return projId == ProjIds.ParasiticBomb || projId == ProjIds.ParasiticBombCharged ||
					   projId == ProjIds.ParasiticBombExplode;
			case TornadoFang:
				return projId == ProjIds.AcidBurst || projId == ProjIds.AcidBurstCharged || 
					   projId == ProjIds.AcidBurstSmall || projId == ProjIds.AcidBurstPoison;
		}

		return false;
	}
	public enum MeleeIds {
		None = -1,
		Headbutt,
		SpeedBurnerCharged,
		Shoryuken,
		X3Saber,
		X6Saber,
		NovaStrike,
		UPGrab,
		UPPunch,
		UPParryBlock,
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		string[] hs = headbuttSprite();
		for (int i = 0; i < hs.Length; i++) {
			if (sprite.name == hs[i]) return (int)MeleeIds.Headbutt;
		}

		return (int)(sprite.name switch {
			"mmx_speedburner" => MeleeIds.SpeedBurnerCharged,
			"mmx_shoryuken" => MeleeIds.Shoryuken,
			"mmx_beam_saber" or
			"mmx_beam_saber_air" => MeleeIds.X3Saber,
			"mmx_beam_saber2" or
			"mmx_beam_saber_air2" => MeleeIds.X6Saber,
			"mmx_nova_strike" or
			"mmx_nova_strike_down" or
			"mmx_nova_strike_up" => MeleeIds.NovaStrike,
			"mmx_unpo_grab_dash" => MeleeIds.UPGrab,
			"mmx_unpo_punch" or
			"mmx_unpo_air_punch" => MeleeIds.UPPunch,
			"mmx_unpo_parry_start" => MeleeIds.UPParryBlock,

			_ => MeleeIds.None
		});
	}

	string[] headbuttSprite() {
		return new string[] {
			"mmx_jump",
			"mmx_jump_shoot",
			"mmx_wall_kick",
			"mmx_wall_kick_shoot",
			"mmx_up_dash",
			"mmx_up_dash_shoot"
		};
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		// We create the headbutt melee attack ONLY when X is using x1 helmet, obviosly.
		if (id == (int)MeleeIds.Headbutt && player.hasHelmetArmor(ArmorId.Light)) {
			float hDamage = sprite.name.Contains("up_dash") ? 4 : 2;
			int hFlinch = sprite.name.Contains("up_dash") ? Global.defFlinch : Global.halfFlinch;

			return new GenericMeleeProj(
				new LhHeadbutt(), projPos, ProjIds.Headbutt, player,
				hDamage, hFlinch, 0.5f
			);
		}

		return id switch {
			(int)MeleeIds.SpeedBurnerCharged => new GenericMeleeProj(
				new SpeedBurner(player), projPos, ProjIds.SpeedBurnerCharged, player
			),
			(int)MeleeIds.Shoryuken => new GenericMeleeProj(
				new ShoryukenWeapon(player), projPos, ProjIds.Shoryuken, player
			),
			(int)MeleeIds.X3Saber => new GenericMeleeProj(
				new ZXSaber(player), projPos, ProjIds.XSaber, player
			),
			(int)MeleeIds.X6Saber => new GenericMeleeProj(
				new ZXSaber(player), projPos, ProjIds.X6Saber, player,
				damage: grounded ? 3 : 2, flinch: 0
			),
			(int)MeleeIds.NovaStrike => new GenericMeleeProj(
				new NovaStrike(player), projPos, ProjIds.NovaStrike, player
			),
			(int)MeleeIds.UPGrab => new GenericMeleeProj(
				new XUPGrab(), projPos, ProjIds.UPGrab, player, 0, 0, 0
			),
			(int)MeleeIds.UPPunch => new GenericMeleeProj(
				new XUPPunch(player), projPos, ProjIds.UPPunch, player,
				flinch: grounded ? Global.defFlinch : Global.halfFlinch
			),
			(int)MeleeIds.UPParryBlock => new GenericMeleeProj(
				new XUPParry(), projPos, ProjIds.UPParryBlock, player, 0, 0, 1
			),
			
			_ => null
		};
	}

	/* public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
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
	} */

	public void popAllBubbles() {
		for (int i = chargedBubbles.Count - 1; i >= 0; i--) {
			chargedBubbles[i].destroySelf();
		}
	}

	public override bool canClimbLadder() {
		if (hasBusterProj()) {
			return false;
		}
		return base.canClimbLadder();
	}

	public override bool canCharge() {
		if (beeSwarm != null) return false;
		if (chargedTornadoFang != null) return false;
		if (chargedFrostShield != null) return false;
		if (chargedSpinningBlade != null) return false;
		Weapon weapon = player.weapon;
		if (weapon is RollingShield && chargedRollingShieldProj != null) return false;
		if (stingActive) return false;
		if (flag != null) return false;
		if (player.weapons.Count == 0) return false;
		if (weapon is AbsorbWeapon) return false;
		if (isInvulnerableAttack()) return false;

		return true;
	}

	public override bool canShoot() {
		if (isInvulnerableAttack()) return false;
		if (hasBusterProj()) return false;
		if (shootCooldown > 0) return false;
		if (invulnTime > 0) return false;

		return base.canShoot();
	}

	public override bool canChangeWeapons() {
		if (strikeChainProj != null) return false;
		if (strikeChainChargedProj != null) return false;
		if (isShootingRaySplasher) return false;
		if (chargedSpinningBlade != null) return false;
		if (chargedFrostShield != null) return false;
		if (charState is GravityWellChargedState) return false;
		if (charState is XRevive || charState is XReviveStart) return false;

		return base.canChangeWeapons();
	}

	public override void onWeaponChange(Weapon oldWeapon, Weapon newWeapon) {
		stingChargeTime = 0;

		//New switch cooldown logic
		if (getChargeLevel() >= 2) {
			shootCooldown = 0;
		} else {
			// Switching from laggy move (like tornado) to a fast one
			if (oldWeapon.switchCooldownFrames != null && shootCooldown > 0) {
				shootCooldown = Math.Max(shootCooldown, oldWeapon.switchCooldownFrames.Value);
			} 
		}
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
		strikeChainChargedProj?.destroySelf();
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
		return !Global.level.is1v1() && player.hasArmArmor(1) && player.hasBootsArmor(1) && player.hasHelmetArmor(1) && player.hasBodyArmor(1) && player.weapons.Any(w => w is XBuster);
	}

	public bool hasShoryukenEquipped() {
		return !Global.level.is1v1() && player.hasArmArmor(2) && player.hasBootsArmor(2) && player.hasHelmetArmor(2) && player.hasBodyArmor(2) && player.weapons.Any(w => w is XBuster);
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
			!stingActive && canAffordFgMove() && 
			hadoukenCooldownTime == 0 && player.weapon is XBuster && 
			player.fgMoveAmmo >= player.fgMoveMaxAmmo && grounded;
	}

	public bool shouldDrawFgCooldown() {
		return 
			!isInvulnerableAttack() && 
			chargedRollingShieldProj == null && 
			!stingActive && canAffordFgMove() && 
			hadoukenCooldownTime == 0;
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
					LhHeadbutt.netWeapon, centerPoint, ProjIds.Headbutt, player,
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
		shotgunIceChargeTime = 0;
		strikeChainProj?.destroySelf();
		strikeChainChargedProj?.destroySelf();
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
		} else {
			if (newState.shootSprite != null && sprite.name != getSprite(newState.shootSprite) && hasBusterProj()) {
				changeSpriteFromName(newState.shootSprite, false);
			}
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
		if (index == (int)WeaponIds.HyperCharge && ownedByLocalPlayer) {
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
			if (weapon is GigaCrush or NovaStrike or HyperCharge &&
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

		if (isHyperX) {
			player.weapon.addAmmo(player.weapon.getAmmoUsage(0) * 0.625f * Global.spf, player);
		}
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
		return player.weapon is HyperCharge hb && hb.canShootIncludeCooldown(player) || flag != null;
	}

	public override bool isInvulnerable(bool ignoreRideArmorHide = false, bool factorHyperMode = false) {
		bool invul = base.isInvulnerable(ignoreRideArmorHide, factorHyperMode);
		if (stingActive) {
			return !factorHyperMode;
		}
		return invul;
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();

		int weaponIndex = player.weapon.index;
		if (weaponIndex == (int)WeaponIds.HyperCharge) {
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
