using System;
using System.Collections.Generic;
namespace MMXOnline;

public class DrDoppler : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.DrDoppler, 159); }
	public int shootTimes;
	public Weapon? meleeWeapon;
	public int ballType;
	public float targetTime;
	public DrDoppler(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(MShoot), new(45, true) },
			{ typeof(DrDopplerAbsorbState), new(45, true) },
			{ typeof(DrDopplerDashStartState), new(90) }
		};

		weapon = getWeapon();
		canClimbWall = true;
		canClimb = true;
		spriteFrameToSounds["drdoppler_run/1"] = "run";
		spriteFrameToSounds["drdoppler_run/5"] = "run";
		awardWeaponId = WeaponIds.Buster;
		weakWeaponId = WeaponIds.AcidBurst;
		weakMaverickWeaponId = WeaponIds.ToxicSeahorse;

		netActorCreateId = NetActorCreateId.DrDoppler;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		usesAmmo = true;
		canHealAmmo = true;
		ammo = 32;
		maxAmmo = 32;
		grayAmmoLevel = 8;
		barIndexes = (66, 55);
		gameMavs = GameMavs.X3;
	}

	public override void update() {
		base.update();

		if (state is not DrDopplerAbsorbState) {
			rechargeAmmo(1);
		} else {
			drainAmmo(2);
		}
		Helpers.decrementFrames(ref targetTime);
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (input.isPressed(Control.Special2, player)) {
				if (state is not StingCClimb && grounded) {
					ballType++;
					if (ballType == 2) ballType = 0;
					if (ballType == 1) {
						Global.level.gameMode.setHUDErrorMessage(player, "Switched to Neurocomputer Vaccine.", false, true);
						if (player.weapon is DrDopplerWeapon ddw) {
							ddw.ballType = 1;
						}
					} else {
						Global.level.gameMode.setHUDErrorMessage(player, "Switched to Neurocomputer Shock Gun.", false, true);
						if (player.weapon is DrDopplerWeapon ddw) {
							ddw.ballType = 0;
						}
					}
				}
			}

			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new DrDopplerShootState());
				} else if (input.isPressed(Control.Special1, player) && ammo >= 8) {
					deductAmmo(8);
					changeState(new DrDopplerAbsorbState());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new DrDopplerDashStartState());
				}
			} else if (state is MJump || state is MFall) {
			}
		}
	}

	public override string getMaverickPrefix() {
		return "drdoppler";
	}

	public override MaverickState[] strikerStates() {
		return [
			new DrDopplerShootState(),
			new DrDopplerAbsorbState(),
			new DrDopplerDashStartState(),
			getShootState(true),
		];
	}

	public override MaverickState[] aiAttackStates() {
		List<MaverickState> aiStates = [];
		if (shootTimes < 3) {
			shootTimes++;
			aiStates.Add(new DrDopplerShootState());
		} else {
			aiStates.Add(new DrDopplerDashStartState());
			shootTimes = 0;
		}
		return aiStates.ToArray();
	}
	public override void aiUpdate() {
		base.aiUpdate();
		if (controlMode == MaverickModeId.Summoner && Helpers.randomRange(0, 10) == 1 &&
			ammo >= 8 && state is not DrDopplerAbsorbState or DrDopplerDashState && health < maxHealth) {
			foreach (GameObject gameObject in getCloseActors(64, true, false, false)) {
				if (gameObject is Projectile proj &&
					proj.damager.owner.alliance != player.alliance &&
					!proj.isMelee
				) {
					deductAmmo(4);
					changeState(new DrDopplerAbsorbState());
				}
			}
		}
		if (controlMode == MaverickModeId.Summoner) {
			if (target == null && targetTime <= 0) {
				ballType = 1;
				changeState(getShootState(false));
			} else if (target != null) {
				ballType = 0;
				targetTime = 120;
			}
		}

	}

	public MaverickState getShootState(bool isAI) {
		var mshoot = new MShoot((Point pos, int xDir) => {
			 playSound("busterX3", sendRpc: true); 
			new DrDopplerBallProj(
				pos, xDir, 1, 0, this, 
				player, player.getNextActorNetId(), rpc: true
			);

		}, null!);
		if (isAI) {
			mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 5, 0f);
		}
		return mshoot;
	} 

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Charge,
		ChargeUnderwater,
		AttackAbsorb,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"drdoppler_dash" => MeleeIds.Charge,
			"drdoppler_dash_water" => MeleeIds.ChargeUnderwater,
			"drdoppler_absorb" => MeleeIds.AttackAbsorb,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Charge => new GenericMeleeProj(
				weapon, pos, ProjIds.DrDopplerDash, player,
				4, Global.defFlinch, addToLevel: addToLevel
			),
			MeleeIds.ChargeUnderwater => new GenericMeleeProj(
				weapon, pos, ProjIds.DrDopplerDashWater, player,
				2, Global.defFlinch, addToLevel: addToLevel
			),
			MeleeIds.AttackAbsorb => new GenericMeleeProj(
				weapon, pos, ProjIds.DrDopplerAbsorb, player,
				0, 0, 15, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public void healDrDoppler(Player attacker, float damage) {
		if (ownedByLocalPlayer && health < maxHealth) {
			addHealth(damage, true);
			playSound("healX3", sendRpc: true);
			addDamageText(-damage);
			ammo -= damage;
			if (ammo < 0) ammo = 0;
			RPC.addDamageText.sendRpc(attacker.id, netId, -damage);
		}
	}
}

public class DrDopplerBallProj : Projectile {
	public int type = 0;
	public int num = 0;
	public DrDopplerBallProj(
		Point pos, int xDir, int type, int num, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, type == 0 ? "drdoppler_proj_ball" : "drdoppler_proj_ball2", netId, player
	) {
		weapon = DrDoppler.getWeapon();
		damager.damage = 3;
		damager.hitCooldown = 30;
		this.type = type;
		this.num = num;
		if (type == 0) {
			damager.flinch = Global.miniFlinch;
			projId = (int)ProjIds.DrDopplerBall;
		} else {
			projId = (int)ProjIds.DrDopplerBall2;
			damager.damage = 0;
			destroyOnHit = false;
		}
		if (num == 0) vel = new Point(250 * xDir, 0);	
		if (num == 1) vel = new Point(250 * xDir, 160);
		if (num == 2) vel = new Point(250 * xDir, -160);
		if (num == 3) vel = new Point(250 * xDir, -70);
		if (num == 4) vel = new Point(250 * xDir, 70);
		
		maxTime = 0.75f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, 
				new byte[] { (byte)type, (byte) num}
			);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new DrDopplerBallProj(
			args.pos, args.xDir, args.extraData[0], args.extraData[1], args.owner, args.player, args.netId
		);
	}
}
public class DrDopplerShootState : MaverickState {
	public DrDoppler drDoppler = null!;

	public DrDopplerShootState() : base("shoot") {
		attackCtrl = true;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		drDoppler = maverick as DrDoppler ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		bool upHeld = player.input.isHeld(Control.Up, player);
		bool downHeld = player.input.isHeld(Control.Down, player);
		bool LeftOrRightHeld = player.input.isHeld(Control.Left, player) || 
							   player.input.isHeld(Control.Right, player);
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos != null) {
			if (!once) {
				once = true;
				if (drDoppler.ballType == 0) drDoppler.playSound("electricSpark", sendRpc: true);
				else drDoppler.playSound("busterX3", sendRpc: true); 
				
				if (downHeld && LeftOrRightHeld) {
					DopplerProjectile(4);
				} else if (downHeld) {
					DopplerProjectile(1);
				} else if (upHeld && LeftOrRightHeld) {
					DopplerProjectile(3);
				} else if (upHeld) {
					DopplerProjectile(2);
				} else 
					DopplerProjectile(0);
			}
		}
		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
	public void DopplerProjectile(int type) {
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos != null) {
			new DrDopplerBallProj(
				shootPos.Value, maverick.xDir, drDoppler.ballType, type,
				drDoppler, player, player.getNextActorNetId(), rpc: true
			);
		}
	}
}
public class DrDopplerDashStartState : MaverickState {
	public DrDopplerDashStartState() : base("dash_charge") {
		stopMoving = true;
		enterSound = "ryuenjin";
	}

	public override void update() {
		base.update();
		if (maverick.isAnimOver()) {
			maverick.changeState(new DrDopplerDashState());
		}
	}
}

public class DrDopplerDashState : MaverickState {
	Anim? barrier;
	float soundTime;
	public DrDopplerDashState() : base("dash", "dash_start") {
		stopMoving = true;
	}

	public override void update() {
		base.update();
		if (maverick.sprite.name == "drdoppler_dash") {
			soundTime += Global.spf;
			if (soundTime > 6f/60f) {
				soundTime = 0;
				maverick.playSound("flamethrower", sendRpc: true);
			}
		}
		if (inTransition()) {
			if (!once) {
				once = true;
				barrier = new Anim(maverick.pos, "drdoppler_barrier", maverick.xDir, player.getNextActorNetId(), false, sendRpc: true);
			}
			if (barrier != null) {
				barrier.incPos(maverick.deltaPos);
			}
			return;
		} else if (barrier != null) {
			barrier.destroySelf();
			barrier = null;
		}

		if (maverick.isUnderwater()) {
			if (!maverick.sprite.name.EndsWith("dash_water")) {
				maverick.changeSpriteFromName("dash_water", false);
			}
		} else {
			if (!maverick.sprite.name.EndsWith("dash")) {
				maverick.changeSpriteFromName("dash", false);
			}
		}

		var move = new Point(250 * maverick.xDir, 0);

		var hitWall = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 2, -5);
		if (hitWall?.isSideWallHit() == true) {
			maverick.changeToIdleOrFall();
			return;
		}

		maverick.move(move);

		if (stateTime > 1.25f || input.isPressed(Control.Dash, player)) {
			maverick.changeState(new MIdle());
			return;
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		useGravity = false;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		barrier?.destroySelf();
		useGravity = true;
	}
}

public class DrDopplerAbsorbState : MaverickState {
	float soundTime = 119f/60f;
	public SoundWrapper? sound;
	public DrDopplerAbsorbState() : base("absorb") {
		exitOnAnimEnd = true;
		superArmor = true;
		enterSound = "bcrabShield";
	}

	public override void update() {
		base.update();
		if (maverick.frameIndex >= 9) {
			soundTime += Global.spf;
			if (soundTime >= 120f/60f) {
				soundTime = 0;
				sound = maverick.playSound("greenaurax3", sendRpc: true);
			}
		}
		if (maverick.ammo <= 0) {
			maverick.changeToIdleOrFall();
			return;
		}
		if (isAI) {
			if (maverick.frameIndex == 13) {
				maverick.frameIndex = 9;
			}
			if (stateTime > 90f / 60f) {
				maverick.changeToIdleOrFall();
			}
		}
		if (input.isHeld(Control.Special1, player) && maverick.frameIndex == 13) {
			maverick.frameIndex = 9;
		}
	}
	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		if (sound != null && !sound.deleted) {
			sound.sound?.Stop();
		}
		RPC.stopSound?.sendRpc("greenaurax3", maverick.netId);

	}
}

public class DrDopplerUncoatState : MaverickState {
	public DrDopplerUncoatState() : base("uncoat") {
		exitOnAnimEnd = true;
		enterSound = "transform";
		aiAttackCtrl = true;
	}

	public override void update() {
		base.update();
		if (!once && maverick.frameIndex >= 1) {
			once = true;
			new Anim(maverick.getFirstPOIOrDefault(), "drdoppler_coat", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true) { vel = new Point(maverick.xDir * 50, 0) };
		}
	}
}
