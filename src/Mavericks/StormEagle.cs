using System;
using System.Collections.Generic;

namespace MMXOnline;

public class StormEagle : Maverick {
	public static Weapon netWeapon = new Weapon(WeaponIds.StormEGeneric, 99);
	public StormEDiveWeapon diveWeapon;

	public StormEagle(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(StormEAirShootState), new(2f, true, true) },
			{ typeof(MShoot), new(2f, true, true) },
			{ typeof(StormEEggState), new(90, true) },
			{ typeof(StormEGustState), new(45, true) },
			{ typeof(StormEDiveState), new(60) }
		};

		diveWeapon = new StormEDiveWeapon();
		weapon = new Weapon(WeaponIds.StormEGeneric, 99);

		awardWeaponId = WeaponIds.StormTornado;
		weakWeaponId = WeaponIds.ChameleonSting;
		weakMaverickWeaponId = WeaponIds.StingChameleon;

		netActorCreateId = NetActorCreateId.StormEagle;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		canFly = true;
		flyBarIndexes = (44, 38);
		maxFlyBar = 960;
		flyBar = 960;
	}

	public override void update() {
		base.update();

		if (!isUnderwater()) {
			spriteFrameToSounds["storme_fly/1"] = "stormeFlap";
			spriteFrameToSounds["storme_fly_fall/1"] = "stormeFlap";
		} else {
			spriteFrameToSounds.Clear();
		}

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (shootPressed()) {
					changeState(getShootState());
				}
				if (specialPressed()) {
					changeState(new StormEEggState(true));
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new StormEGustState());
				}
			} else if (state is MJump || state is MFall) {
				if (input.isPressed(Control.Dash, player)) {
					changeState(new StormEDiveState());
				}
			} else if (state is MFly) {
				if (shootPressed()) {
					changeState(new StormEAirShootState());
				}
				if (specialPressed()) {
					changeState(new StormEEggState(false));
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new StormEDiveState());
				}
			}
		}
	}

	public override string getMaverickPrefix() {
		return "storme";
	}

	public MaverickState getShootState() {
		return new MShoot((Point pos, int xDir) => {
			new TornadoProj(pos, xDir, true, this, player, player.getNextActorNetId(), rpc: true);
		}, "tornado");
	}

	public override MaverickState[] strikerStates() {
		return [
			getShootState(),
			new StormEEggState(true),
			new StormEGustState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = target.pos.distanceTo(pos);
		}
		List<MaverickState> aiStates = [
			getShootState(),
			new StormEEggState(grounded)
		];
		if (grounded && enemyDist <= 90) {
			aiStates.Add(new StormEGustState());
		}
		return aiStates.ToArray();
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Dive,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"storme_dive" or "storme_dive2" => MeleeIds.Dive,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Dive => new GenericMeleeProj(
				diveWeapon, pos, ProjIds.StormEDive, player,
				4, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}
}

#region weapons
public class StormETornadoWeapon : Weapon {
	public static StormETornadoWeapon netWeapon = new();
	public StormETornadoWeapon() {
		index = (int)WeaponIds.StormETornado;
		killFeedIndex = 99;
	}
}

public class StormEDiveWeapon : Weapon {
	public static StormEDiveWeapon netWeapon = new();
	public StormEDiveWeapon() {
		index = (int)WeaponIds.StormEDive;
		killFeedIndex = 99;
	}
}

public class StormEEggWeapon : Weapon {
	public static StormEEggWeapon netWeapon = new();
	public StormEEggWeapon() {
		index = (int)WeaponIds.StormEEgg;
		killFeedIndex = 99;
	}
}

public class StormEBirdWeapon : Weapon {
	public static StormEBirdWeapon netWeapon = new();
	public StormEBirdWeapon() {
		index = (int)WeaponIds.StormEBird;
		killFeedIndex = 99;
	}
}

public class StormEGustWeapon : Weapon {
	public static StormEGustWeapon netWeapon = new();
	public StormEGustWeapon() {
		index = (int)WeaponIds.StormEGust;
		killFeedIndex = 99;
	}
}
#endregion

#region projectiles
public class StormEEggProj : Projectile {
	public StormEEggProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "storme_proj_egg", netId, player	
	) {
		weapon = StormEEggWeapon.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		vel = new Point(100 * xDir, 0);
		projId = (int)ProjIds.StormEEgg;
		maxTime = 0.675f;
		useGravity = true;
		vel.y = -100;
		collider.wallOnly = true;
		fadeSound = "explosion";
		fadeSprite = "explosion";

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new StormEEggProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
		if (time > maxTime) return;
		new StormEBirdProj(pos, xDir, 1,
		this, owner, owner.getNextActorNetId(), rpc: true);
		new StormEBirdProj(pos, xDir, 2,
		this, owner, owner.getNextActorNetId(), rpc: true);
		new StormEBirdProj(pos, xDir, 3,
		this, owner, owner.getNextActorNetId(), rpc: true);
		new StormEBirdProj(pos, xDir, 4,
		this, owner, owner.getNextActorNetId(), rpc: true);
	}
}

public class StormEBirdProj : Projectile, IDamagable {
	public int health = 1;
	public int type = 1;
	public StormEBirdProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "storme_proj_baby", netId, player	
	) {
		weapon = StormEBirdWeapon.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.StormEBird;
		maxTime = 1.5f;
		fadeSound = "explosion";
		fadeSprite = "explosion";
		this.type = type;
		
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new StormEBirdProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (time < 20f/60f) {
			if (type == 3) vel = new Point(-100 * xDir, -32);		
			if (type == 4) vel = new Point(10 * xDir, -32);
			if (type == 1) vel = new Point(10 * xDir, 32);
			if (type == 2) vel = new Point(-100 * xDir, 32);
			
		} else if (type == 4) vel = new Point(150 * xDir, 10);
		  else if (type == 1) vel = new Point(170 * xDir, -17);
		  else if (type == 2) vel = new Point(150 * xDir, -5);
		  else if (type == 3) vel = new Point(165 * xDir, 10);
		
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		destroySelf();
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damagerAlliance != owner.alliance;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool isPlayableDamagable() {
		return false;
	}

}

public class StormEGustProj : Projectile {
	float maxSpeed = 250;
	public StormEGustProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "storme_proj_gust", netId, player	
	) {
		weapon = StormEGustWeapon.netWeapon;
		damager.hitCooldown = 30;
		vel = new Point(250 * xDir, 0);
		projId = (int)ProjIds.StormEGust;
		maxTime = 0.75f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new StormEGustProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		vel.y -= Global.spf * 100;
	}


	public override void onHitDamagable(IDamagable damagable) {
		if (!damagable.isPlayableDamagable()) { return; }
		if (damagable is not Actor actor || !actor.ownedByLocalPlayer) {
			return;
		}
		float modifier = 1;
		if (actor.grounded) { modifier = 0.5f; };
		if (damagable is Character character) {
			if (character.isPushImmune()) { return; }
			if (character.charState is Crouch) { modifier = 0.25f; }
			character.pushedByTornadoInFrame = true;
		}
		//character.damageHistory.Add(new DamageEvent(damager.owner, weapon.killFeedIndex, true, Global.frameCount));
		actor.move(new Point(maxSpeed * 0.9f * xDir * modifier, 0));
	}
}
#endregion

#region states
public class StormEDiveState : MaverickState {
	Point diveVel;
	bool reverse;
	float incAmount;
	bool wasPrevStateFly;
	public StormEDiveState() : base("dive") {
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (reverse) {
			diveVel.y -= incAmount * Global.spf;
			incAmount += Global.spf * 1500;
			if (incAmount > 1500) incAmount = 1500;
			if (diveVel.y < 0 && sprite != "dive2") {
				sprite = "dive2";
				maverick.changeSpriteFromName(sprite, true);
			}
			if (diveVel.y < -250) diveVel.y = -250;
		}

		Point finalDiveVel = diveVel;
		if (maverick.isUnderwater()) {
			finalDiveVel = finalDiveVel.times(0.75f);
		}
		if (maverick.pos.y <= -5) {
			finalDiveVel.y = Math.Max(finalDiveVel.y, 0);
		}

		maverick.move(finalDiveVel);

		if (maverick.grounded) {
			maverick.changeState(new MIdle());
			return;
		}

		var hit = checkCollisionNormal(finalDiveVel.x * Global.spf, finalDiveVel.y * Global.spf);
		if (hit != null && !hit.getNormalSafe().isGroundNormal()) {
			maverick.changeState(wasPrevStateFly ? new MFly() : new MFall());
			return;
		}

		if (stateTime > 1) {
			maverick.changeState(wasPrevStateFly ? new MFly() : new MFall());
			return;
		}

		if (input.isHeld(Control.Up, player) && stateTime > 0.1f) {
			reverse = true;
		}
	}

	public override bool canEnter(Maverick maverick) {
		return base.canEnter(maverick);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		maverick.useGravity = false;
		diveVel = new Point(maverick.xDir * 250, maverick.yDir * 250);
		maverick.playSound("stormeDive");
		incAmount = 750;
		wasPrevStateFly = oldState is MFly;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}
}

public class StormEGustState : MaverickState {
	float soundTime = 0.5f;
	float gustTime;
	public StormEagle StormEagleed = null!;
	public StormEGustState() : base("flap") {
		aiAttackCtrl = true;
	}

	public override bool canEnter(Maverick maverick) {
		return base.canEnter(maverick);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		StormEagleed = maverick as StormEagle ?? throw new NullReferenceException();		
		maverick.stopMoving();
	}

	public override void update() {
		base.update();
		if (StormEagleed == null) return;

		soundTime += Global.spf;
		if (soundTime > 0.4f) {
			soundTime = 0;
			if (!maverick.isUnderwater()) maverick.playSound("stormeFlap", sendRpc: true);
		}

		gustTime += Global.spf;
		if (gustTime > 0.1f) {
			gustTime = 0;
			float randX = maverick.pos.x + maverick.xDir * Helpers.randomRange(0, 65);
			Point pos = new Point(randX, maverick.pos.y);
			new StormEGustProj(
				pos, maverick.xDir, StormEagleed, player,
				player.getNextActorNetId(), rpc: true
			);
		}

		if (isAI) {
			if (stateTime > 4) {
				maverick.changeState(new MIdle());
			}
		} else {
			if (!input.isHeld(Control.Dash, player)) {
				maverick.changeState(new MIdle());
			}
		}
	}
}

public class StormEEggState : MaverickState {
	bool isGrounded;
	public StormEagle StormEagleed = null!;
	public StormEEggState(bool isGrounded) : base(isGrounded ? "air_eggshoot" : "air_eggshoot") {
		this.isGrounded = isGrounded;
	}

	public override bool canEnter(Maverick maverick) {
		return base.canEnter(maverick);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		StormEagleed = maverick as StormEagle ?? throw new NullReferenceException();		
		if (!isGrounded) {
			maverick.stopMoving();
			maverick.useGravity = false;
		}
	}

	public override void update() {
		base.update();
		if (StormEagleed == null) return;

		if (maverick.frameIndex == 3 && !once) {
			once = true;
			new StormEEggProj(
				StormEagleed.getFirstPOI() ?? StormEagleed.getCenterPos(), maverick.xDir,
				StormEagleed, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(isGrounded ? new MIdle() : new MFly());
		}
	}
}

public class StormEAirShootState : MaverickState {
	bool shotOnce;
	public StormEagle StormEagleed = null!;

	public StormEAirShootState() : base("air_shoot") {
	}

	public override void update() {
		base.update();
		if (StormEagleed == null) return;

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			maverick.playSound("tornado", sendRpc: true);
			new TornadoProj(
				shootPos.Value, maverick.xDir, true, StormEagleed,
				player, player.getNextActorNetId(), rpc: true
			);
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MFly());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;
		StormEagleed = maverick as StormEagle ?? throw new NullReferenceException();

	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
	}
}
#endregion
