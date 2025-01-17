using System;
using System.Globalization;
using SFML.Graphics;

namespace MMXOnline;

public enum ZeroGigaType {
	Rakuhouha,
	CFlasher,
	Rekkoha,
	ShinMessenkou,
	DarkHold,
}

public class RakuhouhaWeapon : Weapon {
	public static RakuhouhaWeapon netWeapon = new();
	
	public RakuhouhaWeapon() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		ammo = 0;
		maxAmmo = 28;
		fireRate = 60;
		index = (int)WeaponIds.Rakuhouha;
		weaponBarBaseIndex = 27;
		weaponBarIndex = 33;
		killFeedIndex = 16;
		weaponSlotIndex = 51;
		type = (int)ZeroGigaType.Rakuhouha;
		displayName = "Rakuhouha";
		description = new string[] { "Channels stored energy in one blast. Energy cost: 14." };
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		allowSmallBar = false;
		damage = "4";
		hitcooldown = "1";
		Flinch = "26";
		effect = "42 Frames of Invincibility.";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 14;
	}

	public static Weapon getWeaponFromIndex(int index) {
		return index switch {
			(int)ZeroGigaType.Rakuhouha => new RakuhouhaWeapon(),
			(int)ZeroGigaType.CFlasher => new CFlasher(),
			(int)ZeroGigaType.Rekkoha => new RekkohaWeapon(),
			_ => throw new Exception("Invalid Zero hyouretsuzan weapon index!")
		};
	}
}

public class RekkohaWeapon : Weapon {
	public static RekkohaWeapon netWeapon = new();

	public RekkohaWeapon() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		ammo = 0;
		maxAmmo = 28;
		fireRate = 120;
		index = (int)WeaponIds.Rekkoha;
		weaponBarBaseIndex = 40;
		weaponBarIndex = 34;
		killFeedIndex = 38;
		weaponSlotIndex = 63;
		type = (int)ZeroGigaType.Rekkoha;
		displayName = "Rekkoha";
		description = new string[] { "Summon down pillars of light energy. Energy cost: 28." };
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		allowSmallBar = false;
		damage = "3";
		hitcooldown = "0.5";
		Flinch = "26";
		effect = "79 Frames of Invincibility.";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 28;
	}
}

public class CFlasher : Weapon {
	public static CFlasher netWeapon = new();
	
	public CFlasher() : base() {
		//damager = new Damager(player, 2, 0, 0.5f);
		ammo = 0;
		maxAmmo = 28;
		fireRate = 60;
		index = (int)WeaponIds.CFlasher;
		weaponBarBaseIndex = 41;
		weaponBarIndex = 35;
		killFeedIndex = 81;
		weaponSlotIndex = 64;
		type = (int)ZeroGigaType.CFlasher;
		displayName = "Messenkou";
		description = new string[] { "A weak blast that can pierce enemies. Energy cost: 7." };
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		allowSmallBar = false;
		damage = "2";
		hitcooldown = "0.5";
		Flinch = "0";
		effect = "42 Frames of Invincibility. Ignores Defense.";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 7;
	}
}

public class ShinMessenkou : Weapon {
	public ShinMessenkou() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		ammo = 0;
		maxAmmo = 28;
		fireRate = 60;
		index = (int)WeaponIds.ShinMessenkou;
		killFeedIndex = 86;
		type = (int)ZeroGigaType.ShinMessenkou;
		weaponBarBaseIndex = 43;
		weaponBarIndex = 37;
		weaponSlotIndex = 64;
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		allowSmallBar = false;
		damage = "4";
		hitcooldown = "1";
		Flinch = "26";
		effect = "42 Frames of Invincibility";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 14;
	}
}

public class Rakuhouha : CharState {
	public Weapon weapon;
	ZeroGigaType type { get { return (ZeroGigaType)weapon.type; } }
	bool fired = false;
	bool fired2 = false;
	bool fired3 = false;
	const float shinMessenkouWidth = 40;
	public DarkHoldProj? darkHoldProj;
	public Rakuhouha(
		Weapon weapon
	) : base(
		(weapon.type == (int)ZeroGigaType.DarkHold) ? "darkhold" : 
		weapon.type == (int)ZeroGigaType.CFlasher ||
		weapon.type == (int)ZeroGigaType.DarkHold ? "cflasher" : "rakuhouha"
	) {
		this.weapon = weapon;
		invincible = true;
	}

	public override void update() {
		base.update();
		bool isCFlasher = type == ZeroGigaType.CFlasher;
		bool isRakuhouha = type == ZeroGigaType.Rakuhouha;
		bool isShinMessenkou = type == ZeroGigaType.ShinMessenkou;
		bool isDarkHold = type == ZeroGigaType.DarkHold;
		if (character.frameIndex == 5 && !once && !isDarkHold) {
			once = true;
			/*rakuanim = new Anim(
				character.pos.addxy(character.xDir, 0),
				"zero_rakuanim", character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: true, sendRpc: true
			);*/
		}
		float x = character.pos.x;
		float y = character.pos.y;
		if (character.frameIndex > 7 && !fired) {
			fired = true;
			if (isShinMessenkou) {
				character.playSound("zeroshinmessenkoubullet", forcePlay: false, sendRpc: true);
				new ShinMessenkouProj(
					weapon, new Point(x - shinMessenkouWidth, y),
					character.xDir, player, player.getNextActorNetId(), rpc: true
				);
				new ShinMessenkouProj(
					weapon, new Point(x + shinMessenkouWidth, y), character.xDir,
					player, player.getNextActorNetId(), rpc: true
				);
			} else if (isDarkHold) {
				darkHoldProj = new DarkHoldProj(
					weapon, new Point(x, y - 20), character.xDir, player, player.getNextActorNetId(), rpc: true
				);
			} else {
				for (int i = 256; i >= 128; i -= 16) {
					new RakuhouhaProj(
						weapon, new Point(x, y), isCFlasher, i,
						player, player.getNextActorNetId(), rpc: true
					);
				}
			}
			if (isRakuhouha) {
				character.shakeCamera(sendRpc: true);
				character.playSound("rakuhouha", sendRpc: true);
				character.playSound("crash", sendRpc: true);
			} else if (isCFlasher) { 
				character.shakeCamera(sendRpc: true);
				character.playSound("messenkou", sendRpc: true);
				character.playSound("crashX3", sendRpc: true);
			} else if (isDarkHold) {
				character.playSound("darkhold", forcePlay: false, sendRpc: true);
			} else if (isShinMessenkou) {
				character.playSound("crash", forcePlay: false, sendRpc: true);
				character.shakeCamera(sendRpc: true);
			}
		}
		if (!fired2 && isShinMessenkou && character.frameIndex > 11) {
			fired2 = true;
			character.playSound("zeroshinmessenkoubullet", forcePlay: false, sendRpc: true);
			new ShinMessenkouProj(
				weapon, new Point(x - shinMessenkouWidth * 2, y),
				character.xDir, player, player.getNextActorNetId(), rpc: true
			);
			new ShinMessenkouProj(
				weapon, new Point(x + shinMessenkouWidth * 2, y),
				character.xDir, player, player.getNextActorNetId(), rpc: true
			);
		}
		if (!fired3 && isShinMessenkou && character.frameIndex > 14) {
			fired3 = true;
			character.playSound("zeroshinmessenkoubullet", forcePlay: false, sendRpc: true);
			new ShinMessenkouProj(
				weapon, new Point(x - shinMessenkouWidth * 3, y),
				character.xDir, player, player.getNextActorNetId(), rpc: true
			);
			new ShinMessenkouProj(
				weapon, new Point(x + shinMessenkouWidth * 3, y),
				character.xDir, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onExit(CharState newState) {
		weapon.shootCooldown = weapon.fireRate;
		base.onExit(newState);
	}
}

public class RakuhouhaProj : Projectile {
	bool isCFlasher;
	public RakuhouhaProj(
		Weapon weapon, Point pos, bool isCFlasher, float byteAngle,
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 300, 4, player, isCFlasher ? "cflasher" : "rakuhouha",
		Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer
	) {
		this.isCFlasher = isCFlasher;
		byteAngle = byteAngle % 256;

		/*if (angle == 128+16) {
			var sprite = isCFlasher ? "cflasher_diag" : "rakuhouha_diag";
			changeSprite(sprite, false);
		} else if (angle == 128+32) {
			var sprite = isCFlasher ? "cflasher_up" : "rakuhouha_up";
			changeSprite(sprite, false);
		} else if (angle == 128) {
			xDir = -1;
			var sprite = isCFlasher ? "cflasher_diag" : "rakuhouha_diag";
			changeSprite(sprite, false);
		} else if (angle == 180) {
			xDir = -1;
		}*/

		if (!isCFlasher) {
			fadeSprite = "rakuhouha_fade";
		} else {
			damager.damage = 2;
			damager.hitCooldown = 30;
			damager.flinch = 0;
			destroyOnHit = false;
		}

		reflectable = true;
		projId = (int)ProjIds.Rakuhouha;
		if (isCFlasher) {
			projId = (int)ProjIds.CFlasher;
		}
		vel.x = 300 * Helpers.cosb(byteAngle);
		vel.y = 300 * Helpers.sinb(byteAngle);
		this.byteAngle = byteAngle;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netProjId, byteAngle);
		}
	}

	public override void update() {
		base.update();
		if (time > 0.5) {
			destroySelf(fadeSprite);
		}
	}
}

public class ShinMessenkouProj : Projectile {
	int state = 0;
	public ShinMessenkouProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 4, player, "shinmessenkou_start", Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.6f;
		destroyOnHit = false;
		projId = (int)ProjIds.ShinMessenkou;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (state == 0 && isAnimOver()) {
			state = 1;
			changeSprite("shinmessenkou_proj", true);
			vel.y = -300;
		}
	}
}

public class Rekkoha : CharState {
	bool fired1 = false;
	bool fired2 = false;
	bool fired3 = false;
	bool fired4 = false;
	bool fired5 = false;
	bool sound;
	int loop;
	public RekkohaEffect? effect;
	public Weapon weapon;
	public Rekkoha(Weapon weapon) : base("rekkoha") {
		this.weapon = weapon;
		invincible = true;
		immuneToWind = true;
	}

	public override void update() {
		base.update();

		float topScreenY = Global.level.getTopScreenY(character.pos.y);

		if (character.frameIndex == 13 && loop < 15) {
			character.frameIndex = 10;
			loop++;
		}

		if (character.frameIndex == 5 && !sound) {
			sound = true;
			character.shakeCamera(sendRpc: true);
			character.playSound("crashX2", sendRpc: true);
			character.playSound("rekkoha", sendRpc: true);
		}

		if (stateTime > 26/60f && !fired1) {
			fired1 = true;
			new RekkohaProj(weapon, new Point(character.pos.x, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 0.6f && !fired2) {
			fired2 = true;
			new RekkohaProj(weapon, new Point(character.pos.x - 35, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new RekkohaProj(weapon, new Point(character.pos.x + 35, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 0.8f && !fired3) {
			fired3 = true;
			new RekkohaProj(weapon, new Point(character.pos.x - 70, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new RekkohaProj(weapon, new Point(character.pos.x + 70, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 1f && !fired4) {
			fired4 = true;
			new RekkohaProj(weapon, new Point(character.pos.x - 110, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new RekkohaProj(weapon, new Point(character.pos.x + 110, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 1.2f && !fired5) {
			fired5 = true;
			new RekkohaProj(weapon, new Point(character.pos.x - 150, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new RekkohaProj(weapon, new Point(character.pos.x + 150, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (player.isMainPlayer) {
			effect = new RekkohaEffect();
		}
	}

	public override void onExit(CharState newState) {
		weapon.shootCooldown = weapon.fireRate;
		base.onExit(newState);
	}
}

public class RekkohaProj : Projectile {
	float len = 0;
	private float reverseLen;
	private bool updatedDamager = false;

	public RekkohaProj(
		Weapon weapon, Point pos, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 0, 3, player, "rekkoha_proj",
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.Rekkoha;
		vel.y = 400;
		maxTime = 1.6f;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		netcodeOverride = NetcodeModel.FavorDefender;

		isOwnerLinked = true;
		if (player.character != null) {
			owningActor = player.character;
		}
	}

	public override void update() {
		base.update();
		len += Global.spf * 300;
		if (time >= 1f && !updatedDamager) {
			updateLocalDamager(0, 0);
			updatedDamager = true;
		}
		if (len >= 200) {
			len = 200;
			reverseLen += Global.spf * 200 * 2;
			if (reverseLen > 200) {
				reverseLen = 200;
			}
			vel.y = 100;
		}
		Rect newRect = new Rect(0, 0, 16, 60 + len - reverseLen);
		globalCollider = new Collider(newRect.getPoints(), true, this, false, false, 0, new Point(0, 0));
	}

	public override void render(float x, float y) {
		float finalLen = len - reverseLen;

		float newAlpha = 1;
		if (len <= 50) {
			newAlpha = len / 50;
		}
		if (reverseLen >= 100) {
			newAlpha = (200 - reverseLen) / 100;
		}

		alpha = newAlpha;

		float basePosY = System.MathF.Floor(pos.y + y);

		Global.sprites["rekkoha_proj_mid"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY - 63f,
			1, 1, null, alpha,
			1f, finalLen / 23f + 0.01f, zIndex
		);
		Global.sprites["rekkoha_proj_top"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY - 63f - finalLen,
			1, 1, null, alpha,
			1f, 1f, zIndex
		);
		Global.sprites["rekkoha_proj"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY,
			1, 1, null, alpha,
			1f, 1f, zIndex
		);
	}
}


public class RekkohaEffect : Effect {
	public RekkohaEffect() : base(new Point(Global.level.camX, Global.level.camY)) {
	}

	public override void update() {
		base.update();
		pos.x = Global.level.camX;
		pos.y = Global.level.camY;

		if (effectTime > 2) {
			destroySelf();
		}
	}

	public override void render(float offsetX, float offsetY) {
		float scale = 1;
		if (Global.level.server.fixedCamera) {
			scale = 2;
		}

		offsetX += pos.x;
		offsetY += pos.y;

		float baseAlpha = 0.4f;
		float alpha = baseAlpha;
		if (effectTime < 0.25f) {
			alpha = baseAlpha * (effectTime * 4);
		}
		if (effectTime > 1.75f) {
			alpha = baseAlpha - baseAlpha * ((effectTime - 1.75f) * 4);
		}
		alpha *= 1.5f;

		for (int i = 0; i < 50 * scale; i++) {
			float offY = (effectTime * 448) * (i % 2 == 0 ? 1 : -1);
			while (offY > 596) offY -= 596;
			while (offY < -596) offY += 596;

			int index = i + (int)(effectTime * 20);

			Global.sprites["rekkoha_effect_strip"].draw(
				index % 3,
				offsetX + i * 8, offsetY + offY - 596,
				1, 1, null, alpha, 1, 1, ZIndex.Backwall + 5
			);
			Global.sprites["rekkoha_effect_strip"].draw(
				index % 3,
				offsetX + i * 8, offsetY + offY,
				1, 1, null, alpha, 1, 1, ZIndex.Backwall + 5
			);
			Global.sprites["rekkoha_effect_strip"].draw(
				index % 3,
				offsetX + i * 8, offsetY + offY + 596,
				1, 1, null, alpha, 1, 1, ZIndex.Backwall + 5
			);
		}
		for (int i = 0; i < 50 * scale; i++) {
			float offY = (effectTime * 448) * (i % 2 == 0 ? 1 : -1);
			while (offY > 596) offY -= 596;
			while (offY < -596) offY += 596;

			int index = i + (int)(effectTime * 20);

			Global.sprites["rekkoha_effect_strip"].draw(
				index % 3,
				offsetX + i * 8, offsetY + offY - 596,
				1, 1, null, alpha, 1, 1, ZIndex.Background + 5
			);
			Global.sprites["rekkoha_effect_strip"].draw(
				index % 3,
				offsetX + i * 8, offsetY + offY,
				1, 1, null, alpha, 1, 1, ZIndex.Background + 5
			);
			Global.sprites["rekkoha_effect_strip"].draw(
				index % 3,
				offsetX + i * 8, offsetY + offY + 596,
				1, 1, null, alpha, 1, 1, ZIndex.Background + 5
			);
		}
	}
}

public class DarkHoldWeapon : Weapon {
	public DarkHoldWeapon() : base() {
		ammo = 0;
		maxAmmo = 28;
		fireRate = 60 * 3;
		index = (int)WeaponIds.DarkHold;
		type = (int)ZeroGigaType.DarkHold;
		killFeedIndex = 175;
		weaponBarBaseIndex = 69;
		weaponBarIndex = 58;
		weaponSlotIndex = 122;
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		allowSmallBar = false;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 14;
	}
}

public class DarkHoldProj : Projectile {
	public float radius = 10;
	public float attackRadius => (radius + 15);
	public ShaderWrapper? screenShader;
	float timeInFrames;

	public DarkHoldProj(
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 0, player, "empty", 0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1.25f;
		vel = new Point();
		projId = (int)ProjIds.DarkHold;
		setIndestructableProperties();
		Global.level.darkHoldProjs.Add(this);
		if (Options.main.enablePostProcessing) {
			screenShader = player.darkHoldScreenShader;
			updateShader();
		}
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		updateShader();

		if (timeInFrames  <= 30) {
			foreach (GameObject gameObject in getCloseActors(MathInt.Ceiling(radius + 50))) {
				if (gameObject != this && gameObject is Actor actor && actor.locallyControlled && inRange(actor)) {
					// For characters.
					if (actor is Character chara && chara.darkHoldInvulnTime <= 0) {
						if (timeInFrames >= 30) {
							continue;
						}
						if (chara.canBeDamaged(damager.owner.alliance, damager.owner.id, null)) {
							chara.addDarkHoldTime(150 - timeInFrames, damager.owner);
							chara.darkHoldInvulnTime = (150 - timeInFrames) * 60f;
						}
						// We freeze the player in the same way we freeze the armor if inside of it.
						if (chara.charState is InRideArmor or InRideChaser) {
							if (actor.timeStopTime <= 0) {
								actor.timeStopTime = 120 - timeInFrames;
							}
							continue;
						}
						continue;
					}
					// For maverick and rides
					if (actor is RideArmor or Maverick or Mechaniloid or RideChaser) {
						if (actor.timeStopTime > 0) {
							continue;
						}
						IDamagable? damagable = actor as IDamagable;
						if (damagable?.canBeDamaged(damager.owner.alliance, damager.owner.id, null) != true) {
							continue;
						}
						if (120 - timeInFrames > 0) {
							actor.timeStopTime = 120 - timeInFrames;
						}
					}
				}
			}
		}
		if (timeInFrames <= 30) {
			radius += 6.5f;
		}
		if (timeInFrames >= 60 && radius > 0) {
			radius -= 13;
			if (radius <= 0) {
				radius = 0;
			}
		}
		timeInFrames++;
	}

	public bool inRange(Actor actor) {
		return (actor.getCenterPos().distanceTo(pos) <= attackRadius);
	}

	public void updateShader() {
		if (screenShader != null) {
			var screenCoords = new Point(
				pos.x - Global.level.camX,
				pos.y - Global.level.camY
			);
			var normalizedCoords = new Point(
				screenCoords.x / Global.viewScreenW,
				1 - screenCoords.y / Global.viewScreenH
			);
			float ratio = Global.screenW / (float)Global.screenH;
			float normalizedRadius = (radius / Global.screenH);

			screenShader.SetUniform("ratio", ratio);
			screenShader.SetUniform("x", normalizedCoords.x);
			screenShader.SetUniform("y", normalizedCoords.y);
			screenShader.SetUniform("r", normalizedRadius / Global.viewSize);
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (screenShader == null) {
			var col = new Color(255, 251, 239, (byte)(164 - 164 * (time / maxTime)));
			var col2 = new Color(255, 219, 74, (byte)(224 - 224 * (time / maxTime)));
			DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, true, col, 1, zIndex + 1, true);
			DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, false, col2, 3, zIndex + 1, true, col2);
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		Global.level.darkHoldProjs.Remove(this);
	}
}

public class DarkHoldState : CharState {
	public float stunTime = totalStunTime;
	public const float totalStunTime = 2.5f * 60;
	int frameIndex;
	public bool shouldDrawAxlArm = true;
	public float lastArmAngle = 0;

	public DarkHoldState(Character character, float time) : base(character.sprite.name) {
		immuneToWind = true;
		stunTime = time;

		this.frameIndex = character?.frameIndex ?? 0;
		if (character is Axl axl) {
			this.shouldDrawAxlArm = axl.shouldDrawArm();
			this.lastArmAngle = axl.netArmAngle;
		}
	}

	public override void update() {
		base.update();
		character.stopMoving();
		if (stunTime <= 0) {
			stunTime = 0;
			character.changeToIdleOrFall();
		}
		// Does not stack with other time stops.
		stunTime -= 1;
	}

	public override bool canEnter(Character character) {
		if (character.darkHoldInvulnTime > 0 || character.isTimeImmune()) {
			return false;
		}
		return base.canEnter(character);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.frameSpeed = 0;
		character.frameIndex = frameIndex;
		character.stopMoving();
		character.isDarkHoldState = true;
		invincible = oldState.invincible;
		specialId = oldState.specialId;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
		character.frameSpeed = 1;
		character.darkHoldInvulnTime = 1;
		character.isDarkHoldState = false;
	}
}
