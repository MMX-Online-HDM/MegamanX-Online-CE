namespace MMXOnline;

public class PunchyZeroMeleeWeapon : Weapon {
	public static PunchyZeroMeleeWeapon netWeapon = new();

	public PunchyZeroMeleeWeapon() : base() {
		index = (int)WeaponIds.PunchyZSaberProjSwing;
		killFeedIndex = 9;
	}
}
public class PunchyZeroMeleeWave : Projectile {
	public PunchyZeroMeleeWave(
		Point pos, int xDir, int saberId, float maxTime, float hitCooldown,
 		bool isAZ, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "zsaber_shot", netId, player
	) {
		weapon = PunchyZeroMeleeWeapon.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 15;
		vel = new Point(300 * xDir, 0);
		if (maxTime > 4) {
			maxTime = 4;
		}
		isMelee = true;
		reflectable = true;
		destroyOnHit = true;
		this.maxTime = maxTime;
		//projId = (int)ProjIds.PZeroHadangeki;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir,
			new byte[] { 
				(byte)saberId,
				(byte)MathInt.Ceiling(maxTime * 60f),
				(byte)MathInt.Floor(hitCooldown * 60f),
				isAZ ? (byte)1 : (byte)0});
		}
		projId = saberId;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new PunchyZeroMeleeWave(
			args.pos, args.xDir,
			args.extraData[0], args.extraData[1] - 60f,
			args.extraData[2] - 60f,
			args.extraData[3] == 1,
			args.owner, args.player, args.netId
		);
	}
}


public class PZeroHadangeki : Projectile {
	public PZeroHadangeki(
		Point pos, int xDir, bool isAZ, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "zsaber_shot", netId, player
	) {
		weapon = PunchyZeroMeleeWeapon.netWeapon;
		damager.damage = 3;
		vel = new Point(350 * xDir, 0);
		fadeSprite = "zsaber_shot_fade";
		fadeOnAutoDestroy = true;
		reflectable = true;
		destroyOnHit = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.PZeroHadangeki;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir,
			new byte[] {isAZ ? (byte)1 : (byte)0});
		}
		if (isAZ) {
			genericShader = player.zeroAzPaletteShader;
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new PZeroHadangeki(
			args.pos, args.xDir, args.extraData[0] == 1, args.owner, args.player, args.netId
		);
	}

}
