using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Velguarder : Maverick {
	public VelGMeleeWeapon meleeWeapon = new();

	public Velguarder(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(MShoot), new(45, true) }
		};
		canClimbWall = true;

		awardWeaponId = WeaponIds.Buster;
		weakWeaponId = WeaponIds.ShotgunIce;
		weakMaverickWeaponId = WeaponIds.ChillPenguin;

		weapon = new Weapon(WeaponIds.VelGGeneric, 101);

		netActorCreateId = NetActorCreateId.Velguarder;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		armorClass = ArmorClass.Light;
	}

	public override void update() {
		base.update();
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (shootPressed()) {
					changeState(getShootState());
				} else if (specialPressed()) {
					changeState(getShootState2());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new VelGPounceStartState());
				}
			} else if (state is MJump || state is MFall) {
			}
		}
	}

	public override string getMaverickPrefix() {
		return "velg";
	}

	public override float getRunSpeed() {
		return 135f;
	}

	public MaverickState getShootState() {
		return new VelGShootFireState();
	}

	public MaverickState getShootState2() {
		return new VelGShootIceState();
	}

	public override MaverickState[] strikerStates() {
		return [
			new VelGShootFireState(),
			new VelGShootIceState(),
			new VelGPounceStartState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		if (enemyDist > 50) {
			return [new VelGPounceStartState()];
		}
		return [
			getShootState2(),
			getShootState(),
			new VelGPounceStartState()
		];
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Pounce,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"velg_pounce" => MeleeIds.Pounce,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Pounce => new GenericMeleeProj(
				meleeWeapon, pos, ProjIds.VelGMelee, player,
				3, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}

}

#region weapons
public class VelGFireWeapon : Weapon {
	public static VelGFireWeapon netWeapon = new();
	public VelGFireWeapon() {
		index = (int)WeaponIds.VelGFire;
		killFeedIndex = 101;
	}
}

public class VelGIceWeapon : Weapon {
	public static VelGIceWeapon netWeapon = new();
	public VelGIceWeapon() {
		index = (int)WeaponIds.VelGIce;
		killFeedIndex = 101;
	}
}

public class VelGMeleeWeapon : Weapon {
	public static VelGMeleeWeapon netWeapon = new();
	public VelGMeleeWeapon() {
		index = (int)WeaponIds.VelGMelee;
		killFeedIndex = 101;
	}
}
#endregion

#region projectiles
public class VelGFireProj : Projectile {
	public VelGFireProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "velg_proj_fire", netId, player	
	) {
		weapon = VelGFireWeapon.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 1;
		vel = new Point(125 * xDir, 0);
		projId = (int)ProjIds.VelGFire;
		maxTime = 1f;
		vel.y = 200;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VelGFireProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (MathF.Abs(vel.y) < 600) vel.y -= Global.spf * 600;
		if (isUnderwater()) {
			destroySelf(disableRpc: true);
			return;
		}
	}
}

public class VelGIceProj : Projectile {
	public int type = 0;
	public VelGIceProj(
		Point pos, int xDir, int type,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "velg_proj_ice", netId, player	
	) {
		weapon = VelGIceWeapon.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 1;
		this.type = type;
		if (type == 0) vel = new Point(200 * xDir, -200);	
		if (type == 1) vel = new Point(150 * xDir, -200);
		if (type == 2) vel = new Point(250 * xDir, -200);
		projId = (int)ProjIds.VelGIce;
		maxTime = 0.75f;
		useGravity = true;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new VelGIceProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}
}
#endregion

#region states
public class VelGShootFireState : MaverickState {
	float shootTime;
	public Velguarder Velguader = null!;
	public VelGShootFireState() : base("shoot2") {

	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		Velguader = maverick as Velguarder ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		if (Velguader == null) return;

		if (maverick.frameIndex == 1) {
			var poi = maverick.getFirstPOIOrDefault();
			shootTime += Global.spf;
			if (shootTime > 0.05f) {
				shootTime = 0;
				maverick.playSound("fireWave", sendRpc: true);
				new VelGFireProj(
					poi, maverick.xDir, Velguader,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}
}

public class VelGShootIceState : MaverickState {
	bool shot;
	int index = 0;
	public Velguarder Velguader = null!;
	public VelGShootIceState() : base("shoot") {
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		Velguader = maverick as Velguarder ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		if (Velguader == null) return;

		maverick.turnToInput(input, player);

		if (maverick.frameIndex % 2 == 1) {
			if (!shot) {
				shot = true;
				index++;
				if (player.input.isHeld(Control.Up, player))
					Proj(0);
				else if (player.input.isHeld(Control.Right, player) || player.input.isHeld(Control.Left, player)) 
					Proj(2);
				else
				 	Proj(1);
			}
		} else {
			shot = false;
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}
	public void Proj(int type) {
		var poi = Velguader.getFirstPOIOrDefault();
		new VelGIceProj(
			poi, maverick.xDir, type, Velguader, 
			player, player.getNextActorNetId(), rpc: true
		);
	}
}

public class VelGPounceStartState : MaverickState {
	public VelGPounceStartState() : base("jump_start") {
	}

	public override void update() {
		base.update();

		if (maverick.isAnimOver()) {
			maverick.vel.y = -maverick.getJumpPower() * 0.75f;
			maverick.changeState(new VelGPounceState());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
	}
}

public class VelGPounceState : MaverickState {
	public VelGPounceState() : base("pounce") {
	}

	public override void update() {
		base.update();

		if (maverick.grounded && stateTime > 0.05f) {
			landingCode();
			return;
		}

		wallClimbCode();

		if (Global.level.checkTerrainCollisionOnce(maverick, 0, -1) != null && maverick.vel.y < 0) {
			maverick.vel.y = 0;
		}

		maverick.move(new Point(maverick.xDir * 300, 0));
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
	}
}

public class VelGDeathAnim : Anim {
	Player player;
	public VelGDeathAnim(
		Point pos, int xDir, Player player, ushort? netId = null,
		bool sendRpc = false, bool ownedByLocalPlayer = true
	) : base(
		pos, "velg_anim_die", xDir, netId, false, sendRpc, ownedByLocalPlayer
	) {
		vel = new Point(-xDir * 150, -150);
		ttl = 0.5f;
		useGravity = true;
		collider.wallOnly = true;
		this.player = player;
	}

	public override void update() {
		base.update();
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (other.gameObject is Wall && time > 0.1f) {
			destroySelf();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		var dieEffect = new ExplodeDieEffect(
			player, getCenterPos(), getCenterPos(), "empty", 1, zIndex, false, 25, 0.75f, false
		);
		Global.level.addEffect(dieEffect);
		Anim.createGibEffect(
			"velg_pieces", getCenterPos(), player,
			GibPattern.SemiCircle, randVelStart: 150, randVelEnd: 200, sendRpc: true
		);
	}
}

#endregion
