using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class Doppma : BaseSigma {
	public float sigma3FireballCooldown;

	public Doppma(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId,
		bool ownedByLocalPlayer, bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn
	) {
		sigmaSaberMaxCooldown = 0.5f;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		player.sigmaFireWeapon.update();
		Helpers.decrementTime(ref sigma3FireballCooldown);
		Helpers.decrementTime(ref sigma3ShieldCooldown);
	}

	public override Collider getBlockCollider() {
		Rect rect = Rect.createFromWH(0, 0, 23, 55);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override bool attackCtrl() {
		if (isInvulnerableAttack() || player.weapon is MaverickWeapon) {
			return false;
		}
		bool attackPressed = false;
		if (player.weapon is not AssassinBullet) {
			if (player.input.isPressed(Control.Shoot, player)) {
				attackPressed = true;
				lastAttackFrame = Global.level.frameCount;
			}
		}
		framesSinceLastAttack = Global.level.frameCount - lastAttackFrame;
		bool lenientAttackPressed = (attackPressed || framesSinceLastAttack < 5);

		// Shoot button attacks.
		if (lenientAttackPressed) {
			if (charState is LadderClimb) {
				if (player.input.isHeld(Control.Left, player)) {
					xDir = -1;
				} else if (player.input.isHeld(Control.Right, player)) {
					xDir = 1;
				}
			}

			if (player.sigmaFireWeapon.shootTime == 0 && sigma3FireballCooldown == 0) {
				changeState(new Sigma3Shoot(player.input.getInputDir(player)), true);
				sigma3FireballCooldown = maxSigma3FireballCooldown;
				changeSpriteFromName(charState.shootSprite, true);
				return true;
			}
		}
		if (grounded && player.input.isPressed(Control.Special1, player) &&
			charState is not SigmaThrowShieldState && sigma3ShieldCooldown == 0
		) {
			sigma3ShieldCooldown = maxSigma3ShieldCooldown;
			changeState(new SigmaThrowShieldState(), true);
			return true;
		}
		return base.attackCtrl();
	}

	public override string getSprite(string spriteName) {
		return "sigma3_" + spriteName;
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override Projectile getProjFromHitbox(Collider collider, Point centerPoint) {
		if (collider.name == "shield") {
			return new GenericMeleeProj(
				new Weapon(), centerPoint, ProjIds.Sigma3ShieldBlock, player,
				damage: 0, flinch: 0, hitCooldown: 1, isDeflectShield: true, isShield: true
			);
		}
		return base.getProjFromHitbox(collider, centerPoint);
	}
}
