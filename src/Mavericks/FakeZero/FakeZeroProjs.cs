using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FakeZeroBusterProj : Projectile {
	public FakeZeroBusterProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "buster1", netId, altPlayer
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 1;
		vel = new Point(250 * xDir, 0);
		projId = (int)ProjIds.FakeZeroBuster;
		reflectable = true;
		maxTime = 0.5f;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroBusterProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		if (MathF.Abs(vel.x) < 360 && reflectCount == 0) {
			vel.x += Global.spf * xDir * 900f;
			if (MathF.Abs(vel.x) >= 360) {
				vel.x = (float)xDir * 360;
			}
		}
	}
}

public class FakeZeroBuster2Proj : Projectile {
	public FakeZeroBuster2Proj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "fakezero_buster_proj", netId, altPlayer
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 2;
		damager.damage = Global.miniFlinch;
		vel.x = 350 * xDir;
		projId = (int)ProjIds.FakeZeroBuster2;
		reflectable = true;
		maxTime = 0.5f;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroBuster2Proj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}
}

public class FakeZeroBusterProj3 : Projectile {
	public int type = 0;
	public FakeZeroBusterProj3(
		Point pos, int xDir, int type, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "fakezero_buster2_proj", netId, altPlayer
	) {
		weapon = FakeZero.getWeapon();
		damager.flinch = Global.halfFlinch;
		if (type == 0) {
			damager.damage = 2;
		} else if (type == 1) {
			damager.damage = 3;
		}
		vel.x = 325 * xDir;
		projId = (int)ProjIds.FakeZeroBuster3;
		maxTime = 0.75f;
		reflectable = true;
		this.type = type;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroBusterProj3(
			args.pos, args.xDir, args.extraData[0], args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();

		if (frameTime >= 36) {
			damager.flinch = Global.miniFlinch;
			damager.damage = 2;
		}
	}
}

public class FakeZeroSwordBeamProj : Projectile {
	public FakeZeroSwordBeamProj(
		Point pos, int xDir, Actor owner,
		ushort? netId, bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "fakezero_sword_proj", netId, altPlayer
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		vel = new Point(325 * xDir, 0);
		projId = (int)ProjIds.FakeZeroSwordBeam;
		maxTime = 0.75f;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroSwordBeamProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		if (frameTime >= 36) {
			damager.flinch = Global.miniFlinch;
			damager.damage = 2;
		}
	}
}

public class FakeZeroRockProj : Projectile {
	public FakeZeroRockProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "fakezero_rock", netId, altPlayer
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 6;
		projId = (int)ProjIds.FakeZeroGroundPunch;
		maxTime = 1.25f;
		useGravity = true;
		vel = new Point(0, -500);

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroRockProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}
}

public class FakeZeroMeleeProj : Projectile {
	public FakeZeroMeleeProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "fakezero_run_sword", netId, altPlayer
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.FakeZeroMelee;
		setIndestructableProperties();
		visible = false;
		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroMeleeProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();

		if (deltaPos.magnitude > 200 * Global.spf) {
			damager.damage = 4;
			damager.flinch = Global.defFlinch;
		} else if (deltaPos.magnitude <= 200 * Global.spf && deltaPos.magnitude > 150 * Global.spf) {
			damager.damage = 3;
			damager.flinch = Global.halfFlinch;
		} else {
			damager.damage = 2;
			damager.flinch = 0;
		}
	}
}
