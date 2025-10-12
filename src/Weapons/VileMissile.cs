using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SFML.Graphics;
namespace MMXOnline;

public enum VileMissileType {
	None = -1,
	ElectricShock,
	HumerusCrush,
	PopcornDemon
}

public class VileMissile : Weapon {
	public float vileAmmoUsage;
	public VileMissile() : base() {
		index = (int)WeaponIds.ElectricShock;
		weaponBarBaseIndex = 26;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 42;
		killFeedIndex = 17;
	}
}
public class ElectricShock : VileMissile {
	public static ElectricShock netWeapon = new();
	public ElectricShock() : base() {
		type = (int)VileMissileType.ElectricShock;
		fireRate = 45;
		displayName = "Electric Shock";
		vileAmmoUsage = 8;
		vileWeight = 2;
		ammousage = vileAmmoUsage;
		damage = "0";
		hitcooldown = "0.15";
		effect = "Stuns Enemies. CD: 2";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new MissileAttack(this), true);
		if (vile.charState is InRideArmor) {
			shoot(vile, []);
		}
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage, true);
		Point shootVel = vava.getVileShootVel(true);
		Point shootPos = vava.setCannonAim(new Point(shootVel.x, shootVel.y));
		if (vava.getShootXDir() == -1) shootVel = new Point(shootVel.x * vava.getShootXDir(), shootVel.y);
		bool isMK2 = false;
		isMK2 = vava.isVileMK2;
		if (isMK2 || vava.isVileMK5) shootPos = vava.getVileMK2StunShotPos() ?? shootPos;
			
		int xDir = character.getShootXDir();
		if (isMK2) {
			character.playSound("mk2stunshot", sendRpc: true);
			new Anim(shootPos, "dust", 1, character.player.getNextActorNetId(), true, true);
			new VileMK2StunShotProj(
				shootPos, xDir, MathF.Round(shootVel.byteAngle), character,
				character.player, character.player.getNextActorNetId(), rpc: true
			);
		} else {
			new StunShotProj(
				shootPos, xDir, MathF.Round(shootVel.byteAngle), character, 
				character.player, character.player.getNextActorNetId(), rpc: true
			);
		}
	}
}
public class VileMK2StunShot : VileMissile {
	public static VileMK2StunShot netWeapon = new();
	public VileMK2StunShot() : base() {
		type = (int)VileMissileType.ElectricShock;
		fireRate = 45;
		index = (int)WeaponIds.MK2StunShot;
		killFeedIndex = 67;
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		//vile.changeState(new MissileAttack(vile.grounded), true);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage, true);
	}
}
public class HumerusCrush : VileMissile {
	public static HumerusCrush netWeapon = new();
	public HumerusCrush() : base() {
		type = (int)VileMissileType.HumerusCrush;
		fireRate = 45;
		displayName = "Humerus Crush";
		vileAmmoUsage = 8;
		killFeedIndex = 74;
		vileWeight = 2;
		ammousage = vileAmmoUsage;
		damage = "3";
		hitcooldown = "0.15";
		effect = "None.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new MissileAttack(this), true);
		if (vile.charState is InRideArmor) {
			shoot(vile, []);
		}
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage, true);
		character.playSound("vileMissile", sendRpc: true);
		Point shootVel = vava.getVileShootVel(true);
		Point shootPos = vava.setCannonAim(new Point(shootVel.x, shootVel.y));
		if (vava.getShootXDir() == -1) shootVel = new Point(shootVel.x * vava.getShootXDir(), shootVel.y);
		int xDir = character.getShootXDir();
		new VileMissileProj(
			shootPos, xDir, 1, MathF.Round(shootVel.byteAngle), "missile_hc_proj",
			character, character.player, character.player.getNextActorNetId(), rpc: true
		);
	}
}
public class PopcornDemon : VileMissile {
	public static PopcornDemon netWeapon = new();
	public PopcornDemon() : base() {
		type = (int)VileMissileType.PopcornDemon;
		fireRate = 45;
		displayName = "Popcorn Demon";
		vileAmmoUsage = 12;
		description = new string[] { "This missile splits into 3", "and can cause great damage." };
		killFeedIndex = 76;
		vileWeight = 3;
		vileWeight = 2;
		ammousage = vileAmmoUsage;
		damage = "2";
		hitcooldown = "0.15/0";
		effect = "Can Split.";
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new MissileAttack(this), true);
		if (vile.charState is InRideArmor) {
			shoot(vile, []);
		}
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage, true);
		character.playSound("vileMissile", sendRpc: true);
		Point shootVel = vava.getVileShootVel(true);
		Point shootPos = vava.setCannonAim(new Point(shootVel.x, shootVel.y));
		if (vava.getShootXDir() == -1) shootVel = new Point(shootVel.x * vava.getShootXDir(), shootVel.y);
		int xDir = character.getShootXDir();
		new VileMissileProj(
			shootPos, xDir, 2, MathF.Round(shootVel.byteAngle), "missile_pd_proj",
			character, character.player, character.player.getNextActorNetId(), rpc: true
		);
	}
}

public class MissileAttack : VileState {
	public bool shot;
	public int shootFrame = 0;
	public VileMissile weapon;
	public MissileAttack(VileMissile weapon) : base("idle_shoot") {
		airSprite = "cannon_air";
		landSprite = "idle_shoot";
		useDashJumpSpeed = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
		this.weapon = weapon;
	}

	public override void update() {
		base.update();
		character.turnToInput(player.input, player);
		if (character.sprite.frameIndex >= shootFrame && !shot) {
			shot = true;
			weapon.shoot(vile, []);
		}
		if (character.isAnimOver()) {
			character.changeToCrouchOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.turnToInput(player.input, player);
		if (!character.grounded) {
			sprite = "cannon_air";
			character.changeSpriteFromName(sprite, true);
			character.useGravity = false;
			character.vel = new Point();
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}


public class VileMissileProj : Projectile {
	bool split;
	public int num = 0;
	public VileMissileProj(
		Point pos, int xDir, int num, float byteAngle, string sprite,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, sprite , netId, player
	) {
		xScale = xDir;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		maxTime = 0.6f;
		destroyOnHit = true;
		fadeOnAutoDestroy = true;
		reflectableFBurner = true;
		this.num = num;
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		vel.x = 200 * Helpers.cosb(byteAngle);
		vel.y = 200 * Helpers.sinb(byteAngle);
		if (num == (int)VileMissileType.HumerusCrush) {
			weapon = HumerusCrush.netWeapon;
			projId = (int)ProjIds.VileMissile;
			damager.damage = 3;
			damager.hitCooldown = 9;
			maxTime = 0.35f;
			sprite = "missile_hc_proj";
			vel.x = 350 * Helpers.cosb(byteAngle);
			vel.y = 350 * Helpers.sinb(byteAngle);
		}
		if (num == (int)VileMissileType.PopcornDemon) {
			weapon = PopcornDemon.netWeapon;
			projId = (int)ProjIds.PopcornDemon;
			damager.damage = 2;
			damager.hitCooldown = 9;
			sprite = "missile_pd_proj";
		}
		if (num == 3) {
			weapon = PopcornDemon.netWeapon;
			projId = (int)ProjIds.PopcornDemonSplit;
			damager.damage = 2;
			damager.hitCooldown = 0;
			sprite = "missile_pd_proj";		
		}		
		if (rpc) {
			List<Byte> extraBytes = new List<Byte> {
			};
			extraBytes.Add((byte)num);
			extraBytes.AddRange(Encoding.ASCII.GetBytes(sprite));
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle, extraBytes.ToArray());

		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		string sprite = Encoding.ASCII.GetString(args.extraData[1..]);
		return new VileMissileProj(
			args.pos, args.xDir, args.extraData[0], args.byteAngle, sprite, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (num == (int)VileMissileType.PopcornDemon && !split) {
			if (time > 0.3f || owner.input.isPressed(Control.Special1, owner)) {
				split = true;
				playSound("vileMissile", sendRpc: true);
				destroySelfNoEffect();
				new VileMissileProj(
					pos, xDir, 3, byteAngle-30, "missile_pd_proj", this,
					owner, owner.getNextActorNetId(),  rpc: true);
				new VileMissileProj(
					pos, xDir, 3, byteAngle, "missile_pd_proj", this, owner, 
					owner.getNextActorNetId(),  rpc: true);
				new VileMissileProj(
					pos, xDir, 3, byteAngle+30, "missile_pd_proj", this, 
					owner, owner.getNextActorNetId(),  rpc: true);
			}
		}
	}
}

public class StunShotProj : Projectile {
	public StunShotProj(
		Point pos, int xDir, float byteAngle,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "vile_stun_shot", netId, player
	) {
		weapon = ElectricShock.netWeapon;
		xScale = xDir;
		damager.hitCooldown = 9;
		fadeSprite = "vile_stun_shot_fade";
		projId = (int)ProjIds.ElectricShock;
		maxTime = 0.75f;
		destroyOnHit = true;
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		vel.x = 225 * Helpers.cosb(byteAngle);
		vel.y = 225 * Helpers.sinb(byteAngle);	
		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new StunShotProj(
			args.pos, args.xDir, args.byteAngle, args.owner, args.player, args.netId
		);
	}
}
public class StunShotProj2 : Projectile {
	public int num = 0;
	public int type = 0;
	Point[] velnum = new Point[] {
		new Point(150, 0),
		new Point(133, 75),
		new Point(75, 133),
		new Point(0, 150),
		new Point(-75, 133),
		new Point(-133, 75),
		new Point(-150, 0)
	};
	Point[] veltype = new Point[] {
		new Point(80, -70),
		new Point(90, -40),
		new Point(100, 0),
		new Point(100, 40),
		new Point(90,70),
		new Point(70,-100),
		new Point(80,100)
	};
	public StunShotProj2(
		Point pos, int xDir, int num, int type, 
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "vile_ebomb_start" , netId, player
	) {
		weapon = VileElectricBomb.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 9;
		fadeSprite = "vile_stun_shot_fade";
		projId = (int)ProjIds.SpreadShot;
		maxTime = 0.75f;
		destroyOnHit = true;
		this.num = num;
		this.type = type;
		if (num >= 1 && num <= 7) vel = new Point(velnum[num-1].x * xDir, velnum[num-1].y);
		if (type >= 1 && type <= 7) vel = new Point(veltype[type-1].x * xDir, veltype[type-1].y);
		
		
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte) num, (byte)type});
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new StunShotProj2(
			args.pos, args.xDir, args.extraData[0], args.extraData[1], args.owner, args.player, args.netId
		);
	}

}

public class VileMK2StunShotProj : Projectile {
	public VileMK2StunShotProj(
		Point pos, int xDir, float byteAngle,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "vile_stun_shot2" , netId, player
	) {
		weapon = VileMK2StunShot.netWeapon;
		fadeSprite = "vile_stun_shot_fade";
		projId = (int)ProjIds.MK2StunShot;
		maxTime = 0.75f;
		destroyOnHit = true;
		damager.damage = 1;
		damager.hitCooldown = 9;
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		vel.x = 225 * Helpers.cosb(byteAngle);
		vel.y = 225 * Helpers.sinb(byteAngle);	
		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new VileMK2StunShotProj(
			args.pos, args.xDir, args.byteAngle, args.owner, args.player, args.netId
		);
	}
}

/*
public class VileMissile : Weapon {
	public string projSprite = "";
	public float vileAmmo;
	public static VileMissile netWeaponES = new VileMissile(VileMissileType.ElectricShock);
	public static VileMissile netWeaponHC = new VileMissile(VileMissileType.HumerusCrush);
	public static VileMissile netWeaponPD = new VileMissile(VileMissileType.PopcornDemon);
	public VileMissile(VileMissileType vileMissileType) : base() {
		index = (int)WeaponIds.ElectricShock;
		weaponBarBaseIndex = 26;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 42;
		killFeedIndex = 17;
		type = (int)vileMissileType;

		if (vileMissileType == VileMissileType.None) {
			displayName = "None";
			description = new string[] { "Do not equip a Missile." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmo = 0;
			fireRate = 0;
			vileWeight = 0;			
		} else if (vileMissileType == VileMissileType.ElectricShock) {
			fireRate = 45;
			displayName = "Electric Shock";
			vileAmmo = 8;
			description = new string[] { "Stops enemies in their tracks,", "but deals no damage." };
			vileWeight = 3;
			vileWeight = 2;
			ammousage = vileAmmo;
			damage = "0";
			hitcooldown = "0.15";
			effect = "Stuns Enemies. CD: 2";
		} else if (vileMissileType == VileMissileType.HumerusCrush) {
			fireRate = 45;
			displayName = "Humerus Crush";
			projSprite = "missile_hc_proj";
			vileAmmo = 8;
			description = new string[] { "This missile shoots straight", "and deals decent damage." };
			killFeedIndex = 74;
			vileWeight = 3;
			vileWeight = 2;
			ammousage = vileAmmo;
			damage = "3";
			hitcooldown = "0.15";
			effect = "None.";
		} else if (vileMissileType == VileMissileType.PopcornDemon) {
			fireRate = 45;
			displayName = "Popcorn Demon";
			projSprite = "missile_pd_proj";
			vileAmmo = 12;
			description = new string[] { "This missile splits into 3", "and can cause great damage." };
			killFeedIndex = 76;
			vileWeight = 3;
			vileWeight = 2;
			ammousage = vileAmmo;
			damage = "2";
			hitcooldown = "0.15/0";
			effect = "Can Split.";
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (vile.missileWeapon.type == (int)VileMissileType.None ||
			shootCooldown > 0) {
			return;
		}
		vile.changeState(new MissileAttack(vile.grounded));
		if (vile.charState is InRideArmor) {
			vile.missileWeapon.shoot(vile, []);
			vile.missileWeapon.shootCooldown = vile.missileWeapon.fireRate;
		}
				
	}

	public override void shoot(Character character, int[] args) {
		Point shootVel = new Point(1, 0);
		Point shootPos = character.getFirstPOIOrDefault();
		bool isMK2 = false;
		int xDir = character.getShootXDir();

		if (character is Vile vile) {
			isMK2 = vile.isVileMK2;
			if (type == (int)VileMissileType.ElectricShock || vile.charState is InRideArmor) {
				shootVel = vile.getVileShootVel(true);
			}
			shootPos = vile.setCannonAim(new Point(shootVel.x, shootVel.y));
			if (isMK2 || vile.isVileMK5) {
				shootPos = vile.getVileMK2StunShotPos() ?? shootPos;
			}
		}
		if (xDir == -1) {
			shootVel.x *= -1;
		}
		if (type == ((int)VileMissileType.ElectricShock)) {
			if (isMK2) {
				character.playSound("mk2stunshot", sendRpc: true);
				new Anim(shootPos, "dust", 1, character.player.getNextActorNetId(), true, true);
				new VileMK2StunShotProj(
					shootPos, xDir, MathF.Round(shootVel.byteAngle), character,
					character.player, character.player.getNextActorNetId(), rpc: true
				);
			} else {
				new StunShotProj(
					shootPos, xDir, MathF.Round(shootVel.byteAngle), character, 
					character.player, character.player.getNextActorNetId(), rpc: true
				);
			}
		} else {
			character.playSound("vileMissile", sendRpc: true);
			if (type == ((int)VileMissileType.HumerusCrush)) {
				new VileMissileProj(
					shootPos, xDir, 1, MathF.Round(shootVel.byteAngle),   projSprite,
					character, character.player, character.player.getNextActorNetId(), rpc: true
				);	
			}
			if (type == ((int)VileMissileType.PopcornDemon)) {
				new VileMissileProj(
					shootPos, xDir, 2 , MathF.Round(shootVel.byteAngle), projSprite,
					character, character.player, character.player.getNextActorNetId(), rpc: true
				);	
			}
		}
	}
}
public class MissileAttack : VileState {
	public bool shoot;

	public MissileAttack(bool grounded) : base(getSprite(grounded)) {
		useDashJumpSpeed = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
		airSprite = "cannon_air";
		landSprite = "idle_shoot";
	}
	public static string getSprite(bool grounded) {
		return grounded ? "idle_shoot" : "cannon_air";
	}

	public override void update() {
		base.update();
		if (vile.getFirstPOI() != null && !shoot) {
			vile.missileWeapon.shoot(vile, []);
			vile.addAmmo(-vile.missileWeapon.vileAmmo);
			vile.missileWeapon.shootCooldown = vile.missileWeapon.fireRate;
			shoot = true;
		}
		if (vile.isAnimOver()) {
			vile.changeToIdleOrFall();
		}
	}
}
*/