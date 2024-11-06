using System;
using System.Collections.Generic;
using System.Linq;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public partial class Player {
	public List<Weapon> weapons = new();
	public List<Weapon> oldWeapons = new();

	public Weapon nonOwnerWeapon;

	public int prevWeaponSlot;
	private int _weaponSlot;
	public int weaponSlot {
		get {
			return _weaponSlot;
		}
		set {
			if (_weaponSlot != value) {
				prevWeaponSlot = _weaponSlot;
				_weaponSlot = value;
			}
		}
	}

	public Weapon weapon {
		get {
			if (ownedByLocalPlayer) {
				if (weapons.InRange(weaponSlot)) {
					return weapons[weaponSlot];
				}
				return new Weapon();
			}
			return nonOwnerWeapon ?? new Weapon();
		}
	}

	public Weapon lastHudWeapon = null;

	public AxlWeapon? axlWeapon {
		get {
			return weapon as AxlWeapon;
		}
	}

	public MaverickWeapon? maverickWeapon {
		get { return weapon as MaverickWeapon; }
	}

	public List<Maverick> mavericks {
		get {
			var mavericks = new List<Maverick>();
			foreach (var weapon in weapons) {
				if (weapon is MaverickWeapon mw && mw.maverick != null) {
					mavericks.Add(mw.maverick);
				}
			}
			return mavericks;
		}
	}

	public MaverickWeapon? currentMaverickWeapon {
		get {
			foreach (var weapon in weapons) {
				if (weapon is MaverickWeapon mw && mw.maverick != null && mw.maverick == currentMaverick) {
					return mw;
				}
			}
			return null;
		}
	}

	public Maverick? currentMaverick {
		get {
			var mw = weapons.FirstOrDefault(
				w => w is MaverickWeapon mw && mw.maverick?.aiBehavior == MaverickAIBehavior.Control
			);
			return (mw as MaverickWeapon)?.maverick;
		}
	}

	public bool shouldBlockMechSlotScroll() {
		if (character is Vile { isVileMK5: true, startRideArmor: not null }) {
			return false;
		}
		return Options.main.blockMechSlotScroll;
		
	}

	public bool gridModeHeld;
	public Point gridModePos = new Point();
	public void changeWeaponControls() {
		if (character == null) return;
		if (!character.canChangeWeapons()) return;

		if (isGridModeEnabled() && weapons.Count > 1) {
			if (input.isHeldMenu(Control.WeaponRight)) {
				gridModeHeld = true;
				if (input.isPressedMenu(Control.Up)) gridModePos.y--;
				else if (input.isPressedMenu(Control.Down)) gridModePos.y++;

				if (input.isPressedMenu(Control.Left)) gridModePos.x--;
				else if (input.isPressedMenu(Control.Right)) gridModePos.x++;

				if (gridModePos.y < -1) gridModePos.y = -1;
				if (gridModePos.y > 1) gridModePos.y = 1;
				if (gridModePos.x < -1) gridModePos.y = -1;
				if (gridModePos.x > 1) gridModePos.x = 1;

				var gridPoints = gridModePoints();

				for (int i = 0; i < weapons.Count; i++) {
					if (i >= gridPoints.Length) break;
					var gridPoint = gridPoints[i];
					if (gridModePos.x == gridPoint.x && gridModePos.y == gridPoint.y && weapons.Count >= i + 1) {
						changeWeaponSlot(i);
					}
				}
			} else {
				gridModeHeld = false;
				gridModePos = new Point();
			}
			return;
		}

		if ((isAxl || isDisguisedAxl) && isMainPlayer) {
			if (Input.mouseScrollUp) {
				weaponLeft();
				return;
			} else if (Input.mouseScrollDown) {
				weaponRight();
				return;
			}
		}

		if (input.isPressed(Control.WeaponLeft, this)) {
			if (isDisguisedAxl && isZero && input.isHeld(Control.Down, this)) return;
			weaponLeft();
		} else if (input.isPressed(Control.WeaponRight, this)) {
			if (isDisguisedAxl && isZero && input.isHeld(Control.Down, this)) return;
			weaponRight();
		} else if (character != null && !Control.isNumberBound(realCharNum, Options.main.axlAimMode)) {
			if (isVile && weapon is MechMenuWeapon mmw &&
				character.startRideArmor == null &&
				shouldBlockMechSlotScroll()
			) {
				if (input.isPressed(Key.Num1, canControl)) {
					selectedRAIndex = 0;
					character.onMechSlotSelect(mmw);
				} else if (input.isPressed(Key.Num2, canControl)) {
					selectedRAIndex = 1;
					character.onMechSlotSelect(mmw);
				} else if (input.isPressed(Key.Num3, canControl)) {
					selectedRAIndex = 2;
					character.onMechSlotSelect(mmw);
				} else if (input.isPressed(Key.Num4, canControl)) {
					selectedRAIndex = 3;
					character.onMechSlotSelect(mmw);
				} else if (input.isPressed(Key.Num5, canControl)) {
					selectedRAIndex = 4;
					character.onMechSlotSelect(mmw);
				}
			} else {
				if (input.isPressed(Key.Num1, canControl) && weapons.Count >= 1) {
					changeWeaponSlot(0);
					if (isVile && weapon is MechMenuWeapon mmw2 && shouldBlockMechSlotScroll()) {
						character.onMechSlotSelect(mmw2);
					}
				} else if (input.isPressed(Key.Num2, canControl) && weapons.Count >= 2) {
					changeWeaponSlot(1);
					if (isVile && weapon is MechMenuWeapon mmw2 && shouldBlockMechSlotScroll()) {
						character.onMechSlotSelect(mmw2);
					}
				} else if (input.isPressed(Key.Num3, canControl) && weapons.Count >= 3) {
					changeWeaponSlot(2);
					if (isVile && weapon is MechMenuWeapon mmw2 && shouldBlockMechSlotScroll()) {
						character.onMechSlotSelect(mmw2);
					}
				} else if (input.isPressed(Key.Num4, canControl) && weapons.Count >= 4) { changeWeaponSlot(3); } else if (input.isPressed(Key.Num5, canControl) && weapons.Count >= 5) { changeWeaponSlot(4); } else if (input.isPressed(Key.Num6, canControl) && weapons.Count >= 6) { changeWeaponSlot(5); } else if (input.isPressed(Key.Num7, canControl) && weapons.Count >= 7) { changeWeaponSlot(6); } else if (input.isPressed(Key.Num8, canControl) && weapons.Count >= 8) { changeWeaponSlot(7); } else if (input.isPressed(Key.Num9, canControl) && weapons.Count >= 9) { changeWeaponSlot(8); } else if (input.isPressed(Key.Num0, canControl) && weapons.Count >= 10) { changeWeaponSlot(9); }
			}
		}
	}

	public void changeWeaponFromWi(int weaponIndex) {
		nonOwnerWeapon = weapons.FirstOrDefault(w => w.index == weaponIndex) ?? nonOwnerWeapon;
	}

	public void changeToSigmaSlot() {
		for (int i = 0; i < weapons.Count; i++) {
			if (weapons[i] is SigmaMenuWeapon) {
				changeWeaponSlot(i);
				return;
			}
		}
	}

	public void removeWeaponSlot(int index) {
		if (index < 0 || index >= weapons.Count) return;
		if (index < weaponSlot && weaponSlot > 0) weaponSlot--;
		for (int i = weapons.Count - 1; i >= 0; i--) {
			if (i == index) {
				weapons.RemoveAt(i);
				return;
			}
		}
	}

	public void changeWeaponSlot(int newWeaponSlot) {
		if (weaponSlot == newWeaponSlot) return;
		if (isDead) return;
		if (!weapons.InRange(newWeaponSlot)) return;
		if (weapons[newWeaponSlot].index == (int)WeaponIds.MechMenuWeapon) {
			selectedRAIndex = 0;
		}

		Weapon oldWeapon = weapon;
		if (oldWeapon is MechMenuWeapon mmw) {
			mmw.isMenuOpened = false;
		}

		weaponSlot = newWeaponSlot;
		Weapon newWeapon = weapon;

		if (newWeapon is MaverickWeapon mw) {
			mw.selCommandIndex = 1;
			mw.selCommandIndexX = 1;
			mw.isMenuOpened = false;
		}

		character.onWeaponChange(oldWeapon, newWeapon);

		/* if (isX) {
			if (character.getChargeLevel() >= 2) {
				newWeapon.shootTime = 0;
			} else {
				// Switching from laggy move (like tornado) to a fast one
				if (oldWeapon.switchCooldown != null && oldWeapon.shootTime > 0) {
					newWeapon.shootTime = Math.Max(newWeapon.shootTime, oldWeapon.switchCooldown.Value);
				} else {
					newWeapon.shootTime = Math.Max(newWeapon.shootTime, oldWeapon.shootTime);
				}
				/*
				if (newWeapon is NovaStrike ns) {
					ns.shootTime = 0;
				}
				
			}
		} */

		if (character is Axl axl) {
			if (oldWeapon is AxlWeapon) {
				axl.axlSwapTime = axl.switchTime;
				axl.axlAltSwapTime = axl.altSwitchTime;
			}
			if (axl.isZooming()) {
				axl.zoomOut();
			}
		}
	}

	public void weaponLeft() {
		int ws = weaponSlot - 1;
label:
		if (ws < 0) {
			ws = weapons.Count - 1;
			if (shouldBlockMechSlotScroll() && isVile && weapons.ElementAtOrDefault(ws) is MechMenuWeapon) {
				ws--;
				if (ws < 0) ws = 0;
			}
		}
		if ((weapons.ElementAtOrDefault(ws) is GigaCrush && Options.main.gigaCrushSpecial) || (weapons.ElementAtOrDefault(ws) is NovaStrike && Options.main.novaStrikeSpecial)) {
			ws--;
			goto label;
		}
		changeWeaponSlot(ws);
	}

	public void weaponRight() {
		int ws = weaponSlot + 1;
label:
		int max = weapons.Count;
		if (shouldBlockMechSlotScroll() && isVile && weapons.ElementAtOrDefault(max - 1) is MechMenuWeapon) {
			max--;
		}
		if (ws >= max) {
			ws = 0;
		}
		if ((weapons.ElementAtOrDefault(ws) is GigaCrush && Options.main.gigaCrushSpecial) || (weapons.ElementAtOrDefault(ws) is NovaStrike && Options.main.novaStrikeSpecial)) {
			ws++;
			goto label;
		}
		changeWeaponSlot(ws);
	}

	public void clearSigmaWeapons() {
		preSigmaReviveWeapons = new List<Weapon>(weapons);
		weapons.Clear();
	}

	public List<Weapon>? preSigmaReviveWeapons;
	public void configureWeapons() {
		if (!ownedByLocalPlayer) {
			return;
		}

		var oldGigaCrush = weapons?.Find(w => w is GigaCrush);
		var oldHyperbuster = weapons?.Find(w => w is HyperCharge);
		var oldWeapons = weapons;

		if (preSigmaReviveWeapons != null) {
			oldWeapons = preSigmaReviveWeapons;
			preSigmaReviveWeapons = null;
		}
		weapons = new List<Weapon>();


		if (isX) {
			if (Global.level.isTraining() && !Global.level.server.useLoadout) {
				weapons = Weapon.getAllXWeapons().Select(w => w.clone()).ToList();
				if (hasArmArmor(3)) weapons.Add(new HyperCharge());
				if (hasBodyArmor(2)) weapons.Add(new GigaCrush());
				if (hasUltimateArmor()) weapons.Add(new NovaStrike(this));
			} else if (Global.level.is1v1()) {
				if (xArmor1v1 == 1) {
					weapons.Add(new XBuster());
					weapons.Add(new HomingTorpedo());
					weapons.Add(new ChameleonSting());
					weapons.Add(new RollingShield());
					weapons.Add(new FireWave());
					weapons.Add(new StormTornado());
					weapons.Add(new ElectricSpark());
					weapons.Add(new BoomerangCutter());
					weapons.Add(new ShotgunIce());
				} else if (xArmor1v1 == 2) {
					weapons.Add(new XBuster());
					weapons.Add(new CrystalHunter());
					weapons.Add(new BubbleSplash());
					weapons.Add(new SilkShot());
					weapons.Add(new SpinWheel());
					weapons.Add(new SonicSlicer());
					weapons.Add(new StrikeChain());
					weapons.Add(new MagnetMine());
					weapons.Add(new SpeedBurner(this));
					weapons.Add(new GigaCrush());
				} else if (xArmor1v1 == 3) {
					weapons.Add(new XBuster());
					weapons.Add(new AcidBurst());
					weapons.Add(new ParasiticBomb());
					weapons.Add(new TriadThunder());
					weapons.Add(new SpinningBlade());
					weapons.Add(new RaySplasher());
					weapons.Add(new GravityWell());
					weapons.Add(new FrostShield());
					weapons.Add(new TornadoFang());
					weapons.Add(new HyperCharge());
				}

				foreach (var enemyPlayer in Global.level.players) {
					if (enemyPlayer.maverick1v1 != null && enemyPlayer.alliance != alliance) {
						Weapon? weaponToDeplete = null;
						if (enemyPlayer.maverick1v1 == 0) weaponToDeplete = weapons.FirstOrDefault(w => w is FireWave);
						if (enemyPlayer.maverick1v1 == 1) weaponToDeplete = weapons.FirstOrDefault(w => w is ShotgunIce);
						if (enemyPlayer.maverick1v1 == 2) weaponToDeplete = weapons.FirstOrDefault(w => w is ElectricSpark);
						if (enemyPlayer.maverick1v1 == 3) weaponToDeplete = weapons.FirstOrDefault(w => w is RollingShield);
						if (enemyPlayer.maverick1v1 == 4) weaponToDeplete = weapons.FirstOrDefault(w => w is HomingTorpedo);
						if (enemyPlayer.maverick1v1 == 5) weaponToDeplete = weapons.FirstOrDefault(w => w is BoomerangCutter);
						if (enemyPlayer.maverick1v1 == 6) weaponToDeplete = weapons.FirstOrDefault(w => w is ChameleonSting);
						if (enemyPlayer.maverick1v1 == 7) weaponToDeplete = weapons.FirstOrDefault(w => w is StormTornado);
						if (enemyPlayer.maverick1v1 == 8) weaponToDeplete = weapons.FirstOrDefault(w => w is ShotgunIce);

						if (enemyPlayer.maverick1v1 == 9) weaponToDeplete = weapons.FirstOrDefault(w => w is SonicSlicer);
						if (enemyPlayer.maverick1v1 == 10) weaponToDeplete = weapons.FirstOrDefault(w => w is StrikeChain);
						if (enemyPlayer.maverick1v1 == 11) weaponToDeplete = weapons.FirstOrDefault(w => w is SpinWheel);
						if (enemyPlayer.maverick1v1 == 12) weaponToDeplete = weapons.FirstOrDefault(w => w is BubbleSplash);
						if (enemyPlayer.maverick1v1 == 13) weaponToDeplete = weapons.FirstOrDefault(w => w is SpeedBurner);
						if (enemyPlayer.maverick1v1 == 14) weaponToDeplete = weapons.FirstOrDefault(w => w is SilkShot);
						if (enemyPlayer.maverick1v1 == 15) weaponToDeplete = weapons.FirstOrDefault(w => w is MagnetMine);
						if (enemyPlayer.maverick1v1 == 16) weaponToDeplete = weapons.FirstOrDefault(w => w is CrystalHunter);
						if (enemyPlayer.maverick1v1 == 17) weaponToDeplete = weapons.FirstOrDefault(w => w is SpeedBurner);

						if (enemyPlayer.maverick1v1 == 18) weaponToDeplete = weapons.FirstOrDefault(w => w is ParasiticBomb);
						if (enemyPlayer.maverick1v1 == 19) weaponToDeplete = weapons.FirstOrDefault(w => w is FrostShield);
						if (enemyPlayer.maverick1v1 == 20) weaponToDeplete = weapons.FirstOrDefault(w => w is AcidBurst);
						if (enemyPlayer.maverick1v1 == 21) weaponToDeplete = weapons.FirstOrDefault(w => w is TornadoFang);
						if (enemyPlayer.maverick1v1 == 22) weaponToDeplete = weapons.FirstOrDefault(w => w is TriadThunder);
						if (enemyPlayer.maverick1v1 == 23) weaponToDeplete = weapons.FirstOrDefault(w => w is SpinningBlade);
						if (enemyPlayer.maverick1v1 == 24) weaponToDeplete = weapons.FirstOrDefault(w => w is RaySplasher);
						if (enemyPlayer.maverick1v1 == 25) weaponToDeplete = weapons.FirstOrDefault(w => w is GravityWell);
						if (enemyPlayer.maverick1v1 == 26) weaponToDeplete = weapons.FirstOrDefault(w => w is AcidBurst);

						if (weaponToDeplete != null) weaponToDeplete.ammo = 4;
					}
				}
			} else {
				weapons = loadout.xLoadout.getWeaponsFromLoadout(this);
				/*
				foreach (Weapon weapon in weapons)
				{
					if (weapon is GigaCrush && oldGigaCrush != null) weapon.ammo = oldGigaCrush.ammo;
					if (weapon is HyperBuster && oldHyperbuster != null) weapon.ammo = oldHyperbuster.ammo;
				}
				*/
			}
		} else if (isAxl) {
			if (Global.level.isTraining() && !Global.level.server.useLoadout) {
				weapons = Weapon.getAllAxlWeapons(axlLoadout).Select(w => w.clone()).ToList();
				weapons[0] = getAxlBullet(axlBulletType);
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
				weapons = loadout.axlLoadout.getWeaponsFromLoadout();
				weapons.Insert(0, getAxlBullet(axlBulletType));
			}
			if (ownedByLocalPlayer) {
				foreach (var dnaCore in savedDNACoreWeapons) {
					weapons.Add(dnaCore);
				}
			}
			if (weapons[0].type > 0) {
				weapons[0].ammo = axlBulletTypeLastAmmo[weapons[0].type];
			}
		} else if (isVile) {
			weapons = loadout.vileLoadout.getWeaponsFromLoadout(true);
		} else if (isSigma) {
			if (Global.level.isTraining() && !Global.level.server.useLoadout) {
				weapons = Weapon.getAllSigmaWeapons(this).Select(w => w.clone()).ToList();
			} else if (Global.level.is1v1()) {
				if (maverick1v1 != null) {
					weapons = new List<Weapon>() { Weapon.getAllSigmaWeapons(this).Select(w => w.clone()).ToList()[maverick1v1.Value + 1] };
				} else if (!Global.level.isHyper1v1()) {
					int sigmaForm = Options.main.sigmaLoadout.sigmaForm;
					weapons = Weapon.getAllSigmaWeapons(this, sigmaForm).Select(w => w.clone()).ToList();
				}
			} else {
				weapons = loadout.sigmaLoadout.getWeaponsFromLoadout(this, Options.main.sigmaWeaponSlot);
			}
			// Preserve HP on death so can summon for free until they die
			if (oldWeapons != null && isRefundableMode() &&
				previousLoadout?.sigmaLoadout?.commandMode == loadout.sigmaLoadout.commandMode
			) {
				foreach (var weapon in weapons) {
					if (weapon is not MaverickWeapon mw) continue;
					MaverickWeapon? matchingOldWeapon = oldWeapons.FirstOrDefault(
						w => w is MaverickWeapon && w.GetType() == weapon.GetType()
					) as MaverickWeapon;
					if (matchingOldWeapon == null) {
						continue;
					}
					if (matchingOldWeapon.lastHealth > 0 && matchingOldWeapon.summonedOnce) {
						mw.summonedOnce = true;
						mw.lastHealth = matchingOldWeapon.lastHealth;
						mw.isMoth = matchingOldWeapon.isMoth;
					}

				}
			}
		}
		weaponSlot = 0;
		if (ownedByLocalPlayer && isSigma && weapons.Count == 3) {
			weaponSlot = Options.main.sigmaWeaponSlot;
		}
	}

	private Weapon getAxlBullet(int axlBulletType) {
		if (axlBulletType == (int)AxlBulletWeaponType.DoubleBullets) {
			return new DoubleBullet();
		}
		return new AxlBullet((AxlBulletWeaponType)axlBulletType);
	}

	public Weapon getAxlBulletWeapon() {
		return getAxlBulletWeapon(axlBulletType);
	}

	public Weapon getAxlBulletWeapon(int type) {
		if (type == (int)AxlBulletWeaponType.DoubleBullets) {
			return new DoubleBullet();
		} else {
			return new AxlBullet((AxlBulletWeaponType)type);
		}
	}

	public int getLastWeaponIndex() {
		int miscSlots = 0;
		if (weapons.Any(w => w is GigaCrush)) miscSlots++;
		if (weapons.Any(w => w is HyperCharge)) miscSlots++;
		if (weapons.Any(w => w is NovaStrike)) miscSlots++;
		return weapons.Count - miscSlots;
	}

	public void addGigaCrush() {
		if (!weapons.Any(w => w is GigaCrush)) {
			weapons.Add(new GigaCrush());
		}
	}

	public void addHyperCharge() {
		if (!weapons.Any(w => w is HyperCharge)) {
			weapons.Insert(getLastWeaponIndex(), new HyperCharge());
		}
	}

	public void addNovaStrike() {
		if (!weapons.Any(w => w is NovaStrike)) {
			weapons.Add(new NovaStrike(this));
		}
	}

	public void removeNovaStrike() {
		if (weapon is NovaStrike) {
			weaponSlot = 0;
		}
		weapons.RemoveAll(w => w is NovaStrike);
	}

	public void removeHyperCharge() {
		if (weapon is HyperCharge) {
			weaponSlot = 0;
		}
		weapons.RemoveAll(w => w is HyperCharge);
	}

	public void removeGigaCrush() {
		if (weapon is GigaCrush) {
			weaponSlot = 0;
		}
		weapons.RemoveAll(w => w is GigaCrush);
	}

	public void updateWeapons() {
		foreach (var weapon in weapons) {
			weapon.update();
			if (character != null && health > 0) {
				bool alwaysOn = false;
				if (weapon is GigaCrush && Options.main.gigaCrushSpecial ||
					weapon is NovaStrike && Options.main.novaStrikeSpecial
				) {
					alwaysOn = true;
				}
				weapon.charLinkedUpdate(character, alwaysOn);
			}
		}
	}
}
