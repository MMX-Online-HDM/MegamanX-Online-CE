﻿using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.Graphics.Glsl;

namespace MMXOnline;

public class RaySplasher : Weapon {
	public static RaySplasher netWeapon = new RaySplasher();

	public RaySplasher() : base() {
		displayName = "Ray Splasher";
		shootSounds = ["raySplasher", "raySplasher", "raySplasher", "warpIn"];
		fireRate = 80;
		index = (int)WeaponIds.RaySplasher;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 21;
		killFeedIndex = 44;
		weaknessIndex = (int)WeaponIds.SpinningBlade;
		damage = "1/1";
		effect = "Charged: Grants Super Armor.";
		hitcooldown = "5";
		hasCustomChargeAnim = true;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (chargeLevel < 3) {
			mmx.shootingRaySplasher = this;
		} else {
			if (character.ownedByLocalPlayer) {
				character.changeState(new RaySplasherChargedState(), true);
			}
		}
	}

	public void burstLogic(MegamanX mmx) {
		if (mmx.currentWeapon is RaySplasher && mmx.invulnTime <= 0 && mmx.charState is not WarpIn) {
			if (mmx.shootingRaySplasher != null) {
				mmx.raySplasherCooldown += Global.speedMul;
				if (mmx.raySplasherCooldown > 1) {
					if (mmx.raySplasherCooldown >= 4) {
						addAmmo(-0.15f, mmx.player);
						mmx.raySplasherCooldown = 1;
						new RaySplasherProj(
							mmx.getShootPos(), mmx.getShootXDir(), mmx.raySplasherFrameIndex % 3,
							 (mmx.raySplasherFrameIndex / 5) % 5, mmx,
							mmx.player, mmx.player.getNextActorNetId(), true
						);
						mmx.raySplasherFrameIndex++;
					}
					mmx.shootAnimTime = 10;
					mmx.raySplasherCooldown2 += Global.speedMul;
				}
				if (mmx.raySplasherCooldown2 >= 76) {
					mmx.shootingRaySplasher = null;
					mmx.raySplasherCooldown2 = 0;
				}
			}
		} else {
			mmx.shootingRaySplasher = null;
			mmx.raySplasherFrameIndex = 0;
		}
		if (mmx.shootingRaySplasher == null) {
			mmx.raySplasherFrameIndex = 0;
		}
	}
}

public class RaySplasherProj : Projectile {
	public RaySplasherProj(
		Point pos, int xDir, int spriteType, int dirType, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "raysplasher_proj", netId, player
	) {
		weapon = RaySplasher.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 6;
		vel = new Point(600 * xDir, 0);
		maxTime = 0.25f;
		projId = (int)ProjIds.RaySplasher;
		
		reflectable = true;
		frameIndex = spriteType;
		frameSpeed = 0;
		fadeSprite = "raysplasher_fade";
		if (dirType == 0) {
			vel = new Point(600 * xDir, -150);
		}
		if (dirType == 1) {
			vel = new Point(600 * xDir, 150);
		}
		if (dirType == 2) {
			vel = new Point(600 * xDir, 0);
		}
		if (dirType == 3) {
			vel = new Point(600 * xDir, -250);
		}
		if (dirType == 4) {
			vel = new Point(600 * xDir, 250);
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir,
			new byte[] { (byte)spriteType, (byte)dirType});
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new RaySplasherProj(
			args.pos, args.xDir, args.extraData[0], args.extraData[1],
			args.owner, args.player, args.netId
		);
	}

	public override void onReflect() {
		base.onReflect();
		if (ownedByLocalPlayer) {
			float randY = Helpers.randomRange(-2f, 1f);
			vel.y *= randY;
			forceNetUpdateNextFrame = true;
		}
	}
}

public class RaySplasherTurret : Actor, IDamagable {
	MegamanX? mmx;
	int state = 0;
	Actor? target;
	float health = 4;
	float maxHealth = 4;
	const float range = 130;
	float drainTime;
	float raySplasherShootTime;
	int raySplasherMod;
	float velY;
	static ShaderWrapper replaceColorShaderAlly = Helpers.cloneShaderSafe("replacecolor");
	static ShaderWrapper replaceColorShaderBlue = Helpers.cloneShaderSafe("replacecolor");
	static ShaderWrapper replaceColorShaderRed = Helpers.cloneShaderSafe("replacecolor");
	ShaderWrapper? replaceColorShader;

	public RaySplasherTurret(
		Point pos, Player player, int xDir, ushort netId, bool ownedByLocalPlayer, bool rpc = false, MegamanX? mmx = null
	) : base("raysplasher_turret_start", pos, netId, ownedByLocalPlayer, false
	) {
		this.mmx = mmx;
		useGravity = false;
		velY = -50;

		this.xDir = xDir;

		removeRenderEffect(RenderEffectType.BlueShadow);
		removeRenderEffect(RenderEffectType.RedShadow);

		replaceColorShader = replaceColorShaderAlly;
		if (replaceColorShader != null) {
			Vec4 origColor = new Vec4(8 / 255f, 8 / 255f, 8 / 255f, 0);
			if (player.isMainPlayer) {
				replaceColorShader = replaceColorShaderAlly;
				replaceColorShader.SetUniform("origColor", origColor);
				replaceColorShader.SetUniform("replaceColor", new Vec4(0, 0.75f, 0, 0.5f));
			} else if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance) {
				replaceColorShader = replaceColorShaderRed;
				replaceColorShader.SetUniform("origColor", origColor);
				replaceColorShader.SetUniform("replaceColor", new Vec4(0.75f, 0, 0, 0.5f));
			} else if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.blueAlliance) {
				replaceColorShader = replaceColorShaderBlue;
				replaceColorShader.SetUniform("origColor", origColor);
				replaceColorShader.SetUniform("replaceColor", new Vec4(0, 0, 0.75f, 0.5f));
			}
		}

		netOwner = player;
		netActorCreateId = NetActorCreateId.RaySplasherTurret;
		if (rpc) {
			createActorRpc(player.id);
		}
	}

	public override List<ShaderWrapper>? getShaders() {
		if (replaceColorShader != null) {
			return new List<ShaderWrapper>() { replaceColorShader };
		}
		return null;
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (state == 1 || state == 2) {
			drainTime += Global.spf;
			if (drainTime >= 4) {
				destroySelf();
				return;
			}
		}

		if (state == 0) {
			var hits = Global.level.getTerrainTriggerList(this, new Point(0, velY * Global.spf), typeof(Wall));
			if (hits.Count == 0) {
				move(new Point(0, velY));
			}
			velY += Global.spf * 75;
			if (velY >= 0) {
				velY = 0;
				state = 1;
				changeSprite("raysplasher_turret", true);
			}
		} else if (state == 1) {
			isStatic = true;
			Actor? closestTarget = null;
			if (netOwner != null) {
				closestTarget = Global.level.getClosestTarget(pos, netOwner.alliance, true, aMaxDist: range);
			}
			if (closestTarget != null) {
				target = closestTarget;
				state = 2;
				changeSprite("raysplasher_turret_fire", true);
				playSound("raySplasher");
			}
		} else if (state == 2) {
			if (target != null) {
				Actor oldTarget = target;
			}
			if (netOwner != null) {
				target = Global.level.getClosestTarget(pos, netOwner.alliance, true);
			}
			if (target == null || pos.distanceTo(target.getCenterPos()) >= range) {
				state = 1;
				target = null;
				changeSprite("raysplasher_turret", true);
			} else {
				raySplasherShootTime += Global.spf;
				if (raySplasherShootTime > 0.1f) {
					float ang = pos.directionToNorm(target.getCenterPos()).byteAngle;
					byteAngle += Helpers.randomRange(-22.5f, 22.5f);

					new RaySplasherTurretProj(
						pos, (pos.x > target.getCenterPos().x ? -1 : 1), raySplasherMod % 3, ang,
						this, netOwner!, netOwner!.getNextActorNetId(), rpc: true
					);
					//float randAngle = Helpers.randomRange(0, 360);
					//proj.vel = new Point(600 * Helpers.cosd(randAngle), 600 * Helpers.sind(randAngle));

					raySplasherShootTime = 0;
					raySplasherMod++;
				}
			}
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (projId == (int)ProjIds.SpinningBlade || projId == (int)ProjIds.SpinningBladeCharged) {
			damage *= 2;
		}

		addDamageTextHelper(owner, damage, 4, false);

		health -= damage;
		if (health <= 0) {
			health = 0;
			if (ownedByLocalPlayer) destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		if (sprite.name == "raysplasher_turret_start") return false;
		return projId == (int)ProjIds.RaySplasherHurtSelf || netOwner?.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return netOwner?.alliance == healerAlliance && health < maxHealth;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
		health += healAmount;
		if (drawHealText && healer != netOwner && ownedByLocalPlayer) {
			addDamageTextHelper(netOwner, -healAmount, 16, sendRpc: true);
		}
		if (health > maxHealth) health = maxHealth;
	}

	public override void onDestroy() {
		base.onDestroy();
		playSound("freezebreak2");
		new Anim(pos, "raysplasher_turret_pieces", 1, null, false) { ttl = 2, useGravity = true, vel = Point.random(-150, -50, -100, -50), frameIndex = 0, frameSpeed = 0 };
		new Anim(pos, "raysplasher_turret_pieces", -1, null, false) { ttl = 2, useGravity = true, vel = Point.random(50, 150, -100, -50), frameIndex = 0, frameSpeed = 0 };
		new Anim(pos, "raysplasher_turret_pieces", 1, null, false) { ttl = 2, useGravity = true, vel = Point.random(-150, -50, -100, -50), frameIndex = 1, frameSpeed = 0 };
		new Anim(pos, "raysplasher_turret_pieces", -1, null, false) { ttl = 2, useGravity = true, vel = Point.random(50, 150, -100, -50), frameIndex = 1, frameSpeed = 0 };
		mmx?.rayTurrets.Remove(this);
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (netOwner?.alliance == Global.level.mainPlayer.alliance) {
			float healthBarInnerWidth = 14;
			Color color = new Color();

			float healthPct = health / maxHealth;
			float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
			if (healthPct > 0.66) color = Color.Green;
			else if (healthPct <= 0.66 && healthPct >= 0.33) color = Color.Yellow;
			else if (healthPct < 0.33) color = Color.Red;

			float offY = 12;
			DrawWrappers.DrawRect(pos.x - 8, pos.y - 3 - offY, pos.x + 8, pos.y - offY, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
			DrawWrappers.DrawRect(pos.x - 7, pos.y - 2 - offY, pos.x - 7 + width, pos.y - 1 - offY, true, color, 0, ZIndex.HUD - 1);
		}

		if (netOwner?.isMainPlayer == true && replaceColorShader == null) {
			Global.sprites["cursorchar"].draw(0, pos.x + x, pos.y + y - 17, 1, 1, null, 1, 1, 1, zIndex + 1);
		}

		renderDamageText(10);
	}

	public bool isPlayableDamagable() {
		return false;
	}
}


public class RaySplasherTurretProj : Projectile {
	public RaySplasherTurretProj(
		Point pos, int xDir, int spriteType, float byteAngle, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base (
		pos, xDir, owner, "raysplasher_proj", netId, player
	) {
		weapon = RaySplasher.netWeapon;
		damager.damage = 1;
		maxTime = 0.25f;
		projId = (int)ProjIds.RaySplasherChargedProj;
		frameIndex = spriteType;
		frameSpeed = 0;
		fadeSprite = "raysplasher_fade";
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle).times(600);
			
		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle, 
			new byte[] { (byte)spriteType,});
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new RaySplasherTurretProj(
			args.pos, args.xDir, args.extraData[0], args.byteAngle,
			args.owner, args.player, args.netId
		);
	}
}


public class RaySplasherChargedState : CharState {
	MegamanX mmx = null!;
	bool fired = false;
	public RaySplasherChargedState() : base("point_up") {
		superArmor = true;

		landSprite = "point_up";
		airSprite = "point_up_air";
	}

	public override void update() {
		base.update();

		if (character.frameIndex >= 4 && !fired) {
			fired = true;
			var turret = new RaySplasherTurret(
				character.getShootPos(), player, character.xDir, 
				player.getNextActorNetId(), character.ownedByLocalPlayer, rpc: true,
				mmx
			);
			mmx.rayTurrets.Add(turret);
			if (mmx.rayTurrets.Count > 1) {
				mmx.rayTurrets[0].destroySelf();
				mmx.rayTurrets.RemoveAt(0);
			}
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = new Point();
		mmx = character as MegamanX ?? throw new NullReferenceException();
		if (!character.grounded) {
			sprite = airSprite;
			character.changeSpriteFromName(sprite, true);
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
