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
		Projectile? proj = sprite.name switch {
			"zero_punch" => new GenericMeleeProj(
				meleeWeapon, centerPoint, ProjIds.PZeroPunch, player, 2, 0, 0.25f
			),
			"zero_punch2" => new GenericMeleeProj(
				meleeWeapon, centerPoint, ProjIds.PZeroPunch2, player, 2, Global.halfFlinch, 0.25f
			),
			"zero_spinkick" => new GenericMeleeProj(
				meleeWeapon, centerPoint, ProjIds.PZeroPunch2, player, 2, Global.halfFlinch, 0.5f
			),
			"zero_kick_air" => new GenericMeleeProj(
				meleeWeapon, centerPoint, ProjIds.PZeroAirKick, player, 3, 0, 0.25f
			),
			"zero_parry_start" => new GenericMeleeProj(
				parryWeapon, centerPoint, ProjIds.PZeroParryStart, player, 0, Global.defFlinch, 0.25f
			),
			"zero_parry" => new GenericMeleeProj(
				parryWeapon, centerPoint, ProjIds.PZeroParryAttack, player, 4, Global.defFlinch, 0.25f
			),
			"zero_shoryuken" => new GenericMeleeProj(
				meleeWeapon, centerPoint, ProjIds.PZeroShoryuken, player, 4, Global.defFlinch, 0.25f
			),
			"zero_megapunch" => new GenericMeleeProj(
				meleeWeapon, centerPoint, ProjIds.PZeroYoudantotsu, player, 6, Global.defFlinch, 0.25f
			),
			"zero_dropkick" => new GenericMeleeProj(
				meleeWeapon, centerPoint, ProjIds.PZeroEnkoukyaku, player, 4, Global.halfFlinch, 0.25f
			),
			_ => null
		};
		if (proj != null) {
			return proj;
		}
		return null;
	}
}
