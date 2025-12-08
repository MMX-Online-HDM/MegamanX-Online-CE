using System.Collections.Generic;
namespace MMXOnline;

public enum AxlBulletWeaponType {
	AxlBullets,
	MetteurCrash,
	BeastKiller,
	MachineBullets,
	DoubleBullets,
	RevolverBarrel,
	AncientGun
}
public class AxlBullet : AxlWeapon {
	public AxlBullet(AxlBulletWeaponType type = AxlBulletWeaponType.AxlBullets) : base(0) {
		shootSounds = new string[] { "axlBullet", "axlBullet", "axlBullet", "axlBulletCharged" };
		this.type = (int)type;
		index = (int)WeaponIds.AxlBullet;
		weaponBarBaseIndex = 28;
		weaponBarIndex = 28;
		weaponSlotIndex = 28;
		killFeedIndex = 28;
		sprite = "axl_arm_pistol";
		flashSprite = "axl_pistol_flash";
		chargedFlashSprite = "axl_pistol_flash_charged";
		fireRate = 9;
		altFireCooldown = 18;
		displayName = "Axl Bullets";
		canHealAmmo = true;
		isAxlBullets = true;
	}
	public override float getAmmoUsage(int chargeLevel) {
		switch (chargeLevel) {
			case 1:
				return 4;
			case 2:
				return 6;
			case >= 3:
				return 8;
			default:
				return 1;
		}
	}
	public override void axlShoot(Character character, int[] args) {
		if (altShotCooldown > 0) return;
		base.axlShoot(character, args);
	}
	public override void axlAltShoot(Character character, int[] args) {
		if (shootCooldown > 0) return;
		if (character.currentWeapon?.ammo < 4) return;
		base.axlAltShoot(character, args);
	}
	public override void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new CopyShotProj(weapon, bulletPos, chargeLevel, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}

	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new AxlBulletProj(weapon, bulletPos, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}

}
public class DoubleBullet : AxlWeapon {
	public DoubleBullet() : base(0) {
		sprite = "axl_arm_pistol";
		flashSprite = "axl_pistol_flash";
		chargedFlashSprite = "axl_pistol_flash_charged";
		shootSounds = new string[] { "axlBullet", "axlBullet", "axlBullet", "axlBulletCharged" };
		index = (int)WeaponIds.DoubleBullet;
		weaponBarBaseIndex = 31;
		weaponBarIndex = 28;
		weaponSlotIndex = 35;
		killFeedIndex = 34;
		altFireCooldown = 14;
		fireRate = 7;
		displayName = "Double Bullets";
		type = (int)AxlBulletWeaponType.DoubleBullets;
		isAxlBullets = true;
	}
	public override float getAmmoUsage(int chargeLevel) {
		switch (chargeLevel) {
			case 1:
				return 4;
			case 2:
				return 6;
			case >= 3:
				return 8;
			default:
				return 1;
		}
	}
	public override void axlAltShoot(Character character, int[] args) {
		if (shootCooldown > 0) return;
		if (character.currentWeapon?.ammo < 4) return;
		base.axlAltShoot(character, args);
	}

	public override void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new CopyShotProj(weapon, bulletPos, chargeLevel, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}

	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new AxlBulletProj(weapon, bulletPos, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}
}
public class MettaurCrash : AxlWeapon {
	public MettaurCrash() : base(0) {
		index = (int)WeaponIds.MetteurCrash;
		weaponBarBaseIndex = 48;
		weaponBarIndex = 28;
		weaponSlotIndex = 99;
		killFeedIndex = 127;
		sprite = "axl_arm_metteurcrash";
		flashSprite = "axl_pistol_flash";
		chargedFlashSprite = "axl_pistol_flash_charged";
		altFireCooldown = 18;
		displayName = "Mettaur Crash";
		shootSounds = new string[] { "mettaurCrash", "axlBullet", "axlBullet", "axlBulletCharged" };
		type = (int)AxlBulletWeaponType.MetteurCrash;
		isAxlBullets = true;
	}
	public override float getAmmoUsage(int chargeLevel) {
		switch (chargeLevel) {
			case 1:
				return 4;
			case 2:
				return 6;
			case >= 3:
				return 8;
			default:
				return 1;
		}
	}
	public override void axlShoot(Character character, int[] args) {
		if (altShotCooldown > 0) return;
		base.axlShoot(character, args);
	}
	public override void axlAltShoot(Character character, int[] args) {
		if (shootCooldown > 0) return;
		if (character.currentWeapon?.ammo < 4) return;
		base.axlAltShoot(character, args);
	}
	public override void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new CopyShotProj(weapon, bulletPos, chargeLevel, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}

	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new MettaurCrashProj(weapon, bulletPos, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}
}
public class BeastKiller : AxlWeapon {
	public BeastKiller() : base(0) {
		index = (int)WeaponIds.BeastHunter;
		weaponBarBaseIndex = 46;
		weaponBarIndex = 28;
		weaponSlotIndex = 97;
		killFeedIndex = 128;
		sprite = "axl_arm_beastkiller";
		flashSprite = "axl_pistol_flash";
		chargedFlashSprite = "axl_pistol_flash_charged";
		altFireCooldown = 18;
		fireRate = 45;
		displayName = "Beast Killer";
		shootSounds = new string[] { "beastKiller", "axlBullet", "axlBullet", "axlBulletCharged" };
		type = (int)AxlBulletWeaponType.BeastKiller;
		isAxlBullets = true;
	}
	public override float getAmmoUsage(int chargeLevel) {
		return 3;
	}
	public override void axlShoot(Character character, int[] args) {
		if (altShotCooldown > 0) return;
		base.axlShoot(character, args);
	}
	public override void axlAltShoot(Character character, int[] args) {
		if (shootCooldown > 0) return;
		if (character.currentWeapon?.ammo < 4) return;
		base.axlAltShoot(character, args);
	}

	public override void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new CopyShotProj(weapon, bulletPos, chargeLevel, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}

	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new BeastKillerProj(weapon, bulletPos, player, Point.createFromAngle(angle - 45), player.getNextActorNetId(), sendRpc: true);
		bullet = new BeastKillerProj(weapon, bulletPos, player, Point.createFromAngle(angle - 22.5f), player.getNextActorNetId(), sendRpc: true);
		bullet = new BeastKillerProj(weapon, bulletPos, player, bulletDir, player.getNextActorNetId(), sendRpc: true);
		bullet = new BeastKillerProj(weapon, bulletPos, player, Point.createFromAngle(angle + 22.5f), player.getNextActorNetId(), sendRpc: true);
		bullet = new BeastKillerProj(weapon, bulletPos, player, Point.createFromAngle(angle + 45), player.getNextActorNetId(), sendRpc: true);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}
}
public class MachineBullets : AxlWeapon {
	public MachineBullets() : base(0) {
		index = (int)WeaponIds.MachineBullets;
		weaponBarBaseIndex = 45;
		weaponBarIndex = 28;
		weaponSlotIndex = 96;
		killFeedIndex = 129;
		sprite = "axl_arm_machinebullets";
		flashSprite = "axl_pistol_flash";
		chargedFlashSprite = "axl_pistol_flash_charged";
		altFireCooldown = 18;
		fireRate = 9;
		displayName = "Machine Bullets";
		shootSounds = new string[] { "machineBullets", "axlBullet", "axlBullet", "axlBulletCharged" };
		type = (int)AxlBulletWeaponType.MachineBullets;
		isAxlBullets = true;
	}
	public override float getAmmoUsage(int chargeLevel) {
		return 2;
	}
	public override void axlShoot(Character character, int[] args) {
		if (altShotCooldown > 0) return;
		base.axlShoot(character, args);
	}
	public override void axlAltShoot(Character character, int[] args) {
		if (shootCooldown > 0) return;
		if (character.currentWeapon?.ammo < 4) return;
		base.axlAltShoot(character, args);
	}
	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile? bullet = null;
		if (chargeLevel == 0) {
			bullet = new MachineBulletProj(weapon, bulletPos, player, Point.createFromAngle(angle + Helpers.randomRange(-25, 25)), player.getNextActorNetId(), sendRpc: true);
			bullet = new MachineBulletProj(weapon, bulletPos, player, Point.createFromAngle(angle + Helpers.randomRange(-25, 25)), player.getNextActorNetId(), sendRpc: true);
		} else {
			bullet = new CopyShotProj(weapon, bulletPos, chargeLevel, player, bulletDir, netId);
		}
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}
}
public class RevolverBarrel : AxlWeapon {
	public RevolverBarrel() : base(0) {
		index = (int)WeaponIds.RevolverBarrel;
		weaponBarBaseIndex = 47;
		weaponBarIndex = 28;
		weaponSlotIndex = 98;
		killFeedIndex = 130;
		sprite = "axl_arm_revolverbarrel";
		flashSprite = "axl_pistol_flash";
		chargedFlashSprite = "axl_pistol_flash_charged";
		altFireCooldown = 18;
		displayName = "Revolver Barrel";
		shootSounds = new string[] { "revolverBarrel", "axlBullet", "axlBullet", "axlBulletCharged" };
		type = (int)AxlBulletWeaponType.RevolverBarrel;
		isAxlBullets = true;
	}
	public override float getAmmoUsage(int chargeLevel) {
		switch (chargeLevel) {
			case 1:
				return 4;
			case 2:
				return 6;
			case >= 3:
				return 8;
			default:
				return 1;
		}
	}
	public override void axlShoot(Character character, int[] args) {
		if (altShotCooldown > 0) return;
		base.axlShoot(character, args);
	}
	public override void axlAltShoot(Character character, int[] args) {
		if (shootCooldown > 0) return;
		if (character.currentWeapon?.ammo < 4) return;
		base.axlAltShoot(character, args);
	}

	public override void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new CopyShotProj(weapon, bulletPos, chargeLevel, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}

	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new RevolverBarrelProj(weapon, bulletPos, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}

}
public class AncientGun : AxlWeapon {
	public AncientGun(AxlBulletWeaponType type = AxlBulletWeaponType.AncientGun) : base(0) {
		index = (int)WeaponIds.AncientGun;
		weaponBarBaseIndex = 49;
		this.type = (int)type;
		weaponBarIndex = 28;
		weaponSlotIndex = 100;
		killFeedIndex = 131;
		sprite = "axl_arm_ancientgun";
		flashSprite = "axl_pistol_flash";
		chargedFlashSprite = "axl_pistol_flash_charged";
		altFireCooldown = 14;
		fireRate = 6;
		displayName = "Ancient Gun";
		shootSounds = new string[] { "ancientGun3", "axlBullet", "axlBullet", "axlBulletCharged" };
		isAxlBullets = true;
	}
	public override float getAmmoUsage(int chargeLevel) {
		switch (chargeLevel) {
			case 1:
				return 4;
			case 2:
				return 6;
			case >= 3:
				return 8;
			default:
				return 1;
		}
	}
	public override void axlShoot(Character character, int[] args) {
		if (altShotCooldown > 0) return;
		base.axlShoot(character, args);
	}
	public override void axlAltShoot(Character character, int[] args) {
		if (shootCooldown > 0) return;
		if (character.currentWeapon?.ammo < 4) return;
		base.axlAltShoot(character, args);
	}
	public override void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new CopyShotProj(weapon, bulletPos, chargeLevel, player, bulletDir, netId);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}
	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		Point bulletDir = Point.createFromAngle(angle);
		Projectile bullet;
		bullet = new AncientGunProj(weapon, bulletPos, player, bulletDir, player.getNextActorNetId(), sendRpc: true);
		bullet = new AncientGunProj(weapon, bulletPos, player, Point.createFromAngle(angle + Helpers.randomRange(-25, 25)), player.getNextActorNetId(), sendRpc: true);
		if (player.ownedByLocalPlayer) {
			RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
		}
	}
}
