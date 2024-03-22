using System;
using System.Linq;

namespace MMXOnline;

public class Vile : Character {
	public float vulcanLingerTime;
	public const int callNewMechCost = 5;
	float mechBusterCooldown;
	public bool usedAmmoLastFrame;
	public float vileLadderShootCooldown;
	public int buckshotDanceNum;
	public float vileAmmoRechargeCooldown;
	public bool isShootingLongshotGizmo;
	public int longshotGizmoCount;
	public float gizmoCooldown;
	public bool hasFrozenCastleBarrier() {
		return player.frozenCastle;
	}
	public bool summonedGoliath;
	public int vileForm;
	public bool isVileMK1 { get { return vileForm == 0; } }
	public bool isVileMK2 { get { return vileForm == 1; } }
	public bool isVileMK5 { get { return vileForm == 2; } }
	public float vileHoverTime;
	public float vileMaxHoverTime = 6;

	public const decimal frozenCastlePercent = 0.125m;
	public const float speedDevilRunSpeed = 110;
	public const int frozenCastleCost = 3;
	public const int speedDevilCost = 3;
	public bool lastFrameWeaponLeftHeld;
	public bool lastFrameWeaponRightHeld;
	public int cannonAimNum;

	public Vile(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, bool mk2VileOverride = false, bool mk5VileOverride = false
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		if (isWarpIn) {
			if (mk5VileOverride) {
				vileForm = 2;
			} else if (mk2VileOverride) {
				vileForm = 1;
			}
			if (player.vileFormToRespawnAs == 2 || Global.quickStartVileMK5 == true) {
				vileForm = 2;
			} else if (player.vileFormToRespawnAs == 1 || Global.quickStartVileMK2 == true) {
				vileForm = 1;
			}
		}
	}

	public Sprite getCannonSprite(out Point poiPos, out int zIndexDir) {
		poiPos = getCenterPos();
		zIndexDir = 0;

		string vilePrefix = "vile_";
		if (isVileMK2) vilePrefix = "vilemk2_";
		if (isVileMK5) vilePrefix = "vilemk5_";
		string cannonSprite = vilePrefix + "cannon";
		for (int i = 0; i < currentFrame.POIs.Count; i++) {
			var poi = currentFrame.POIs[i];
			var tag = currentFrame.POITags[i] ?? "";
			zIndexDir = tag.EndsWith("b") ? -1 : 1;
			int? frameIndexToDraw = null;
			if (tag.StartsWith("cannon1") && cannonAimNum == 0) frameIndexToDraw = 0;
			if (tag.StartsWith("cannon2") && cannonAimNum == 1) frameIndexToDraw = 1;
			if (tag.StartsWith("cannon3") && cannonAimNum == 2) frameIndexToDraw = 2;
			if (tag.StartsWith("cannon4") && cannonAimNum == 3) frameIndexToDraw = 3;
			if (tag.StartsWith("cannon5") && cannonAimNum == 4) frameIndexToDraw = 4;
			if (frameIndexToDraw != null) {
				poiPos = new Point(pos.x + (poi.x * getShootXDirSynced()), pos.y + poi.y);
				return Global.sprites[cannonSprite];
			}
		}
		return null;
	}

	public Point setCannonAim(Point shootDir) {
		float shootY = -shootDir.y;
		float shootX = MathF.Abs(shootDir.x);
		float ratio = shootY / shootX;
		if (ratio > 1.25f) cannonAimNum = 3;
		else if (ratio <= 1.25f && ratio > 0.75f) cannonAimNum = 2;
		else if (ratio <= 0.75f && ratio > 0.25f) cannonAimNum = 1;
		else if (ratio <= 0.25f && ratio > -0.25f) cannonAimNum = 0;
		else cannonAimNum = 4;

		var cannonSprite = getCannonSprite(out Point poiPos, out _);
		Point? nullablePos = cannonSprite?.frames?.ElementAtOrDefault(cannonAimNum)?.POIs?.FirstOrDefault();
		if (nullablePos == null) {
		}
		Point cannonSpritePOI = nullablePos ?? Point.zero;

		return poiPos.addxy(cannonSpritePOI.x * getShootXDir(), cannonSpritePOI.y);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}

		if ((grounded || charState is LadderClimb || charState is LadderEnd || charState is WallSlide) && vileHoverTime > 0) {
			vileHoverTime -= Global.spf * 6;
			if (vileHoverTime < 0) vileHoverTime = 0;
		}

		//bool isShootingVulcan = sprite.name.EndsWith("shoot") && player.weapon is Vulcan;
		bool isShootingVulcan = vulcanLingerTime <= 0.1;
		if (isShootingVulcan) {
			vileAmmoRechargeCooldown = 0.15f;
		}

		if (vileAmmoRechargeCooldown > 0) {
			Helpers.decrementTime(ref vileAmmoRechargeCooldown);
		} else if (usedAmmoLastFrame) {
			usedAmmoLastFrame = false;
		} else if (!isShootingLongshotGizmo && !isShootingVulcan) {
			player.vileAmmo += Global.spf * 15;
			if (player.vileAmmo > player.vileMaxAmmo) {
				player.vileAmmo = player.vileMaxAmmo;
			}
		}


		if (player.vileAmmo >= player.vileMaxAmmo) {
			weaponHealAmount = 0;
		}
		if (weaponHealAmount > 0 && player.health > 0) {
			weaponHealTime += Global.spf;
			if (weaponHealTime > 0.05) {
				weaponHealTime = 0;
				weaponHealAmount--;
				player.vileAmmo = Helpers.clampMax(player.vileAmmo + 1, player.vileMaxAmmo);
				playSound("heal", forcePlay: true);
			}
		}

		if (vulcanLingerTime <= 0.1f && player.weapon.shootTime == 0f) {
			vulcanLingerTime += Global.spf;
			if (vulcanLingerTime > 0.1f && sprite.name.EndsWith("shoot")) {
				changeSpriteFromName(charState.sprite, resetFrame: false);
			}
		}

		player.vileStunShotWeapon.update();
		player.vileMissileWeapon.update();
		player.vileRocketPunchWeapon.update();
		player.vileNapalmWeapon.update();
		player.vileBallWeapon.update();
		player.vileCutterWeapon.update();
		player.vileLaserWeapon.update();
		player.vileFlamethrowerWeapon.update();

		if (calldownMechCooldown > 0) {
			calldownMechCooldown -= Global.spf;
			if (calldownMechCooldown < 0) calldownMechCooldown = 0;
		}
		Helpers.decrementTime(ref grabCooldown);
		Helpers.decrementTime(ref vileLadderShootCooldown);
		Helpers.decrementTime(ref mechBusterCooldown);
		Helpers.decrementTime(ref gizmoCooldown);

		if (player.weapon is not AssassinBullet && (player.vileLaserWeapon.type > -1 || isVileMK5)) {
			if (player.input.isHeld(Control.Special1, player) && charState is not Die && invulnTime == 0 && flag == null && player.vileAmmo >= player.vileLaserWeapon.getAmmoUsage(0)) {
				increaseCharge();
			} else {
				if (isCharging() && getChargeLevel() >= 3) {
					if (getChargeLevel() >= 4 && isVileMK5) {
						changeState(new HexaInvoluteState(), true);
					} else {
						player.vileLaserWeapon.vileShoot(WeaponIds.VileLaser, this);
					}
				}
				stopCharge();
			}
			chargeLogic();
		}

		var raState = charState as InRideArmor;
		if (rideArmor != null && raState != null && !raState.isHiding) {
			if (rideArmor.rideArmorState is RAIdle || rideArmor.rideArmorState is RAJump || rideArmor.rideArmorState is RAFall || rideArmor.rideArmorState is RADash) {
				bool stunShotPressed = player.input.isPressed(Control.Special1, player);
				bool goliathShotPressed = player.input.isPressed(Control.WeaponLeft, player) || player.input.isPressed(Control.WeaponRight, player);

				if (rideArmor.raNum == 4 && Options.main.swapGoliathInputs) {
					bool oldStunShotPressed = stunShotPressed;
					stunShotPressed = goliathShotPressed;
					goliathShotPressed = oldStunShotPressed;
				}

				if (stunShotPressed && !player.input.isHeld(Control.Down, player) && invulnTime == 0) {
					if (player.vileMissileWeapon.type == 1 || player.vileMissileWeapon.type == 2) {
						if (tryUseVileAmmo(player.vileMissileWeapon.vileAmmo)) {
							player.vileMissileWeapon.vileShoot(WeaponIds.ElectricShock, this);
						}
					} else if (player.vileStunShotWeapon.type == -1 || player.vileStunShotWeapon.type == 0) {
						if (tryUseVileAmmo(player.vileMissileWeapon.vileAmmo)) {
							player.vileStunShotWeapon.vileShoot(WeaponIds.ElectricShock, this);
						}
					}
				}

				if (goliathShotPressed) {
					if (rideArmor.raNum == 4 && !rideArmor.isAttacking() && mechBusterCooldown == 0) {
						rideArmor.changeState(new RAGoliathShoot(rideArmor.grounded), true);
						mechBusterCooldown = 1;
					}
				}
			}
			player.gridModeHeld = false;
			player.gridModePos = new Point();
			return;
		}

		if (charState is InRideChaser) {
			return;
		}

		player.changeWeaponControls();
		if (player.weapons.Count == 1 && player.weapon is MechMenuWeapon mmw2 && mmw2.isMenuOpened) {
			if (player.input.isPressed(Control.WeaponLeft, player) || player.input.isPressed(Control.WeaponRight, player)) {
				mmw2.isMenuOpened = false;
			}
		}

		bool wL = player.input.isHeld(Control.WeaponLeft, player);
		bool wR = player.input.isHeld(Control.WeaponRight, player);
		if (isVileMK5 && vileStartRideArmor != null && Options.main.mk5PuppeteerHoldOrToggle && player.weapon is MechMenuWeapon && !wL && !wR) {
			if (lastFrameWeaponRightHeld) {
				player.weaponSlot--;
				if (player.weaponSlot < 0) {
					player.weaponSlot = player.weapons.Count - 1;
				}
			} else {
				player.weaponSlot++;
				if (player.weaponSlot >= player.weapons.Count) {
					player.weaponSlot = 0;
				}
			}
		}
		lastFrameWeaponLeftHeld = wL;
		lastFrameWeaponRightHeld = wR;

		var mmw = player.weapon as MechMenuWeapon;

		// Vile V Ride control.

		if (!isVileMK5 || vileStartRideArmor == null) {
			if (player.input.isPressed(Control.Shoot, player) && mmw != null && calldownMechCooldown == 0) {
				onMechSlotSelect(mmw);
				return;
			}
		} else if (player.input.isPressed(Control.Special2, player) && !player.input.isHeld(Control.Down, player)) {
			onMechSlotSelect(mmw);
			return;
		}

		/* else if (mmw != null) {
			if (player.input.isPressed(Control.Up, player)) {
				onMechSlotSelect(mmw);
				player.changeWeaponSlot(player.prevWeaponSlot);
				return;
			}
		} */

		if (isVileMK5 && vileStartRideArmor != null) {
			if (canLinkMK5()) {
				if (vileStartRideArmor.character == null) {
					vileStartRideArmor.linkMK5(this);
				}
			} else {
				if (vileStartRideArmor.character != null) {
					vileStartRideArmor.unlinkMK5();
				}
			}
		}

		if (isVileMK5 && vileStartRideArmor != null &&
			player.input.isPressed(Control.Special2, player) &&
			player.input.isHeld(Control.Down, player)
		) {
			if (vileStartRideArmor.rideArmorState is RADeactive) {
				vileStartRideArmor.manualDisabled = false;
				vileStartRideArmor.changeState(new RAIdle("ridearmor_activating"), true);
			} else {
				vileStartRideArmor.manualDisabled = true;
				vileStartRideArmor.changeState(new RADeactive(), true);
				Global.level.gameMode.setHUDErrorMessage(
					player, "Deactivated Ride Armor.",
					playSound: false, resetCooldown: true
				);
			}
		}

		/* if (isVileMK5 && vileStartRideArmor != null && mmw != null && grounded && vileStartRideArmor.grounded && player.input.isPressed(Control.Down, player)) {
			if (vileStartRideArmor.rideArmorState is not RADeactive) {
				vileStartRideArmor.changeState(new RADeactive(), true);
				player.changeWeaponSlot(player.prevWeaponSlot);
				Global.level.gameMode.setHUDErrorMessage(player, "Deactivated Ride Armor.", playSound: false, resetCooldown: true);
				return;
			}
		} */

		if (isInvulnerableAttack()) return;
		if (!player.canControl) return;

		// GMTODO: Consider a better way here instead of a hard-coded deny list
		if (charState is Die || charState is Hurt || charState is VileRevive || charState is VileMK2Grabbed || charState is DeadLiftGrabbed || charState is WhirlpoolGrabbed || charState is UPGrabbed || charState is Taunt ||
			charState is DarkHoldState || charState is HexaInvoluteState || charState is CallDownMech || charState is NapalmAttack) return;

		if (charState is Dash || charState is AirDash) {
			if (isVileMK2 && (player.input.isPressed(Control.Special1, player))) {
				charState.isGrabbing = true;
				charState.superArmor = true;
				changeSpriteFromName("dash_grab", true);
			}
		}

		if (isShootingLongshotGizmo && player.weapon is VileCannon) {
			player.weapon.vileShoot(WeaponIds.FrontRunner, this);
		} else if (player.input.isPressed(Control.Special1, player)) {
			if (charState is Crouch) {
				if (player.vileNapalmWeapon.type == (int)NapalmType.NoneBall) {
					player.vileBallWeapon.vileShoot(WeaponIds.Napalm, this);
				} else if (player.vileNapalmWeapon.type == (int)NapalmType.NoneFlamethrower) {
					player.vileFlamethrowerWeapon.vileShoot(WeaponIds.Napalm, this);
				} else {
					player.vileNapalmWeapon.vileShoot(WeaponIds.Napalm, this);
				}
			} else if (charState is Jump || charState is Fall || charState is VileHover) {
				if (!player.input.isHeld(Control.Down, player)) {
					if (player.vileBallWeapon.type == (int)VileBallType.NoneNapalm) {
						player.vileNapalmWeapon.vileShoot(WeaponIds.VileBomb, this);
					} else if (player.vileBallWeapon.type == (int)VileBallType.NoneFlamethrower) {
						player.vileFlamethrowerWeapon.vileShoot(WeaponIds.VileBomb, this);
					} else {
						player.vileBallWeapon.vileShoot(WeaponIds.VileBomb, this);
					}
				} else {
					if (player.vileFlamethrowerWeapon.type == (int)VileFlamethrowerType.NoneNapalm) {
						player.vileNapalmWeapon.vileShoot(WeaponIds.VileFlamethrower, this);
					} else if (player.vileFlamethrowerWeapon.type == (int)VileFlamethrowerType.NoneBall) {
						player.vileBallWeapon.vileShoot(WeaponIds.VileFlamethrower, this);
					} else {
						player.vileFlamethrowerWeapon.vileShoot(WeaponIds.VileFlamethrower, this);
					}
				}
			} else if (charState is Idle || charState is Dash || charState is Run || charState is RocketPunchAttack) {
				if ((player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) && !player.input.isHeld(Control.Up, player)) {
					if (player.vileRocketPunchWeapon.type > -1) {
						player.vileRocketPunchWeapon.vileShoot(WeaponIds.RocketPunch, this);
					}
				} else if (charState is not RocketPunchAttack) {
					if (!player.input.isHeld(Control.Up, player) || player.vileCutterWeapon.type == -1) {
						if (player.vileMissileWeapon.type > -1) {
							player.vileMissileWeapon.vileShoot(WeaponIds.ElectricShock, this);
						}
					} else {
						player.vileCutterWeapon.vileShoot(WeaponIds.VileCutter, this);
					}
				}
			}
		} else if (player.input.isHeld(Control.Shoot, player)) {
			if (player.vileCutterWeapon.shootTime < player.vileCutterWeapon.rateOfFire * 0.75f) {
				player.weapon.vileShoot(0, this);
			}
		}
	}

	public override bool normalCtrl() {
		if (sprite.name.EndsWith("cannon_air") && isAnimOver()) {
			changeSpriteFromName("fall", true);
		}
		if (!grounded &&
			canVileHover() &&
			player.input.isPressed(Control.Jump, player) &&
			charState is not VileHover
		) {
			changeState(new VileHover(), true);
			return true;
		}
		return base.normalCtrl();
	}

	public bool canLinkMK5() {
		if (vileStartRideArmor == null) return false;
		if (vileStartRideArmor.rideArmorState is RADeactive && vileStartRideArmor.manualDisabled) return false;
		if (vileStartRideArmor.pos.distanceTo(pos) > Global.screenW * 0.75f) return false;
		return charState is not Die && charState is not VileRevive && charState is not CallDownMech && charState is not HexaInvoluteState;
	}

	public bool isVileMK5Linked() {
		return isVileMK5 && vileStartRideArmor?.character == this;
	}

	public bool canVileHover() {
		return isVileMK5 && player.vileAmmo > 0 && flag == null;
	}

	public override void onMechSlotSelect(MechMenuWeapon mmw) {
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
				if (player.selectedRAIndex == 4 && player.currency < 10) {
					if (isVileMK2) {
						Global.level.gameMode.setHUDErrorMessage(
							player, $"Goliath armor requires 10 {Global.nameCoins}"
						);
					} else {
						Global.level.gameMode.setHUDErrorMessage(
							player, $"Devil Bear armor requires 10 {Global.nameCoins}"
						);
					}
				} else {
					alreadySummonedNewMech = true;
					if (vileStartRideArmor != null) vileStartRideArmor.selfDestructTime = 1000;
					buyRideArmor();
					mmw.isMenuOpened = false;
					int raIndex = player.selectedRAIndex;
					if (isVileMK5 && raIndex == 4) raIndex++;
					vileStartRideArmor = new RideArmor(player, pos, raIndex, 0, player.getNextActorNetId(), true, sendRpc: true);
					if (vileStartRideArmor.raNum == 4) summonedGoliath = true;
					if (isVileMK5) {
						vileStartRideArmor.ownedByMK5 = true;
						vileStartRideArmor.zIndex = zIndex - 1;
						player.weaponSlot = 0;
						if (player.weapon is MechMenuWeapon) player.weaponSlot = 1;
					}
					changeState(new CallDownMech(vileStartRideArmor, true), true);
				}
			} else {
				if (player.selectedRAIndex == 4 && player.currency < 10) {
					if (isVileMK2) Global.level.gameMode.setHUDErrorMessage(
						player, $"Goliath armor requires 10 {Global.nameCoins}"
					);
					else Global.level.gameMode.setHUDErrorMessage(
						player, $"Devil Bear armor requires 10 {Global.nameCoins}"
					);
				} else {
					cantAffordRideArmorMessage();
				}
			}
		} else {
			if (!(charState is Idle || charState is Run || charState is Crouch)) return;
			changeState(new CallDownMech(vileStartRideArmor, false), true);
		}
	}

	public bool tryUseVileAmmo(float ammo) {
		if (player.weapon is Vulcan) {
			usedAmmoLastFrame = true;
		}
		if (player.vileAmmo > ammo - 0.1f) {
			usedAmmoLastFrame = true;
			if (weaponHealAmount == 0) {
				player.vileAmmo -= ammo;
				if (player.vileAmmo < 0) player.vileAmmo = 0;
			}
			return true;
		}
		return false;
	}

	private void cantAffordRideArmorMessage() {
		if (Global.level.is1v1()) {
			Global.level.gameMode.setHUDErrorMessage(player, "Ride Armor requires 16 HP");
		} else {
			Global.level.gameMode.setHUDErrorMessage(
				player, "Ride Armor requires " + callNewMechCost + " " + Global.nameCoins
			);
		}
	}

	public Point getVileShootVel(bool aimable) {
		Point vel = new Point(1, 0);
		if (!aimable) {
			return vel;
		}

		if (rideArmor != null) {
			if (player.input.isHeld(Control.Up, player)) {
				vel = new Point(1, -0.5f);
			} else {
				vel = new Point(1, 0.5f);
			}
		} else if (charState is VileMK2GrabState) {
			vel = new Point(1, -0.75f);
		} else if (player.input.isHeld(Control.Up, player)) {
			if (!canVileAim60Degrees() || (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player))) {
				vel = new Point(1, -0.75f);
			} else {
				vel = new Point(1, -3);
			}
		} else if (player.input.isHeld(Control.Down, player) && player.character.charState is not Crouch && charState is not MissileAttack) {
			vel = new Point(1, 0.5f);
		} else if (player.input.isHeld(Control.Down, player) && player.input.isLeftOrRightHeld(player) && player.character.charState is Crouch) {
			vel = new Point(1, 0.5f);
		}

		if (charState is RisingSpecterState) {
			vel = new Point(1, -0.75f);
		}

		/*
		if (charState is CutterAttackState)
		{
			vel = new Point(1, -3);
		}
		*/

		return vel;
	}

	public bool canVileAim60Degrees() {
		return charState is MissileAttack || charState is Idle || charState is CannonAttack;
	}

	public Point? getVileMK2StunShotPos() {
		if (charState is InRideArmor) {
			return pos.addxy(xDir * -8, -12);
		}

		var headPos = getHeadPos();
		if (headPos == null) return null;
		return headPos.Value.addxy(-xDir * 5, 3);
	}

	public void setVileShootTime(Weapon weapon, float modifier = 1f, Weapon targetCooldownWeapon = null) {
		targetCooldownWeapon = targetCooldownWeapon ?? weapon;
		if (isVileMK2) {
			float innerModifier = 1f;
			if (weapon is VileMissile) innerModifier = 0.33f;
			weapon.shootTime = targetCooldownWeapon.rateOfFire * innerModifier * modifier;
		} else {
			weapon.shootTime = targetCooldownWeapon.rateOfFire * modifier;
		}
	}

	public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint) {
		Projectile proj = null;
		if (sprite.name.Contains("dash_grab")) {
			proj = new GenericMeleeProj(new VileMK2Grab(), centerPoint, ProjIds.VileMK2Grab, player, 0, 0, 0);
		}
		return proj;
	}

	public override bool isSoftLocked() {
		if (isShootingLongshotGizmo) {
			return true;
		}
		if (isVileMK5 && player.weapon is MechMenuWeapon && vileStartRideArmor != null) {
			return true;
		}
		if (sprite.name.EndsWith("_idle_shoot") && sprite.frameTime < 0.1f) {
			return true;
		}
		return base.isSoftLocked();
	}

	public override bool canClimbLadder() {
		if (vileLadderShootCooldown > 0) {
			return false;
		}
		return base.canClimbLadder();
	}

	public override bool canChangeWeapons() {
		if (isShootingLongshotGizmo) {
			return false;
		}
		return base.canChangeWeapons();
	}

	public override bool canEnterRideArmor() {
		if (isVileMK5) {
			return false;
		}
		return base.canEnterRideArmor();
	}

	public override void changeSprite(string spriteName, bool resetFrame) {
		cannonAimNum = 0;
		base.changeSprite(spriteName, resetFrame);
	}

	public override string getSprite(string spriteName) {
		if (isVileMK5) {
			return "vilemk5_" + spriteName;
		}
		if (isVileMK2) {
			return "vilemk2_" + spriteName;
		}
		return "vile_" + spriteName;
	}

	public override void changeToIdleOrFall() {
		if (!grounded && charState.wasVileHovering && canVileHover()) {
			changeState(new VileHover(), true);
			return;
		}
		base.changeToIdleOrFall();
	}

	public override void render(float x, float y) {
		if (isSpeedDevilActiveBS.getValue()) {
			addRenderEffect(RenderEffectType.SpeedDevilTrail);
		} else {
			removeRenderEffect(RenderEffectType.SpeedDevilTrail);
		}
		if (currentFrame?.POIs?.Count > 0) {
			Sprite cannonSprite = getCannonSprite(out Point poiPos, out int zIndexDir);
			cannonSprite?.draw(
				cannonAimNum, poiPos.x, poiPos.y, getShootXDirSynced(),
				1, getRenderEffectSet(), alpha, 1, 1, zIndex + zIndexDir,
				getShaders(), actor: this
			);
		}

		if (player.isMainPlayer && isVileMK5 && vileHoverTime > 0 && charState is not HexaInvoluteState) {
			float healthPct = Helpers.clamp01((vileMaxHoverTime - vileHoverTime) / vileMaxHoverTime);
			float sy = -27;
			float sx = 20;
			if (xDir == -1) sx = 90 - 20;
			drawFuelMeter(healthPct, sx, sy);
		}
		base.render(x, y);
	}

	public override Point getAimCenterPos() {
		if (sprite.name.Contains("_ra_")) {
			return pos.addxy(0, -10);
		}
		return pos.addxy(0, -24);
	}

	public override Collider getGlobalCollider() {
		var rect = new Rect(0, 0, 18, 42);
		if (sprite.name.Contains("_ra_")) {
			rect.y2 = 20;
		}
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getDashingCollider() {
		Rect rect = new Rect(0, 0, 18, 30);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getCrouchingCollider() {
		Rect rect = new Rect(0, 0, 18, 30);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getRaCollider() {
		var rect = new Rect(0, 0, 18, 22);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}
}
