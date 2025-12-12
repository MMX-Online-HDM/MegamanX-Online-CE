using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;


public class VileWeaponSystem : Weapon {
	public (VileWeaponSystemSub alt, VileWeaponSystemSub shoot, VileWeaponSystemSub special) slots;
	public (VileWeaponSystemSub alt, VileWeaponSystemSub shoot, VileWeaponSystemSub special) airSlots;
	public Weapon[] extraWeapons;
	public Weapon[] weaponList;

	public VileWeaponSystem(
		Weapon?[] altWps, Weapon?[] shootWps,
		Weapon?[] specialWps, Weapon[] extraWeapons
	) : base() {
		slots.alt = new VileWeaponSystemSub(altWps, this);
		slots.shoot = new VileWeaponSystemSub(shootWps, this);
		slots.special = new VileWeaponSystemSub(specialWps, this);
		this.extraWeapons = extraWeapons;

		HashSet<Weapon> uniqueWeapons = [];
		Weapon?[][] iterationArray = [altWps, shootWps, specialWps, extraWeapons];
		foreach (Weapon?[] weaponArray in iterationArray) {
			foreach (Weapon? weapon in weaponArray) {
				if (weapon != null) { uniqueWeapons.Add(weapon); }
			}
		}
		weaponList = uniqueWeapons.ToArray();

		index = (int)WeaponIds.VileWeaponSystem;
		weaponSlotIndex = 32;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void update() {
		foreach (Weapon weapon in weaponList) {
			weapon.update();
		}
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

	public override void vileShoot(WeaponIds id, Vile vile) {
		vile.usedAmmoLastFrame = false;

		if (vile.player.input.isHeld(Control.Shoot, vile.player)) {
			if (slots.shoot.weaponSystemShoot(vile, Control.Shoot)) { return; }
		}
		if (vile.player.input.isHeld(Control.Special1, vile.player) && !vile.isCharging()) {
			vile.stopCharge();
			if (slots.special.weaponSystemShoot(vile, Control.Special1)) { return; }
		}
		if (vile.player.input.isHeld(Control.WeaponRight, vile.player)) {
			if (slots.alt.weaponSystemShoot(vile, Control.WeaponRight)) { return; }
		}
	}

	public void extraWeaponShoot(Vile vile) {
		foreach (Weapon weapon in extraWeapons) {
			if (weapon.customShootCondition(vile)) {
				weapon.vileShoot(0, vile);
			}
		}
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
