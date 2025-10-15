using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MMXOnline;

public class Vile : Character {
	public const float maxCalldownMechCooldown = 2;
	public float grabCooldown = 1;
	public float vulcanLingerTime;
	public const int callNewMechCost = 5;
	public float mechBusterCooldown;
	public bool usedAmmoLastFrame;
	public int buckshotDanceNum;
	public bool isShootingGizmo;
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
	public VileCannon cannonWeapon;
	public VileLoadout loadout;
	public VileVulcan vulcanWeapon;
	public VileMissile missileWeapon;
	public RocketPunch rocketPunchWeapon;
	public VileNapalm napalmWeapon;
	public VileBall grenadeWeapon;
	public VileCutter cutterWeapon;
	public VileFlamethrower flamethrowerWeapon;
	public VileLaser laserWeapon;
	public MechMenuWeapon rideMenuWeapon;
	public Weapon downAirSpWeapon;
	public Weapon airSpWeapon;
	public Weapon downSpWeapon;

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

		vulcanWeapon = loadout.vulcan switch {
			1 => new DistanceNeedler(),
			2 => new BuckshotDance(),
			3 => new NoneVulcan(),
			_ => new CherryBlast()
		};
		cannonWeapon = loadout.cannon switch {
			1 => new FatBoy(),
			2 => new LongShotGizmo(),
			3 => new NoneCannon(),
			_ => new FrontRunner()
		};
		missileWeapon = loadout.missile switch {
			1 => new HumerusCrush(),
			2 => new PopcornDemon(),
			3 => new NoneMissile(),
			_ => new ElectricShock()
		};
		rocketPunchWeapon = loadout.rocketPunch switch {
			1 => new SpoiledBrat(),
			2 => new InfinityGig(),
			3 => new NoneRocketPunch(),
			_ => new GoGetterRight()
		};
		napalmWeapon = loadout.napalm switch {
			1 => new FireGrenade(),
			2 => new SplashHit(),
			3 => new NoneNapalm(),
			_ => new RumblingBang()
		};
		grenadeWeapon = loadout.ball switch {
			1 => new SpreadShot(),
			2 => new PeaceOutRoller(),
			3 => new NoneBall(),
			_ => new ExplosiveRound()
		};
		cutterWeapon = loadout.cutter switch {
			1 => new ParasiteSword(),
			2 => new MaroonedTomahawk(),
			3 => new NoneCutter(),
			_ => new QuickHomesick()
		};
		flamethrowerWeapon = loadout.flamethrower switch {
			1 => new SeaDragonRage(),
			2 => new DragonsWrath(),
			3 => new NoneFlamethrower(),
			_ => new WildHorseKick()
		};
		downSpWeapon = loadout.downSpWeapon switch {
			0 => napalmWeapon,
			1 => grenadeWeapon,
			2 => flamethrowerWeapon,
			_ => napalmWeapon,
		};
		airSpWeapon = loadout.airSpWeapon switch {
			0 => napalmWeapon,
			1 => grenadeWeapon,
			2 => flamethrowerWeapon,
			_ => napalmWeapon,
		};
		downAirSpWeapon = loadout.downAirSpWeapon switch {
			0 => napalmWeapon,
			1 => grenadeWeapon,
			2 => flamethrowerWeapon,
			_ => napalmWeapon,
		};
		laserWeapon = loadout.laser switch {
			1 => new NecroBurst(),
			2 => new StraightNightmare(),
			3 => new NoneLaser(),
			_ => new RisingSpecter()
		};
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
		
		if (!isShootingGizmo && !isShootingVulcan) energy.addAmmo(0.25f * speedMul, player);
		if (isShootingVulcan && sprite.name.EndsWith("shoot"))
			changeSpriteFromName(charState.shootSpriteEx, false);
		else changeSpriteFromName(charState.sprite, resetFrame: false);

		Helpers.decrementTime(ref calldownMechCooldown);
		Helpers.decrementTime(ref grabCooldown);
		Helpers.decrementTime(ref mechBusterCooldown);
		Helpers.decrementFrames(ref aiAttackCooldown);
		Helpers.decrementFrames(ref vulcanLingerTime);
		addWeaponHealAmmo();

		if ((grounded || charState is LadderClimb or LadderEnd or WallSlide) && vileHoverTime > 0) {
			vileHoverTime -= Global.spf * 6;
			if (vileHoverTime < 0) vileHoverTime = 0;
		}
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;
		
		cannonWeapon.update();
		vulcanWeapon.update();
		missileWeapon.update();
		rocketPunchWeapon.update();
		napalmWeapon.update();
		grenadeWeapon.update();
		cutterWeapon.update();
		laserWeapon.update();
		flamethrowerWeapon.update();

		RideArmorAttacks();
		RideLinkMK5();

		// GMTODO: Consider a better way here instead of a hard-coded deny list
		// Gacel: Done, now it uses attackCtrl
		
		if (!charState.attackCtrl || charState is VileMK2GrabState) {
			chargeLogic(null);
		} else {
			chargeLogic(shoot);
		}
	}

	public override bool attackCtrl() {
		// First test the special weapons.
		if (dashGrabSpecial() ||
			airDownAttacks() ||
			normalAttacks()
		) {
			return true;
		}
		// Get input.
		bool shootHeld = player.input.isHeld(Control.Shoot, player);
		bool weaponRightHeld = (
			player.input.isHeld(Control.WeaponRight, player) && 
			(!isATrans || !player.input.isHeld(Control.Up, player))
		);

		if (shootHeld && cannonWeapon.type > -1) {
			if (cannonWeapon.shootCooldown < cannonWeapon.fireRate * 0.75f) {
				cannonWeapon.vileShoot(0, this);
				return true;
			}
		}
		if (weaponRightHeld && vulcanWeapon.type > -3) {
			vulcanWeapon.vileShoot(0, this);
			return true;
		}
		return base.attackCtrl();
	}

	public bool normalAttacks() {
		if (!grounded) {
			return false;
		}
		if (!player.input.isPressed(Control.Special1, player)) {
			return false;
		}
		bool leftorRightHeld = player.input.getXDir(player) != 0;
		bool upHeld = player.input.getYDir(player) == -1;
		bool downHeld = player.input.getYDir(player) == 1;
		if (downHeld) {
			downSpWeapon.vileShoot(WeaponIds.Napalm, this);
			return true;
		}
		if (upHeld) {
			cutterWeapon.vileShoot(WeaponIds.VileCutter, this);
			return true;
		}
		if (leftorRightHeld) {
			rocketPunchWeapon.vileShoot(0, this);
			return true;
		}
		missileWeapon.vileShoot(WeaponIds.ElectricShock, this);
		return true;
	}

	public bool airDownAttacks() {
		if (grounded) {
			return false;
		}
		if (!player.input.isPressed(Control.Special1, player)) {
			return false;
		}
		bool heldDown = player.input.getYDir(player) == 1;
		if (heldDown) {
			downAirSpWeapon.vileShoot(WeaponIds.VileFlamethrower, this);
			return true;
		}
		airSpWeapon.vileShoot(WeaponIds.VileBomb, this);
		return true;
	}

	public bool dashGrabSpecial() {
		if (!player.input.isPressed(Control.Special1, player)) {
			return false;
		}
		if (charState is Dash or AirDash && isVileMK2) {
			charState.isGrabbing = true;
			charState.superArmor = true; //peakbalance
			changeSpriteFromName("dash_grab", true);
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
		bool raStates = rideArmor?.rideArmorState is RAIdle or RAJump or RAFall or RADash;
		if (rideArmor != null && raState != null && !raState.isHiding) {
			if (raStates) {
				if (Goliath && Options.main.swapGoliathInputs) {
					bool oldStunShotPressed = stunShotPressed;
					stunShotPressed = goliathShotPressed;
					goliathShotPressed = oldStunShotPressed;
				}
				if (stunShotPressed && !HeldDown && missileWeapon.shootCooldown <= 0) {
					missileWeapon.vileShoot(WeaponIds.ElectricShock, this);
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
		if (currentWeapon is AssassinBulletChar) return player.input.isHeld(Control.Up, player);
		return player.input.isHeld(Control.Special1, player);
	}
	public override bool canCharge() {
		return !isInvulnerable(true) && charState is not Die && invulnTime == 0 && energy.ammo >= laserWeapon.getAmmoUsage(0);
	}
	public override int getMaxChargeLevel() {
		return isVileMK5 ? 4 : 3;
	}
	public override bool canShoot() {
		if (isInvulnerableAttack()) return false;
		if (invulnTime > 0) return false;
		return base.canShoot();
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
			// Ride Menu
		} else if (!oldATrans &&
			player.input.isPressed(Control.Special2, player) &&
			!player.input.isHeld(Control.Down, player)
		) {
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
	public float aiAttackCooldown;
	public override void aiAttack(Actor? target) {
		int Vattack = Helpers.randomRange(1, 7);
		bool isFacingTarget = (pos.x < target?.pos.x && xDir == 1) || (pos.x >= target?.pos.x && xDir == -1);
		if (!charState.isGrabbedState && !player.isDead && !isInvulnerableAttack()
			&& !(charState is VileRevive or HexaInvoluteState or LaserAttack or VileMK2GrabState 
			or GenericStun or Hurt or Die) && aiAttackCooldown <= 0) {
			if (isVileMK2 && charState is Dash or AirDash && isFacingTarget) {
				player.press(Control.Special1);
			}
			switch (Vattack) {
				case 1 when isFacingTarget:
					cannonWeapon.vileShoot(WeaponIds.FrontRunner, this);
					break;
				case 2 when isFacingTarget:
					rocketPunchWeapon.vileShoot(WeaponIds.RocketPunch, this);
					break;
				case 3 when !grounded:
					grenadeWeapon.vileShoot(WeaponIds.VileBomb, this);
					break;
				case 4 when isFacingTarget:
					missileWeapon.vileShoot(WeaponIds.ElectricShock, this);
					break;
				case 5 when isFacingTarget:
					cutterWeapon.vileShoot(WeaponIds.VileCutter, this);
					break;
				case 6 when grounded:
					napalmWeapon.vileShoot(WeaponIds.Napalm, this);
					break;
				case 7 when charState is Fall:
					flamethrowerWeapon.vileShoot(WeaponIds.VileFlamethrower, this);
					break;
			}
			aiAttackCooldown = 20;
		}
		base.aiAttack(target);
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
