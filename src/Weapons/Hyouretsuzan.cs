using System;
using System.Collections.Generic;

namespace MMXOnline;

public enum ZeroFallStabType {
	Hyouretsuzan,
	Rakukojin,
	QuakeBlazer,
	DropKick,
}

public class HyouretsuzanWeapon : Weapon {
	public HyouretsuzanWeapon(Player player) : base() {
		damager = new Damager(player, 4, 12, 0.5f);
		index = (int)WeaponIds.Hyouretsuzan;
		rateOfFire = 0;
		weaponBarBaseIndex = 24;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 12;
		type = (int)ZeroFallStabType.Hyouretsuzan;
		displayName = "Hyouretsuzan";
		description = new string[] { "A dive attack that can freeze enemies." };
	}

	public static Weapon getWeaponFromIndex(Player player, int index) {
		if (index == (int)ZeroFallStabType.Hyouretsuzan) return new HyouretsuzanWeapon(player);
		else if (index == (int)ZeroFallStabType.Rakukojin) return new RakukojinWeapon(player);
		else if (index == (int)ZeroFallStabType.QuakeBlazer) return new QuakeBlazerWeapon(player);
		else if (index == (int)ZeroFallStabType.DropKick) return new DropKickWeapon(player);
		else throw new Exception("Invalid Zero hyouretsuzan weapon index!");
	}
}

public class RakukojinWeapon : Weapon {
	public RakukojinWeapon(Player player) : base() {
		damager = new Damager(player, 4, 12, 0.5f);
		index = (int)WeaponIds.Rakukojin;
		rateOfFire = 0;
		weaponBarBaseIndex = 38;
		killFeedIndex = 37;
		type = (int)ZeroFallStabType.Rakukojin;
		displayName = "Rakukojin";
		description = new string[] { "Drop with a metal blade that deals more damage", "the faster Zero is falling." };
	}
}

public class QuakeBlazerWeapon : Weapon {
	public QuakeBlazerWeapon(Player player) : base() {
		damager = new Damager(player, 2, 0, 0.5f);
		index = (int)WeaponIds.QuakeBlazer;
		rateOfFire = 0.3f;
		weaponBarBaseIndex = 38;
		killFeedIndex = 82;
		type = (int)ZeroFallStabType.QuakeBlazer;
		displayName = "Danchien";
		description = new string[] { "A dive attack that can burn enemies", "and knock them downwards." };
	}
}

public class ZeroFallStab : CharState {
	public Weapon weapon;
	public ZeroFallStabType type { get { return (ZeroFallStabType)weapon.type; } }
	public bool canFreeze;
	public Zero zero = null!;

	public ZeroFallStab(Weapon weapon) : base(
		getSpriteName(weapon.type) + "_fall", "", "", getSpriteName(weapon.type) + "_start"
	) {
		this.weapon = weapon;
	}

	public static string getSpriteName(int type) {
		if (type == (int)ZeroFallStabType.Hyouretsuzan) return "hyouretsuzan";
		else if (type == (int)ZeroFallStabType.Rakukojin) return "rakukojin";
		else return "quakeblazer";
	}

	public override void update() {
		base.update();
		if (isUnderwaterQuakeBlazer()) {
			if (!sprite.EndsWith("_water")) {
				transitionSprite += "";
				sprite += "_water";
				defaultSprite += "_water";
				character.changeSpriteFromName(sprite, false);
			}
		}
		if (type == ZeroFallStabType.QuakeBlazer) {
			if (player.input.isHeld(Control.Left, player)) {
				character.xDir = -1;
				character.move(new Point(-100, 0));
			} else if (player.input.isHeld(Control.Right, player)) {
				character.xDir = 1;
				character.move(new Point(100, 0));
			}
		}
		if (character.grounded) {
			character.changeState(new ZeroFallStabLand(weapon.type, getSpriteName(weapon.type) + "_land"), true);
			if (weapon.type == (int)ZeroFallStabType.QuakeBlazer) {
				quakeBlazerExplode(true);
			}
		}
	}

	public bool isUnderwaterQuakeBlazer() {
		return character.isUnderwater() && type == ZeroFallStabType.QuakeBlazer;
	}

	public void quakeBlazerExplode(bool hitGround) {
		if (!character.ownedByLocalPlayer) return;

		if (isUnderwaterQuakeBlazer()) return;

		if (!character.sprite.name.Contains("_start") || character.frameIndex > 0) {
			character.playSound("circleBlazeExplosion", sendRpc: true);
			new QuakeBlazerExplosionProj(weapon, character.pos.addxy(10 * character.xDir, -10), character.xDir, player, player.getNextActorNetId(), sendRpc: true);
		}

		if (!hitGround) {
			if (player.input.isHeld(Control.Jump, player) && zero.quakeBlazerBounces < 1) {
				character.vel.y = -300;
				zero.quakeBlazerBounces++;
				// character.airAttackCooldown = character.maxAirAttackCooldown;
			} else {
				// weapon.shootTime = weapon.rateOfFire * 2;
			}
			character.changeState(new Fall(), true);
		} else {
			// weapon.shootTime = weapon.rateOfFire * 2;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (character.vel.y < 0) character.vel.y = 0;
		var ground = Global.level.raycast(character.pos, character.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
		if (ground == null) {
			canFreeze = true;
		}
		zero = character as Zero ?? throw new NullReferenceException();
	}

	public override void onExit(CharState oldState) {
		zero.quakeBlazerBounces = 0;
	}
}

public class ZeroFallStabLand : CharState {
	int type;
	public ZeroFallStabLand(int type, string sprite) : base(sprite) {
		exitOnAirborne = true;
		this.type = type;
	}

	public override void update() {
		base.update();
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.playSound("land", sendRpc: true);
		switch (type) {
			case (int)ZeroFallStabType.Hyouretsuzan:
				character.breakFreeze(player, character.pos.addxy(character.xDir * 5, 0), sendRpc: true);
				break;
			case (int)ZeroFallStabType.Rakukojin:
				character.playSound("swordthud", sendRpc: true);
				break;
		}
	}
}

public class QuakeBlazerExplosionProj : Projectile {
	public QuakeBlazerExplosionProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, xDir, 0, 2, player, "quakeblazer_explosion", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		destroyOnHit = false;
		projId = (int)ProjIds.QuakeBlazer;
		shouldShieldBlock = false;
		xScale = 1f;
		yScale = 1f;
		if (sendRpc) {
			rpcCreate(pos, owner, netProjId, xDir);
		}
	}

	public override void onStart() {
		base.onStart();
		if (!ownedByLocalPlayer) return;

		float velMag = 75;
		new QuakeBlazerFlamePart(weapon, pos.addxy(0, -10).addRand(5, 5), xDir, new Point(-velMag, 0), owner, owner.getNextActorNetId(), rpc: true);
		new QuakeBlazerFlamePart(weapon, pos.addxy(0, -10).addRand(5, 5), xDir, new Point(velMag, 0), owner, owner.getNextActorNetId(), rpc: true);
		new QuakeBlazerFlamePart(weapon, pos.addxy(0, 0).addRand(5, 5), xDir, new Point(0, 0), owner, owner.getNextActorNetId(), rpc: true);
		new QuakeBlazerFlamePart(weapon, pos.addxy(0, 10).addRand(5, 5), xDir, new Point(-velMag, 0), owner, owner.getNextActorNetId(), rpc: true);
		new QuakeBlazerFlamePart(weapon, pos.addxy(0, 10).addRand(5, 5), xDir, new Point(velMag, 0), owner, owner.getNextActorNetId(), rpc: true);

	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			destroySelf(disableRpc: true);
		}
	}
}

public class QuakeBlazerFlamePart : Projectile {
	public QuakeBlazerFlamePart(Weapon weapon, Point pos, int xDir, Point vel, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 0, player, "quakeblazer_part", 0, 1f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.QuakeBlazerFlame;
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = false;
		shouldShieldBlock = false;
		this.vel = vel;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (grounded) vel = new Point();

		if (isUnderwater()) {
			destroySelf(disableRpc: true);
			return;
		}

		if (isAnimOver()) {
			destroySelf(disableRpc: true);
			return;
		}
	}
}

public class DropKickWeapon : Weapon {
	public DropKickWeapon(Player player) : base() {
		damager = new Damager(player, 4, 12, 0.5f);
		index = (int)WeaponIds.DropKick;
		rateOfFire = 0;
		killFeedIndex = 112;
		type = (int)ZeroFallStabType.DropKick;
	}
}

public class ZeroShoryukenWeapon : Weapon {
	public ZeroShoryukenWeapon(Player player) : base() {
		damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		index = (int)WeaponIds.ZeroShoryuken;
		type = (int)RyuenjinType.Shoryuken;
		killFeedIndex = 113;
	}
}


public class MegaPunchWeapon : Weapon {
	public MegaPunchWeapon(Player player) : base() {
		damager = new Damager(player, 3, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.MegaPunchWeapon;
		killFeedIndex = 106;
		type = (int)GroundSpecialType.MegaPunch;
	}
}
