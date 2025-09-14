using System;
using System.Collections.Generic;
namespace MMXOnline;

public class FlameMammoth : Maverick {
	public FlameMStompWeapon stompWeapon = new();

	public FlameMammoth(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(MShoot), new(30, true) },
			{ typeof(FlameMOilState), new(30, true) }
		};

		awardWeaponId = WeaponIds.FireWave;
		weakWeaponId = WeaponIds.StormTornado;
		weakMaverickWeaponId = WeaponIds.StormEagle;

		weapon = new Weapon(WeaponIds.FlameMGeneric, 100);

		netActorCreateId = NetActorCreateId.FlameMammoth;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		armorClass = ArmorClass.Heavy;
		canStomp = true;
	}

	public override void update() {
		base.update();
		subtractTargetDistance = 70;
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (shootPressed()) {
					changeState(getShootState(false));
				} else if (specialPressed()) {
					changeState(new FlameMOilState());
				}
			} else if (state is MJump || state is MFall) {
				if (input.isPressed(Control.Dash, player)) {
					changeState(new FlameMJumpPressState());
				}
			}
		}
	}

	public override string getMaverickPrefix() {
		return "flamem";
	}

	public MaverickState getShootState(bool isAI) {
		var shootState = new MShoot((Point pos, int xDir) => {
			new FlameMFireballProj(
				pos, xDir, player.input.isHeld(Control.Down, player),
				this, player, player.getNextActorNetId(), rpc: true
			);
		}, "flamemShoot");
		if (isAI) {
			shootState.consecutiveData = new MaverickStateConsecutiveData(0, 3, 0.1f);
		}
		return shootState;
	}

	public override MaverickState[] strikerStates() {
		return [
			getShootState(true),
			new FlameMJumpStateAI(),
			new FlameMOilState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = target.pos.distanceTo(pos);
		}
		List<MaverickState> aiStates = [
			new FlameMOilState(),
			getShootState(false)
		];
		if (grounded && enemyDist <= 30) {
			aiStates.Add(new FlameMJumpStateAI());
		}
		return aiStates.ToArray();
	}


	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Fall,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"flamem_fall" => MeleeIds.Fall,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Fall => new GenericMeleeProj(
				stompWeapon, pos, ProjIds.FlameMStomp, player,
				6, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.FlameMStomp) {
			float damage = Helpers.clamp(MathF.Floor(vel.y / 75), 1, 6);
			if (vel.y > 300) damage += 2;
			proj.damager.damage = damage;
		}
	}

}

#region weapons
public class FlameMFireballWeapon : Weapon {
	public static FlameMFireballWeapon netWeapon = new();
	public FlameMFireballWeapon() {
		index = (int)WeaponIds.FlameMFireball;
		killFeedIndex = 100;
	}
}

public class FlameMStompWeapon : Weapon {
	public static FlameMStompWeapon netWeapon = new();
	public FlameMStompWeapon() {
		index = (int)WeaponIds.FlameMStomp;
		killFeedIndex = 100;
	}
}

public class FlameMOilWeapon : Weapon {
	public static FlameMOilWeapon netWeapon = new();
	public FlameMOilWeapon() {
		index = (int)WeaponIds.FlameMOil;
		killFeedIndex = 100;
	}
}

public class FlameMOilFireWeapon : Weapon {
	public static FlameMOilFireWeapon netWeapon = new();
	public FlameMOilFireWeapon() {
		index = (int)WeaponIds.FlameMOilFire;
		killFeedIndex = 100;
	}
}

#endregion

#region projectiles
public class FlameMFireballProj : Projectile {
	public FlameMFireballProj(
		Point pos, int xDir, bool isShort, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "flamem_proj_fireball", netId, player	
	) {
		weapon = FlameMFireballWeapon.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 1;
		vel = new Point(250 * xDir, 0);
		projId = (int)ProjIds.FlameMFireball;
		fadeSprite = "flamem_anim_fireball_fade";
		maxTime = 0.75f;
		useGravity = true;
		gravityModifier = 0.5f;
		if (collider != null) { collider.wallOnly = true; }
		if (isShort) {
			vel.x *= 0.5f;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, isShort ? (byte)1 : (byte)0);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FlameMFireballProj(
			args.pos, args.xDir, args.extraData[0] == 1, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (isUnderwater()) {
			destroySelf();
			return;
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (other.gameObject is FlameMOilSpillProj oilSpill && oilSpill.ownedByLocalPlayer) {
			playSound("flamemOilBurn", sendRpc: true);
			new FlameMBigFireProj(
				oilSpill.pos, oilSpill.xDir, oilSpill.angle,
				this, owner, owner.getNextActorNetId(), rpc: true
			);
			// oilSpill.time = 0;
			oilSpill.destroySelf(doRpcEvenIfNotOwned: true);
			destroySelf();
		}
	}
}

public class FlameMOilProj : Projectile {
	public FlameMOilProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "flamem_proj_oilball", netId, player	

	) {
		weapon = FlameMOilWeapon.netWeapon;
		vel = new Point(175 * xDir, 0);
		projId = (int)ProjIds.FlameMOil;
		maxTime = 0.75f;
		useGravity = true;
		vel.y = -150;
		if (collider != null) { collider.wallOnly = true; }

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FlameMOilProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;
		if (!destroyed) {
			new FlameMOilSpillProj(
				other.getHitPointSafe(), 1, other.getNormalSafe().angle + 90,
				this, owner, owner.getNextActorNetId(), rpc: true
			);
			playSound("flamemOil", sendRpc: true);
			destroySelf();
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (isUnderwater()) {
			destroySelf();
			return;
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (other.gameObject is FlameMBigFireProj bigFire && bigFire.ownedByLocalPlayer && !destroyed) {
			playSound("flamemOilBurn", sendRpc: true);
			bigFire.reignite();
			destroySelf();
		}
	}
}

public class FlameMOilSpillProj : Projectile {
	public FlameMOilSpillProj(
		Point pos, int xDir, float angle,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "flamem_proj_oilspill", netId, player	

	) {
		weapon = FlameMOilWeapon.netWeapon;
		vel = new Point(0, 0);
		projId = (int)ProjIds.FlameMOilSpill;
		maxTime = 8f;
		this.angle = angle;
		destroyOnHit = false;

		if (rpc) {
			rpcCreateAngle(pos, owner, ownerPlayer, netId, angle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FlameMOilSpillProj(
			args.pos, args.xDir, args.angle, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		moveWithMovingPlatform();
		if (!ownedByLocalPlayer) return;

		if (isUnderwater()) {
			destroySelf();
			return;
		}
	}
}

public class FlameMBigFireProj : Projectile {
	public FlameMBigFireProj(
		Point pos, int xDir, float angle,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "flamem_proj_bigfire", netId, player	

	) {
		weapon = FlameMOilFireWeapon.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 15;
		damager.flinch = Global.defFlinch;
		vel = new Point(0, 0);
		projId = (int)ProjIds.FlameMOilFire;
		maxTime = 8;
		this.angle = angle;
		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;

		if (rpc) {
			rpcCreateAngle(pos, owner, ownerPlayer, netId, angle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FlameMBigFireProj(
			args.pos, args.xDir, args.angle, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		moveWithMovingPlatform();

		if (!ownedByLocalPlayer) return;
		if (isUnderwater()) {
			destroySelf();
			return;
		}
	}

	public void reignite() {
		frameIndex = 0;
		frameTime = 0;
		time = 0;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (other.gameObject is FlameMOilSpillProj oilSpill && oilSpill.ownedByLocalPlayer && frameIndex >= 4) {
			playSound("flamemOilBurn", sendRpc: true);
			new FlameMBigFireProj(
				oilSpill.pos, oilSpill.xDir,
				oilSpill.angle, this,
				owner, owner.getNextActorNetId(), rpc: true
			);
			// oilSpill.time = 0;
			oilSpill.destroySelf();
		}
	}
}


public class FlameMStompShockwave : Projectile {
	public FlameMStompShockwave(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "flamem_proj_shockwave", netId, player	
	) {
		weapon = FlameMStompWeapon.netWeapon;
		damager.hitCooldown = 60;
		vel = new Point(0, 0);
		maxTime = 0.75f;
		projId = (int)ProjIds.FlameMStompShockwave;
		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FlameMStompShockwave(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void onStart() {
		base.onStart();
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (isAnimOver()) {
			destroySelf();
		}
	}
}

#endregion

#region states
public class MammothMState : MaverickState {
	public FlameMammoth BurninNoumander = null!;
	public MammothMState(
		string sprite, string transitionSprite = ""
	) : base(
		sprite, transitionSprite
	) {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		BurninNoumander = maverick as FlameMammoth ?? throw new NullReferenceException();
	}
}

public class FlameMOilState : MammothMState {
	public FlameMOilState() : base("shoot2") {
	}

	public override bool canEnter(Maverick maverick) {
		return base.canEnter(maverick);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
	}

	public override void update() {
		base.update();
		if (BurninNoumander == null) return;

		if (maverick.frameIndex == 6 && !once) {
			once = true;
			new FlameMOilProj(
			BurninNoumander.getFirstPOI() ?? BurninNoumander.getCenterPos(), maverick.xDir, 
			BurninNoumander, player, player.getNextActorNetId(), rpc: true);
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
}

public class FlameMJumpPressState : MammothMState {
	public FlameMJumpPressState() : base("fall") {
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (maverick.grounded) {
			if (maverick.isAI && maverick.controlMode == MaverickModeId.Striker) {
				landingCode(false);
			} else {
				landingCode();				
			}
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.vel = new Point(0, 300);
	}
}
public class FlameMJumpStateAI : MammothMState {
	public FlameMJumpStateAI() : base("jump", "jump_start") {
	}

	public override void update() {
		base.update();
		if (player == null) return;
		if (stateTime >= 24f/60f) {
			maverick.changeState(new FlameMJumpPressState());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.vel.y = -maverick.getJumpPower() * 1.25f;
	}
}
#endregion
