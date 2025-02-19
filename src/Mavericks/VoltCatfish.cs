using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class VoltCatfish : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.VoltCatfish, 154); }
	public static Weapon mainWeapon = new Weapon(WeaponIds.VoltCatfish, 154);
	public static Weapon getMeleeWeapon(Player player) { return new Weapon(WeaponIds.VoltCatfish, 154); }
	public static Weapon netWeapon = new Weapon(WeaponIds.VoltCatfish, 154);

	public Weapon meleeWeapon;
	public List<VoltCTriadThunderProj> mines = new List<VoltCTriadThunderProj>();
	//public ShaderWrapper chargeShader;
	public bool bouncedOnce;

	public VoltCatfish(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
		base(player, pos, destPos, xDir, netId, ownedByLocalPlayer) {
		stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, false, 1));
		stateCooldowns.Add(typeof(VoltCTriadThunderState), new MaverickStateCooldown(false, true, 0.75f));

		weapon = getWeapon();
		meleeWeapon = getMeleeWeapon(player);

		awardWeaponId = WeaponIds.TriadThunder;
		weakWeaponId = WeaponIds.TornadoFang;
		weakMaverickWeaponId = WeaponIds.TunnelRhino;

		netActorCreateId = NetActorCreateId.VoltCatfish;
		bouncedOnce = true;

		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		usesAmmo = true;
		canHealAmmo = true;
		ammo = 0;
		maxAmmo = 32;
		grayAmmoLevel = 8;
		barIndexes = (65, 54);
		gameMavs = GameMavs.X3;
	}

	public override void update() {
		if (input.isPressed(Control.Special1, player)) {
			foreach (var mine in mines) {
			//	mine.stopMoving();
			}
		}
		base.update();
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(getShootState(false));
				} else if (input.isPressed(Control.Special1, player)) {
					if (!mines.Any(m => m.electrified)) {
						changeState(new VoltCTriadThunderState());		
					} else {
						changeState(new VoltCSuckState());
					}
				} else if (input.isPressed(Control.Dash, player)) {
					if (ammo >= 32) {
						changeState(new VoltCSpecialState());
					} else if (ammo >= 8) {
						changeState(new VoltCUpBeamState());
					}
				}
			} else if (state is MJump || state is MFall) {
			}
		}
	}

	public override string getMaverickPrefix() {
		return "voltc";
	}

	public override MaverickState getRandomAttackState() {
		return aiAttackStates().GetRandomItem();
	}

	public override MaverickState[] aiAttackStates() {
		var states = new List<MaverickState>
		{
				getShootState(true),
				getSpecialState(),
				new VoltCUpBeamState(),
			};

		return states.ToArray();
	}

	public MaverickState getSpecialState() {
		if (!mines.Any(m => m.electrified)) {
			return new VoltCTriadThunderState();
		} else {
			return new VoltCSuckState();
		}
	}

	public MaverickState? getDashState() {
		if (ammo >= 32) {
			return new VoltCSpecialState();
		} else if (ammo >= 8) {
			return new VoltCUpBeamState();
		} else {
			return null;
		}
	}

	public override List<ShaderWrapper> getShaders() {
		if (player.catfishChargeShader == null || !sprite.name.EndsWith("_charge")) return new List<ShaderWrapper>();

		if (Global.isOnFrameCycle(4)) {
			player.catfishChargeShader.SetUniform("palette", 1);
		} else {
			player.catfishChargeShader.SetUniform("palette", 2);
		}
		return new List<ShaderWrapper>() { player.catfishChargeShader };
	}

	public MaverickState getShootState(bool isAI) {
		var mshoot = new MShoot((Point pos, int xDir) => {
			new TriadThunderProjCharged(pos, xDir, 2, this, player, player.getNextActorNetId(), rpc: true);
		}, "crashX3");
		return mshoot;
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Fall,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"voltc_fall" => MeleeIds.Fall,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Fall => new GenericMeleeProj(
				weapon, pos, ProjIds.VoltCStomp, player,
				3, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.VoltCStomp) {
			float damage = Helpers.clamp(MathF.Floor(deltaPos.y * 0.6125f), 1, 3);
			proj.damager.damage = damage;
		}
	}

}

public class VoltCTriadThunderProj : Projectile {
	public bool electrified;
	public VoltCatfish ElectroNamazuros;
	public int type;
	public int num = 0;

	public VoltCTriadThunderProj(
		Point pos, int xDir, int type, int num, Actor owner,
		VoltCatfish ElectroNamazuros, Player player, ushort netId, bool rpc = false
	) : base(
		pos, xDir, owner, type == 0 ? "voltc_proj_triadt_deactivated" : "voltc_proj_ball", netId, player
	) {
		weapon = VoltCatfish.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.VoltCTriadThunder;
		if (type == 1) {
			damager.flinch = Global.miniFlinch;
		}
		destroyOnHit = false;
		shouldShieldBlock = false;
		maxTime = 1.75f;
		this.ElectroNamazuros = ElectroNamazuros;
		this.type = type;
		this.num = num;
		if (num == 0) vel = new Point(150 * xDir, 75);
		if (num == 1) vel = new Point(150 * xDir, -75);
		if (num == 2) vel = new Point(150 * xDir, 0);

		if (rpc) {
			byte[] ElectroNamazurosbNetIdBytes = BitConverter.GetBytes(ElectroNamazuros.netId ?? 0);
			byte[] ElectroNamazurosTypeBytes = new byte[] { (byte)type };
			byte[] ElectroNamazurosNumBytes = new byte[] { (byte)num };

			rpcCreate(
				pos, owner, ownerPlayer,
				netId, xDir, new byte[] { 
					ElectroNamazurosTypeBytes[0], ElectroNamazurosNumBytes[0],
					ElectroNamazurosbNetIdBytes[0], ElectroNamazurosbNetIdBytes[1]
				}
			);
		}
		canBeLocal = false;
		//skull
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		ushort ElectroNamazurosId = BitConverter.ToUInt16(args.extraData, 0);
		VoltCatfish? ElectroNamazuros = Global.level.getActorByNetId(ElectroNamazurosId) as VoltCatfish;
		return new VoltCTriadThunderProj(
			args.pos, args.xDir, args.extraData[0], args.extraData[1],
			args.owner, ElectroNamazuros!,  args.player, args.netId
		);
	}

	public void electrify() {
		if (!electrified) {
			electrified = true;
			changeSprite("voltc_proj_triadt_electricity", true);
			time = 0;
		}
	}

	public override void update() {
		base.update();
		if (sprite.name == "voltc_proj_triadt_electricity") {
			damager.flinch = Global.miniFlinch;
			damager.hitCooldown = 15;
		}
		if (time > 0.75f) {
			stopMoving();
		}
		if (owner.input.isPressed(Control.Special1, owner)) {
			stopMoving();	
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;

		if (vel.isZero() && other.gameObject is VoltCTriadThunderProj ttp && 
			ttp.ownedByLocalPlayer && ttp.owner == owner && ttp.sprite.name == "voltc_proj_ball"
		) {
			forceNetUpdateNextFrame = true;
			electrify();
			ttp.destroySelf();
		} else if (
			other.gameObject is VoltCatfish vc && vc.state is VoltCSuckState && 
			vc.ownedByLocalPlayer && vc == this.ElectroNamazuros
		) {
			ElectroNamazuros.addAmmo(electrified ? 8 : 4);
			destroySelf();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;

		ElectroNamazuros?.mines.Remove(this);
	}
}

public class VoltCTriadThunderState : MaverickState {
	public VoltCatfish ElectroNamazuros = null!;
	public VoltCTriadThunderState() : base("spit") {
		exitOnAnimEnd = true;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ElectroNamazuros = maverick as VoltCatfish ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		int xDir = maverick.xDir;
		if (!once && maverick.frameIndex >= 1) {
			maverick.playSound("voltcTriadThunder", sendRpc: true);
			once = true;
			int type = (ElectroNamazuros.mines.Count == 0 ? 0 : 1);
			var proj1 = new VoltCTriadThunderProj(
					ElectroNamazuros.getCenterPos().addxy(18*xDir,-23), xDir, type, 
					0, ElectroNamazuros, ElectroNamazuros,
					player, player.getNextActorNetId(), rpc: true
				);
			var proj2 = new VoltCTriadThunderProj(
					ElectroNamazuros.getCenterPos().addxy(18*xDir,-23), xDir, type,
					1, ElectroNamazuros, ElectroNamazuros, player,
					player.getNextActorNetId(), rpc: true
				);
			var proj3 = new VoltCTriadThunderProj(
					ElectroNamazuros.getCenterPos().addxy(18*xDir,-31), xDir, type,
					2, ElectroNamazuros, ElectroNamazuros, player,
					player.getNextActorNetId(), rpc: true
				);
			if (type == 0) {
				ElectroNamazuros.mines.Add(proj1);
				ElectroNamazuros.mines.Add(proj2);
				ElectroNamazuros.mines.Add(proj3);
			}
		}
	}
}

public class VoltCSuckProj : Projectile {
	public VoltCatfish vc;

	public VoltCSuckProj(
		Point pos, int xDir, VoltCatfish vc, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "voltc_proj_suck", netId, player
	) {
		weapon = VoltCatfish.getWeapon();
		projId = (int)ProjIds.VoltCSuck;
		setIndestructableProperties();
		this.vc = vc;
		maxTime = 12;
		//Another projectile with infinite time
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, BitConverter.GetBytes(vc.netId ?? 0));
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		ushort ElectroNamazurosId = BitConverter.ToUInt16(args.extraData, 0);
		VoltCatfish? ElectroNamazuros = Global.level.getActorByNetId(ElectroNamazurosId) as VoltCatfish;

		return new VoltCSuckProj(
		args.pos, args.xDir, ElectroNamazuros!, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (vc.state is not VoltCSuckState) {
			destroySelf();
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (other.gameObject is VoltCTriadThunderProj ttp && ttp.ownedByLocalPlayer && ttp.owner == owner) {
			ttp.moveToPos(vc.getFirstPOIOrDefault(), 150);
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (vc == null) return;
		if (!damagable.isPlayableDamagable()) { return; }
		if (damagable is not Actor actor || !actor.ownedByLocalPlayer) {
			return;
		}
		if (damagable is Character chr) {
			if (!chr.ownedByLocalPlayer) return;
			if (chr.isPushImmune()) return;
		}
		actor.moveToPos(vc.getFirstPOIOrDefault(), 150);
	}
}

public class VoltCSuckState : MaverickState {
	float partTime;
	VoltCSuckProj? suckProj;
	public VoltCatfish ElectroNamazuros = null!;
	public VoltCSuckState() : base("suck") {
	}

	public override void update() {
		base.update();
		partTime += Global.spf;
		if (partTime > 0.1f) {
			partTime = 0;
			float randX = Helpers.randomRange(25, 50);
			float randY = Helpers.randomRange(-25, 25);
			var spawnPos = maverick.pos.addxy(randX * maverick.xDir, -14 + randY);
			var spawnVel = spawnPos.directionToNorm(maverick.getFirstPOIOrDefault()).times(150);
			new Anim(spawnPos, "voltc_particle_suck", 1, player.getNextActorNetId(), false, sendRpc: true) {
				vel = spawnVel,
				ttl = 0.15f,
			};
		}

		if (isHoldStateOver(0.5f, 4, 2, Control.Special1)) {
			maverick.changeToIdleOrFall();
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ElectroNamazuros = maverick as VoltCatfish ?? throw new NullReferenceException();
		suckProj = new VoltCSuckProj(
			maverick.pos.addxy(maverick.xDir * 75, 0), maverick.xDir,
			ElectroNamazuros, ElectroNamazuros, player, player.getNextActorNetId(), rpc: true);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		suckProj?.destroySelf();
	}
}


public class VoltCUpBeamProj : Projectile {
	public VoltCUpBeamProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "voltc_proj_thunder_small", netId, player
	) {
		weapon = VoltCatfish.getWeapon();		
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.VoltCUpBeam;
		destroyOnHit = false;
		shouldShieldBlock = false;
		if (type == 0) {
			vel = new Point(0, -150);
			maxTime = 0.675f;
		} else {
			vel = new Point(0, 450);
			maxTime = 0.25f;
			projId = (int)ProjIds.VoltCUpBeam2;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new VoltCUpBeamProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (loopCount >= 2 && sprite.name == "voltc_proj_thunder_small") {
			changeSprite("voltc_proj_thunder_medium", true);
		} else if (loopCount >= 2 && sprite.name == "voltc_proj_thunder_medium") {
			changeSprite("voltc_proj_thunder_big", true);
		}
	}
}

public class VoltCUpBeamState : MaverickState {
	public VoltCatfish ElectroNamazuros = null!;
	public VoltCUpBeamState() : base("thunder_vertical") {
		exitOnAnimEnd = true;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ElectroNamazuros = maverick as VoltCatfish ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		Point? shootPos = maverick.getFirstPOI(0);
		Point? shootPos2 = maverick.getFirstPOI(1);
		if (!once && shootPos != null && shootPos2 != null) {
			once = true;
			maverick.playSound("voltcWeakBolt", sendRpc: true);
			if (isAI || maverick.ammo >= 8) {
				if (!isAI) ElectroNamazuros.deductAmmo(8);
				new VoltCUpBeamProj(
					shootPos.Value, maverick.xDir, 0, ElectroNamazuros,
					player, player.getNextActorNetId(), rpc: true
				);
			}
			if (isAI || maverick.ammo >= 8) {
				if (!isAI) ElectroNamazuros.deductAmmo(8);
				new VoltCUpBeamProj(
					shootPos2.Value, maverick.xDir, 0, ElectroNamazuros,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}
	}
}

public class VoltCChargeProj : Projectile {
	public VoltCChargeProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "voltc_proj_charge", netId, player
	) {
		weapon = VoltCatfish.getWeapon();
		damager.damage = 2;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.VoltCCharge;
		setIndestructableProperties();
		maxTime = 12;
		//Another projectile with infinite time
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VoltCChargeProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class VoltCBarrierProj : Projectile {
	public VoltCBarrierProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "voltc_proj_wall", netId, player
	) {
		weapon = VoltCatfish.getWeapon();
		damager.damage = 2;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.VoltCBarrier;
		setIndestructableProperties();
		isShield = true;
		maxTime = 12;
		//Another projectile with infinite time
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VoltCBarrierProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class VoltCSparkleProj : Projectile {
	public VoltCSparkleProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, type == 0 ? "voltc_proj_sparkle" : "voltc_proj_sparkle2", netId, player
	) {
		weapon = VoltCatfish.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.VoltCSparkle;
		vel = new Point(Helpers.randomRange(-150, 150), Helpers.randomRange(-400, -200));
		useGravity = true;
		maxTime = 0.75f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new VoltCSparkleProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}
}

public class VoltCSpecialState : MaverickState {
	int state = 0;
	VoltCUpBeamProj? upBeamProj;
	VoltCChargeProj? chargeProj1;
	VoltCChargeProj? chargeProj2;
	VoltCBarrierProj? barrierProj1;
	VoltCBarrierProj? barrierProj2;
	const float drainAmmoRate = 6;
	VoltCatfish ElectroNamazuros = null!;
	public VoltCSpecialState() : base("charge_start") {
		superArmor = true;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		ElectroNamazuros = maverick as VoltCatfish ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		if (state == 0) {
			maverick.ammo -= 8;
			var beamPos = maverick.pos.addxy(0, -150);
			upBeamProj = new VoltCUpBeamProj(
				beamPos, maverick.xDir, 1, ElectroNamazuros,
				player, player.getNextActorNetId(), rpc: true
			);
			maverick.playSound("voltcStrongBolt", sendRpc: true);
			state = 1;
		} else if (state == 1) {
			maverick.drainAmmo(drainAmmoRate);
			if (upBeamProj?.destroyed == true) {
				upBeamProj = null;
				chargeProj2 = new VoltCChargeProj(
					maverick.getFirstPOIOrDefault(1), maverick.xDir, ElectroNamazuros,
					player, player.getNextActorNetId(), rpc: true
				);
				chargeProj1 = new VoltCChargeProj(
					maverick.getFirstPOIOrDefault(0), maverick.xDir, ElectroNamazuros, 
					player, player.getNextActorNetId(), rpc: true
				);
				stateTime = 0;
				maverick.playSound("voltcStatic", sendRpc: true);
				maverick.changeSpriteFromName("charge", true);
				state = 2;
			}
		} else if (state == 2) {
			maverick.drainAmmo(drainAmmoRate);
			updateChargeProjs();
			if (stateTime > 0.5f) {
				stateTime = 0;
				state = 3;
				barrierProj1 = new VoltCBarrierProj(
					maverick.getFirstPOIOrDefault(2), maverick.xDir, ElectroNamazuros,
					player, player.getNextActorNetId(), rpc: true
				);
				barrierProj2 = new VoltCBarrierProj(
					maverick.getFirstPOIOrDefault(3), maverick.xDir, ElectroNamazuros,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		} else if (state == 3) {
			maverick.drainAmmo(drainAmmoRate);
			updateChargeProjs();
			updateBarrierProjs();
			spawnParticles();
			if (maverick.ammo <= 0 || isHoldStateOver(1, float.MaxValue, 2, Control.Dash)) {
				maverick.changeToIdleOrFall();
			}
		}
	}

	float partTime;
	float partSoundTime;
	float shakecameraTime;
	public void spawnParticles() {
		int type = Helpers.randomRange(0, 1);
		partTime += Global.spf;
		if (partTime > 15f/60f) {
			partTime = 0;
			new VoltCSparkleProj(
				maverick.getFirstPOIOrDefault(0), maverick.xDir, type,
				ElectroNamazuros, player, player.getNextActorNetId(), rpc: true
			);
		}
		Helpers.decrementTime(ref partSoundTime);
		if (partSoundTime <= 0) {
			partSoundTime = 0.5f;
			maverick.playSound("crashX3", sendRpc: true);
		}
		Helpers.decrementTime(ref shakecameraTime);
		if (shakecameraTime <= 0) {
			shakecameraTime = 0.2f;
			maverick.shakeCamera(sendRpc: true);
		}
	}

	public void updateChargeProjs() {
		chargeProj1?.changePos(maverick.getFirstPOIOrDefault(0));
		chargeProj2?.changePos(maverick.getFirstPOIOrDefault(1));
	}

	public void updateBarrierProjs() {
		barrierProj1?.changePos(maverick.getFirstPOIOrDefault(2));
		barrierProj2?.changePos(maverick.getFirstPOIOrDefault(3));
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		chargeProj1?.destroySelf();
		chargeProj2?.destroySelf();
		barrierProj1?.destroySelf();
		barrierProj2?.destroySelf();
	}
}

public class VoltCBounce : MaverickState {
	public VoltCBounce() : base("jump") {
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();

		if (maverick.vel.y * maverick.getYMod() > 0) {
			maverick.changeState(new MFall());
			return;
		}
		airCode();
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.vel.y = -maverick.getJumpPower() * 0.4f;
	}
}
