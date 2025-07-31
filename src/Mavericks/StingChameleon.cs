using System;
using System.Collections.Generic;

namespace MMXOnline;

public class StingChameleon : Maverick {
	public StingCStingWeapon stingWeapon = new StingCStingWeapon();
	public StingCTongueWeapon tongueWeapon;
	public StingCSpikeWeapon specialWeapon = new StingCSpikeWeapon();
	public bool uncloakSoundPlayed;
	public float invisibleCooldown;
	public const float maxInvisibleCooldown = 2;
	public bool isInvisible;
	public float cloakTransitionTime;
	public float uncloakTransitionTime;

	public StingChameleon(
		Player player, Point pos, Point destPos, int xDir, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		tongueWeapon = new StingCTongueWeapon();
		stateCooldowns = new() {
			{ typeof(MShoot), new(45, true) },
			{ typeof(StingCTongueState), new(60, true) },
			{ typeof(StingCClimbTongueState), new(60, true) },
			{ typeof(StingCJumpAI), new(2 * 60) },
			{ typeof(StingCHangState), new(2 * 60) },
			{ typeof(StingCClingShootState), new(30) }
		};

		weapon = new Weapon(WeaponIds.StingCGeneric, 98);

		canClimb = true;
		//invisibleShader = Helpers.cloneShaderSafe("invisible");

		awardWeaponId = WeaponIds.ChameleonSting;
		weakWeaponId = WeaponIds.BoomerangCutter;
		weakMaverickWeaponId = WeaponIds.BoomerangKuwanger;

		netActorCreateId = NetActorCreateId.StingChameleon;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		usesAmmo = true;
		canHealAmmo = true;
		ammo = 32;
		maxAmmo = 32;
		grayAmmoLevel = 8;
		barIndexes = (57, 46);
	}

	public bool isCloakTransition() {
		return cloakTransitionTime > 0 || uncloakTransitionTime > 0;
	}

	public override void update() {
		base.update();

		Helpers.decrementTime(ref invisibleCooldown);

		if (!isCloakTransition()) {
			if (isInvisible) {
				drainAmmo(4);
				if (ammo <= 0) {
					uncloakTransitionTime = 1;
					playSound("76stingcCloak", sendRpc: true);
				}
			} else {
				rechargeAmmo(1);
			}
		}

		if (uncloakTransitionTime > 0) {
			Helpers.decrementTime(ref uncloakTransitionTime);
			alpha = Helpers.clamp01(1 - uncloakTransitionTime);
			if (uncloakTransitionTime == 0) {
				isInvisible = false;
			}
		} else if (cloakTransitionTime > 0) {
			Helpers.decrementTime(ref cloakTransitionTime);
			alpha = Helpers.clamp01(cloakTransitionTime);
		}

		if (aiBehavior == MaverickAIBehavior.Control && !isCloakTransition()) {
			if (state is MIdle or MRun or MLand) {
				if (shootPressed() && !isInvisible) {
					var inputDir = input.getInputDir(player);
					int type = 0;
					if (inputDir.y == -1 && inputDir.x == 0) type = 2;
					if (inputDir.y == -1 && inputDir.x != 0) type = 1;
					changeState(new StingCTongueState(type));
				} else if (specialPressed() && !isInvisible) {
					changeState(getShootState(false));
				} else if (input.isPressed(Control.Dash, player)) {
					cloakOrUncloak();
				}
			} else if (state is MJump || state is MFall) {
				if (input.isHeld(Control.Special1, player) && !isInvisible) {
					var hit = Global.level.raycast(pos, pos.addxy(0, -105), new List<Type>() { typeof(Wall) });
					if (vel.y < 100 && hit?.gameObject is Wall wall && !wall.topWall) {
						changeState(new StingCHangState(hit.getHitPointSafe().y));
					}
				}
			} else if (state is StingCClimb) {
				if (input.isPressed(Control.Shoot, player) && !isInvisible) {
					var inputDir = input.getInputDir(player);
					changeState(new StingCClimbTongueState(inputDir));
				} else if (input.isPressed(Control.Special1, player) && !isInvisible) {
					changeState(new StingCClingShootState());
				} else if (input.isPressed(Control.Dash, player)) {
					cloakOrUncloak();
				}
			}
		}
	}

	public void decloak() {
		isInvisible = false;
		cloakTransitionTime = 0;
		uncloakTransitionTime = 0;
		alpha = 1;
	}

	public void cloakOrUncloak() {
		if (isCloakTransition()) return;
		if (!isInvisible) {
			if (ammo >= 8) {
				deductAmmo(8);
				isInvisible = true;
				cloakTransitionTime = 1;
				playSound("76stingcCloak", sendRpc: true);
			}
		} else {
			uncloakTransitionTime = 1;
			playSound("76stingcCloak", sendRpc: true);
		}
	}

	public override string getMaverickPrefix() {
		return "stingc";
	}

	public MaverickState getShootState(bool isAI) {
		var shootState = new MShoot((Point pos, int xDir) => {
			new StingCStingProj(pos, xDir, 3, this, player, player.getNextActorNetId(), rpc: true);
			new StingCStingProj(pos, xDir, 4, this, player, player.getNextActorNetId(), rpc: true);
			new StingCStingProj(pos, xDir, 5, this, player, player.getNextActorNetId(), rpc: true);
		}, "stingcSting");
		if (isAI) {
			shootState.consecutiveData = new MaverickStateConsecutiveData(0, 2);
		}
		return shootState;
	}

	public override MaverickState[] strikerStates() {
		return [
			new StingCTongueState(0),
			new StingCTongueState(1),
			new StingCJumpAI(),
			getShootState(true)
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		List<MaverickState> aiStates = [
			getShootState(isAI: false)
		];
		if (enemyDist <= 125) {
			aiStates.Add(new StingCTongueState(0));
		}
		if (Helpers.randomRange(0, 10) == 0 && grounded) {
			aiStates.Add(new StingCJumpAI());
		}
		return aiStates.ToArray();
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Tongue,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"stingc_tongue" or "stingc_tongue2" or "stingc_tongue3" or
			"stingc_cling_tongue" or "stingc_cling_tongue2" or 
			"stingc_cling_tongue3" or "stingc_cling_tongue4" or
			"stingc_cling_tongue5"  => MeleeIds.Tongue,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Tongue => new GenericMeleeProj(
				tongueWeapon, pos, ProjIds.StingCTongue, player,
				4, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}

	/*
	public override List<Shader> getShaders()
	{
		List<Shader> shaders = new List<Shader>();

		// alpha float doesn't work if one or more shaders exist. So need to use the invisible shader instead
		if (alpha < 1)
		{
			invisibleShader?.SetUniform("alpha", alpha);
			shaders.Add(invisibleShader);
		}
		return shaders;
	}
	*/

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();
		customData.Add((byte)(isInvisible ? 1 : 0));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		base.updateCustomActorNetData(data);
		data = data[Maverick.CustomNetDataLength..];

		isInvisible = (data[0] == 1);
	}
}

#region weapons
public class StingCStingWeapon : Weapon {
	public static StingCStingWeapon netWeapon = new();
	public StingCStingWeapon() {
		index = (int)WeaponIds.StingCSting;
		killFeedIndex = 98;
	}
}

public class StingCTongueWeapon : Weapon {
	public static StingCTongueWeapon netWeapon = new();
	public StingCTongueWeapon() {
		index = (int)WeaponIds.StingCTongue;
		killFeedIndex = 98;
	}
}

public class StingCSpikeWeapon : Weapon {
	public static StingCSpikeWeapon netWeapon = new();
	public StingCSpikeWeapon() {
		index = (int)WeaponIds.StingCSpike;
		killFeedIndex = 98;
	}
}
#endregion

#region projectiles
public class StingCStingProj : Projectile {
	public StingCStingProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "stingc_proj_csting", netId, player
	) {
		weapon = StingCStingWeapon.netWeapon;
		damager.damage = 2;
		projId = (int)ProjIds.StingCSting;
		maxTime = 0.75f;

		frameSpeed = 0;
		if (type == 0) {
			vel = new Point(0, 250);
		}
		if (type == 1) {
			frameIndex = 1;
			vel = new Point(125 * xDir, 250);
		} else if (type == 2) {
			frameIndex = 2;
			vel = new Point(250 * xDir, 250);
		} else if (type == 3) {
			frameIndex = 3;
			vel = new Point(250 * xDir, 100);
		} else if (type == 4) {
			frameIndex = 4;
			vel = new Point(250 * xDir, 0);
		} else if (type == 5) {
			frameIndex = 5;
			vel = new Point(250 * xDir, -100);
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new StingCStingProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
	}
}

public class StingCSpikeProj : Projectile {
	public StingCSpikeProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "stingc_proj_spike", netId, player	
	) {
		weapon = StingCSpikeWeapon.netWeapon;
		damager.damage = 2;
		damager.flinch = Global.defFlinch;
		projId = (int)ProjIds.StingCSpike;
		maxTime = 0.75f;
		useGravity = true;
		vel.y = 50;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new StingCSpikeProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
#endregion

#region states
public class StingCClimb : MaverickState {
	public StingCClimb() : base("climb") {
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		maverick.useGravity = false;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.frameSpeed = 1;
		maverick.useGravity = true;
	}

	public override void update() {
		base.update();
		maverick.stopMoving();
		if (inTransition()) {
			return;
		}

		maverick.frameSpeed = 0;
		Point oldPos = maverick.pos;
		if (input.isHeld(Control.Up, player)) {
			maverick.move(new Point(0, -75));
			maverick.frameSpeed = 1;
		} else if (input.isHeld(Control.Down, player)) {
			maverick.move(new Point(0, 75));
			maverick.frameSpeed = 1;
		}
		if (input.isHeld(Control.Left, player)) {
			maverick.move(new Point(-75, 0));
			maverick.frameSpeed = 1;
		} else if (input.isHeld(Control.Right, player)) {
			maverick.move(new Point(75, 0));
			maverick.frameSpeed = 1;
		}

		maverick.turnToInput(input, player);

		if (!player.isAI && input.isPressed(Control.Jump, player)) {
			maverick.changeState(new MFall());
		}

		bool lastFrameHitLadder = hitLadder;
		if (!checkClimb()) {
			if (!lastFrameHitLadder) {
				maverick.changePos(oldPos);
			} else {
				maverick.changeState(new MFall());
			}
		}

		if (maverick.grounded) {
			maverick.changeState(new MIdle());
		}
	}
}
public class StingCJumpAI : MaverickState {
	public StingCJumpAI() : base("jump", "jump_start") {
	}

	public override void update() {
		base.update();
		var hit = Global.level.raycast(maverick.pos, maverick.pos.addxy(0, -105), new List<Type>() { typeof(Wall) });
		if (maverick.vel.y < 100 && hit?.gameObject is Wall wall && !wall.topWall) {
			maverick.changeState(new StingCHangState(hit.getHitPointSafe().y));
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.vel.y = -maverick.getJumpPower() * 1.25f;
	}
}

public class StingCTongueState : MaverickState {
	public StingCTongueState(int type) : base(type == 0 ? "tongue" : (type == 1 ? "tongue2" : "tongue3")) {
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
		if (player == null) return;

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}
}

public class StingCClimbTongueState : MaverickState {
	public MaverickState? oldState;
	public StingCClimbTongueState(Point inputDir) : base(getSpriteFromInputDir(inputDir)) {
	}

	private static string getSpriteFromInputDir(Point inputDir) {
		if (inputDir.y == 0) return "cling_tongue";
		else if (inputDir.y == 1) {
			if (inputDir.x != 0) return "cling_tongue2";
			else return "cling_tongue3";
		} else {
			if (inputDir.x != 0) return "cling_tongue4";
			else return "cling_tongue5";
		}
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (maverick.isAnimOver()) {
			maverick.changeState(oldState ?? new MIdle());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		maverick.useGravity = false;
		this.oldState = oldState;
	}
}

public class StingCClingShootState : MaverickState {
	bool shotOnce;
	public MaverickState? oldState;
	public StingChameleon StingChameleao= null!;
	public StingCClingShootState() : base("cling_shoot") {
	}

	public override void update() {
		base.update();
		if (StingChameleao == null) return;

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			maverick.playSound("stingcSting", sendRpc: true);
			
			for (int i = 0; i <= 2; i++) {
				proj(i);
			}
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(oldState ?? new MIdle());
		}
	}
	public void proj(int type) {
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos != null) {
			new StingCStingProj(
				shootPos.Value, maverick.xDir, type, StingChameleao, 
				player, player.getNextActorNetId(), rpc: true
				);
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;
		this.oldState = oldState;
		StingChameleao = maverick as StingChameleon ?? throw new NullReferenceException();

	}
}

public class StingCHangState : MaverickState {
	int state;
	float spikeTime;
	float endTime;
	float? _ceilingY;
	float ceilingY => _ceilingY ?? 0;
	public StingChameleon StingChameleao= null!;

	public StingCHangState(float? ceilingY) : base("hang") {
		_ceilingY = ceilingY;
	}

	public override bool canEnter(Maverick maverick) {
		Point incPos = getTargetPos(maverick).subtract(maverick.pos);
		if (Global.level.checkTerrainCollisionOnce(maverick, incPos.x, incPos.y) != null) {
			return false;
		}
		return base.canEnter(maverick);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		StingChameleao = maverick as StingChameleon ?? throw new NullReferenceException();
		maverick.stopMoving();
		maverick.useGravity = false;
		maverick.changePos(getTargetPos(maverick));
		maverick.frameSpeed = 0;

		if (_ceilingY == null) {
			_ceilingY = Global.level.raycast(
				maverick.pos, maverick.pos.addxy(0, -105), [typeof(Wall)]
			)?.getHitPointSafe().y;
		}
	}

	private Point getTargetPos(Maverick maverick) {
		return new Point(maverick.pos.x, ceilingY + 97);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}

	public override void update() {
		base.update();
		if (StingChameleao == null) return;

		if (state == 0) {
			if (stateTime > 0.25f) {
				maverick.frameSpeed = 1;
				state = 1;
			}
		} else if (state == 1) {
			spikeTime += Global.spf;
			if (spikeTime > 0.075f) {
				spikeTime = 0;
				float randX = Helpers.randomRange(-150, 150);
				Point pos = new Point(maverick.pos.x + randX, ceilingY);

				new StingCSpikeProj(
					pos, 1, StingChameleao, player,
					player.getNextActorNetId(), rpc: true
				);

				maverick.playSound("stingcSpikeDrop", sendRpc: true);
			}

			if (maverick.loopCount > 4) {
				maverick.frameSpeed = 0;
				maverick.frameIndex = 1;
				state = 2;
			}
		} else if (state == 2) {
			endTime += Global.spf;
			if (endTime > 0.25f) {
				maverick.changeState(new MFall());
			}
		}
	}
}

#endregion
