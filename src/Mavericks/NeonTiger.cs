using System;
using System.Collections.Generic;
namespace MMXOnline;

public class NeonTiger : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.NeonTGeneric, 156); }

	public const float pounceSpeed = 275;
	public const float wallPounceSpeed = 325;
	public int shootNum;
	public bool isDashing;
	public int shootTimes;
	public float dashAICooldown;
	public NeonTiger(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(NeonTClawState), new(20, true) },
			//{ typeof(MShoot), new(20, true) },
			//{ typeof(NeonTDashClawState), new(30, true) }
		};
		weapon = getWeapon();

		canClimbWall = true;

		awardWeaponId = WeaponIds.RaySplasher;
		weakWeaponId = WeaponIds.SpinningBlade;
		weakMaverickWeaponId = WeaponIds.CrushCrawfish;

		netActorCreateId = NetActorCreateId.NeonTiger;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}
		gameMavs = GameMavs.X3;
	}

	public override void update() {
		base.update();
		Helpers.decrementFrames(ref dashAICooldown);
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isHeld(Control.Special1, player)) {
					changeState(new NeonTShootState());
				} else if (input.isPressed(Control.Dash, player)) {
						changeState(new NeonTDashState());
				} else if (input.isHeld(Control.Shoot, player)) {
					changeState(new NeonTClawState(false));
				}
			} else if (state is MJump || state is MFall || state is MWallKick || state is NeonTPounceState) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new NeonTAirClawState());
				}
			} else if (state is MWallSlide wallSlide) {
				if (input.isHeld(Control.Shoot, player)) {
					changeState(new NeonTWallClawState(wallSlide.cloneLeaveOff()));
				} else if (input.isHeld(Control.Special1, player)) {
					changeState(new NeonTWallShootState(wallSlide.cloneLeaveOff()));
				}
			}
		}
	}

	public override string getMaverickPrefix() {
		return "neont";
	}

	public override float getRunSpeed() {
		return 200f;
	}

	public override float getDashSpeed() {
		return 1f;
	}

	public override MaverickState[] strikerStates() {
		return [
			new NeonTClawState(false),
			new NeonTShootState(),
			new NeonTDashClawState()
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		List<MaverickState> aiStates = [];
		if (state is MFall or MJump) {
			aiStates.Add(new NeonTAirClawState());
		}
		if (enemyDist >= 70 && shootTimes < 4) {
			aiStates.Add(new NeonTShootState());
		}
		if (enemyDist <= 40) {
			aiStates.Add(new NeonTClawState(isSecond: false));
			aiStates.Add(new NeonTDashClawState());
			shootTimes = 0;
		}
		if (dashAICooldown <= 0) {
			dashAICooldown = 90;
			aiStates.Add(new NeonTDashState());
		}
		
		return aiStates.ToArray();
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Slash,
		Slash2,
		JumpSlash,
		DashSlash,
		WallSlash,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"neont_slash" => MeleeIds.Slash,
			"neont_slash2" => MeleeIds.Slash2,
			"neont_jump_slash" => MeleeIds.JumpSlash,
			"neont_dash_slash" => MeleeIds.DashSlash,
			"neont_wall_slash" => MeleeIds.WallSlash,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Slash => new GenericMeleeProj(
				weapon, pos, ProjIds.NeonTClaw, player,
				2, 0, 12, addToLevel: addToLevel
			),
			MeleeIds.Slash2 => new GenericMeleeProj(
				weapon, pos, ProjIds.NeonTClaw2, player,
				2, Global.halfFlinch, 15, addToLevel: addToLevel
			),
			MeleeIds.JumpSlash => new GenericMeleeProj(
				weapon, pos, ProjIds.NeonTClawAir, player,
				3, Global.defFlinch, 15, addToLevel: addToLevel
			),
			MeleeIds.DashSlash => new GenericMeleeProj(
				weapon, pos, ProjIds.NeonTClawDash, player,
				3, Global.halfFlinch, 15, addToLevel: addToLevel
			),
			MeleeIds.WallSlash => new GenericMeleeProj(
				weapon, pos, ProjIds.NeonTClawWall, player,
				3, 0, 15, addToLevel: addToLevel
			),
			_ => null
		};
	}
}

public class NeonTRaySplasherProj : Projectile {
	public int[] randomType0 = { 0, 30, -30 };
	public int[] randomType1 = { -120, -80, -160 };
	public int[] randomType2 = { 60, 90, 140 };
	public int type = 0;
	public NeonTRaySplasherProj(
		Point pos, int xDir, int type,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "neont_projectile", netId, player	
	) {
		weapon = NeonTiger.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 1;
		this.type = type;
		projId = (int)ProjIds.NeonTRaySplasher;
		maxTime = 0.8f;
		fadeSprite = "raysplasher_fade";
		fadeOnAutoDestroy = true;
		int randomType0F = randomType0[Helpers.randomRange(0,2)];
		int randomType1F = randomType1[Helpers.randomRange(0,2)];
		int randomType2F = randomType2[Helpers.randomRange(0,2)];
		if (type == 0) vel = new Point(250 * xDir, randomType0F);	
		if (type == 1) vel = new Point(250 * xDir, randomType1F);
		if (type == 2) vel = new Point(250 * xDir, randomType2F);

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new NeonTRaySplasherProj(
			args.pos, args.xDir, args.extraData[0],
			args.owner, args.player, args.netId
		);
	}
}
public class NeonTShootState : MaverickState {
	public bool once2;
	public NeonTiger ShiningTigerd = null!;
	public NeonTShootState() : base("shoot") {
		
	}
	public override void update() {
		base.update();
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos != null) {
			if (stateTime > 4f/60f && !once2) {
				once2 = true;
				new Anim(shootPos.Value, "neont_projectile_start", maverick.xDir,
				player.getNextActorNetId(), true, sendRpc: true);
			}
			if (!isAI) {
				if (!once && stateTime > 16f / 60f) {
					once = true;
					maverick.playSound("neontRaySplasher", sendRpc: true);
					shotProj();
				}
			}
			if (isAI && ShiningTigerd.shootTimes < 4 && stateTime > 16f / 60f) {
				shotProj();
				maverick.playSound("neontRaySplasher", sendRpc: true);
				maverick.changeState(new NeonTShootState(), true);
				ShiningTigerd.shootTimes++;
			}
		}
		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
	public void shotProj() {
		bool upHeld = player.input.isHeld(Control.Up, player);
		bool downHeld = player.input.isHeld(Control.Down, player);
		if (downHeld) {
			SplasherProj(2);
		} else if (upHeld) {
			SplasherProj(1);
		} else {
			SplasherProj(0);
		}
	}
	public void SplasherProj(int type) {
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos != null) {
			new NeonTRaySplasherProj(
				shootPos.Value, maverick.xDir, type, ShiningTigerd,
				player, player.getNextActorNetId(), rpc: true
			);
		}
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ShiningTigerd = maverick as NeonTiger ?? throw new NullReferenceException();
	}

}
public class NeonTWallShootState : MaverickState {
	MaverickState prevState;
	public bool once2;
	public NeonTiger ShiningTigerd = null!;
	public NeonTWallShootState(MaverickState prevState) : base("wall_shoot") {
		useGravity = false;
		this.prevState = prevState;
	}

	public override void update() {
		base.update();
		bool upHeld = player.input.isHeld(Control.Up, player);
		bool downHeld = player.input.isHeld(Control.Down, player);
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos != null) {
			if (stateTime > 4f/60f && !once2) {
				once2 = true;
				new Anim(shootPos.Value, "neont_projectile_start", maverick.xDir,
				player.getNextActorNetId(), true, sendRpc: true);
			}
			if (!once && stateTime > 16f/60f) {
				once = true;
				maverick.playSound("neontRaySplasher", sendRpc: true);
				if (downHeld) {
					SplasherProj(2);
				} else if (upHeld) {
					SplasherProj(1);
				} else {
					SplasherProj(0);
				}
			}
		}
		if (maverick.isAnimOver()) {
			maverick.changeState(prevState, true);
		}
	}
	public void SplasherProj(int type) {
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos != null) {
			new NeonTRaySplasherProj(
				shootPos.Value, maverick.xDir*-1, type, ShiningTigerd,
				player, player.getNextActorNetId(), rpc: true
			);
		}
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ShiningTigerd = maverick as NeonTiger ?? throw new NullReferenceException();
	}
}

public class NeonTWallClawState : MaverickState {
	MaverickState prevState;
	public NeonTWallClawState(MaverickState prevState) : base("wall_slash") {
		useGravity = false;
		this.prevState = prevState;
		enterSound = "neontSlash";
	}

	public override void update() {
		base.update();
		if (maverick.isAnimOver()) {
			maverick.changeState(prevState, true);
		}
	}
}

public class NeonTClawState : MaverickState {
	bool isSecond;
	bool shootPressed;
	public NeonTClawState(bool isSecond) : base(isSecond ? "slash2" : "slash") {
		this.isSecond = isSecond;
		exitOnAnimEnd = true;
		canEnterSelf = true;
		enterSound = "neontSlash";
	}

	public override void update() {
		base.update();

		shootPressed = shootPressed || input.isPressed(Control.Shoot, player);
		if (!isSecond && (shootPressed || isAI) && maverick.frameIndex > 2) {
			maverick.sprite.restart();
			maverick.changeState(new NeonTClawState(true), true);
			return;
		}

		if (!isSecond && input.isHeld(Control.Shoot, player) && maverick.frameIndex > 2 && maverick.frameTime >= 5) {
			maverick.sprite.restart();
			maverick.changeState(new NeonTClawState(false), true);
			return;
		}
	}
}

public class NeonTAirClawState : MaverickState {
	bool wasPounce;
	bool wasWallPounce;
	public NeonTiger ShiningTigerd = null!;
	public NeonTAirClawState() : base("jump_slash") {
		exitOnAnimEnd = true;
		enterSound = "neontSlash";
	}

	public override void update() {
		base.update();
		airCode(canMove: !wasPounce);
		if (wasPounce) {
			maverick.move(new Point(maverick.xDir * (wasWallPounce ? NeonTiger.wallPounceSpeed : NeonTiger.pounceSpeed), 0));
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ShiningTigerd = maverick as NeonTiger ?? throw new NullReferenceException();
		if (oldState is NeonTPounceState || ShiningTigerd.isDashing) {
			wasPounce = true;
			wasWallPounce = (oldState as NeonTPounceState)?.isWallPounce ?? false;
		}
	}
}

public class NeonTDashClawState : MaverickState {
	float velX;
	public NeonTDashClawState() : base("dash_slash") {
		exitOnAnimEnd = true;
		enterSound = "neontSlash";
	}

	public override void update() {
		base.update();
		maverick.move(new Point(velX, 0));
		velX = Helpers.lerp(velX, 0, Global.spf * 5);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		velX = maverick.xDir * 250;
	}
}

public class NeonTDashState : MaverickState {
	float dustTime;
	public NeonTiger ShiningTigerd = null!;
	public NeonTDashState() : base("dash") {
		enterSound = "dashX3";
		normalCtrl = true;
		attackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		float enemyDist = 300;
		if (maverick.target != null) {
			enemyDist = MathF.Abs(maverick.target.pos.x - maverick.pos.x);
		}
		Helpers.decrementTime(ref dustTime);
		if (dustTime == 0) {
			new Anim(maverick.pos.addxy(-maverick.xDir * 27, 0), "dust", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
			dustTime = 0.075f;
		}

		if (input.isPressed(Control.Jump, player)) {
			maverick.changeState(new MJumpStart());
			return;
		}

		var move = new Point(250 * maverick.xDir, 0);

		var hitGround = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 5, 20);
		if (hitGround == null) {
			maverick.changeState(new MIdle());
			return;
		}

		var hitWall = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 2, -5);
		if (hitWall?.isSideWallHit() == true) {
			maverick.changeState(new MIdle());
			return;
		}
		maverick.move(move);
		if (input.isPressed(Control.Shoot, player) || (isAI && enemyDist <= 10)) {
			maverick.changeState(new NeonTDashClawState());
		} else if (isHoldStateOver(0.1f, 0.6f, 50f / 60f, Control.Dash)) {
			maverick.changeToIdleOrFall();
		} 
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ShiningTigerd = maverick as NeonTiger ?? throw new NullReferenceException();
	}
}

public class NeonTPounceState : MaverickState {
	public bool isWallPounce;
	public NeonTPounceState() : base("fall") {
		enterSound = "jump";
		normalCtrl = true;
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();

		if (maverick.grounded) {
			landingCode();
			return;
		}

		if (stateTime > 0.25f) {
			wallClimbCode();
		}

		if (Global.level.checkTerrainCollisionOnce(maverick, 0, -1) != null && maverick.vel.y < 0) {
			maverick.vel.y = 0;
		}

		maverick.move(new Point(maverick.xDir * (isWallPounce ? NeonTiger.wallPounceSpeed : NeonTiger.pounceSpeed), 0));
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		if (oldState is MWallSlide) {
			isWallPounce = true;
		}
	}
}
