using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class GigaCrush : Weapon {
	public GigaCrush() : base() {
		shootSounds = new List<string>() { "gigaCrush", "gigaCrush", "gigaCrush", "gigaCrush" };
		rateOfFire = 1;
		ammo = 0;
		index = (int)WeaponIds.GigaCrush;
		weaponBarBaseIndex = 25;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 25;
		killFeedIndex = 13;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		if (player.character.ownedByLocalPlayer) {
			player.character.changeState(new GigaCrushCharState(), true);
		}
		new GigaCrushEffect(player.character);
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 32;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return player.character?.flag == null && ammo >= (player.hasChip(3) ? 16 : 32);
	}
}

public class GigaCrushProj : Projectile {
	public float radius = 10;
	public float maxActiveTime;

	public GigaCrushProj(
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 12, player, "empty", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxActiveTime = 0.4f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		vel = new Point();
		projId = (int)ProjIds.GigaCrush;
		shouldVortexSuck = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		netcodeOverride = NetcodeModel.FavorDefender;
	}

	public override void update() {
		base.update();
		foreach (var gameObject in Global.level.getGameObjectArray()) {
			if (gameObject is Actor actor &&
				actor.ownedByLocalPlayer &&
				gameObject is IDamagable damagable &&
				damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null) &&
				actor.pos.distanceTo(pos) <= radius + 15
			) {
				damager.applyDamage(damagable, false, weapon, this, projId);
			}
		}
		radius += Global.spf * 400;

		if (time > maxActiveTime) {
			destroySelf(disableRpc: false);
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		double transparency = (time - 0.2) / (maxActiveTime - 0.2);
		if (transparency < 0) { transparency = 0; }
		Color col1 = new(0, 0, 0, (byte)(225.0 - 225.0 * (transparency)));
		Color col2 = new(255, 255, 255, (byte)(255.0 - 255.0 * (transparency)));
		DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, filled: true, col1, 5f, zIndex + 1, isWorldPos: true, col2);
	}
}

public class GigaCrushCharState : CharState {
	GigaCrushProj proj;
	bool fired;
	Point moveDir = new(0, -20);

	public GigaCrushCharState() : base("gigacrush", "", "", "") {
		invincible = true;
	}

	public override void update() {
		base.update();
		if (character.frameIndex == 4 && !fired) {
			fired = true;
			proj = new GigaCrushProj(
				new GigaCrush(), character.getCenterPos(), character.xDir,
				player, player.getNextActorNetId(), rpc: true
			);
		}
		if (character.sprite.isAnimOver()) {
			character.changeState(new Idle(), true);
		}

		if (stateTime <= 1.6) {
			character.vel.x = moveDir.x * (1f - stateTime / 1.6f);
			character.vel.y = moveDir.y * (1f - stateTime / 1.6f);
		}
		else {
			character.vel.x = 0;
			character.vel.y = 0;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		player.character.useGravity = false;
		player.character.vel.y = 0;
		if (character.ownedByLocalPlayer && player == Global.level.mainPlayer) {
			new GigaCrushBackwall(character.pos, character);
		}

		if (player.input.isHeld("left", base.player)) {
			moveDir.x = -40f;
			moveDir.y = 0;
		}
		else if (player.input.isHeld("right", base.player)) {
			moveDir.x = 40f;
			moveDir.y = 0;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		player.character.useGravity = true;
		if (proj != null && !proj.destroyed) proj.destroySelf();
	}
}

public class GigaCrushEffect : Effect {
	public Character character;
	float frame1Time;
	float frame2Time;
	float time;
	Color linesColor = new(136, 184, 248, 200);

	public GigaCrushEffect(Character character) : base(character.pos) {
		this.character = character;
	}

	public override void update() {
		base.update();

		pos = character.pos;
		time += Global.spf;
		if (time > 3) {
			destroySelf();
			return;
		}

		if (character.sprite.name != "mmx_gigacrush" || character.frameIndex > 2) {
			return;
		}

		if (character.frameIndex < 2) {
			frame1Time += Global.spf;
		}
		if (character.frameIndex == 2) {
			frame2Time += Global.spf;
		}
	}

	public override void render(float offsetX, float offsetY) {
		base.render(offsetX, offsetY);

		if (character.sprite.name != "mmx_gigacrush" || character.frameIndex > 2) {
			return;
		}

		var pos = character.getCenterPos();
		if (character.frameIndex < 2) {
			for (int i = 0; i < 8; i++) {
				float angle = i * 45;
				float ox = Helpers.randomRange(10, 30) * Helpers.cosd(angle) * (MathF.Round(25 * frame1Time / 1.5f) % 5);
				float oy = Helpers.randomRange(10, 30) * Helpers.sind(angle) * (MathF.Round(25 * frame1Time / 1.5f) % 5);
				DrawWrappers.DrawLine(
					pos.x + ox, pos.y + oy,
					pos.x + ox * 2, pos.y + oy * 2,
					linesColor, 1, character.zIndex, true
				);
			}
		} else if (character.frameIndex == 2) {
			float radius = 150 - (frame2Time * 150 / 0.5f);
			if (radius <= 0) {
				return;
			}
			byte colour2 = (byte)(255.0 - 255.0 * ((radius-75) / 75.0));
				byte colour1 = 255;
				if (radius <= 75) {
				colour2 = 0;
				colour1 = (byte)(255.0 - 255.0 * (radius / 75.0));
			}
			Color colour = new(colour1, colour2, 255, 164);
			DrawWrappers.DrawCircle(pos.x, pos.y, radius, filled: true, colour, 0f, -2000001L);
		}
	}
}

public class GigaCrushBackwall : Effect {
	public Character rootChar;

	public GigaCrushBackwall(Point pos, Character character) : base(pos) {
		rootChar = character;
	}

	public override void update() {
		base.update();
		if (effectTime > 2.8) {
			destroySelf();
		}
	}

	public override void render(float offsetX, float offsetY) {
		float transparecy = 100;
		if (effectTime < 0.2) {
			transparecy = effectTime * 500f;
		}
		if (effectTime > 2.6) {
			transparecy = 100f - ((effectTime - 2.6f) * 500f);
		}
		
		DrawWrappers.DrawRect(
			Global.level.camX,  Global.level.camY,
			Global.level.camX + 1000,  Global.level.camY + 1000,
			true, new Color(0, 0, 0, (byte)System.MathF.Round(transparecy)), 1, ZIndex.Backwall
		);
	}
}