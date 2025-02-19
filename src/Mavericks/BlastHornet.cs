using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class BlastHornet : Maverick {
	public static Weapon netWeapon = new Weapon(WeaponIds.BHornetGeneric, 158);
	public static Weapon getWeapon() { return new Weapon(WeaponIds.BHornetGeneric, 158); }

	public BHornetCursorProj? cursor;
	//public Actor lockOnTarget;
	//public float lockOnTime;
	public Sprite wings;

	public BlastHornet(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
		base(player, pos, destPos, xDir, netId, ownedByLocalPlayer) {
		stateCooldowns.Add(typeof(BHornetShootState), new MaverickStateCooldown(false, false, 2f));
		stateCooldowns.Add(typeof(BHornetShootCursorState), new MaverickStateCooldown(false, false, 0.25f));
		stateCooldowns.Add(typeof(BHornetShoot2State), new MaverickStateCooldown(false, false, 0f));
		stateCooldowns.Add(typeof(BHornetStingState), new MaverickStateCooldown(false, false, 0.5f));

		weapon = new Weapon(WeaponIds.BHornetGeneric, 158);
		wings = new Sprite("bhornet_wings");
		canFly = true;

		awardWeaponId = WeaponIds.ParasiticBomb;
		weakWeaponId = WeaponIds.GravityWell;
		weakMaverickWeaponId = WeaponIds.GravityBeetle;

		netActorCreateId = NetActorCreateId.BlastHornet;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		canFly = true;
		maxFlyBar = 960;
		flyBar = 960;
		flyBarIndexes = (68, 57);
		gameMavs = GameMavs.X3;
	}

	public override void update() {
		base.update();
		wings.update();
		if (!ownedByLocalPlayer) return;

		if (cursor != null && cursor.destroyed) {
			cursor = null;
		}

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new BHornetShootState(true));
				} else if (input.isPressed(Control.Special1, player)) {
					if (cursor == null) {
						changeState(new BHornetShootCursorState(true));
					}
				}
			} else if (state is MJump || state is MFall || state is MFly) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new BHornetShootState(false));
				} else if (input.isPressed(Control.Special1, player)) {
					var specialState = getSpecialState();
					if (specialState != null) changeState(specialState);
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new BHornetStingState());
				}
			}
		}
	}

	public override string getMaverickPrefix() {
		return "bhornet";
	}

	public MaverickState? getSpecialState() {
		if (cursor != null) {
			if (cursor.target != null) {
				return new BHornetShoot2State(cursor.target);
			} else {
				return null;
			}
		} else {
			return new BHornetShootCursorState(false);
		}
	}

	public override MaverickState getRandomAttackState() {
		return aiAttackStates().GetRandomItem();
	}

	public override MaverickState[] aiAttackStates() {
		var states = new List<MaverickState>
		{
				new BHornetShootState(grounded),
				new BHornetShoot2State(null),
				new BHornetStingState(),
			};

		return states.ToArray();
	}

	public Point? getWingPOI(out string tag) {
		tag = "";
		if (sprite.getCurrentFrame().POIs.Length > 0) {
			for (int i = 0; i < sprite.getCurrentFrame().POITags.Length; i++) {
				tag = sprite.getCurrentFrame().POITags[i];
				if (tag == "wings") {
					return getFirstPOIOffsetOnly(i);
				}
			}
		}
		return null;
	}

	public override void render(float x, float y) {
		base.render(x, y);

		var wingsPOI = getWingPOI(out string tag);
		if (wingsPOI != null && tag == "wings") {
			wings.draw(wings.frameIndex, pos.x + (xDir * wingsPOI.Value.x), pos.y + wingsPOI.Value.y, xDir, 1, null, alpha, 1, 1, zIndex - 100, useFrameOffsets: true);
		}
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Stinger,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"bhornet_fly_stinger_attack" => MeleeIds.Stinger,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Stinger => new GenericMeleeProj(
				weapon, pos, ProjIds.BHornetSting, player,
				7, Global.defFlinch, owningActor: this,
				addToLevel: addToLevel
			),
			_ => null
		};
	}
}

public class BHornetBeeProj : Projectile, IDamagable {
	public Point lastMoveAmount;
	const float maxSpeed = 150;
	public Actor? latchTarget;
	float latchLerpTime;
	public int type = 1;
	public BHornetBeeProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bhornet_proj_wasp_small", netId, player
	) {
		weapon = BlastHornet.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 60;
		fadeOnAutoDestroy = true;
		fadeSprite = "bhornet_particle_explosion";
		fadeSound = "explosionX3";
		maxTime = 1.25f;
		projId = (int)ProjIds.BHornetBee;
		destroyOnHit = false;
		shouldShieldBlock = true;
		this.type = type;
		if (type == 1) vel = new Point(170 * xDir, 30);	
		if (type == 2) vel = new Point(150 * xDir, 80);
		if (type == 3) vel = new Point(130 * xDir, 120);
		if (type == 4) vel = new Point(110 * xDir, 150);
		if (type == 5) vel = new Point(90 * xDir, 180);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new BHornetBeeProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (latchTarget?.destroyed == true) {
			latchTarget = null;
		}

		if (latchTarget != null) {
			Helpers.decrementTime(ref latchLerpTime);
			if (latchLerpTime > 0) {
				changePos(Point.lerp(pos, latchTarget.getCenterPos(), Global.spf * 5));
			}
			incPos(latchTarget.deltaPos);
		}
	}

	public override void render(float x, float y) {
		if (!ownedByLocalPlayer && latchTarget != null) {
			base.render(x + latchTarget.deltaPos.x, y + latchTarget.deltaPos.y);
		} else {
			base.render(x, y);
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (damagable is Character chr && chr.ownedByLocalPlayer && !chr.isSlowImmune()) {
			chr.slowdownTime = Math.Max(0.05f, chr.slowdownTime);
		}
		if (latchTarget == null) {
			stopMoving();
			latchTarget = damagable.actor();
			latchLerpTime = 0.1f;
		}
		maxTime = 3;
		forceNetUpdateNextFrame = true;
		if (!ownedByLocalPlayer) return;


	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		stopMoving();
		maxTime = 3;
		forceNetUpdateNextFrame = true;
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (damage > 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return damager.owner.alliance != damagerAlliance; }
	public bool isInvincible(Player attacker, int? projId) { return false; }
	public bool canBeHealed(int healerAlliance) { return false; }
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
	public bool isPlayableDamagable() { return false; }

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();
		customData.AddRange(BitConverter.GetBytes(latchTarget?.netId ?? ushort.MaxValue));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		ushort targetNetId = BitConverter.ToUInt16(data, 0);
		if (targetNetId == ushort.MaxValue) {
			return;
		}
		latchTarget = Global.level.getActorByNetId(targetNetId);
	}
}

public class BHornetHomingBeeProj : Projectile, IDamagable {
	public Actor? target;
	public Point lastMoveAmount;
	const float maxSpeed = 150;
	public BHornetHomingBeeProj(
		Point pos, int xDir, Actor? target, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bhornet_proj_wasp_small_glowing", netId, player
	) {
		weapon = BlastHornet.getWeapon();
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		fadeOnAutoDestroy = true;
		fadeSprite = "bhornet_particle_explosion";
		fadeSound = "explosionX3";
		maxTime = 3f;
		projId = (int)ProjIds.BHornetHomingBee;
		destroyOnHit = true;
		shouldShieldBlock = true;
		if (target == null) {
			target = Global.level.getClosestTarget(pos, player.alliance, false, 150);
		}
		if (target == null) {
			vel = new Point(xDir, 2).normalize().times(150);
		}
		this.target = target;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new BHornetHomingBeeProj(
			args.pos, args.xDir, null, args.owner, args.player, args.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		if (Global.isOnFrameCycle(10)) {
			changeSpriteIfDifferent("bhornet_proj_wasp_small", false);
		} else {
			changeSpriteIfDifferent("bhornet_proj_wasp_small_glowing", false);
		}

		if (!ownedByLocalPlayer) return;

		if (target != null && !target.destroyed) {
			Point amount = pos.directionToNorm(target.getCenterPos()).times(150);
			vel = Point.lerp(vel, amount, Global.spf * 4);
			if (vel.magnitude > maxSpeed) vel = vel.normalize().times(maxSpeed);
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (damage > 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return damager.owner.alliance != damagerAlliance; }
	public bool isInvincible(Player attacker, int? projId) { return false; }
	public bool canBeHealed(int healerAlliance) { return false; }
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
	public bool isPlayableDamagable() { return false; }
}

public class BHornetShootState : MaverickState {
	bool shotOnce;
	public BlastHornet ExploseHorneck  = null!;
	public BHornetShootState(bool isGrounded) : base(isGrounded ? "attack" : "fly_attack") {
	}

	public override void update() {
		base.update();

		Point? shootPos = maverick.getFirstPOI("s");
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			for (int i = 1; i <= 5; i++)
				HornetProj(i);
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleFallOrFly();
		}
	}
	public void HornetProj(int type) {
		Point? shootPos = maverick.getFirstPOI("s");
		if (shootPos != null) {
			new BHornetBeeProj(
				shootPos.Value, maverick.xDir, type, ExploseHorneck,
				player, player.getNextActorNetId(), rpc: true);
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ExploseHorneck = maverick as BlastHornet ?? throw new NullReferenceException();
		if (wasFlying) maverick.useGravity = false;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}
}

public class BHornetShootCursorState : MaverickState {
	bool shotOnce;
	public BlastHornet ExploseHorneck  = null!;
	public BHornetShootCursorState(bool isGrounded) : base(isGrounded ? "attack" : "fly_attack") {
	}

	public override void update() {
		base.update();
		bool upHeld = player.input.isHeld(Control.Up, player);
		bool downHeld = player.input.isHeld(Control.Down, player);
		bool LeftOrRightHeld = player.input.isHeld(Control.Left, player) || 
							   player.input.isHeld(Control.Right, player);
		Point? shootPos = maverick.getFirstPOI("s");
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			if (downHeld && LeftOrRightHeld) {
				CursorProj(4);
			} else if (downHeld) {
				CursorProj(1);
			} else if (upHeld && LeftOrRightHeld) {
				CursorProj(3);
			} else if (upHeld) {
				CursorProj(2);
			} else 
				CursorProj(0);
		}
		if (maverick.isAnimOver()) {
			maverick.changeToIdleFallOrFly();
		}
	}
	public void CursorProj(int type) {
		Point? shootPos = maverick.getFirstPOI("s");
		if (shootPos != null) {
			ExploseHorneck.cursor = new BHornetCursorProj(
				shootPos.Value, maverick.xDir, type,
				ExploseHorneck, ExploseHorneck, player,
				player.getNextActorNetId(), rpc: true
			);
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ExploseHorneck = maverick as BlastHornet ?? throw new NullReferenceException();
		if (wasFlying) maverick.useGravity = false;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}
}

public class BHornetCursorProj : Projectile {
	public BlastHornet bh;
	public Actor? target;
	public int type = 0;
	public BHornetCursorProj(
		Point pos, int xDir, int type, BlastHornet bh, 
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bhornet_particle_aim_small", netId, player
	) {
		weapon = BlastHornet.getWeapon();
		this.bh = bh;
		this.type = type;
		maxTime = 0.8f;
		projId = (int)ProjIds.BHornetCursor;
		destroyOnHit = false;
		setIndestructableProperties();
		if (type == 0) vel = new Point(200 * xDir, 0);	
		if (type == 1) vel = new Point(200 * xDir, 180);
		if (type == 2) vel = new Point(200 * xDir, -180);
		if (type == 3) vel = new Point(200 * xDir, -90);
		if (type == 4) vel = new Point(200 * xDir, 90);
		if (rpc) {
			byte[] bhbNetIdBytes = BitConverter.GetBytes(bh.netId ?? 0);
			byte[] bhTypeBytes = new byte[] { (byte)type };
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] {bhTypeBytes[0], bhbNetIdBytes[0], bhbNetIdBytes[1]});
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		ushort ExploseHorneckId = BitConverter.ToUInt16(args.extraData, 1);
		BlastHornet? ExploseHorneck = Global.level.getActorByNetId(ExploseHorneckId) as BlastHornet;
		return new BHornetCursorProj(
			args.pos, args.xDir, args.extraData[0], ExploseHorneck!, args.owner, args.player, args.netId
		);
	}

	public override void postUpdate() {
		base.postUpdate();
		if (!ownedByLocalPlayer) return;

		if (target != null) {
			changePos(getTargetPos(target));

			if (target.pos.distanceTo(bh.pos) > 200 || target.destroyed) {
				target = null;
				destroySelf();
			}
		}
	}

	public Point getTargetPos([NotNull] Actor target) {
		if (target is Character chr) {
			return chr.getParasitePos();
		} else {
			return target.getCenterPos();
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!ownedByLocalPlayer) return;

		if (target == null) {
			var hitActor = damagable.actor();
			target = hitActor;
			stopMoving();
			time = 0;
			maxTime = 6;
			forceNetUpdateNextFrame = true;
			playSound("bhornetLockOn", sendRpc: true);
			changeSprite("bhornet_particle_aim_big", true);
		}
	}

	public override void render(float x, float y) {
		if (target != null && !ownedByLocalPlayer) {
			var targetPos = getTargetPos(target);
			var diff = targetPos.subtract(pos);
			base.render(x + diff.x, y + diff.y);
		} else {
			base.render(x, y);
		}
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();
		customData.AddRange(BitConverter.GetBytes(target?.netId ?? ushort.MaxValue));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		ushort targetNetId = BitConverter.ToUInt16(data, 0);
		if (targetNetId == ushort.MaxValue) {
			return;
		}
		target = Global.level.getActorByNetId(targetNetId);
	}
}

public class BHornetShoot2State : MaverickState {
	bool shotOnce;
	Actor? target;
	bool isAIRise;
	public BlastHornet ExploseHorneck  = null!;
	public BHornetShoot2State(Actor? target) : base("fly_wasp_spawn") {
		this.target = target;
	}

	public override void update() {
		base.update();

		if (isAIRise) {
			if (stateTime < 0.25f) {
				maverick.useGravity = false;
				maverick.grounded = false;
				maverick.move(new Point(0, -200));
				maverick.frameSpeed = 0;
			} else {
				maverick.frameSpeed = 1;
			}
		}

		Point? shootPos = maverick.getFirstPOI("s");
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			new BHornetHomingBeeProj(
				shootPos.Value, maverick.xDir, target,
				ExploseHorneck, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleFallOrFly();
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ExploseHorneck = maverick as BlastHornet ?? throw new NullReferenceException();
		if (wasFlying) maverick.useGravity = false;
		if (maverick.grounded && isAI) {
			isAIRise = true;
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}
}

public class BHornetStingState : MaverickState {
	float moveTime;
	Anim? stingAnim;
	bool isAIRise;
	public BHornetStingState() : base("fly_stinger_attack", "fly_stinger_start") {
		useGravity = false;
	}

	public override void update() {
		base.update();
		if (inTransition()) {
			if (isAIRise && maverick.frameIndex < 7) {
				maverick.useGravity = false;
				maverick.grounded = false;
				maverick.move(new Point(0, -200));
			}

			maverick.turnToInput(input, player);

			if (!once && maverick.frameIndex >= 7) {
				once = true;
				maverick.playSound("bhornetSting", sendRpc: true);
			}

			Point? stingAnimPos = maverick.getFirstPOI("spark");
			if (stingAnim == null && stingAnimPos != null) {
				stingAnim = new Anim(
					stingAnimPos.Value, "bhornet_particle_stingerflash", maverick.xDir,
					player.getNextActorNetId(), true, sendRpc: true
				);
			}

			return;
		}

		var moveVel = new Point(maverick.xDir * 300, 400);
		if (maverick.isUnderwater()) {
			moveVel = moveVel.times(0.75f);
		}

		maverick.move(moveVel);
		moveTime += Global.spf;

		if (maverick.grounded) {
			maverick.changeState(new MIdle("fly_stinger_end"));
			return;
		}

		var hit = checkCollisionNormal(moveVel.x * Global.spf, moveVel.y * Global.spf);
		if (hit != null && !hit.getNormalSafe().isGroundNormal()) {
			maverick.changeState(wasFlying ? new MFly("fly_stinger_end") : new MFall("fly_stinger_end"));
			return;
		}

		if (moveTime > 0.375f) {
			maverick.changeState(wasFlying ? new MFly("fly_stinger_end") : new MFall("fly_stinger_end"));
			return;
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		if (maverick.grounded && isAI) {
			isAIRise = true;
		}
	}
}
