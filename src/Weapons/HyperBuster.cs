using System;
using System.Collections.Generic;

namespace MMXOnline;

public class HyperBuster : Weapon {
	public const float ammoUsage = 7;
	public const float weaponAmmoUsage = 8;

	public HyperBuster() : base() {
		index = (int)WeaponIds.HyperBuster;
		killFeedIndex = 48;
		weaponBarBaseIndex = 32;
		weaponBarIndex = 31;
		weaponSlotIndex = 36;
		//shootSounds = new string[] { "buster3X3", "buster3X3", "buster3X3", "buster3X3" };
		fireRate = 120;
		//switchCooldown = 0.25f;
		switchCooldownFrames = 15;
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
		return ammoUsage;
	}

	public float getChipFactoredAmmoUsage(Player player) {
		return player.hasChip(3) ? ammoUsage / 2 : ammoUsage;
	}

	public static float getRateofFireMod(Player player) {
		if (player != null && player.hyperChargeSlot < player.weapons.Count &&
			player.weapons[player.hyperChargeSlot] is Buster && !player.hasUltimateArmor()
		) {
			return 0.75f;
		}
		return 1;
	}

	public float getRateOfFire(Player player) {
		return fireRate * getRateofFireMod(player);
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return 
			ammo >= getChipFactoredAmmoUsage(player) && 
			player.weapons[player.hyperChargeSlot].ammo > 0 && 
			base.canShoot(chargeLevel, player) && player.character?.flag == null;
	}

	public bool canShootIncludeCooldown(Player player) {
		return 
			ammo >= getChipFactoredAmmoUsage(player) &&
			player.weapons.InRange(player.hyperChargeSlot) && 
			player.weapons[player.hyperChargeSlot].ammo > 0;
	}

	bool changeToWeaponSlot(Weapon wep) {
		return wep is
			Sting or
			RollingShield or
			BubbleSplash or
			ParasiticBomb or
			TunnelFang;
	} 

	public override void shoot(Character character, int[] args) {
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		Weapon wep = player.weapons[player.hyperChargeSlot];

		if (wep is Buster) {
			character.changeState(new X3ChargeShot(this), true);
			character.playSound("buster3X3");
		} else {
			if (changeToWeaponSlot(wep)) player.changeWeaponSlot(player.hyperChargeSlot);
			wep.shoot(character, new int[] {3});
			wep.addAmmo(-wep.getAmmoUsage(3), player);
			mmx.shootCooldown = MathF.Max(wep.fireRate, switchCooldownFrames.GetValueOrDefault());
			if (!string.IsNullOrEmpty(wep.shootSounds[3])) {
				character.playSound(wep.shootSounds[3]);
			}
			
			if (wep is BubbleSplash bs) bs.hyperChargeDelay = 15;
		}
	}
}
