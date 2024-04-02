using System;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class PunchyZeroMeleeWeapon : Weapon {
	public static PunchyZeroMeleeWeapon netWeapon = new();
}

public class AwakenedSaberProj : Projectile {
	public AwakenedSaberProj(
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
		projId = (int)ProjIds.AwakenedZeroSaber;
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
