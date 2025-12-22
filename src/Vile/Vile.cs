using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MMXOnline;

public class Vile : Character {
	public const float maxCalldownMechCooldown = 120;
	public float vulcanLingerTime;
	public const int callNewMechCost = 5;
	public float mechBusterCooldown;
	public bool usedAmmoLastFrame;
	public bool isShootingGizmo;
	public bool wasShootingVulcan;
	public bool isShootingVulcan => vulcanLingerTime > 0;
	public bool hasFrozenCastle;
	public bool hasSpeedDevil;
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
	public int cannonAimNum;
	public float calldownMechCooldown;
	public VileAmmoWeapon energy = new();
	public float deadCooldown;
	public const float maxdeadCooldown = 60;
	public float[] chargeTimeEx = new float[3];
	public VileLoadout loadout;
	public MechMenuWeapon rideMenuWeapon;
	public VileWeaponSystem weaponSystem;
	public float aiAttackCooldown;

	public Vile(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, bool mk2VileOverride = false, bool mk5VileOverride = false,
		VileLoadout? loadout = null,
		int? heartTanks = null, bool isATrans = false
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn, heartTanks, isATrans
	) {
		charId = CharIds.Vile;
		vileForm = 1;
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
		loadout ??= player.loadout.vileLoadout.clone();
		this.loadout = loadout;

		weaponSystem = setupWeaponSystem();

		rideMenuWeapon = new MechMenuWeapon(VileMechMenuType.All);
		hasFrozenCastle = player.frozenCastle;
		hasSpeedDevil = player.speedDevil;
	}

	public (Sprite? spr, Point drawPos, Point shootPos) getCannonSprite() {
		string vilePrefix = "vile_";
		if (isVileMK2) { vilePrefix = "vilemk2_"; }
		if (isVileMK5) { vilePrefix = "vilemk5_"; }
		string cannonSprite = vilePrefix + "cannon";

		for (int i = 0; i < currentFrame.POIs.Length; i++) {
			string tag = currentFrame.POITags[i] ?? "";
			if (tag == "") {
				continue;
			}
			int frameIndexToDraw = tag.ToLower() switch {
				"cannon1" or "cannon1b" => 0,
				"cannon2" or "cannon2b" => 1,
				"cannon3" or "cannon3b" => 2,
				"cannon4" or "cannon4b" => 3,
				"cannon5" or "cannon5b" => 4,
				"cannon" => cannonAimNum,
				_ => -1
			};
			if (frameIndexToDraw != cannonAimNum) {
				continue;
			}
			Point poi = currentFrame.POIs[i];
			Sprite retSprite = new Sprite(cannonSprite);
			int dir = getShootXDirSynced();
			Point altPOI = (
				retSprite.animData.frames.ElementAtOrDefault(cannonAimNum)?.POIs?.FirstOrDefault() ??
				Point.zero
			);
			altPOI.x *= dir;

			Point drawPos = new Point(poi.x * dir + pos.x, poi.y + pos.y);
			Point shootPos = drawPos + altPOI;

			return (retSprite, drawPos, shootPos);
		}
		return (null, pos, getCenterPos());
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

		return getCannonSprite().shootPos;
	}

	public override void preUpdate() {
		base.preUpdate();

		if (isVileMK1) altSoundId = AltSoundIds.X1;
		else if (isVileMK2 || isVileMK5) altSoundId = AltSoundIds.X3;

		if (!ownedByLocalPlayer) return;

		if (!isShootingGizmo && !isShootingVulcan && !usedAmmoLastFrame) {
			energy.addAmmo(0.25f * speedMul, player);
		}
		usedAmmoLastFrame = false;

		if (isShootingVulcan) {
			string targeSprite = charState.shootSpriteEx;
			if (targeSprite == "") {
				targeSprite = grounded ? "shoot" : "shoot_fall";
			}
			if (getSprite(sprite.name) != charState.shootSpriteEx) {
				changeSpriteFromName(charState.shootSpriteEx, false);
			}
			wasShootingVulcan = true;
		}
		else if (wasShootingVulcan) {
			changeSpriteFromName(charState.sprite, resetFrame: false);
			wasShootingVulcan = false;
		}
		shootAnimTime = vulcanLingerTime;

		Helpers.decrementFrames(ref calldownMechCooldown);
		Helpers.decrementFrames(ref mechBusterCooldown);
		Helpers.decrementFrames(ref aiAttackCooldown);
		Helpers.decrementFrames(ref vulcanLingerTime);
		Helpers.decrementFrames(ref deadCooldown);
		addWeaponHealAmmo();

		if ((grounded || charState is LadderClimb or LadderEnd or WallSlide) && vileHoverTime > 0) {
			vileHoverTime -= Global.spf * 6;
			if (vileHoverTime < 0) vileHoverTime = 0;
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		
		// Update the weapon system.
		// And subweapons by extension.
		weaponSystem.update();
		weaponSystem.charLinkedUpdate(this, false);

		rideArmorAttacks();
		rideLinkPenta();

		// GMTODO: Consider a better way here instead of a hard-coded deny list
		// Gacel: Done, now it uses attackCtrl
		if (!charState.attackCtrl || charState is VileMK2GrabState) {
			chargeLogic(null);
		} else {
			chargeLogic(shoot);
		}
	}

	public override bool attackCtrl() {
		if (dashGrabSpecial()) {
			return true;
		}
		return weaponSystem.shootLogic(this);
	}

	public bool dashGrabSpecial() {
		if (!player.input.isHeld(Control.Special1, player)) {
			return false;
		}
		if (isDashing && isVileMK2 &&
			charState is Dash or AirDash { stop: false }
		) {
			charState.isGrabbing = true;
			charState.superArmor = true; //peakbalance
			changeSpriteFromName("dash_grab", true);
			return true;
		}
		return false;
	}

	public bool rideArmorAttacks() {
		bool goliath = rideArmor?.raNum == 4;
		bool stunShotPressed = player.input.isPressed(Control.Special1, player);
		bool HeldDown = player.input.isHeld(Control.Down, player);
		bool goliathShotPressed = (
			player.input.isPressed(Control.WeaponLeft, player) ||
			player.input.isPressed(Control.WeaponRight, player)
		);
		bool raStates = rideArmor?.rideArmorState is RAIdle or RAJump or RAFall or RADash;
		if (rideArmor != null && charState is InRideArmor raState && !raState.isHiding) {
			if (raStates) {
				if (goliath && Options.main.swapGoliathInputs) {
					(goliathShotPressed, stunShotPressed) = (stunShotPressed, goliathShotPressed);
				}
				Weapon rideWeapon = weaponSystem.rideWeapon;
				if (stunShotPressed && !HeldDown && rideWeapon.shootCooldown <= 0) {
					rideWeapon.vileShoot(WeaponIds.ElectricShock, this);
				}
				if (goliathShotPressed) {
					if (goliath && !rideArmor.isAttacking() && mechBusterCooldown <= 0) {
						rideArmor.changeState(new RAGoliathShoot(rideArmor.grounded), true);
						mechBusterCooldown = 60;
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
		if (chargeLevel >= 4 && isVileMK5) {
			changeState(new HexaInvoluteState(), true);
		}
		else if (chargeLevel >= 3) {
			weaponSystem.chargeWeapon.vileShoot(WeaponIds.VileLaser, this);
		}
	}

	public override bool chargeButtonHeld() {
		if (currentWeapon is AssassinBulletChar) {
			return player.input.isHeld(Control.Shoot, player);
		}
		return player.input.isHeld(Control.Special1, player);
	}

	public override bool canCharge() {
		return (
			!isInvulnerable(true) &&
			alive && invulnTime == 0 &&
			charState is not VileRevive and not Die and not HexaInvoluteState
		);
	}

	public override int getMaxChargeLevel() {
		return isVileMK5 ? 4 : 3;
	}

	public override bool canShoot() {
		if (isInvulnerableAttack() || invulnTime > 0) {
			return false;
		}
		return base.canShoot();
	}

	public void rideLinkPenta() {
		//Do not use if dead
		if (!alive) { 
			rideMenuWeapon.isMenuOpened = false;
			return;
		}
		// Deactivation code.
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
		// Ride Menu
		} else if (!oldATrans &&
			player.input.isPressed(Control.Special2, player) &&
			!player.input.isHeld(Control.Down, player)
		) {
			onMechSlotSelect(rideMenuWeapon);
			return;
		}
		// Menu controls.
		if (rideMenuWeapon?.isMenuOpened == true) {
			if (player.input.isPressed(Control.Special1, player) ||
				player.input.isPressed(Control.WeaponLeft, player)
			) {
				rideMenuWeapon.isMenuOpened = false;
			}
		}
		// Link code.
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
		return isVileMK5 && energy.ammo > 0 && flag == null;
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
		//Do not use if dead
		if (!alive) return;	
		if (linkedRideArmor == null) {
			if (!mmw.isMenuOpened) {
				mmw.isMenuOpened = true;
				return;
			}
		}
		int randomString = Helpers.randomRange(1, 2);
		string brownOrGoliath = randomString == 1 ? "Brown Bear" : randomString == 2 ? "Goliath" : "";
		if (player.isAI) {
			calldownMechCooldown = maxCalldownMechCooldown;
		}
		if (linkedRideArmor == null) {
			if (alreadySummonedNewMech) {
				Global.level.gameMode.setHUDErrorMessage(player, "Can only summon a mech once per life");
			} else if (canAffordRideArmor()) {
				if (!(charState is Idle || charState is Run || charState is Crouch)) return;
				if (isVileMK1 && player.selectedRAIndex == 4) {
					Global.level.gameMode.setHUDErrorMessage(player, brownOrGoliath + " only available as MKII"); return;
				}
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

	public bool tryUseVileAmmo(float ammo, bool isVulcan = false) {
		// Do not drain if negative, use ammo regen for that.
		if (ammo < 0) {
			return true;
		}
		if (weaponHealAmount > 0) {
			return true;
		}
		if (isVulcan) {
			usedAmmoLastFrame = true;
		}
		if (energy.ammo >= ammo) {
			usedAmmoLastFrame = true;
			energy.addAmmo(-ammo, player);
			return true;
		}
		return false;
	}

	public override void addAmmo(float amount) {
		if (amount < 0) {
			energy.addAmmo(amount, player);
			return;
		}
		weaponHealAmount += amount;
	}
	public override void addPercentAmmo(float amount) {
		weaponHealAmount += amount * 0.32f;
	}
	public override bool canAddAmmo() {
		return energy.ammo < energy.maxAmmo;
	}
	public void addWeaponHealAmmo() {
        if (energy.ammo >= energy.maxAmmo) {
			weaponHealAmount = 0;
		}
		if (weaponHealAmount > 0 && alive) {
			weaponHealTime += Global.spf;
			if (weaponHealTime > 0.05) {
				weaponHealTime = 0;
				weaponHealAmount--;
				energy.addAmmo(1, player);
				if (isVileMK1) {
					playSound("heal", forcePlay: true, true);
				} else {
					playSound("healX3", forcePlay: true, true);
				}
			}
		}
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
		if (!aimable) return vel;
		bool isHeldUp = player.input.isHeld(Control.Up, player);
		bool isHeldDown = player.input.isHeld(Control.Down, player);
		bool isLeftOrRightHeld = player.input.isLeftOrRightHeld(player);
		bool isRideArmor = rideArmor != null;
		bool isCrouchState = charState is Crouch;
		if (isRideArmor) {
			if (isHeldUp) vel = new Point(1, -0.5f);
			else vel = new Point(1, 0.5f);
		} else if (!isCrouchState) {
			if (!canVileAim60Degrees()) return vel;
			if (isHeldUp) {
				if (isLeftOrRightHeld) vel = new Point(1, -0.75f);
				else vel = new Point(1, -3);
			} else if (isHeldDown) {
				if (isLeftOrRightHeld) vel = new Point(1, 0.75f);
				else vel = new Point(1, 3);
			}
		} else if (isCrouchState) {
			if (isHeldUp) vel = new Point(1, -0.5f);
			if (isLeftOrRightHeld) vel = new Point(1, 0.5f);
        }

		/*
		if (rideArmor != null) {
			if (player.input.isHeld(Control.Up, player)) {
				vel = new Point(1, -0.5f);
			} else {
				vel = new Point(1, 0.5f);
			}
		} else if (charState is VileMK2GrabState) {
			vel = new Point(1, -0.75f); //This code was from old times when you could shoot cannon on GrabState
		} else if (player.input.isHeld(Control.Up, player)) {
			if (!canVileAim60Degrees() || (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player))) {
				vel = new Point(1, -0.75f);
			} else {
				vel = new Point(1, -3);
			}
		} else if (player.input.isHeld(Control.Down, player) && charState is not Crouch && charState is not MissileAttack) {
			vel = new Point(1, 0.5f);
		} else if (player.input.isHeld(Control.Down, player) && player.input.isLeftOrRightHeld(player) && charState is Crouch) {
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

	public Point getVileMK2StunShotPos() {
		if (charState is InRideArmor) {
			return pos.addxy(xDir * -8, -12);
		}
		return pos.addxy(-xDir * 5, -32);
	}

	public void setVileShootTime(Weapon weapon, float modifier = 1f, Weapon? targetCooldownWeapon = null) {
		targetCooldownWeapon ??= weapon;
		if (isVileMK2 || isVileMK5) {
			float innerModifier = 1f;
			if (weapon is VileMissile) innerModifier = isVileMK2 ? 0.3333f : 0.6666f;
			weapon.shootCooldown = MathF.Ceiling(targetCooldownWeapon.fireRate * innerModifier * modifier);
		} else {
			weapon.shootCooldown = MathF.Ceiling(targetCooldownWeapon.fireRate * modifier);
		}
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Grab,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		if (sprite.name.Contains("dash_grab")) {
			return (int)MeleeIds.Grab;
		};
		return (int)MeleeIds.None;
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Grab => new GenericMeleeProj(
				new VileMK2Grab(), pos, ProjIds.VileMK2Grab, player,
				0, 0, 0,
				addToLevel: true
			),
			_ => null
		};
	}

	public override bool isSoftLocked() {
		if (isShootingGizmo) {
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
		if (isShootingGizmo) {
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

	public override void changeToIdleOrFall(string transitionSprite = "", string transShootSprite = "") {
		if (!grounded && charState.wasVileHovering && canVileHover()) {
			changeState(new VileHover(), true);
			return;
		}
		base.changeToIdleOrFall(transitionSprite, transShootSprite);
	}

	public override float getLabelOffY() {
		if (sprite.name.Contains("_ra_")) {
			return 25;
		}
		return 50;
	}

	public override void render(float x, float y) {
		if (hasSpeedDevil) {
			addRenderEffect(RenderEffectType.SpeedDevilTrail);
		} else {
			removeRenderEffect(RenderEffectType.SpeedDevilTrail);
		}
		if (currentFrame.POIs.Length > 0) {
			(Sprite? cannonSprite, Point drawPos, _) = getCannonSprite();
			cannonSprite?.draw(
				cannonAimNum, drawPos.x, drawPos.y, getShootXDirSynced(),
				1, getRenderEffectSet(), alpha, 1, 1, zIndex + 1,
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

		if (hasFrozenCastle && player.frozenCastleShader != null) {
			shaders.Add(player.frozenCastleShader);
		}

		return shaders;
	}

	public override float getRunSpeed() {
		if (hasSpeedDevil) {
			return base.getRunSpeed() * 1.1f;
		}
		return base.getRunSpeed();
	}

	public override float getDashSpeed() {
		float dashSpeed = 3.45f;
		if (hasSpeedDevil) {
			dashSpeed *= 1.1f;
		}
		return dashSpeed * getRunDebuffs();
	}

	public override Point getParasitePos() {
		if (sprite.name.Contains("_ra_")) {
			if (sprite.name.Contains("_ra_hide")) {
				pos.addxy(0, -6 + 22 * (sprite.frameIndex / (float)sprite.totalFrameNum));
			}
			return pos.addxy(0, -6);
		}
		return pos.addxy(0, -24);
	}

	public override void onDeath() {
		base.onDeath();
		player.lastDeathWasVileMK2 = isVileMK2;
		player.lastDeathWasVileV = isVileMK5;
		deadCooldown = maxdeadCooldown;
	}

	public VileWeaponSystem setupWeaponSystem() {	
		Weapon? vulcanWeapon = loadout.vulcan switch {
			0 => new CherryBlast(),
			1 => new DistanceNeedler(),
			2 => new BuckshotDance(),
			_ => null
		};
		Weapon? cannonWeapon = loadout.cannon switch {
			1 => new FatBoy(),
			2 => new LongShotGizmo(),
			0 => new FrontRunner(),
			_ => null
		};
		Weapon? missileWeapon = loadout.missile switch {
			0 => new ElectricShock(),
			1 => new HumerusCrush(),
			2 => new PopcornDemon(),
			_ => null
		};
		Weapon? rocketPunchWeapon = loadout.rocketPunch switch {
			0 => new GoGetterRight(),
			1 => new SpoiledBrat(),
			2 => new InfinityGig(),
			_ => null
		};
		Weapon? napalmWeapon = loadout.napalm switch {
			0 => new RumblingBang(),
			1 => new FireGrenade(),
			2 => new SplashHit(),
			_ => null
		};
		Weapon? grenadeWeapon = loadout.ball switch {
			0 => new ExplosiveRound(),
			1 => new SpreadShot(),
			2 => new PeaceOutRoller(),
			_ => null
		};
		Weapon? cutterWeapon = loadout.cutter switch {
			0 => new QuickHomesick(),
			1 => new ParasiteSword(),
			2 => new MaroonedTomahawk(),
			_ => null
		};
		Weapon? flamethrowerWeapon = loadout.flamethrower switch {
			0 => new WildHorseKick(),
			1 => new SeaDragonRage(),
			2 => new DragonsWrath(),
			_ => null
		};
		Weapon? downSpWeapon = loadout.downSpWeapon switch {
			0 => napalmWeapon,
			1 => grenadeWeapon,
			2 => flamethrowerWeapon,
			_ => napalmWeapon,
		};
		Weapon? airSpWeapon = loadout.airSpWeapon switch {
			0 => napalmWeapon,
			1 => grenadeWeapon,
			2 => flamethrowerWeapon,
			_ => napalmWeapon,
		};
		Weapon? downAirSpWeapon = loadout.downAirSpWeapon switch {
			0 => napalmWeapon,
			1 => grenadeWeapon,
			2 => flamethrowerWeapon,
			_ => napalmWeapon,
		};
		Weapon? laserWeapon = loadout.laser switch {
			0 => new RisingSpecter(),
			1 => new NecroBurst(),
			2 => new StraightNightmare(),
			_ => null
		};
		// Assing weapons to specific slots.
		Weapon?[] shootWps = [cannonWeapon, null, null, null];
		Weapon?[] specialWps = [missileWeapon, rocketPunchWeapon, null, downSpWeapon];
		Weapon?[] airSpecialWps = [airSpWeapon, null, null, downAirSpWeapon];
		Weapon?[] altWps = [vulcanWeapon, null, cutterWeapon, null];

		return new VileWeaponSystem(
			altWps, shootWps, specialWps,
			altWps, shootWps, airSpecialWps, [
				laserWeapon ?? new EmptyWeapon(),
				missileWeapon ?? new ElectricShock(),
				napalmWeapon ?? new RumblingBang()
			]
		);
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();

		customData.Add(Helpers.boolArrayToByte([
			hasFrozenCastle,
			hasSpeedDevil
		]));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		bool[] boolData = Helpers.byteToBoolArray(data[0]);
		hasFrozenCastle = boolData[0];
		hasSpeedDevil = boolData[1];
	}

	public override void aiAttack(Actor? target) {
		int vattack = Helpers.randomRange(1, 7);
		bool isFacingTarget = (pos.x * xDir < target?.pos.x * xDir);

		if (canShoot() && charState.attackCtrl && aiAttackCooldown <= 0) {
			if (isVileMK2 && charState is Dash or AirDash && isFacingTarget) {
				player.press(Control.Special1);
				aiAttackCooldown = 20;
				return;
			}
			if (isFacingTarget && canTurn() && charState.normalCtrl) {
				if (xDir == 1) {
					player.press(Control.Left);
				} else {
					player.press(Control.Right);
				}
			}

			bool shotWeapon = weaponSystem.shootRandomWeapon(this);
			if (shotWeapon) {
				aiAttackCooldown = 20;
			}
		}
	}

	public override void aiUpdate(Actor? target) {
		base.aiUpdate(target);
		if (!player.isMainPlayer) {
			if (player.canReviveVile() && isVileMK1) {
				player.reviveVile(false);
			}
			if (isVileMK2 && player.canReviveVile()) {
				player.reviveVile(true);
			}
		}
		if (!player.isMainPlayer) {
			if (player.currency >= 3 && !player.frozenCastle) {
				player.frozenCastle = true;
				hasFrozenCastle = true;
				player.currency -= Vile.frozenCastleCost;
			}
			if (player.currency >= 3 && !player.speedDevil) {
				player.speedDevil = true;
				hasSpeedDevil = true;
				player.currency -= Vile.speedDevilCost;
			}
		}
	}
}


public class VileAmmoWeapon : Weapon {
	public VileAmmoWeapon() { 
		index = (int)WeaponIds.VileLaser;
		weaponSlotIndex = 32;
		weaponBarBaseIndex = 39;
		weaponBarIndex = 32;
		allowSmallBar = true;
		drawRoundedDown = true;

		maxAmmo = 32;
		ammo = maxAmmo;
		drawCooldown = false;
	}
}
