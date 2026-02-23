using System;

namespace MMXOnline;

public class WolfSigma : Character {
	public WolfSigmaHead head;
	public WolfSigmaHand leftHand;
	public WolfSigmaHand rightHand;
	public Point? sigmaHeadGroundCamCenterPos;

	public WolfSigma(
		Player player, float x, float y, int xDir, bool isVisible,
		ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = false
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.WolfSigma;
		altSoundId = AltSoundIds.X1;

		bool isRevive = true;

		if (!ownedByLocalPlayer) {
			visible = true;
		} else {
			if (isRevive) {
				useGravity = false;
				changeSprite("sigma_wolf_head", true);
				changeState(new WolfSigmaRevive(player.explodeDieEffect), true);
			} else {
				visible = true;
				changeSprite("sigma_wolf_head", true);
				changeState(new WolfSigmaHeadState(), true);
			}
		}
	}


	public override void update() {
		player.changeWeaponControls();

	}
	public override bool isSoundCentered() {
		return false;
	}

	public override Point getCenterPos() {
		return pos.addxy(0, -7);
	}

	public override float getLabelOffY() {
		return 25;
	}

	public override void destroySelf(
		string spriteName = null, string fadeSound = null, bool disableRpc = false,
		bool doRpcEvenIfNotOwned = false, bool favorDefenderProjDestroy = false
	) {
		head?.explode();
		leftHand?.destroySelf();
		rightHand?.destroySelf();
		destroyMusicSource();
		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy);
	}
	

		public override int getMaxHealth() {
		if (isATrans) {
			return base.getMaxHealth();
		}
		return MathInt.Ceiling(Player.getModifiedHealth(32) * Player.getHpMod());
	}

	public override string getSprite(string spriteName) {
		if (Global.sprites.ContainsKey("sigma_wolf_" + spriteName)) {
			return "sigma_wolf_" + spriteName;
		}
		return "sigma_" + spriteName;
		// Do not Remove this bc it basically prevents shenanigans that would softlock him
	}

	public override Point getCamCenterPos(bool ignoreZoom = false) {
		if (player.weapon is WolfSigmaHandWeapon handWeapon && handWeapon.hand.isControlling) {
			var hand = handWeapon.hand;
			Point camCenter = sigmaHeadGroundCamCenterPos ?? getCenterPos();
			if (hand.pos.x > camCenter.x + Global.halfScreenW ||
				hand.pos.x < camCenter.x - Global.halfScreenW ||
				hand.pos.y > camCenter.y + Global.halfScreenH ||
				hand.pos.y < camCenter.y - Global.halfScreenH
			) {
				float overFactorX = MathF.Abs(hand.pos.x - camCenter.x) - Global.halfScreenW;
				if (overFactorX > 0) {
					float remainder = overFactorX - Global.halfScreenW;
					int sign = MathF.Sign(hand.pos.x - camCenter.x);
					camCenter.x += Math.Min(overFactorX, Global.halfScreenW) * sign * 2;
					camCenter.x += Math.Max(remainder, 0) * sign;
				}

				float overFactorY = MathF.Abs(hand.pos.y - camCenter.y) - Global.halfScreenH;
				if (overFactorY > 0) {
					float remainder = overFactorY - Global.halfScreenH;
					int sign = MathF.Sign(hand.pos.y - camCenter.y);
					camCenter.y += Math.Min(overFactorY, Global.halfScreenH) * sign * 2;
					camCenter.y += Math.Max(remainder, 0) * sign;
				}

				return camCenter.round();
			}
		}
		if (sigmaHeadGroundCamCenterPos != null) {
			return sigmaHeadGroundCamCenterPos.Value;
		}
		return pos.round().addxy(camOffsetX, 0);
	}

	public override void onDeath() {
		base.onDeath();
		player.lastDeathWasSigmaHyper = true;

		visible = false;
		Anim anim = new Anim(
			pos, "sigma_wolf_head_drop", 1, player.getNextActorNetId(), false, sendRpc: true
		);
		anim.useGravity = true;
		anim.ttl = 3;
		anim.blink = true;
		if (anim.collider != null) {
			anim.collider.wallOnly = true;
		}
		var ede = new ExplodeDieEffect(
			player, pos, pos, "empty", 1, zIndex, false, 20, 3, false
		);
		ede.host = anim;
		Global.level.addEffect(ede);
	}
}
