using System;
using System.Collections.Generic;

namespace MMXOnline;

public class HyperCharge : Weapon {
	public bool active;
	public const float ammoUsage = 7;

	public HyperCharge() : base() {
		index = (int)WeaponIds.HyperCharge;
		killFeedIndex = 48;
		weaponBarBaseIndex = 32;
		weaponBarIndex = 31;
		weaponSlotIndex = 36;
		//shootSounds = new string[] { "buster3X3", "buster3X3", "buster3X3", "buster3X3" };
		fireRate = 120;
		switchCooldown = 15;
		ammo = 0;
		maxAmmo = 28;
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		allowSmallBar = false;
	}

	public override void update() {
		base.update();
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) {
			return 0;
		}
		return 7;
	}

	public float getChipFactoredAmmoUsage(Player player) {
		return player.character is MegamanX mmx && mmx.hyperArmArmor == ArmorId.Max ? ammoUsage / 2 : ammoUsage;
	}

	public static float getRateofFireMod(Player player) {
		if (player != null && player.hyperChargeSlot < player.weapons.Count &&
			player.weapons[player.hyperChargeSlot] is XBuster &&
			(player.character as MegamanX)?.hasUltimateArmor != true
		) {
			return 0.75f;
		}
		return 1;
	}

	public float getRateOfFire(Player player) {
		return fireRate * getRateofFireMod(player);
	}

	public override bool canShoot(int chargeLevel, MegamanX mmx) {
		if (mmx.stockedMaxBuster) {
			return false;
		}
		return (
			(ammo >= getChipFactoredAmmoUsage(mmx.player) || chargeLevel >= 3) && 
			mmx.weapons[mmx.player.hyperChargeSlot].ammo > 0 && 
			base.canShoot(chargeLevel, mmx) && mmx.flag == null
		);
	}

	public bool canShootIncludeCooldown(Player player) {
		return 
			ammo >= getChipFactoredAmmoUsage(player) &&
			player.weapons.InRange(player.hyperChargeSlot) && 
			player.weapons[player.hyperChargeSlot].ammo > 0;
	}

	bool changeToWeaponSlot(Weapon wep) {
		return (wep is
			ChameleonSting or
			RollingShield or
			BubbleSplash or
			ParasiticBomb or
			TornadoFang
		);
	} 

	public override void shoot(Character character, int[] args) {
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		Weapon wep = player.weapons[player.hyperChargeSlot];

		if (wep is XBuster) {
			character.changeState(new X3ChargeShot(this), true);
			if (!mmx.hasUltimateArmor)
			character.playSound("buster3X3");
		} else {
			if (changeToWeaponSlot(wep)) player.changeWeaponSlot(player.hyperChargeSlot);
			wep.shoot(character, [3]);
			wep.addAmmo(-wep.getAmmoUsage(3), player);
			if (!string.IsNullOrEmpty(wep.shootSounds[3])) {
				character.playSound(wep.shootSounds[3]);
			}
			
			if (wep is BubbleSplash bs) {
				bs.hyperChargeDelay = 15;
			}
		}
	}
}
