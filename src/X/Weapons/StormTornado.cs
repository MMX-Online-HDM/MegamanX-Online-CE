using System;
using System.Collections.Generic;

namespace MMXOnline;

public class StormTornado : Weapon {

	public static StormTornado netWeapon = new();
	public StormTornado() : base() {
		index = (int)WeaponIds.StormTornado;
		killFeedIndex = 5;
		weaponBarBaseIndex = 5;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 5;
		weaknessIndex = (int)WeaponIds.ChameleonSting;
		shootSounds = new string[] { "tornado", "tornado", "tornado", "buster3" };
		fireRate = 120;
		//switchCooldown = 0.5f;
		switchCooldownFrames = 30;
		damage = "1/4";
		effect = "Weak push. Extinguishes Fire. Ignores Shields.\nUncharged won't give assists.";
		hitcooldown = "0.25/0.33";
		Flinch = "0/26";
		FlinchCD = "0/1";
		maxAmmo = 16;
		ammo = maxAmmo;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) { return 4; }
		return 1;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new TornadoProj(this, pos, xDir, false, player, player.getNextActorNetId(), true);
		} else {
			new TornadoProjCharged(this, pos, xDir, player, player.getNextActorNetId(), true);
		}
	}
}


public class TornadoProj : Projectile {
	public Sprite spriteStart;
	public List<Sprite> spriteMids = new List<Sprite>();
	public Sprite spriteEnd;
	public float length = 1;
	public float maxSpeed = 400;
	public float tornadoTime;
	public float blowModifier = 0.25f;

	public TornadoProj(
		Weapon weapon, Point pos, int xDir, bool isStormE,
		Player player, ushort netProjId, bool sendRpc = false
	) : base(
		weapon, pos, xDir, 400, 1, player, "tornado_mid", 
		0, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		projId = isStormE ? (int)ProjIds.StormETornado : (int)ProjIds.Tornado;
		if (isStormE) {
			blowModifier = 1;
			damager.hitCooldown = 30;
		}
		maxTime = 2;
		sprite.visible = false;
		spriteStart = new Sprite("tornado_start");
		for (var i = 0; i < 6; i++) {
			var midSprite = new Sprite("tornado_mid");
			midSprite.visible = false;
			spriteMids.Add(midSprite);
		}
		spriteEnd = new Sprite("tornado_end");
		vel.x = 0;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TornadoProj(
			StormTornado.netWeapon, arg.pos, arg.xDir, 
			false, arg.player, arg.netId
		);
	}

	public override void render(float x, float y) {
		spriteStart.draw(frameIndex, pos.x + x, pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		int spriteMidLen = 16;
		int i = 0;
		for (i = 0; i < length; i++) {
			spriteMids[i].visible = true;
			spriteMids[i].draw(frameIndex, pos.x + x + (i * xDir * spriteMidLen), pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}
		spriteEnd.draw(frameIndex, pos.x + x + (i * xDir * spriteMidLen), pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		if (Global.showHitboxes && collider != null) {
			//Helpers.drawPolygon(Global.level.uiCtx, this.collider.shape.clone(x, y), true, "blue", "", 0, 0.5);
			//Helpers.drawCircle(Global.level.ctx, this.pos.x + x, this.pos.y + y, 1, "red");
		}
	}

	public override void update() {
		base.update();

		var topX = 0;
		var topY = 0;

		var spriteMidLen = 16;
		var spriteEndLen = 14;

		var botX = (length * spriteMidLen) + spriteEndLen;
		var botY = 32;

		var rect = new Rect(topX, topY, botX, botY);
		globalCollider = new Collider(rect.getPoints(), true, this, false, false, 0, new Point(0, 0));

		tornadoTime += Global.spf;
		if (tornadoTime > 0.2f) {
			if (length < 6) {
				length++;
			} else {
				vel.x = maxSpeed * xDir;
			}
			tornadoTime = 0;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!damagable.isPlayableDamagable()) { return; }
		if (damagable is not Actor actor || !actor.ownedByLocalPlayer) {
			return;
		}
		float modifier = 1;
		if (actor.grounded) { modifier = 0.5f; };
		if (damagable is Character character) {
			if (character.isPushImmune()) { return; }
			if (character.charState is Crouch) { modifier = 0.25f; }
			character.pushedByTornadoInFrame = true;
		}
		//character.damageHistory.Add(new DamageEvent(damager.owner, weapon.killFeedIndex, true, Global.frameCount));
		actor.move(new Point(maxSpeed * 0.9f * xDir * modifier * blowModifier, 0));
	}
}

public class TornadoProjCharged : Projectile {
	public Sprite spriteStart;
	public List<Sprite> bodySprites = new List<Sprite>();
	public int length = 1;
	public float groundY = 0;
	public const int maxLength = 10;
	public float maxSpeed = 100;
	public const float pieceHeight = 32;
	public float growTime = 0;
	public float maxLengthTime = 0;

	public TornadoProjCharged(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2, player, "tornado_charge", 
		Global.defFlinch, 0.33f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.TornadoCharged;
		sprite.visible = false;
		spriteStart = new Sprite("tornado_charge");
		for (var i = 0; i < maxLength; i++) {
			var midSprite = new Sprite("tornado_charge");
			midSprite.visible = false;
			bodySprites.Add(midSprite);
		}
		//this.ground();
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TornadoProjCharged(
			StormTornado.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.netId
		);
	}

	public override void render(float x, float y) {
		spriteStart.draw(frameIndex, pos.x + x, pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		for (int i = 0; i < length && i < bodySprites.Count; i++) {
			bodySprites[i].visible = true;
			bodySprites[i].draw(frameIndex, pos.x + x, pos.y + y - (i * pieceHeight), xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}
		if (Global.showHitboxes && collider != null) {
			//Helpers.drawPolygon(Global.level.uiCtx, this.collider.shape.clone(x, y), true, "blue", "", 0, 0.5);
		}
	}

	public void ground() {
		var ground = Global.level.raycast(pos.addxy(0, -10), pos.addxy(0, Global.level.height), new List<Type> { typeof(Wall) });
		if (ground != null) {
			pos.y = ((Point)ground.hitData.hitPoint).y;
		}
	}

	public override void update() {
		base.update();

		var botY = pieceHeight + (length * pieceHeight);

		var rect = new Rect(0, 0, 64, botY);
		globalCollider = new Collider(rect.getPoints(), true, this, false, false, 0, new Point(0, 0));

		growTime += Global.spf;
		if (growTime > 0.01) {
			if (length < maxLength) {
				length++;
				incPos(new Point(0, pieceHeight / 2));
			} else {
				//this.vel.x = this.maxSpeed * this.xDir;
			}
			growTime = 0;
		}

		if (length >= maxLength) {
			maxLengthTime += Global.spf;
			if (maxLengthTime > 1) {
				destroySelf();
			}
		}

	}
}
