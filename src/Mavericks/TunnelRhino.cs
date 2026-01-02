using System;
using System.Collections.Generic;

namespace MMXOnline;

public class TunnelRhino : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.TunnelRGeneric, 153); }
	public static Weapon getMeleeWeapon(Player player) { return new Weapon(WeaponIds.TunnelRGeneric, 153); }

	public Weapon meleeWeapon;
	public TunnelRhino(
		Player player, Point pos, int xDir, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(TunnelRShootState), new(45, false, true) },
			{ typeof(TunnelRShoot2State), new(45, false, true) },
			{ typeof(TunnelRDashState), new(60) }
		};

		weapon = getWeapon();
		meleeWeapon = getMeleeWeapon(player);

		spriteFrameToSounds["tunnelr_run/3"] = "walkStomp";
		spriteFrameToSounds["tunnelr_run/11"] = "walkStomp";

		awardWeaponId = WeaponIds.TornadoFang;
		weakWeaponId = WeaponIds.AcidBurst;
		weakMaverickWeaponId = WeaponIds.ToxicSeahorse;

		netActorCreateId = NetActorCreateId.TunnelRhino;
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
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new TunnelRShootState(false));
				} else if (input.isPressed(Control.Special1, player)) {
					changeState(new TunnelRShoot2State());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new TunnelRDashState());
				}
			}
		}
	}

	public override float getRunSpeed() {
		return Physics.WalkSpeedSec * getRunDebuffs();
	}

	public override string getMaverickPrefix() {
		return "tunnelr";
	}

	public override MaverickState[] strikerStates() {
		return [
			new TunnelRShootState(false),
			new TunnelRShoot2State(),
			new TunnelRDashState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		int enemyxDir = 1;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
			enemyxDir = target.xDir * -1;
		}
		List<MaverickState> aiStates = [];
		if (enemyDist > 30) {
			aiStates.Add(new TunnelRShootState(false));
			aiStates.Add(new TunnelRShoot2State());

		}
		if (enemyDist > 30) {
			aiStates.Add(new TunnelRDashState());
		}
		if (enemyDist <= 30) {
			xDir = -xDir;
			aiStates.Add(new TunnelRDashState());
			aiStates.Add(new TunnelRJumpAI());
		}
		return aiStates.ToArray();
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
			"tunnelr_dash" => MeleeIds.Dash,
			"tunnelr_fall" => MeleeIds.Fall,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Dash => new GenericMeleeProj(
				weapon, pos, ProjIds.TunnelRDash, player,
				4, Global.defFlinch, addToLevel: addToLevel
			),
			MeleeIds.Fall => new GenericMeleeProj(
				weapon, pos, ProjIds.TunnelRStomp, player,
				4, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.TunnelRStomp) {
			float damage = 1 + Helpers.clamp(MathF.Floor(deltaPos.y * 0.6125f), 0, 4);
			proj.damager.damage = damage;
		}
	}

}

public class TunnelRTornadoFang : Projectile {
	int state = 0;
	float stateTime = 0;
	int type;
	float sparksCooldown;
	public TunnelRTornadoFang(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "tunnelr_proj_drillbig", netId, player
	) {
		weapon = TunnelRhino.getWeapon();
		damager.damage = 1;
		damager.hitCooldown = 15;
		vel = new Point(100 * xDir, 0);
		maxTime = 1.5f;
		projId = (int)ProjIds.TunnelRTornadoFang;
		destroyOnHit = false;
		this.type = type;
		if (type != 0) {
			vel.x = 0;
			vel.y = (type == 1 ? -100 : 100);
			projId = (int)ProjIds.TunnelRTornadoFang2;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TunnelRTornadoFang(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref sparksCooldown);

		if (state == 0) {
			if (type == 0) {
				if (stateTime > 0.15f) {
					vel.x = 0;
				}
			} else if (type == 1 || type == 2) {
				if (stateTime > 0.15f) {
					vel.y = 0;
				}
				if (stateTime > 0.15f && stateTime < 0.3f) vel.x = 100 * xDir;
				else vel.x = 0;
			}
			stateTime += Global.spf;
			if (stateTime >= 0.75f) {
				state = 1;
			}
		} else if (state == 1) {
			vel.x += Global.spf * 500 * xDir;
			if (Math.Abs(vel.x) > 350) vel.x = 350 * xDir;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		vel.x = 4 * xDir;

		if (damagable is not CrackedWall) {
			time -= Global.spf;
			if (time < 0) time = 0;
		}

		if (sparksCooldown == 0) {
			playSound("tunnelrDrill");
			var sparks = new Anim(pos, "tunnelfang_sparks", xDir, null, true);
			sparks.setzIndex(zIndex + 100);
			sparksCooldown = 0.25f;
		}
		var chr = damagable as Character;
		if (chr != null && chr.ownedByLocalPlayer && !chr.isSlowImmune()) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
			chr.slowdownTime = 0.25f;
		}
	}
}
public class TunnelRMState : MaverickState {
	public TunnelRhino screwMasaider = null!;

	public TunnelRMState(
		string sprite, string transitionSprite = ""
	) : base(
		sprite, transitionSprite
	) {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		screwMasaider = maverick as TunnelRhino ?? throw new NullReferenceException();
	}
}

public class TunnelRShootState : TunnelRMState {
	bool shotOnce;
	bool isSecond;
	public TunnelRShootState(bool isSecond) : base("shoot1") {
		this.isSecond = isSecond;
		exitOnAnimEnd = true;
		canEnterSelf = true;
	}

	public override void update() {
		base.update();

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			maverick.playSound("tunnelrShoot", sendRpc: true);
			if (!isSecond) {
				new TunnelRTornadoFang(
					shootPos.Value, maverick.xDir, 0, screwMasaider,
					player, player.getNextActorNetId(), rpc: true
				);
			} else {
				new TunnelRTornadoFang(
					shootPos.Value, maverick.xDir, 1, screwMasaider,
					 player, player.getNextActorNetId(), rpc: true);
				new TunnelRTornadoFang(
					shootPos.Value, maverick.xDir, 2, screwMasaider,
					 player, player.getNextActorNetId(), rpc: true);
			}
		}

		if (!isSecond && (input.isPressed(Control.Shoot, player) || isAI) && maverick.frameIndex > 6) {
			maverick.sprite.restart();
			maverick.changeState(new TunnelRShootState(true), true);
		}
	}
}

public class TunnelRTornadoFangDiag : Projectile {
	public TunnelRTornadoFangDiag(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "tunnelr_proj_drill", netId, player	
	) {
		weapon = TunnelRhino.getWeapon();
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 15;
		maxTime = 1.5f;
		projId = (int)ProjIds.TunnelRTornadoFangDiag;
		destroyOnHit = false;
		vel = new Point(xDir * 150, -150);

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new TunnelRTornadoFangDiag(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}


public class TunnelRShoot2State : TunnelRMState {
	bool shotOnce;
	bool shotOnce2;
	bool shotOnce3;
	public TunnelRShoot2State() : base("shoot3") {
		exitOnAnimEnd = true;
	}
	public override void update() {
		base.update();

		Point? shootPos = maverick.getFirstPOI("drillbig");
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			new TunnelRTornadoFang(
				shootPos.Value, maverick.xDir, 0,
				screwMasaider, player, player.getNextActorNetId(), rpc: true
			);
			maverick.playSound("tunnelrShoot", sendRpc: true);
		}

		Point? shootPos2 = maverick.getFirstPOI("drillfront");
		if (!shotOnce2 && shootPos2 != null) {
			shotOnce2 = true;
			new TunnelRTornadoFangDiag(
				shootPos2.Value, maverick.xDir * -1,
				screwMasaider, player, player.getNextActorNetId(), rpc: true);
		}

		Point? shootPos3 = maverick.getFirstPOI("drillback");
		if (!shotOnce3 && shootPos3 != null) {
			shotOnce3 = true;
			new TunnelRTornadoFangDiag(
				shootPos3.Value, maverick.xDir,
				screwMasaider, player, player.getNextActorNetId(), rpc: true
			);
		}
	}
}

public class TunnelRDashState : MaverickState {
	float dustTime;
	public TunnelRDashState() : base("dash", "dash_start") {
	}

	public override void update() {
		base.update();
		if (inTransition()) return;

		dustTime += Global.spf;
		if (dustTime > 0.05f) {
			dustTime = 0;
			new Anim(
				maverick.pos.addxy(-maverick.xDir * 10, 0), "dust", 1,
				player.getNextActorNetId(), true, sendRpc: true
			) {
				vel = new Point(0, -50)
			};
		}

		var move = new Point(250 * maverick.xDir, 0);
		var hitGround = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 5, 20);
		if (hitGround == null) {
			maverick.changeState(new MIdle());
			return;
		}

		var hitWall = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 2, -5);
		if (hitWall?.isSideWallHit() == true) {
			maverick.playSound("crash", sendRpc: true);
			maverick.shakeCamera(sendRpc: true);
			maverick.changeToIdleOrFall();
			return;
		}

		maverick.move(move);

		if (isHoldStateOver(0.5f, 1.5f, 1.5f, Control.Dash)) {
			maverick.changeToIdleOrFall();
			return;
		}
	}
}
public class TunnelRJumpAI : MaverickState {
	public TunnelRJumpAI() : base("jump", "jump_start") {
	}

	public override void update() {
		base.update();
		if (stateTime > 12f / 60f) {
			maverick.changeState(new MFall());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.vel.y = -maverick.getJumpPower() * 1.25f;
	}
}
