﻿using System;

namespace MMXOnline;

public class CrystalSnail : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.CSnailGeneric, 148); }

	public CrystalSnailShell? shell;
	public bool noShell { get { return shell != null; } }
	public CrystalHunterCharged? chargedCrystalHunter;
	float ammoTime;

	public CrystalSnail(
		Player player, Point pos, Point destPos,
		int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(CSnailShootState), new(45) }
		};

		spriteToCollider["shell"] = getShellCollider();
		spriteToCollider["shell_spin"] = getShellCollider();
		spriteToCollider["shell_dash"] = getShellCollider();

		weapon = getWeapon();

		awardWeaponId = WeaponIds.CrystalHunter;
		weakWeaponId = WeaponIds.MagnetMine;
		weakMaverickWeaponId = WeaponIds.MagnetMine;

		netActorCreateId = NetActorCreateId.CrystalSnail;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		usesAmmo = true;
		canHealAmmo = true;
		ammo = 32;
		maxAmmo = 32;
		barIndexes = (54, 43);

		armorClass = ArmorClass.Light;
		gameMavs = GameMavs.X2;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (chargedCrystalHunter != null) {
			if (chargedCrystalHunter.destroyed) {
				chargedCrystalHunter = null;
			} else {
				chargedCrystalHunter.incPos(deltaPos);
			}
		}

		Helpers.decrementTime(ref ammoTime);
		if (state is CSnailShellState) {
			if (ammoTime == 0) {
				ammoTime = 0.5f;
				ammo--;
				if (ammo <= 0) {
					ammo = 32;
					changeState(new CSnailWeaknessState(false));
				}
			}
		} else if (!isInShell()) {
			if (ammoTime == 0) {
				ammoTime = 0.25f;
				ammo++;
				if (ammo >= 32) ammo = 32;
			}
		}

		if (aiBehavior == MaverickAIBehavior.Control) {
			if ((state is MIdle or MRun or MLand)) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new CSnailShootState());
				} else if (input.isPressed(Control.Dash, player)) {
					if (noShell) {
						changeState(new CSnailDashState());
					} else {
						changeState(new CSnailShellState(true));
					}
				}
			} else if (state is MJump || state is MFall) {
			}
		}
	}

	public bool isInShell() {
		return state is CSnailShellState || state is CSnailShellSpinDashState || state is CSnailShellSpinSlowState;
	}

	public override string getMaverickPrefix() {
		return "csnail";
	}

	public override float getRunSpeed() {
		return noShell ? 100 : 75;
	}

	public override MaverickState[] strikerStates() {
		return [
			new CSnailShootState(),
			new CSnailShellState(true),
			new CSnailShellState(false),
		];
	}

	public override MaverickState[] aiAttackStates() {
		if (noShell) {
			return [
				new CSnailShootState(),
			];
		}
		return [
			new CSnailShootState(),
			new CSnailShootState(),
			new CSnailShellState(true),
			new CSnailShellState(false),
		];
	}

	public Collider getShellCollider() {
		var rect = new Rect(0, 0, width, height * 0.7f);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		ShellDash,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"csnail_shell_dash" => MeleeIds.ShellDash,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.ShellDash => new GenericMeleeProj(
				weapon, pos, ProjIds.CSnailMelee, player,
				3, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;

		if (other.gameObject == shell && state is not MHurt && state is not CSnailWeaknessState) {
			enterShell();
		}

		var character = other.gameObject as Character;
		if (state is CSnailDashState && character != null &&
			character.isCrystalized && character.player.alliance != player.alliance
		) {
			Damager.applyDamage(
				player, 3, 60, Global.defFlinch, character,
				false, (int)WeaponIds.CrystalHunter, 20, player.character!, (int)ProjIds.CrystalHunterDash
			);
		}
	}

	public void removeShell() {
		if (noShell) return;
		shell = new CrystalSnailShell(getCenterPos(), xDir, this, player, player.getNextActorNetId(), sendRpc: true);
		shell.vel = new Point(-xDir * 150, -150);
		shell.setzIndex(zIndex - 1);
		changeSpriteFromName(sprite.name.RemovePrefix(getMaverickPrefix() + "_"), false);
	}

	public void enterShell() {
		if (!noShell) return;
		shell?.destroySelf();
		shell = null;
		changeSpriteFromName(sprite.name.RemovePrefix(getMaverickPrefix() + "_noshell_"), false);
	}

	public override void onDestroy() {
		base.onDestroy();
		chargedCrystalHunter?.destroySelf();
		shell?.destroySelf();
	}
}

public class CrystalSnailShell : Actor {
	const float leeway = 500;
	int bounces;
	CrystalSnail? cs;

	public CrystalSnailShell(
		Point pos, int xDir, CrystalSnail? cs, Player owner,
		ushort? netId, bool sendRpc = false, bool ownedByLocalPlayer = true
	) : base(
		"csnail_shell_empty", pos, netId, ownedByLocalPlayer, false
	) {
		this.xDir = xDir;
		this.cs = cs;
		frameSpeed = 0;

		netOwner = owner;
		netActorCreateId = NetActorCreateId.CrystalSnailShell;
		if (sendRpc) {
			createActorRpc(owner.id);
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (grounded) {
			frameIndex = 1;
			stopMoving();
		}

		if (pos.x > Global.level.width + leeway || pos.x < -leeway ||
			pos.y > Global.level.height + leeway || pos.y < -leeway
		) {
			destroySelf();
			return;
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;

		if (other.gameObject is Character || other.gameObject is Maverick || other.gameObject is RideArmor) {
			if (!grounded || other.gameObject == cs) {
				return;
			}
			var actor = other.gameObject as Actor;
			if (actor != null) vel = actor.deltaPos.times(1 / Global.spf);
			if (vel.y >= -50) vel.y = -MathF.Abs(vel.x);
			grounded = false;
			playSound("csnailShellBounce", sendRpc: true);
		} else if (other.gameObject is Wall) {
			bounces++;
			if (bounces >= 3) {
				bounces = 0;
				stopMoving();
				return;
			}

			var normal = other.hitData.normal ?? new Point(0, -1);

			if (normal.isSideways()) {
				vel.x *= -0.5f;
				incPos(new Point(5 * MathF.Sign(vel.x), 0));
			} else {
				vel.y *= -0.5f;
				if (vel.y < -300) vel.y = -300;
				incPos(new Point(0, 5 * MathF.Sign(vel.y)));
			}
			playSound("csnailShellBounce", sendRpc: true);
		}
	}
}

public class CSnailCrystalHunterProj : Projectile {
	public int type = 0;
	public CSnailCrystalHunterProj(
		Point pos, int xDir, int type, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "csnail_projectile", netId, player
	) {
		weapon = CrystalSnail.getWeapon();
		projId = (int)ProjIds.CSnailCrystalHunter;
		maxTime = 1.25f;
		this.type = type;
		destroyOnHit = true;
		if (type == 0) vel = new Point(175 * xDir, -145);	
		if (type == 1) vel = new Point(185 * xDir, -90);
		if (type == 2) vel = new Point(200 * xDir, 0);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new CSnailCrystalHunterProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		vel.y += Global.spf * 50;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		if (other.isSideWallHit()) {
			fadeSprite = "csnail_projectile_hitwall";
		}
		if (other.isGroundHit()) {
			fadeSprite = "csnail_projectile_hitground";
		}
		if (other.isCeilingHit()) {
			yDir = -1;
			fadeSprite = "csnail_projectile_hitground";
		}

		if (!string.IsNullOrEmpty(fadeSprite)) {
			changePos(other.getHitPointSafe());
			stopMoving();
			destroySelf();
		}
	}
}

public class CSnailShootState : MaverickState {
	bool shotOnce;
	CrystalSnail CristarMymine = null!;
	public CSnailShootState() : base("spit") {
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		CristarMymine = maverick as CrystalSnail ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		if (CristarMymine == null) return;

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			maverick.playSound("csnailShoot", sendRpc: true);
			new CSnailCrystalHunterProj(
				shootPos.Value, maverick.xDir, 0, CristarMymine,
				player, player.getNextActorNetId(), rpc: true
			);
			new CSnailCrystalHunterProj(
				shootPos.Value, maverick.xDir, 1, CristarMymine,
				player, player.getNextActorNetId(), rpc: true
			);
			new CSnailCrystalHunterProj(
				shootPos.Value, maverick.xDir, 2, CristarMymine,
				player, player.getNextActorNetId(), rpc: true
			);
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}
}

public class CSnailShellState : MaverickState {
	float jumpDist;
	float maxJumpDist = 112;
	float exhaustTime;
	bool isFirstTime;
	bool isRising;
	int aiChoice;
	public CSnailShellState(bool isFirstTime) : base("shell", isFirstTime ? "shell_enter" : "") {
		this.isFirstTime = isFirstTime;
		attackCtrl = true;
		aiAttackCtrl = true;
	}

	public override void update() {
		base.update();
		if (player == null) return;
		if (inTransition()) return;
		maverick.useGravity = true;
		maverick.turnToInput(input, player);

		if (aiChoice == 1 || input.isPressed(Control.Special1, player)) {
			maverick.changeState(new CSnailShellSpinSlowState());
			return;
		}

		if (aiChoice == 2 || input.isHeld(Control.Shoot, player)) {
			if (maverick.ammo >= 8) {
				maverick.changeState(new CSnailShellSpinDashState());
				return;
			}
		}

		if (input.isPressed(Control.Jump, player)) {
			if (maverick.grounded && !isRising) {
				isRising = true;
			} else if (!maverick.grounded && isRising) {
				isRising = false;
			}
		}

		if (isRising && jumpDist < maxJumpDist) {
			maverick.stopMoving();
			maverick.useGravity = false;
			maverick.grounded = false;
			float riseSpeed = 150;
			if (!maverick.tryMove(new Point(0, -riseSpeed), out _)) {
				jumpDist = maxJumpDist;
			}
			jumpDist += riseSpeed * Global.spf;
			Helpers.decrementTime(ref exhaustTime);
			if (exhaustTime == 0) {
				exhaustTime = 0.15f;
				var anim = new Anim(
					maverick.pos, "csnail_shell_flame",
					maverick.xDir, player.getNextActorNetId(), true, sendRpc: true
				);
				maverick.playSound("csnailFlame", sendRpc: true);
				anim.setzIndex(maverick.zIndex - 1);
			}
		}

		if (maverick.grounded) {
			jumpDist = 0;
			isRising = false;
		}

		if (isAI) {
			if (stateTime > 2) {
				maverick.changeToIdleOrFall("shell_end");
			}
		} else if (input.isPressed(Control.Dash, player)) {
			maverick.changeToIdleOrFall("shell_end");
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		if (!isFirstTime) {
			jumpDist = maxJumpDist;
		}
		if (isAI) {
			int rand = Helpers.randomRange(0, 3);
			if (rand == 0) aiChoice = 1;
			if (rand == 1) aiChoice = 2;
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}
}

public class CSnailShellSpinDashState : MaverickState {
	float spinDist;
	float angleSpeed = 360;
	float chargeDist;
	float maxChargeDist = 150;
	Anim? shell;
	int state;
	float exhaustTime;
	public CSnailShellSpinDashState() : base("shell") {
	}

	public override void update() {
		base.update();
		if (player == null) return;
		if (shell != null) {
			shell.incPos(maverick.deltaPos);
			shell.xDir = maverick.xDir;
		}
		if (state == 0) {
			shell!.angle += angleSpeed * Global.spf * maverick.xDir;
			spinDist += angleSpeed * Global.spf;
			if ((!input.isHeld(Control.Shoot, player) && spinDist >= 90) || stateTime > 2) {
				if (MathF.Abs(spinDist - 90) <= angleSpeed * Global.spf) {
					spinDist = 90;
					shell.angle = 90 * maverick.xDir;
				}
				state = 1;
				maverick.changeSpriteFromName("shell_dash", true);
				maverick.deductAmmo(8);
			}
		} else if (state == 1) {
			Point chargeDir = Point.createFromAngle(shell!.angle!.Value - 90);
			float chargeSpeed = 350;

			Point chargeDirReversed = chargeDir.times(-1);
			Point exhaustPos = shell.pos.addxy(chargeDirReversed.x * 16, chargeDirReversed.y * 16);
			Helpers.decrementTime(ref exhaustTime);
			if (exhaustTime == 0) {
				exhaustTime = 0.1f;
				maverick.playSound("csnailFlame", sendRpc: true);
				var anim = new Anim(
					exhaustPos, "csnail_shell_flame", 
					maverick.xDir, player.getNextActorNetId(), true, sendRpc: true
				);
				anim.setzIndex(maverick.zIndex - 1);
			}

			if (maverick.grounded) {
				maverick.move(chargeDir.times(chargeSpeed), pushIncline: true);
				var hit = checkCollision(chargeDir.x, chargeDir.y);
				if (chargeDist > maxChargeDist || hit?.isSideWallHit() == true || hit?.isCeilingHit() == true) {
					state = 2;
				} else {
					chargeDist += Global.spf * chargeSpeed;
				}
			} else {
				if (chargeDist > maxChargeDist || !maverick.tryMove(chargeDir.times(chargeSpeed), out _)) {
					state = 2;
				} else {
					chargeDist += Global.spf * chargeSpeed;
				}
			}
		} else if (state == 2) {
			maverick.changeToIdleOrFall("shell_exit");
			return;
		}

		/*
		if (!input.isHeld(Control.Dash, player))// && MathF.Abs(shell.angle.Value) < angleSpeed * Global.spf * 2)
		{
			maverick.changeToIdleOrFall("shell_exit");
		}
		*/
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;
		maverick.visible = false;
		maverick.stopMoving();
		shell = new Anim(
			maverick.getCenterPos().addxy(-4 * maverick.xDir, 6),
			"csnail_shell_spin", maverick.xDir, player.getNextActorNetId(), false, sendRpc: true
		);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
		maverick.visible = true;
		shell?.destroySelf();
	}
}

public class CSnailShellSpinSlowState : MaverickState {
	float spinDist;
	float angleSpeed = 720;
	Anim? shell;
	int state;
	float soundTime;
	float ammoTime;
	public CSnailShellSpinSlowState() : base("shell") {
	}

	public override void update() {
		base.update();

		Helpers.decrementTime(ref ammoTime);
		Helpers.decrementTime(ref soundTime);

		if (ammoTime <= 0) {
			ammoTime = 0.1f;
			maverick.deductAmmo(1);
		}

		if (soundTime == 0) {
			maverick.playSound("csnailSlowSpin", sendRpc: true);
			soundTime = 0.22f;
		}
		if (shell != null) {
			shell.incPos(maverick.deltaPos);
			shell.xDir = maverick.xDir;
		}
		if (shell != null) {
			if (state == 0) {
				shell.angle += angleSpeed * Global.spf;
				spinDist += angleSpeed * Global.spf;
				if (isAI) {
					if (spinDist > 360 * 2) {
						state = 1;
					}
				} else if (!input.isHeld(Control.Special1, player) || spinDist > 360 * 4 || maverick.ammo <= 0) {
					state = 1;
				}
			} else if (state == 1 && shell.angle != null) {
				shell.angle += angleSpeed * Global.spf;
				spinDist += angleSpeed * Global.spf;
				if (MathF.Abs(shell.angle.Value) < angleSpeed * Global.spf * 2) {
					maverick.changeState(new CSnailTimeStopState(spinDist / 360f));
				}
			}
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;
		maverick.visible = false;
		maverick.stopMoving();
		shell = new Anim(
			maverick.getCenterPos().addxy(-4 * maverick.xDir, 6),
			"csnail_shell_spin", maverick.xDir,
			player.getNextActorNetId(), false, sendRpc: true
		);
		shell.angle = 0;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
		maverick.visible = true;
		shell?.destroySelf();
	}
}

public class CSnailTimeStopState : MaverickState {
	bool shotOnce;
	float spinCycles;
	bool soundOnce;
	CrystalSnail CristarMymine = null!;
	public CSnailTimeStopState(float spinCycles) : base("timestop", "timestop_start") {
		this.spinCycles = spinCycles;
	}

	public override void update() {
		base.update();
		if (CristarMymine == null) return;
		if (inTransition()) return;

		if (!soundOnce) {
			soundOnce = true;
			maverick.playSound("csnailSpecial", sendRpc: true);
		}

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			float overrideTime = spinCycles * 2;
			if (CristarMymine.chargedCrystalHunter != null) {
				CristarMymine.chargedCrystalHunter.destroySelf();
			}

			CristarMymine.chargedCrystalHunter = new CrystalHunterCharged(
				shootPos.Value, player, player.getNextActorNetId(),
				player.ownedByLocalPlayer, overrideTime: overrideTime, sendRpc: true
			);
			maverick.playSound("csnailSlowStart", sendRpc: true);
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		maverick.useGravity = false;
		CristarMymine = maverick as CrystalSnail ?? throw new NullReferenceException();
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}
}

public class CSnailWeaknessState : MaverickState {
	float hurtSpeed;
	float soundTime;
	CrystalSnail CristarMymine = null!;
	public CSnailWeaknessState(bool wasMagnetMine) : base(wasMagnetMine ? "weakness" : "hurt") {
		aiAttackCtrl = true;
		canBeCanceled = false;
		if (!wasMagnetMine) {
			once = true;
			stateTime = 1;
		}
	}

	public override void update() {
		base.update();
		if (CristarMymine == null) return;
		if (inTransition()) return;

		maverick.removeRenderEffect(RenderEffectType.Flash);
		if (!once) {
			Helpers.decrementTime(ref soundTime);
			if (soundTime == 0) {
				maverick.playSound("csnailWeakness", sendRpc: true);
				soundTime = 0.290f;
			}

			if (stateTime < 1) {
				if (Global.isOnFrameCycle(4)) {
					maverick.addRenderEffect(RenderEffectType.Flash);
				}
			} else {
				once = true;
				maverick.changeSpriteFromName("hurt", true);
				maverick.frameIndex = 2;
			}
			return;
		}

		if (!CristarMymine.noShell) {
			CristarMymine.removeShell();
		}

		hurtSpeed = Helpers.toZero(hurtSpeed, 600 * Global.spf, 1);
		maverick.move(new Point(hurtSpeed * maverick.xDir, 0));

		if (stateTime > 1.5f) {
			maverick.changeToIdleOrFall();
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		maverick.useGravity = false;
		hurtSpeed = 300;
		CristarMymine = maverick as CrystalSnail ?? throw new NullReferenceException();
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.removeRenderEffect(RenderEffectType.Flash);
		maverick.useGravity = true;
	}
}

public class CSnailDashState : MaverickState {
	CrystalSnail CristarMymine = null!;
	public CSnailDashState() : base("dash") {
		aiAttackCtrl = true;
		canBeCanceled = false;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		CristarMymine = maverick as CrystalSnail ?? throw new NullReferenceException();
	}


	public override void update() {
		base.update();
		if (CristarMymine == null) return;
		if (inTransition()) return;

		if (!CristarMymine.noShell) {
			maverick.changeToIdleOrFall();
			return;
		}
		var move = new Point(200 * maverick.xDir, 0);

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

		if (stateTime > 0.6 || (stateTime > 0.1f && !input.isHeld(Control.Dash, player))) {
			maverick.changeState(new MIdle());
			return;
		}
	}
}
