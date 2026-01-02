using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class MorphMoth : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.MorphMGeneric, 146); }

	public MorphMoth(
		Player player, Point pos, int xDir, ushort? netId,
		bool ownedByLocalPlayer, bool isHatch, bool sendRpc = false
	) : base(
		player, pos, xDir, netId, ownedByLocalPlayer,
		overrideState: isHatch ? new MorphMHatchState() : null
	) {
		stateCooldowns = new() {
			{ typeof(MorphMShoot), new(30, false, true) },
			{ typeof(MorphMShootAir), new(30, false, true) }
		};

		weapon = getWeapon();
		spriteToCollider["sweep"] = getDashCollider();

		canFly = true;

		awardWeaponId = WeaponIds.SilkShot;
		weakWeaponId = WeaponIds.SpeedBurner;
		weakMaverickWeaponId = WeaponIds.FlameStag;

		netActorCreateId = NetActorCreateId.MorphMoth;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		canFly = true;
		flyBarIndexes = (53, 42);
		maxFlyBar = 960;
		flyBar = 960;
		gameMavs = GameMavs.X2;
	}

	public override void update() {
		base.update();

		if (!isUnderwater()) {
			spriteFrameToSounds["morphm_fly/0"] = "morphmFlap";
			spriteFrameToSounds["morphm_fly/4"] = "morphmFlap";
			spriteFrameToSounds["morphm_fly/8"] = "morphmFlap";
			spriteFrameToSounds["morphm_fly_fall/0"] = "morphmFlap";
			spriteFrameToSounds["morphm_fly_fall/4"] = "morphmFlap";
			spriteFrameToSounds["morphm_fly_fall/8"] = "morphmFlap";
		} else {
			spriteFrameToSounds.Clear();
		}

		if (!ownedByLocalPlayer) return;

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new MorphMShoot());
				}
			} else if (state is MFly) {
				if (input.isPressed(Control.Dash, player)) {
					changeState(new MorphMSweepState(isStriker: false));
				} else if (input.isPressed(Control.Shoot, player)) {
					changeState(new MorphMShootAir());
				}
			}
		}
		if (controlMode == MaverickModeId.Striker) {
			if (player.input.isHeld(Control.Shoot, player)) {
				var mmw = player.weapons.FirstOrDefault(w => w is MorphMothWeapon mmw) as MorphMothWeapon;
				if (mmw != null) {
					bool wasCocoon = ownerChar?.currentMaverick == this;
					mmw.isMoth = false;
				}
			}
		}
	}

	public override string getMaverickPrefix() {
		return "morphm";
	}

	public override MaverickState[] strikerStates() {
		return [
			new MorphMShoot(),
			new MorphMAIJump(1),
			new MorphMAIJump(3),
			new MorphMAIJump(2),
			new MorphMAIJump(4),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		List<MaverickState> aiStates = [
		];
		if (grounded) {
			aiStates.Add(new MorphMAIJump(1));
			if (enemyDist <= 125) {
				aiStates.Add(new MorphMShoot());
			}
		} else if (!grounded) {
			aiStates.Add(new MorphMSweepState(isStriker: false));
			if (enemyDist <= 30) {
				aiStates.Add(new MorphMShootAir(3));
			}
			if (enemyDist > 140) {
				aiStates.Add(new MorphMShootAir(2));
			}
		}
		return aiStates.ToArray();
	}
}

public class MorphMBeamProj : Projectile {
	public Point endPos;
	public MorphMBeamProj(Weapon weapon, Point pos, Point endPos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 5, player, "morphm_beam", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.MorphMBeam;
		maxTime = 0.33f;
		setIndestructableProperties();

		var hits = Global.level.raycastAllSorted(pos, endPos, new List<Type>() { typeof(Wall) });
		var hit = hits.FirstOrDefault();
		if (hit != null) {
			endPos = hit.getHitPointSafe();
		}

		setEndPos(endPos);

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public void setEndPos(Point endPos) {
		this.endPos = endPos;
		float ang = pos.directionToNorm(endPos).angle;
		var points = new List<Point>();
		if (xDir == 1) {
			float sideY = 8 * Helpers.cosd(ang);
			float sideX = -8 * Helpers.sind(ang);
			points.Add(new Point(pos.x - sideX, pos.y - sideY));
			points.Add(new Point(endPos.x - sideX, endPos.y - sideY));
			points.Add(new Point(endPos.x + sideX, endPos.y + sideY));
			points.Add(new Point(pos.x + sideX, pos.y + sideY));
		} else {
			float sideY = 8 * Helpers.cosd(ang);
			float sideX = 8 * Helpers.sind(ang);
			points.Add(new Point(endPos.x - sideX, endPos.y + sideY));
			points.Add(new Point(endPos.x + sideX, endPos.y - sideY));
			points.Add(new Point(pos.x + sideX, pos.y - sideY));
			points.Add(new Point(pos.x - sideX, pos.y + sideY));
		}

		globalCollider = new Collider(points, true, null, false, false, 0, Point.zero);
	}

	public override void render(float x, float y) {
		var colors = new List<Color>()
		{
				new Color(49, 255, 255, 192),
				new Color(66, 40, 255, 192),
				new Color(49, 255, 33, 192),
				new Color(255, 255, 255, 192),
				new Color(255, 40, 33, 192),
				new Color(255, 40, 255, 192),
				new Color(255, 255, 33, 192),
			};

		if (MathF.Abs(pos.y - endPos.y) < 0.1f) DrawWrappers.DrawLine(pos.x, pos.y, endPos.x, endPos.y, colors[frameIndex], 16, ZIndex.Actor);
		else {
			var points = new List<Point>()
			{
					pos.addxy(-8, 0),
					endPos.addxy(-8, 0),
					endPos.addxy(8, 0),
					pos.addxy(8, 0),
				};
			DrawWrappers.DrawPolygon(points, colors[frameIndex], true, ZIndex.AboveFont);
		}
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();

		customData.AddRange(BitConverter.GetBytes(endPos.x));
		customData.AddRange(BitConverter.GetBytes(endPos.y));

		return customData;
	}
	public override void updateCustomActorNetData(byte[] data) {
		float endX = BitConverter.ToSingle(data[0..4], 0);
		float endY = BitConverter.ToSingle(data[4..8], 0);

		setEndPos(new Point(endX, endY));
	}
}

public class MorphMShoot : MaverickState {
	bool shotOnce;
	public MorphMShoot() : base("shoot_ground", "shoot_ground_start") {
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (!shotOnce) maverick.turnToInput(input, player);
		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			morphMothBeam(shootPos.Value, true);
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}
}

public class MorphMShootAir : MaverickState {
	bool shotOnce;
	public int stateAI;
	public MorphMShootAir(int stateAI = 1) : base("shoot", "shoot_start") {
		this.stateAI = stateAI;
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (!shotOnce) maverick.turnToInput(input, player);
		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			if (stateAI == 1) {
				morphMothBeam(shootPos.Value, false);
			}
			if (stateAI == 2) {
				maverick.playSound("morphmBeam", sendRpc: true);
				new MorphMBeamProj(
					maverick.weapon, shootPos.Value,
					maverick.getCenterPos().addxy(160 * maverick.xDir, 50),
					maverick.xDir, player, player.getNextActorNetId(), rpc: true
				);
			} else if (stateAI == 3) {
				morphMothBeam(shootPos.Value, false);
			} else if (stateAI == 4) {
				new MorphMBeamProj(
					maverick.weapon, shootPos.Value,
					maverick.getCenterPos().addxy(160 * maverick.xDir, -50),
					maverick.xDir, player, player.getNextActorNetId(), rpc: true
				);
				maverick.playSound("morphmBeam", sendRpc: true);
			}
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleFallOrFly();
		}
		if (isAI) {
			maverick.vel.y = 0;
			if (stateTime > 32f / 60f) {
				maverick.changeToIdleFallOrFly();
			}
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;
	}
}

public class MorphMPowderProj : Projectile {
	public float sparkleTime = 0;
	public MorphMPowderProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "morphm_sparkles", netId, player
	) {
		weapon = MorphMoth.getWeapon();
		damager.damage = 1;
		projId = (int)ProjIds.MorphMPowder;
		maxTime = 1f;
		vel = new Point(0, 100);
		healAmount = 1;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new MorphMPowderProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		sparkleTime += Global.spf;
		if (sparkleTime > 0.05) {
			sparkleTime = 0;
			new Anim(pos, "morphm_sparkles_fade", 1, null, true);
		}
	}
}

public class MorphMSweepState : MaverickState {
	public bool isStriker;

	public MorphMSweepState(bool isStriker) : base("sweep", "sweep_start") {
		this.isStriker = isStriker;
	}
	public MorphMoth? Moth = null;
	float shootTime;
	float xVel;
	float yVel;
	const float maxSpeed = 250;
	const float startSpeed = 50;
	public override void update() {
		base.update();
		if (Moth == null) return;

		Helpers.decrementTime(ref shootTime);
		if (isAI || input.isHeld(Control.Shoot, player)) {
			Helpers.decrementTime(ref maverick.ammo);
			if (shootTime == 0) {
				shootTime = isStriker ? 0.1f : 0.15f;
				Point shootPos = maverick.getFirstPOIOrDefault().addRand(10, 5);
				new MorphMPowderProj(
					shootPos, maverick.xDir, Moth,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (isStriker) {
			maverick.move(new Point(20*maverick.xDir, -20));
			if (stateTime > 60f / 60f) maverick.changeToIdleFallOrFly();
		}
		var inputDir = input.getInputDir(player);

		if (inputDir.x != 0 && MathF.Sign(inputDir.x) != maverick.xDir) {
			xVel = Math.Clamp(xVel + (inputDir.x * 1000 * Global.spf), -maxSpeed, maxSpeed);
			if (xVel < 0) maverick.xDir = -1;
			else if (xVel > 0) maverick.xDir = 1;
		} else {
			xVel = Math.Clamp(xVel + (maverick.xDir * 400 * Global.spf), -maxSpeed, maxSpeed);
		}

		if (inputDir.y == 0) {
			float yVelSign = MathF.Sign(yVel);
			if (yVelSign == 0) yVelSign = 1;
			yVel = Math.Clamp(yVel + (yVelSign * 1000 * Global.spf), -100, 100);
		} else {
			yVel = Math.Clamp(yVel + (inputDir.y * 1000 * Global.spf), -100, 100);
		}

		Point moveAmount = new Point(xVel, yVel);

		var hit = checkCollisionNormal(moveAmount.x * Global.spf, moveAmount.y * Global.spf);
		if (hit != null) {
			if (hit.getNormalSafe().isCeilingNormal()) {
				yVel *= -1;
			} else if (hit.getNormalSafe().isSideways()) {
				maverick.xDir *= -1;
				xVel *= -0.5f;
			} else {
				changeBack();
				return;
			}
		}

		moveAmount = new Point(xVel, yVel);
		maverick.move(moveAmount);

		if (maverick.ammo <= 0) changeBack();
		else if (!isAI && !input.isHeld(Control.Dash, player)) changeBack();
		else if (isAI && stateTime > 1) changeBack();
		else if (stateTime > 2.5f) changeBack();
	}

	public void changeBack() {
		maverick.changeState(new MFly(), true);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		Moth = maverick as MorphMoth ?? throw new NullReferenceException();
		maverick.useGravity = false;
		xVel = maverick.xDir * startSpeed;
	}
}

public class MorphMHatchState : MaverickState {
	float riseDist;
	public MorphMHatchState() : base("fly") {
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();

		if (maverick.grounded) {
			maverick.grounded = false;
			maverick.incPos(new Point(0, -5));
		}
		maverick.move(new Point(0, -75));
		riseDist += Global.spf * 75;
		if (riseDist > 37.5f) {
			maverick.changeState(new MFly());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMovingS();
		maverick.useGravity = false;
	}
}
public class MorphMAIJump : MaverickState {
	public int stateAI;
	public MorphMAIJump(int stateAI) : base("jump", "jump_start") {
		this.stateAI = stateAI;
	}

	public override void update() {
		base.update();
		if (stateAI == 1 && stateTime > 24f / 60f) {
			maverick.changeState(new MorphMSweepState(isStriker: false));
		} else if (stateAI == 2 && stateTime > 8f / 60f) {
			maverick.changeState(new MorphMShootAir(2));			
		} else if (stateAI == 3 && stateTime > 8f / 60f) {
			maverick.changeState(new MorphMShootAir(3));			
		} else if (stateAI == 4 && stateTime > 8f / 60f) {
			maverick.changeState(new MorphMShootAir(4));			
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.vel.y = -maverick.getJumpPower() * 1.25f;
	}
}
