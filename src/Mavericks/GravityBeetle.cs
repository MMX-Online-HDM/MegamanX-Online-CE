using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class GravityBeetle : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.GravityBeetle, 157); }
	public static Weapon getMeleeWeapon(Player player) { return new Weapon(WeaponIds.GravityBeetle, 157, new Damager(player, 6, Global.defFlinch, 0.5f)); }

	public Weapon meleeWeapon;
	public GBeetleGravityWellProj? well;

	public GravityBeetle(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(GBeetleShoot), new(60) },
			{ typeof(GBeetleGravityWellState), new(60) },
			{ typeof(GBeetleDashState), new(90) }
		};

		weapon = getWeapon();
		meleeWeapon = getMeleeWeapon(player);

		awardWeaponId = WeaponIds.GravityWell;
		weakWeaponId = WeaponIds.RaySplasher;
		weakMaverickWeaponId = WeaponIds.NeonTGeneric;

		netActorCreateId = NetActorCreateId.GravityBeetle;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		armorClass = ArmorClass.Heavy;
		canStomp = true;
		gameMavs = GameMavs.X3;
	}

	public override void update() {
		base.update();
		if (well?.destroyed == true) {
			well = null;
		}

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new GBeetleShoot(false));
				} else if (input.isPressed(Control.Special1, player) && well == null) {
					changeState(new GBeetleGravityWellState());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new GBeetleDashState());
				}
			} else if (state is MJump || state is MFall) {
			}
		}
	}

	public override float getRunSpeed() {
		return 80;
	}

	public override string getMaverickPrefix() {
		return "gbeetle";
	}

	public override MaverickState[] strikerStates() {
		return [
			new GBeetleShoot(false),
			new GBeetleGravityWellState(),
			new GBeetleDashState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		return [
			new GBeetleShoot(false),
			new GBeetleGravityWellState(),
			new GBeetleDashState(),
		];
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Dash,
		Fall,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"gbeetle_dash" => MeleeIds.Dash,
			"gbeetle_fall" => MeleeIds.Fall,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Dash => new GenericMeleeProj(
				weapon, pos, ProjIds.GBeetleLift, player,
				0, 0, 0, addToLevel: addToLevel
			),
			MeleeIds.Fall => new GenericMeleeProj(
				weapon, pos, ProjIds.GBeetleStomp, player,
				1, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.GBeetleStomp) {
			float damage = Helpers.clamp(MathF.Floor(deltaPos.y * 0.9f), 1, 4);
			proj.damager.damage = damage;
		}
	}

}

public class GBeetleBallProj : Projectile {
	bool firstHit;
	int size;
	const float moveSpeed = 200;
	bool isSecond;
	public GBeetleBallProj(
		Point pos, int xDir, bool isSecond, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "gbeetle_proj1", netId, player
	) {
		weapon = GravityBeetle.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 30;
		vel = new Point (moveSpeed * xDir, 0);
		projId = (int)ProjIds.GBeetleBall;
		maxTime = 3f;
		destroyOnHit = false;
		this.isSecond = isSecond;
		if (isSecond) {
			vel = new Point(0, -moveSpeed);
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, isSecond ? (byte) 1 : (byte) 0);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new GBeetleBallProj(
			args.pos, args.xDir, args.extraData[0] == 1, args.owner, args.player, args.netId
		);
	}

	public void increaseSize() {
		if (size >= 2) return;
		if (size == 0) changeSprite("gbeetle_proj2", true);
		else if (size == 1) changeSprite("gbeetle_proj3", true);

		int flinch = 0;
		if (size == 1) flinch = Global.halfFlinch;
		if (size == 2) flinch = Global.defFlinch;

		updateDamager((size + 1) * 2, flinch);
		size++;
		forceNetUpdateNextFrame = true;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (other.myCollider.isTrigger) return;

		bool didHit = false;
		if (!firstHit) {
			firstHit = true;
			didHit = true;
			if (!isSecond) {
				vel.x *= -1;
				vel.y = -moveSpeed;
			} else {
				vel.x = xDir * moveSpeed;
				vel.y = moveSpeed;
			}
		} else if (other.isSideWallHit()) {
			vel.x *= -1;
			didHit = true;
		} else if (other.isCeilingHit() || other.isGroundHit()) {
			vel.y *= -1;
			didHit = true;
		}
		if (didHit) {
			playSound("gbeetleProjBounce", sendRpc: false);
			increaseSize();
		}
	}
}

public class GBeetleShoot : MaverickState {
	bool shotOnce;
	bool isSecond;
	public GravityBeetle GravityBeetbood = null!;
	public GBeetleShoot(bool isSecond) :
		base(isSecond ? "attackproj2" : "attackproj", isSecond ? "attackproj2_start" : "attackproj_start") {
		this.isSecond = isSecond;
		exitOnAnimEnd = true;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		GravityBeetbood = maverick as GravityBeetle ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			new GBeetleBallProj(
				shootPos.Value, maverick.xDir, isSecond,
				GravityBeetbood, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (!isSecond && maverick.frameIndex >= 5) {
			if (isAI || input.isPressed(Control.Shoot, player)) {
				maverick.changeState(new GBeetleShoot(true));
				return;
			}
		}
	}
}

public class GBeetleDashState : MaverickState {
	float soundTime;
	float dustTime;
	float partTime;
	public GBeetleDashState() : base("dash", "dash_start") {
	}

	public override void update() {
		base.update();
		if (inTransition()) return;
		if (ftdWaitTime > 0) {
			tryChangeToIdle();
			return;
		}

		Helpers.decrementTime(ref soundTime);
		if (soundTime == 0) {
			maverick.playSound("gbeetleDash", sendRpc: true);
			soundTime = 0.085f;
		}
		Helpers.decrementTime(ref dustTime);
		if (dustTime == 0) {
			new Anim(maverick.getFirstPOIOrDefault(0), "dust", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
			dustTime = 0.05f;
		}
		Helpers.decrementTime(ref partTime);
		if (partTime == 0) {
			var vel = new Point(0, -250);
			float ttl = 0.6f;
			new Anim(maverick.getFirstPOIOrDefault(1), "gbeetle_debris", maverick.xDir, player.getNextActorNetId(), false, sendRpc: true) { vel = vel, useGravity = true, ttl = ttl };
			partTime = 0.1f;
		}

		var move = new Point(250 * maverick.xDir, 0);

		var hitGround = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 5, 20);
		if (hitGround == null) {
			tryChangeToIdle();
			return;
		}

		var hitWall = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 2, -5);
		if (hitWall?.isSideWallHit() == true) {
			maverick.playSound("crash", sendRpc: true);
			maverick.shakeCamera(sendRpc: true);
			tryChangeToIdle();
			return;
		}

		maverick.move(move);

		if (stateTime > 1f) {
			tryChangeToIdle();
			return;
		}
	}

	float ftdWaitTime;
	public void tryChangeToIdle() {
		if (player.isDefenderFavored) {
			ftdWaitTime += Global.spf;
			if (ftdWaitTime < 0.25f) {
				return;
			}
		}
		maverick.changeState(new MIdle("dash_end"));
	}

	public override bool trySetGrabVictim(Character grabbed) {
		maverick.changeState(new GBeetleLiftState(grabbed), true);
		return true;
	}
}

public class GBeetleLiftState : MaverickState {
	public Character grabbedChar;
	float timeWaiting;
	bool grabbedOnce;
	public GBeetleLiftState(Character grabbedChar) : base("dash_lift") {
		this.grabbedChar = grabbedChar;
	}

	public override void update() {
		base.update();

		if (!grabbedOnce && grabbedChar != null && !grabbedChar.sprite.name.EndsWith("_grabbed") && maverick.frameIndex > 1 && timeWaiting < 0.5f) {
			maverick.frameSpeed = 0;
			timeWaiting += Global.spf;
		} else {
			maverick.frameSpeed = 1;
		}

		if (grabbedChar != null && grabbedChar.sprite.name.EndsWith("_grabbed")) {
			grabbedOnce = true;
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}
}
public class GravityBeetboodDeadLiftWeapon : Weapon {
	public GravityBeetboodDeadLiftWeapon(Player player) {
		index = (int)WeaponIds.GravityBeetle;
		killFeedIndex = 97;
		damager = new Damager(player, 4, Global.defFlinch, 0.5f);
	}
}
public class BeetleGrabbedState : GenericGrabbedState {
	public Character? grabbedChar;
	public bool launched;
	float launchTime;
	public GravityBeetle? GravityBeetbood;
	public BeetleGrabbedState(GravityBeetle grabber) : base(grabber, 1, "") {
		customUpdate = true;
	}


	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) { return; }

		if (launched) {
			launchTime += Global.spf;
			if (launchTime > 0.33f) {
				character.changeToIdleOrFall();
				return;
			}
			if (Global.level.checkTerrainCollisionOnce(character, 0, -1) != null) {
				new GravityBeetboodDeadLiftWeapon((grabber as Maverick)?.player ?? player).applyDamage(character, false, character, (int)ProjIds.GBeetleLiftCrash);
				character.playSound("crashX3", sendRpc: true);
				character.shakeCamera(sendRpc: true);
			}
			return;
		}

		if (grabber.sprite?.name.EndsWith("_dash_lift") == true) {
			if (grabber.frameIndex < 2) {
				trySnapToGrabPoint(true);
			} else if (!launched) {
				launched = true;
				character.unstickFromGround();
				character.vel.y = -600;
			}
		} else {
			notGrabbedTime += Global.spf;
		}

		if (notGrabbedTime > 0.5f) {
			character.changeToIdleOrFall();
		}
	}
}

public class GBeetleGravityWellProj : Projectile {
	public int state;
	public float drawRadius;
	const float riseSpeed = 150;
	public float radiusFactor;
	float randPartTime;
	public float maxRadius = 50;
	float ttl = 4;
	public int chargeTime;

	public GBeetleGravityWellProj(
		Point pos, int xDir, int chargeTime, Actor owner,
		Player player, ushort netProjId, bool sendRpc = false
	) : base(
		pos, xDir, owner, "gbeetle_proj_blackhole", netProjId, player

	) {
		weapon = GravityBeetle.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.GBeetleGravityWell;
		setIndestructableProperties();
		this.chargeTime = chargeTime;
		maxRadius = 25 + (50 * (chargeTime / 2f));
		ttl = chargeTime * 2;
		//Just in case
		maxTime = 8;
		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)chargeTime);
		}
		canBeLocal = false; 
		//AAAAAA
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new GBeetleGravityWellProj(
			args.pos, args.xDir, args.extraData[0],
			args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		drawRadius = radiusFactor * maxRadius;
		if (radiusFactor > 0) {
			globalCollider = new Collider(new Rect(0, 0, 24 + (radiusFactor * maxRadius), 24 + (radiusFactor * maxRadius)).getPoints(), true, this, false, false, 0, Point.zero);
		}

		if (!ownedByLocalPlayer) return;

		if (state == 0) {
			move(new Point(0, -riseSpeed));
			moveDistance += riseSpeed * Global.spf;
			var hit = checkCollision(0, -1);
			if (moveDistance > 175 || hit?.isCeilingHit() == true) {
				state = 1;
				playSound("gbeetleWell", sendRpc: true);
			}
		} else if (state == 1) {
			radiusFactor += Global.spf * 1.5f;
			if (radiusFactor >= 1) {
				state = 2;
				maxTime = ttl;
				time = 0;
			}
		} else if (state == 2) {
			randPartTime += Global.spf;
			if (randPartTime > 0.025f) {
				randPartTime = 0;
				var partSpawnAngle = Helpers.randomRange(0, 360);
				float spawnRadius = maxRadius;
				float spawnSpeed = 300;
				var partSpawnPos = pos.addxy(Helpers.cosd(partSpawnAngle) * spawnRadius, Helpers.sind(partSpawnAngle) * spawnRadius);
				var partVel = partSpawnPos.directionToNorm(pos).times(spawnSpeed);
				var partSprite = "gbeetle_proj_flare" + (Helpers.randomRange(0, 1) == 0 ? "2" : "");
				new Anim(partSpawnPos, partSprite, 1, owner.getNextActorNetId(), false, sendRpc: true) {
					vel = partVel,
					ttl = ((spawnRadius - 10) / spawnSpeed),
				};
			}
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!damagable.isPlayableDamagable()) { return; }
		var actor = damagable.actor();
		Character? chr = actor as Character;
		if (chr != null && !chr.isPushImmune()) return;
		if (actor is not Character && actor is not RideArmor && actor is not Maverick) return;
		if (chr != null && (chr.charState is DeadLiftGrabbed || chr.charState is BeetleGrabbedState)) return;

		float mag = 100;
		if (!actor.grounded) actor.vel.y = 0;
		Point velVector = actor.getCenterPos().directionToNorm(pos).times(mag);
		actor.move(velVector, true);
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (state >= 1) {
			DrawWrappers.DrawCircle(pos.x + x, pos.y + y, drawRadius, true, Color.Black, 1, ZIndex.Background + 10);
		}
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();

		customData.AddRange(BitConverter.GetBytes(radiusFactor));
		customData.AddRange(BitConverter.GetBytes(maxRadius));
		customData.Add((byte)state);

		return customData;
	}
	public override void updateCustomActorNetData(byte[] data) {
		radiusFactor = BitConverter.ToSingle(data[0..4], 0);
		maxRadius = BitConverter.ToSingle(data[4..8], 0);
		state = data[8];
	}
}

public class GBeetleGravityWellState : MaverickState {
	int state = 0;
	float partTime;
	float chargeTime;
	public GravityBeetle GravityBeetbood = null!;
	public GBeetleGravityWellState() : base("blackhole_start") {
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		GravityBeetbood = maverick as GravityBeetle ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		if (state == 0) {
			Helpers.decrementTime(ref partTime);
			if (partTime <= 0) {
				partTime = 0.2f;
				var vel = new Point(0, 50);
				new Anim(
					maverick.getFirstPOIOrDefault(0), "gbeetle_proj_flare", 1, 
					player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
				new Anim(
					maverick.getFirstPOIOrDefault(1), "gbeetle_proj_flare", 1,
					 player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
				new Anim(
					maverick.getFirstPOIOrDefault(2), "gbeetle_proj_flare", 1,
					player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
				new Anim(
					maverick.getFirstPOIOrDefault(3), "gbeetle_proj_flare", 1,
					player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
				new Anim(
					maverick.getFirstPOIOrDefault(4), "gbeetle_proj_flare", 1,
					player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
			}

			if (isHoldStateOver(1f, 3f, 2f, Control.Special1)) {
				maverick.changeSpriteFromName("blackhole", true);
				chargeTime = (int)stateTime;
				state = 1;
			}
		} else if (state == 1) {
			Point? shootPos = maverick.getFirstPOI();
			if (!once && shootPos != null) {
				once = true;
				GravityBeetbood.well = new GBeetleGravityWellProj(
					shootPos.Value, maverick.xDir, (int)chargeTime,
					GravityBeetbood, player, player.getNextActorNetId(), sendRpc: true
				);
			}
			if (maverick.isAnimOver()) {
				maverick.changeToIdleOrFall();
			}
		}
	}
}
