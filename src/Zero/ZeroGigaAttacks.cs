using System;
using System.Globalization;
using SFML.Graphics;

namespace MMXOnline;

public enum ZeroGigaType {
	Rakuhouha,
	Messenkou,
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
		flinch = "26";
		effect = "42 Frames of Invincibility.";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 14;
	}

	public static Weapon getWeaponFromIndex(int index) {
		return index switch {
			(int)ZeroGigaType.Rakuhouha => new RakuhouhaWeapon(),
			(int)ZeroGigaType.Messenkou => new Messenkou(),
			(int)ZeroGigaType.Rekkoha => new RekkohaWeapon(),
			_ => throw new Exception("Invalid Zero hyouretsuzan weapon index!")
		};
	}

	public override void shoot(Character character, int[] args) {
		addAmmo(-getAmmoUsage(0), character.player);
		character.changeState(new RakuhouhaState(this), true);
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
		flinch = "26";
		effect = "79 Frames of Invincibility.";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 28;
	}

	public override void shoot(Character character, int[] args) {
		addAmmo(-getAmmoUsage(0), character.player);
		character.changeState(new RekkohaState(this), true);
	}
}

public class Messenkou : Weapon {
	public static Messenkou netWeapon = new();

	public Messenkou() : base() {
		//damager = new Damager(player, 2, 0, 0.5f);
		ammo = 0;
		maxAmmo = 28;
		fireRate = 60;
		index = (int)WeaponIds.Messenkou;
		weaponBarBaseIndex = 41;
		weaponBarIndex = 35;
		killFeedIndex = 81;
		weaponSlotIndex = 64;
		type = (int)ZeroGigaType.Messenkou;
		displayName = "Messenkou";
		description = new string[] { "A weak blast that can pierce enemies. Energy cost: 7." };
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		allowSmallBar = false;
		damage = "2";
		hitcooldown = "0.5";
		flinch = "0";
		effect = "42 Frames of Invincibility. Ignores Defense.";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 7;
	}

	public override void shoot(Character character, int[] args) {
		addAmmo(-getAmmoUsage(0), character.player);
		character.changeState(new MessenkouState(this), true);
	}
}

public class ShinMessenkou : Weapon {
	public static ShinMessenkou netWeapon = new();
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
		flinch = "26";
		effect = "42 Frames of Invincibility";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 14;
	}

	public override void shoot(Character character, int[] args) {
		addAmmo(-getAmmoUsage(0), character.player);
		character.changeState(new ShinMessenkouState(this), true);
	}
}

public class DarkHoldWeapon : Weapon {
	public static DarkHoldWeapon netWeapon = new();
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

	public override void shoot(Character character, int[] args) {
		addAmmo(-getAmmoUsage(0), character.player);
		character.changeState(new DarkHoldShootState(this), true);
	}
}

public class RakuhouhaProj : Projectile {
	bool isCFlasher;
	public RakuhouhaProj(
		Point pos, bool isCFlasher, float byteAngle, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, isCFlasher ? "cflasher" : "rakuhouha", netId, player
	) {
		weapon = isCFlasher ? Messenkou.netWeapon : RakuhouhaWeapon.netWeapon;
		damager.damage = 4;
		damager.hitCooldown = 60;
		damager.flinch = Global.defFlinch;
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		vel = new Point(300 * xDir, 0);
		vel.x = 300 * Helpers.cosb(byteAngle);
		vel.y = 300 * Helpers.sinb(byteAngle);
		zIndex = ZIndex.Character;
		this.isCFlasher = isCFlasher;
		reflectable = true;
		fadeOnAutoDestroy = true;
		maxTime = 0.5f;
		if (!isCFlasher) {
			fadeSprite = "rakuhouha_fade";
			projId = (int)ProjIds.Rakuhouha;
		} else {
			damager.damage = 2;
			damager.hitCooldown = 30;
			damager.flinch = 0;
			destroyOnHit = false;
			fadeSprite = "buster4_fade";
			projId = (int)ProjIds.CFlasher;
		}
		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle,
			new byte[] { isCFlasher ? (byte)1 : (byte)0 }
			);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new RakuhouhaProj(
			args.pos, args.extraData[0] == 1, args.byteAngle,
			args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class ShinMessenkouProj : Projectile {
	int state = 0;
	public ShinMessenkouProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "shinmessenkou_start", netId, player
	) {
		weapon = ShinMessenkou.netWeapon;
		fadeSprite = "rakuhouha_fade";
		damager.damage = 4;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		maxTime = 0.6f;
		destroyOnHit = false;
		projId = (int)ProjIds.ShinMessenkou;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new ShinMessenkouProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
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

public class RekkohaProj : Projectile {
	float len = 0;
	private float reverseLen;
	private bool updatedDamager = false;

	public RekkohaProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "rekkoha_proj", netId, player
	) {
		weapon = RekkohaWeapon.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		projId = (int)ProjIds.Rekkoha;
		vel.y = 400;
		maxTime = 1.6f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		netcodeOverride = NetcodeModel.FavorDefender;
		isOwnerLinked = true;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		if (ownerPlayer?.character != null) {
			ownerActor = ownerPlayer.character;
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new RekkohaProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
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
			basePosY - 41f,
			1, 1, null, alpha,
			1f, finalLen / 9f + 0.01f, zIndex
		);
		Global.sprites["rekkoha_proj_top"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY - 41f - finalLen,
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

public class DarkHoldProj : Projectile {
	public float radius = 10;
	public float attackRadius => (radius + 15);
	public ShaderWrapper? screenShader;
	float timeInFrames;

	public DarkHoldProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "empty", netId, player
	) {
		weapon = DarkHoldWeapon.netWeapon;
		damager.hitCooldown = 30;
		maxTime = 1.25f;
		vel = new Point();
		projId = (int)ProjIds.DarkHold;
		setIndestructableProperties();
		Global.level.darkHoldProjs.Add(this);
		if (Options.main.enablePostProcessing) {
			//screenShader = player.darkHoldScreenShader;
			updateShader();
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new DarkHoldProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		updateShader();

		if (timeInFrames <= 30) {
			foreach (GameObject gameObject in getCloseActors(MathInt.Ceiling(radius + 50))) {
				if (gameObject != this && gameObject is Actor actor && actor.locallyControlled && inRange(actor)) {
					// For characters.
					if (actor is Character chara && chara.darkHoldInvulnTime <= 0 &&
						actor is not KaiserSigma and not ViralSigma and not WolfSigma
					) {
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
					// For maverick and rides... and battle bodies.
					if (actor is RideArmor or Maverick or Mechaniloid or RideChaser or
						KaiserSigma or ViralSigma or WolfSigma
					) {
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

public abstract class ZeroGigaAttack : CharState {
	// General varaibles.
	public string effectName = "";
	public Action? onShoot;
	public Weapon weapon;
	public int attackMaxTime = 38;
	public int effectFrame = 9;
	public int shootFrame = 8;
	// Tracking variables.
	public bool shotProjectile;
	public bool effectPlayed;
	public bool exiting;

	public ZeroGigaAttack(string sprite, Weapon weapon) : base(sprite) {
		invincible = true;
		stunImmune = true;
		this.weapon = weapon;
	}

	public override void update() {
		// Keep it at max cooldown.
		weapon.shootCooldown = weapon.fireRate;
		// Base update.
		base.update();
		// Exit animation.
		if (exiting) {
			if (character.isAnimOver()) {
				character.changeToIdleOrFall();
			}
			return;
		}
		// Shoot function call.
		if (character.frameIndex >= shootFrame && !shotProjectile) {
			shotProjectile = true;
			onShoot?.Invoke();
		}
		// Shoot effect.
		if (character.frameIndex >= effectFrame && !effectPlayed) {
			effectPlayed = true;
			new Anim(
				character.pos, effectName, character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: true, sendRpc: true
			);
		}
		if (stateFrames >= attackMaxTime) {
			exiting = true;
			character.changeSpriteFromName("giga_end", true);
		}
	}
	
	public override void onEnter(CharState oldState) {
		character.clenaseDmgDebuffs();
		base.onEnter(oldState);
	}

	public override void onExit(CharState? newState) {
		weapon.shootCooldown = weapon.fireRate;
		base.onExit(newState);
	}
}

public class RakuhouhaState : ZeroGigaAttack {
	public RakuhouhaState(Weapon weapon) : base("rakuhouha", weapon) {
		onShoot = shootGiga;
		effectName = "zero_rakuanim";
	}

	public void shootGiga() {
		int xDir = character.getShootXDir();
		Point shootPos = character.pos.addxy(4 * xDir, 0);
		for (int i = 256; i >= 128; i -= 16) {
			new RakuhouhaProj(
				shootPos, false, i, 1, character,
				player, player.getNextActorNetId(), rpc: true
			);
		}
		character.shakeCamera(sendRpc: true);
		character.playSound("rakuhouha", sendRpc: true);
		character.playSound("crashX2", sendRpc: true);
	}
}

public class MessenkouState : ZeroGigaAttack {
	public MessenkouState(Weapon weapon) : base("cflasher", weapon) {
		onShoot = shootGiga;
		effectName = "zero_cflasheranim";
	}
	
	public void shootGiga() {
		int xDir = character.getShootXDir();
		Point shootPos = character.pos.addxy(4 * xDir, 0);
		for (int i = 256; i >= 128; i -= 16) {
			new RakuhouhaProj(
				shootPos, true, i, 1, character,
				player, player.getNextActorNetId(), rpc: true
			);
		}
		character.shakeCamera(sendRpc: true);
		character.playSound("messenkou", sendRpc: true);
		character.playSound("crashX3", sendRpc: true);
	}
}

public class RekkohaState : CharState {
	public int shootInterval = 12;
	public int distance = 36;
	public int shotMax = 4;
	public int rumbleMax = 7;
	public int rumbleNum;
	public int shotNum;
	public float shootTimer;
	public bool shotModeActive;
	public RekkohaEffect? effect;
	public Weapon weapon;
	public int attackMaxTime = 118;
	public bool exiting;

	public RekkohaState(Weapon weapon) : base("rekkoha") {
		this.weapon = weapon;
		invincible = true;
		stunImmune = true;
		pushImmune = true;
	}

	public override void update() {
		weapon.shootCooldown = weapon.fireRate;
		base.update();
		if (exiting) {
			if (character.isAnimOver()) {
				character.changeToIdleOrFall();
			}
			return;
		}
		if (shotModeActive) {
			if (shootTimer >= shootInterval) {
				if (shotNum < shotMax) {
					shotNum++;
					float topScreenY = Global.level.getTopScreenY(character.pos.y);
					float distXL = character.pos.x + distance * shotNum * -1;
					float distXR = character.pos.x + distance * shotNum * 1;

					new RekkohaProj(
						new Point(distXL, topScreenY), -1, character,
						player, player.getNextActorNetId(),
						rpc: true
					);
					new RekkohaProj(
						new Point(distXR, topScreenY), 1, character,
						player, player.getNextActorNetId(),
						rpc: true
					);
				}
				if (rumbleNum < rumbleMax) {
					character.shakeCamera(sendRpc: true);
					rumbleNum++;
				}
				shootTimer = 0;
			} else {
				shootTimer += character.speedMul;
			}
		}
		if (character.frameIndex >= 8 && !shotModeActive) {
			shotModeActive = true;
			character.shakeCamera(sendRpc: true);
			character.playSound("rekkoha", sendRpc: true);
			character.playSound("crashX2", sendRpc: true);
			float topScreenY = Global.level.getTopScreenY(character.pos.y);
			new RekkohaProj(
				new Point(character.pos.x, topScreenY),
				character.xDir, character,
				player, player.getNextActorNetId(),
				rpc: true
			);
		}
		if (stateFrames >= attackMaxTime) {
			exiting = true;
			character.changeSpriteFromName("giga_end", true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.clenaseAllDebuffs();
		if (player.isMainPlayer) {
			effect = new RekkohaEffect();
		}
	}
}

public class ShinMessenkouState : ZeroGigaAttack {
	public float shotDistance = 40;

	public ShinMessenkouState(Weapon weapon) : base("rakuhouha", weapon) {
		onShoot = shootGiga;
		effectName = "zero_rakuanim";
	}
	
	public void shootGiga() {
		for (int i = 1; i < 3; i++) {
			int j = i + 1;
			Global.level.delayedActions.Add(
				new DelayedAction(() => {
					shootProj(j);
				},
				(i * 8) / 60f)
			);
		}
		shootProj(1);
		character.shakeCamera(sendRpc: true);
		character.playSound("crash", sendRpc: true);
	}

	void shootProj(int i) {
		Point shootPos = character.pos.addxy(4 * character.xDir, 0);
		new ShinMessenkouProj(
			shootPos.addxy(shotDistance * i * character.xDir, 0),
			1, character, player, player.getNextActorNetId(), rpc: true
		);
		new ShinMessenkouProj(
			shootPos.addxy(shotDistance * i * -character.xDir, 0),
			-1, character, player, player.getNextActorNetId(), rpc: true
		);
		character.playSound("zeroshinmessenkoubullet");
	}
}

public class DarkHoldShootState : CharState {
	public bool shoot;
	public Weapon gigaAttack;

	public DarkHoldShootState(Weapon gigaAttack) : base("darkhold") {
		invincible = true;
		stunImmune = true;
		this.gigaAttack = gigaAttack;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 7 && !shoot) {
			shoot = true;
			new DarkHoldProj(
				character.pos.addxy(0, -20), character.xDir, character,
				player, player.getNextActorNetId(), rpc: true
			);
			character.playSound("darkhold");
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
			return;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.clenaseDmgDebuffs();
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		gigaAttack.shootCooldown = gigaAttack.fireRate;
	}
}

public class DarkHoldState : CharState {
	public float stunTime = totalStunTime;
	public const float totalStunTime = 2.5f * 60;
	public int frameIndex;
	public bool shouldDrawAxlArm = true;
	public float lastArmAngle = 0;

	public DarkHoldState(Character character, float time) : base(character.sprite.name) {
		pushImmune = true;
		stunTime = time;

		this.frameIndex = character?.frameIndex ?? 0;
		if (character is Axl axl) {
			this.shouldDrawAxlArm = axl.shouldDrawArm();
			this.lastArmAngle = axl.netArmAngle;
		}
	}

	public override void update() {
		base.update();
		character.stopMovingS();
		if (stunTime <= 0) {
			stunTime = 0;
			character.changeToIdleOrFall();
		}
		// Does not stack with other time stops.
		stunTime -= player.mashValue() * 60f;
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
		character.stopMovingS();
		character.isDarkHoldState = true;
		invincible = oldState.invincible;
		specialId = oldState.specialId;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
		character.frameSpeed = 1;
		character.darkHoldInvulnTime = 1;
		character.isDarkHoldState = false;
	}
}
