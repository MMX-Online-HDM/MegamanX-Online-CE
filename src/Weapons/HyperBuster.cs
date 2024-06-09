using System.Collections.Generic;

namespace MMXOnline;

public class HyperBuster : Weapon {
	public const float ammoUsage = 8;
	public const float weaponAmmoUsage = 8;

	public HyperBuster() : base() {
		index = (int)WeaponIds.HyperBuster;
		killFeedIndex = 48;
		weaponBarBaseIndex = 32;
		weaponBarIndex = 31;
		weaponSlotIndex = 36;
		shootSounds = new string[] { "buster3X3", "buster3X3", "buster3X3", "buster3X3" };
		rateOfFire = 2f;
		switchCooldown = 0.25f;
		ammo = 0;
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
		return rateOfFire * getRateofFireMod(player);
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return ammo >= getChipFactoredAmmoUsage(player) && player.weapons[player.hyperChargeSlot].ammo > 0 && shootTime == 0 && player.character?.flag == null;
	}

	public bool canShootIncludeCooldown(Player player) {
		return ammo >= getChipFactoredAmmoUsage(player) && player.weapons.InRange(player.hyperChargeSlot) && player.weapons[player.hyperChargeSlot].ammo > 0;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		player.character.changeState(new X3ChargeShot(this), true);
	}
}
