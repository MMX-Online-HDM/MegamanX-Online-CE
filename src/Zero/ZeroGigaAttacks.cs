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

public class RakuhouhaProj : Projectile {
	bool isCFlasher;
	public RakuhouhaProj(
		Weapon weapon, Point pos, bool isCFlasher, float byteAngle,
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 300, 4, player, isCFlasher ? "cflasher" : "rakuhouha",
		Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer
	) {
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
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
			rpcCreateByteAngle(pos, player, netProjId, byteAngle);
		}
	}

	public override void update() {
		base.update();
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
	public bool exitOnOver = true;
	public int loop;
	public Anim? Anim;
	public Weapon weapon;
	public ZeroGigaAttack(Weapon weapon, string spr) : base(spr) {
		invincible = true;
		this.weapon = weapon;
	}

	public override void update() {
		base.update();
		if (exitOnOver && character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
	public void GigaAttackAnim(string sprite) {
		int xDir = character.getShootXDir();
		Anim = new Anim(
			character.getCenterPos().addxy(4*xDir, 24),
			sprite, xDir, player.getNextActorNetId(),
			destroyOnEnd: true, sendRpc: true
		);
	}
	public void LoopSprite(int firstFrame, int secondFrame, int frameloop) {
		if (character.frameIndex == firstFrame && loop < frameloop) {
			character.frameIndex = secondFrame;
			character.shakeCamera(sendRpc: true);
			loop++;
		}
	}
	public void playSound(string sound) {
		character.playSound(sound, forcePlay: false, sendRpc: true);
	}
	public override void onExit(CharState newState) {
		weapon.shootCooldown = weapon.fireRate;
		if (Anim != null) {
			Anim.destroySelf();
		}
		base.onExit(newState);
	}
}

public class RakuhouhaState : ZeroGigaAttack {
	public RakuhouhaState(Weapon weapon) : base(weapon, "rakuhouha") {
		this.weapon = weapon;
	}

	public override void update() {
		base.update();
		RakuhouhaShoot();
	}
	public void FenghuangProj(bool isCFlasher) {
		int xDir = character.getShootXDir();
		float x = character.getCenterPos().x + 4 * xDir;
		float y = character.getCenterPos().y + 12;
		for (int i = 256; i >= 128; i -= 16) {
			new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, i,
			player, player.getNextActorNetId(), rpc: true
			);
		}
	}
	public void RakuhouhaShoot() {
		if (character.frameIndex >= 11 && !once) {
			once = true;
			GigaAttackAnim("zero_rakuanim");
			FenghuangProj(false);
			playSound("rakuhouha");
			playSound("crash");
		}
		LoopSprite(13, 11, 3);
	}
}

public class CFlasherState : ZeroGigaAttack {
	public CFlasherState(Weapon weapon) : base(weapon, "cflasher") {
		this.weapon = weapon;	
	}

	public override void update() {
		base.update();
		CFlasherShoot();
	}
	public void FenghuangProj(bool isCFlasher) {
		int xDir = character.getShootXDir();
		float x = character.getCenterPos().x + 4 * xDir;
		float y = character.getCenterPos().y + 12;
		for (int i = 256; i >= 128; i -= 16) {
			new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, i,
			player, player.getNextActorNetId(), rpc: true
			);
		}
	}
	public void CFlasherShoot() {
		if (character.frameIndex >= 11 && !once) {
			once = true;
			GigaAttackAnim("zero_cflasheranim");
			FenghuangProj(true);
			playSound("messenkou");
			playSound("crashX3");
		}
		LoopSprite(13, 11, 3);
	}
}

public class RekkohaState : ZeroGigaAttack {
	public float[] StateTime = { 36f/60f, 48f/60f, 60f/60f, 72f/60f, 84f/60f };
	public bool[] fired = { false, false, false, false, false };
	public int[] Space = { 0, 35, 70, 110, 150 };
	public bool sound;
	public RekkohaEffect? effect;
	public RekkohaState(Weapon weapon) : base(weapon, "rekkoha") {
		this.weapon = weapon;	
		immuneToWind = true;
	}

	public override void update() {
		base.update();	
		for (int i = 0; i < StateTime.Length; i++) {
			if (stateTime > StateTime[i] && !fired[i]) {
				fired[i] = true;
		        RekkohaProj(i == 0 ? false : true, Space[i]);
			}
		}
		LoopSprite(11, 9, 11);
		playSoundRekkoha();
	}
	public void RekkohaProj(bool isDouble, int Space) {
		float x = character.pos.x;
		float y = character.pos.y;
		float topScreenY = Global.level.getTopScreenY(y);
		if (isDouble) {
			new RekkohaProj(weapon, new Point(x + Space, topScreenY),
			player, player.getNextActorNetId(), rpc: true);
			new RekkohaProj(weapon, new Point(x - Space, topScreenY),
			player, player.getNextActorNetId(), rpc: true);
		} else {
			new RekkohaProj(weapon, new Point(x, topScreenY),
			player, player.getNextActorNetId(), rpc: true);
		}
	}
	public void playSoundRekkoha() {
		if (character.frameIndex == 9 && !sound) {
			sound = true;
			character.playSound("crashX2", sendRpc: true);
			character.playSound("rekkoha", sendRpc: true);
		}
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (player.isMainPlayer) {
			effect = new RekkohaEffect();
		}
	}
}

public class ShinMessenkouState : ZeroGigaAttack {
	public float[] AnimSeconds = { 20f/60f, 28f/60f, 36f/60f };
	public bool[] fired = { false, false, false };
	public const float shinMessenkouWidth = 40;
	public ShinMessenkouState(Weapon weapon) : base(weapon, "rakuhouha") {
		this.weapon = weapon;
	}

	public override void update() {
		base.update();
		ShinMessenkouShoot();
	}
	public void ShinMessenkouProj(int multiplier) {
		float x = character.pos.x;
		float y = character.pos.y;
		new ShinMessenkouProj(
			weapon, new Point(x - shinMessenkouWidth * multiplier, y),
			character.xDir, player, player.getNextActorNetId(), rpc: true
		);
		new ShinMessenkouProj(
			weapon, new Point(x + shinMessenkouWidth * multiplier, y),
			character.xDir, player, player.getNextActorNetId(), rpc: true
		);
		playSound("zeroshinmessenkoubullet");
	}
	public void ShinMessenkouShoot() {
		for (int i = 0; i < AnimSeconds.Length; i++) {
			if (stateTime > AnimSeconds[i] && !fired[i]) {
				fired[i] = true;
		        ShinMessenkouProj(i+1);
				if (i == 0) {
					GigaAttackAnim("zero_rakuanim");
      				playSound("crash");
				}
			}
		}
		LoopSprite(13, 11, 3);
	}
}

public class DarkHoldShootState : ZeroGigaAttack {
	public DarkHoldProj? darkHoldProj;
	public DarkHoldShootState(Weapon weapon) : base(weapon, "darkhold") {
		this.weapon = weapon;	
	}
	public override void update() {
		base.update();
		DarkHoldShoot();
	}
	public void DarkHoldShoot() {
		int xDir = character.getShootXDir();
		float x = character.getCenterPos().x - 2 * xDir;
		float y = character.getCenterPos().y + 12;
		if (character.frameIndex >= 10 && !once) {
			once = true;
			darkHoldProj = new DarkHoldProj(
				weapon, new Point(x, y - 20), xDir, 
				player, player.getNextActorNetId(), rpc: true
			);
			playSound("darkhold");	
		}
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
