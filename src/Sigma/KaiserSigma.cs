using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public partial class KaiserSigma : Character {
	public bool kaiserWinTauntOnce;
	public float kaiserMissileShootTime;
	public Anim kaiserExhaustL;
	public Anim kaiserExhaustR;
	public float kaiserHoverCooldown;
	public float kaiserLeftMineShootTime;
	public float kaiserRightMineShootTime;
	public int leftMineMod;
	public int rightMineMod;

	public float kaiserHoverTime;
	public float kaiserMaxHoverTime = 4;

	public string lastHyperSigmaSprite;
	public int lastHyperSigmaFrameIndex;
	public int lastHyperSigmaXDir;

	public KaiserSigma(
		Player player, float x, float y, int xDir, bool isVisible,
		ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = false
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.KaiserSigma;
	}

	public override void update() {
		base.update();

		if (charState is not Die) {
			lastHyperSigmaSprite = sprite?.name;
			lastHyperSigmaFrameIndex = frameIndex;
			lastHyperSigmaXDir = xDir;
		}

		if (!ownedByLocalPlayer) {
			return;
		}

		if (charState is KaiserSigmaBaseState ksState && ksState.showExhaust) {
			kaiserExhaustL.visible = true;
			kaiserExhaustR.visible = true;
			kaiserExhaustL.changePos(getFirstPOIOrDefault("exhaustL"));
			kaiserExhaustR.changePos(getFirstPOIOrDefault("exhaustR"));
			if (ksState.exhaustMoveDir != 0) {
				kaiserExhaustL.changeSpriteIfDifferent("kaisersigma_exhaust2", true);
				kaiserExhaustR.changeSpriteIfDifferent("kaisersigma_exhaust2", true);
				kaiserExhaustL.xDir = -ksState.exhaustMoveDir;
				kaiserExhaustR.xDir = -ksState.exhaustMoveDir;
			} else {
				kaiserExhaustL.changeSpriteIfDifferent("kaisersigma_exhaust", true);
				kaiserExhaustR.changeSpriteIfDifferent("kaisersigma_exhaust", true);
			}
		} else {
			kaiserExhaustL.visible = false;
			kaiserExhaustR.visible = false;
		}
	}

	public override bool normalCtrl() {
		return false;
	}

	public override Collider getGlobalCollider() {
		if (player.isKaiserViralSigma()) {
			if (sprite.name == "kaisersigma_virus_return") {
				return null;
			}
			var rect2 = new Rect(0, 0, 20, 32);
			return new Collider(rect2.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
		} else {
			var rect2 = new Rect(0, 0, 60, 110);
			return new Collider(rect2.getPoints(), false, this, false, false, HitboxFlag.None, new Point(0, 0));
		}
	}

	public void changeToKaiserIdleOrFall() {
		changeState(new KaiserSigmaIdleState(), true);
	}

	public bool isKaiserSigmaGrounded() {
		return charState is not KaiserSigmaHoverState && charState is not KaiserSigmaFallState;
	}

	public static bool canKaiserSpawn(Character character, out Point spawnPos) {
		// Get ground snapping pos
		Point groundPos;
		var groundHit = character.getGroundHit(Global.halfScreenH);
		if (groundHit != null) {
			groundPos = groundHit.Value;
		} else {
			spawnPos = Point.zero;
			return false;
		}

		// Check if ample space to revive in
		int w = 60;
		int h = 110;
		var rect = new Rect(
			new Point(groundPos.x - w / 2, groundPos.y - h), new Point(groundPos.x + w / 2, groundPos.y - 1)
		);

		// DrawWrappers.DrawRect(rect.x1, rect.y1, rect.x2, rect.y2, true, new Color(255, 0, 0, 64), 1, ZIndex.HUD);

		List<CollideData> hits = null;
		hits = Global.level.checkCollisionsShape(rect.getShape(), null);
		if (hits.Any(h => h.gameObject is Wall)) {
			var hitPoints = new List<Point>();
			foreach (var hit in hits) {
				if (hit?.hitData?.hitPoints == null) continue;
				hitPoints.AddRange(hit.hitData.hitPoints.Where(p => p.y > character.pos.y - 30));
			}
			if (hitPoints.Count > 0) {
				var bestHitPoint = hitPoints.OrderBy(p => p.y).First();
				float savedH = rect.h();
				rect.y2 = bestHitPoint.y - 1;
				rect.y1 = rect.y2 - savedH;
				groundPos.y = bestHitPoint.y;
			}

			//DrawWrappers.DrawRect(rect.x1, rect.y1, rect.x2, rect.y2, true, new Color(0, 0, 255, 64), 1, ZIndex.HUD);
		}

		hits = Global.level.checkCollisionsShape(rect.getShape(), null);
		if (hits.Any(h => h.gameObject is Wall)) {
			spawnPos = Point.zero;
			return false;
		}

		/*
		foreach (var player in Global.level.players) {
			Character otherChar = player?.character;
			if (otherChar != null &&
				otherChar != character && otherChar.isHyperSigmaBS.getValue() == true &&
				otherChar.player.isSigma1Or3() &&
				character.pos.distanceTo(otherChar.pos) < Global.screenW
			) {
				spawnPos = Point.zero;
				return false;
			}
		}
		*/

		spawnPos = groundPos;
		return true;
	}

	public override void destroySelf(
		string spriteName = null, string fadeSound = null,
		bool rpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {
		base.destroySelf(spriteName, fadeSound, rpc, doRpcEvenIfNotOwned);

		kaiserExhaustL?.destroySelf();
		kaiserExhaustR?.destroySelf();
	}

	public override void render(float x, float y) {
		base.render(x, y);

		if (player.isMainPlayer && player.isKaiserNonViralSigma() && kaiserHoverTime > 0) {
			float healthPct = Helpers.clamp01((kaiserMaxHoverTime - kaiserHoverTime) / kaiserMaxHoverTime);
			float sy = -70;
			float sx = 0;
			if (xDir == -1) sx = 90 - sx;
			drawFuelMeter(healthPct, sx, sy);
		}
	}

	public override Point getCenterPos() {
		if (sprite.name.StartsWith("kaisersigma_virus")) {
			return pos.addxy(0, -16);
		} else {
			return pos.addxy(0, -60);
		}
	}

	public override Point getAimCenterPos() {
		if (!player.isKaiserViralSigma()) {
			return pos.addxy(13 * xDir, -95);
		}
		return getCenterPos();
	}

	public override string getSprite(string spriteName) {
		return "kaisersigma_" + spriteName;
	}

	public override Projectile getProjFromHitbox(Collider collider, Point centerPoint) {
		if (sprite.name == "kaisersigma_fall" && collider.isAttack()) {
			return new GenericMeleeProj(
				new KaiserStompWeapon(player), centerPoint, ProjIds.Sigma3KaiserStomp, player,
				damage: 12 * getKaiserStompDamage(), flinch: Global.defFlinch, hitCooldown: 1f
			);
		} else if (collider.name == "body") {
			return new GenericMeleeProj(
				new Weapon(), centerPoint, ProjIds.Sigma3KaiserSuit, player,
				damage: 0, flinch: 0, hitCooldown: 1, isShield: true
			);
		}
		return null;
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.Sigma3KaiserStomp) {
			float damagePercent = getKaiserStompDamage();
			if (damagePercent > 0) {
				proj.damager.damage = 12 * damagePercent;
			}
		}
	}

	public override bool canPickupFlag() {
		return false;
	}
}
