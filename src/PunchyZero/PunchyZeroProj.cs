namespace MMXOnline;

public class PunchyZeroMeleeWeapon : Weapon {
	public static PunchyZeroMeleeWeapon netWeapon = new();
}

public class PunchyZeroMeleeWave : Projectile {
	public PunchyZeroMeleeWave(
		Point pos, int xDir, Player player,
		int saberId, float maxTime, float hitCooldown,
		ushort netProjId, bool rpc = false
	) : base(
		PunchyZeroMeleeWeapon.netWeapon, pos, xDir,
		300f, 2f, player, "zsaber_shot", 0, 0.25f,
		netProjId, player.ownedByLocalPlayer
	) {
		if (maxTime > 4) {
			maxTime = 4;
		}
		isMelee = true;
		reflectable = true;
		destroyOnHit = true;
		this.maxTime = maxTime;
		//projId = (int)ProjIds.AwakenedZeroSaber;
		Character character = player.character;
		if (rpc) {
			rpcCreate(
				pos, player, netProjId, xDir,
				(byte)saberId, (byte)(MathInt.Ceiling(maxTime * 60f)),
				(byte)(MathInt.Floor(hitCooldown * 60f))
			);
		}
		projId = saberId;
	}
}


public class PZeroHadangeki : Projectile {
	public PZeroHadangeki(
		Point pos, int xDir, Player player,
		ushort netProjId, bool rpc = false
	) : base(
		PunchyZeroMeleeWeapon.netWeapon, pos, xDir,
		300, 3, player, "zsaber_shot", 0, 0,
		netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "zsaber_shot_fade";
		fadeOnAutoDestroy = true;
		reflectable = true;
		destroyOnHit = true;
		maxTime = 0.5f;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}
}
