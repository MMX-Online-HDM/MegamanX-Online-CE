using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class Vile : Character {
	public const float maxCalldownMechCooldown = 2;
	public float grabCooldown = 1;
	public bool vulcanActive;
	public float vulcanLingerTime;
	public const int callNewMechCost = 5;
	float mechBusterCooldown;
	public bool usedAmmoLastFrame;
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
	
	public float calldownMechCooldown;

	public VileCannon cannonWeapon;
	public Vulcan vulcanWeapon;
	public VileMissile missileWeapon;
	public RocketPunch rocketPunchWeapon;
	public Napalm napalmWeapon;
	public VileBall grenadeWeapon;
	public VileCutter cutterWeapon;
	public VileFlamethrower flamethrowerWeapon;
	public VileLaser laserWeapon;
	public MechMenuWeapon rideMenuWeapon;

	public Vile(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, bool mk2VileOverride = false, bool mk5VileOverride = false
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.Vile;
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
		VileLoadout vileLoadout = player.loadout.vileLoadout;

		vulcanWeapon = new Vulcan((VulcanType)vileLoadout.vulcan);
		cannonWeapon = new VileCannon((VileCannonType)vileLoadout.cannon);
		missileWeapon = new VileMissile((VileMissileType)vileLoadout.missile);
		rocketPunchWeapon = new RocketPunch((RocketPunchType)vileLoadout.vulcan);
		napalmWeapon = new Napalm((NapalmType)vileLoadout.napalm);
		grenadeWeapon = new VileBall((VileBallType)vileLoadout.ball);
		cutterWeapon = new VileCutter((VileCutterType)vileLoadout.cutter);
		flamethrowerWeapon = vileLoadout.flamethrower switch {
			1 => new SeaDragonRage(),
			2 => new DragonsWrath(),
			_ => new WildHorseKick()
		};
		laserWeapon = new VileLaser((VileLaserType)vileLoadout.laser);
		rideMenuWeapon = new MechMenuWeapon(VileMechMenuType.All);
	}

	public Sprite? getCannonSprite(out Point poiPos, out int zIndexDir) {
		poiPos = getCenterPos();
		zIndexDir = 0;

		string vilePrefix = "vile_";
		if (isVileMK2) vilePrefix = "vilemk2_";
		if (isVileMK5) vilePrefix = "vilemk5_";
		string cannonSprite = vilePrefix + "cannon";
		for (int i = 0; i < currentFrame.POIs.Length; i++) {
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
				return new Sprite(cannonSprite);
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
		Point? nullablePos = cannonSprite?.animData.frames?.ElementAtOrDefault(cannonAimNum)?.POIs?.FirstOrDefault();
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

		if (vulcanLingerTime <= 0.1f && vulcanWeapon.shootCooldown == 0f) {
			vulcanLingerTime += Global.spf;
			if (vulcanLingerTime > 0.1f && sprite.name.EndsWith("shoot")) {
				changeSpriteFromName(charState.sprite, resetFrame: false);
			}
		}
		cannonWeapon.update();
		vulcanWeapon.update();
		missileWeapon.update();
		rocketPunchWeapon.update();
		napalmWeapon.update();
		grenadeWeapon.update();
		cutterWeapon.update();
		laserWeapon.update();
		flamethrowerWeapon.update();

		if (calldownMechCooldown > 0) {
			calldownMechCooldown -= Global.spf;
			if (calldownMechCooldown < 0) calldownMechCooldown = 0;
		}
		Helpers.decrementTime(ref grabCooldown);
		Helpers.decrementTime(ref mechBusterCooldown);
		Helpers.decrementTime(ref gizmoCooldown);


		if (charState is InRideChaser) {
			return;
		}
		RideArmorAttacks();
		RideLinkMK5();
		// GMTODO: Consider a better way here instead of a hard-coded deny list
		// Gacel: Done, now it uses attackCtrl
		if (!charState.attackCtrl || charState is VileMK2GrabState) {
			return;
		}
		chargeLogic(shoot);
	}
	public override bool attackCtrl() {
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		bool shootHeld = player.input.isHeld(Control.Shoot, player);
		bool WeaponRightHeld = player.input.isHeld(Control.WeaponRight, player);
		if (specialPressed) {
			dashGrabSpecial();
			airDownAttacks();
			return normalAttacks();
		}
		if (shootHeld) {
			if (cutterWeapon.shootCooldown < cutterWeapon.fireRate * 0.75f) 
				cannonWeapon.vileShoot(0, this);
		}
		if (WeaponRightHeld) {
			vulcanWeapon.vileShoot(0, this);
		}
		return base.attackCtrl();
	}
	public bool normalAttacks() {
		bool LeftorRightHeld = player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player);
		bool UpHeld = player.input.isHeld(Control.Up, player);
		if (charState is Crouch) {
			napalmWeapon.vileShoot(WeaponIds.Napalm, this);
			return true;
		}
		if (LeftorRightHeld && !UpHeld && grounded) {
			if (rocketPunchWeapon.type > -1) {
				rocketPunchWeapon.vileShoot(WeaponIds.RocketPunch, this);
			}
			return true;
		}
		if (!UpHeld || cutterWeapon.type == -1) {
			if (missileWeapon.type > -1) 
				missileWeapon.vileShoot(WeaponIds.ElectricShock, this);					
			return true;			
		}
		if (UpHeld) {
			cutterWeapon.vileShoot(WeaponIds.VileCutter, this);
			return true;
		}
		return false;
	}
	public bool airDownAttacks() {
		bool HeldDown = player.input.isHeld(Control.Down, player);
		bool dashorairdash = charState is Dash || charState is AirDash;
		if (!grounded && !dashorairdash) {
			if (!HeldDown) 
				grenadeWeapon.vileShoot(WeaponIds.VileBomb, this);
			 else 
				flamethrowerWeapon.vileShoot(WeaponIds.VileFlamethrower, this);
			return true;
		}
		return false;
	}
	public bool dashGrabSpecial() {
		if (charState is Dash || charState is AirDash) {
			if (isVileMK2) {
				charState.isGrabbing = true;
				charState.superArmor = true; //peakbalance
				changeSpriteFromName("dash_grab", true);
			}
			return true;
		}
		return false;
	} 
	public bool RideArmorAttacks() {
		var raState = charState as InRideArmor;
		bool Goliath = rideArmor?.raNum == 4;
		bool stunShotPressed = player.input.isPressed(Control.Special1, player);
		bool HeldDown = player.input.isHeld(Control.Down, player);
		bool goliathShotPressed = player.input.isPressed(Control.WeaponLeft, player) || player.input.isPressed(Control.WeaponRight, player);
		bool raStates = rideArmor?.rideArmorState is RAIdle || rideArmor?.rideArmorState is RAJump || rideArmor?.rideArmorState is RAFall || rideArmor?.rideArmorState is RADash;
		if (rideArmor != null && raState != null && !raState.isHiding) {
			if (raStates) {
				if (Goliath && Options.main.swapGoliathInputs) {
					bool oldStunShotPressed = stunShotPressed;
					stunShotPressed = goliathShotPressed;
					goliathShotPressed = oldStunShotPressed;
				}
				if (stunShotPressed && !HeldDown) {
					if (tryUseVileAmmo(missileWeapon.vileAmmo)) {
						missileWeapon.vileShoot(WeaponIds.ElectricShock, this);
					}
				}
				if (goliathShotPressed) {
					if (Goliath && !rideArmor.isAttacking() && mechBusterCooldown == 0) {
						rideArmor.changeState(new RAGoliathShoot(rideArmor.grounded), true);
						mechBusterCooldown = 1;
					}
				}
			}
			player.gridModeHeld = false;
			player.gridModePos = new Point();
			return true;
		}
		return false;
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
	public void shoot(int chargeLevel) {
		if (chargeLevel >= 3) {
			laserWeapon.vileShoot(WeaponIds.VileLaser, this);
		}
		if (chargeLevel == 4 && isVileMK5) {
			changeState(new HexaInvoluteState(), true);
		} 
	}
	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Special1, player);
	}
	public override bool canCharge() {
		return !isInvulnerableAttack() && charState is not Die && invulnTime == 0;
	}
	public override bool canShoot() {
		if (isInvulnerableAttack()) return false;
		if (invulnTime > 0) return false;
		if (!player.canControl) return false;
		return base.canShoot();
	}
	public override void chargeLogic(Action<int> shootFunct) {
		if (chargeButtonHeld() && flag == null && player.vileAmmo >= laserWeapon.getAmmoUsage(0)) {
			if (canCharge()) {
				increaseCharge();
			}
		}
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
	public void RideLinkMK5() {
		if (isVileMK5 && linkedRideArmor != null &&
			player.input.isPressed(Control.Special2, player) &&
			player.input.isHeld(Control.Down, player)
		) {
			if (linkedRideArmor.rideArmorState is RADeactive) {
				linkedRideArmor.manualDisabled = false;
				linkedRideArmor.changeState(new RAIdle("ridearmor_activating"), true);
			} else {
				linkedRideArmor.manualDisabled = true;
				linkedRideArmor.changeState(new RADeactive(), true);
				Global.level.gameMode.setHUDErrorMessage(
					player, "Deactivated Ride Armor.",
					playSound: false, resetCooldown: true
				);
			}
		}
		// Vile V Ride control.
		if (!isVileMK5 || linkedRideArmor == null) {
			if (player.input.isPressed(Control.Special2, player) &&
				rideMenuWeapon != null && calldownMechCooldown == 0 &&
				(!alreadySummonedNewMech || linkedRideArmor != null)
			) {
				onMechSlotSelect(rideMenuWeapon);
				return;
			}
		//Ride Menu
		} else if (player.input.isPressed(Control.Special2, player) && !player.input.isHeld(Control.Down, player)) {
			onMechSlotSelect(rideMenuWeapon);
			return;
		}
		if (rideMenuWeapon?.isMenuOpened == true) {
			if (player.input.isPressed(Control.Special1, player) || player.input.isPressed(Control.WeaponLeft, player)) {
				rideMenuWeapon.isMenuOpened = false;
			}
		}

		if (isVileMK5 && linkedRideArmor != null) {
			if (canLinkMK5()) {
				if (linkedRideArmor.character == null) {
					linkedRideArmor.linkMK5(this);
				}
			} else {
				if (linkedRideArmor.character != null) {
					linkedRideArmor.unlinkMK5();
				}
			}
		}
	}
	public bool canLinkMK5() {
		if (linkedRideArmor == null) return false;
		if (linkedRideArmor.rideArmorState is RADeactive && linkedRideArmor.manualDisabled) return false;
		if (linkedRideArmor.pos.distanceTo(pos) > Global.screenW * 0.75f) return false;
		return charState is not Die && charState is not VileRevive && charState is not CallDownMech && charState is not HexaInvoluteState;
	}

	public bool isVileMK5Linked() {
		return isVileMK5 && linkedRideArmor?.character == this;
	}

	public bool canVileHover() {
		return isVileMK5 && player.vileAmmo > 0 && flag == null;
	}

	public override bool canTurn() {
		if (rideArmorPlatform != null) {
			return false;
		}
		return base.canTurn();
	}

	public override bool canWallClimb() {
		if (charState is VileHover) {
			return !player.input.isHeld(Control.Jump, player);
		}
		return base.canWallClimb();
	}

	public override bool canUseLadder() {
		if (charState is VileHover) {
			return !player.input.isHeld(Control.Jump, player);
		}
		return base.canWallClimb();
	}

	public override Point getDashDustEffectPos(int xDir) {
		float dashXPos = -30;
		return pos.addxy(dashXPos * xDir + (5 * xDir), -4);
	}

	public override void onMechSlotSelect(MechMenuWeapon mmw) {
		if (linkedRideArmor == null) {
			if (!mmw.isMenuOpened) {
				mmw.isMenuOpened = true;
				return;
			}
		}

		if (player.isAI) {
			calldownMechCooldown = maxCalldownMechCooldown;
		}
		if (linkedRideArmor == null) {
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
					if (linkedRideArmor != null) linkedRideArmor.selfDestructTime = 1000;
					buyRideArmor();
					mmw.isMenuOpened = false;
					int raIndex = player.selectedRAIndex;
					if (isVileMK5 && raIndex == 4) raIndex++;
					linkedRideArmor = new RideArmor(player, pos, raIndex, 0, player.getNextActorNetId(), true, sendRpc: true);
					if (linkedRideArmor.raNum == 4) summonedGoliath = true;
					if (isVileMK5) {
						linkedRideArmor.ownedByMK5 = true;
						linkedRideArmor.zIndex = zIndex - 1;
						player.weaponSlot = 0;
					}
					changeState(new CallDownMech(linkedRideArmor, true), true);
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
			changeState(new CallDownMech(linkedRideArmor, false), true);
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
	public override void addAmmo(float amount) {
		weaponHealAmount += amount;
	}
	public override void addPercentAmmo(float amount) {
		weaponHealAmount += amount * 0.32f;
	}
	public override bool canAddAmmo() {
		return player.vileAmmo < player.vileMaxAmmo;
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

	public void setVileShootTime(Weapon weapon, float modifier = 1f, Weapon? targetCooldownWeapon = null) {
		targetCooldownWeapon = targetCooldownWeapon ?? weapon;
		if (isVileMK2) {
			float innerModifier = 1f;
			if (weapon is VileMissile) innerModifier = 0.3333f;
			weapon.shootCooldown = MathF.Ceiling(targetCooldownWeapon.fireRate * innerModifier * modifier);
		} else {
			weapon.shootCooldown = MathF.Ceiling(targetCooldownWeapon.fireRate * modifier);
		}
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		Projectile? proj = null;
		if (sprite.name.Contains("dash_grab")) {
			proj = new GenericMeleeProj(new VileMK2Grab(), centerPoint, ProjIds.VileMK2Grab, player, 0, 0, 0);
		}
		return proj;
	}

	public override bool isSoftLocked() {
		if (isShootingLongshotGizmo) {
			return true;
		}
		if (isVileMK5 && linkedRideArmor != null && player.input.isHeld(Control.WeaponLeft, player)) {
			return true;
		}
		if (sprite.name.EndsWith("_idle_shoot") && sprite.frameTime < 6) {
			return true;
		}
		return base.isSoftLocked();
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

	public override void changeToIdleOrFall(string transitionSprite = "") {
		if (!grounded && charState.wasVileHovering && canVileHover()) {
			changeState(new VileHover(), true);
			return;
		}
		base.changeToIdleOrFall(transitionSprite);
	}

	public override float getLabelOffY() {
		if (sprite.name.Contains("_ra_")) {
			return 25;
		}
		return 50;
	}

	public override void render(float x, float y) {
		if (player.speedDevil) {
			addRenderEffect(RenderEffectType.SpeedDevilTrail);
		} else {
			removeRenderEffect(RenderEffectType.SpeedDevilTrail);
		}
		if (currentFrame.POIs.Length > 0) {
			Sprite? cannonSprite = getCannonSprite(out Point poiPos, out int zIndexDir);
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

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = base.getShaders();

		if (player.frozenCastle && player.frozenCastleShader != null) {
			shaders.Add(player.frozenCastleShader);
		}

		return shaders;
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();

		customData.Add(Helpers.boolArrayToByte([
			player.frozenCastle,
			player.speedDevil
		]));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		bool[] boolData = Helpers.byteToBoolArray(data[0]);
		player.frozenCastle = boolData[0];
		player.speedDevil = boolData[1];
	}
}
