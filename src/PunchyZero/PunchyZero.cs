using System.Collections.Generic;

namespace MMXOnline;

public class PunchyZero : Character {
	public float blackZeroTime;
	public float awakenedZeroTime;
	public bool isViral;
	public bool isAwakened;
	public bool isBlack;
	public byte hypermodeBlink;
	public int hyperMode;
	public PunchyZeroMeleeWeapon meleeWeapon = new();
	public KKnuckleParry parryWeapon = new();

	public PunchyZero(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {

	}

	public override void update() {
		base.update();
		// Charge and release charge logic.
		//chargeLogic(shoot);
	}

	public override bool canCharge() {
		return (!isInvulnerableAttack());
	}

	public override bool normalCtrl() {
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		if (grounded && vel.y >= 0) {
			return groundAttacks();
		}
		return airAttacks();
	}
	
	public override bool altAttackCtrl(bool[] ctrls) {
		return false;
	}

	public bool groundAttacks() {
		if (player.input.isPressed(Control.Shoot, player)) {
			changeState(new ZeroGroundPunches(0), true);
		}
		return false;
	}

	public bool airAttacks() {
		return false;
	}

	public override bool canAirJump() {
		return dashedInAir == 0;
	}

	public override string getSprite(string spriteName) {
		return "zero_" + spriteName;
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;

		int paletteNum = 0;
		if (isBlack) {
			paletteNum = 1;
		} 
		if (paletteNum != 0) {
			palette = player.zeroPaletteShader;
			palette?.SetUniform("palette", paletteNum);
			palette?.SetUniform("paletteTexture", Global.textures["hyperZeroPalette"]);
		}
		if (isViral) {
			palette = player.nightmareZeroShader;
		}
		if (palette != null && hypermodeBlink > 0) {
			float blinkRate =  MathInt.Ceiling(hypermodeBlink / 30f);
			palette =  ((Global.frameCount % (blinkRate * 2) >= blinkRate) ? null : palette);
		}
		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}
		shaders.AddRange(baseShaders);
		return shaders;
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		int meleeId = getHitboxMeleeId(hitbox);
		if (meleeId == -1) {
			return null;
		}
		Projectile? proj = getMeleeProjById(meleeId, centerPoint);
		if (proj != null) {
			proj.meleeId = meleeId;
			proj.owningActor = this;
			return proj;
		}
		return null;
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"zero_punch" => MeleeIds.Punch,
			"zero_punch2" => MeleeIds.Punch2,
			"zero_spinkick" => MeleeIds.Spin,
			"zero_kick_air" => MeleeIds.AirKick,
			"zero_parry_start" => MeleeIds.Parry,
			"zero_parry" => MeleeIds.ParryAttack,
			"zero_shoryuken" => MeleeIds.Uppercut,
			"zero_megapunch" => MeleeIds.StrongPunch,
			"zero_dropkick" => MeleeIds.DropKick,
			_ => MeleeIds.None
		});
	}

	public Projectile? getMeleeProjById(int id, Point? pos = null, bool addToLevel = true) {
		Point projPos = pos ?? new Point(0, 0);
		Projectile? proj = id switch {
			(int)MeleeIds.Punch => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroPunch, player,
				2, 0, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Punch2 => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroPunch2, player, 2, Global.halfFlinch, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Spin => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroSenpuukyaku, player, 2, Global.halfFlinch, 0.5f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.AirKick => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroAirKick, player, 3, 0, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Parry => new GenericMeleeProj(
				parryWeapon, projPos, ProjIds.PZeroParryStart, player, 0, Global.defFlinch, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.ParryAttack => new GenericMeleeProj(
				parryWeapon, projPos, ProjIds.PZeroParryAttack, player, 4, Global.defFlinch, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Uppercut => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroShoryuken, player, 4, Global.defFlinch, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.StrongPunch => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroYoudantotsu, player, 6, Global.defFlinch, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.DropKick => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroEnkoukyaku, player, 4, Global.halfFlinch, 0.25f,
				addToLevel: addToLevel
			),
			_ => null
		};
		if (proj != null) {
			return proj;
		}
		return null;
	}

	public enum MeleeIds {
		None = -1,
		Punch,
		Punch2,
		Spin,
		StrongPunch,
		AirKick,
		Uppercut,
		DropKick,
		Parry,
		ParryAttack
	}
}
