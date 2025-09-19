using System;
using System.Collections.Generic;

namespace MMXOnline;

public class TriadThunder : Weapon {
	public static TriadThunder netWeapon = new TriadThunder();

	public TriadThunder() : base() {
		displayName = "Triad Thunder";
		shootSounds = new string[] { "triadThunder", "triadThunder", "triadThunder", "" };
		fireRate = 135;
		switchCooldown = 60;
		index = (int)WeaponIds.TriadThunder;
		weaponBarBaseIndex = 19;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 19;
		killFeedIndex = 42;
		weaknessIndex = (int)WeaponIds.TornadoFang;
		damage = "2/4+3";
		effect = "U:Projectile won't destroy on hit nor give assists.\nC:Grants Flinch Immunity.";
		hitcooldown = "30";
		flinch = "6/26";
		flinchCD = "2.25/0";
		maxAmmo = 10;
		ammo = maxAmmo;
		hasCustomChargeAnim = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) { return 2.5f; }
		return 1;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (!player.ownedByLocalPlayer) {
			return;
		}
		if (chargeLevel < 3) {
			if (player.ownedByLocalPlayer) {
				new TriadThunderProj(
					pos, xDir, player.input.isHeld(Control.Down, player) ? -1 : 1,
					mmx, player, player.getNextActorNetId(), true
				);
				player.setNextActorNetId((ushort)(player.getNextActorNetId() + 4));
			}
		} else {
			if (character != null && character.ownedByLocalPlayer) {
				character.changeState(new TriadThunderChargedState(character.grounded), true);
			}
		}
	}
}

public class TriadThunderProj : Projectile {
	int state;
	Character? character;
	MegamanX mmx = null!;
	public List<TriadThunderBall> balls;
	public TriadThunderProj(
		Point pos, int xDir, int yDir, Actor owner, Player player, ushort netProjId, bool rpc = false
	) : base(
		pos, xDir, owner, "triadthunder_proj", netProjId, player	
	) {
		weapon = TriadThunder.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 30;
		damager.flinch = Global.miniFlinch;
		projId = (int)ProjIds.TriadThunder;
		character = player.character;
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		this.yDir = yDir;
		maxTime = 1.5f;
		mmx.linkedTriadThunder = this;

		visible = false;

		// Clockwise from top
		balls = new List<TriadThunderBall>() {
			new TriadThunderBall(pos, xDir, this, player, netProjId: (ushort)(netProjId + 1)),
			new TriadThunderBall(pos, xDir, this, player, netProjId: (ushort)(netProjId + 2)),
			new TriadThunderBall(pos, xDir, this, player, netProjId: (ushort)(netProjId + 3)),
		};

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte)(yDir + 2) });
		}

		isMelee = true;
		if (ownerPlayer?.character != null) {
			ownerActor = ownerPlayer.character;
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TriadThunderProj(
			arg.pos, arg.xDir, arg.extraData[0] - 2, arg.owner, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (character != null) {
			Point incAmount = pos.directionTo(character.getCenterPos());
			incPos(incAmount);
			foreach (var ball in balls) {
				ball.incPos(incAmount);
			}
		}
		if (state == 0) {
			if (yDir == 1) {
				balls[0].move(new Point(0, -300));
				balls[1].move(new Point(250, 200));
				balls[2].move(new Point(-250, 200));
			} else {
				balls[0].move(new Point(0, 300));
				balls[1].move(new Point(250, -200));
				balls[2].move(new Point(-250, -200));
			}
			if (time > 0.125f) {
				state = 1;
				time = 0;
				foreach (var ball in balls) {
					ball.visible = false;
				}
				visible = true;
			}
		} else if (state == 1) {
			if (time > 1.33f) {
				foreach (var ball in balls) {
					ball.visible = true;
					ball.time = 0;
					ball.maxTime = 1;
					ball.startDropTime = Global.spf;
					ball.changeSprite("triadthunder_ball_drop", true);
				}
				if (ownedByLocalPlayer) {
					if (yDir == 1) {
						new TriadThunderBeam(balls[0].pos.addxy(0, 15), 0, 1, 1, owner, ownedByLocalPlayer);
						new TriadThunderBeam(balls[1].pos.addxy(-5, 0), 1, 1, 1, owner, ownedByLocalPlayer);
						new TriadThunderBeam(balls[2].pos.addxy(5, 0), 1, -1, 1, owner, ownedByLocalPlayer);
					} else {
						new TriadThunderBeam(balls[0].pos.addxy(0, -15), 0, 1, -1, owner, ownedByLocalPlayer);
						new TriadThunderBeam(balls[1].pos.addxy(-5, 0), 1, 1, -1, owner, ownedByLocalPlayer);
						new TriadThunderBeam(balls[2].pos.addxy(5, 0), 1, -1, -1, owner, ownedByLocalPlayer);
					}
				}
				destroySelf();
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.linkedTriadThunder = null;
	}
}

public class TriadThunderBall : Projectile {
	public float startDropTime;
	public TriadThunderBall(
		Point pos, int xDir, Actor owner, Player player, ushort netProjId, bool rpc = false
	) : base(
		pos, xDir, owner, "triadthunder_ball", netProjId, player	
	) {
		weapon = TriadThunder.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		damager.flinch = Global.miniFlinch;
		projId = (int)ProjIds.TriadThunder;
		destroyOnHit = false;
		shouldShieldBlock = false;
		maxTime = 2.25f;

		isMelee = true;
		if (ownerPlayer.character != null) {
			ownerActor = ownerPlayer.character;
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TriadThunderBall(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (startDropTime > 0) {
			startDropTime += Global.spf;
			if (startDropTime > 0.075f) {
				useGravity = true;
				destroySelf();
			}
		}
	}
}

public class TriadThunderBeam : Actor {
	int type;
	int count;
	float time = 1;
	Player player;
	public List<TriadThunderBeamPiece> pieces = new List<TriadThunderBeamPiece>();
	public TriadThunderBeam(
		Point pos, int type, int xDir, int yDir, Player player, bool ownedByLocalPlayer
	) : base(
		"empty", pos, null, ownedByLocalPlayer, false
	) {
		this.xDir = xDir;
		this.yDir = yDir;
		this.type = type;
		this.player = player;
		useGravity = false;
		canBeLocal = false;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		time += Global.spf;
		if (time > 0.03f && count < 5) {
			int xInc = 0;
			int yInc = -16 * yDir;
			if (type == 1) {
				xInc = 12 * xDir;
				yInc = 12 * yDir;
			}
			time = 0;
			count++;
			Point lastPos = pieces.Count > 0 ? pieces[pieces.Count - 1].pos : pos;
			pieces.Add(new TriadThunderBeamPiece(lastPos.addxy(xInc, yInc), xDir, yDir, this, player, type, player.getNextActorNetId(), rpc: true));
		}
		if (count >= 5) {
			destroySelf();
		}
	}
}

public class TriadThunderBeamPiece : Projectile {
	public int type = 0;
	public TriadThunderBeamPiece(

		Point pos, int xDir, int yDir, Actor owner, Player player, int type, ushort? netProjId, bool rpc = false
	) : base(
		pos, xDir, owner,  type == 0 ? "triadthunder_beam_up" : "triadthunder_beam_diag", netProjId, player	
	) {
		weapon = TriadThunder.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		damager.flinch = Global.miniFlinch;
		this.type = type;
		this.yDir = yDir;
		maxTime = 0.125f;
		projId = (int)ProjIds.TriadThunderBeam;
		destroyOnHit = false;
		shouldShieldBlock = false;
		if (type == 0) {
			vel = new Point(0, -300 * yDir);
		} else {
			vel = new Point(212 * xDir, 212 * yDir);
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte)(yDir + 2), (byte)type });
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TriadThunderBeamPiece(
			args.pos, args.xDir, args.extraData[0] - 2, args.owner,
			args.player, args.extraData[1], args.netId
		);
	}

	public override void update() {
		base.update();
	}
}

public class TriadThunderProjCharged : Projectile {
	public int type = 0;
	public TriadThunderProjCharged(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netProjId, bool rpc = false
	) : base(
		pos, xDir, owner, "triadthunder_charged", netProjId, player
	) {	
		weapon = type == 1 ? SparkMandrill.netWeapon : type == 2 ? VoltCatfish.netWeapon : TriadThunder.netWeapon;
		damager.damage = 4;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		projId = (int)ProjIds.TriadThunderCharged;
		this.type = type;
		if (type == 1) {
			maxTime = 1f;
			projId = (int)ProjIds.SparkMSpark;
			changeSprite("sparkm_proj_spark", true);
		} else if (type == 2) {
			projId = (int)ProjIds.VoltCBall;
			changeSprite("voltc_proj_ground_thunder", true);
			maxTime = 0.9f;
			wallCrawlSpeed = 185;
		} else {
			maxTime = 1f;
		}

		destroyOnHit = false;
		shouldShieldBlock = false;

		setupWallCrawl(new Point(xDir, 0));

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte)type });
		}
	}

	public override void update() {
		base.update();
		updateWallCrawl();
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TriadThunderProjCharged(
			arg.pos, arg.xDir, arg.extraData[0], arg.owner, arg.player, arg.netId
		);
	}
}

public class TriadThunderQuake : Projectile {
	public TriadThunderQuake(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "triadthunder_charged_quake", netId, player
	) {
		weapon = TriadThunder.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		useGravity = false;
		projId = (int)ProjIds.TriadThunderQuake;
		maxTime = 0.25f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}

		isMelee = true;
		if (ownerPlayer?.character != null) {
			ownerActor = ownerPlayer.character;
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new TriadThunderQuake(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class TriadThunderChargedState : CharState {
	bool fired = false;
	bool groundedOnce;
	public MegamanX mmx = null!;
	public TriadThunderChargedState(bool grounded) : base(!grounded ? "fall" : "punch_ground") {
		superArmor = true;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) return;
		if (!groundedOnce) {
			if (!character.grounded) {
				stateTime = 0;
				return;
			} else {
				groundedOnce = true;
				sprite = "punch_ground";
				character.changeSprite("mmx_punch_ground", true);
			}
		}
		if (character.frameIndex >= 6 && !fired) {
			fired = true;
			float x = character.pos.x;
			float y = character.pos.y;
			character.shakeCamera(sendRpc: true);
			new TriadThunderProjCharged(new Point(x, y), -1, 0, mmx, player, player.getNextActorNetId(), rpc: true);
			new TriadThunderProjCharged(new Point(x, y), 1, 0, mmx, player, player.getNextActorNetId(), rpc: true);
			new TriadThunderQuake(new Point(x, y), 1, mmx, player, player.getNextActorNetId(), rpc: true);
			character.playSound("crashX3", forcePlay: false, sendRpc: true);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		else if (stateTime > 120f/120f) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		if (character.vel.y < 0) character.vel.y = 0;
	}
}
