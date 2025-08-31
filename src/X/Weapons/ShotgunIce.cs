using System;
using System.Collections.Generic;
using SFML.System;

namespace MMXOnline;

public class ShotgunIce : Weapon {
	public static ShotgunIce netWeapon = new ShotgunIce();

	public ShotgunIce() : base() {
		displayName = "Shotgun Ice";
		index = (int)WeaponIds.ShotgunIce;
		killFeedIndex = 8;
		weaponBarBaseIndex = 8;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 8;
		weaknessIndex = (int)WeaponIds.FireWave;
		shootSounds = new string[] { "shotgunIce", "shotgunIce", "shotgunIce", "icyWind" };
		fireRate = 30;
		damage = "2/1-2";
		effect = "U:Splits on contact on enemies or walls.\nC:Insta Freeze enemies. Ice sled up to 12 DMG.";
		hitcooldown = "0/30";
		flinch = "0";
		hasCustomChargeAnim = true;
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new ShotgunIceProj(pos, xDir, mmx, player, 0, player.getNextActorNetId(), rpc: true);
		} else {
			pos = pos.addxy(xDir * 25, 0);
			pos.y = mmx.pos.y;

			//mmx.shotgunIceChargeTime = 1.5f;
			character.changeState(new ShotgunIceChargedShot(), true);
			new ShotgunIceProjSled(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		}
	}
}


public class ShotgunIceProj : Projectile {
	public int type = 0;
	public float sparkleTime = 0;
	public Character? hitChar;
	public float maxSpeed = 400;

	public ShotgunIceProj(
		Point pos, int xDir, Actor owner, Player player, int type, ushort? netProjId,
		(int x, int y)? velOverride = null, Character? hitChar = null, bool rpc = false
	) : base(
		pos, xDir, owner, "shotgun_ice", netProjId, player	
	) {
		weapon = ShotgunIce.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 1;
		vel = new Point(400 * xDir, 0);
		projId = (int)ProjIds.ShotgunIce;
		maxTime = 0.4f;
		this.hitChar = hitChar;
		if (type == 1) {
			changeSprite("shotgun_ice_piece", true);
		}

		fadeSprite = "buster1_fade";
		this.type = type;
		if (velOverride != null) {
			vel = new Point(maxSpeed * velOverride.Value.x, maxSpeed * (velOverride.Value.y * 0.5f));
		}
		reflectable = true;
		//this.fadeSound = "explosion";
		if (rpc) {
			byte[] extraArgs;
			if (velOverride != null) {
				extraArgs = new byte[] {
					(byte)type,
					(byte)(velOverride.Value.x + 128),
					(byte)(velOverride.Value.y + 128)
				};
			} else {
				extraArgs = new byte[] { (byte)type, (byte)(128 + xDir), 128 };
			}
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
	}

	public override void update() {
		base.update();
		sparkleTime += Global.spf;
		if (sparkleTime > 0.05) {
			sparkleTime = 0;
			new Anim(pos, "shotgun_ice_sparkles", 1, null, true);
		}
	}

	public void onHit() {
		if (!ownedByLocalPlayer && type == 0) {
			destroySelf(disableRpc: true);
			return;
		}
		if (type == 0) {
			destroySelf(disableRpc: true);
			Character? chr = null;
			new ShotgunIceProj(
				pos, xDir, this, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), -2), chr, rpc: true
			);
			new ShotgunIceProj(
				pos, xDir, this, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), -1), chr, rpc: true
			);
			new ShotgunIceProj(
				pos, xDir, this, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), 0), chr, rpc: true
			);
			new ShotgunIceProj(
				pos, xDir, this, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), 1), chr, rpc: true
			);
			new ShotgunIceProj(
				pos, xDir, this, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), 2), chr, rpc: true
			);
		}
	}

	public override void onHitWall(CollideData other) {
		if (!other.gameObject.collider?.isClimbable == true) return;
		onHit();
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) onHit();
		playSound("shotgunicehitX1", forcePlay: false, sendRpc: true);
		base.onHitDamagable(damagable);
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ShotgunIceProj(
			args.pos, args.xDir, args.owner, args.player,
			args.extraData[0], args.netId, (args.extraData[1] - 128, args.extraData[2] - 128)
		);
	}
}

public class ShotgunIceProjCharged : Projectile {
	public ShotgunIceProjCharged(
		Point pos, int xDir, Actor owner, Player player, int type, 
		bool isChillP, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, type == 0 ? "shotgun_ice_charge_wind2" : "shotgun_ice_charge_wind", netId, player	
	) {
		weapon = isChillP ? ChillPenguin.netWeapon : ShotgunIce.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 30;
		vel = new Point(150 * xDir, 0);
		projId = isChillP ? (int)ProjIds.ChillPIceBlow : (int)ProjIds.ShotgunIceCharged;
		shouldShieldBlock = false;
		destroyOnHit = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir,
			new byte[] {  (byte)type, isChillP ? (byte)1 : (byte)0 }
			);
		}

		isOwnerLinked = true;
		if (ownerPlayer?.character != null) {
			owningActor = ownerPlayer.character;
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ShotgunIceProjCharged(
			args.pos, args.xDir, args.owner, args.player,
			args.extraData[0], args.extraData[1] == 1, args.netId
		);
	}

	public override void update() {
		base.update();
		if (time > 0.5f) {
			destroySelf();
		}
	}
}

public class ShotgunIceProjSled : Projectile {
	public Character? character;
	bool setVelOnce = false;
	float lastY;
	int heightIncreaseDir = 0;
	float nonRideTime = 0;
	public bool ridden;

	public ShotgunIceProjSled(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "shotgun_ice_charge", netId, player	
	) {
		weapon = ShotgunIce.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 60;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.ShotgunIceSled;
		fadeSound = "iceBreak";
		shouldShieldBlock = false;
		isPlatform = true;
		Global.level.modifyObjectGridGroups(this, isActor: true, isTerrain: true);
		//this.collider.wallOnly = true;
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public void increaseVel() {
		if (time > 3) {
			float increaseFactor = 1;
			vel.x += MathF.Sign(vel.x) * Global.spf * 100 * increaseFactor;

			float absVelX = MathF.Abs(vel.x);
			if (absVelX > 200) {
				damager.damage = 5 + MathInt.Floor((absVelX - 200f) / 25f);

				if (damager.damage > 12) {
					damager.damage = 12;
				}
			}
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ShotgunIceProjSled(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (sprite.frameIndex == sprite.totalFrameNum - 1) {
			damager.flinch = Global.defFlinch;
			useGravity = true;
		}

		if (time > 3) {
			if (!setVelOnce) {
				setVelOnce = true;
				damager.damage = 4;
				damager.flinch = Global.defFlinch;
				vel.x = xDir * 175;
			}

			animSeconds += Global.spf;
			if (animSeconds > 0.15) {
				animSeconds = 0;
				if (grounded) {
					new Anim(pos, "sled_scrape_part", Math.Sign(vel.x), null, true);
				}
			}

			float heightIncrease = lastY - pos.y;
			if (heightIncrease > 0 && heightIncreaseDir == 0) {
				heightIncreaseDir = -Math.Sign(vel.x);
			}
			if (heightIncreaseDir != 0) {
				vel.x += heightIncreaseDir * 10;
				if (MathF.Abs(vel.x) >= 175) {
					heightIncreaseDir = 0;
				}
			}

			if (grounded) {
				increaseVel();
			}
		}

		if (character == null) {
			nonRideTime += Global.spf;
			if (nonRideTime >= 10) {
				destroySelf();
			}
		}

		lastY = pos.y;
		ridden = (character != null);
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (other.isSideWallHit()) {
			destroySelf();
			return;
		}
	}

	public override void onDestroy() {
		breakFreeze(owner);
	}
}

public class ShotgunIceChargedShot : CharState {
	public float[] StateTime = { 2/60f, 10/60f, 18/60f, 26/60f, 34/60f, 42/60f, 50/60f, 58/60f};
	public bool[] fired = { false, false, false, false, false, false, false, false };
	MegamanX mmx = null!;
	public float time;
	public ShotgunIceChargedShot() : base("shoot") {
	}
	public override void update() {
		base.update();
		for (int i = 0; i < StateTime.Length; i++) {
			if (stateTime > StateTime[i] && !fired[i]) {
				fired[i] = true;
		        ShotProjectile();
			}
		}
		States();
		if (stateTime > 60f/60f) {
			character.changeToIdleOrFall();
		}
	}
	public void States() {
		if (!character.grounded) {
			character.changeSpriteFromName(character.vel.y < 0 ? "jump_shoot" : "fall_start_shoot" , false);
			if (character.isAnimOver()) {
				character.changeSpriteFromName("fall_shoot", false);
			}
		}
		if (character.sprite.name == "mmx_fall_shoot" && character.grounded) {
			character.changeSpriteFromName("land_shoot", false);
		}
		if (character.sprite.name == "mmx_land_shoot") {
			time++;
			if (time >= 8) character.changeSpriteFromName("shoot", false);
		}
	}
	public void ShotProjectile() {
		int xDir = character.xDir;
		new ShotgunIceProjCharged(
			mmx.getShootPos(), xDir, mmx, player, 1,
			false, player.getNextActorNetId(), true
		);
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
	}
}
