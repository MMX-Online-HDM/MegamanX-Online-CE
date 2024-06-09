using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ShotgunIce : Weapon {
	public static ShotgunIce netWeapon = new ShotgunIce();

	public ShotgunIce() : base() {
		index = (int)WeaponIds.ShotgunIce;
		killFeedIndex = 8;
		weaponBarBaseIndex = 8;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 8;
		weaknessIndex = 4;
		shootSounds = new string[] { "shotgunIce", "shotgunIce", "shotgunIce", "icyWind" };
		rateOfFire = 0.5f;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		if (chargeLevel < 3) {
			new ShotgunIceProj(this, pos, xDir, player, 0, netProjId);
		} else if (player?.character is MegamanX mmx) {
			pos = pos.addxy(xDir * 25, 0);
			pos.y = mmx.pos.y;

			mmx.shotgunIceChargeTime = 1.5f;

			/*
			var rect = new Rect(pos.x - 20, pos.y - 16, pos.x + 20, pos.y);
			var collisions = Global.level.checkCollisionsShape(rect.getShape(), new List<GameObject>());
			foreach (var collision in collisions)
			{
				var damagable = collision.gameObject as IDamagable;
				if (damagable != null && damagable.canBeDamaged(player.alliance))
				{
					return;
				}
			}
			*/

			new ShotgunIceProjSled(this, pos, xDir, player, netProjId);
		}
	}
}


public class ShotgunIceProj : Projectile {
	public int type = 0;
	public float sparkleTime = 0;
	public Character? hitChar;
	public float maxSpeed = 400;

	public ShotgunIceProj(
		Weapon weapon, Point pos, int xDir, Player player, int type, ushort netProjId,
		(int x, int y)? velOverride = null, Character? hitChar = null, bool rpc = false
	) : base(
		weapon, pos, xDir, 400, 2, player, "shotgun_ice", 0, 0.01f, netProjId, player.ownedByLocalPlayer
	) {
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
		useGravity = true;
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
			rpcCreate(pos, player, netProjId, xDir, extraArgs);
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
		if (!ownedByLocalPlayer) {
			destroySelf(disableRpc: true);
			return;
		}
		if (type == 0) {
			destroySelf(disableRpc: true);
			Character? chr = null;
			new ShotgunIceProj(
				weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), -2), chr, rpc: true
			);
			new ShotgunIceProj(
				weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), -1), chr, rpc: true
			);
			new ShotgunIceProj(
				weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), 0), chr, rpc: true
			);
			new ShotgunIceProj(
				weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), 1), chr, rpc: true
			);
			new ShotgunIceProj(
				weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(),
				((-1 * xDir), 2), chr, rpc: true
			);
		}
	}

	public override void onHitWall(CollideData other) {
		if (!other.gameObject.collider.isClimbable) return;
		onHit();
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) onHit();
		playSound("shotgunicehitX1", forcePlay: false, sendRpc: true);
		base.onHitDamagable(damagable);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ShotgunIceProj(
			ShotgunIce.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.extraData[0], arg.netId, (arg.extraData[1] - 128, arg.extraData[2] - 128)
		);
	}
}

public class ShotgunIceProjCharged : Projectile {
	public ShotgunIceProjCharged(Weapon weapon, Point pos, int xDir, Player player, int type, bool isChillP, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 150, 1, player, type == 0 ? "shotgun_ice_charge_wind2" : "shotgun_ice_charge_wind", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		projId = isChillP ? (int)ProjIds.ChillPIceBlow : (int)ProjIds.ShotgunIceCharged;

		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
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
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId
	) : base(
		weapon, pos, xDir, 0, 2, player, "shotgun_ice_charge", 0, 1, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.ShotgunIceSled;
		fadeSound = "iceBreak";
		shouldShieldBlock = false;
		isPlatform = true;
		//this.collider.wallOnly = true;
		canBeLocal = true;
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

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (sprite.frameIndex == sprite.frames.Count - 1) {
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

			animTime += Global.spf;
			if (animTime > 0.15) {
				animTime = 0;
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
