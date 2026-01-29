using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

// Vile weapon systen.
// This controls all sub weapons is a free form way.
// Originally created for HDM for Vile and nicknamed "Lego Vile",
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
	public Weapon rideGrenade;
	// We have an input buffer here.
	// So it's easier to combo in non stream weapons.
	public Dictionary<string, float> bufferedInputs = new() {
		{Control.Shoot, 0},
		{Control.Special1, 0},
		{Control.WeaponRight, 0},
	};
	public float bufferTime = 12;

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
		Weapon?[] groundArray = [..altWps, ..shootWps, ..specialWps];
		Weapon?[] airArray = [..airAltWps,..airShootWps,..airSpecialWps];


		// Populate unique weapon list.
		int i = 0;
		foreach (Weapon?[] weaponArray in iterationArray) {
			foreach (Weapon? weapon in weaponArray) {
				if (weapon != null) {
					uniqueWeapons.Add(weapon);
					if (groundArray.Contains(weapon)) { uniqueGroundWeapons.Add(weapon); }
					if (airArray.Contains(weapon)) { uniqueAirWeapons.Add(weapon); }
				}
			}
		}
		weaponList = uniqueWeapons.ToArray();
		groundWeaponList = uniqueGroundWeapons.ToArray();
		airWeaponList = uniqueAirWeapons.ToArray();

		// Fixed slot weapons.
		chargeWeapon = extraWeapons[0];
		rideWeapon = extraWeapons[1];
		rideGrenade = extraWeapons[1];

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
	public override void charLinkedUpdate(Character chara, bool isAlwaysOn) {
		foreach (Weapon weapon in weaponList) {
			weapon.update();
		}
		foreach (var kvp in bufferedInputs) {
			if (chara.player.input.isPressed(kvp.Key, chara.player)) {
				bufferedInputs[kvp.Key] = bufferTime;
			} else {
				bufferedInputs[kvp.Key] -= Global.gameSpeed;
				if (bufferedInputs[kvp.Key] < 0) { bufferedInputs[kvp.Key] = 0; }
			}
		}
		base.charLinkedUpdate(chara, isAlwaysOn);
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
		// We set a preliminary key order.
		string[] keys = [Control.Shoot, Control.Special1, Control.WeaponRight];
		bool[] helds = new bool[3];
		// We check if is pressed, if so we set it to max buffer time.
		for (int i = 0; i < keys.Length; i++) {
			helds[i] = vile.player.input.isHeld(keys[i], vile.player) || bufferedInputs[keys[i]] > 0;
			if (helds[i]) {
				bufferedInputs[keys[i]] = bufferTime;
			}
		}

		if (helds[0]) {
			if (tartgetSlot.shoot.weaponSystemShoot(vile, keys[0])) {
				bufferedInputs[keys[0]] = 0;
				return true;
			}
		}
		if (helds[1] && !vile.isCharging()) {
			if (tartgetSlot.special.weaponSystemShoot(vile, keys[1])) {
				bufferedInputs[keys[1]] = 0;
				vile.stopCharge();
				return true;
			}
		}
		if (helds[2]) {
			if (tartgetSlot.alt.weaponSystemShoot(vile, keys[2])) {
				bufferedInputs[keys[2]] = 0;
				return true;
			}
		}
		return false;
	}

	public void extraWeaponShoot(Vile vile) {
		foreach (Weapon weapon in extraWeapons) {
			if (weapon.customShootCondition(vile)) {
				weapon.vileShoot(vile);
			}
		}
	}

	public bool shootRandomWeapon(Vile vile) {
		// Get target list.
		Weapon[] targetWeapons = vile.grounded ? groundWeaponList : airWeaponList;
		float ammoLeft = vile.energy.ammo;

		// Get all off-cooldown ones.
		Weapon[] offCooldownWeapons = targetWeapons.Where(
			w => w.shootCooldown <= 0 && w.getAmmoUsage(0) <= ammoLeft
		).ToArray();

		// If list is emtpty. Return.
		if (offCooldownWeapons.Length == 0) {
			return false;
		}
		int targetWeapon = Helpers.randomRange(0, offCooldownWeapons.Length - 1);
		Weapon target = offCooldownWeapons[targetWeapon];
		target.vileShoot(vile);

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
			targetWeapon.vileShoot(vile);
			return true;
		}
		return false;
	}

	// Esentially. A IsStream check.
	public bool checkShootAble(Character character, Weapon weapon, string button) {
		if (weapon.isStream && character.player.input.isHeld(button, character.player)) {
			return true;
		}
		return (
			character.player.input.isPressed(button, character.player) ||
			parentSystem.bufferedInputs[button] > 0
		);
	}
}
