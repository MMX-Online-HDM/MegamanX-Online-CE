using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RagingChargeBuster : Weapon {

	public static RagingChargeBuster netWeapon = new(); 

	public RagingChargeBuster() : base() {
		index = (int)WeaponIds.RagingChargeBuster;
		killFeedIndex = 180;
		weaponBarBaseIndex = 70;
		weaponBarIndex = 59;
		weaponSlotIndex = 121;
		shootSounds = new string[] { "buster2", "buster2", "buster2", "buster2" };
		fireRate = 45;
		canHealAmmo = true;
		drawAmmo = true;
		drawCooldown = true;
		allowSmallBar = false;
		ammoGainMultiplier = 2;
		maxAmmo = 12;
		ammo = maxAmmo;
	}

	public override float getAmmoUsage(int chargeLevel) { return 3; }

	public override void shoot(Character character, int[] args) {
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		new BusterUnpoProj(this, pos, xDir, player, player.getNextActorNetId(), true);
		new Anim(pos, "buster_unpo_muzzle", xDir, null, true);
	}
}
