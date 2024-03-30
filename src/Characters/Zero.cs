using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class Zero : Character {
	public float rakuhouhaCooldown {
		get => zeroGigaAttackWeapon.shootTime;
	}
	public float ryuenjinCooldown {
		get => Math.Max(zeroUppercutWeaponA.shootTime, zeroUppercutWeaponS.shootTime);
	}
	public float hyouretsuzanCooldown {
		get => Math.Max(zeroDownThrustWeaponA.shootTime, zeroDownThrustWeaponS.shootTime);
	}

	public float dashAttackCooldown;
	public float maxDashAttackCooldown = 0.75f;
	public float airAttackCooldown;
	public float maxAirAttackCooldown = 0.5f;

	public float genmuCooldown;

	public float zSaberShotCooldown;
	public float maxZSaberShotCooldown = 0.33f;
	public float knuckleSoundCooldown;

	public float maxHyperZeroTime = 12;
	public float blackZeroTime;
	public float awakenedZeroTime;
	public bool hyperZeroUsed;
	public bool isNightmareZero;
	//public ShaderWrapper zeroPaletteShader;
	//public ShaderWrapper nightmareZeroShader;
	public int quakeBlazerBounces;
	public float zero3SwingComboStartTime;
	public float zero3SwingComboEndTime;
	public float hyorogaCooldown = 0;
	public const float maxHyorogaCooldown = 1f;
	public float zeroLemonCooldown;
	public bool doubleBusterDone;

	public ZSaber zSaberWeapon;
	public KKnuckleWeapon kKnuckleWeapon;
	public ZeroBuster zeroBusterWeapon;
	public ZSaberProjSwing zSaberProjSwingWeapon;
	public ShippuugaWeapon shippuugaWeapon;
	public Weapon raijingekiWeapon;
	public Raijingeki2Weapon raijingeki2Weapon;
	public ShinMessenkou zeroShinMessenkouWeapon;
	public AwakenedAura awakenedAuraWeapon;
	public DarkHoldWeapon zeroDarkHoldWeapon;
	public Weapon zeroAirSpecialWeapon;
	public Weapon zeroUppercutWeaponA;
	public Weapon zeroUppercutWeaponS;
	public Weapon zeroDownThrustWeaponA;
	public Weapon zeroDownThrustWeaponS;
	private Weapon _zeroGigaAttackWeapon;
	public Weapon zeroGigaAttackWeapon {
		get {
			if (isAwakenedZero() == true) { return zeroShinMessenkouWeapon; }
			if (isNightmareZeroBS.getValue() == true) { return zeroDarkHoldWeapon; }
			return _zeroGigaAttackWeapon;
		}
		set {
			_zeroGigaAttackWeapon = value;
		}
	}
	public int zeroHyperMode;

	public Zero(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		zSaberWeapon = new ZSaber(player);
		kKnuckleWeapon = new KKnuckleWeapon(player);
		shippuugaWeapon = new ShippuugaWeapon(player);
		raijingeki2Weapon = new Raijingeki2Weapon(player);
		zeroShinMessenkouWeapon = new ShinMessenkou(player);
		zeroDarkHoldWeapon = new DarkHoldWeapon();
		zeroBusterWeapon = new ZeroBuster();
		zSaberProjSwingWeapon = new ZSaberProjSwing(player);
		awakenedAuraWeapon = new AwakenedAura(player);

		var zeroLoadout = player.loadout.zeroLoadout;

		if (!player.hasKnuckle()) {
			raijingekiWeapon = RaijingekiWeapon.getWeaponFromIndex(player, zeroLoadout.groundSpecial);
			zeroAirSpecialWeapon = KuuenzanWeapon.getWeaponFromIndex(player, zeroLoadout.airSpecial);
			zeroUppercutWeaponA = RyuenjinWeapon.getWeaponFromIndex(player, zeroLoadout.uppercutA);
			zeroUppercutWeaponS = RyuenjinWeapon.getWeaponFromIndex(player, zeroLoadout.uppercutS);
			zeroDownThrustWeaponA = HyouretsuzanWeapon.getWeaponFromIndex(player, zeroLoadout.downThrustA);
			zeroDownThrustWeaponS = HyouretsuzanWeapon.getWeaponFromIndex(player, zeroLoadout.downThrustS);
		} else {
			raijingekiWeapon = new MegaPunchWeapon(player);
			zeroAirSpecialWeapon = KuuenzanWeapon.getWeaponFromIndex(player, zeroLoadout.airSpecial);
			zeroUppercutWeaponA = new ZeroShoryukenWeapon(player);
			zeroUppercutWeaponS = new ZeroShoryukenWeapon(player);
			zeroDownThrustWeaponA = new DropKickWeapon(player);
			zeroDownThrustWeaponS = new DropKickWeapon(player);
		}

		_zeroGigaAttackWeapon = RakuhouhaWeapon.getWeaponFromIndex(player, zeroLoadout.gigaAttack);
		zeroHyperMode = zeroLoadout.hyperMode;
	}

	public override bool isAttacking() {
		return (
			sprite.name.Contains("attack") ||
			sprite.name.Contains("zero_hyouretsuzan") ||
			sprite.name.Contains("zero_raijingeki") ||
			sprite.name.Contains("zero_ryuenjin") ||
			sprite.name.Contains("zero_rakukojin") ||
			sprite.name.Contains("zero_raijingeki2") ||
			sprite.name.Contains("zero_eblade") ||
			sprite.name.Contains("zero_rising") ||
			sprite.name.Contains("zero_quakeblazer") ||
			sprite.name.Contains("zero_genmu") ||
			sprite.name.Contains("zero_projswing") ||
			sprite.name.Contains("zero_tbreaker") ||
			sprite.name.Contains("zero_spear") ||
			sprite.name.Contains("punch") ||
			sprite.name.Contains("zero_kick_air") ||
			sprite.name.Contains("zero_dropkick")
		);
	}

	public override void update() {
		base.update();

		if (awakenedZeroTime > 0) {
			updateAwakenedZero();
		}
		if (isAwakenedZeroBS.getValue()) {
			updateAwakenedAura();
		}
		Helpers.decrementTime(ref blackZeroTime);
		if (!Global.level.is1v1()) {
			if (isBlackZero()) {
				if (musicSource == null) {
					addMusicSource("blackzero", getCenterPos(), true);
				}
			} else {
				destroyMusicSource();
			}
		}
		if (!ownedByLocalPlayer) {
			return;
		}

		// All code here bellow is only executed by local players.
		raijingekiWeapon.update();
		zeroAirSpecialWeapon.update();
		zeroUppercutWeaponA.update();
		zeroUppercutWeaponS.update();
		zeroDownThrustWeaponA.update();
		zeroDownThrustWeaponS.update();
		zeroGigaAttackWeapon.update();

		if (dashAttackCooldown > 0) dashAttackCooldown = Helpers.clampMin0(dashAttackCooldown - Global.spf);
		if (airAttackCooldown > 0) airAttackCooldown = Helpers.clampMin0(airAttackCooldown - Global.spf);
		Helpers.decrementTime(ref saberCooldown);
		Helpers.decrementTime(ref xSaberCooldown);
		Helpers.decrementTime(ref genmuCooldown);
		Helpers.decrementTime(ref zSaberShotCooldown);
		Helpers.decrementTime(ref knuckleSoundCooldown);
		Helpers.decrementTime(ref hyorogaCooldown);

		if (shootAnimTime > 0) {
			shootAnimTime -= Global.spf;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				changeSpriteFromName(charState.defaultSprite, false);
				if (charState is WallSlide) {
					frameIndex = sprite.frames.Count - 1;
				}
			}
		}

		Weapon gigaWeapon = zeroGigaAttackWeapon;
		if (gigaWeapon.ammo >= gigaWeapon.maxAmmo) {
			weaponHealAmount = 0;
		}
		if (weaponHealAmount > 0 && player.health > 0) {
			weaponHealTime += Global.spf;
			if (weaponHealTime > 0.05) {
				weaponHealTime = 0;
				weaponHealAmount--;
				gigaWeapon.ammo = Helpers.clampMax(gigaWeapon.ammo + 1, gigaWeapon.maxAmmo);
				playSound("heal", forcePlay: true);
			}
		}

		bool attackPressed = false;
		if (player.weapon is not AssassinBullet) {
			if (player.input.isPressed(Control.Shoot, player)) {
				attackPressed = true;
				lastAttackFrame = Global.level.frameCount;
			}
		}
		framesSinceLastAttack = Global.level.frameCount - lastAttackFrame;

		if (chargeButtonHeld() && (
			player.currency > 0 || player.isZBusterZero() || player.weapon is AssassinBullet
			) && flag == null && rideChaser == null && rideArmor == null
		) {
			if (!stockedXSaber && !isInvulnerableAttack()) {
				increaseCharge();
			}
		} else {
			int chargeLevel = getChargeLevel();
			if (isCharging()) {
				if (player.weapon is AssassinBullet) {
					shootAssassinShot();
				} else if (chargeLevel == 1) {
					zeroShoot(1);
				} else if (chargeLevel == 2) {
					zeroShoot(2);
				} else if (chargeLevel >= 3) {
					zeroShoot(chargeLevel);
				}
			}
			stopCharge();
		}
		chargeLogic();

		Helpers.decrementTime(ref zeroLemonCooldown);

		// Handles Standard Hypermode Activations.
		if (player.currency >= Player.zeroHyperCost &&
			player.input.isHeld(Control.Special2, player) &&
			charState is not HyperZeroStart and not WarpIn && (
				!isNightmareZero &&
				!isAwakenedZero() &&
				!isBlackZero() &&
				!isBlackZero2()
			)
		) {
			if (!player.isZBusterZero()) {
				hyperProgress += Global.spf;
			}
		} else {
			hyperProgress = 0;
		}
		if (hyperProgress >= 1 && player.currency >= Player.zeroHyperCost) {
			hyperProgress = 0;
			changeState(new HyperZeroStart(zeroHyperMode), true);
		}
		if (player.currency < Player.zeroHyperCost && player.input.isHeld(Control.Special2, player)) {
			Global.level.gameMode.setHUDErrorMessage(
				player, Player.zeroHyperCost + " " + Global.nameCoins + " needed to enter hypermode.",
				playSound: false, resetCooldown: true
			);
		}

		if (player.isZBusterZero()) {
			if (charState.canShoot() && !isCharging()) {
				var shootPressed = player.input.isPressed(Control.Shoot, player);
				if (shootPressed) {
					lastShootPressed = Global.frameCount;
				}

				int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
				int framesSinceLastShootReleased = Global.frameCount - lastShootReleased;
				var shootHeld = player.input.isHeld(Control.Shoot, player);

				if (shootPressed || (framesSinceLastShootPressed < Global.normalizeFrames(6) && framesSinceLastShootReleased > Global.normalizeFrames(30))) {
					if (stockedXSaber) {
						if (!doubleBusterDone) {
							changeState(new ZeroDoubleBuster(true, false), true);
						} else if (xSaberCooldown == 0) {
							swingStockedSaber();
						}
						return;
					}

					if (zeroLemonCooldown == 0) {
						zeroLemonCooldown = 0.15f;
						zeroShoot(0);
						return;
					}
				}
			}

			if (player.input.isPressed(Control.Special1, player)) {
				if (charState.canAttack()) {
					if (xSaberCooldown == 0) {
						xSaberCooldown = 1;
						if (stockedXSaber) {
							swingStockedSaber();
						} else {
							changeState(new ZSaberProjSwingState(grounded, false), true);
						}
					}
				}
			}

			// Handles ZBusterZero's Hyper activations.
			if (player.input.isHeld(Control.Special2, player) &&
				player.currency >= Player.zBusterZeroHyperCost && !isBlackZero2() &&
				charState is not HyperZeroStart && invulnTime == 0 &&
				rideChaser == null && rideArmor == null && charState is not WarpIn) {
				hyperProgress += Global.spf;
			} else {
				hyperProgress = 0;
			}

			if (hyperProgress >= 1 && player.currency >= Player.zBusterZeroHyperCost &&
				!isBlackZero2()) {
				hyperProgress = 0;
				changeState(new HyperZeroStart(0), true);
			}

			if (player.currency < Player.zBusterZeroHyperCost && player.input.isHeld(Control.Special2, player)) {
				Global.level.gameMode.setHUDErrorMessage(
					player,
					Player.zBusterZeroHyperCost + " " + Global.nameCoins + " needed to enter hypermode.",
					playSound: false, resetCooldown: true
				);
			}
			return;
		}

		// Cutoff point for non-zero buster loadouts
		bool lenientAttackPressed = (attackPressed || framesSinceLastAttack < 5);
		bool spcPressed = player.input.isPressed(Control.Special1, player);
		bool spcActivated = false;

		if (hyorogaCooldown > 0 && charState is HyorogaState) {
			lenientAttackPressed = false;
		}

		if (lenientAttackPressed && charState is HyorogaState) {
			hyorogaCooldown = maxHyorogaCooldown;
		}

		if (player.isDisguisedAxl && player.axlWeapon is UndisguiseWeapon) {
			lenientAttackPressed = false;
			spcPressed = false;
		}

		bool isMidairRising = ((
				(lenientAttackPressed && zeroUppercutWeaponA is RisingWeapon) ||
				(spcPressed && zeroUppercutWeaponS is RisingWeapon)
			) &&
			canAirDash() &&
			flag == null
		);

		bool notUpLogic = !player.input.isHeld(Control.Up, player) || !isMidairRising;
		if (zeroAirSpecialWeapon.type != (int)AirSpecialType.Kuuenzan && spcPressed && !player.input.isHeld(Control.Down, player) && !isAttacking() && !player.hasKnuckle() &&
			(charState is Jump || charState is Fall || charState is WallKick) && !isInvulnerableAttack() &&
			(zeroUppercutWeaponS.type != (int)RyuenjinType.Rising || !player.input.isHeld(Control.Up, player))) {
			zeroAirSpecialWeapon.attack(this);
		} else if (lenientAttackPressed && !player.hasKnuckle() && charState.canAttack() && !isAttacking() && notUpLogic && !player.input.isHeld(Control.Down, player) &&
			  (charState is Idle || charState is Crouch || charState is Run || charState is Dash || charState is AirDash || charState is Jump || charState is Fall)) {
			if (stockedXSaber) {
				if (xSaberCooldown == 0) {
					xSaberCooldown = 1;
					stockSaber(false);
					changeState(new ZSaberProjSwingState(grounded, true), true);
				}
				return;
			} else if ((charState is Idle || charState is Crouch || charState is Run || charState is Dash) && isAwakenedGenmuZero()) {
				if (genmuCooldown == 0 && xSaberCooldown < 0.5f) {
					genmuCooldown = 2;
					changeState(new GenmuState(), true);
				}
				return;
			} else if (isAwakenedZero()) {
				if (xSaberCooldown == 0 && genmuCooldown < 1) {
					xSaberCooldown = 1f;
					changeState(new ZSaberProjSwingState(grounded, true), true);
				}
				return;
			}
		}

		if (charState.canAttack() &&
			charState is Idle && !player.hasKnuckle() &&
			((sprite.name == "zero_attack" && framePercent > 0.7f) ||
			(sprite.name == "zero_attack2" && framePercent > 0.55f)) &&
			spcPressed &&
			!player.input.isHeld(Control.Up, player) &&
			!player.input.isHeld(Control.Down, player)
		) {
			raijingekiWeapon.attack2(this);
		} else if (charState.canAttack() && charState is Idle && player.hasKnuckle() && ((sprite.name == "zero_punch" && framePercent > 0.6f) || (sprite.name == "zero_punch2" && framePercent > 0.6f)) && spcPressed &&
				!player.input.isHeld(Control.Up, player) && !player.input.isHeld(Control.Down, player)) {
			raijingekiWeapon.attack2(this);
		} else if (charState.canAttack() && (spcPressed || lenientAttackPressed) && !isAttacking()) {
			if (charState is Idle || charState is Run || charState is Crouch) {
				if (player.input.isHeld(Control.Up, player)) {
					spcActivated = true;
					if (ryuenjinCooldown <= 0) {
						changeState(
							new ZeroUppercut(
								lenientAttackPressed ? zeroUppercutWeaponA : zeroUppercutWeaponS,
								isUnderwater()
							),
							true
						);
					}
				} else if (player.input.isHeld(Control.Down, player)) {
					if (spcPressed && rakuhouhaCooldown == 0 && flag == null) {
						spcActivated = true;

						float gigaAmmoUsage = gigaWeapon.getAmmoUsage(0);
						if (gigaWeapon.ammo >= gigaAmmoUsage) {
							zeroGigaAttackWeapon.addAmmo(-gigaAmmoUsage, player);
							if (zeroGigaAttackWeapon is RekkohaWeapon) {
								changeState(new Rekkoha(zeroGigaAttackWeapon), true);
							} else {
								changeState(new Rakuhouha(zeroGigaAttackWeapon), true);
							}
						}
					}
				} else {
					if (!lenientAttackPressed) {
						raijingekiWeapon.attack(this);
					}
				}
			} else if ((charState is Jump || charState is Fall || charState is WallKick)) {
				if (player.input.isHeld(Control.Up, player) && isMidairRising) {
					spcActivated = true;
					if (ryuenjinCooldown <= 0) {
						changeState(new ZeroUppercut(zeroUppercutWeaponA is RisingWeapon ? zeroUppercutWeaponA : zeroUppercutWeaponS, isUnderwater()), true);
					}
				} else if (player.input.isHeld(Control.Down, player)) {
					spcActivated = true;
					if (hyouretsuzanCooldown <= 0) {
						if (!player.hasKnuckle()) changeState(new ZeroFallStab(lenientAttackPressed ? zeroDownThrustWeaponA : zeroDownThrustWeaponS), true);
						else changeState(new DropKickState(), true);
					}
				} else if ((Options.main.swapAirAttacks || airAttackCooldown == 0) && !lenientAttackPressed && !player.hasKnuckle()) {
					if (zeroAirSpecialWeapon.type == (int)AirSpecialType.Kuuenzan || !spcPressed) {
						playSound("saber1", sendRpc: true);
						changeState(new Fall(), true);
						changeSprite(Options.main.getSpecialAirAttack(), true);
						if (!Options.main.swapAirAttacks) airAttackCooldown = maxAirAttackCooldown;
					}
				}

			} else if (charState is Dash && !lenientAttackPressed) {
				if (!player.hasKnuckle()) {
					if (dashAttackCooldown > 0) return;
					dashAttackCooldown = maxDashAttackCooldown;
					slideVel = xDir * getDashSpeed();
					changeState(new Idle(), true);
					playSound("saber1", sendRpc: true);
					changeSprite("zero_attack_dash2", true);
				} else {
					if (dashAttackCooldown > 0) return;
					changeState(new ZeroSpinKickState(), true);
					return;
				}
			}
		}

		if (charState.canAttack() && !spcActivated && lenientAttackPressed) {
			if (!isAttacking()) {
				if (charState is LadderClimb) {
					if (player.input.isHeld(Control.Left, player)) {
						xDir = -1;
					} else if (player.input.isHeld(Control.Right, player)) {
						xDir = 1;
					}
				}
				var attackSprite = charState.attackSprite;
				if (player.hasKnuckle()) {
					attackSprite = attackSprite.Replace("attack", "punch");
					string attackSound = "punch1";
					if (charState is Jump || charState is Fall || charState is WallKick) attackSprite = "kick_air";
					if (charState is Dash) {
						if (dashAttackCooldown > 0) return;
						changeState(new ZeroSpinKickState(), true);
						return;
					}
					if (charState is Crouch) {
						return;
					}
					if (Global.spriteNames.Contains(getSprite(attackSprite))) {
						playSound(attackSound, sendRpc: true);
					}
					if (charState is Run) changeState(new Idle(), true);
					else if (charState is Jump) changeState(new Fall(), true);
				} else {
					if (charState is Run) changeState(new Idle(), true);
					else if (charState is Jump) changeState(new Fall(), true);
					else if (charState is Dash) {
						if (dashAttackCooldown > 0) return;
						dashAttackCooldown = maxDashAttackCooldown;
						slideVel = xDir * getDashSpeed();
						changeState(new Idle(), true);
					}
					if (charState is Fall) {
						if (Options.main.swapAirAttacks) {
							if (airAttackCooldown > 0) return;
							airAttackCooldown = maxAirAttackCooldown;
						}
					}
				}
				changeSprite(getSprite(attackSprite), true);

				if (!player.hasKnuckle()) {
					if (stockedXSaber || isAwakenedZero()) {
						stockSaber(false);
						playSound("saberShot", sendRpc: true);
						if (zSaberShotCooldown == 0) {
							zSaberShotCooldown = maxZSaberShotCooldown;
							Global.level.delayedActions.Add(new DelayedAction(() => {
								new ZSaberProj(new ZSaber(player), pos.addxy(30 * getShootXDir(), -20), getShootXDir(), player, player.getNextActorNetId(), rpc: true);
							}, 0.1f));
						}
					} else {
						playSound("saber1", sendRpc: true);
					}
				}
			} else if (charState is Idle && sprite.name == "zero_attack" && framePercent > 0.4f) {
				playSound("saber2", sendRpc: true);
				changeSprite("zero_attack2", true);
				turnToInput(player.input, player);
			} else if (charState is Idle && sprite.name == "zero_attack2" && framePercent > 0.4f) {
				playSound("saber3", sendRpc: true);
				changeSprite("zero_attack3", true);
				turnToInput(player.input, player);
			} else if (charState is Idle && sprite.name == "zero_punch" && framePercent > 0.4f) {
				changeSprite("zero_punch2", true);
				playSound("punch2");
				turnToInput(player.input, player);
			}
		}

		if (isAttacking()) {
			if (isAnimOver() && charState is not ZSaberProjSwingState) {
				changeSprite(getSprite(charState.defaultSprite), true);
				if (charState is WallSlide) {
					frameIndex = sprite.frames.Count - 1;
				}
			}
		}
	}

	public override bool normalCtrl() {
		bool changedState = base.normalCtrl();
		if (changedState) {
			return true;
		}
		if (charState is not Dash && grounded && !isAttacking() && player.isZSaber() && (
				player.input.isHeld(Control.WeaponLeft, player) ||
				player.input.isHeld(Control.WeaponRight, player)
			) && (
				!player.isDisguisedAxl ||
				player.input.isHeld(Control.Down, player)
			)
		) {
			turnToInput(player.input, player);
			changeState(new SwordBlock());
			return true;
		} else if (!player.isZBusterZero() && !isDashing && (
				  player.input.isPressed(Control.WeaponLeft, player) ||
				  player.input.isPressed(Control.WeaponRight, player)
			  ) && (
				  !player.isDisguisedAxl || player.input.isHeld(Control.Down, player)
			  )
		  ) {
			if (!player.hasKnuckle()) {
				if (grounded && !isAttacking()){
				turnToInput(player.input, player);
				changeState(new SwordBlock());}
				return true;
			} else if (parryCooldown == 0) {
				changeState(new KKnuckleParryStartState());
				return true;
			}
		}
		return false;
	}

	public void swingStockedSaber() {
		xSaberCooldown = 1;
		doubleBusterDone = false;
		stockSaber(false);
		changeState(new ZSaberProjSwingState(grounded, true), true);
	}

	// This can run on both owners and non-owners. So data used must be in sync
	public override Projectile? getProjFromHitbox(Collider collider, Point centerPoint) {
		if (sprite.name == "zero_attack3") {
			float timeSinceStart = zero3SwingComboEndTime - zero3SwingComboStartTime;
			float overrideDamage = 4;
			int overrideFlinch = Global.defFlinch;
			if (timeSinceStart < 0.4f) {
				overrideDamage = 2;
				overrideFlinch = Global.halfFlinch;
			} else if (timeSinceStart < 0.5f) {
				overrideDamage = 3;
				overrideFlinch = Global.defFlinch;
			}
			return new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.ZSaber3, player,
				overrideDamage, overrideFlinch, 0.25f, isReflectShield: true
			);
		} else if (sprite.name.Contains("hyouretsuzan")) {
			return new GenericMeleeProj(
				new HyouretsuzanWeapon(player), centerPoint, ProjIds.Hyouretsuzan2, player, 4, 12, 0.5f
			);
		} else if (sprite.name.Contains("rakukojin")) {
			float damage = 3 + Helpers.clamp(MathF.Floor(deltaPos.y * 0.8f), 0, 10);
			return new GenericMeleeProj(
				new RakukojinWeapon(player), centerPoint, ProjIds.Rakukojin, player, damage, 12, 0.5f
			);
		} else if (sprite.name.Contains("quakeblazer")) {
			return new GenericMeleeProj(
				new QuakeBlazerWeapon(player), centerPoint, ProjIds.QuakeBlazer, player, 2, 0, 0.5f
			);
		} else if (sprite.name.Contains("zero_projswing")) {
			return new GenericMeleeProj(
				zSaberProjSwingWeapon, centerPoint, ProjIds.ZSaberProjSwing, player,
				isBlackZero2() ? 4 : 3, Global.defFlinch, 0.5f, isReflectShield: true
			);
		} else if (sprite.name.Contains("zero_block") && !collider.isHurtBox()) {
			return new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.SwordBlock, player, 0, 0, 0, isDeflectShield: true
			);
		}
		Projectile? proj = sprite.name switch {
			"zero_punch" => new GenericMeleeProj(
				kKnuckleWeapon, centerPoint, ProjIds.KKnuckle, player, 2, 0, 0.25f
			),
			"zero_punch2" => new GenericMeleeProj(
				kKnuckleWeapon, centerPoint, ProjIds.KKnuckle2, player, 2, Global.halfFlinch, 0.25f
			),
			"zero_spinkick" => new GenericMeleeProj(
				kKnuckleWeapon, centerPoint, ProjIds.KKnuckle2, player, 2, Global.halfFlinch, 0.5f
			),
			"zero_kick_air" => new GenericMeleeProj(
				kKnuckleWeapon, centerPoint, ProjIds.KKnuckleAirKick, player, 3, 0, 0.25f
			),
			"zero_parry_start" => new GenericMeleeProj(
				new KKnuckleParry(), centerPoint, ProjIds.KKnuckleParryStart, player, 0, Global.defFlinch, 0.25f
			),
			"zero_parry" => new GenericMeleeProj(
				new KKnuckleParry(), centerPoint, ProjIds.KKnuckleParry, player, 4, Global.defFlinch, 0.25f
			),
			"zero_shoryuken" => new GenericMeleeProj(
				zeroUppercutWeaponA, centerPoint, ProjIds.KKnuckleShoryuken, player, 4, Global.defFlinch, 0.25f
			),
			"zero_megapunch" => new GenericMeleeProj(
				raijingekiWeapon, centerPoint, ProjIds.KKnuckleMegaPunch, player, 6, Global.defFlinch, 0.25f
			),
			"zero_dropkick" => new GenericMeleeProj(
				zeroDownThrustWeaponA, centerPoint, ProjIds.KKnuckleDropKick, player, 4, Global.halfFlinch, 0.25f
			),
			_ => null
		};
		if (proj != null) {
			return proj;
		}

		proj = sprite.name switch {
			"zero_attack" => new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.ZSaber1, player, 2, 0, 0.25f, isReflectShield: true
			),
			"zero_attack2" => new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.ZSaber2, player, 2, 0, 0.25f, isReflectShield: true
			),
			"zero_attack3" => new GenericMeleeProj(
					zSaberWeapon, centerPoint, ProjIds.ZSaber2, player, 2, 0, 0.25f, isReflectShield: true
			),
			"zero_hyoroga_attack" => new GenericMeleeProj(
				zeroAirSpecialWeapon, centerPoint, ProjIds.HyorogaSwing, player, 4, 0, 0.25f
			),
			"zero_attack_dash" => new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.ZSaberdash, player, 2, 0, 0.25f, isReflectShield: true
			),
			"zero_attack_dash2" => new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.Shippuuga, player, 2, Global.halfFlinch, 0.25f
			),
			"zero_attack_air" => new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.ZSaberair, player, 2, 0, 0.25f, isReflectShield: true
			),
			"zero_attack_air2" => new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.ZSaberair, player, 1, 0, 0.125f, isDeflectShield: true
			),
			"zero_ladder_attack" => new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.ZSaberladder, player, 3, 0, 0.25f, isReflectShield: true
			),
			"zero_wall_slide_attack" => new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.ZSaberslide, player, 3, 0, 0.25f, isReflectShield: true
			),
			"zero_attack_crouch" => new GenericMeleeProj(
				zSaberWeapon, centerPoint, ProjIds.ZSabercrouch, player, 3, 0, 0.25f, isReflectShield: true
			),
			"zero_raijingeki" => new GenericMeleeProj(
				raijingekiWeapon, centerPoint, ProjIds.Raijingeki, player, 2, Global.defFlinch, 0.06f
			),
			"zero_raijingeki2" => new GenericMeleeProj(
				raijingeki2Weapon, centerPoint, ProjIds.Raijingeki2, player, 2, Global.defFlinch, 0.06f
			),
			"zero_tbreaker" => new GenericMeleeProj(
				raijingekiWeapon, centerPoint, ProjIds.TBreaker, player, 6, Global.defFlinch, 0.5f
			),
			"zero_ryuenjin" => new GenericMeleeProj(
				new RyuenjinWeapon(player), centerPoint, ProjIds.Ryuenjin, player, 4, 0, 0.2f
			),
			"zero_eblade" => new GenericMeleeProj(
				new EBladeWeapon(player), centerPoint, ProjIds.EBlade, player, 3, Global.defFlinch, 0.1f
			),
			"zero_rising" => new GenericMeleeProj(
				new RisingWeapon(player), centerPoint, ProjIds.Rising, player, 1, 0, 0.15f
			),
			_ => null
		};

		return proj;
	}

	public List<BusterProj> zeroLemonsOnField = new List<BusterProj>();
	private void zeroShoot(int chargeLevel) {
		if (!player.isZBusterZero() && player.currency <= 0) return;

		if (player.isZBusterZero() && chargeLevel == 0) {
			for (int i = zeroLemonsOnField.Count - 1; i >= 0; i--) {
				if (zeroLemonsOnField[i].destroyed) {
					zeroLemonsOnField.RemoveAt(i);
					continue;
				}
			}
			if (zeroLemonsOnField.Count >= 3) return;
		}

		string zeroShootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(zeroShootSprite)) {
			if (grounded) zeroShootSprite = "zero_shoot";
			else zeroShootSprite = "zero_fall_shoot";
		}
		// Zero MMXOD Vanilla intended balance: Z-Buster Cancel on Zerofallstabland
		if (shootAnimTime == 0f && (		
			charState is ZeroFallStabLand 
		)) {
			changeToIdleOrFall();
		}
		bool hasShootSprite = !string.IsNullOrEmpty(charState.shootSprite);
		if (shootAnimTime == 0) {
			if (hasShootSprite) {
				changeSprite(zeroShootSprite, false);
			} else {
				if (charState is not Crouch) {
					return;
				}
			}
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

		//Sometimes transitions cause the shoot sprite not to be played immediately, so force it here
		if (currentFrame.getBusterOffset() == null) {
			if (hasShootSprite) changeSprite(zeroShootSprite, false);
		}

		if (hasShootSprite) shootAnimTime = 0.3f;
		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		if (isAwakenedZero() && !player.isZBusterZero()) {
			if (chargeLevel == 1) {
				new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0, player, player.getNextActorNetId(), rpc: true);
				playSound("ShingetsurinX5", forcePlay: false, sendRpc: true);
			}
			if (chargeLevel == 2) {
				playSound("ShingetsurinX5", forcePlay: false, sendRpc: true);
				new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0, player, player.getNextActorNetId(), rpc: true);
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0.15f, player, player.getNextActorNetId(), rpc: true);
					playSound("ShingetsurinX5", forcePlay: false, sendRpc: true);
				}, 0.15f));
			}
			if (chargeLevel == 3) {
				playSound("ShingetsurinX5", forcePlay: false, sendRpc: true);
				new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0, player, player.getNextActorNetId(), rpc: true);
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0.15f, player, player.getNextActorNetId(), rpc: true);
					playSound("ShingetsurinX5", forcePlay: false, sendRpc: true);
				}, 0.15f));
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0.3f, player, player.getNextActorNetId(), rpc: true);
					playSound("ShingetsurinX5", forcePlay: false, sendRpc: true);
				}, 0.3f));
			}
			if (chargeLevel == 4) {
				playSound("ShingetsurinX5", forcePlay: false, sendRpc: true);
				new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0, player, player.getNextActorNetId(), rpc: true);
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0.15f, player, player.getNextActorNetId(), rpc: true);
					playSound("ShingetsurinX5", forcePlay: false, sendRpc: true);
				}, 0.15f));
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0.3f, player, player.getNextActorNetId(), rpc: true);
					playSound("ShingetsurinX5", forcePlay: false, sendRpc: true);
				}, 0.3f));
			}
			if (!player.isZBusterZero() || !player.isAI) {
				player.currency--;
			}
			if (player.currency < 0) {
				player.currency = 0;
			}
		} else {
			int type = player.isZBusterZero() ? 1 : 0;

			if (stockedCharge) {
				changeState(new ZeroDoubleBuster(true, true), true);
			} else if (chargeLevel == 0) {
				playSound("busterX3", sendRpc: true);
				zeroLemonCooldown = 0.15f;
				var lemon = new BusterProj(
					zeroBusterWeapon, shootPos, xDir, 1, player, player.getNextActorNetId(), rpc: true
				);
				zeroLemonsOnField.Add(lemon);
			} else if (chargeLevel == 1) {
				if (!player.isZBusterZero()) {
					if (type == 0) player.currency -= 1;
					playSound("buster2", sendRpc: true);
					zeroLemonCooldown = 0.375f;
					new ZBuster2Proj(
						zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true
					);
				}
				if (type == 1) {
					playSound("buster2X3", sendRpc: true);
					zeroLemonCooldown = 0.375f;
					new ZBuster2Proj(
						zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true
					);
				}
			} else if (chargeLevel == 2) {
				if (type == 0) player.currency -= 1;
				zeroLemonCooldown = 0.375f;
				if (!player.isZBusterZero()) {
					playSound("buster3", sendRpc: true);
					new ZBuster3Proj(
						zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true
					);
				} else {
					playSound("buster3X3", sendRpc: true);
					new ZBuster4Proj(
						zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true
					);
				}
			} else if (chargeLevel == 3 || chargeLevel >= 4) {
				if (type == 0) player.currency -= 1;
				if (chargeLevel == 3 && player.isZBusterZero()) {
					changeState(new ZeroDoubleBuster(false, true), true);
					//playSound("zbuster2", sendRpc: true);
					//new ZBuster2Proj(player.zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true);
					//stockedCharge = true;
				} else if (chargeLevel >= 4 && canUseDoubleBusterCombo()) {
					//if (!isBlackZero2()) player.currency -= 1;
					changeState(new ZeroDoubleBuster(false, false), true);
				} else {
					playSound("buster4", sendRpc: true);
					zeroLemonCooldown = 0.375f;
					new ZBuster4Proj(
						zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true
					);
				}
			}
		}

		chargeTime = 0;
		//saberCooldown = 0.5f;
	}

	public bool canUseDoubleBusterCombo() {
		//if (isBlackZero()) return true;
		if (player.isZBusterZero()) return true;
		return false;
	}

	public bool isCrouchSlashing() {
		return charState is Crouch && isAttacking();
	}

	public bool isHyperZero() {
		return isBlackZero() || isAwakenedZeroBS.getValue();
	}

	// isBlackZero below can be used by non-owners as these times are sync'd
	public bool isBlackZero() {
		return blackZeroTime > 0 && !player.isZBusterZero();
	}

	public bool isBlackZero2() {
		return blackZeroTime > 0 && player.isZBusterZero();
	}

	// These methods below can't be used by non-owners of the character since the times aren't sync'd. Use the BS states instead
	public bool isAwakenedZero() {
		return awakenedZeroTime > 0;
	}

	public bool isAwakenedGenmuZero() {
		return awakenedZeroTime > 30;
	}

	float awakenedCurrencyTime;
	float awakenedAuraAnimTime;
	public int awakenedAuraFrame;
	public void updateAwakenedZero() {
		awakenedZeroTime += Global.spf;
		awakenedCurrencyTime += Global.spf;
		int currencyDeductTime = 2;
		if (isAwakenedGenmuZero()) currencyDeductTime = 1;

		if (awakenedCurrencyTime > currencyDeductTime) {
			awakenedCurrencyTime = 0;
			player.currency--;
			if (player.currency <= 0) {
				player.currency = 0;
				awakenedZeroTime = 0;
			}
		}

		updateAwakenedAura();
	}

	int lastAwakenedAuraFrameUpdate;
	public void updateAwakenedAura() {
		if (lastAwakenedAuraFrameUpdate == Global.frameCount) return;
		lastAwakenedAuraFrameUpdate = Global.frameCount;
		awakenedAuraAnimTime += Global.spf;
		if (awakenedAuraAnimTime > 0.06f) {
			awakenedAuraAnimTime = 0;
			awakenedAuraFrame++;
			if (awakenedAuraFrame > 3) awakenedAuraFrame = 0;
		}
	}

	public override bool isToughGuyHyperMode() {
		return isBlackZero();
	}
	public override void increaseCharge() {
		float factor = 1;
		if (isBlackZero2()) factor = 1.5f;
		chargeTime += Global.spf * factor;
	}

	public override void changeSprite(string spriteName, bool resetFrame) {
		if (sprite != null && sprite.name != "zero_attack" && spriteName == "zero_attack") {
			zero3SwingComboStartTime = Global.time;
		}
		if (sprite != null && sprite.name != "zero_attack3" && spriteName == "zero_attack3") {
			zero3SwingComboEndTime = Global.time;
		}
		base.changeSprite(spriteName, resetFrame);
	}

	public override string getSprite(string spriteName) {
		return "zero_" + spriteName;
	}

	public override void render(float x, float y) {
		if (isNightmareZeroBS.getValue()) {
			addRenderEffect(RenderEffectType.Trail);
		} else {
			removeRenderEffect(RenderEffectType.Trail);
		}
		if (isAwakenedZeroBS.getValue() && visible) {
			float xOff = 0;
			int auraXDir = 1;
			float yOff = 5;
			string auraSprite = "zero_awakened_aura";
			if (sprite.name.Contains("dash")) {
				auraSprite = "zero_awakened_aura2";
				auraXDir = xDir;
				yOff = 8;
			}
			var shaders = new List<ShaderWrapper>();
			if (isAwakenedGenmuZeroBS.getValue() &&
				Global.frameCount % Global.normalizeFrames(6) > Global.normalizeFrames(3) &&
				Global.shaderWrappers.ContainsKey("awakened")
			) {
				shaders.Add(Global.shaderWrappers["awakened"]);
			}
			Global.sprites[auraSprite].draw(
				awakenedAuraFrame,
				pos.x + x + (xOff * auraXDir),
				pos.y + y + yOff, auraXDir,
				1, null, 1, 1, 1,
				zIndex - 1, shaders: shaders
			);
		}

		if (!hideNoShaderIcon()) {
			float dummy = 0;
			getHealthNameOffsets(out _, ref dummy);
			if (isBlackZero() && !Global.shaderWrappers.ContainsKey("hyperzero")) {
				Global.sprites["hud_killfeed_weapon"].draw(
					125, pos.x, pos.y - 6 + currentLabelY,
					1, 1, null, 1, 1, 1, ZIndex.HUD
				);
				deductLabelY(labelKillFeedIconOffY);
			} else if (isNightmareZeroBS.getValue() && player.nightmareZeroShader == null) {
				Global.sprites["hud_killfeed_weapon"].draw(
					174, pos.x, pos.y - 6 + currentLabelY,
					1, 1, null, 1, 1, 1, ZIndex.HUD
				);
				deductLabelY(labelKillFeedIconOffY);
			}
		}
		base.render(x, y);
	}

	public override Point getAimCenterPos() {
		if (sprite.name.Contains("_ra_")) {
			return pos.addxy(0, -10);
		}
		return pos.addxy(0, -21);
	}

	public override Dictionary<int, Func<Projectile>> getGlobalProjs() {
		if (player.isZero && isAwakenedZeroBS.getValue() && globalCollider != null) {
			Dictionary<int, Func<Projectile>> retProjs = new();
			retProjs[(int)ProjIds.AwakenedAura] = () => {
				//playSound("Aura", forcePlay: true, sendRpc: true); 
				Point centerPoint = globalCollider.shape.getRect().center();
				float damage = 2;
				int flinch = 0;
				if (isAwakenedGenmuZeroBS.getValue()) {
					damage = 4;
					flinch = Global.defFlinch;
				}
				Projectile proj = new GenericMeleeProj(
					awakenedAuraWeapon, centerPoint,
					ProjIds.AwakenedAura, player, damage, flinch, 0.5f
				);
				proj.globalCollider = globalCollider.clone();
				return proj;
			};
		}
		return base.getGlobalProjs();
	}

	public override float getRunSpeed() {
		float runSpeed = 90;
		if (isBlackZero() || isBlackZero2()) {
			runSpeed *= 1.15f;
		}
		return runSpeed * getRunDebuffs();
	}

	public override float getDashSpeed() {
		if (flag != null || !isDashing) {
			return getRunSpeed();
		}
		float dashSpeed = 210;

		if (isBlackZero() || isBlackZero2()) {
			dashSpeed *= 1.15f;
		}
		return dashSpeed * getRunDebuffs();
	}

	public override Collider getGlobalCollider() {
		var rect = new Rect(0, 0, 18, 40);
		if (sprite.name.Contains("_ra_")) {
			rect.y2 = 20;
		}
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

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
		if (specialState == (int)SpecialStateIds.HyorogaStart) {
			return new Collider(
				new Rect(0f, 0f, 18, 40).getPoints(),
				false, this, false, false,
				HitboxFlag.Hurtbox, new Point(0, 0)
			);
		} else if (sprite.name.Contains("hyoroga")) {
			return new Collider(
				new Rect(0f, 0f, 18, hSize).getPoints(),
				false, this, false, false,
				HitboxFlag.Hurtbox, new Point(0, 40 - hSize)
			);
		}
		return new Collider(
			new Rect(0f, 0f, 18, hSize).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, new Point(0, 0)
		);
	}

	public override Collider getDashingCollider() {
		var rect = new Rect(0, 0, 18, 26);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getCrouchingCollider() {
		var rect = new Rect(0, 0, 18, 26);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getRaCollider() {
		var rect = new Rect(0, 0, 18, 20);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getBlockCollider() {
		var rect = new Rect(0, 0, 16, 16);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public override bool canAirJump() {
		return dashedInAir == 0;
	}
}
