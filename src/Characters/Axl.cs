using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;
using SFML.Window;

namespace MMXOnline;

public class RaycastHitData {
	public List<IDamagable> hitGos = new List<IDamagable>();
	public Point hitPos;
	public bool isHeadshot;
}

public class Axl : Character {
	public float stingChargeTime;
	public int lastXDir;
	public float shootTime {
		get { return currentWeapon?.shootCooldown ?? 0; }
		set { if (currentWeapon != null) { currentWeapon.shootCooldown = value; } }
	}
	public bool aiming;
	public IDamagable? axlCursorTarget = null;
	public Character? axlHeadshotTarget = null;
	public Anim muzzleFlash;
	public float recoilTime;
	public float axlSwapTime;
	public float axlAltSwapTime;
	public float switchTime;
	public float altSwitchTime;
	public float netArmAngle;
	float targetSoundCooldown;
	public Point nonOwnerAxlBulletPos;
	public float stealthRevealTime;

	public bool aimBackwardsToggle;
	public bool positionLockToggle;
	public bool cursorLockToggle;
	public void resetToggle() {
		aimBackwardsToggle = false;
		positionLockToggle = false;
		cursorLockToggle = false;
	}

	public bool isNonOwnerZoom;
	public Point nonOwnerScopeStartPos;
	public Point nonOwnerScopeEndPos;
	public Point? netNonOwnerScopeEndPos;
	private bool _zoom;
	public bool isZoomingIn;
	public bool isZoomingOut;
	public bool isZoomOutPhase1Done;
	public float zoomCharge;
	public float savedCamX;
	public float savedCamY;
	public bool hyperAxlStillZoomed;

	public float revTime;
	public float revIndex;
	public bool aimingBackwards;
	public LoopingSound? iceGattlingLoop;
	public bool isRevving;
	public bool isNonOwnerRev;
	public SniperMissileProj? sniperMissileProj;
	public AxlLoadout loadout;
	public LoopingSound iceGattlingSound;
	public float whiteAxlTime;
	public float dodgeRollCooldown;
	public const float maxDodgeRollCooldown = 1.5f;
	public bool hyperAxlUsed;
	public bool hyperAxlFix;
	//public ShaderWrapper axlPaletteShader;
	public float maxHyperAxlTime = 30;
	public List<int> ammoUsages = new List<int>();

	public RayGunAltProj? rayGunAltProj;
	public GaeaShieldProj? gaeaShield;

	// Cursor stuff.
	public Point axlCursorPos;
	public Point? assassinCursorPos;
	public Point axlCursorWorldPos => axlCursorPos.addxy(Global.level.camX, Global.level.camY);
	public Point axlScopeCursorWorldPos;
	public Point axlScopeCursorWorldLerpPos;
	public Point axlZoomOutCursorDestPos;
	public Point axlLockOnCursorPos;
	public Point axlGenericCursorWorldPos {
		get {
			if (!isZooming() || isZoomingIn || isZoomOutPhase1Done) {
				return axlCursorWorldPos;
			}
			return axlScopeCursorWorldPos;
		}
	}
	public AxlWeapon? axlWeapon => currentWeapon as AxlWeapon;

	// Used to be 0.5, 100
	public const float maxStealthRevealTime = 0.5f;
	// The ping divided by this number indicates stealth reveal time in online
	public const float stealthRevealPingDenom = 100;

	public PlasmaGunAltProj? plasmaGunAltProj;

	public bool shouldDrawArmNet;
	public bool stealthActive;

	public int axlHyperMode;
	bool whiteAxlLoadout => axlHyperMode == 0;
	bool jumpPressed => player.input.isPressed(Control.Jump, player);
	bool dashPressed => player.input.isPressed(Control.Dash, player);
	bool commandHeld => player.input.isHeld(Control.Special2, player);

	public float[] rshootDebuffTime = [0, 0, 0];
	public float[] rshootDebuffAmmount = [0, 0, 0];

	public Axl(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, AxlLoadout? loadout = null,
		int? heartTanks = null, bool isATrans = false
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn, heartTanks, isATrans
	) {
		charId = CharIds.Axl;
		loadout ??= player.loadout.axlLoadout.clone();
		this.loadout = loadout;
		iceGattlingSound = new LoopingSound("iceGattlingLoopStart", "iceGattlingLoopStop", "iceGattlingLoop", this);

		muzzleFlash = new Anim(new Point(), "axl_pistol_flash", xDir, null, false) {
			visible = false
		};
		axlHyperMode = loadout.hyperMode;

		configureWeapons(loadout);
		altSoundId = AltSoundIds.X3;
	}

	public override CharState getTauntState() => new AxlTaunt();

	public void zoomIn() {
		if (isZoomingIn) return;
		if (_zoom) return;

		_zoom = true;
		if (isWhiteAxl()) hyperAxlStillZoomed = true;
		axlCursorPos.x = Helpers.clamp(axlCursorPos.x, 0, Global.viewScreenW);
		axlCursorPos.y = Helpers.clamp(axlCursorPos.y, 0, Global.viewScreenH);
		savedCamX = Global.level.camX;
		savedCamY = Global.level.camY;
		axlScopeCursorWorldPos = getCamCenterPos(ignoreZoom: true);
		axlScopeCursorWorldLerpPos = axlCursorWorldPos;
		isZoomingIn = true;
		isZoomingOut = false;
	}

	public void zoomOut() {
		if (isZoomingOut) return;
		if (!_zoom) return;

		zoomCharge = 0;
		axlZoomOutCursorDestPos = getCamCenterPos(ignoreZoom: true);
		axlCursorPos = getAxlBulletPos().add(getAxlBulletDir().times(50)).addxy(-savedCamX, -savedCamY);

		isZoomingOut = true;
		isZoomingIn = false;
	}

	public bool isZooming() {
		return _zoom;
	}

	public bool isAnyZoom() {
		return isZooming() || isZoomingOut || isZoomingIn;
	}

	public bool hasScopedTarget() {
		if (isZoomingOut || isZoomingIn) return false;
		if (axlCursorTarget == null && axlHeadshotTarget == null) return false;
		var hitData = getFirstHitPos(player.adjustedZoomRange, ignoreDamagables: true);
		if (axlCursorTarget != null && axlHeadshotTarget != null) {
			if (hitData.hitGos.Contains(axlCursorTarget) || hitData.hitGos.Contains(axlHeadshotTarget)) {
				return true;
			}
		}
		return false;
	}

	public RaycastHitData getFirstHitPos(float range, float backOffDist = 0, bool ignoreDamagables = false) {
		var retData = new RaycastHitData();
		Point bulletPos = getAxlBulletPos();
		Point bulletDir = getAxlBulletDir();

		Point maxPos = bulletPos.add(bulletDir.times(range));

		List<CollideData> hits = Global.level.raycastAll(bulletPos, maxPos, new List<Type>() { typeof(Actor), typeof(Wall) });

		CollideData? hit = null;

		foreach (var p in Global.level.players) {
			if (p.character == null || p.character.getHeadPos() == null) continue;
			Rect headRect = p.character.getHeadRect();

			Point startTestPoint = bulletPos.add(bulletDir.times(-range * 2));
			Point endTestPoint = bulletPos.add(bulletDir.times(range * 2));
			Line testLine = new Line(startTestPoint, endTestPoint);
			Shape headShape = headRect.getShape();
			List<CollideData> lineIntersections = headShape.getLineIntersectCollisions(testLine);
			if (lineIntersections.Count > 0) {
				hits.Add(new CollideData(null, p.character.globalCollider, bulletDir, false, p.character, new HitData(null, new List<Point>() { lineIntersections[0].getHitPointSafe() })));
			}
		}

		hits.Sort((cd1, cd2) => {
			float d1 = bulletPos.distanceTo(cd1.getHitPointSafe());
			float d2 = bulletPos.distanceTo(cd2.getHitPointSafe());
			if (d1 < d2) return -1;
			else if (d1 > d2) return 1;
			else return 0;
		});

		foreach (var h in hits) {
			if (h.gameObject is Wall) {
				hit = h;
				break;
			}
			if (h.gameObject is IDamagable damagable && damagable.canBeDamaged(player.alliance, player.id, null)) {
				retData.hitGos.Add(damagable);
				if (h.gameObject is Character c) {
					if (c.isAlwaysHeadshot()) {
						retData.isHeadshot = true;
					}
					// Detect headshots
					else if (h?.hitData?.hitPoint != null && c.getHeadPos() != null) {
						Point headPos = c.getHeadPos()!.Value;
						Rect headRect = c.getHeadRect();

						Point hitPoint = h.hitData.hitPoint.Value;
						// Bullet position inside head rect
						if (headRect.containsPoint(bulletPos)) {
							hitPoint = bulletPos;
						}

						float xLeeway = c.headshotRadius * 5f;
						float yLeeway = c.headshotRadius;

						float xDist = MathF.Abs(hitPoint.x - headPos.x);
						float yDist = MathF.Abs(hitPoint.y - headPos.y);

						if (xDist < xLeeway && yDist < yLeeway) {
							Point startTestPoint = bulletPos.add(bulletDir.times(-range * 2));
							Point endTestPoint = bulletPos.add(bulletDir.times(range * 2));
							Line testLine = new Line(startTestPoint, endTestPoint);
							Shape headShape = headRect.getShape();
							List<CollideData> lineIntersections = headShape.getLineIntersectCollisions(testLine);
							if (lineIntersections.Count > 0) {
								retData.isHeadshot = true;
							}
						}
					}
				}
				if (ignoreDamagables == false) {
					hit = h;
					break;
				}
			}
		}

		Point targetPos = hit?.hitData?.hitPoint ?? maxPos;
		if (backOffDist > 0) {
			retData.hitPos = bulletPos.add(bulletPos.directionTo(targetPos).unitInc(-backOffDist));
		} else {
			retData.hitPos = targetPos;
		}

		return retData;
	}

	public Point getDoubleBulletArmPos() {
		if (sprite.name == "axl_dash") {
			return new Point(-7, -2);
		}
		if (sprite.name == "axl_run") {
			return new Point(-7, 1);
		}
		if (sprite.name == "axl_jump" || sprite.name == "axl_fall_start" || sprite.name == "axl_fall" || sprite.name == "axl_hover") {
			return new Point(-7, 0);
		}
		return new Point(-5, 2);
	}

	public int axlXDir {
		get {
			if (sprite.name.Contains("wall_slide")) return -xDir;
			return xDir;
		}
	}

	public bool canChangeDir() {
		return !(charState is InRideArmor) && !(charState is Die) && !(charState is GenericStun);
	}

	float lastAltShootPressedTime;
	float voltTornadoTime;


	public override void preUpdate() {
		lastXDir = xDir;
		base.preUpdate();
	}

	public override void update() {
		base.update();

		if (whiteAxlTime > 0) {
			whiteAxlTime -= Global.spf;
			if (whiteAxlTime < 0) {
				whiteAxlTime = 0;
				if (ownedByLocalPlayer) {
					weapons[0] = new AxlBullet();
				}
			}
		}

		if (stealthActive) {
			addRenderEffect(RenderEffectType.StealthModeBlue);
		} else {
			removeRenderEffect(RenderEffectType.StealthModeBlue);
		}

		// Cutoff point for things not controlled by the local player.
		if (!ownedByLocalPlayer) {
			if (isNonOwnerRev) {
				iceGattlingSound.play();
				revTime += Global.spf;
				if (revTime > 1) revTime = 1;
			} else {
				if (!iceGattlingSound.destroyed) {
					iceGattlingSound.stopRev(revTime);
				}
				Helpers.decrementTime(ref revTime);
			}
			if (netNonOwnerScopeEndPos != null) {
				float frameSmooth = Global.tickRate;
				if (frameSmooth < 2) {
					frameSmooth = 2;
				}
				var incPos = netNonOwnerScopeEndPos.Value.subtract(nonOwnerScopeEndPos).times(1f / frameSmooth);
				var framePos = nonOwnerScopeEndPos.add(incPos);
				if (nonOwnerScopeEndPos.distanceTo(framePos) > float.Epsilon) {
					nonOwnerScopeEndPos = framePos;
				}
			}
			return;
		}

		if (linkedRideArmor != null &&
			player.input.isHeld(Control.Down, player) &&
			player.input.isHeld(Control.Special2, player) &&
			linkedRideArmor.rideArmorState is not RACalldown
		) {
			linkedRideArmor.changeState(new RACalldown(pos, false), true);
			linkedRideArmor.xDir = xDir;
		}

		if (stingChargeTime > 0) {
			stingChargeTime -= Global.spf;

			if (stingChargeTime <= 0) {
				player.delaySubtank();
				enterCombat();
				stingChargeTime = 0;
			}
		}

		isRevving = false;
		bool altRayGunHeld = false;
		bool altPlasmaGunHeld = false;
		bool gaeaHeld = false;

		if (isStealthMode()) {
			updateStealthMode();
			stealthActive = true;
		} else {
			stealthActive = false;
		}

		if (isZooming() && deltaPos.magnitude > 1) {
			zoomOut();
		}

		if (isZooming() && !isZoomingIn && !isZoomingOut) {
			zoomCharge += Global.spf * 0.5f;
			if (isWhiteAxl()) zoomCharge = 1;
			if (zoomCharge > 1) zoomCharge = 1;
		}
		if (assassinTime > 0) {
			stopMovingS();
			useGravity = false;
			Helpers.decrementFrames(ref assassinTime);
			if (assassinTime <= 0) {
				useGravity = true;
				return;
			}
		}
		if (targetSoundCooldown > 0) targetSoundCooldown += Global.spf;
		if (targetSoundCooldown >= 1) targetSoundCooldown = 0;
		Helpers.decrementTime(ref dodgeRollCooldown);
		Helpers.decrementTime(ref axlSwapTime);
		Helpers.decrementTime(ref axlAltSwapTime);
		Helpers.decrementTime(ref switchTime);
		Helpers.decrementTime(ref altSwitchTime);
		Helpers.decrementTime(ref revTime);
		Helpers.decrementTime(ref voltTornadoTime);
		Helpers.decrementTime(ref stealthRevealTime);

		if (currentWeapon != null && currentWeapon.ammo >= currentWeapon.maxAmmo) {
			weaponHealAmount = 0;
		}
		if (currentWeapon != null && weaponHealAmount > 0 && alive) {
			weaponHealTime += Global.spf;
			if (weaponHealTime > 0.05) {
				weaponHealTime = 0;
				weaponHealAmount--;
				currentWeapon.ammo = Helpers.clampMax(currentWeapon.ammo + 1, currentWeapon.maxAmmo);
				playSound("healX3", forcePlay: true, true);
			}
		}

		player.changeWeaponControls();
		updateAxlAim();

		sprite.reversed = false;
		if (axlWeapon != null && (axlWeapon.isTwoHanded(false) || isZooming()) && canChangeDir() && charState is not WallSlide) {
			int newXDir = (pos.x > axlGenericCursorWorldPos.x ? -1 : 1);
			if (charState is Run && xDir != newXDir) {
				sprite.reversed = true;
			}
			xDir = newXDir;
		}

		var axlBullet = weapons.FirstOrDefault(w => w is AxlBullet) as AxlBullet;

		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool shootHeld = player.input.isHeld(Control.Shoot, player);
		bool altShootPressed = player.input.isPressed(Control.Special1, player);
		bool altShootHeld = player.input.isHeld(Control.Special1, player);
		bool altShootRecentlyPressed = false;

		if (!player.isAI) {
			shootPressed = shootPressed || Input.isMousePressed(Mouse.Button.Left, player.canControl);
			shootHeld = shootHeld || Input.isMouseHeld(Mouse.Button.Left, player.canControl);
			altShootPressed = altShootPressed || Input.isMousePressed(Mouse.Button.Right, player.canControl);
			altShootHeld = altShootHeld || Input.isMouseHeld(Mouse.Button.Right, player.canControl);
		}

		if (altShootPressed) {
			lastAltShootPressedTime = Global.time;
		} else {
			altShootRecentlyPressed = Global.time - lastAltShootPressedTime < 0.1f;
		}

		if (isInvulnerableAttack() || isWarpIn()) {
			if (charState is not DodgeRoll) {
				shootPressed = false;
				shootHeld = false;
				altShootPressed = false;
				altShootHeld = false;
				altShootRecentlyPressed = false;
			}
		}

		if (axlSwapTime > 0) {
			shootPressed = false;
			shootHeld = false;
		}
		if (axlAltSwapTime > 0) {
			altShootPressed = false;
			altShootHeld = false;
			altShootRecentlyPressed = false;
		}
		if (currentWeapon is DoubleBullet && Global.level.isHyperMatch() && player.axlBulletTypeAmmo[4] < 99) {
			player.axlBulletTypeAmmo[4] += 1;
		}
		bool bothHeld = shootHeld && altShootHeld;

		if (currentWeapon is AxlBullet || currentWeapon is DoubleBullet ||
			currentWeapon is MettaurCrash || currentWeapon is BeastKiller || currentWeapon is MachineBullets ||
			currentWeapon is RevolverBarrel || currentWeapon is AncientGun) {
			(currentWeapon as AxlWeapon)?.rechargeAxlBulletAmmo(player, this, shootHeld, 1);
		} else {
			foreach (var weapon in weapons) {
				if (weapon is AxlBullet || weapon is DoubleBullet ||
					weapon is MettaurCrash || weapon is BeastKiller || weapon is MachineBullets ||
					weapon is RevolverBarrel || weapon is AncientGun) {
					(weapon as AxlWeapon)?.rechargeAxlBulletAmmo(player, this, shootHeld, 2);
				}
			}
		}
		customSettingReloadWeapon();
		if (weapons.Count > 0 && weapons[0].type > 0) {
			player.axlBulletTypeLastAmmo[weapons[0].type] = weapons[0].ammo;
		}

		if (currentWeapon is not AssassinBulletChar) {
			if (altShootHeld && !bothHeld && (currentWeapon is AxlBullet || currentWeapon is DoubleBullet ||
			currentWeapon is MettaurCrash || currentWeapon is BeastKiller || currentWeapon is MachineBullets ||
			currentWeapon is RevolverBarrel || currentWeapon is AncientGun) && invulnTime == 0 && flag == null) {
				increaseCharge();
			} else if (isCharging()) {
				if (currentWeapon is AxlBullet || currentWeapon is DoubleBullet ||
					currentWeapon is MettaurCrash || currentWeapon is BeastKiller || currentWeapon is MachineBullets ||
					currentWeapon is RevolverBarrel || currentWeapon is AncientGun) {
					recoilTime = 0.2f;
					if (!isWhiteAxl()) {
						axlWeapon?.axlShoot(player, AxlBulletType.AltFire);
						if (axlWeapon != null) {
							afterAxlShoot(axlWeapon);
						}
					} else {
						axlWeapon?.axlShoot(player, AxlBulletType.WhiteAxlCopyShot2);
						if (axlWeapon != null) {
							afterAxlShoot(axlWeapon);
						}
					}
				}
				stopCharge();
				if (shootHeld) {
					increaseCharge();
				} else {
					if (isCharging()) {
						shootAssassinShot();
					}
					stopCharge();
				}
			}
		}
		chargeGfx();
		bool weCanShoot = (undisguiseTime == 0 && assassinTime == 0);
		if (weCanShoot) {
			// Axl bullet
			if (!isCharging() && axlWeapon != null) {
				if (currentWeapon is AxlBullet && canShoot() && !currentWeapon.noAmmo()) {
					if (shootHeld && shootTime == 0 && currentWeapon.altShotCooldown == 0) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player);
						afterAxlShoot(axlWeapon);
					} else if ((altShootPressed || altShootRecentlyPressed) && shootTime == 0 && currentWeapon.altShotCooldown == 0 && currentWeapon.ammo >= 4) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player, AxlBulletType.AltFire);
						afterAxlShoot(axlWeapon);
					}
				}
				switch (currentWeapon) {
					case MettaurCrash:
					case BeastKiller:
					case MachineBullets:
					case RevolverBarrel:
					case AncientGun:
						if (canShoot() && !currentWeapon.noAmmo()) {
							if (shootHeld && shootTime == 0 && currentWeapon.altShotCooldown == 0) {
								recoilTime = 0.2f;
								axlWeapon.axlShoot(player);
								afterAxlShoot(axlWeapon);
							} else if ((altShootPressed || altShootRecentlyPressed) && shootTime == 0 && currentWeapon.altShotCooldown == 0 && currentWeapon.ammo >= 4) {
								recoilTime = 0.2f;
								axlWeapon.axlShoot(player, AxlBulletType.AltFire);
								afterAxlShoot(axlWeapon);
							}
						}
						break;
				}
				// Double bullet
				if (currentWeapon is DoubleBullet && canShoot() && !currentWeapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player);
						afterAxlShoot(axlWeapon);
						if (bothHeld) axlWeapon.shootCooldown *= 2f;
					}
					if (bothHeld && currentWeapon.altShotCooldown == 0) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player, AxlBulletType.AltFire);
						afterAxlShoot(axlWeapon);
						if (bothHeld) axlWeapon.altShotCooldown *= 2f;
					} else if ((altShootPressed || altShootRecentlyPressed) && shootTime == 0 && currentWeapon.altShotCooldown == 0 && currentWeapon.ammo >= 4) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player, AxlBulletType.AltFire);
						afterAxlShoot(axlWeapon);
					}
				}

				if (currentWeapon is BlastLauncher && canShoot()) {
					if (shootHeld && shootTime == 0 && currentWeapon.ammo >= 1) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player);
						afterAxlShoot(axlWeapon);
					}

					if (loadout.blastLauncherAlt == 0) {
						if (altShootPressed && shootTime == 0 && currentWeapon.altShotCooldown == 0 && currentWeapon.ammo >= 1) {
							recoilTime = 0.2f;
							axlWeapon.axlShoot(player, AxlBulletType.AltFire);
							afterAxlShoot(axlWeapon);
						}
					} else {
						if (altShootPressed && player.grenades.Count > 0) {
							foreach (var grenade in player.grenades) {
								grenade.detonate();
							}
							player.grenades.Clear();
						}
					}
				}

				if (currentWeapon is RayGun && canShoot() && !currentWeapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player);
						afterAxlShoot(axlWeapon);
					} else if (altShootHeld) {
						if (shootTime == 0) {
							recoilTime = 0.2f;
							axlWeapon.axlShoot(player, AxlBulletType.AltFire);
							afterAxlShoot(axlWeapon);
						}
						altRayGunHeld = axlWeapon.ammo > 0;

						if (loadout.rayGunAlt == 0) {
							Point bulletDir = getAxlBulletDir();
							float whiteAxlMod = isWhiteAxl() ? 2 : 1;
							move(bulletDir.times(-50 * whiteAxlMod));
						}
					}
				}

				if (currentWeapon is BlackArrow && canShoot() && !currentWeapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player);
						afterAxlShoot(axlWeapon);
					} else if (altShootHeld && shootTime == 0 && currentWeapon.altShotCooldown == 0) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player, AxlBulletType.AltFire);
						afterAxlShoot(axlWeapon);
					}
				}

				if (currentWeapon is SpiralMagnum && canShoot()) {
					if (shootHeld && shootTime == 0) {
						if (!currentWeapon.noAmmo()) {
							recoilTime = 0.2f;
							axlWeapon.axlShoot(player);
							afterAxlShoot(axlWeapon);
						}
					} else {
						if (loadout.spiralMagnumAlt == 0) {
							if (altShootPressed && axlWeapon.ammo > 0 && shootTime == 0 && currentWeapon.altShotCooldown == 0) {
								recoilTime = 0.2f;
								axlWeapon.axlShoot(player, AxlBulletType.AltFire);
								afterAxlShoot(axlWeapon);
							}
						} else {
							if (altShootPressed && (charState is Idle || charState is Crouch)) {
								if (!_zoom) {
									zoomIn();
								} else if (!isZoomingIn && !isZoomingOut) {
									zoomOut();
								}
							}
						}
					}
				}

				if (currentWeapon is BoundBlaster && canShoot() && !currentWeapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player);
						afterAxlShoot(axlWeapon);
					} else if (altShootHeld && shootTime == 0 && currentWeapon.altShotCooldown == 0) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player, AxlBulletType.AltFire);
						afterAxlShoot(axlWeapon);
					}
				}

				if (currentWeapon is PlasmaGun && canShoot() && !currentWeapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						axlWeapon.altShotCooldown = axlWeapon.altFireCooldown;
						axlWeapon.axlShoot(player);
						afterAxlShoot(axlWeapon);
					} else if (altShootHeld) {
						if (loadout.plasmaGunAlt == 0) {
							if (axlWeapon.altShotCooldown == 0 && grounded) {
								recoilTime = 0.2f;
								voltTornadoTime = 0.2f;
								axlWeapon.axlShoot(player, AxlBulletType.AltFire);
								afterAxlShoot(axlWeapon);
							}
						} else {
							if (axlWeapon.altShotCooldown == 0) {
								recoilTime = 0.2f;
								axlWeapon.axlShoot(player, AxlBulletType.AltFire);
								afterAxlShoot(axlWeapon);
							}
							altPlasmaGunHeld = axlWeapon.ammo > 0;
						}
					}
				}

				if (currentWeapon is IceGattling && canShoot() && currentWeapon.ammo > 0) {
					if (altShootPressed && loadout.iceGattlingAlt == 0 && gaeaShield == null) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player, AxlBulletType.AltFire);
						afterAxlShoot(axlWeapon);
					}
					gaeaHeld = true;
					bool isAltRev = (altShootHeld && loadout.iceGattlingAlt == 1);
					if (shootHeld || isAltRev) {
						isRevving = true;
						revTime += Global.spf * 2 * (isWhiteAxl() ? 10 : (isAltRev ? 2 : 1));
						if (revTime > 1) {
							revTime = 1;
						}
					}

					if (shootHeld && shootTime == 0 && revTime >= 1) {
						recoilTime = 0.2f;
						axlWeapon.axlShoot(player);
						afterAxlShoot(axlWeapon);
					}
				}

				if (currentWeapon is FlameBurner && canShoot() && currentWeapon.ammo > 0) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.05f;
						axlWeapon.axlShoot(player);
						afterAxlShoot(axlWeapon);
					}

					if (loadout.flameBurnerAlt == 0) {
						if (altShootHeld && shootTime == 0 && currentWeapon.altShotCooldown == 0) {
							recoilTime = 0.2f;
							axlWeapon.axlShoot(player, AxlBulletType.AltFire);
							axlWeapon.shootCooldown = 30;
							afterAxlShoot(axlWeapon);
						}
					} else {
						if (altShootHeld) {
							if (shootTime == 0 && currentWeapon.altShotCooldown == 0) {
								recoilTime = 0.2f;
								axlWeapon.axlShoot(player, AxlBulletType.AltFire);
								afterAxlShoot(axlWeapon);
							}
						}
					}
				}

				// DNA Core
				if (currentWeapon is DNACore && canShoot()) {
					if (currentWeapon is AxlWeapon realWeapon) {
						if (shootPressed && shootTime == 0) {
							realWeapon.axlShoot(player);
						}
					}
				}
			}
		}

		Helpers.decrementTime(ref recoilTime);

		if (!isRevving && !iceGattlingSound.destroyed) {
			iceGattlingSound.stopRev(revTime);
		} else {
			iceGattlingSound.play();
		}

		if (axlWeapon is IceGattling) {
			if (isRevving) {
				RPC.playerToggle.sendRpc(player.id, RPCToggleType.StartRev);
			} else {
				RPC.playerToggle.sendRpc(player.id, RPCToggleType.StopRev);
			}
		} else {
			if (revTime > 0) {
				RPC.playerToggle.sendRpc(player.id, RPCToggleType.StopRev);
			}
		}

		if (!altRayGunHeld) {
			rayGunAltProj?.destroySelf();
			rayGunAltProj = null;
		}

		if (!altPlasmaGunHeld) {
			plasmaGunAltProj?.destroySelf();
			plasmaGunAltProj = null;
		}
		if (!gaeaHeld) {
			gaeaShield?.destroySelf();
			gaeaShield = null;
		}
	}

	public override bool normalCtrl() {
		if (jumpPressed && canJump() && !grounded &&
		 	!isDashing && canAirDash() && flag == null
		) {
			dashedInAir++;
			changeState(new Hover(), true);
			return true;
		}
		if (dodgeRollCooldown == 0) {
			if (charState is Crouch && dashPressed) {
				changeState(new DodgeRoll(), true);
				return true;
			} else if (dashPressed && player.input.checkDoubleTap(Control.Dash)) {
				changeState(new DodgeRoll(), true);
				return true;
			}
		}
		int cost = Player.AxlHyperCost;
		if (commandHeld && player.currency >= cost && !isWhiteAxl() &&
			charState is not HyperAxlStart and not WarpIn)
		{
			hyperProgress += Global.spf;
		} else {
			hyperProgress = 0;
		}
		if (hyperProgress >= 1 && player.currency >= cost && !isHypermodeAxl()) {
			hyperProgress = 0;
			if (whiteAxlLoadout) {
				changeState(new HyperAxlStart(grounded), true);
			} else {
				if (!hyperAxlFix) {
					foreach (var weapon in weapons)
					weapon.ammo = weapon.maxAmmo;
				}
				hyperAxlFix = true;
				stingChargeTime = 12;
				playSound("stingCharge", sendRpc: true);
			}
		}
		if (isStealthMode() && hyperProgress >= 1) {
			hyperProgress = 0;
			stingChargeTime = 0;
			playSound("stingCharge", sendRpc: true);
		}
		return base.normalCtrl();
	}

	public float getAimBackwardsAmount() {
		// Skip if disabled.
		if (Global.customSettings?.axlBackwardsDebuff == false) {
			return 0;
		}
		// Get angles.
		float forwardAngle = getShootXDir() == 1 ? 0 : 128;
		float bulletAngle = getAxlBulletDir().byteAngle;
		// Calculate angle diference.
		float dist = MathF.Abs(bulletAngle - forwardAngle);
		// Reduce dist by 64 byeangle (90 degrees) and clamp.
		dist -= 128;
		if (dist < 0) { dist = 0; }

		return dist / 128;
	}

	public float getBackwardMoveDebuff() {
		return Helpers.clamp01(getAimBackwardsAmount() * 4);
	}

	public float getShootBackwardsDebuff() {
		return Helpers.clamp01(getAimBackwardsAmount() * 2);
	}

	public void updateAxlAim() {
		if (Global.level.gameMode.isOver || isAnyZoom() || sniperMissileProj != null) {
			resetToggle();
		}

		if (!Global.level.gameMode.isOver) {
			if (isZooming()) {
				if (!isZoomingIn && !isZoomingOut) {
					updateAxlScopePos();
				} else if (isZoomingIn) {
					axlScopeCursorWorldPos = Point.lerp(axlScopeCursorWorldPos, axlScopeCursorWorldLerpPos, Global.spf * 15);
					if (axlScopeCursorWorldPos.distanceTo(axlScopeCursorWorldLerpPos) < 1) {
						axlScopeCursorWorldPos = axlScopeCursorWorldLerpPos;
						isZoomingIn = false;
					}
				} else if (isZoomingOut) {
					Point destPos = axlZoomOutCursorDestPos;
					axlScopeCursorWorldPos = Point.lerp(axlScopeCursorWorldPos, destPos, Global.spf * 15);
					float dist = axlScopeCursorWorldPos.distanceTo(destPos);
					if (dist < 50 && !isZoomOutPhase1Done) {
						//axlCursorPos = axlScopeCursorWorldPos.addxy(-Global.level.camX, -Global.level.camY);
						isZoomOutPhase1Done = true;
					}
					if (dist < 1) {
						axlScopeCursorWorldPos = destPos;
						isZoomingOut = false;
						isZoomOutPhase1Done = false;
						_zoom = false;
						hyperAxlStillZoomed = false;
					}
				}
				return;
			}

			if (player.isAI) {
				updateAxlCursorPos();
			} else if (Options.main.axlAimMode == 2) {
				updateAxlCursorPos();
			} else {
				updateAxlDirectionalAim();
			}
		}
	}

	public void updateAxlDirectionalAim() {
		if (player.input.isCursorLocked(player)) {
			Point worldCursorPos = pos.add(lastDirToCursor);
			axlCursorPos = worldCursorPos.addxy(-Global.level.camX, -Global.level.camY);
			lockOn(out _);
			return;
		}
		if (charState is AssassinateChar) {
			return;
		}
		Point aimDir = new Point(0, 0);

		if (Options.main.aimAnalog) {
			aimDir.x = Input.aimX;
			aimDir.y = Input.aimY;
		}

		bool aimLeft = player.input.isHeld(Control.AimLeft, player);
		bool aimRight = player.input.isHeld(Control.AimRight, player);
		bool aimUp = player.input.isHeld(Control.AimUp, player);
		bool aimDown = (
			player.input.isHeld(Control.AimDown, player) &&
			(Options.main.axlSeparateAimDownAndCrouch ||
			charState is not Idle && charState is not Crouch)
		);

		if (aimDir.magnitude < 10) {
			if (aimLeft) {
				aimDir.x = -100;
			} else if (aimRight) {
				aimDir.x = 100;
			}
			if (aimUp) {
				aimDir.y = -100;
			} else if (aimDown) {
				aimDir.y = 100;
			}
		}

		aimingBackwards = player.input.isAimingBackwards(player);

		int aimBackwardsMod = 1;
		if (aimingBackwards && charState is not LadderClimb) {
			if (axlWeapon?.isTwoHanded(false) != true) {
				if (Math.Sign(aimDir.x) == Math.Sign(xDir)) {
					aimDir.x *= -1;
				}
				aimBackwardsMod = -1;
			} else {
				// By design, aiming backwards with 2-handed weapons does not actually cause Axl to aim backwards like with 1-handed weapons as this would look really weird.
				// Instead, it locks Axl's aim forward and allows him to backpedal without changing direction.
				xDir = lastXDir;
				if (Math.Sign(aimDir.x) != Math.Sign(xDir)) {
					aimDir.x *= -1;
				}
			}
		}

		if (aimDir.magnitude < 10) {
			aimDir = new Point(xDir * 100 * aimBackwardsMod, 0);
		}

		if (charState is WallSlide) {
			if (xDir == -1) {
				if (aimDir.x < 0) aimDir.x *= -1;
			}
			if (xDir == 1) {
				if (aimDir.x > 0) aimDir.x *= -1;
			}
		}

		float xOff = 0;
		float yOff = -24;
		if (charState is Crouch) yOff = -16;

		//axlCursorPos = pos.addxy(xOff * xDir, yOff).addxy(aimDir.x, aimDir.y).addxy(-Global.level.camX, -Global.level.camY);
		Point destCursorPos = pos.addxy(xOff * xDir, yOff).addxy(aimDir.x, aimDir.y).addxy(-Global.level.camX, -Global.level.camY);

		if (charState is Dash || charState is AirDash) {
			destCursorPos = destCursorPos.addxy(15 * xDir, 0);
		}

		// Try to see if where cursor will go to has auto-aim target. If it does, make that the dest, not the actual dest
		Point oldCursorPos = axlCursorPos;
		axlCursorPos = destCursorPos;
		lockOn(out Point? lockOnPoint);
		if (lockOnPoint != null) {
			destCursorPos = lockOnPoint.Value;
		}
		axlCursorPos = oldCursorPos;

		// Lerp to the new target
		//axlCursorPos = Point.moveTo(axlCursorPos, destCursorPos, Global.spf * 1000);
		if (!Options.main.aimAnalog) {
			axlCursorPos = Point.lerp(axlCursorPos, destCursorPos, Global.spf * 15);
		} else {
			axlCursorPos = destCursorPos;
		}

		lastDirToCursor = pos.directionTo(axlCursorWorldPos);
	}

	Point lastDirToCursor;

	public bool canUpdateAimAngle() {
		if (shootTime > 0) return true;
		return !(charState is LadderClimb) && !(charState is LadderEnd) && canChangeDir();
	}

	public Character? getLockOnTarget() {
		Character? newTarget = null;
		foreach (var enemy in Global.level.players) {
			if (enemy.character != null && enemy.character.canBeDamaged(player.alliance, player.id, null) && enemy.character.pos.distanceTo(pos) < 150 && !enemy.character.isStealthy(player.alliance)) {
				float distPercent = 1 - (enemy.character.pos.distanceTo(pos) / 150);
				var dirToEnemy = getAxlBulletPos().directionTo(enemy.character.getAimCenterPos());
				var dirToCursor = getAxlBulletPos().directionTo(axlGenericCursorWorldPos);

				float angle = dirToEnemy.angleWith(dirToCursor);

				float leeway = 22.5f;
				if (angle < leeway + (distPercent * (90 - leeway))) {
					newTarget = enemy.character;
					break;
				}
			}
		}

		return newTarget;
	}

	public void lockOn(out Point? lockOnPoint) {
		// Check for lock on targets
		lockOnPoint = null;
		var prevTarget = axlCursorTarget;
		axlCursorTarget = null;
		axlHeadshotTarget = null;
		assassinCursorPos = null;

		if (!Options.main.lockOnSound) return;
		//This sht was bugging assassin time, i was like +2 hours trying to see whats wrong with it
		//if (player.isDisguisedAxl && !player.isAxl && currentWeapon is not AssassinBulletChar) return;
		if (player.isDisguisedAxl && axlWeapon is UndisguiseWeapon) return;
		if (player.input.isCursorLocked(player)) return;

		axlCursorTarget = getLockOnTarget();

		if (axlCursorTarget != null && prevTarget == null && player.isMainPlayer && targetSoundCooldown == 0) {
			Global.playSound("axlTarget", false);
			targetSoundCooldown = Global.spf;
		}

		if (axlCursorTarget is Character chara) {
			axlLockOnCursorPos = chara.getAimCenterPos();
			lockOnPoint = axlLockOnCursorPos.addxy(-Global.level.camX, -Global.level.camY);
			// axlCursorPos = (axlCursorTarget as Character).getAimCenterPos().addxy(-Global.level.camX, -Global.level.camY);

			
		}
	}

	public void updateAxlScopePos() {
		float aimThreshold = 5;
		bool axisXMoved = false;
		bool axisYMoved = false;
		// Options.main.aimSensitivity is a float from 0 to 1.
		float distFromNormal = Options.main.aimSensitivity - 0.5f;
		float sensitivity = 1;
		if (distFromNormal > 0) {
			sensitivity += distFromNormal * 7.5f;
		} else {
			sensitivity += distFromNormal * 1.75f;
		}

		// Controller joystick axis move section
		if (Input.aimX > aimThreshold && Input.aimX >= Input.lastAimX) {
			axlScopeCursorWorldPos.x += Global.spf * Global.screenW * (Input.aimX / 100f) * sensitivity;
			axisXMoved = true;
		} else if (Input.aimX < -aimThreshold && Input.aimX <= Input.lastAimX) {
			axlScopeCursorWorldPos.x -= Global.spf * Global.screenW * (MathF.Abs(Input.aimX) / 100f) * sensitivity;
			axisXMoved = true;
		}
		if (Input.aimY > aimThreshold && Input.aimY >= Input.lastAimY) {
			axlScopeCursorWorldPos.y += Global.spf * Global.screenW * (Input.aimY / 100f) * sensitivity;
			axisYMoved = true;
		} else if (Input.aimY < -aimThreshold && Input.aimY <= Input.lastAimY) {
			axlScopeCursorWorldPos.y -= Global.spf * Global.screenW * (MathF.Abs(Input.aimY) / 100f) * sensitivity;
			axisYMoved = true;
		}

		// Controller or keyboard button based aim section
		if (!axisXMoved) {
			if (player.input.isHeld(Control.AimLeft, player)) {
				axlScopeCursorWorldPos.x -= Global.spf * 200 * sensitivity;
				axisXMoved = true;
			} else if (player.input.isHeld(Control.AimRight, player)) {
				axlScopeCursorWorldPos.x += Global.spf * 200 * sensitivity;
				axisXMoved = true;
			}
		}
		if (!axisYMoved) {
			if (player.input.isHeld(Control.AimUp, player)) {
				axlScopeCursorWorldPos.y -= Global.spf * 200 * sensitivity;
				axisYMoved = true;
			} else if (player.input.isHeld(Control.AimDown, player)) {
				axlScopeCursorWorldPos.y += Global.spf * 200 * sensitivity;
				axisYMoved = true;
			}
		}

		// Mouse based aim
		if (!Menu.inMenu && !player.isAI) {
			if (Options.main.useMouseAim) {
				axlScopeCursorWorldPos.x += Input.mouseDeltaX * 0.125f * sensitivity;
				axlScopeCursorWorldPos.y += Input.mouseDeltaY * 0.125f * sensitivity;
			}
		}

		// Aim assist
		if (!Options.main.useMouseAim && Options.main.lockOnSound)// && !axisXMoved && !axisYMoved)
		{
			Character? target = null;
			float bestDist = float.MaxValue;
			foreach (var enemy in Global.level.players) {
				if (enemy.character != null && enemy.character.canBeDamaged(player.alliance, player.id, null) && !enemy.character.isStealthy(player.alliance)) {
					float cursorDist = enemy.character.getAimCenterPos().distanceTo(axlScopeCursorWorldPos);
					if (cursorDist < bestDist) {
						bestDist = cursorDist;
						target = enemy.character;
					}
				}
			}
			const float aimAssistRange = 25;
			if (target != null && bestDist < aimAssistRange) {
				//float aimAssistPower = (float)Math.Pow(1 - (bestDist / aimAssistRange), 2);
				axlScopeCursorWorldPos = Point.lerp(axlScopeCursorWorldPos, target.getAimCenterPos(), Global.spf * 5);
				//axlScopeCursorWorldPos = target.getAimCenterPos();
			}
		}

		// Aimbot
		if (!player.isAI) {
			//var target = Global.level.getClosestTarget(axlScopeCursorWorldPos, player.alliance, true);
			//if (target != null && target.pos.distanceTo(axlScopeCursorWorldPos) < 100) axlScopeCursorWorldPos = target.getAimCenterPos();
		}

		Point centerPos = getAimCenterPos();
		if (axlScopeCursorWorldPos.distanceTo(centerPos) > player.zoomRange) {
			axlScopeCursorWorldPos = centerPos.add(centerPos.directionTo(axlScopeCursorWorldPos).normalize().times(player.zoomRange));
		}

		getMouseTargets();
	}

	public void getMouseTargets() {
		axlCursorTarget = null;
		axlHeadshotTarget = null;

		int cursorSize = 1;
		var shape = new Rect(axlGenericCursorWorldPos.x - cursorSize, axlGenericCursorWorldPos.y - cursorSize, axlGenericCursorWorldPos.x + cursorSize, axlGenericCursorWorldPos.y + cursorSize).getShape();
		var hit = Global.level.checkCollisionsShape(shape, new List<GameObject>() { this }).FirstOrDefault(c => c.gameObject is IDamagable);
		if (hit != null) {
			var target = hit.gameObject as IDamagable;
			if (target != null) {
				if (target.canBeDamaged(player.alliance, player.id, null)) {
					axlCursorTarget = target;
				}
			}
		}
		foreach (var enemy in Global.level.players) {
			if (enemy.character != null &&
				enemy.character.canBeDamaged(player.alliance, player.id, null) &&
				enemy.character.getHeadPos() != null
			) {
				if (axlGenericCursorWorldPos.distanceTo(enemy.character.getHeadPos()!.Value) < headshotRadius) {
					axlCursorTarget = enemy.character;
					axlHeadshotTarget = enemy.character;
				}
			}
		}
	}

	public void updateAxlCursorPos() {
		float aimThreshold = 5;
		bool axisXMoved = false;
		bool axisYMoved = false;
		// Options.main.aimSensitivity is a float from 0 to 1.
		float distFromNormal = Options.main.aimSensitivity - 0.5f;
		float sensitivity = 1;
		if (distFromNormal > 0) {
			sensitivity += distFromNormal * 7.5f;
		} else {
			sensitivity += distFromNormal * 1.75f;
		}

		// Controller joystick axis move section
		if (Input.aimX > aimThreshold && Input.aimX >= Input.lastAimX) {
			axlCursorPos.x += Global.spf * Global.screenW * (Input.aimX / 100f) * sensitivity;
			axisXMoved = true;
		} else if (Input.aimX < -aimThreshold && Input.aimX <= Input.lastAimX) {
			axlCursorPos.x -= Global.spf * Global.screenW * (MathF.Abs(Input.aimX) / 100f) * sensitivity;
			axisXMoved = true;
		}
		if (Input.aimY > aimThreshold && Input.aimY >= Input.lastAimY) {
			axlCursorPos.y += Global.spf * Global.screenW * (Input.aimY / 100f) * sensitivity;
			axisYMoved = true;
		} else if (Input.aimY < -aimThreshold && Input.aimY <= Input.lastAimY) {
			axlCursorPos.y -= Global.spf * Global.screenW * (MathF.Abs(Input.aimY) / 100f) * sensitivity;
			axisYMoved = true;
		}

		// Controller or keyboard button based aim section
		if (!axisXMoved) {
			if (player.input.isHeld(Control.AimLeft, player)) {
				axlCursorPos.x -= Global.spf * 200 * sensitivity;
			} else if (player.input.isHeld(Control.AimRight, player)) {
				axlCursorPos.x += Global.spf * 200 * sensitivity;
			}
		}
		if (!axisYMoved) {
			if (player.input.isHeld(Control.AimUp, player)) {
				axlCursorPos.y -= Global.spf * 200 * sensitivity;
			} else if (player.input.isHeld(Control.AimDown, player)) {
				axlCursorPos.y += Global.spf * 200 * sensitivity;
			}
		}

		// Mouse based aim
		if (!Menu.inMenu && !player.isAI) {
			if (Options.main.useMouseAim) {
				axlCursorPos.x += Input.mouseDeltaX * 0.125f * sensitivity;
				axlCursorPos.y += Input.mouseDeltaY * 0.125f * sensitivity;
			}
			axlCursorPos.x = Helpers.clamp(axlCursorPos.x, 0, Global.viewScreenW);
			axlCursorPos.y = Helpers.clamp(axlCursorPos.y, 0, Global.viewScreenH);
		}

		if (isWarpIn()) {
			axlCursorPos = getCenterPos().addxy(-Global.level.camX + 50 * xDir, -Global.level.camY);
		}

		// aimbot
		if (player.isAI) {
			var target = Global.level.getClosestTarget(pos, player.alliance, true);
			if (target != null) {
				axlCursorPos = target.pos.addxy(
					-Global.level.camX,
					-Global.level.camY - ((target as Character)?.charState is InRideArmor ? 0 : 16)
				);
			};
		}

		getMouseTargets();
	}

	public bool isAxlLadderShooting() {
		if (currentWeapon is AssassinBulletChar) return false;
		if (recoilTime > 0) return true;
		bool canShootBool = (
			canShoot() && shootTime == 0
		);
		if (player.input.isHeld(Control.Shoot, player) && canShootBool) {
			return true;
		}
		return false;
	}

	public override void render(float x, float y) {
		base.render(x, y);

		if (!ownedByLocalPlayer) {
			if (shouldDrawArmNet) {
				drawArm(netArmAngle);
			}

			if (isNonOwnerZoom) {
				Color laserColor = new Color(255, 0, 0, 160);
				DrawWrappers.DrawLine(nonOwnerScopeStartPos.x, nonOwnerScopeStartPos.y, nonOwnerScopeEndPos.x, nonOwnerScopeEndPos.y, laserColor, 2, ZIndex.HUD);
				DrawWrappers.DrawCircle(nonOwnerScopeEndPos.x, nonOwnerScopeEndPos.y, 2f, true, laserColor, 1, ZIndex.HUD);
			}

			return;
		}

		drawAxlCursor();

		if (axlWeapon != null) netArmAngle = getShootAngle();

		float angleOffset = 0;
		if (recoilTime > 0.1f) angleOffset = (0.2f - recoilTime) * 50;
		else if (recoilTime < 0.1f && recoilTime > 0) angleOffset = (0.1f - (0.1f - recoilTime)) * 50;
		angleOffset *= -axlXDir;
		netArmAngle += angleOffset;

		if (charState is DarkHoldState dhs) {
			netArmAngle = dhs.lastArmAngle;
		}

		/*
		if (charState is LadderClimb) {
			if (isAxlLadderShooting()) {
				xDir = (pos.x > axlGenericCursorWorldPos.x ? -1 : 1);
				changeSprite("axl_ladder_shoot", true);
			} else {
				changeSprite("axl_ladder_climb", true);
			}
		}*/

		if (shootTime > 0 && !muzzleFlash.isAnimOver()) {
			muzzleFlash.xDir = axlXDir;
			muzzleFlash.visible = true;
			muzzleFlash.angle = netArmAngle;
			muzzleFlash.pos = getAxlBulletPos();
			if (muzzleFlash.sprite.name.StartsWith("axl_raygun_flash")) {
				muzzleFlash.xScale = 1f;
				muzzleFlash.yScale = 1f;
				muzzleFlash.setzIndex(zIndex - 2);
			} else {
				muzzleFlash.xScale = 1f;
				muzzleFlash.yScale = 1f;
				muzzleFlash.setzIndex(zIndex + 200);
			}
		} else {
			muzzleFlash.visible = false;
		}

		if (shouldDrawArm()) {
			drawArm(netArmAngle);
		}

		if (!hideNoShaderIcon()) {
			if (isWhiteAxl() && !Global.shaderWrappers.ContainsKey("hyperaxl")) {
				Global.sprites["hud_killfeed_weapon"].draw(
					123, pos.x, pos.y - 4 + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD
				);
				deductLabelY(labelKillFeedIconOffY);
			}
		}

		if (player.isMainPlayer && !player.isDead) {
			if (Options.main.aimKeyToggle) {
				if (player.input.isAimingBackwards(player)) {
					Global.sprites["hud_axl_aim"].draw(
						0, pos.x, pos.y + currentLabelY, xDir, 1, null, 1, 1, 1, ZIndex.HUD
					);
					deductLabelY(labelAxlAimModeIconOffY);
				} else if (player.input.isPositionLocked(player)) {
					Global.sprites["hud_axl_aim"].draw(
						1, pos.x, pos.y + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD
					);
					deductLabelY(labelAxlAimModeIconOffY);
				}
			}
		}

		if (Global.showHitboxes) {
			Point bulletPos = getAxlBulletPos();
			DrawWrappers.DrawLine(
				bulletPos.x, bulletPos.y, axlGenericCursorWorldPos.x,
				axlGenericCursorWorldPos.y, Color.Magenta, 1, ZIndex.Default + 1
			);
		}

		drawAxlCursor();

		//DEBUG CODE
		/*
		if (Keyboard.IsKeyPressed(Key.I)) axlWeapon.renderAngleOffset++;
		else if (Keyboard.IsKeyPressed(Key.J)) axlWeapon.renderAngleOffset--;
		Global.debugString1 = axlWeapon.renderAngleOffset.ToString();
		if (Keyboard.IsKeyPressed(Key.K)) axlWeapon.shootAngleOffset++;
		else if (Keyboard.IsKeyPressed(Key.L)) axlWeapon.shootAngleOffset--;
		Global.debugString2 = axlWeapon.shootAngleOffset.ToString();
		*/
	}

	float axlCursorAngle;
	public void drawAxlCursor() {
		if (!ownedByLocalPlayer) return;
		if (Global.level.gameMode.isOver) return;
		if (isZooming() && !isZoomOutPhase1Done) return;
		// if (isWarpIn()) return;

		if (Options.main.useMouseAim || Global.showHitboxes) {
			drawBloom();
			Global.sprites["axl_cursor"].draw(0, axlCursorWorldPos.x, axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
			if (player.assassinHitPos?.isHeadshot == true && currentWeapon is AssassinBulletChar && Global.level.isTraining()) {
				Global.sprites["hud_kill"].draw(0, axlCursorWorldPos.x, axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
			}
		}
		if (!Options.main.useMouseAim) {
			if (axlWeapon != null && (currentWeapon is AssassinBulletChar || player.input.isCursorLocked(player))) {
				Point bulletPos = getAxlBulletPos();
				float radius = 120;
				float ang = getShootAngle();
				float x = Helpers.cosd(ang) * radius * getShootXDir();
				float y = Helpers.sind(ang) * radius * getShootXDir();
				DrawWrappers.DrawLine(bulletPos.x, bulletPos.y, bulletPos.x + x, bulletPos.y + y, new Color(255, 0, 0, 128), 2, ZIndex.HUD, true);
				if (axlCursorTarget != null && player.assassinHitPos?.isHeadshot == true && currentWeapon is AssassinBulletChar && Global.level.isTraining()) {
					Global.sprites["hud_kill"].draw(0, axlLockOnCursorPos.x, axlLockOnCursorPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
				}
			}
			if (axlCursorTarget != null && !isAnyZoom()) {
				axlCursorAngle += Global.spf * 360;
				if (axlCursorAngle > 360) axlCursorAngle -= 360;
				Global.sprites["axl_cursor_x7"].draw(0, axlLockOnCursorPos.x, axlLockOnCursorPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1, angle: axlCursorAngle);
				//drawBloom();
			}
		}

		/*
		if (currentWeapon.ammo <= 0)
		{
			if (currentWeapon.rechargeCooldown > 0)
			{
				float textPosX = axlCursorPos.x;
				float textPosY = axlCursorPos.y - 20;
				if (!Options.main.useMouseAim)
				{
					textPosX = pos.x - Global.level.camX / Global.viewSize;
					textPosY = (pos.y - 50 - Global.level.camY) / Global.viewSize;
				}
				DrawWrappers.DeferTextDraw(() =>
				{
					Helpers.drawTextStd(
						"Reload:" + currentWeapon.rechargeCooldown.ToString("0.0"),
						textPosX, textPosY, Alignment.Center, fontSize: 20,
						outlineColor: Helpers.getAllianceColor()
					);
				});
			}
		}
		*/
	}

	public void drawBloom() {
		Global.sprites["axl_cursor_top"].draw(0, axlCursorWorldPos.x, axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
		Global.sprites["axl_cursor_bottom"].draw(0, axlCursorWorldPos.x, axlCursorWorldPos.y + 1, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
		Global.sprites["axl_cursor_left"].draw(0, axlCursorWorldPos.x, axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
		Global.sprites["axl_cursor_right"].draw(0, axlCursorWorldPos.x + 1, axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
		Global.sprites["axl_cursor_dot"].draw(0, axlCursorWorldPos.x, axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
	}

	float mcFrameTime;
	float mcMaxFrameTime = 0.03f;
	int mcFrameIndex;
	Sprite pistol2Sprite = new Sprite("axl_arm_pistol2");
	public void drawArm(float angle) {
		long zIndex = this.zIndex - 1;
		Point gunArmOrigin;
		/*
		if (charState is AssassinateEX assasinate) {
			gunArmOrigin = getAxlGunArmOrigin();
			getAxlArmSprite().draw(
				0, gunArmOrigin.x, gunArmOrigin.y,
				axlXDir, 1, getRenderEffectSet(), 1, 1, 1,
				zIndex, angle: angle, shaders: getShaders()
			);
			return;
		} */

		if (axlWeapon != null && axlWeapon.isTwoHanded(false)) {
			zIndex = this.zIndex + 1;
		}
		gunArmOrigin = getAxlGunArmOrigin();

		if (axlWeapon is DoubleBullet) {
			var armPos = getDoubleBulletArmPos();
			if (shouldDraw()) {
				pistol2Sprite.draw(0, gunArmOrigin.x + armPos.x * axlXDir, gunArmOrigin.y + armPos.y, axlXDir, 1, getRenderEffectSet(), 1, 1, 1, this.zIndex + 100, angle: angle, shaders: getShaders(), actor: this);
			}
		}

		if (shouldDraw()) {
			int frameIndex = 0;
			if (axlWeapon is IceGattling) {
				revIndex += revTime * Global.spf * 60;
				if (revIndex >= 4) {
					revIndex = 0;
				}

				if (revTime > 0) {
					frameIndex = (int)revIndex;
				}
			}
			if (axlWeapon is AxlBullet ab && ab.type == (int)AxlBulletWeaponType.MetteurCrash) {
				if (shootTime > 0) {
					mcFrameTime += Global.spf;
					if (mcFrameTime > mcMaxFrameTime) {
						mcFrameTime = 0;
						mcFrameIndex++;
						if (mcFrameIndex > 3) mcFrameIndex = 0;
					}
				}
				frameIndex = mcFrameIndex;
			}
			getAxlArmSprite().draw(frameIndex, gunArmOrigin.x, gunArmOrigin.y, axlXDir, 1, getRenderEffectSet(), 1, 1, 1, zIndex, angle: angle, shaders: getShaders(), actor: this);
		}
	}

	public bool shouldDrawArm() {
		if (charState is DarkHoldState dhs) {
			return dhs.shouldDrawAxlArm;
		}

		bool ladderClimb = false;
		if (charState is LadderClimb) {
			if (!isAxlLadderShooting()) {
				ladderClimb = true;
			}
		} else if (charState is LadderEnd) ladderClimb = true;

		return !(charState is HyperAxlStart || isWarpIn() || charState is Hurt || charState is Die || charState is GenericStun || charState is InRideArmor || charState is DodgeRoll || charState is VileMK2Grabbed || charState is KnockedDown
			|| sprite.name.Contains("win") || sprite.name.Contains("lose") || ladderClimb || charState is DeadLiftGrabbed || charState is UPGrabbed || charState is WhirlpoolGrabbed || charState is InRideChaser);
	}

	public Point getAxlBulletDir() {
		Point origin = getAxlBulletPos();
		Point cursorPos = getCorrectedCursorPos();
		return origin.directionTo(cursorPos).normalize();
	}

	public ushort netAxlArmSpriteIndex;
	public string getAxlArmSpriteName() {
		if (gaeaShield != null) {
			return "axl_arm_icegattling2";
		}

		return axlWeapon?.sprite ?? "axl_arm_pistol";
	}

	public Sprite getAxlArmSprite() {
		if (!ownedByLocalPlayer && Global.spriteNameByIndex.ContainsKey(netAxlArmSpriteIndex)) {
			return new Sprite(Global.spriteNameByIndex[netAxlArmSpriteIndex]);
		}

		return new Sprite(getAxlArmSpriteName());
	}

	public Point getCorrectedCursorPos() {
		if (axlWeapon == null) return new Point();
		Point cursorPos = axlGenericCursorWorldPos;
		Point gunArmOrigin = getAxlGunArmOrigin();

		Sprite sprite = getAxlArmSprite();
		float minimumAimRange = sprite.animData.frames[0].POIs[0].magnitude + 5;

		if (gunArmOrigin.distanceTo(cursorPos) < minimumAimRange) {
			Point angleDir = Point.createFromAngle(getShootAngle(true));
			cursorPos = cursorPos.add(angleDir.times(minimumAimRange));
		}
		return cursorPos;
	}

	public Point getAxlHitscanPoint(float maxRange) {
		Point bulletPos = getAxlBulletPos();
		Point bulletDir = getAxlBulletDir();
		return bulletPos.add(bulletDir.times(maxRange));
	}

	public Point getMuzzleOffset(float angle) {
		if (axlWeapon == null) return new Point();
		Sprite sprite = getAxlArmSprite();
		Point muzzlePOI = sprite.animData.frames[0].POIs[0];

		float horizontalOffX = 0;// Helpers.cosd(angle) * muzzlePOI.x;
		float horizontalOffY = 0;// Helpers.sind(angle) * muzzlePOI.x;

		float verticalOffX = -axlXDir * Helpers.sind(angle) * muzzlePOI.y;
		float verticalOffY = axlXDir * Helpers.cosd(angle) * muzzlePOI.y;

		return new Point(horizontalOffX + verticalOffX, horizontalOffY + verticalOffY);
	}

	public Point getAxlBulletPos(int poiIndex = 0) {
		if (axlWeapon == null) return new Point();

		Point gunArmOrigin = getAxlGunArmOrigin();

		var doubleBullet = currentWeapon as DoubleBullet;
		if (doubleBullet != null && doubleBullet.isSecondShot) {
			Point dbArmPos = getDoubleBulletArmPos();
			gunArmOrigin = gunArmOrigin.addxy(dbArmPos.x * getAxlXDir(), dbArmPos.y);
		}

		Sprite sprite = getAxlArmSprite();
		float angle = getShootAngle(ignoreXDir: true) + sprite.animData.frames[0].POIs[poiIndex].angle * axlXDir;
		Point angleDir = Point.createFromAngle(angle).times(sprite.animData.frames[0].POIs[poiIndex].magnitude);

		return gunArmOrigin.addxy(angleDir.x, angleDir.y);
	}

	public Point getAxlScopePos() {
		if (axlWeapon == null) return new Point();
		Point gunArmOrigin = getAxlGunArmOrigin();
		Sprite sprite = getAxlArmSprite();
		if (sprite.animData.frames[0].POIs.Length < 2) return new Point();
		float angle = getShootAngle(ignoreXDir: true) + sprite.animData.frames[0].POIs[1].angle * axlXDir;
		Point angleDir = Point.createFromAngle(angle).times(sprite.animData.frames[0].POIs[1].magnitude);
		return gunArmOrigin.addxy(angleDir.x, angleDir.y);
	}

	public float getShootAngle(bool ignoreXDir = false) {
		if (voltTornadoTime > 0) {
			return -90 * getAxlXDir();
		}

		Point gunArmOrigin = getAxlGunArmOrigin();
		Point cursorPos = axlGenericCursorWorldPos;
		float angle = gunArmOrigin.directionTo(cursorPos).angle;

		Point adjustedOrigin = gunArmOrigin.add(getMuzzleOffset(angle));
		float adjustedAngle = adjustedOrigin.directionTo(cursorPos).angle;

		// DEBUG CODE
		//Global.debugString1 = angle.ToString();
		//Global.debugString2 = adjustedAngle.ToString();
		//DrawWrappers.DrawPixel(adjustedOrigin.x, adjustedOrigin.y, Color.Red, ZIndex.Default + 1);
		//DrawWrappers.DrawPixel(gunArmOrigin.x, gunArmOrigin.y, Color.Red, ZIndex.Default + 1);
		//Point angleLine = Point.createFromAngle(angle).times(100);
		//DrawWrappers.DrawLine(gunArmOrigin.x, gunArmOrigin.y, gunArmOrigin.x + angleLine.x, gunArmOrigin.y + angleLine.y, Color.Magenta, 1, ZIndex.Default + 1);
		//Point angleLine2 = Point.createFromAngle(angleWithOffset).times(100);
		//DrawWrappers.DrawLine(gunArmOrigin.x, gunArmOrigin.y, gunArmOrigin.x + angleLine2.x, gunArmOrigin.y + angleLine2.y, Color.Red, 1, ZIndex.Default + 1);
		// END DEBUG CODE

		if (axlXDir == -1 && !ignoreXDir) adjustedAngle += 180;

		return adjustedAngle;
	}

	public Point getTwoHandedOffset() {
		if (axlWeapon == null) return new Point();
		if (axlWeapon.isTwoHanded(false)) {
			if (axlWeapon is FlameBurner) return new Point(-6, 1);
			else if (axlWeapon is IceGattling) return new Point(-6, 1);
			else return new Point(-6, 1);
		}
		return new Point();
	}

	public Point getAxlGunArmOrigin() {
		Point retPoint;
		var pois = sprite.getCurrentFrame().POIs;
		Point offset = getTwoHandedOffset();
		Point roundPos = new Point(MathInt.Round(pos.x), MathInt.Round(pos.y));
		if (pois.Length > 0) {
			retPoint = roundPos.addxy((offset.x + pois[0].x) * axlXDir, pois[0].y + offset.y);
		} else retPoint = roundPos.addxy((offset.x + 3) * axlXDir, -21 + offset.y);

		return retPoint;
	}

	public void addDNACore(Character hitChar) {
		if (!player.ownedByLocalPlayer) return;
		if (Global.level.is1v1()) return;

		if (weapons.Count((Weapon weapon) => weapon is DNACore) < 4) {
			var dnaCoreWeapon = new DNACore(hitChar, player) {
				index = (int)WeaponIds.DNACore - weapons.Count
			};
			if (isATrans) {
				linkedATransChar?.weapons.Add(dnaCoreWeapon);
			} else {
				weapons.Add(dnaCoreWeapon);
			}
			player.savedDNACoreWeapons.Add(dnaCoreWeapon);
		}
	}

	public int getAxlXDir() {
		if (axlWeapon != null && (axlWeapon.isTwoHanded(false))) {
			return pos.x < axlGenericCursorWorldPos.x ? 1 : -1;
		}
		return xDir;
	}

	public bool isWhiteAxl() {
		return whiteAxlTime > 0;
	}

	public bool isStealthMode() {
		return isInvisible();
	}
	public bool isHypermodeAxl() {
		return isWhiteAxl() || isInvisible();
	}

	float stealthCurrencyTime;

	public void updateStealthMode() {
		stealthCurrencyTime += Global.spf;
		stingChargeTime = 8;
		if (stealthCurrencyTime > 1) {
			stealthCurrencyTime = 0;
			player.currency--;
			if (player.currency <= 0) {
				player.currency = 0;
				stingChargeTime = 0;
			}
		}
	}

	// New data starts here.
	public override List<ShaderWrapper> getShaders() {
		var shaders = new List<ShaderWrapper>();
		ShaderWrapper? palette = null;

		int paletteNum = 0;
		if (whiteAxlTime > 3) paletteNum = 1;
		else if (whiteAxlTime > 0) {
			int mod = MathInt.Ceiling(whiteAxlTime) * 2;
			paletteNum = (Global.floorFrameCount % (mod * 2)) < mod ? 0 : 1;
		}
		palette = player.axlPaletteShader;
		palette?.SetUniform("palette", paletteNum);
		palette?.SetUniform("paletteTexture", Global.textures["hyperAxlPalette"]);

		if (palette != null) {
			shaders.Add(palette);
		}
		shaders.AddRange(base.getShaders());

		return shaders;
	}

	public override bool isSoftLocked() {
		if (isAnyZoom() || sniperMissileProj != null) {
			return true;
		}
		if (assassinTime > 0) return true;
		return base.isSoftLocked();
	}

	public override bool canDash() {
		if (isAnyZoom() || sniperMissileProj != null || isRevving || charState is Crouch) {
			return false;
		}
		return base.canDash();
	}

	public override bool canClimbLadder() {
		if (recoilTime > 0) {
			return false;
		}
		return base.canClimbLadder();
	}

	public override bool canChangeWeapons() {
		//if (gaeaShield != null) return false;
		if (sniperMissileProj != null) return false;
		if (revTime > 0.5f) return false;

		return base.canChangeWeapons();
	}

	public override float getDashSpeed() {
		float dashSpeed = 3.45f;
		if (axlWeapon != null && axlWeapon.isTwoHanded(false)) {
			dashSpeed *= 0.875f;
		}
		return dashSpeed * getRunDebuffs();
	}

	public override float getRunDebuffs() {
		float speed = getRunDebuffs();
		if (rshootDebuffTime[axlXDir + 1] > 0) {
			speed *= (1 - 0.60f * rshootDebuffAmmount[axlXDir + 1]);
		}
		return speed;
	}

	public void afterAxlShoot(Weapon axlWeapon) {
		float debuffTime = axlWeapon.shootCooldown + 6;
		if (debuffTime < 15) {
			debuffTime = 15;
		}
		rshootDebuffTime[axlXDir + 1] = debuffTime;
		rshootDebuffAmmount[axlXDir + 1] = getBackwardMoveDebuff();
	}

	public override bool canShoot() {
		if (sniperMissileProj != null) { return false; }
		if (invulnTime > 0) return false;
		return base.canShoot();
	}

	public override bool isToughGuyHyperMode() {
		if (isWhiteAxl()) { return true; }

		return base.isToughGuyHyperMode();
	}

	public override Point getCamCenterPos(bool ignoreZoom = false) {
		if (sniperMissileProj != null) {
			return sniperMissileProj.getCenterPos();
		}
		if (isZooming() && !ignoreZoom) {
			return axlScopeCursorWorldPos;
		}
		return base.getCamCenterPos(ignoreZoom);
	}

	public override bool changeState(CharState newState, bool forceChange = true) {
		bool hasChanged = base.changeState(newState, forceChange);
		if (!hasChanged) {
			return false;
		}
		if (gaeaShield != null && shouldDrawArm() == false) {
			gaeaShield.destroySelf();
			gaeaShield = null;
		}
		return true;
	}

	public override void destroySelf(
		string spriteName = "", string fadeSound = "",
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {
		iceGattlingSound?.destroy();
		gaeaShield?.destroySelf();
		muzzleFlash?.destroySelf();
		sniperMissileProj?.destroySelf();

		base.destroySelf(
			spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy
		);
	}

	public bool isInvisible() {
		return stingChargeTime > 0 && stealthRevealTime <= 0;
	}

	public override string getSprite(string spriteName) {
		if (spriteName == "crystalized" || spriteName == "die" ||
			spriteName == "hurt" || spriteName == "hyper_start" ||
			spriteName == "hyper_start_air" || spriteName == "knocked_down" ||
			spriteName == "roll" || spriteName == "warp_in" || spriteName == "win"
		) {
			if (player.axlBulletType == 1) spriteName += "_mc";
			else if (player.axlBulletType == 2) spriteName += "_bk";
			else if (player.axlBulletType == 3) spriteName += "_mb";
			else if (player.axlBulletType == 5) spriteName += "_rb";
			else if (player.axlBulletType == 6) spriteName += "_ag";
		}
		return "axl_" + spriteName;
	}

	public override bool canCrouch() {
		if (Options.main.axlAimMode == 0 && player.input.getXDir(player) != 0 || isSoftLocked() || isDashing) {
			return false;
		}
		return true;
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

	public override bool canAddAmmo() {
		if (currentWeapon == null) { return false; }
		bool hasEmptyAmmo = false;
		foreach (Weapon weapon in weapons) {
			if (weapon.canHealAmmo && weapon.ammo < weapon.maxAmmo) {
				hasEmptyAmmo = true;
				break;
			}
		}
		return hasEmptyAmmo;
	}

	public override bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		bool damaged = base.canBeDamaged(damagerAlliance, damagerPlayerId, projId);
		if (stealthActive) {
			return false;
		}
		return damaged;
	}

	public override bool isStealthy(int alliance) {
		return (
			player.alliance != alliance &&
			(stealthActive || isATrans && !disguiseCoverBlown)
		);
	}

	public override bool isNonDamageStatusImmune() {
		return isStealthMode();
	}


	public override bool isInvulnerable(bool ignoreRideArmorHide = false, bool factorHyperMode = false) {
		bool invul = base.isInvulnerable(ignoreRideArmorHide, factorHyperMode);
		/*if (stealthActive) {
			return true;
		}*/
		return invul;
	}

	public override void onFlagPickup(Flag flag) {
		stingChargeTime = 0;
		base.onFlagPickup(flag);
	}

	public override bool canMove() {
		// TODO: Move this to axl.cs
		if (isAimLocked()) {
			return false;
		}
		if (isSoftLocked()) {
			return false;
		}
		return base.canMove();
	}

	
	public bool isAimLocked() {
		if (player.input.isPositionLocked(player) && Options.main.axlAimMode == 0) {
			return true;
		}
		if (Options.main.axlAimMode == 0 && !Options.main.moveInDiagAim && !isDashing &&
			(grounded || charState is Hover || player.input.isHeld(Control.Shoot, player) || player.input.isHeld(Control.Special1, player)) &&
			(player.input.isHeld(Control.Up, player) || player.input.isHeld(Control.Down, player))) {
			return true;
		}
		return false;
	}

	public void configureWeapons(AxlLoadout axlLoadout) {
		if (Global.level.isTraining() && !Global.level.server.useLoadout) {
			weapons = Weapon.getAllAxlWeapons(axlLoadout).Select(w => w.clone()).ToList();
			weapons[0] = getAxlBullet(player.axlBulletType);
		} else if (Global.level.is1v1()) {
			weapons.Add(new AxlBullet());
			weapons.Add(new RayGun(axlLoadout.rayGunAlt));
			weapons.Add(new BlastLauncher(axlLoadout.blastLauncherAlt));
			weapons.Add(new BlackArrow(axlLoadout.blackArrowAlt));
			weapons.Add(new SpiralMagnum(axlLoadout.spiralMagnumAlt));
			weapons.Add(new BoundBlaster(axlLoadout.boundBlasterAlt));
			weapons.Add(new PlasmaGun(axlLoadout.plasmaGunAlt));
			weapons.Add(new IceGattling(axlLoadout.iceGattlingAlt));
			weapons.Add(new FlameBurner(axlLoadout.flameBurnerAlt));
		} else {
			weapons = axlLoadout.getWeaponsFromLoadout();
			weapons.Insert(0, getAxlBullet(player.axlBulletType));
		}
		if (ownedByLocalPlayer) {
			foreach (var dnaCore in player.savedDNACoreWeapons) {
				weapons.Add(dnaCore);
			}
		}
		if (weapons[0].type > 0) {
			weapons[0].ammo = player.axlBulletTypeLastAmmo[weapons[0].type];
		}
	}

	public Weapon getAxlBullet(int axlBulletType) {
		if (axlBulletType == (int)AxlBulletWeaponType.DoubleBullets) {
			return new DoubleBullet();
		}
		return new AxlBullet((AxlBulletWeaponType)axlBulletType);
	}

	public Weapon getAxlBulletWeapon() {
		return getAxlBulletWeapon(player.axlBulletType);
	}

	public Weapon getAxlBulletWeapon(int type) {
		if (type == (int)AxlBulletWeaponType.DoubleBullets) {
			return new DoubleBullet();
		} else {
			return new AxlBullet((AxlBulletWeaponType)type);
		}
	}
	public void customSettingReloadWeapon() {
		//Reload Weapon Custom Setting
		bool shootHeld = player.input.isHeld(Control.Shoot, player);
		switch (currentWeapon) {
			case RayGun:
				(currentWeapon as RayGun)?.rechargeAmmoCustomSetting(player, this, shootHeld, 1, 1);
				break;
			case BlastLauncher:
				(currentWeapon as BlastLauncher)?.rechargeAmmoCustomSetting(player, this, shootHeld, 1, 4);
				break;
			case BlackArrow:
				(currentWeapon as BlackArrow)?.rechargeAmmoCustomSetting(player, this, shootHeld, 1, 1);
				break;
			case SpiralMagnum:
				(currentWeapon as SpiralMagnum)?.rechargeAmmoCustomSetting(player, this, shootHeld, 1, 1);
				break;
			case BoundBlaster:
				(currentWeapon as BoundBlaster)?.rechargeAmmoCustomSetting(player, this, shootHeld, 1, 1);
				break;
			case PlasmaGun:
				(currentWeapon as PlasmaGun)?.rechargeAmmoCustomSetting(player, this, shootHeld, 1, 1);
				break;
			case IceGattling:
				(currentWeapon as IceGattling)?.rechargeAmmoCustomSetting(player, this, shootHeld, 1, 4);
				break;
			case FlameBurner:
				(currentWeapon as FlameBurner)?.rechargeAmmoCustomSetting(player, this, shootHeld, 1, 4);
				break;
		}
		if (axlWeapon != null) {
			if (isAnyZoom()) {
				axlWeapon.rechargeAmmoCustomSettingAxl2 = 200;
			}
		}
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();

		customData.Add((byte)(currentWeapon?.index ?? 0));
		customData.Add((byte)MathF.Ceiling(currentWeapon?.ammo ?? 0));

		customData.Add(Helpers.degreeToByte(netArmAngle));
		customData.Add(Helpers.degreeToByte(player.axlBulletType));

		customData.Add(Helpers.boolArrayToByte([
			shouldDrawArm(),
			stealthActive
		]));

		customData.AddRange(BitConverter.GetBytes(
			Global.spriteIndexByName.GetValueOrCreate(getAxlArmSpriteName(), ushort.MaxValue)
		));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		Weapon? targetWeapon = weapons.Find(w => w.index == data[0]);
		if (targetWeapon != null) {
			weaponSlot = weapons.IndexOf(targetWeapon);
			targetWeapon.ammo = data[1];
		}
		netArmAngle = Helpers.byteToDegree(data[2]);
		player.axlBulletType = data[3];

		bool[] boolData = Helpers.byteToBoolArray(data[4]);
		shouldDrawArmNet = boolData[0];
		stealthActive = boolData[1];

		netAxlArmSpriteIndex = BitConverter.ToUInt16(data[5..7]);
	}
	public override void aiAttack(Actor? target) {
		if (whiteAxlLoadout && player.currency >= 10 && !player.isDead && !isInvulnerable() 
			&& !(isWhiteAxl() || isStealthMode()) && charState.attackCtrl) {
			changeState(new HyperAxlStart(grounded), true);
		}
		if (axlHyperMode == 1 && player.currency >= 10 && !player.isDead && !isInvulnerable() 
			&& !(isWhiteAxl() || isStealthMode()) && charState.attackCtrl) {
			stingChargeTime = 12;
		}
		int AAttack = Helpers.randomRange(0, 1);
		if (canShoot() && currentWeapon?.ammo > 0 && axlWeapon != null
			&& !player.isDead && canChangeWeapons() && charState.attackCtrl) {
			switch (AAttack) {
				case 0:
					player.press(Control.Shoot);
					break;
				case 1 when currentWeapon is not IceGattling or PlasmaGun:
					player.press(Control.Special1);
					break;
			}
		}
		base.aiAttack(target);
	}
	public override void aiDodge(Actor? target) {
		foreach (GameObject gameObject in getCloseActors(32, true, false, false)) {
			if (gameObject is Projectile proj && proj.damager.owner.alliance != player?.alliance) {
				if (grounded && canDash() && charState is not DodgeRoll && dodgeRollCooldown <= 0 && charState.normalCtrl) {
					changeState(new DodgeRoll());
					dodgeRollCooldown = maxDodgeRollCooldown;
				} else if (currentWeapon is FlameBurner && loadout.flameBurnerAlt == 1 &&
				 (proj is not GenericMeleeProj || (proj.reflectableFBurner == true)) && currentWeapon?.ammo > 0) {
					player.press(Control.Special1);
				}
			}
		}
		base.aiDodge(target);
	}
}
