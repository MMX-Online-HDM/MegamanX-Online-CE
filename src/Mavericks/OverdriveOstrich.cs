using System;
using System.Collections.Generic;

namespace MMXOnline;

public class OverdriveOstrich : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.OverdriveOGeneric, 149); }

	public float dashDist;
	public float baseSpeed = 0;
	public float accSpeed;
	public int lastDirX;
	public float crystalizeCooldown;

	public OverdriveOstrich(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(OverdriveOShootState), new(45, true) },
			{ typeof(OverdriveOShoot2State), new(2 * 60, true, true) },
			{ typeof(OverdriveOJumpKickState), new(60, true, true) }
		};

		weapon = getWeapon();

		awardWeaponId = WeaponIds.SonicSlicer;
		weakWeaponId = WeaponIds.CrystalHunter;
		weakMaverickWeaponId = WeaponIds.CrystalSnail;

		netActorCreateId = NetActorCreateId.OverdriveOstrich;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}
		gameMavs = GameMavs.X2;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		subtractTargetDistance = 50;
		Helpers.decrementTime(ref crystalizeCooldown);

		if (lastDirX != xDir) {
			accSpeed = 0;
			dashDist = 0;
		}
		lastDirX = xDir;

		if (state is MRun) {
			dashDist += accSpeed * Global.spf;
			accSpeed += Global.spf * 800;
			if (accSpeed > 300) accSpeed = 300;
		}

		// Momentum carrying states
		if (state is OverdriveOShootState or
			OverdriveOShoot2State or MRun or MFall or MJumpStart or MLand
		) {
			int inputDir = input.getXDir(player);
			if (inputDir != xDir && (state is not MRun || inputDir == 0)) {
				accSpeed = Helpers.lerp(accSpeed, 0, Global.spf * 5);
			}
		} else {
			accSpeed = Helpers.lerp(accSpeed, 0, Global.spf * 5);
		}

		if (state is OverdriveOShootState || state is OverdriveOShoot2State) {
			var moveAmount = new Point(getRunSpeed() * xDir, 0);
			if (moveAmount.magnitude > 0) {
				move(moveAmount);
			}
		}

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new OverdriveOShootState());
				} else if (input.isPressed(Control.Special1, player)) {
					changeState(new OverdriveOShoot2State());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new OverdriveOJumpKickState());
				}
			} else if (state is MJump || state is MFall) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new OverdriveOShootState());
				} else if (input.isPressed(Control.Special1, player)) {
					changeState(new OverdriveOShoot2State());
				}
			}
		}
	}

	public float dustSpeed { get { return 300; } }
	public float skidSpeed { get { return 300; } }
	public float damageSpeed { get { return 299; } }
	public float wallSkidSpeed { get { return 300; } }

	public override float getRunSpeed() {
		float speed = baseSpeed + accSpeed;
		if (state is MRun or OverdriveOSkidState or OverdriveOShootState or OverdriveOShoot2State)
		return speed * getRunDebuffs();
		else
		return Math.Max(100, speed) * getRunDebuffs();
	}

	public override string getMaverickPrefix() {
		return "overdriveo";
	}

	public override MaverickState[] strikerStates() {
		return [
			new OverdriveOShootState(),
			new OverdriveOShoot2State(),
			new OverdriveOJumpKickState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		List<MaverickState> aiStates = [
			new OverdriveOShootState(),
			new OverdriveOJumpKickState()
		];
		if (enemyDist <= 70) {
			aiStates.Add(new OverdriveOShoot2State());
		}
		return aiStates.ToArray();
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Skip,
		Run,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"overdriveo_skip" or "overdriveo_skip2" => MeleeIds.Skip,
			"overdriveo_run" => MeleeIds.Skip,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Skip => new GenericMeleeProj(
				weapon, pos, ProjIds.OverdriveOMelee, player,
				3, Global.defFlinch, 60, addToLevel: addToLevel
			),
			MeleeIds.Run => new GenericMeleeProj(
				weapon, pos, ProjIds.OverdriveOMelee, player,
				2, Global.defFlinch, 60, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (sprite.name.EndsWith("_run")) {
			if (MathF.Abs(deltaPos.x) >= damageSpeed * Global.spf) {
				proj.damager.damage = 2;
			} else {
				proj.damager.damage = 0;
			}
		}
	}
}

public class OverdriveOSonicSlicerProj : Projectile {
	bool once;
	public OverdriveOSonicSlicerProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "overdriveo_slicer_start", netId, player
	) {
		weapon = OverdriveOstrich.getWeapon();
		damager.damage = 3;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.OverdriveOSonicSlicer;
		maxTime = 0.5f;
		destroyOnHit = false;
		fadeSprite = "buster4_fade";
		fadeOnAutoDestroy = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new OverdriveOSonicSlicerProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		if (sprite.isAnimOver() && !once) {
			once = true;
			changeSprite("overdriveo_slicer", true);
			vel = new Point(xDir * 350, 0);
		}
	}
}
public class OOstrichMState : MaverickState {
	public OverdriveOstrich sonicOstreague = null!;

	public OOstrichMState(
		string sprite, string transitionSprite = ""
	) : base(
		sprite, transitionSprite
	) {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		sonicOstreague = maverick as OverdriveOstrich ?? throw new NullReferenceException();
	}
}

public class OverdriveOShootState : OOstrichMState {
	bool shotOnce;
	public OverdriveOShootState() : base("attack") {
	}
	public override void update() {
		base.update();

		//maverick.turnToInput(input, player);
		//maverick.move(new Point(maverick.getRunSpeed() * maverick.xDir, 0), true);

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			maverick.playSound("overdriveoShoot", sendRpc: true);
			new OverdriveOSonicSlicerProj(
				shootPos.Value, maverick.xDir, sonicOstreague,
				player, player.getNextActorNetId(), rpc: true
			);
			//proj.vel.x += maverick.getRunSpeed() * 3 * proj.xDir;
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleRunOrFall();
		}
	}
}

public class OverdriveOSonicSlicerUpProj : Projectile {
	public float displacent;
	public float offset;
	public Point dest;
	public bool fall;

	public OverdriveOSonicSlicerUpProj(
		Point pos, int xDir, int num, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "overdriveo_slicer_vertical", netId, player
	) {
		weapon = OverdriveOstrich.getWeapon();
		damager.damage = 3;
		damager.hitCooldown = 15;
		damager.flinch = Global.defFlinch;
		fadeSprite = "sonicslicer_charged_fade";
		maxTime = 1;
		projId = (int)ProjIds.OverdriveOSonicSlicerUp;
		destroyOnHit = false;
		fadeOnAutoDestroy = true;
		if (num == 0) dest = new Point(-90, -100);
		if (num == 1) dest = new Point(-45, -100);
		if (num == 2) dest = new Point(-0, -100);
		if (num == 3) dest = new Point(45, -100);
		if (num == 4) dest = new Point(90, -100);

		vel.y = -500;
		useGravity = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)num);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new OverdriveOSonicSlicerUpProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		vel.y += Global.speedMul * getGravity();
		if (!fall) {
			displacent = Helpers.lerp(displacent, dest.x, Global.spf * 10);
			incPos(new Point(displacent - offset, 0));
			offset = displacent;

			if (vel.y > 0) {
				fall = true;
				yDir = -1;
			}
		}
	}
}

public class OverdriveOShoot2State : OOstrichMState {
	bool shotOnce;
	public OverdriveOShoot2State() : base("attack2") {
	}

	public override void update() {
		base.update();

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			maverick.playSound("overdriveoShoot2", sendRpc: true);
			for (int i = 0; i < 5; i++) {
				new OverdriveOSonicSlicerUpProj(
					shootPos.Value, 1, i, sonicOstreague, player,
					player.getNextActorNetId(), rpc: true
				);
			}
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleRunOrFall();
		}
	}
}

public class OverdriveOJumpKickState : OOstrichMState {
	public OverdriveOJumpKickState() : base("skip") {
	}

	public override void update() {
		base.update();

		if (maverick.grounded && stateTime > 0.05f) {
			landingCode();
			return;
		}

		if (Global.level.checkTerrainCollisionOnce(maverick, 0, -1) != null && maverick.vel.y < 0) {
			maverick.vel.y = 0;
		}

		maverick.move(new Point(maverick.xDir * 300, 0));
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.vel.y = -maverick.getJumpPower() * 0.75f;
	}
}

public class OverdriveOSkidState : OOstrichMState {
	float dustTime;
	public OverdriveOSkidState() : base("skid") {
		enterSound = "overdriveoSkid";
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();

		sonicOstreague.accSpeed = Helpers.lerp(sonicOstreague.accSpeed, 0, Global.spf * 5);

		Helpers.decrementTime(ref dustTime);
		if (dustTime == 0) {
			new Anim(maverick.pos.addxy(maverick.xDir * 10, 0), "dust", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true) { frameSpeed = 1.5f };
			dustTime = 0.05f;
		}

		var inputDir = input.getInputDir(player);
		if (inputDir.x == -sonicOstreague.xDir) maverick.frameSpeed = 1.5f;
		else maverick.frameSpeed = 1;

		var move = new Point(maverick.getRunSpeed() * maverick.xDir, 0);
		if (move.magnitude > 0) {
			maverick.move(move);
		}

		if (!once) {
			if (maverick.loopCount > 2) {
				maverick.changeSpriteFromName("skid_end", true);
				once = true;
			}
		} else {
			if (maverick.isAnimOver()) {
				maverick.changeToIdleOrFall();
			}
		}
	}
	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		sonicOstreague.accSpeed = 0;
	}
}

public class OverdriveOCrystalizedState : OOstrichMState {
	public OverdriveOCrystalizedState() : base("hurt_weakness") {
		enterSound = "crystalize";
		aiAttackCtrl = true;
		canBeCanceled = false;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
	}

	public override void update() {
		base.update();

		if (maverick.isAnimOver()) {
			Anim.createGibEffect("overdriveo_weakness_glass", maverick.getCenterPos(), player, sendRpc: true);
			maverick.playSound("freezebreak2", sendRpc: true);
			maverick.changeToIdleOrFall();
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		sonicOstreague.crystalizeCooldown = 2;
	}
}
