using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ToxicSeahorse : Maverick {
	public static Weapon netWeapon = new Weapon(WeaponIds.TSeahorseGeneric, 152);
	public static Weapon getWeapon() { return new Weapon(WeaponIds.TSeahorseGeneric, 152); }
	public float teleportCooldown;

	public ToxicSeahorse(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(MShoot), new(60, true, true) },
			{ typeof(TSeahorseShoot2State), new(2 * 60, true, true) },
			{ typeof(TSeahorseTeleportState), new(45, true) }
		};

		weapon = getWeapon();

		awardWeaponId = WeaponIds.AcidBurst;
		weakWeaponId = WeaponIds.FrostShield;
		weakMaverickWeaponId = WeaponIds.BlizzardBuffalo;

		spriteToCollider["teleport"] = getDashCollider(1, 0.25f);

		netActorCreateId = NetActorCreateId.ToxicSeahorse;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		usesAmmo = true;
		canHealAmmo = true;
		ammo = 32;
		maxAmmo = 32;
		grayAmmoLevel = 8;
		barIndexes = (67, 56);
		gameMavs = GameMavs.X3;
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref teleportCooldown);

		if (state is not TSeahorseTeleportState) {
			rechargeAmmo(4);
		} else {
			drainAmmo(3);
		}

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(getShootState(false));
				} else if (input.isPressed(Control.Special1, player)) {
					changeState(new TSeahorseShoot2State());
				} else if (input.isPressed(Control.Dash, player) && teleportCooldown == 0) {
					if (ammo >= 8) {
						deductAmmo(8);
						changeState(new TSeahorseTeleportState());
					}
				}
			}
		}
	}

	public override string getMaverickPrefix() {
		return "tseahorse";
	}

	public override MaverickState[] strikerStates() {
		return [
			getShootState(false),
			new TSeahorseShoot2State(),
			new TSeahorseTeleportState()
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = target.pos.distanceTo(pos);
		}
		List<MaverickState> aiStates = [
			getShootState(isAI: false),
			new TSeahorseShoot2State()
		];
		if (enemyDist <= 70) {
			aiStates.Add(new TSeahorseTeleportState());
		}
		return aiStates.ToArray();
	}

	public MaverickState getShootState(bool isAI) {
		var mshoot = new MShoot((Point pos, int xDir) => {
			// playSound("zbuster2", sendRpc: true);
			new TSeahorseAcidProj(pos, xDir, this, player, player.getNextActorNetId(), rpc: true);
		}, null!);
		if (isAI) {
			// mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
		}
		return mshoot;
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Teleport,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"tseahorse_teleport2" => MeleeIds.Teleport,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Teleport => new GenericMeleeProj(
				weapon, pos, ProjIds.TSeahorseEmerge, player,
				4, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}

}

public class TSeahorseAcidProj : Projectile, IDamagable {
	bool firstHit;
	float hitWallCooldown;
	float health = 3;
	public TSeahorseAcidProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "tseahorse_proj_acid_start", netId, player	
	) {
		weapon = ToxicSeahorse.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 1;
		projId = (int)ProjIds.TSeahorseAcid3;
		maxTime = 2;
		useGravity = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		checkBigAcidUnderwater();
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TSeahorseAcidProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		if (sprite.name.EndsWith("_start")) {
			if (isAnimOver()) {
				changeSprite("tseahorse_proj_acid", true);
				vel = new Point(xDir * 125, 0);
				speed = 125;
			}
			return;
		}

		Helpers.decrementTime(ref hitWallCooldown);
		checkBigAcidUnderwater();
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;
		if (hitWallCooldown > 0) return;

		bool didHit = false;
		bool wasSideHit = false;
		int moveDirX = MathF.Sign(vel.x);
		if (!firstHit) {
			firstHit = true;
			vel.x *= -1;
			vel.y = -speed * 0.25f;
			didHit = true;
			wasSideHit = true;
		} else if (other.isSideWallHit()) {
			vel.x *= -1;
			didHit = true;
			wasSideHit = true;
		} else if (other.isCeilingHit() || other.isGroundHit()) {
			acidSplashEffect(other, ProjIds.TSeahorseAcid1);
			vel.y *= -1;
			didHit = true;
			destroySelf();
		}
		if (didHit) {
			//playSound("gbeetleProjBounce", sendRpc: true);
			hitWallCooldown = 0.1f;
			if (wasSideHit && ownedByLocalPlayer) {
				new AcidBurstProjSmall(
					other.getHitPointSafe().addxy(-5 * moveDirX, 0), 1, new Point(-moveDirX * 50, 0),
					true, ProjIds.TSeahorseAcid1, this, owner, owner.getNextActorNetId(), rpc: true
				);
				new AcidBurstProjSmall(
					other.getHitPointSafe().addxy(-5 * moveDirX, 0), 1, new Point(-moveDirX * 100, 0),
					true, ProjIds.TSeahorseAcid1, this, owner, owner.getNextActorNetId(), rpc: true
				);
			}
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;
		health -= damage;
		if (health <= 0) {
			destroySelf();
			Anim.createGibEffect("tseahorse_acid_gib", pos, owner!, gibPattern: GibPattern.Random, sendRpc: true);
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) => damagerAlliance != owner.alliance;
	public bool isInvincible(Player attacker, int? projId) => false;
	public bool canBeHealed(int healerAlliance) => false;
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
	public bool isPlayableDamagable() { return false; }
}

public class TSeahorseAcid2Proj : Projectile {
	int bounces = 0;
	public int type = 0;
	bool once;

	public TSeahorseAcid2Proj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "tseahorse_proj_acid", netId, player	
	) {
		weapon = ToxicSeahorse.getWeapon();
		damager.hitCooldown = 30;
		maxTime = 4f;
		projId = (int)ProjIds.TSeahorseAcid2;
		useGravity = true;
		fadeSound = "acidBurst";
		this.type = type;
		if (type == 1) vel = new Point(xDir * 50, -300);
		else vel = new Point(xDir * 112, -235);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
		checkBigAcidUnderwater();
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TSeahorseAcid2Proj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (sprite.name == "acidburst_charged_start" && isAnimOver()) {
			changeSprite("acidburst_charged", true);
			vel.x = xDir * 100;
		}
		checkBigAcidUnderwater();
	}

	public override void onHitWall(CollideData other) {
		acidSplashEffect(other, ProjIds.TSeahorseAcid2);
		bounces++;
		if (bounces > 3) {
			destroySelf();
			return;
		}

		var normal = other.hitData.normal ?? new Point(0, -1);

		if (normal.isSideways()) {
			vel.x *= -1;
			incPos(new Point(5 * MathF.Sign(vel.x), 0));
		} else {
			if (type == 1 && !once) {
				once = true;
				vel.x *= -1;
			}
			vel.y *= -1;
			if (vel.y < -300) vel.y = -300;
			incPos(new Point(0, 5 * MathF.Sign(vel.y)));
		}
		playSound("acidBurst");
	}

	bool acidSplashOnce;
	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) {
			if (!acidSplashOnce) {
				acidSplashOnce = true;
				acidSplashParticles(pos, false, 1, 1, ProjIds.TSeahorseAcid2);
				acidFadeEffect();
			}
		}
		base.onHitDamagable(damagable);
	}

}

public class TSeahorseShoot2State : MaverickState {
	bool shotOnce;
	public ToxicSeahorse AcidSeaforce = null!;
	public TSeahorseShoot2State() : base("shoot2") {
		exitOnAnimEnd = true;
	}

	public override void update() {
		base.update();

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			maverick.playSound("acidBurst", sendRpc: true);
			new TSeahorseAcid2Proj(
				shootPos.Value, maverick.xDir, 0, 
				AcidSeaforce, player, player.getNextActorNetId(), rpc: true
			);
			new TSeahorseAcid2Proj(
				shootPos.Value, maverick.xDir, 1,
				AcidSeaforce, player, player.getNextActorNetId(), rpc: true
			);
		}
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		AcidSeaforce = maverick as ToxicSeahorse ?? throw new NullReferenceException();
	}
}

public class TSeahorseTeleportState : MaverickState {
	int state = 0;
	float shootCooldown;

	public TSeahorseTeleportState() : base("teleport") {
		enterSound = "tseahorseTeleportOut";
	}

	public override void update() {
		base.update();

		if (state == 0) {
			if (maverick.frameIndex == maverick.sprite.totalFrameNum - 1) {
				state = 1;
				stateTime = 0;
				maverick.frameIndex = maverick.sprite.totalFrameNum - 1;
				maverick.frameSpeed = 0;
			}
		} else if (state == 1) {
			bool isSummoner = maverick.controlMode == MaverickModeId.Summoner;
			bool isStriker = maverick.controlMode == MaverickModeId.Striker;
			bool isAiOnly = isSummoner || isStriker;
			float enemyDist = 300;

			Helpers.decrementTime(ref shootCooldown);
			maverick.frameSpeed = 0;
			int dir = isAiOnly ? maverick.xDir : input.getXDir(player);
			maverick.turnToInput(input, player);

			if (isSummoner) {
				bool back = false;
				if (maverick.target != null) {
					enemyDist = MathF.Abs(maverick.target.pos.x - maverick.pos.x);
					back = maverick.target.pos.x > maverick.pos.x;
				}
				if (enemyDist >= 120) {
					dir = 0;
				} else if (back) {
					dir = -1;
				} else {
					dir = 1;
				}
			}

			if (dir != 0) {
				var move = new Point(100 * dir, 0);
				var hitGroundMove = Global.level.checkTerrainCollisionOnce(maverick, dir * 20, 20);
				if (hitGroundMove == null) {
				} else {
					maverick.move(move);
					maverick.frameSpeed = 1;
				}
			}

			if (!isAI && input.isPressed(Control.Dash, player) ||
				isSummoner && enemyDist >= 110 ||
				maverick.ammo <= 0 ||
				isAI && stateTime > 1
			) {
				state = 2;
				maverick.changeSpriteFromName("teleport2", true);
				maverick.playSound("tseahorseTeleportIn", sendRpc: true);
			}
		} else if (state == 2 && maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
	}
	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
		maverick.angle = 0;
		if (maverick is ToxicSeahorse seaforce) {
			seaforce.teleportCooldown = 0.25f;
		}
	}
}
