using System;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;


public abstract class ZeroGroundPunches : CharState {
	[AllowNull]
	PunchyZero zero;
	private bool fired;

	private string sound = "";
	private int attackNum;

	private int projFrame;
	private float projMaxTime;
	private int projId;
	private int comboFrame = Int32.MaxValue;

	public ZeroGroundPunches(int attackNum) : base(getSpr(attackNum)) {
		switch (attackNum) {
			case 0:
				sound = "punch1";
				projMaxTime = 0.2f;
				projId = (int)ProjIds.PZeroPunch;
				comboFrame = 3;
				projFrame = 3;
				break;
			case 1:
				sound = "punch2";
				projMaxTime = 0.2f;
				projId = (int)ProjIds.PZeroPunch2;
				comboFrame = 4;
				projFrame = 3;
				break;
		}
		this.attackNum = attackNum;
	}

	public override void update() {
		base.update();
		if (zero.isAwakened && character.frameIndex >= projFrame && !fired) {
			fired = true;
			character.playSound("saberShot", forcePlay: false, sendRpc: true);
			Projectile? saberHitbox = character.getProjFromHitbox(null, character.pos);
			if (saberHitbox != null) {
				saberHitbox.destroySelf();
				/*new AwakenedSaberProj(
					projId, projMaxTime,
					player.zSaberWeapon,
					character.pos.addxy(30 * character.xDir, -20f),
					character.xDir,
					player,
					player.getNextActorNetId(),
					rpc: true
				);*/
			}
		}
		if (character.sprite.frameIndex >= comboFrame) {
			altAttackCtrls[0] = true;
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.turnToInput(player.input, player);
		character.playSound(sound, forcePlay: false, sendRpc: true);
		zero = character as PunchyZero;
	}

	public static string getSpr(int attackNum) {
		return attackNum switch {
			1 => "punch2",
			_ => "punch"
		};
	}
}
