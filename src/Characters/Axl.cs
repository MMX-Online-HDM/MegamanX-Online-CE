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
		get { return player.weapon?.shootCooldown ?? 0; }
		set { if (player.weapon != null) { player.weapon.shootCooldown = value; } }
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
	public LoopingSound iceGattlingSound;
	public float whiteAxlTime;
	public float dodgeRollCooldown;
	public const float maxDodgeRollCooldown = 1.5f;
	public bool hyperAxlUsed;
	//public ShaderWrapper axlPaletteShader;
	public float maxHyperAxlTime = 30;
	public List<int> ammoUsages = new List<int>();

	public RayGunAltProj? rayGunAltProj;
	public GaeaShieldProj? gaeaShield;

	// Used to be 0.5, 100
	public const float maxStealthRevealTime = 0.25f;
	// The ping divided by this number indicates stealth reveal time in online
	public const float stealthRevealPingDenom = 200;

	public PlasmaGunAltProj? plasmaGunAltProj;

	public bool shouldDrawArmNet;
	public bool stealthActive;

	public int axlHyperMode;

	public Axl(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.Axl;
		iceGattlingSound = new LoopingSound("iceGattlingLoopStart", "iceGattlingLoopStop", "iceGattlingLoop", this);

		muzzleFlash = new Anim(new Point(), "axl_pistol_flash", xDir, null, false);
		muzzleFlash.visible = false;
		axlHyperMode = player.loadout?.axlLoadout?.hyperMode ?? 0;

		configureWeapons();
	}

	public void zoomIn() {
		if (isZoomingIn) return;
		if (_zoom) return;

		_zoom = true;
		if (isWhiteAxl()) hyperAxlStillZoomed = true;
		player.axlCursorPos.x = Helpers.clamp(player.axlCursorPos.x, 0, Global.viewScreenW);
		player.axlCursorPos.y = Helpers.clamp(player.axlCursorPos.y, 0, Global.viewScreenH);
		savedCamX = Global.level.camX;
		savedCamY = Global.level.camY;
		player.axlScopeCursorWorldPos = player.character.getCamCenterPos(ignoreZoom: true);
		player.axlScopeCursorWorldLerpPos = player.axlCursorWorldPos;
		isZoomingIn = true;
		isZoomingOut = false;
	}

	public void zoomOut() {
		if (isZoomingOut) return;
		if (!_zoom) return;

		zoomCharge = 0;
		player.axlZoomOutCursorDestPos = player.character.getCamCenterPos(ignoreZoom: true);
		player.axlCursorPos = getAxlBulletPos().add(getAxlBulletDir().times(50)).addxy(-savedCamX, -savedCamY);

		isZoomingOut = true;
		isZoomingIn = false;
	}

	public bool isZooming() {
		return _zoom && player.isAxl;
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
						Point headPos = c.getHeadPos().Value;
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

	float assassinSmokeTime;
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
					player.weapons[0] = new AxlBullet();
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

		if (stingChargeTime > 0) {
			stingChargeTime -= Global.spf;
			player.weapon.ammo -= (Global.spf * 3 * (player.hasChip(3) ? 0.5f : 1));
			if (player.weapon.ammo < 0) player.weapon.ammo = 0;
			stingChargeTime = player.weapon.ammo;
			
			if (stingChargeTime <= 0) {
				player.delaySubtank();
				stingChargeTime = 0;
			}
		}

		isRevving = false;
		bool altRayGunHeld = false;
		bool altPlasmaGunHeld = false;

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
			assassinSmokeTime += Global.spf;
			if (assassinSmokeTime > 0.06f) {
				assassinSmokeTime = 0;
				// new Anim(getAxlBulletPos(0), "torpedo_smoke", 1, player.getNextActorNetId(), false, true, true) { vel = new Point(0, -100) };
			}
			assassinTime -= Global.spf;
			if (assassinTime < 0) {
				assassinTime = 0;
				useGravity = true;
			}
			return;
		}
		if (targetSoundCooldown > 0) targetSoundCooldown += Global.spf;
		if (targetSoundCooldown >= 1) targetSoundCooldown = 0;

		Helpers.decrementTime(ref dodgeRollCooldown);
		Helpers.decrementTime(ref undisguiseTime);
		Helpers.decrementTime(ref axlSwapTime);
		Helpers.decrementTime(ref axlAltSwapTime);
		Helpers.decrementTime(ref switchTime);
		Helpers.decrementTime(ref altSwitchTime);
		Helpers.decrementTime(ref revTime);
		Helpers.decrementTime(ref voltTornadoTime);
		Helpers.decrementTime(ref stealthRevealTime);

		if (player.weapon.ammo >= player.weapon.maxAmmo) {
			weaponHealAmount = 0;
		}
		if (weaponHealAmount > 0 && player.health > 0) {
			weaponHealTime += Global.spf;
			if (weaponHealTime > 0.05) {
				weaponHealTime = 0;
				weaponHealAmount--;
				player.weapon.ammo = Helpers.clampMax(player.weapon.ammo + 1, player.weapon.maxAmmo);
				playSound("heal", forcePlay: true);
			}
		}

		player.changeWeaponControls();
		updateAxlAim();

		sprite.reversed = false;
		if (player.axlWeapon != null && (player.axlWeapon.isTwoHanded(false) || isZooming()) && canChangeDir() && charState is not WallSlide) {
			int newXDir = (pos.x > player.axlGenericCursorWorldPos.x ? -1 : 1);
			if (charState is Run && xDir != newXDir) {
				sprite.reversed = true;
			}
			xDir = newXDir;
		}

		var axlBullet = player.weapons.FirstOrDefault(w => w is AxlBullet) as AxlBullet;

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

		bool bothHeld = shootHeld && altShootHeld;

		if (player.weapon is AxlBullet || player.weapon is DoubleBullet || 
			player.weapon is MettaurCrash || player.weapon is BeastKiller || player.weapon is MachineBullets || 
			player.weapon is RevolverBarrel || player.weapon is AncientGun) {
			(player.weapon as AxlWeapon)?.rechargeAxlBulletAmmo(player, this, shootHeld, 1);
		} else {
			foreach (var weapon in player.weapons) {
				if (weapon is AxlBullet || weapon is DoubleBullet || 
					weapon is MettaurCrash || weapon is BeastKiller || weapon is MachineBullets || 
					weapon is RevolverBarrel || weapon is AncientGun) {
					(weapon as AxlWeapon)?.rechargeAxlBulletAmmo(player, this, shootHeld, 2);
				}
			}
		}

		if (player.weapons.Count > 0 && player.weapons[0].type > 0) {
			player.axlBulletTypeLastAmmo[player.weapons[0].type] = player.weapons[0].ammo;
		}

		if (player.weapon is not AssassinBullet) {
			if (altShootHeld && !bothHeld && (player.weapon is AxlBullet || player.weapon is DoubleBullet ||
			player.weapon is MettaurCrash || player.weapon is BeastKiller || player.weapon is MachineBullets || 
			player.weapon is RevolverBarrel || player.weapon is AncientGun) && invulnTime == 0 && flag == null) {
				increaseCharge();
			} else {
				/* if (isCharging() && getChargeLevel() >= 3 && player.scrap >= 10 && !isWhiteAxl() && !hyperAxlUsed && (player.axlHyperMode > 0 || player.axlBulletType == 0)) {
					if (player.axlHyperMode == 0) {
						changeState(new HyperAxlStart(grounded), true);
					} else {
						if (!hyperAxlUsed) {
							hyperAxlUsed = true;
							//addHealth(player.maxHealth);
							foreach (var weapon in player.weapons) {
								weapon.ammo = weapon.maxAmmo;
							}
							stingChargeTime = 12;
							playSound("stingCharge", sendRpc: true);
						}
					}
				} */
				if (isCharging() && getChargeLevel() >= 3 && isStealthMode()) {
					stingChargeTime = 0;
					playSound("stingCharge", sendRpc: true);
				} else if (isCharging()) {
					if (player.weapon is AxlBullet || player.weapon is DoubleBullet ||
						player.weapon is MettaurCrash || player.weapon is BeastKiller || player.weapon is MachineBullets || 
						player.weapon is RevolverBarrel || player.weapon is AncientGun) {
						recoilTime = 0.2f;
						if (!isWhiteAxl()) {
							player.axlWeapon?.axlShoot(player, AxlBulletType.AltFire);
						} else {
							player.axlWeapon?.axlShoot(player, AxlBulletType.WhiteAxlCopyShot2);
						}
					}
				}
				stopCharge();

				// Handles Hyper activation.

				if (player.input.isHeld(Control.Special2, player) &&
					player.currency >= 10 && (!(charState is HyperAxlStart)) &&
					(!hyperAxlUsed) && (!(charState is WarpIn))
				) {
					hyperProgress += Global.spf;
				} else {
					hyperProgress = 0;
				}
				if (hyperProgress >= 1 && player.currency >= 10) {
					hyperProgress = 0;
					if (axlHyperMode == 0) {
						changeState(new HyperAxlStart(grounded), true);
					} else {
						if (!hyperAxlUsed) {
							hyperAxlUsed = true;
							//addHealth(player.maxHealth);
							foreach (var weapon in player.weapons) {
								weapon.ammo = weapon.maxAmmo;
							}
							stingChargeTime = 12;
							playSound("stingCharge", sendRpc: true);
						}
					}
				}
			}
		} else {
			if (shootHeld) {
				increaseCharge();
			} else {
				if (isCharging()) {
					shootAssassinShot();
				}
				stopCharge();
			}
		}
		chargeGfx();
		bool weCanShoot = (undisguiseTime == 0 && assassinTime == 0);
		if (weCanShoot) {
			// Axl bullet
			if (!isCharging() && player.axlWeapon != null) {
				if (player.weapon is AxlBullet && canShoot() && !player.weapon.noAmmo()) {
					if (shootHeld && shootTime == 0 && player.weapon.altShotCooldown == 0) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player);
					} else if ((altShootPressed || altShootRecentlyPressed) && shootTime == 0 && player.weapon.altShotCooldown == 0 && player.weapon.ammo >= 4) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
					}
				}
				switch(player.weapon) {
					case MettaurCrash:
					case BeastKiller: 
					case MachineBullets:
					case RevolverBarrel:
					case AncientGun:
						if (canShoot() && !player.weapon.noAmmo()) {
							if (shootHeld && shootTime == 0 && player.weapon.altShotCooldown == 0) {
								recoilTime = 0.2f;
								player.axlWeapon.axlShoot(player);
							} else if ((altShootPressed || altShootRecentlyPressed) && shootTime == 0 && player.weapon.altShotCooldown == 0 && player.weapon.ammo >= 4) {
								recoilTime = 0.2f;
								player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
							}
						}
						break; 
				}
				// Double bullet
				if (player.weapon is DoubleBullet && canShoot() && !(charState is LadderClimb) && !player.weapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player);
						if (bothHeld) player.axlWeapon.shootCooldown *= 2f;
					}
					if (bothHeld && player.weapon.altShotCooldown == 0) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
						if (bothHeld) player.axlWeapon.altShotCooldown *= 2f;
					} else if ((altShootPressed || altShootRecentlyPressed) && shootTime == 0 && player.weapon.altShotCooldown == 0 && player.weapon.ammo >= 4) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
					}
				}

				if (player.weapon is BlastLauncher && canShoot() && !(charState is LadderClimb)) {
					if (shootHeld && shootTime == 0 && player.weapon.ammo >= 1) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player);
					}

					if (player.axlLoadout.blastLauncherAlt == 0) {
						if (altShootPressed && shootTime == 0 && player.weapon.altShotCooldown == 0 && player.weapon.ammo >= 1) {
							recoilTime = 0.2f;
							player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
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

				if (player.weapon is RayGun && canShoot() && !player.weapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player);
					} else if (altShootHeld) {
						if (shootTime == 0) {
							recoilTime = 0.2f;
							player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
						}
						altRayGunHeld = player.axlWeapon.ammo > 0;

						if (player.axlLoadout.rayGunAlt == 0) {
							Point bulletDir = getAxlBulletDir();
							float whiteAxlMod = isWhiteAxl() ? 2 : 1;
							player.character.move(bulletDir.times(-50 * whiteAxlMod));
						}
					}
				}

				if (player.weapon is BlackArrow && canShoot() && !player.weapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player);
					} else if (altShootHeld && shootTime == 0 && player.weapon.altShotCooldown == 0) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
					}
				}

				if (player.weapon is SpiralMagnum && canShoot()) {
					if (shootHeld && shootTime == 0) {
						if (!player.weapon.noAmmo()) {
							recoilTime = 0.2f;
							player.axlWeapon.axlShoot(player);
						}
					} else {
						if (player.axlLoadout.spiralMagnumAlt == 0) {
							if (altShootPressed && player.axlWeapon.ammo > 0 && shootTime == 0 && player.weapon.altShotCooldown == 0) {
								recoilTime = 0.2f;
								player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
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

				if (player.weapon is BoundBlaster && canShoot() && !player.weapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player);
					} else if (altShootHeld && shootTime == 0 && player.weapon.altShotCooldown == 0) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
					}
				}

				if (player.weapon is PlasmaGun && canShoot() && !player.weapon.noAmmo()) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.2f;
						player.axlWeapon.altShotCooldown = player.axlWeapon.altFireCooldown;
						player.axlWeapon.axlShoot(player);
					} else if (altShootHeld) {
						if (player.axlLoadout.plasmaGunAlt == 0) {
							if (player.axlWeapon.altShotCooldown == 0 && grounded) {
								recoilTime = 0.2f;
								voltTornadoTime = 0.2f;
								player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
							}
						} else {
							if (player.axlWeapon.altShotCooldown == 0) {
								recoilTime = 0.2f;
								player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
							}
							altPlasmaGunHeld = player.axlWeapon.ammo > 0;
						}
					}
				}

				if (player.weapon is IceGattling && canShoot() && !(charState is LadderClimb) && player.weapon.ammo > 0) {
					if (altShootPressed && player.axlLoadout.iceGattlingAlt == 0 && gaeaShield == null) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
					}

					bool isAltRev = (altShootHeld && player.axlLoadout.iceGattlingAlt == 1);
					if (shootHeld || isAltRev) {
						isRevving = true;
						revTime += Global.spf * 2 * (isWhiteAxl() ? 10 : (isAltRev ? 2 : 1));
						if (revTime > 1) {
							revTime = 1;
						}
					}

					if (shootHeld && shootTime == 0 && revTime >= 1) {
						recoilTime = 0.2f;
						player.axlWeapon.axlShoot(player);
					}
				}

				if (player.weapon is FlameBurner && canShoot() && !(charState is LadderClimb) && player.weapon.ammo > 0) {
					if (shootHeld && shootTime == 0) {
						recoilTime = 0.05f;
						player.axlWeapon.axlShoot(player);
					}

					if (player.axlLoadout.flameBurnerAlt == 0) {
						if (altShootHeld && shootTime == 0 && player.weapon.altShotCooldown == 0) {
							recoilTime = 0.2f;
							player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
							player.axlWeapon.shootCooldown = 30;
						}
					} else {
						if (altShootHeld) {
							if (shootTime == 0 && player.weapon.altShotCooldown == 0) {
								recoilTime = 0.2f;
								player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
							}
						}
					}
				}

				// DNA Core
				if (player.weapon is DNACore && canShoot()) {
					AxlWeapon? realWeapon = player.weapons[player.weaponSlot] as AxlWeapon;
					if (realWeapon != null) {
						if (shootPressed && shootTime == 0) {
							if (flag != null) {
								Global.level.gameMode.setHUDErrorMessage(player, "Cannot transform with flag");
							} else if (player.currency < 1) {
								Global.level.gameMode.setHUDErrorMessage(player, "Transformation requires 1 Metal");
							} else if (isWhiteAxl() || isStealthMode()) {
								Global.level.gameMode.setHUDErrorMessage(player, "Cannot transform as Hyper Axl");
							} else {
								player.currency--;
								realWeapon.axlShoot(player);
							}
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

		if (player.axlWeapon is IceGattling) {
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
	}

	public override bool normalCtrl() {
		if (player.input.isPressed(Control.Jump, player) &&
			canJump() && !grounded && !isDashing && canAirDash() && flag == null
		) {
			dashedInAir++;
			changeState(new Hover(), true);
			return true;
		}
		if (dodgeRollCooldown == 0 && player.canControl && grounded) {
			if (charState is Crouch && player.input.isPressed(Control.Dash, player)) {
				changeState(new DodgeRoll(), true);
				return true;
			} else if (player.input.isPressed(Control.Dash, player) && player.input.checkDoubleTap(Control.Dash)) {
				changeState(new DodgeRoll(), true);
				return true;
			}
		}
		return base.normalCtrl();
	}

	public float getAimBackwardsAmount() {
		Point bulletDir = getAxlBulletDir();

		float forwardAngle = getShootXDir() == 1 ? 0 : 180;
		float bulletAngle = bulletDir.angle;
		if (bulletAngle > 180) bulletAngle = 360 - bulletAngle;

		float dist = MathF.Abs(forwardAngle - bulletAngle);
		dist = Helpers.clampMin0(dist - 90);
		return Helpers.clamp01(dist / 90f);
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
					player.axlScopeCursorWorldPos = Point.lerp(player.axlScopeCursorWorldPos, player.axlScopeCursorWorldLerpPos, Global.spf * 15);
					if (player.axlScopeCursorWorldPos.distanceTo(player.axlScopeCursorWorldLerpPos) < 1) {
						player.axlScopeCursorWorldPos = player.axlScopeCursorWorldLerpPos;
						isZoomingIn = false;
					}
				} else if (isZoomingOut) {
					Point destPos = player.axlZoomOutCursorDestPos;
					player.axlScopeCursorWorldPos = Point.lerp(player.axlScopeCursorWorldPos, destPos, Global.spf * 15);
					float dist = player.axlScopeCursorWorldPos.distanceTo(destPos);
					if (dist < 50 && !isZoomOutPhase1Done) {
						//player.axlCursorPos = player.axlScopeCursorWorldPos.addxy(-Global.level.camX, -Global.level.camY);
						isZoomOutPhase1Done = true;
					}
					if (dist < 1) {
						player.axlScopeCursorWorldPos = destPos;
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
			player.axlCursorPos = worldCursorPos.addxy(-Global.level.camX, -Global.level.camY);
			lockOn(out _);
			return;
		}

		if (charState is Assassinate) {
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
			if (player.axlWeapon?.isTwoHanded(false) != true) {
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

		//player.axlCursorPos = pos.addxy(xOff * xDir, yOff).addxy(aimDir.x, aimDir.y).addxy(-Global.level.camX, -Global.level.camY);
		Point destCursorPos = pos.addxy(xOff * xDir, yOff).addxy(aimDir.x, aimDir.y).addxy(-Global.level.camX, -Global.level.camY);

		if (charState is Dash || charState is AirDash) {
			destCursorPos = destCursorPos.addxy(15 * xDir, 0);
		}

		// Try to see if where cursor will go to has auto-aim target. If it does, make that the dest, not the actual dest
		Point oldCursorPos = player.axlCursorPos;
		player.axlCursorPos = destCursorPos;
		lockOn(out Point? lockOnPoint);
		if (lockOnPoint != null) {
			destCursorPos = lockOnPoint.Value;
		}
		player.axlCursorPos = oldCursorPos;

		// Lerp to the new target
		//player.axlCursorPos = Point.moveTo(player.axlCursorPos, destCursorPos, Global.spf * 1000);
		if (!Options.main.aimAnalog) {
			player.axlCursorPos = Point.lerp(player.axlCursorPos, destCursorPos, Global.spf * 15);
		} else {
			player.axlCursorPos = destCursorPos;
		}

		lastDirToCursor = pos.directionTo(player.axlCursorWorldPos);
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
				var dirToCursor = getAxlBulletPos().directionTo(player.axlGenericCursorWorldPos);

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
		player.assassinCursorPos = null;

		if (!Options.main.lockOnSound) return;
		if (player.isDisguisedAxl && !player.isAxl && player.axlWeapon is not AssassinBullet) return;
		if (player.isDisguisedAxl && player.axlWeapon is UndisguiseWeapon) return;
		if (player.input.isCursorLocked(player)) return;

		axlCursorTarget = getLockOnTarget();

		if (axlCursorTarget != null && prevTarget == null && player.isMainPlayer && targetSoundCooldown == 0) {
			Global.playSound("axlTarget", false);
			targetSoundCooldown = Global.spf;
		}

		if (axlCursorTarget != null) {
			player.axlLockOnCursorPos = (axlCursorTarget as Character).getAimCenterPos();
			lockOnPoint = player.axlLockOnCursorPos.addxy(-Global.level.camX, -Global.level.camY);
			// player.axlCursorPos = (axlCursorTarget as Character).getAimCenterPos().addxy(-Global.level.camX, -Global.level.camY);

			if (player.axlWeapon is AssassinBullet) {
				player.assassinCursorPos = lockOnPoint;
			}
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
			player.axlScopeCursorWorldPos.x += Global.spf * Global.screenW * (Input.aimX / 100f) * sensitivity;
			axisXMoved = true;
		} else if (Input.aimX < -aimThreshold && Input.aimX <= Input.lastAimX) {
			player.axlScopeCursorWorldPos.x -= Global.spf * Global.screenW * (MathF.Abs(Input.aimX) / 100f) * sensitivity;
			axisXMoved = true;
		}
		if (Input.aimY > aimThreshold && Input.aimY >= Input.lastAimY) {
			player.axlScopeCursorWorldPos.y += Global.spf * Global.screenW * (Input.aimY / 100f) * sensitivity;
			axisYMoved = true;
		} else if (Input.aimY < -aimThreshold && Input.aimY <= Input.lastAimY) {
			player.axlScopeCursorWorldPos.y -= Global.spf * Global.screenW * (MathF.Abs(Input.aimY) / 100f) * sensitivity;
			axisYMoved = true;
		}

		// Controller or keyboard button based aim section
		if (!axisXMoved) {
			if (player.input.isHeld(Control.AimLeft, player)) {
				player.axlScopeCursorWorldPos.x -= Global.spf * 200 * sensitivity;
				axisXMoved = true;
			} else if (player.input.isHeld(Control.AimRight, player)) {
				player.axlScopeCursorWorldPos.x += Global.spf * 200 * sensitivity;
				axisXMoved = true;
			}
		}
		if (!axisYMoved) {
			if (player.input.isHeld(Control.AimUp, player)) {
				player.axlScopeCursorWorldPos.y -= Global.spf * 200 * sensitivity;
				axisYMoved = true;
			} else if (player.input.isHeld(Control.AimDown, player)) {
				player.axlScopeCursorWorldPos.y += Global.spf * 200 * sensitivity;
				axisYMoved = true;
			}
		}

		// Mouse based aim
		if (!Menu.inMenu && !player.isAI) {
			if (Options.main.useMouseAim) {
				player.axlScopeCursorWorldPos.x += Input.mouseDeltaX * 0.125f * sensitivity;
				player.axlScopeCursorWorldPos.y += Input.mouseDeltaY * 0.125f * sensitivity;
			}
		}

		// Aim assist
		if (!Options.main.useMouseAim && Options.main.lockOnSound)// && !axisXMoved && !axisYMoved)
		{
			Character? target = null;
			float bestDist = float.MaxValue;
			foreach (var enemy in Global.level.players) {
				if (enemy.character != null && enemy.character.canBeDamaged(player.alliance, player.id, null) && !enemy.character.isStealthy(player.alliance)) {
					float cursorDist = enemy.character.getAimCenterPos().distanceTo(player.axlScopeCursorWorldPos);
					if (cursorDist < bestDist) {
						bestDist = cursorDist;
						target = enemy.character;
					}
				}
			}
			const float aimAssistRange = 25;
			if (target != null && bestDist < aimAssistRange) {
				//float aimAssistPower = (float)Math.Pow(1 - (bestDist / aimAssistRange), 2);
				player.axlScopeCursorWorldPos = Point.lerp(player.axlScopeCursorWorldPos, target.getAimCenterPos(), Global.spf * 5);
				//player.axlScopeCursorWorldPos = target.getAimCenterPos();
			}
		}

		// Aimbot
		if (!player.isAI) {
			//var target = Global.level.getClosestTarget(player.axlScopeCursorWorldPos, player.alliance, true);
			//if (target != null && target.pos.distanceTo(player.axlScopeCursorWorldPos) < 100) player.axlScopeCursorWorldPos = target.getAimCenterPos();
		}

		Point centerPos = getAimCenterPos();
		if (player.axlScopeCursorWorldPos.distanceTo(centerPos) > player.zoomRange) {
			player.axlScopeCursorWorldPos = centerPos.add(centerPos.directionTo(player.axlScopeCursorWorldPos).normalize().times(player.zoomRange));
		}

		getMouseTargets();
	}

	public void getMouseTargets() {
		axlCursorTarget = null;
		axlHeadshotTarget = null;

		int cursorSize = 1;
		var shape = new Rect(player.axlGenericCursorWorldPos.x - cursorSize, player.axlGenericCursorWorldPos.y - cursorSize, player.axlGenericCursorWorldPos.x + cursorSize, player.axlGenericCursorWorldPos.y + cursorSize).getShape();
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
			if (enemy.character != null && enemy.character.canBeDamaged(player.alliance, player.id, null) && enemy.character.getHeadPos() != null) {
				if (player.axlGenericCursorWorldPos.distanceTo(enemy.character.getHeadPos().Value) < headshotRadius) {
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
			player.axlCursorPos.x += Global.spf * Global.screenW * (Input.aimX / 100f) * sensitivity;
			axisXMoved = true;
		} else if (Input.aimX < -aimThreshold && Input.aimX <= Input.lastAimX) {
			player.axlCursorPos.x -= Global.spf * Global.screenW * (MathF.Abs(Input.aimX) / 100f) * sensitivity;
			axisXMoved = true;
		}
		if (Input.aimY > aimThreshold && Input.aimY >= Input.lastAimY) {
			player.axlCursorPos.y += Global.spf * Global.screenW * (Input.aimY / 100f) * sensitivity;
			axisYMoved = true;
		} else if (Input.aimY < -aimThreshold && Input.aimY <= Input.lastAimY) {
			player.axlCursorPos.y -= Global.spf * Global.screenW * (MathF.Abs(Input.aimY) / 100f) * sensitivity;
			axisYMoved = true;
		}

		// Controller or keyboard button based aim section
		if (!axisXMoved) {
			if (player.input.isHeld(Control.AimLeft, player)) {
				player.axlCursorPos.x -= Global.spf * 200 * sensitivity;
			} else if (player.input.isHeld(Control.AimRight, player)) {
				player.axlCursorPos.x += Global.spf * 200 * sensitivity;
			}
		}
		if (!axisYMoved) {
			if (player.input.isHeld(Control.AimUp, player)) {
				player.axlCursorPos.y -= Global.spf * 200 * sensitivity;
			} else if (player.input.isHeld(Control.AimDown, player)) {
				player.axlCursorPos.y += Global.spf * 200 * sensitivity;
			}
		}

		// Mouse based aim
		if (!Menu.inMenu && !player.isAI) {
			if (Options.main.useMouseAim) {
				player.axlCursorPos.x += Input.mouseDeltaX * 0.125f * sensitivity;
				player.axlCursorPos.y += Input.mouseDeltaY * 0.125f * sensitivity;
			}
			player.axlCursorPos.x = Helpers.clamp(player.axlCursorPos.x, 0, Global.viewScreenW);
			player.axlCursorPos.y = Helpers.clamp(player.axlCursorPos.y, 0, Global.viewScreenH);
		}

		if (isWarpIn()) {
			player.axlCursorPos = getCenterPos().addxy(-Global.level.camX + 50 * xDir, -Global.level.camY);
		}

		// aimbot
		if (player.isAI) {
			var target = Global.level.getClosestTarget(pos, player.alliance, true);
			if (target != null) {
				player.axlCursorPos = target.pos.addxy(
					-Global.level.camX,
					-Global.level.camY - ((target as Character)?.charState is InRideArmor ? 0 : 16)
				);
			};
		}

		getMouseTargets();
	}

	public bool isAxlLadderShooting() {
		if (player.weapon is AssassinBullet) return false;
		if (recoilTime > 0) return true;
		bool canShootBool = canShoot() && !player.weapon.noAmmo() && player.axlWeapon != null && !player.axlWeapon.isTwoHanded(true) && shootTime == 0;
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

		if (player.axlWeapon != null) netArmAngle = getShootAngle();

		float angleOffset = 0;
		if (recoilTime > 0.1f) angleOffset = (0.2f - recoilTime) * 50;
		else if (recoilTime < 0.1f && recoilTime > 0) angleOffset = (0.1f - (0.1f - recoilTime)) * 50;
		angleOffset *= -axlXDir;
		netArmAngle += angleOffset;

		if (charState is DarkHoldState dhs) {
			netArmAngle = dhs.lastArmAngle;
		}

		if (charState is LadderClimb) {
			if (isAxlLadderShooting()) {
				xDir = (pos.x > player.axlGenericCursorWorldPos.x ? -1 : 1);
				changeSprite("axl_ladder_shoot", true);
			} else {
				changeSprite("axl_ladder_climb", true);
			}
		}

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
				bulletPos.x, bulletPos.y, player.axlGenericCursorWorldPos.x,
				player.axlGenericCursorWorldPos.y, Color.Magenta, 1, ZIndex.Default + 1
			);
		}

		drawAxlCursor();

		//DEBUG CODE
		/*
		if (Keyboard.IsKeyPressed(Key.I)) player.axlWeapon.renderAngleOffset++;
		else if (Keyboard.IsKeyPressed(Key.J)) player.axlWeapon.renderAngleOffset--;
		Global.debugString1 = player.axlWeapon.renderAngleOffset.ToString();
		if (Keyboard.IsKeyPressed(Key.K)) player.axlWeapon.shootAngleOffset++;
		else if (Keyboard.IsKeyPressed(Key.L)) player.axlWeapon.shootAngleOffset--;
		Global.debugString2 = player.axlWeapon.shootAngleOffset.ToString();
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
			Global.sprites["axl_cursor"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
			if (player.assassinHitPos?.isHeadshot == true && player.weapon is AssassinBullet && Global.level.isTraining()) {
				Global.sprites["hud_kill"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
			}
		}
		if (!Options.main.useMouseAim) {
			if (player.axlWeapon != null && (player.axlWeapon is AssassinBullet || player.input.isCursorLocked(player))) {
				Point bulletPos = getAxlBulletPos();
				float radius = 120;
				float ang = getShootAngle();
				float x = Helpers.cosd(ang) * radius * getShootXDir();
				float y = Helpers.sind(ang) * radius * getShootXDir();
				DrawWrappers.DrawLine(bulletPos.x, bulletPos.y, bulletPos.x + x, bulletPos.y + y, new Color(255, 0, 0, 128), 2, ZIndex.HUD, true);
				if (axlCursorTarget != null && player.assassinHitPos?.isHeadshot == true && player.weapon is AssassinBullet && Global.level.isTraining()) {
					Global.sprites["hud_kill"].draw(0, player.axlLockOnCursorPos.x, player.axlLockOnCursorPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
				}
			}
			if (axlCursorTarget != null && !isAnyZoom()) {
				axlCursorAngle += Global.spf * 360;
				if (axlCursorAngle > 360) axlCursorAngle -= 360;
				Global.sprites["axl_cursor_x7"].draw(0, player.axlLockOnCursorPos.x, player.axlLockOnCursorPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1, angle: axlCursorAngle);
				//drawBloom();
			}
		}

		/*
		if (player.weapon.ammo <= 0)
		{
			if (player.weapon.rechargeCooldown > 0)
			{
				float textPosX = player.axlCursorPos.x;
				float textPosY = player.axlCursorPos.y - 20;
				if (!Options.main.useMouseAim)
				{
					textPosX = pos.x - Global.level.camX / Global.viewSize;
					textPosY = (pos.y - 50 - Global.level.camY) / Global.viewSize;
				}
				DrawWrappers.DeferTextDraw(() =>
				{
					Helpers.drawTextStd(
						"Reload:" + player.weapon.rechargeCooldown.ToString("0.0"),
						textPosX, textPosY, Alignment.Center, fontSize: 20,
						outlineColor: Helpers.getAllianceColor()
					);
				});
			}
		}
		*/
	}

	public void drawBloom() {
		Global.sprites["axl_cursor_top"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
		Global.sprites["axl_cursor_bottom"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y + 1, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
		Global.sprites["axl_cursor_left"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
		Global.sprites["axl_cursor_right"].draw(0, player.axlCursorWorldPos.x + 1, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
		Global.sprites["axl_cursor_dot"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
	}

	float mcFrameTime;
	float mcMaxFrameTime = 0.03f;
	int mcFrameIndex;
	Sprite pistol2Sprite = new Sprite("axl_arm_pistol2");
	public void drawArm(float angle) {
		long zIndex = this.zIndex - 1;
		Point gunArmOrigin;
		if (charState is Assassinate assasinate) {
			gunArmOrigin = getAxlGunArmOrigin();
			getAxlArmSprite().draw(
				0, gunArmOrigin.x, gunArmOrigin.y,
				axlXDir, 1, getRenderEffectSet(), 1, 1, 1,
				zIndex, angle: angle, shaders: getShaders()
			);
			return;
		}

		if (player.axlWeapon != null && player.axlWeapon.isTwoHanded(false)) {
			zIndex = this.zIndex + 1;
		}
		gunArmOrigin = getAxlGunArmOrigin();

		if (player.axlWeapon is DoubleBullet) {
			var armPos = getDoubleBulletArmPos();
			if (shouldDraw()) {
				pistol2Sprite.draw(0, gunArmOrigin.x + armPos.x * axlXDir, gunArmOrigin.y + armPos.y, axlXDir, 1, getRenderEffectSet(), 1, 1, 1, this.zIndex + 100, angle: angle, shaders: getShaders(), actor: this);
			}
		}

		if (shouldDraw()) {
			int frameIndex = 0;
			if (player.axlWeapon is IceGattling) {
				revIndex += revTime * Global.spf * 60;
				if (revIndex >= 4) {
					revIndex = 0;
				}

				if (revTime > 0) {
					frameIndex = (int)revIndex;
				}
			}
			if (player.axlWeapon is AxlBullet ab && ab.type == (int)AxlBulletWeaponType.MetteurCrash) {
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

		return player.axlWeapon?.sprite ?? "axl_arm_pistol";
	}

	public Sprite getAxlArmSprite() {
		if (!ownedByLocalPlayer && Global.spriteNameByIndex.ContainsKey(netAxlArmSpriteIndex)) {
			return new Sprite(Global.spriteNameByIndex[netAxlArmSpriteIndex]);
		}

		return new Sprite(getAxlArmSpriteName());
	}

	public Point getCorrectedCursorPos() {
		if (player.axlWeapon == null) return new Point();
		Point cursorPos = player.axlGenericCursorWorldPos;
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
		if (player.axlWeapon == null) return new Point();
		Sprite sprite = getAxlArmSprite();
		Point muzzlePOI = sprite.animData.frames[0].POIs[0];

		float horizontalOffX = 0;// Helpers.cosd(angle) * muzzlePOI.x;
		float horizontalOffY = 0;// Helpers.sind(angle) * muzzlePOI.x;

		float verticalOffX = -axlXDir * Helpers.sind(angle) * muzzlePOI.y;
		float verticalOffY = axlXDir * Helpers.cosd(angle) * muzzlePOI.y;

		return new Point(horizontalOffX + verticalOffX, horizontalOffY + verticalOffY);
	}

	public Point getAxlBulletPos(int poiIndex = 0) {
		if (player.axlWeapon == null) return new Point();

		Point gunArmOrigin = getAxlGunArmOrigin();

		var doubleBullet = player.weapon as DoubleBullet;
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
		if (player.axlWeapon == null) return new Point();
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
		Point cursorPos = player.axlGenericCursorWorldPos;
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
		if (player.axlWeapon == null) return new Point();
		if (player.axlWeapon.isTwoHanded(false)) {
			if (player.axlWeapon is FlameBurner) return new Point(-6, 1);
			else if (player.axlWeapon is IceGattling) return new Point(-6, 1);
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
		if (!player.isAxl) return;
		if (Global.level.is1v1()) return;

		if (player.weapons.Count((Weapon weapon) => weapon is DNACore) < 4) {
			var dnaCoreWeapon = new DNACore(hitChar);
			dnaCoreWeapon.index = (int)WeaponIds.DNACore - player.weapons.Count;
			if (player.isDisguisedAxl) {
				player.oldWeapons.Add(dnaCoreWeapon);
			} else {
				player.weapons.Add(dnaCoreWeapon);
			}
			player.savedDNACoreWeapons.Add(dnaCoreWeapon);
		}
	}

	public int getAxlXDir() {
		if (player.axlWeapon != null && (player.axlWeapon.isTwoHanded(false))) {
			return pos.x < player.axlGenericCursorWorldPos.x ? 1 : -1;
		}
		return xDir;
	}

	public bool isWhiteAxl() {
		return player.isAxl && whiteAxlTime > 0;
	}

	public bool isStealthMode() {
		return player.isAxl && isInvisible();
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
			paletteNum = (Global.frameCount % (mod * 2)) < mod ? 0 : 1;
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
		if (gaeaShield != null) return false;
		if (sniperMissileProj != null) return false;
		if (revTime > 0.5f) return false;

		return base.canChangeWeapons();
	}

	public override float getRunSpeed() {
		float runSpeed = 90;
		if (player.isAxl && shootTime > 0) {
			runSpeed = 90 - getAimBackwardsAmount() * 25;
		}
		return runSpeed * getRunDebuffs();
	}

	public override float getDashSpeed() {
		if (flag != null || !isDashing) {
			return getRunSpeed();
		}
		float dashSpeed = 210;

		if (player.axlWeapon != null && player.axlWeapon.isTwoHanded(false)) {
			dashSpeed *= 0.875f;
		}
		if (shootTime > 0) {
			dashSpeed = dashSpeed - getAimBackwardsAmount() * 50;
		}
		return dashSpeed * getRunDebuffs();
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
			return player.axlScopeCursorWorldPos;
		}
		return base.getCamCenterPos(ignoreZoom);
	}

	public override bool changeState(CharState newState, bool forceChange = false) {
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
		string spriteName = null, string fadeSound = null,
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
		return stingChargeTime > 0 && stealthRevealTime == 0;
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
		if (player.weapon.canHealAmmo && player.weapon.ammo < player.weapon.maxAmmo) {
			return player.weapon;
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

	public override bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		bool damaged = base.canBeDamaged(damagerAlliance, damagerPlayerId, projId);
		if (stealthActive) {
			return false;
		}
		return damaged;
	}

	public override bool isStealthy(int alliance) {
		return (player.alliance != alliance && (stealthActive || player.isDisguisedAxl && !disguiseCoverBlown));
	}

	public override bool isCCImmuneHyperMode() {
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

	public void configureWeapons() {
		if (Global.level.isTraining() && !Global.level.server.useLoadout) {
			weapons = Weapon.getAllAxlWeapons(player.axlLoadout).Select(w => w.clone()).ToList();
			weapons[0] = getAxlBullet(player.axlBulletType);
		} else if (Global.level.is1v1()) {
			weapons.Add(new AxlBullet());
			weapons.Add(new RayGun(player.axlLoadout.rayGunAlt));
			weapons.Add(new BlastLauncher(player.axlLoadout.blastLauncherAlt));
			weapons.Add(new BlackArrow(player.axlLoadout.blackArrowAlt));
			weapons.Add(new SpiralMagnum(player.axlLoadout.spiralMagnumAlt));
			weapons.Add(new BoundBlaster(player.axlLoadout.boundBlasterAlt));
			weapons.Add(new PlasmaGun(player.axlLoadout.plasmaGunAlt));
			weapons.Add(new IceGattling(player.axlLoadout.iceGattlingAlt));
			weapons.Add(new FlameBurner(player.axlLoadout.flameBurnerAlt));
		} else {
			weapons = player.loadout.axlLoadout.getWeaponsFromLoadout();
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

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();

		customData.Add((byte)(player.weapon?.index ?? 0));
		customData.Add((byte)MathF.Ceiling(player.weapon?.ammo ?? 0));

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
		player.changeWeaponFromWi(data[0]);
		if (player.weapon != null) {
			player.weapon.ammo = data[1];
		}
		netArmAngle = Helpers.byteToDegree(data[2]);
		player.axlBulletType = data[3];

		bool[] boolData = Helpers.byteToBoolArray(data[4]);
		shouldDrawArmNet = boolData[0];
		stealthActive = boolData[1];

		netAxlArmSpriteIndex = BitConverter.ToUInt16(data[5..7]);
	}
}
