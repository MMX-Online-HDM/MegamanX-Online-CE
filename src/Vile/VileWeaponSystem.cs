using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

// Vile weapon systen.
// This controls all sub weapons is a free from way.
// Originally created for HDM form Vile nicknamed "Lego Vile",
// but helps lot for regular XOD Vile too.
public class VileWeaponSystem : Weapon {
	// Unlike HDM, XOD Vile splits ground and air so we use 2 systems.
	public (VileWeaponSystemSub alt, VileWeaponSystemSub shoot, VileWeaponSystemSub special) slots;
	public (VileWeaponSystemSub alt, VileWeaponSystemSub shoot, VileWeaponSystemSub special) airSlots;
	// Weapons that have unique activation conditions.
	public Weapon[] extraWeapons;
	// This list contain 1 copy of each weapon.
	public Weapon[] weaponList;
	public Weapon[] airWeaponList;
	public Weapon[] groundWeaponList;
	// Some things we need to quick-acces.
	public Weapon chargeWeapon;
	public Weapon rideWeapon;

	// Creation function.
	public VileWeaponSystem(
		Weapon?[] altWps, Weapon?[] shootWps,
		Weapon?[] specialWps, Weapon?[] airAltWps, Weapon?[] airShootWps,
		Weapon?[] airSpecialWps, Weapon[] extraWeapons
	) : base() {
		// Set up sub systems.
		slots.alt = new VileWeaponSystemSub(altWps, this);
		slots.shoot = new VileWeaponSystemSub(shootWps, this);
		slots.special = new VileWeaponSystemSub(specialWps, this);
		airSlots.alt = new VileWeaponSystemSub(airAltWps, this);
		airSlots.shoot = new VileWeaponSystemSub(airShootWps, this);
		airSlots.special = new VileWeaponSystemSub(airSpecialWps, this);
		this.extraWeapons = extraWeapons;

		// Create an HashSet of each weapon.
		// HashSet cannot contain duplicates so helps to create
		// and list of non-repeated elements.
		HashSet<Weapon> uniqueWeapons = [];
		HashSet<Weapon> uniqueGroundWeapons = [];
		HashSet<Weapon> uniqueAirWeapons = [];
		Weapon?[][] iterationArray = [
			altWps, shootWps, specialWps,
			airAltWps, airShootWps, airSpecialWps,
			extraWeapons
		];

		// Populate unique weapon list.
		int i = 0;
		foreach (Weapon?[] weaponArray in iterationArray) {
			foreach (Weapon? weapon in weaponArray) {
				if (weapon != null) {
					uniqueWeapons.Add(weapon);
					if (i <= 2) { uniqueGroundWeapons.Add(weapon); }
					if (i >= 3 && i <= 5) { uniqueAirWeapons.Add(weapon); }
				}
			}
		}
		weaponList = uniqueWeapons.ToArray();
		groundWeaponList = uniqueGroundWeapons.ToArray();
		airWeaponList = uniqueAirWeapons.ToArray();

		// Fixed slot weapons.
		chargeWeapon = extraWeapons[0];
		rideWeapon = extraWeapons[1];

		// Generic weapon stuff.
		index = (int)WeaponIds.VileWeaponSystem;
		weaponSlotIndex = 32;
		weaponSlotIndex = 32;
		weaponBarBaseIndex = 39;
		weaponBarIndex = 32;
		drawCooldown = false;
		drawAmmo = false;
	}

	// Unused, but better be safe than sorry.
	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	// Call update function of each one.
	public override void update() {
		foreach (Weapon weapon in weaponList) {
			weapon.update();
		}
		base.update();
	}

	public void setAllWeaponsToCooldown(float cooldown) {
		foreach (Weapon weapon in weaponList) {
			weapon.shootCooldown = Math.Min(cooldown, weapon.fireRate);
		}
	}

	public void reduceAllWeaponsCooldown(float ammount) {
		foreach (Weapon weapon in weaponList) {
			weapon.shootCooldown = Math.Max(weapon.shootCooldown - ammount, 0);
		}
	}

	public bool shootLogic(Vile vile) {
		vile.usedAmmoLastFrame = false;
		var tartgetSlot = vile.grounded ? slots : airSlots;

		if (vile.player.input.isHeld(Control.Shoot, vile.player)) {
			if (tartgetSlot.shoot.weaponSystemShoot(vile, Control.Shoot)) { return true; }
		}
		if (vile.player.input.isHeld(Control.Special1, vile.player) && !vile.isCharging()) {
			vile.stopCharge();
			if (tartgetSlot.special.weaponSystemShoot(vile, Control.Special1)) { return true; }
		}
		if (vile.player.input.isHeld(Control.WeaponRight, vile.player)) {
			if (tartgetSlot.alt.weaponSystemShoot(vile, Control.WeaponRight)) { return true; }
		}
		return false;
	}

	public void extraWeaponShoot(Vile vile) {
		foreach (Weapon weapon in extraWeapons) {
			if (weapon.customShootCondition(vile)) {
				weapon.vileShoot(0, vile);
			}
		}
	}

	public bool shootRandomWeapon(Vile vile) {
		// Get target list.
		Weapon[] targetWeapons = vile.grounded ? groundWeaponList : airWeaponList;
		float ammoLeft = vile.energy.ammo;

		// Get all off-cooldown ones.
		Weapon[] offCooldownWeapons = targetWeapons.Where(
			w => w.shootCooldown == 0 && w.getAmmoUsage(0) >= ammoLeft
		).ToArray();

		// If list is emtpty. Return.
		if (offCooldownWeapons.Length > 0) {
			return false;
		}
		Weapon target = offCooldownWeapons[Helpers.randomRange(0, offCooldownWeapons.Length - 1)];
		target.vileShoot(0, vile);

		return true;
	}
}

public class VileWeaponSystemSub {
	public (Weapon? normal, Weapon? foward, Weapon? up, Weapon? down) weapons;
	public VileWeaponSystem parentSystem;

	public VileWeaponSystemSub(Weapon?[] wps, VileWeaponSystem parentSystem) : base() {
		// Populate weapons.
		weapons.normal = wps[0];
		weapons.foward = wps[1];
		weapons.up = wps[3];
		weapons.down = wps[2];
		this.parentSystem = parentSystem;

		// If netural is null then we use any avaliable option.
		if (weapons.normal == null) {
			if (weapons.foward != null) {
				(weapons.normal, weapons.foward) = (weapons.foward, weapons.normal);
			}
			if (weapons.up == null) {
				(weapons.normal, weapons.up) = (weapons.up, weapons.normal);
			}
			if (weapons.down == null) {
				(weapons.normal, weapons.down) = (weapons.down, weapons.normal);
			}
		}
	}

	// Used when pressed a button.
	public bool weaponSystemShoot(Vile vile, string button) {
		int yDirPressed = vile.player.input.getYDir(vile.player);
		int xDirPressed = vile.player.input.getXDir(vile.player);
		Weapon? targetWeapon = null;

		if (yDirPressed == -1 && weapons.down != null) {
			targetWeapon = weapons.down;
		} else if (yDirPressed == 1 && weapons.up != null) {
			targetWeapon = weapons.up;
		} else if (xDirPressed != 0 && weapons.foward != null) {
			targetWeapon = weapons.foward;
		} else if (weapons.normal != null) {
			targetWeapon = weapons.normal;
		}

		if (targetWeapon != null &&
			targetWeapon.canShoot(0, vile.player) &&
			checkShootAble(vile, targetWeapon, button)
		) {
			targetWeapon.vileShoot(0, vile);
			return true;
		}
		return false;
	}

	// Esentially. A IsStream check.
	public bool checkShootAble(Character character, Weapon weapon, string button) {
		if (weapon.isStream) {
			return character.player.input.isHeld(button, character.player);
		} else {
			return character.player.input.isPressed(button, character.player);
		}
	}
}
