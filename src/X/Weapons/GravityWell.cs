using System.Collections.Generic;
using System;
namespace MMXOnline;

public class GravityWell : Weapon {
	public static GravityWell netWeapon = new();

	public GravityWell() : base() {
		shootSounds = new string[] { "busterX3", "busterX3", "busterX3", "warpIn" };
		fireRate = 30;
		index = (int)WeaponIds.GravityWell;
		weaponBarBaseIndex = 22;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 22;
		killFeedIndex = 45;
		weaknessIndex = (int)WeaponIds.RaySplasher;
		damage = "2/4";
		effect = "Disables Gravity to the enemy. C: Super Armor.\nUncharged won't give assists.";
		hitcooldown = "0.5";
		Flinch = "0/26";
		maxAmmo = 16;
		ammo = maxAmmo;
		hasCustomChargeAnim = true;
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
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (chargeLevel < 3) {
			var proj = new GravityWellProj(pos, xDir, mmx, player, player.getNextActorNetId(), true);
			if (character.ownedByLocalPlayer) {
				mmx.linkedGravityWell = proj;
			}
		} else {
			if (!character.ownedByLocalPlayer) return;
			character.changeState(new GravityWellChargedState(), true);
		}
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (player.character is not MegamanX mmx) {
			return false;
		}
		if (chargeLevel >= 3 || mmx.stockedBuster == true) {
			return base.canShoot(chargeLevel, player) && (
				mmx.chargedGravityWell == null || mmx.chargedGravityWell.destroyed
			);
		}
		return base.canShoot(chargeLevel, player) && (mmx.linkedGravityWell == null || mmx.linkedGravityWell.destroyed);
	}
}

public class GravityWellProj : Projectile, IDamagable {
	public int state = 0;
	int wellFrameIndex = 0;
	float wellFrameTime = 0;
	float activeTime;
	float maxActiveTime;
	public Anim? wellAnim;
	float health = 2;
	float velX;

	public GravityWellProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "gravitywell_start", netId, player
	) {
		weapon = GravityWell.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		maxActiveTime = 2;
		maxTime = maxActiveTime + 5;
		projId = (int)ProjIds.GravityWell;
		shouldShieldBlock = false;
		destroyOnHit = false;
		velX = 285 * xDir;
		setzIndex(zIndex + 100);
		Global.level.unchargedGravityWells.Add(this);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}

		//if (player.isMainPlayer) {
			//removeRenderEffect(RenderEffectType.BlueShadow);
			//removeRenderEffect(RenderEffectType.RedShadow);
			//addRenderEffect(RenderEffectType.GreenShadow);
		//}

		if (!ownedByLocalPlayer) {
			vel = new Point();
		}
		canBeLocal = false;
	}
	
	public static Projectile rpcInvoke(ProjParameters arg) {
		return new GravityWellProj(
			arg.pos, arg.xDir, arg.owner, arg.player, arg.netId
		);
	}

	public bool active() {
		return sprite?.name == "gravitywell_proj";
	}

	public void startState1() {
		velX = 0;
		state = 1;
		changeSprite("gravitywell_start1", false);
		if (commandShoot()) {
			changeSprite("gravitywell_proj", false);
		}
		if (time >= 24f/60f || commandShoot()) {
			wellAnim = new Anim(pos, "gravitywell_well_start", xDir, owner.getNextActorNetId(), false, true, true);
		}
		playSound("gravityWell", sendRpc: true);
	}

	public bool commandShoot() {
		return owner.input.isPressed(Control.Shoot, owner) && owner.weapon is GravityWell;
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;
		if (state == 0) {
			var hits = Global.level.getTerrainTriggerList(this, new Point(velX * Global.spf, 0), typeof(Wall));
			if (hits.Count == 0) {
				move(new Point(velX, 0));
			}

			if (xDir == 1) {
				velX -= Global.spf * 600;
				if (velX < 0) velX = 0;
			} else {
				velX += Global.spf * 600;
				if (velX > 0) velX = 0;
			}

			if (sprite.time >= 0.525 || commandShoot()) {
				startState1();
			}
		} else if (state == 1) {
			wellFrameTime += Global.spf;
			if (wellFrameTime > 0.06f) {
				wellFrameTime = 0;
				wellFrameIndex++;
				if (wellFrameIndex > 3) {
					wellFrameIndex = 0;
					state = 2;
					wellAnim?.changeSprite("gravitywell_well", true);
				}
			}
		}
		  // Active
		  else if (state == 2) {
			activeTime += Global.spf;
			if (time >= 12/60f) {
				changeSprite("gravitywell_proj", false);
			}
			int xDir = Helpers.randomRange(0, 1) == 0 ? 1 : -1;
			int yDir = Helpers.randomRange(0, 1) == 0 ? 1 : -1;
			if (wellAnim != null) {
				wellAnim.xDir = xDir;
				wellAnim.yDir = yDir;
			}

			if (activeTime > maxActiveTime || (activeTime > 0.01f && commandShoot())) {
				state = 3;
				wellAnim?.changeSprite("gravitywell_well_end", true);
			}
			wellFrameTime += Global.spf;
			if (wellFrameTime > 0.06f) {
				wellFrameTime = 0;
				wellFrameIndex++;
				if (wellFrameIndex > 3) {
					wellFrameIndex = 0;
				}
			}
		} else if (state == 3) {
			wellFrameTime += Global.spf;
			if (wellFrameTime > 0.06f) {
				wellFrameTime = 0;
				wellFrameIndex++;
				if (wellFrameIndex > 3) {
					state = 4;
					wellAnim?.destroySelf();
					frameIndex = 1;
					time = 0;
				}
			}
		} else if (state == 4) {
			if (owner.character == null || owner.character.destroyed) {
				state = 5;
			} else {
				/*
				if (time > 0.01f && commandShoot())
				{
					startState1();
				}
				else
				*/
				{
					var targetPos = owner.character.getCenterPos();
					moveToPos(targetPos, 300);
					changeSprite("gravitywell_start", false);
					if (pos.distanceTo(targetPos) < 10) {
						state = 5;
					}
				}
			}
		} else if (state == 5) {
			destroySelf();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		wellAnim?.destroySelf();
		Global.level.unchargedGravityWells.Remove(this);
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (projId == (int)ProjIds.RaySplasher || projId == (int)ProjIds.RaySplasherTurret) damage *= 2;
		health -= damage;
		if (health <= 0) {
			fadeSound = "explosion";
			fadeSprite = "explosion";
			destroySelf();
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!damagable.isPlayableDamagable()) { return; }
		var actor = damagable.actor();
		if (actor is Character chr && (chr.isPushImmune() || chr.isSlowImmune() || chr.isFlinchImmune())) return;

		float mag = 100;
		if (!actor.grounded) actor.vel.y = 0;
		Point velVector = actor.getCenterPos().directionToNorm(pos).times(mag);
		actor.move(velVector, true);
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return owner.alliance != damagerAlliance; }
	public bool canBeHealed(int healerAlliance) { return false; }
	public void heal(float healAmount, bool allowStacking = true) { }
	public bool isInvincible(Player attacker, int? projId) { return false; }
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }

	public bool isPlayableDamagable() { return false; }
}

public class GravityWellProjCharged : Projectile, IDamagable {
	float health = 4;
	public bool started;
	float velY = -300;
	public GravityWellProjCharged(
		Point pos, int xDir, int yDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "gravitywell_charged", netId, player
	) {
		weapon = GravityWell.netWeapon;
		maxTime = 4;
		projId = (int)ProjIds.GravityWellCharged;
		shouldShieldBlock = false;
		destroyOnHit = false;
		shouldVortexSuck = false;
		this.yDir = yDir;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)yDir);
		}
		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new GravityWellProjCharged(
			arg.pos, arg.xDir, arg.extraData[0], arg.owner, arg.player, arg.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		var ceilHits = Global.level.getTriggerList(this, 0, velY * Global.spf, null, typeof(Wall));
		if (ceilHits.Count == 0) {
			move(new Point(0, velY));
		}

		velY += Global.spf * 400;
		if (velY > 0) {
			velY = 0;
			started = true;
			RPC.actorToggle.sendRpc(netId, RPCActorToggleType.StartGravityWell);
		}

		if (!started) return;

		Point gPos = pos.addxy(0, 112);

		if (Helpers.randomRange(0, 3) == 0) {
			int randomX = Helpers.randomRange(-125, 125);
			var anim = new Anim(gPos.addxy(randomX, 130 * yDir), "gravitywell_charged_part", 1, null, false);
			anim.ttl = 0.55f;
			anim.vel = new Point(0, -450 * yDir);
			anim.frameIndex = Helpers.randomRange(0, 5);
			anim.frameSpeed = 0;
		}

		var rect = new Rect(gPos.x - 149, gPos.y - 112, gPos.x + 149, gPos.y + 112);
		//DrawWrappers.DrawRect(rect.x1, rect.y1, rect.x2, rect.y2, true, new Color(255, 0, 0, 128), 1, ZIndex.HUD, isWorldPos: true);
		var hits = Global.level.checkCollisionsShape(rect.getShape(), new List<GameObject>() { this });

		foreach (CollideData other in hits) {
			var actor = other.gameObject as Actor;
			var chr = other.gameObject as Character;
			var ra = other.gameObject as RideArmor;
			var rc = other.gameObject as RideChaser;
			var maverick = other.gameObject as Maverick;

			if (actor != null && actor.ownedByLocalPlayer) {
				if (chr != null && chr.player.alliance == damager.owner.alliance) continue;
				if (chr != null && chr.isPushImmune()) continue;
				if (ra != null && ra.character == null) continue;
				if (ra != null && ra.player != null && ra.player.alliance == damager.owner.alliance) continue;
				if (rc != null && rc.character == null) continue;
				if (rc != null && rc.player != null && rc.player.alliance == damager.owner.alliance) continue;
				if (maverick != null && maverick.player.alliance == damager.owner.alliance) continue;
				if (!actor.gravityWellable) continue;

				if (chr != null) chr.lastGravityWellDamager = owner;

				if (yDir == 1) {
					actor.gravityWellModifier = -1;
					actor.gravityWellTime = 0.15f;
					actor.grounded = false;
					if (actor.vel.y >= 0) {
						actor.vel.y = -1;
					}
				} else {
					actor.gravityWellModifier = 2f;
					actor.gravityWellTime = 0.15f;
				}

				if (chr != null) {
					chr.damageHistory.Add(new DamageEvent(owner, weapon.killFeedIndex, projId, true, Global.time));
					if (chr is Vile vile) {
						vile.rideArmorPlatform = null;
					}
				}
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;

		if (damager.owner.character is MegamanX mmx) {
			mmx.chargedGravityWell = null;
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (projId == (int)ProjIds.RaySplasher || projId == (int)ProjIds.RaySplasherTurret) damage *= 2;
		health -= damage;
		if (health <= 0) {
			fadeSound = "explosionX3";
			fadeSprite = "explosion";
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return owner.alliance != damagerAlliance; }
	public bool isInvincible(Player attacker, int? projId) { return false; }
	public bool canBeHealed(int healerAlliance) { return false; }
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
	public bool isPlayableDamagable() { return false; }
}

public class GravityWellChargedState : CharState {
	bool fired = false;
	MegamanX mmx = null!;
	public GravityWellChargedState() : base("point_up") {
		superArmor = true;
	}
	public override void update() {
		base.update();
		if (character.frameIndex >= 5 && !fired) {
			fired = true;
			stateTime = 0;
			if (mmx != null) {
				mmx.chargedGravityWell = new GravityWellProjCharged(
					character.getShootPos(), 1,
					player.input.isHeld(Control.Down, player) ? -1 : 1,
					mmx, player, player.getNextActorNetId(), rpc: true
				);
			}	
		}
		if (stateTime > 0.65f) {
			character.changeToIdleOrFall();
		}
		if (!character.grounded && character.frameIndex > 8) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		character.useGravity = false;
		character.vel = new Point();
		if (!character.grounded) {
			character.frameIndex = 2;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
		//character.chargedGravityWell?.destroySelf();
		//character.chargedGravityWell = null;
	}
}
