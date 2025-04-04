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
		if (shootCooldown > 0) return;
		if (vile.missileWeapon.type == (int)VileMissileType.None) return;
		Point shootVel = vile.getVileShootVel(true);
		bool isMK2 = vile.isVileMK2;
		int xDir = vile.xDir;
		
		if (vile.tryUseVileAmmo(vileAmmo)) {
			vile.setVileShootTime(this);
			if (!vile.grounded) {
				vile.changeState(new MissileAttack(grounded: false), true);
			} else {
				vile.changeState(new MissileAttack(grounded: true), true);
			}
		}
		
		if (vile.charState is InRideArmor) {
			if (vile.getShootXDir() == -1) {
				shootVel = new Point(shootVel.x * vile.getShootXDir(), shootVel.y);
			}
			vile.setVileShootTime(this);
			if (vile.missileWeapon.type == ((int)VileMissileType.ElectricShock)) {
				if (isMK2) {
					vile.playSound("mk2stunshot", sendRpc: true);
					new Anim(vile.getFirstPOIOrDefault(), "dust", 1, vile.player.getNextActorNetId(), true, true);
					new VileMK2StunShotProj(
						vile.getFirstPOIOrDefault(), xDir, MathF.Round(shootVel.byteAngle), vile,
						vile.player, vile.player.getNextActorNetId(), rpc: true
					);
				} else {
					new StunShotProj(
						vile.getFirstPOIOrDefault(), xDir, MathF.Round(shootVel.byteAngle), vile, 
						vile.player, vile.player.getNextActorNetId(), rpc: true
					);
				}
			} else {
				vile.playSound("vileMissile", sendRpc: true);
				if (vile.missileWeapon.type == ((int)VileMissileType.HumerusCrush)) {
					new VileMissileProj(
						vile.getFirstPOIOrDefault(), xDir, 1, MathF.Round(shootVel.byteAngle),  vile.missileWeapon.projSprite,
						vile, vile.player, vile.player.getNextActorNetId(), rpc: true
					);
				}
				if (vile.missileWeapon.type == ((int)VileMissileType.PopcornDemon)) {
					new VileMissileProj(
						vile.getFirstPOIOrDefault(), xDir, 2 , MathF.Round(shootVel.byteAngle), vile.missileWeapon.projSprite,
						vile, vile.player, vile.player.getNextActorNetId(), rpc: true
					);
				}
			}
		}
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
			weapon = VileMissile.netWeaponHC;
			projId = (int)ProjIds.VileMissile;
			damager.damage = 3;
			damager.hitCooldown = 9;
			maxTime = 0.35f;
			sprite = "missile_hc_proj";
			vel.x = 350 * Helpers.cosb(byteAngle);
			vel.y = 350 * Helpers.sinb(byteAngle);
		}
		if (num == (int)VileMissileType.PopcornDemon) {
			weapon = VileMissile.netWeaponPD;
			projId = (int)ProjIds.PopcornDemon;
			damager.damage = 2;
			damager.hitCooldown = 9;
			sprite = "missile_pd_proj";
		}
		if (num == 3) {
			weapon = VileMissile.netWeaponPD;
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

public class VileMK2StunShot : Weapon {
	public static VileMK2StunShot netWeapon = new();
	public VileMK2StunShot() : base() {
		fireRate = 45;
		index = (int)WeaponIds.MK2StunShot;
		killFeedIndex = 67;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		//new StunShotProj(pos, xDir, 0, 0, 0, owner, player, netProjId);
	}
}

public class StunShotProj : Projectile {
	public StunShotProj(
		Point pos, int xDir, float byteAngle,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "vile_stun_shot", netId, player
	) {
		weapon = VileMissile.netWeaponES;
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

public class MissileAttack : CharState {
	public Vile vile = null!;
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
		if (character.sprite.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public static void shootLogic(Vile vile) {
		if (vile.sprite.getCurrentFrame().POIs.IsNullOrEmpty()) return;
		bool isMK2 = vile.isVileMK2;
		Point? headPosNullable = vile.getVileMK2StunShotPos();
		if (headPosNullable == null) return;
		Point shootVel = vile.getVileShootVel(true);
		Point shootPos = vile.setCannonAim(new Point(shootVel.x, shootVel.y));
		int xDir = vile.xDir;
		if (vile.getShootXDir() == -1) {
			shootVel = new Point(shootVel.x * vile.getShootXDir(), shootVel.y);
		}

		if (vile.missileWeapon.type == ((int)VileMissileType.ElectricShock)) {
			if (isMK2) {
				vile.playSound("mk2stunshot", sendRpc: true);
				new Anim(headPosNullable.Value, "dust", 1, vile.player.getNextActorNetId(), true, true);
				new VileMK2StunShotProj(
					headPosNullable.Value, xDir, MathF.Round(shootVel.byteAngle), vile,
					vile.player, vile.player.getNextActorNetId(), rpc: true
				);
			} else {
				new StunShotProj(
					shootPos, xDir, MathF.Round(shootVel.byteAngle), vile, 
					vile.player, vile.player.getNextActorNetId(), rpc: true
				);
			}
		} else {
			vile.playSound("vileMissile", sendRpc: true);
			if (vile.missileWeapon.type == ((int)VileMissileType.HumerusCrush)) {
				new VileMissileProj(
					shootPos, xDir, 1, MathF.Round(shootVel.byteAngle),   vile.missileWeapon.projSprite,
					vile, vile.player, vile.player.getNextActorNetId(), rpc: true
				);	
			}
			if (vile.missileWeapon.type == ((int)VileMissileType.PopcornDemon)) {
				new VileMissileProj(
					shootPos, xDir, 2 , MathF.Round(shootVel.byteAngle), vile.missileWeapon.projSprite,
					vile, vile.player, vile.player.getNextActorNetId(), rpc: true
				);	
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		vile = character as Vile ?? throw new NullReferenceException();
		shootLogic(vile);
		if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
			exitOnAirborne = true;
		}
	}
}
