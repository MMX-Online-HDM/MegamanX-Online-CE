using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class ViralSigma : Character {
	long originalZIndex;
	bool viralOnce;

	public float viralSigmaTackleCooldown;
	public float viralSigmaTackleMaxCooldown = 1;

	public string lastViralSprite = "";
	public int lastViralFrameIndex;
	public float lastViralAngle;
	public float viralAngle;

	public float viralSigmaBeamLength;
	public int lastViralXDir = 1;
	public Character? possessTarget;
	public float possessEnemyTime;
	public float maxPossessEnemyTime;
	public int numPossesses;

	public ViralSigma(
		Player player, float x, float y, int xDir, bool isVisible,
		ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = false
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) { 
		charId = CharIds.WolfSigma;
		altSoundId = AltSoundIds.X2;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			base.update();

			if (sprite.name.Contains("sigma2_viral")) {
				if (!viralOnce) {
					viralOnce = true;
					xScale = 0;
					yScale = 0;
					originalZIndex = zIndex;
				}

				if (sprite.name.Contains("sigma2_viral_possess")) {
					setzIndex(ZIndex.Actor);
				} else {
					setzIndex(originalZIndex);
				}
			}
			return;
		}

		Helpers.decrementTime(ref viralSigmaTackleCooldown);
		if (viralSigmaBeamLength < 1 && charState is not ViralSigmaBeamState) {
			viralSigmaBeamLength += Global.spf * 0.1f;
			if (viralSigmaBeamLength > 1) viralSigmaBeamLength = 1;
		}

		if (charState is not Die) {
			lastViralSprite = sprite.name;
			lastViralFrameIndex = frameIndex;
			lastViralAngle = angle ?? 0;

			var inputDir = player.input.getInputDir(player);
			if (inputDir.x != 0) lastViralXDir = MathF.Sign(inputDir.x);

			possessTarget = null;
			if (charState is ViralSigmaIdle) {
				getViralSigmaPossessTarget();
			}

			if (charState is not ViralSigmaRevive) {
				angle = Helpers.moveAngle(angle ?? 0, viralAngle, Global.spf * 500, snap: true);
			}
			if (player.weapons.Count >= 3) {
				if (isWading()) {
					if (player.weapons[2] is MechaniloidWeapon meW && meW.mechaniloidType != MechaniloidType.Fish) {
						player.weapons[2] = new MechaniloidWeapon(player, MechaniloidType.Fish);
					}
				} else {
					if (player.weapons[2] is MechaniloidWeapon meW && meW.mechaniloidType != MechaniloidType.Bird) {
						player.weapons[2] = new MechaniloidWeapon(player, MechaniloidType.Bird);
					}
				}
			}
		}
	}

	public void getViralSigmaPossessTarget() {
		var collideDatas = Global.level.getTriggerList(this, 0, 0);
		foreach (var collideData in collideDatas) {
			if (collideData?.gameObject is Character chr &&
				chr.canBeDamaged(player.alliance, player.id, (int)ProjIds.Sigma2ViralPossess) &&
				chr.player.canBePossessed()) {
				possessTarget = chr;
				maxPossessEnemyTime = 2 + (Helpers.clampMax(numPossesses, 4) * 0.5f);
				//2 - Helpers.progress(chr.player.health, chr.player.maxHealth);
				return;
			}
		}
	}
	
	public bool canPossess(Character target) {
		if (target == null || target.destroyed) return false;
		if (!target.player.canBePossessed()) return false;
		var collideDatas = Global.level.getTriggerList(this, 0, 0);
		foreach (var collideData in collideDatas) {
			if (collideData.gameObject is Character chr &&
				chr.canBeDamaged(player.alliance, player.id, (int)ProjIds.Sigma2ViralPossess)
			) {
				if (target == chr) {
					return true;
				}
			}
		}
		return false;
	}

		public override Dictionary<int, Func<Projectile>> getGlobalProjs() {
		var retProjs = new Dictionary<int, Func<Projectile>>();

		// TODO: Move this to viral Sigma class.
		if (sprite.name.Contains("viral_tackle") && sprite.time > 0.15f) {
			retProjs[(int)ProjIds.Sigma2ViralTackle] = () => {
				var damageCollider = getAllColliders().FirstOrDefault(c => c.isAttack());
				Point centerPoint = damageCollider.shape.getRect().center();
				Projectile proj = new GenericMeleeProj(
					new ViralSigmaTackleWeapon(player), centerPoint, ProjIds.Sigma2ViralTackle, player
				);
				proj.globalCollider = damageCollider.clone();
				return proj;
			};
		}
		return retProjs;
	}

	public override bool isSoundCentered() {
		return false;
	}

	public override Point getCenterPos() {
		return pos.addxy(0, 0);
	}

	public override float getLabelOffY() {
		return 43;
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = base.getShaders();
		ShaderWrapper? palette = null;

		int paletteNum = 6 - MathInt.Ceiling((player.health / player.maxHealth) * 6);
		if (sprite.name.Contains("_enter")) {
			paletteNum = 0;
		}
		palette = player.viralSigmaShader;
		palette?.SetUniform("palette", paletteNum);
		palette?.SetUniform("paletteTexture", Global.textures["paletteViralSigma"]);

		return shaders;
	}

	public override void render(float x, float y) {
		base.render(x, y);

		if (charState is ViralSigmaPossessStart) {
			float healthBarInnerWidth = 30;

			float progress = (possessEnemyTime / maxPossessEnemyTime);
			float width = progress * healthBarInnerWidth;

			getHealthNameOffsets(out bool shieldDrawn, ref progress);

			Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
			Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);

			DrawWrappers.DrawRect(
				topLeft.x, topLeft.y, botRight.x, botRight.y,
				true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White
			);
			DrawWrappers.DrawRect(
				topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1,
				true, Color.Yellow, 0, ZIndex.HUD - 1
			);
			Fonts.drawText(
				FontType.DarkGreen, "Possessing...", pos.x, pos.y - 15 + currentLabelY,
				Alignment.Center, true, depth: ZIndex.HUD
			);
			deductLabelY(labelCooldownOffY);
		}
	}

	public override Point getCamCenterPos(bool ignoreZoom = false) {
		return pos.round().addxy(camOffsetX, 25);
	}

	public override void onDeath() {
		base.onDeath();
		player.lastDeathWasSigmaHyper = true;

		visible = false;
		Anim anim = new Anim(
			pos, lastViralSprite, 1, player.getNextActorNetId(), false, sendRpc: true
		);
		anim.ttl = 3;
		anim.blink = true;
		anim.frameIndex = lastViralFrameIndex;
		anim.frameSpeed = 0;
		anim.angle = lastViralAngle;
		var ede = new ExplodeDieEffect(
			player, pos, pos, "empty", 1, zIndex, false, 20, 3, false
		);
		ede.host = anim;
		Global.level.addEffect(ede);
	}
}
