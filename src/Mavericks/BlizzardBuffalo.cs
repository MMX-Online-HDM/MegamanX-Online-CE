using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class BlizzardBuffalo : Maverick {
	public static Weapon netWeapon = new Weapon(WeaponIds.BBuffaloGeneric, 151);

	public BlizzardBuffalo(
		Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(MShoot), new(45, true) },
			{ typeof(BBuffaloDashState), new(75, true) },
			{ typeof(BBuffaloShootBeamState), new(2 * 60) },
			{ typeof(BBuffaloShootAI), new(60, true) },
		};

		spriteFrameToSounds["bbuffalo_run/2"] = "walkStomp";
		spriteFrameToSounds["bbuffalo_run/8"] = "walkStomp";
		weapon = netWeapon;

		awardWeaponId = WeaponIds.FrostShield;
		weakWeaponId = WeaponIds.ParasiticBomb;
		weakMaverickWeaponId = WeaponIds.BlastHornet;

		netActorCreateId = NetActorCreateId.BlizzardBuffalo;
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
					changeState(getShootState(false));
				} else if (input.isPressed(Control.Special1, player)) {
					changeState(new BBuffaloShootBeamState());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new BBuffaloDashState());
				}
			} else if (state is MJump || state is MFall) {
			}
		}
	}

	public override float getRunSpeed() {
		return Physics.WalkSpeedSec * getRunDebuffs();
	}

	public override string getMaverickPrefix() {
		return "bbuffalo";
	}

	public override MaverickState[] strikerStates() {
		return [
			new BBuffaloShootAI(isStriker: true),
			new BBuffaloShootBeamState(),
			new BBuffaloDashState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		List<MaverickState> aiStates = [];
		if (enemyDist > 170) {
			aiStates.Add(new BBuffaloShootAI(isStriker: false));
		}
		if (enemyDist < 170 && enemyDist > 55) {
			aiStates.Add(new BBuffaloShootBeamState());
		}
		if (enemyDist < 170 && enemyDist > 30) {
			aiStates.Add(new BBuffaloDashState());
		}
		if (enemyDist <= 30) {
			xDir = -xDir;
			aiStates.Add(new BBuffaloDashState());
			aiStates.Add(new BuffaloJumpAI());
		}
		return aiStates.ToArray();
	}

	public MaverickState getShootState(bool isAI) {
		var mshoot = new MShoot(
			(Point pos, int xDir) => {

				Point unitDir = new Point(xDir, -1);
				var inputDir = input.getInputDir(player);
				if (inputDir.y == -1) unitDir.y = -2;
				if (inputDir.y == 1) unitDir.y = 0;
				if (inputDir.x == xDir) unitDir.x = xDir * 2;
				int shootFramesHeld = (int)MathF.Ceiling((state as MShoot)?.shootFramesHeld ?? 0);

				new BBuffaloIceProj(
					pos, xDir, (int)unitDir.x,  (int)unitDir.y, shootFramesHeld,
					this, player.getNextActorNetId(), sendRpc: true
				);
			},
			"bbuffaloShoot"
		);
		if (isAI) {
			mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
		}
		return mshoot;
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Dash,
		Fall,
		Grab,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"bbuffalo_dash" => MeleeIds.Dash,
			"bbuffalo_fall" => MeleeIds.Fall,
			"bbuffalo_dash_grab" => MeleeIds.Grab,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Dash => new GenericMeleeProj(
				weapon, pos, ProjIds.BBuffaloCrash, player,
				4, Global.defFlinch, 60, addToLevel: addToLevel
			),
			MeleeIds.Fall => new GenericMeleeProj(
				weapon, pos, ProjIds.BBuffaloStomp, player,
				4, Global.defFlinch, addToLevel: addToLevel
			),
			MeleeIds.Grab => new GenericMeleeProj(
				weapon, pos, ProjIds.BBuffaloDrag, player,
				0, 0, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.BBuffaloStomp) {
			float damage = Helpers.clamp(MathF.Floor(deltaPos.y * 0.9f), 1, 4);
			proj.damager.damage = damage;
		}
	}

}

public class BBuffaloIceProj : Projectile {
	public BBuffaloIceProj(
		Point pos, int xDir, int shootDirX, int shootDirY, int shootFramesHeld,
		Actor owner, ushort netProjId, bool sendRpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "bbuffalo_proj_iceball", netProjId, player
	) {
		damager.damage = 3;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 1;
		weapon = BlizzardBuffalo.netWeapon;
		projId = (int)ProjIds.BBuffaloIceProj;
		maxTime = 1.5f;
		useGravity = true;

		if (shootFramesHeld >= 255) {
			shootFramesHeld = 255;
		}
		Point unitDir = new Point(shootDirX, shootDirY).normalize();
		float speedModifier = Helpers.clamp((shootFramesHeld + 3) / 10f, 0.5f, 1.5f);
		vel = new Point(unitDir.x * 200 * speedModifier, unitDir.y * 250 * speedModifier);

		if (collider != null) { collider.wallOnly = true; }
		destroyOnHit = true;

		if (sendRpc) {
			rpcCreate(
				pos, ownerPlayer, netProjId, xDir,
				[(byte)(shootDirX + 128), (byte)(shootDirY + 128), (byte)shootFramesHeld]
			);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BBuffaloIceProj(
			args.pos, args.xDir,
			args.extraData[0] - 128, args.extraData[1] - 128, args.extraData[2],
			args.owner, args.netId, player: args.player
		);
	}

	public override void onStart() {
		base.onStart();

		if (checkCollision(0, 0) != null) {
			destroySelf();
			Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner);
			playSound("iceBreak");
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		destroySelf();
		if (!ownedByLocalPlayer) return;
		var hitNormal = other.getNormalSafe();
		new BBuffaloIceProjGround(
			other.getHitPointSafe(), 1, hitNormal.byteAngle, this,
			owner, owner.getNextActorNetId(), rpc: true
		);
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!ownedByLocalPlayer) return;

		destroySelf();
		Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner, sendRpc: true);
		playSound("iceBreak", sendRpc: true);
	}
}

public class BBuffaloIceProjGround : Projectile, IDamagable {
	float health = 6;

	public BBuffaloIceProjGround(
		Point pos, int xDir, float byteAngle, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bbuffalo_proj_ice", netId, player
	) {
		damager.damage = 3;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		weapon = BlizzardBuffalo.netWeapon;
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		maxTime = 5;
		projId = (int)ProjIds.BBuffaloIceProjGround;
		destroyOnHit = true;
		playSound("frostShield");
		updateHitboxes();
		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}
		public static Projectile rpcInvoke(ProjParameters args) {
		return new BBuffaloIceProjGround(
			args.pos, args.xDir, args.byteAngle, args.owner, args.player, args.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		moveWithMovingPlatform();
		updateNewHitboxes();
	}

	public void updateNewHitboxes() {
		if (!angleSet || collider?._shape == null) return;

		float angle360 = Helpers.to256(byteAngle);
		if (angle360 >= 0 && angle360 <= 32) {
			collider._shape.points = new List<Point>()
			{
					new Point(-9, 0),
					new Point(26, 0),
					new Point(26, 30),
					new Point(-9, 30),
				};
		} else if (angle360 > 32 && angle360 <= 96) {
			collider._shape.points = new List<Point>()
			{
					new Point(0 - 12, 0 + 12),
					new Point(30 - 12, 0 + 12),
					new Point(30 - 12, 35 + 12),
					new Point(0 - 12, 35 + 12),
				};
		} else if (angle360 > 96 && angle360 <= 160) {
			collider._shape.points = new List<Point>()
			{
					new Point(-9 - 18, 0),
					new Point(26 - 18, 0),
					new Point(26 - 18, 30),
					new Point(-9 - 18, 30),
				};
		} else if (angle360 > 160) {
			collider._shape.points = new List<Point>()
			{
					new Point(0 - 12, 0 - 12),
					new Point(30 - 12, 0 - 12),
					new Point(30 - 12, 35 - 12),
					new Point(0 - 12, 35 - 12),
				};
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}

	public bool canBeHealed(int healerAlliance) { return false; }
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damagerAlliance != owner.alliance;
	}
	public bool isInvincible(Player attacker, int? projId) {
		if (projId == null) {
			return true;
		}
		return !Damager.canDamageFrostShield(projId.Value);
	}
	public bool isPlayableDamagable() { return false; }

	public override void onDestroy() {
		base.onDestroy();
		Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner);
		playSound("iceBreak");
	}
}

public class BBuffaloBeamProj : Projectile {
	public Point startPos;
	public BlizzardBuffalo bb;
	public bool released;
	public float moveDistance2;
	public float maxDistance2;
	public BBuffaloBeamProj(
		Point pos, int xDir, BlizzardBuffalo bb, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bbuffalo_proj_beam_head", netId, player
	) {
		damager.hitCooldown = 30;
		weapon = BlizzardBuffalo.netWeapon;
		vel = new Point(150*xDir,0);
		projId = (int)ProjIds.BBuffaloBeam;
		setStartPos(pos.addxy(-xDir * 10, 0));
		maxDistance2 = 200;
		this.bb = bb;
		setIndestructableProperties();

		if (rpc) {
			byte[] bbNetIdBytes = BitConverter.GetBytes(bb.netId ?? 0);
			rpcCreate(pos, owner, ownerPlayer, netId, xDir,  new byte[] { bbNetIdBytes[0], bbNetIdBytes[1] });
		}
		// ToDo: Make local.
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		ushort FrozenBuffalioId = BitConverter.ToUInt16(args.extraData, 0);
		BlizzardBuffalo? FrozenBuffalio = Global.level.getActorByNetId(FrozenBuffalioId) as BlizzardBuffalo;
		return new BBuffaloBeamProj(
			args.pos, args.xDir, FrozenBuffalio!, 
			args.owner, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (bb == null) return;

		moveDistance2 += speed * Global.spf;

		if (!released) {
			setStartPos(startPos.add(bb.deltaPos));
			if (moveDistance2 > maxDistance2) {
				release();
			}
		} else {
			setStartPos(startPos.addxy(xDir * speed * Global.spf, 0));
			if (moveDistance2 > maxDistance2) {
				destroySelf();
			}
		}
	}

	public void release() {
		if (released) return;
		released = true;
		vel = Point.zero;
		maxDistance2 = moveDistance2;
		moveDistance2 = 0;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;
		release();
	}

	public void setStartPos(Point startPos) {
		this.startPos = startPos;
		globalCollider = new Collider(getPoints(), true, null, false, false, 0, Point.zero);
	}

	public List<Point> getPoints() {
		var points = new List<Point>();
		if (xDir == 1) {
			points.Add(new Point(startPos.x, startPos.y - 24));
			points.Add(new Point(pos.x, pos.y - 24));
			points.Add(new Point(pos.x, pos.y + 24));
			points.Add(new Point(startPos.x, startPos.y + 24));
		} else {
			points.Add(new Point(pos.x, pos.y - 24));
			points.Add(new Point(startPos.x, startPos.y - 24));
			points.Add(new Point(startPos.x, startPos.y + 24));
			points.Add(new Point(pos.x, pos.y + 24));
		}

		return points;
	}

	public override void render(float x, float y) {
		if (globalCollider?.shape.points == null) return;

		var color = new Color(184, 248, 248);
		DrawWrappers.DrawPolygon(getPoints(), color, true, bb?.zIndex ?? zIndex);

		base.render(x, y);
		Global.sprites["bbuffalo_proj_beam_head"].draw(0, startPos.x, startPos.y, -xDir, 1, null, 1, 1, 1, zIndex);
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();

		customData.AddRange(BitConverter.GetBytes(startPos.x));
		customData.AddRange(BitConverter.GetBytes(startPos.y));

		return customData;
	}
	public override void updateCustomActorNetData(byte[] data) {
		float startX = BitConverter.ToSingle(data[0..4], 0);
		float startY = BitConverter.ToSingle(data[4..8], 0);

		setStartPos(new Point(startX, startY));
	}
}
#region states
public class BuffaloMState : MaverickState {
	public BlizzardBuffalo frozenBuffalio = null!;
	public BuffaloMState(
		string sprite, string transitionSprite = ""
	) : base(
		sprite, transitionSprite
	) {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		frozenBuffalio = maverick as BlizzardBuffalo ?? throw new NullReferenceException();

	}
}
public class BBuffaloShootBeamState : BuffaloMState {
	bool shotOnce;
	public Anim? muzzle;
	public BBuffaloBeamProj? proj;
	public BBuffaloShootBeamState() : base("shoot_beam", "shoot_beam_start") {
	}

	public override void update() {
		base.update();
		if (inTransition()) return;

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			muzzle = new Anim(
				shootPos.Value, "bbuffalo_beam_muzzle", maverick.xDir,
				player.getNextActorNetId(), true,
				sendRpc: true, host: null
			);
			maverick.playSound("bbuffaloBeam", sendRpc: true);
		}

		if (proj != null && !proj.destroyed && input.isPressed(Control.Special1, player)) {
			proj.release();
		}
		if (muzzle?.destroyed == true && proj == null && shootPos != null) {
			proj = new BBuffaloBeamProj(
				shootPos.Value.addxy(maverick.xDir * 20, 0), maverick.xDir, 
				frozenBuffalio, frozenBuffalio, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (proj?.released == true || proj?.destroyed == true) {
			maverick.changeToIdleOrFall();
		}
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
	}
	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		muzzle?.destroySelf();
		proj?.destroySelf();
	}
}

public class BBuffaloDashState : BuffaloMState {
	float dustTime;
	Character? victim;
	public BBuffaloDashState() : base("dash", "dash_start") {
	}

	public override void update() {
		base.update();

		if (inTransition()) return;

		Helpers.decrementTime(ref dustTime);
		if (dustTime == 0) {
			new Anim(maverick.pos.addxy(-maverick.xDir * 30, 0), "dust", maverick.xDir, null, true) { vel = new Point(0, -25) };
			dustTime = 0.1f;
		}

		if (!player.ownedByLocalPlayer) return;

		var move = new Point(150 * maverick.xDir, 0);

		var hitGround = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 5, 20);
		if (hitGround == null) {
			maverick.changeToIdleOrFall();
			return;
		}

		var hitWall = Global.level.checkTerrainCollisionOnce(maverick, maverick.xDir * 20, -5);
		if (hitWall?.isSideWallHit() == true) {
			crashAndDamage();
			maverick.changeState(new MIdle());
			return;
		}

		maverick.move(move);

		if (isHoldStateOver(1, 4, 2, Control.Dash)) {
			maverick.changeToIdleOrFall();
			return;
		}
		if (player.input.isPressed(Control.Special1, player)) {
			maverick.changeSpriteFromName("dash_grab", false);
		}
	}

	public Character? getVictim() {
		if (victim == null) return null;
		if (!victim.sprite.name.EndsWith("_grabbed")) {
			return null;
		}
		return victim;
	}

	public void crashAndDamage() {
		/*
		if (getVictim() != null)
		{
			(maverick as BlizzardBuffalo).meleeWeapon.applyDamage(victim, false, maverick, (int)ProjIds.BBuffaloCrash, sendRpc: true);
		}
		*/

		new BBuffaloCrashProj(
			maverick.pos, maverick.xDir, frozenBuffalio,
			player, player.getNextActorNetId(), rpc: true
		);
		maverick.playSound("crashX3", sendRpc: true);
		maverick.shakeCamera(sendRpc: true);
	}

	public override bool trySetGrabVictim(Character grabbed) {
		if (victim == null) {
			victim = grabbed;
			maverick.changeSpriteFromName("dash_grab", false);
		}
		return true;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		victim?.releaseGrab(maverick);
	}
}

public class BBuffaloCrashProj : Projectile {
	public BBuffaloCrashProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bbuffalo_proj_crash", netId, player
	) {
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 60;
		weapon = BlizzardBuffalo.netWeapon;
		setIndestructableProperties();
		maxTime = 0.15f;
		projId = (int)ProjIds.BBuffaloCrash;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new BBuffaloCrashProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class BBuffaloDragged : GenericGrabbedState {
	public const float maxGrabTime = 4;
	public BBuffaloDragged(
		BlizzardBuffalo grabber
	) : base(
		grabber, maxGrabTime, "_dash", reverseZIndex: true,
		freeOnHitWall: false, lerp: true, additionalGrabSprite: "_dash_grab"
	) {
	}
}

public class BBuffaloShootAI : BuffaloMState {
	public bool isStriker;
	public BBuffaloShootAI(bool isStriker) : base("shoot") {
		this.isStriker = isStriker;
	}

	public override void update() {
		base.update();
		Point? shootPos = maverick.getFirstPOI();
		if (!isStriker && shootPos != null && !once) {
			once = true;
			new BBuffaloIceProjAI(
				shootPos.Value, maverick.xDir, 0,
				frozenBuffalio, player.getNextActorNetId(), sendRpc: true
			);
			new BBuffaloIceProjAI(
				shootPos.Value, maverick.xDir, 1,
				frozenBuffalio, player.getNextActorNetId(), sendRpc: true
			);
			maverick.playSound("bbuffaloShoot", sendRpc: true);
		}
		else if (isStriker && shootPos != null && !once) {
			once = true;
			new BBuffaloIceProjAIStriker(
				shootPos.Value, maverick.xDir, 0,
				frozenBuffalio, player.getNextActorNetId(), sendRpc: true
			);
			new BBuffaloIceProjAIStriker(
				shootPos.Value, maverick.xDir, 1,
				frozenBuffalio, player.getNextActorNetId(), sendRpc: true
			);
			new BBuffaloIceProjAIStriker(
				shootPos.Value, maverick.xDir, 2,
				frozenBuffalio, player.getNextActorNetId(), sendRpc: true
			);
			maverick.playSound("bbuffaloShoot", sendRpc: true);
		}
		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
}
public class BBuffaloIceProjAI : Projectile {
	public int type;
	public BBuffaloIceProjAI(
		Point pos, int xDir, int type,
		Actor owner, ushort netProjId, bool sendRpc = false
	) : base(
		pos, xDir, owner, "bbuffalo_proj_iceball", netProjId
	) {
		damager.damage = 3f;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 1;
		weapon = BlizzardBuffalo.netWeapon;
		projId = (int)ProjIds.BBuffaloIceProjAI;
		maxTime = 1.5f;
		useGravity = true;
		this.type = type;
		if (type == 0) vel = new Point(Helpers.randomRange(150,210) * owner.xDir, -Helpers.randomRange(250,300));
		else if (type == 1) vel = new Point(Helpers.randomRange(210,250) * owner.xDir, -Helpers.randomRange(300,350));
		if (collider != null) { collider.wallOnly = true; }
		destroyOnHit = true;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}

	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BBuffaloIceProjAI(
			args.pos, args.xDir, args.extraData[0], args.owner, args.netId
		);
	}

	public override void onStart() {
		base.onStart();

		if (checkCollision(0, 0) != null) {
			destroySelf();
			Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner);
			playSound("iceBreak");
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		destroySelf();
		if (!ownedByLocalPlayer) return;
		var hitNormal = other.getNormalSafe();
		new BBuffaloIceProjGround(
			other.getHitPointSafe(), 1, hitNormal.byteAngle, this,
			owner, owner.getNextActorNetId(), rpc: true
		);
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!ownedByLocalPlayer) return;

		destroySelf();
		Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner, sendRpc: true);
		playSound("iceBreak", sendRpc: true);
	}
}
public class BuffaloJumpAI : MaverickState {
	public BuffaloJumpAI() : base("jump", "jump_start") {
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
public class BBuffaloIceProjAIStriker : Projectile {
	public int type;
	public BBuffaloIceProjAIStriker(
		Point pos, int xDir, int type,
		Actor owner, ushort netProjId, bool sendRpc = false
	) : base(
		pos, xDir, owner, "bbuffalo_proj_iceball", netProjId
	) {
		damager.damage = 3f;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 1;
		weapon = BlizzardBuffalo.netWeapon;
		projId = (int)ProjIds.BBuffaloIceProjAIStriker;
		maxTime = 1.5f;
		useGravity = true;
		this.type = type;
		if (type == 0) vel = new Point(Helpers.randomRange(50,90), -Helpers.randomRange(270,300));
		else if (type == 1) vel = new Point(Helpers.randomRange(140,180), -Helpers.randomRange(300,350));
		else if (type == 2) vel = new Point(Helpers.randomRange(230,270), -Helpers.randomRange(300,350));
		if (collider != null) { collider.wallOnly = true; }
		destroyOnHit = true;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}

	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BBuffaloIceProjAIStriker(
			args.pos, args.xDir, args.extraData[0], args.owner, args.netId
		);
	}

	public override void onStart() {
		base.onStart();

		if (checkCollision(0, 0) != null) {
			destroySelf();
			Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner);
			playSound("iceBreak");
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		destroySelf();
		if (!ownedByLocalPlayer) return;
		var hitNormal = other.getNormalSafe();
		new BBuffaloIceProjGround(
			other.getHitPointSafe(), 1, hitNormal.byteAngle, this,
			owner, owner.getNextActorNetId(), rpc: true
		);
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!ownedByLocalPlayer) return;

		destroySelf();
		Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner, sendRpc: true);
		playSound("iceBreak", sendRpc: true);
	}
}
#endregion
