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
		shootSounds = new string[] { "stockBuster", "stockBuster", "stockBuster", "stockBuster", "stockBuster" };
		fireRate = 20;
		canHealAmmo = true;
		drawAmmo = true;
		drawCooldown = true;
		allowSmallBar = false;
		ammoGainMultiplier = 2;
		maxAmmo = 12;
		ammo = maxAmmo;
		drawRoundedDown = true;
		drawGrayOnLowAmmo = true;
	}

	public override float getAmmoUsage(int chargeLevel) { return 3; }
	public override bool canShoot(int chargeLevel, Character character) {
		if (ammo < 3) return false;
		return base.canShoot(chargeLevel, character);
	}

	public override void shoot(Character character, int[] args) {
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		new RagingBusterProj(this, pos, xDir, player, player.getNextActorNetId(), true);
		new Anim(pos, "buster_unpo_muzzle", xDir, null, true);
	}
}


public class RagingBusterProj : Projectile {
	public RagingBusterProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId,
		bool rpc = false	
	) : base(
		weapon, pos, xDir, 350, 3,
		 player, "buster_unpo", Global.defFlinch, 0.01f, 
		 netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.BusterUnpo;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RagingBusterProj(
			RagingChargeBuster.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.netId
		);
	}
}

public class AbsorbWeapon : Weapon {
	public Projectile absorbedProj;
	public AbsorbWeapon(Projectile otherProj) {
		index = (int)WeaponIds.UPParry;
		shootSounds = new string[] { "", "", "", "", "" };
		weaponSlotIndex = 118;
		killFeedIndex = 168;
		this.absorbedProj = otherProj;
		fireRate = 0;
		drawAmmo = false;
	}
	public override void shoot(Character character, int[] args) {
		if (character is not RagingChargeX rcx) return;	
		Player player = character.player;
		character.changeState(new XUPParryProjState(absorbedProj, true, true), true);
		rcx.absorbedProj = null;
		player.weapons.RemoveAll(w => w is AbsorbWeapon);
		int busterIndex = player.weapons.FindIndex(w => w is RagingChargeBuster);
		player.changeWeaponSlot(busterIndex);
	}
}

public class RCXZSaber : Weapon {
	public RCXZSaber() : base() {
		shootSounds = ["", "", "", "", ""];
		index = (int)WeaponIds.ZSaber;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 48;
		killFeedIndex = 9;
		drawAmmo = false;
		drawCooldown = false;
	}

	public override void shoot(Character character, int[] args) {
		if (character is not RagingChargeX rcx) {
			return;
		}
		Player player = character.player;
		rcx.changeState(new RCXMaxWaveSaberState(), true);
		player.weapons.RemoveAll(w => w is RCXZSaber);
		int busterIndex = player.weapons.FindIndex(w => w is RagingChargeBuster);
		player.changeWeaponSlot(busterIndex);
	}
}

public class RCXParry : Weapon {
	public static RCXParry netWeapon = new RCXParry();

	public RCXParry() : base() {
		fireRate = 45;
		index = (int)WeaponIds.UPParry;
		killFeedIndex = 168;
	}
}

public class RCXPunch : Weapon {
	public static RCXPunch netWeapon = new();

	public RCXPunch() : base() {
		fireRate = 45;
		index = (int)WeaponIds.UPPunch;
		killFeedIndex = 167;
		//damager = new Damager(player, 3, Global.defFlinch, 0.5f);
	}
}

public class RCXGrab : Weapon {
	public static RCXGrab netWeapon = new();

	public RCXGrab() : base() {
		fireRate = 45;
		//index = (int)WeaponIds.UPGrab;
		killFeedIndex = 92;
	}
}
