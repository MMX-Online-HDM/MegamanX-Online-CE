using System;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;


public class ZeroGroundPunches : CharState {
	[AllowNull]
	PunchyZero zero;
	private int attackNum;

	private float projMaxTime;
	private int projId;
	private int comboFrame = Int32.MaxValue;

	private string sound = "";
	private bool soundPlayed;
	private int soundFrame = Int32.MaxValue;

	public ZeroGroundPunches(int attackNum) : base(getSpr(attackNum)) {
		switch (attackNum) {
			case 0:
				sound = "punch1";
				projMaxTime = 0.2f;
				projId = (int)ProjIds.PZeroPunch;
				soundFrame = 2;
				comboFrame = 3;
				break;
			case 1:
				sound = "punch2";
				projMaxTime = 0.2f;
				projId = (int)ProjIds.PZeroPunch2;
				soundFrame = 2;
				comboFrame = 4;
				break;
		}
		this.attackNum = attackNum;
	}

	public override void update() {
		base.update();
		if (character.sprite.frameIndex >= soundFrame && !soundPlayed) {
			character.playSound(sound, forcePlay: false, sendRpc: true);
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
		zero = character as PunchyZero;
	}

	public static string getSpr(int attackNum) {
		return attackNum switch {
			1 => "punch2",
			_ => "punch"
		};
	}
}
