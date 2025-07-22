using System;
using System.Collections.Generic;
namespace MMXOnline;

public class CrushCrawfish : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.CrushCGeneric, 155); }
	public static Weapon getMeleeWeapon(Player player) { return new Weapon(WeaponIds.CrushCGeneric, 155, new Damager(player, 1, 0, 0)); }

	public Weapon meleeWeapon;
	public CrushCrawfish(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
		base(player, pos, destPos, xDir, netId, ownedByLocalPlayer) {
		stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
		stateCooldowns.Add(typeof(CrushCShootArmState), new MaverickStateCooldown(false, true, 0.75f));
		stateCooldowns.Add(typeof(CrushCDashState), new MaverickStateCooldown(false, false, 0.5f));

		weapon = getWeapon();
		meleeWeapon = getMeleeWeapon(player);

		awardWeaponId = WeaponIds.SpinningBlade;
		weakWeaponId = WeaponIds.TriadThunder;
		weakMaverickWeaponId = WeaponIds.VoltCatfish;

		netActorCreateId = NetActorCreateId.CrushCrawfish;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}
		gameMavs = GameMavs.X3;
	}

	public override void update() {
		base.update();
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new CrushCShootArmState());
				} else if (input.isPressed(Control.Special1, player)) {
					changeState(getShootState(false));
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new CrushCDashState());
				}
			} else if (state is MJump || state is MFall) {
			}
		}
	}

	public override string getMaverickPrefix() {
		return "crushc";
	}

	public override MaverickState[] strikerStates() {
		return [
			new CrushCShootArmState(),
			getShootState(true),
			new CrushCDashState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		List<MaverickState> aiStates = [
			getShootState(isAI: true),
			new CrushCDashState()
		];
		if (enemyDist <= 110) {
			aiStates.Add(new CrushCShootArmState());
		}
		return aiStates.ToArray();
	}

	public MaverickState getShootState(bool isAI) {
		var mshoot = new MShoot((Point pos, int xDir) => {
			new CrushCProj(
				pos, xDir, this, player,
				player.getNextActorNetId(), rpc: true
			);
		}, "crushcShoot");
		if (isAI) {
			mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
		}
		return mshoot;
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Grab,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"crushc_grab" or "crushc_dash" => MeleeIds.Grab,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Grab => new GenericMeleeProj(
				weapon, pos, ProjIds.CrushCGrab, player,
				0, 0, 0, addToLevel: addToLevel
			),
			_ => null
		};
	}

}

public class CrushCProj : Projectile {
	bool once;
	public CrushCProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "crushc_proj", netId, player
	) {
		weapon = CrushCrawfish.getWeapon();
		damager.damage = 3;
		damager.hitCooldown = 1;
		vel = new Point(200 * xDir, 0);
		projId = (int)ProjIds.CrushCProj;
		maxDistance = 150f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new CrushCProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (damagable is Character chr && !chr.isSlowImmune()) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
			chr.slowdownTime = 0.25f;
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!once) {
			once = true;
			var normal = other?.hitData?.normal;
			if (normal != null) {
				if (normal.Value.x == 0) {
					normal = new Point(-1, 0);
				}
				normal = ((Point)normal).leftNormal();
			} else {
				normal = new Point(0, 1);
				return;
			}
			vel = normal.Value.times(speed);
			if (vel.y > 0) vel = vel.times(-1);
		}
	}
}

public class CrushCArmProj : Projectile {
	public int state = 0;
	float moveDistance2;
	float maxDistance2 = 100;
	public float Speed = 200;
	public int type = 0;
	CrushCrawfish cc;
	public CrushCArmProj(
		Point pos, int xDir, int type, CrushCrawfish cc,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner,  getSprite(type), netId, player
	) {
		weapon = CrushCrawfish.getWeapon();
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 60;
		projId = (int)ProjIds.CrushCArmProj;
		this.type = type;
		this.cc = cc;
		setIndestructableProperties();
		//AAAAAAAAA WHY YOU DON'T WORK
		canBeLocal = false;
		if (rpc) {
			byte[] ccbNetIdBytes = BitConverter.GetBytes(cc.netId ?? 0);
			byte[] ccTypeBytes = new byte[] { (byte)type };
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] {ccTypeBytes[0], ccbNetIdBytes[0], ccbNetIdBytes[1]});
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		ushort ScissorsShrimperId = BitConverter.ToUInt16(args.extraData, 0);
		CrushCrawfish? ScissorsShrimper = Global.level.getActorByNetId(ScissorsShrimperId) as CrushCrawfish;

		return new CrushCArmProj(
		args.pos, args.xDir, args.extraData[0], ScissorsShrimper!, args.owner, args.player, args.netId
		);
	}

	public static string getSprite(int type) {
		if (type == 1) return "crushc_proj_claw2";
		else if (type == 2) return "crushc_proj_claw3";
		else if (type == 0) return "crushc_proj_claw";
		return "crushc_proj_claw";
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (state == 0) {
			if (type == 0) move(new Point(200 * xDir, 0));
			if (type == 1) move(new Point(200 * xDir, -145));
			if (type == 2) move(new Point(0, -200));
			moveDistance2 += Speed * Global.spf;
			if (moveDistance2 > maxDistance2 || (cc.input.isPressed(Control.Shoot, cc.player) && time > 0.25f)) {
				state = 1;
			}
		} else if (state == 1) {
			if (type == 0) move(new Point(-200 * xDir, 0));
			if (type == 1) move(new Point(-200 * xDir, 145));
			if (type == 2) move(new Point(0, 200));

			moveDistance2 += Speed * Global.spf;
			if (moveDistance2 > maxDistance2 * 2 || pos.distanceTo(cc.getFirstPOIOrDefault()) < 10) {
				state = 2;
				destroySelf();
			}
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;

		state = 1;
	}
}

public class CrushCShootArmState : MaverickState {
	CrushCArmProj? proj;
	public CrushCrawfish ScissorsShrimper = null!;
	public CrushCShootArmState() : base("attack_claw") {
		superArmor = true;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ScissorsShrimper = maverick as CrushCrawfish ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		bool upHeld = player.input.isHeld(Control.Up, player);
		bool LeftOrRightHeld = player.input.isHeld(Control.Right, player) || 
							   player.input.isHeld(Control.Left, player);
		Point? shootPos = maverick.getFirstPOI();
		if (!once && shootPos != null) {
			once = true;
			maverick.playSound("crushcClaw", sendRpc: true);
			if (LeftOrRightHeld && upHeld) {
				ArmProj(1);
			} else if (upHeld) {
				ArmProj(2);
			} else {
				ArmProj(0);
			}
		}

		if (proj != null && proj.destroyed) {
			maverick.changeToIdleOrFall();
		}
	}
	public void ArmProj(int type) {
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos != null) {
			proj = new CrushCArmProj(
				shootPos.Value, maverick.xDir, type, ScissorsShrimper,
				ScissorsShrimper, player, player.getNextActorNetId(), rpc: true
			);
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		proj?.destroySelf();
	}
}

public class CrushCDashState : MaverickState {
	float dustTime;
	float ftdWaitTime;
	public CrushCDashState() : base("dash", "dash_start") {
		enterSound = "dashX3";
	}

	public override void update() {
		base.update();
		if (inTransition()) return;

		if (ftdWaitTime > 0) {
			tryChangeToIdleOrFall();
			return;
		}

		Helpers.decrementTime(ref dustTime);
		if (dustTime == 0) {
			new Anim(maverick.pos.addxy(-maverick.xDir * 10, 0), "dust", maverick.xDir, null, true);
			dustTime = 0.075f;
		}

		if (!player.ownedByLocalPlayer) return;

		var move = new Point(150 * maverick.xDir, 0);

		var hitGround = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 5, 20);
		if (hitGround == null) {
			tryChangeToIdleOrFall();
			return;
		}

		maverick.move(move);

		if (isHoldStateOver(0.25f, 1f, 0.75f, Control.Dash)) {
			tryChangeToIdleOrFall();
			return;
		}
	}

	public void tryChangeToIdleOrFall() {
		if (player.isDefenderFavored) {
			ftdWaitTime += Global.spf;
			if (ftdWaitTime < 0.25f) return;
		}
		maverick.changeToIdleOrFall();
	}

	public override bool trySetGrabVictim(Character grabbed) {
		maverick.changeState(new CrushCGrabState(grabbed));
		return true;
	}
}

public class CrushCGrabState : MaverickState {
	Character victim;
	float hurtTime;
	public bool victimWasGrabbedSpriteOnce;
	float timeWaiting;
	public CrushCrawfish ScissorsShrimper = null!;
	public CrushCGrabState(Character grabbedChar) : base("grab_attack") {
		victim = grabbedChar;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ScissorsShrimper = maverick as CrushCrawfish ?? throw new NullReferenceException();
	}
	public override void update() {
		base.update();
		if (!victimWasGrabbedSpriteOnce) {
			maverick.frameSpeed = 0;
		} else {
			maverick.frameSpeed = 1;
			if (maverick.frameIndex > 0) {
				Helpers.decrementTime(ref hurtTime);
				if (hurtTime == 0) {
					hurtTime = 0.16666f;
					ScissorsShrimper.meleeWeapon.applyDamage(victim, false, maverick, (int)ProjIds.CrushCGrabAttack);
				}
			}
		}

		if (victimWasGrabbedSpriteOnce && !victim.sprite.name.EndsWith("_grabbed")) {
			maverick.changeToIdleOrFall();
			return;
		}

		if (victim.sprite.name.EndsWith("_grabbed") || victim.sprite.name.EndsWith("_die")) {
			victimWasGrabbedSpriteOnce = true;
		}
		if (!victimWasGrabbedSpriteOnce) {
			timeWaiting += Global.spf;
			if (timeWaiting > 1) {
				victimWasGrabbedSpriteOnce = true;
			}
		}

		if (stateTime > CrushCGrabbed.maxGrabTime) {
			maverick.changeToIdleOrFall();
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		victim?.releaseGrab(maverick);
	}
}

public class CrushCGrabbed : GenericGrabbedState {
	public const float maxGrabTime = 4;
	public CrushCGrabbed(CrushCrawfish grabber) : base(grabber, maxGrabTime, "grab_attack", maxNotGrabbedTime: 1f) {
	}
}
