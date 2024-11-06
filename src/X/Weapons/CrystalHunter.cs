using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class CrystalHunter : Weapon {
	public static CrystalHunter netWeapon = new();

	public CrystalHunter() : base() {
		shootSounds = new string[] { "crystalHunter", "crystalHunter", "crystalHunter", "crystalHunterCharged" };
		fireRate = 75;
		index = (int)WeaponIds.CrystalHunter;
		weaponBarBaseIndex = 9;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 9;
		killFeedIndex = 20;
		weaknessIndex = (int)WeaponIds.MagnetMine;
		//switchCooldown = 0.5f;
		switchCooldownFrames = 30;
		damage = "0-3/0";
		effect = "Crystalize enemies. C: Slows down the area.";
		hitcooldown = "0-1/0";
		Flinch = "0-26/0";
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel < 3) return 2;
		return 8;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new CrystalHunterProj(this, pos, xDir, player, player.getNextActorNetId(), rpc: true);
		} else {
			int amount = Global.level.chargedCrystalHunters.Count(c => c.owner == player);
			if (amount >= 1) {
				for (int i = Global.level.chargedCrystalHunters.Count - 1; i >= 0; i--) {
					var cch = Global.level.chargedCrystalHunters[i];
					if (cch.owner == player) {
						cch.destroySelf();
						amount--;
						if (amount < 2) break;
					}
				}
			}

			new CrystalHunterCharged(pos, player, player.getNextActorNetId(), player.ownedByLocalPlayer, sendRpc: true);
		}
	}
}

public class CrystalHunterProj : Projectile {
	public CrystalHunterProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId,  bool rpc = false
	) : base(
		weapon, pos, xDir, 250, 0, player, "crystalhunter_proj", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		useGravity = true;
		destroyOnHit = true;
		reflectable = true;
		gravityModifier = 0.4f;
		projId = (int)ProjIds.CrystalHunter;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new CrystalHunterProj(
			CrystalHunter.netWeapon, arg.pos, 
			arg.xDir, arg.player, arg.netId
		);
	}
}

public class CrystalHunterCharged : Actor {
	public float time;
	public Player owner;
	public ShaderWrapper? timeSlowShader;
	public const int radius = 120;
	public float drawRadius = 120;
	public float drawAlpha = 64;
	public bool isSnails;
	float maxTime = 4;
	float soundTime;
	public CrystalHunterCharged(
		Point pos, Player owner, ushort? netId, bool ownedByLocalPlayer, 
		float? overrideTime = null, bool sendRpc = false
	) : base(
		"empty", pos, netId, ownedByLocalPlayer, false
	) {
		useGravity = false;
		this.owner = owner;
		isSnails = overrideTime != null;

		if (Options.main.enablePostProcessing) {
			timeSlowShader = owner.timeSlowShader;
		}

		Global.level.chargedCrystalHunters.Add(this);

		if (isSnails) {
			maxTime = overrideTime!.Value;
		}

		netOwner = owner;
		netActorCreateId = NetActorCreateId.CrystalHunterCharged;
		if (sendRpc) {
			createActorRpc(owner.id);
		}

		canBeLocal = true;
	}

	public override void update() {
		base.update();

		var screenCoords = new Point(pos.x - Global.level.camX, pos.y - Global.level.camY);
		var normalizedCoords = new Point(screenCoords.x / Global.viewScreenW, 1 - screenCoords.y / Global.viewScreenH);

		//if (isSnails) {
		Helpers.decrementFrames(ref soundTime);
		if (soundTime == 0) {
			playSound("csnailSlowLoop");
			soundTime = 65;
		}
		//} Why only snail gets the cool sound???

		if (timeSlowShader != null) {
			timeSlowShader.SetUniform("x", normalizedCoords.x);
			timeSlowShader.SetUniform("y", normalizedCoords.y);
			timeSlowShader.SetUniform("t", Global.time);
			if (Global.viewSize == 2) timeSlowShader.SetUniform("r", 0.25f);
			else timeSlowShader.SetUniform("r", 0.5f);
		}

		if (timeSlowShader == null) {
			drawRadius = 120 + 0.5f * MathF.Sin(Global.time * 10);
			drawAlpha = 64f + 32f * MathF.Sin(Global.time * 10);
		}
		time += Global.spf;
		if (time > maxTime) {
			destroySelf(disableRpc: true);
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		Global.level.chargedCrystalHunters.Remove(this);
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (timeSlowShader == null) {
			var fillColor = new Color(96, 80, 240, Helpers.toByte(drawAlpha));
			var lineColor = new Color(208, 200, 240, Helpers.toByte(drawAlpha));
			if (owner.alliance == GameMode.redAlliance && Global.level?.gameMode?.isTeamMode == true) {
				fillColor = new Color(240, 80, 96, Helpers.toByte(drawAlpha));
			}

			//if (Global.isOnFrameCycle(20))
			{
				DrawWrappers.DrawCircle(pos.x, pos.y, drawRadius, true, fillColor, 0, ZIndex.Character - 1, pointCount: 50u);
				//DrawWrappers.DrawCircle(pos.x, pos.y, drawRadius, false, new Color(208, 200, 240, Helpers.toByte(drawAlpha)), 1f, ZIndex.Character - 1, pointCount: 50u);
			}

			float randY = Helpers.randomRange(-1f, 1f);
			float xLen = MathF.Sqrt(1 - MathF.Pow(randY, 2)) * drawRadius;
			float randThickness = Helpers.randomRange(0.5f, 2f);
			DrawWrappers.DrawLine(pos.x - xLen, pos.y + randY * drawRadius, pos.x + xLen, pos.y + randY * drawRadius, lineColor, randThickness, ZIndex.Character - 1);
		}
	}
}
