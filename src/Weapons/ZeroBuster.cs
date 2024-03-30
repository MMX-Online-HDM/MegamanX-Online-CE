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
		shootSounds = new List<string>() { "buster", "buster2", "buster3", "buster4" };
		rateOfFire = 0.15f;
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

public class ZSaberProjSwing : Weapon {
	public ZSaberProjSwing(Player player) : base() {
		index = (int)WeaponIds.ZSaberProjSwing;
		killFeedIndex = 9;
		damager = new Damager(player, 3, Global.defFlinch, 0.5f);
	}
}

public class Shingetsurin : Weapon {
	public Shingetsurin(Player player) : base() {
		index = (int)WeaponIds.Shingetsurin;
		killFeedIndex = 85;
		damager = new Damager(player, 4, Global.defFlinch, 0.5f);
	}
}

public class ShingetsurinProj : Projectile {
	public Actor target;
	public ShingetsurinProj(
		Weapon weapon, Point pos, int xDir, float startTime, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 150, 2, player, "shingetsurin_proj",
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 3f;
		destroyOnHit = false;
		time = startTime;
		//vel.x *= (1 - startTime);
		projId = (int)ProjIds.Shingetsurin;
		ZBuster2Proj.hyorogaCode(this, player);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (time >= 1 && time < 2) {
			vel = new Point();
		} else if (time >= 2) {
			if (target == null) {
				target = Global.level.getClosestTarget(pos, damager.owner.alliance, true);
				if (target != null) {
					vel = pos.directionToNorm(target.getCenterPos()).times(speed);
				}
			}
			forceNetUpdateNextFrame = true;
		}
	}
}

public class ZBuster2Proj : Projectile {
	public ZBuster2Proj(
		Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 350, 2, player, "zbuster2", Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		if (type == 0) {
			projId = (int)ProjIds.ZBuster2;
			changeSprite("buster2", true);
		} else {
			projId = (int)ProjIds.DZBuster2;
			damager.flinch = 0;
		}
		ZBuster2Proj.hyorogaCode(this, player);

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static void hyorogaCode(Projectile proj, Player player) {
		if (player.character?.sprite?.name?.Contains("hyoroga") == true) {
			proj.xDir = 1;
			proj.angle = 90;
			proj.incPos(new Point(0, 10));
			proj.vel.y = Math.Abs(proj.vel.x);
			proj.vel.x = 0;
		}
	}
}

public class ZBuster3Proj : Projectile {
	public ZBuster3Proj(
		Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 350, 4, player, "zbuster3", Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		if (type == 0) {
			projId = (int)ProjIds.ZBuster3;
		} else {
			projId = (int)ProjIds.DZBuster3;
			damager.flinch = Global.halfFlinch;
			damager.damage = 3;
		}
		ZBuster2Proj.hyorogaCode(this, player);
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}
}

public class ZBuster4Proj : Projectile {
	float partTime;
	public ZBuster4Proj(
		Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 350, 6, player, "zbuster4", Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		if (type == 0) {
			projId = (int)ProjIds.ZBuster4;
		} else {
			projId = (int)ProjIds.DZBuster4;
			damager.damage = 3;
			damager.flinch = Global.halfFlinch;
		}
		ZBuster2Proj.hyorogaCode(this, player);
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		partTime += Global.spf;
		if (partTime > 0.075f) {
			partTime = 0;
			new Anim(
				pos.addxy(-20 * xDir, 0).addRand(0, 12),
				"zbuster4_part", 1, null, true
			) {
				vel = vel,
				acc = new Point(-vel.x * 2, 0)
			};
		}
	}
}

public class ZeroDoubleBuster : CharState {
	bool fired1;
	bool fired2;
	bool isSecond;
	bool shootPressedAgain;
	bool isPinkCharge;
	Zero zero;

	public ZeroDoubleBuster(bool isSecond, bool isPinkCharge) : base("doublebuster", "", "", "") {
		this.isSecond = isSecond;
		superArmor = true;
		this.isPinkCharge = isPinkCharge;
		airMove = true;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) return;

		if (player.input.isPressed(Control.Shoot, player)) {
			shootPressedAgain = true;
		}

		int type = player.isZBusterZero() ? 1 : 0;
		if (!fired1 && character.frameIndex == 3) {
			fired1 = true;
			if (!isPinkCharge) {
				character.playSound("buster3X3", sendRpc: true);
				new ZBuster4Proj(
					zero.zeroBusterWeapon, character.getShootPos(),
					character.getShootXDir(), type, player, player.getNextActorNetId(), rpc: true
				);
			} else {
				character.playSound("buster2X3", sendRpc: true);
				new ZBuster2Proj(
					zero.zeroBusterWeapon, character.getShootPos(), character.getShootXDir(),
					type, player, player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (!fired2 && character.frameIndex == 7) {
			fired2 = true;
			if (!isPinkCharge) {
				zero.doubleBusterDone = true;
			} else {
				character.stockCharge(false);
			}
			character.playSound("buster3X3", sendRpc: true);
			new ZBuster4Proj(
				zero.zeroBusterWeapon, character.getShootPos(), character.getShootXDir(),
				type, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else if (!isSecond && character.frameIndex >= 4 && !shootPressedAgain) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded || character.canAirJump()) &&
				player.input.isPressed(Control.Jump, player)
			) {
				if (!character.grounded) {
					character.dashedInAir++;
				}
				character.vel.y = -character.getJumpPower();
				sprite = "doublebuster_air";
				defaultSprite = sprite;
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero = character as Zero;

		if (!isPinkCharge) {
			character.stockSaber(true);
		} else {
			character.stockCharge(!isSecond);
		}
		sprite = "doublebuster";
		defaultSprite = sprite;
		landSprite = "doublebuster";
		if (!character.grounded || character.vel.y < 0) {
			defaultSprite = sprite;
			sprite = "doublebuster_air";
		}
		character.changeSpriteFromName(sprite, true);
		if (isSecond) {
			character.frameIndex = 4;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (isSecond && character is Zero zero) {
			zero.doubleBusterDone = true;
		}
	}
}

public class ZSaberProjSwingState : CharState {
	bool fired;
	bool grounded;
	bool shootProj;
	public ZSaberProjSwingState(
		bool grounded, bool shootProj
	) : base(
		grounded ? "projswing" : "projswing_air", "", "", ""
	) {
		this.grounded = grounded;
		landSprite = "projswing";
		this.shootProj = shootProj;
		if (shootProj) {
			superArmor = true;
		}
		airMove = true;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();

		if (character.frameIndex >= 4 && !fired) {
			fired = true;
			character.playSound("ZeroSaberX3", sendRpc: true);
			if (shootProj) {
				new ZSaberProj(
					new ZSaber(player), character.pos.addxy(30 * character.xDir, -20),
					character.xDir, player, player.getNextActorNetId(), rpc: true
				);
			}
		}

		if (character.isAnimOver()) {
			if (character.grounded) character.changeState(new Idle(), true);
			else character.changeState(new Fall(), true);
		} else {
			if ((character.grounded || character.canAirJump()) &&
				player.input.isPressed(Control.Jump, player)
			) {
				if (!character.grounded) {
					character.dashedInAir++;
				}
				character.vel.y = -character.getJumpPower();
				sprite = "projswing_air";
				defaultSprite = sprite;
				character.changeSpriteFromName(sprite, false);
			}
		}
	}
}
