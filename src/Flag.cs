using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class Flag : Actor {
	public int alliance = 0;
	public Point pedestalPos;
	public Character? linkedChar;
	public float timeDropped = 0;
	public bool pickedUpOnce;
	public FlagPedestal pedestal = null!;
	public float killFeedThrottleTime;
	public float? updraftY;
	public bool nonOwnerHasUpdraft;
	public List<UpdraftParticle> particles = new List<UpdraftParticle>();
	public float pickupCooldown;
	public bool isPickedUpNet;

	public Flag(
		int alliance, Point pos, ushort? netId, bool ownedByLocalPlayer
	) : base(
		alliance == GameMode.blueAlliance ? "blue_flag" : "red_flag",
		pos, netId, ownedByLocalPlayer, false
	) {
		this.alliance = alliance;
		if (collider != null) { collider.wallOnly = true; }
		setzIndex(ZIndex.Character - 2);
		for (int i = 0; i < 4; i++) {
			particles.Add(getRandomParticle(i * (UpdraftParticle.maxTime * 0.25f)));
		}
	}

	public override void onStart() {
		CollideData? hit = Global.level.raycast(
			pos.addxy(0, -10), pos.addxy(0, 60), [typeof(Wall), typeof(Ladder)]
		);
		if (hit?.hitData?.hitPoint != null) {
			changePos(hit.hitData.hitPoint.Value);
		}
		pedestal = new FlagPedestal(alliance, pos, null, ownedByLocalPlayer);
		pedestalPos = pedestal.pos;
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref pickupCooldown);
		if (!ownedByLocalPlayer) {
			return;
		}
		if (killFeedThrottleTime > 0) {
			killFeedThrottleTime += Global.spf;
			if (killFeedThrottleTime > 1) killFeedThrottleTime = 0;
		}

		if (linkedChar != null) {
			changePos(linkedChar.getCenterPos());
			xDir = -linkedChar.xDir;
			if (!Global.level.gameObjects.Contains(linkedChar) ||
				linkedChar.isWarpOut() ||
				linkedChar.isInvulnerable() ||
				linkedChar.destroyed
			) {
				dropFlag();
			}
		} else if (linkedChar?.canKeepFlag() != true) {
			dropFlag();
		}

		if (linkedChar == null) {
			if (Global.level.gameMode.isOvertime()) {
				timeDropped += Global.spf * 5;
			} else {
				timeDropped += Global.spf;
			}
		}

		if (updraftY == null) {
			float? gottenUpdraftY = getUpdraftY();
			if (gottenUpdraftY != null) {
				updraftY = gottenUpdraftY;
				vel.y = 0;
			}
		}
		if (updraftY != null) {
			if (grounded) {
				removeUpdraft();
			} else {
				if (pos.y > updraftY) {
					gravityModifier = -1;
					if (vel.y < -100) vel.y = -100;
				} else {
					gravityModifier = 1;
				}
			}
		}

		if (timeDropped > 30 && pickedUpOnce) {
			returnFlag();
		}
	}

	public float? getUpdraftY() {
		if (grounded || linkedChar != null) {
			return null;
		}
		if (pos.y > Math.Min(Global.level.killY, Global.level.height)) {
			return Global.level.killY - 250;
		}
		var hitKillZones = Global.level.getTerrainTriggerList(this, new Point(0, 0), typeof(KillZone));
		if (hitKillZones.Count > 0 &&
			hitKillZones[0].otherCollider != null &&
			hitKillZones[0].gameObject is KillZone kz &&
			kz.killInvuln
		) {
			return hitKillZones[0].otherCollider.shape.minY - 250;
		}
		return null;
	}

	public void removeUpdraft() {
		gravityModifier = 1;
		stopMovingS();
		updraftY = null;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer ||
			other.otherCollider?.flag == (int)HitboxFlag.Hitbox ||
			pickupCooldown > 0 ||
			linkedChar != null ||
			other.gameObject is not Character newChar
		) {
			return;
		}
		// Avoid neutral alliances from capturing.
		if (newChar.player.alliance >= GameMode.neutralAlliance) {
			return;
		}
		// Take if a 3rd team.
		if (newChar.player.alliance != alliance && newChar.canPickupFlag()) {
			pickupFlag(newChar);
		}
	}

	public void pickupFlag(Character newChar) {
		newChar.onFlagPickup(this);
		linkedChar = newChar;
		if (newChar != linkedChar) {
			if (linkedChar != null) {
				linkedChar.flag = null;
			}
			Global.level.gameMode.addKillFeedEntry(
				new KillFeedEntry(
					newChar.player.name + " took flag", newChar.player.alliance, newChar.player
				)
			);
		}
		if (!newChar.ownedByLocalPlayer) {
			return;
		}
		if (newChar.ai != null && newChar.ai.aiState is FindPlayer) {
			(newChar.ai.aiState as FindPlayer)?.setDestNodePos();
		}
		removeUpdraft();
		useGravity = false;
		pickedUpOnce = true;
		timeDropped = 0;
	}

	public void dropFlag() {
		if (linkedChar == null) {
			return;
		}
		Player oldPlayer = linkedChar.player;
		linkedChar = null;
		if (!ownedByLocalPlayer) {
			return;
		}
		Global.level.gameMode.addKillFeedEntry(
			new KillFeedEntry(
				$"{oldPlayer.name} dropped flag",
				oldPlayer.alliance, oldPlayer
			), true
		);
		removeUpdraft();
		useGravity = true;
	}

	public void returnFlag() {
		removeUpdraft();
		timeDropped = 0;
		pickedUpOnce = false;
		string team = alliance == GameMode.blueAlliance ? "Blue " : "Red ";
		if (killFeedThrottleTime == 0) {
			killFeedThrottleTime += Global.spf;
			Global.level.gameMode.addKillFeedEntry(
				new KillFeedEntry(team + "flag returned", alliance), true
			);
		}
		useGravity = true;
		if (linkedChar != null) {
			linkedChar.flag = null;
		}
		linkedChar = null;
		changePos(pedestalPos);
		if (alliance == GameMode.redAlliance) {
			xDir = -1;
		} else {
			xDir = 1;
		}
	}

	public UpdraftParticle getRandomParticle(float time) {
		return new UpdraftParticle(
			new Point(
				pos.x + Helpers.randomRange(-20, 20),
				pos.y + Helpers.randomRange(50, 100)
			),
			time
		);
	}

	public bool hasUpdraft() {
		if (ownedByLocalPlayer) {
			return updraftY != null;
		} else {
			return nonOwnerHasUpdraft;
		}
	}

	public override void render(float x, float y) {
		if (hasUpdraft()) {
			for (int i = particles.Count - 1; i >= 0; i--) {
				particles[i].time += Global.spf;
				if (particles[i].time > UpdraftParticle.maxTime) {
					particles[i] = getRandomParticle(0);
				} else {
					particles[i].pos.inc(new Point(0, -Global.spf * 200));
				}
				Point pos = particles[i].pos;
				//DrawWrappers.DrawLine(
				//pos.x, pos.y, pos.x, pos.y - 20, Color.White, 1, ZIndex.Foreground, true
				//);
				DrawWrappers.DrawCircle(
					pos.x, pos.y, 1, true,
					new Color(255, 255, 255,
					(byte)(255 * (particles[i].time / UpdraftParticle.maxTime))),
					1, ZIndex.Foreground, true
				);
			}
		}

		// To avoid latency of flag not sticking to character in online
		if (linkedChar != null && !linkedChar.destroyed) {
			Point centerPos = linkedChar.pos.addxy(-10 * linkedChar.xDir, -10);
			Point renderPos = new Point(centerPos.x - MathF.Round(pos.x), centerPos.y - MathF.Round(pos.y));
			base.render(renderPos.x, renderPos.y);
			return;
		}

		if (pickedUpOnce && timeDropped > 0 && linkedChar == null) {
			drawSpinner(1 - (timeDropped / 30));
		}

		base.render(x, y);
	}

	public void drawSpinner(float progress) {
		float cx = pos.x + 8;
		float cy = pos.y - 40;
		float ang = -90;
		float radius = 3f;
		float thickness = 1.25f;
		int count = 40;

		for (int i = 0; i < count; i++) {
			float angCopy = ang;
			DrawWrappers.deferredTextDraws.Add(() => DrawWrappers.DrawCircle(
				(-Global.level.camX + cx + Helpers.cosd(angCopy) * radius) / Global.viewSize,
				(-Global.level.camY + cy + Helpers.sind(angCopy) * radius) / Global.viewSize,
				thickness / Global.viewSize, true, Color.Black, 1, ZIndex.HUD, isWorldPos: false));
			ang += (360f / count);
		}

		for (int i = 0; i < count * progress; i++) {
			float angCopy = ang;
			DrawWrappers.deferredTextDraws.Add(() => DrawWrappers.DrawCircle(
				(-Global.level.camX + cx + Helpers.cosd(angCopy) * radius) / Global.viewSize,
				(-Global.level.camY + cy + Helpers.sind(angCopy) * radius) / Global.viewSize,
				(thickness - 0.5f) / Global.viewSize, true,
				alliance == GameMode.redAlliance ? Color.Red : Color.Blue,
				1, ZIndex.HUD, isWorldPos: false)
			);
			ang += (360f / count);
		}
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = [
			.. BitConverter.GetBytes(linkedChar?.netId ?? ushort.MaxValue),
			(byte)(hasUpdraft() ? 1 : 0),
			(byte)(pickedUpOnce ? 1 : 0),
			(byte)MathF.Min(MathF.Floor(timeDropped * 8), 255),
		];
		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		ushort chrNetId = BitConverter.ToUInt16(data.AsSpan()[0..2]);
		nonOwnerHasUpdraft = data[2] == 1;
		pickedUpOnce = data[3] == 1;
		timeDropped = data[4] / 8f;

		bool wasActive = isPickedUpNet || linkedChar != null;
		if (chrNetId != ushort.MaxValue) {
			if (Global.level.getActorByNetId(chrNetId) is Character chara) {
				if (chara.flag != this) {
					pickupFlag(chara);
				}
				xDir = -chara.xDir;
				linkedChar = chara;
			}
		}
		else if (wasActive) {
			foreach (Player player in Global.level.players) {
				if (player?.character != null && player.character.flag == this) {
					player.character.flag = null;
				}
			}
			if (linkedChar != null) {
				linkedChar.flag = null;
			}
			dropFlag();
			linkedChar = null;
		}
		isPickedUpNet = linkedChar != null;
	}
}

public class UpdraftParticle {
	public Point pos;
	public float time;
	public const float maxTime = 0.25f;
	public UpdraftParticle(Point pos, float time) {
		this.pos = pos;
		this.time = time;
	}
}

public class FlagPedestal : Actor {
	public int alliance = 0;
	public FlagPedestal(
		int alliance, Point pos, ushort? netId, bool ownedByLocalPlayer
	) : base(
		"flag_pedastal", pos, netId, ownedByLocalPlayer, false
	) {
		this.alliance = alliance;
		useGravity = false;
		setzIndex(ZIndex.Character - 1);
		if (this.alliance == GameMode.blueAlliance) {
			addRenderEffect(RenderEffectType.BlueShadow);
		} else {
			addRenderEffect(RenderEffectType.RedShadow);
		}
		canBeLocal = true;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer ||
			other.otherCollider?.flag == (int)HitboxFlag.Hitbox ||
			other.gameObject is not Character chr ||
			chr.flag == null ||
			chr.player.alliance != alliance
		) {
			return;
		}
		chr.flag.returnFlag();
		chr.flag = null;
		chr.ai?.changeState(new FindPlayer(chr));
		string msg = chr.player.name + " scored";
		Global.level.gameMode.addKillFeedEntry(
			new KillFeedEntry(msg, chr.player.alliance, chr.player), true
		);
		if (Global.isHost) {
			if (alliance < GameMode.neutralAlliance) {
				Global.level.gameMode.teamPoints[alliance]++;
			}
			Global.level.gameMode.syncTeamScores();
		}
		chr.player.currency += 5;
		RPC.actorToggle.sendRpc(chr.netId, RPCActorToggleType.AwardCurrency);
	}
}
