using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ZeroBuster : Weapon {
	public static ZeroBuster netWeapon = new();

	public ZeroBuster() : base() {
		index = (int)WeaponIds.Buster;
		killFeedIndex = 160;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 0;
		shootSounds = new string[] { "buster", "buster2", "buster3", "buster4" };
		fireRate = 9;
		displayName = "Z-Buster";
		description = new string[] { "Shoot uncharged Z-Buster with ATTACK." };
		type = (int)ZeroAttackLoadoutType.ZBuster;
	}
}

public class ZBusterSaber : Weapon {
	public static ZBusterSaber netWeapon = new();

	public ZBusterSaber() : base() {
		index = (int)WeaponIds.ZSaberProjSwing;
		killFeedIndex = 9;
	}
}
