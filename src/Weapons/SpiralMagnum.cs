using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SpiralMagnum : AxlWeapon {
	public SpiralMagnum(int altFire) : base(altFire) {
		shootSounds = new string[] { "spiralMagnum", "spiralMagnum", "spiralMagnum", "sniperMissile" };
		fireRate = 45;
		altFireCooldown = 120;
		index = (int)WeaponIds.SpiralMagnum;
		weaponBarBaseIndex = 34;
		weaponSlotIndex = 54;
		killFeedIndex = 69;
		rechargeAmmoCooldown = 120;
		altRechargeAmmoCooldown = 200;

		sprite = "axl_arm_spiralmagnum2";
		flashSprite = "axl_pistol_flash";
		chargedFlashSprite = "axl_pistol_flash";

		if (altFire == 1) {
			sprite = "axl_arm_spiralmagnum";
			altFireCooldown = 60;
			shootSounds[1] = "spiralMagnum";
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel == 3 && altFire == 0) {
			return 8;
		}
		return 4;
	}

	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		if (!player.ownedByLocalPlayer) return;
		if (player.character == null) return;
		if (player.character is not Axl axl) return;
		Point? bulletDir = Point.createFromAngle(angle);
		Projectile? bullet = null;
		Point? origPos = bulletPos;

		if (chargeLevel == 3 && altFire == 0) {
			axl.sniperMissileProj = new SniperMissileProj(weapon, bulletPos, player, bulletDir.Value, netId, rpc: true);
			RPC.playSound.sendRpc(shootSounds[3], axl.netId);
			return;
		}

		Point shellPos = bulletPos.add(axl.getAxlBulletDir().times(-20));
		var spiralMagnumShell = new SpiralMagnumShell(shellPos, -axl.xDir, player.getNextActorNetId(), sendRpc: true);

		if (axl.isZooming() == false) {
			bullet = new SpiralMagnumProj(weapon, bulletPos, 0, 0, player, bulletDir.Value, target, headshotTarget, netId);
		} else {
			float jumpDist = 0;
			if (headshotTarget != null || target != null) {
				bulletPos = axl.axlGenericCursorWorldPos;
				jumpDist = bulletPos.distanceTo(bulletPos);
			}
			bullet = new SpiralMagnumProj(weapon, bulletPos, jumpDist, 1, player, bulletDir.Value, target, headshotTarget, netId);			
			AssassinBulletTrailAnim trail = new AssassinBulletTrailAnim(origPos.Value, bullet);
			axl.zoomCharge = 0;
		}

		RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, origPos.Value, xDir, angle);
	}
}

public class SpiralMagnumShell : Anim {
	public int bounces;
	float angularVel = 0;
	bool stopped;
	float bounceCooldown;
	float timeNoYVel;
	public SpiralMagnumShell(Point pos, int xDir, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
		base(pos, "spiralmagnum_shell", 1, netId, false, sendRpc, ownedByLocalPlayer) {
		vel = new Point(xDir * 75, -150);
		collider.wallOnly = true;
		useGravity = true;
		angle = 0;
		if (xDir == -1) angularVel = -300;
		else angularVel = 300;
		ttl = 4;
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref bounceCooldown);
		if (MathF.Abs(vel.y) < 1) {
			timeNoYVel += Global.spf;
			if (timeNoYVel > 0.15f) {
				vel = new Point();
				stopped = true;
			}
		}
		if (!stopped && angle != null) {
			angle += angularVel * Global.spf;
			angle = Helpers.to360(angle.Value);
		} else {
			angle = 0;
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (other.gameObject is not Wall) return;
		if (stopped) return;
		if (MathF.Abs(vel.y) < 1) {
			playSound("dingX2", sendRpc: true);
			vel = new Point();
			stopped = true;
			return;
		}
		if (bounces > 0 && !stopped) {
			vel = new Point();
			stopped = true;
			return;
		}
		if (bounceCooldown > 0) return;

		bounces++;
		bounceCooldown = 0.5f;
		var normal = other.hitData.normal ?? new Point(0, -1);

		if (normal.isSideways()) {
			vel.x *= -0.5f;
			incPos(new Point(5 * MathF.Sign(vel.x), 0));
		} else {
			vel.y *= -0.5f;
			if (vel.y < -300) vel.y = -300;
			incPos(new Point(0, 5 * MathF.Sign(vel.y)));
		}
		playSound("dingX2", sendRpc: true);
	}
}

public class SpiralMagnumProj : Projectile {
	public Player player;
	public Character headshotChar;
	public IDamagable target;
	public Point destroyPos;
	public float distTraveled;

	public int passCount = 0;
	public int powerDecrements = 0;
	public bool isScoped;
	float dist;
	float maxDist;
	bool doubleDamageBonus;
	bool isHyper;
	bool playedSoundOnce;

	public SpiralMagnumProj(Weapon weapon, Point pos, float jumpDist, int type, Player player, Point bulletDir, IDamagable target, Character headshotChar, ushort netProjId) :
		base(weapon, pos, 1, 1000, 2, player, "spiralmagnum_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		destroyOnHit = false;
		this.target = target;
		reflectable = false;
		vel.x = bulletDir.x * 1000;
		vel.y = bulletDir.y * 1000;
		angle = bulletDir.angle;
		projId = (int)ProjIds.SpiralMagnum;
		this.player = player;
		this.headshotChar = headshotChar;

		Axl? axl = player.character as Axl;

		if (type == 0) damager.damage = 1.5f;
		isHyper = axl?.isWhiteAxl() == true;
		isScoped = type == 1;
		maxTime = float.MaxValue;
		maxDist = Global.screenW / 2;
		if (isHyper) {
			maxDist *= 2;
		}
		if (isScoped) {
			projId = (int)ProjIds.SpiralMagnumScoped;
			maxDist = player.adjustedZoomRange;
			dist = jumpDist;
			damager.damage += MathF.Round(6 * (axl?.zoomCharge ?? 0));
			if (axl?.hasScopedTarget() != true) {
				damager.damage = 0;
			}
		}
		canBeLocal = false;
	}

	public bool playZing() {
		if (playedSoundOnce) return false;
		var mainCharPos = new Point(Global.level.camCenterX, Global.level.camCenterY);
		var ownerChar = owner?.character;
		if (Global.level.mainPlayer?.character != null && Global.level.mainPlayer.character == ownerChar) return false;
		if (ownerChar != null && ownerChar.getCenterPos().distanceTo(mainCharPos) < Global.screenW / 2) return false;
		if (mainCharPos.distanceTo(pos) > Global.screenW / 2) return false;
		return true;
	}

	public override void update() {
		if (playZing()) {
			playedSoundOnce = true;
			playSound("zing1");
		}

		if (!ownedByLocalPlayer) {
			base.update();
			return;
		}
		if (destroyed) return;

		bool weakness = false;

		if (isScoped && damager.damage > 0 && (target != null || headshotChar != null)) {
			var hitTarget = headshotChar ?? target;
			if (hitTarget != null) {
				weakness = (hitTarget.actor() as Character)?.isAlwaysHeadshot() == true || (headshotChar != null);
				float overrideDamage = weakness ? (damager.damage * Damager.headshotModifier) : damager.damage;
				damager.applyDamage(hitTarget, false, weapon, this, projId, overrideDamage: overrideDamage);
			}

			if (weakness) {
				playSound("hurt", sendRpc: true);
			}

			damager.damage = 0;
		}

		var col = Global.level.checkCollisionPoint(pos, new List<GameObject>() { player.character });

		float velXDist = vel.x * Global.spf;
		float velYDist = vel.y * Global.spf;

		weakness = false;
		IDamagable? victim = null;
		Point? hitPoint = null;

		float distPerFrame = speed * Global.spf;
		distTraveled += distPerFrame;

		if (distTraveled <= (maxTime * speed)) {
			weakness = getHeadshotVictim(player, out victim, out hitPoint);
			/*
			if (victim == null)
			{
				CollideData hit = Global.level.raycast(pos, pos.addxy(velXDist, velYDist), new List<Type>() { typeof(Actor) });
				IDamagable hitDamagable = hit?.gameObject as IDamagable;
				if (hitDamagable != null && hitDamagable.canBeDamaged(player.alliance, player.id, projId)) victim = hitDamagable;
				hitPoint = hit?.hitData?.hitPoint;
				Projectile proj = hit?.gameObject as Projectile;
				if (proj != null && (proj.isShield || proj.isDeflectShield || proj.isReflectShield))
				{
					destroySelf();
					playSound("m10ding");
					return;
				}
			}
			*/
		}

		if (victim != null) {
			var hitChar = victim as Character;

			bool canBlock = (
				reflectable && !weakness && hitChar != null && hitChar.player.isZero && (
					hitChar.sprite.name == "zero_block" ||
					(hitChar.sprite.name.Contains("zero_attack") &&
					hitChar.sprite.frameHitboxes[hitChar.sprite.getFrameIndexSafe()].Length > 0)
				)
			);

			if (hitChar != null && canBlock && hitChar.sprite.name != "zero_attack_air2") {
				canBlock = canBlock && hitChar.xDir == -MathF.Sign(vel.x);
				if (player.character.pos.distanceTo(hitChar.pos) < 50) {
					canBlock = false;
				}
			}

			if (hitChar != null && canBlock) {
				player = hitChar.player;
				damager.owner = hitChar.player;
				vel.x *= -1;
				vel.y *= -1;
				weakness = false;
				time = 0;
				playSound("m10ding");
				Global.serverClient?.rpc(RPC.playerToggle, (byte)damager.owner.id, (byte)RPCToggleType.PlayDingSound);
				return;
			}

			if (hitChar != null && !hitChar.isInvulnerable()) {
				// Headshots
				if (hitChar.isAlwaysHeadshot()) {
					weakness = true;
				}

				if (hitPoint != null) {
					changePos(hitPoint.Value);
				}
			}

			if (hitChar is MegamanX rockmanX && rockmanX.chargedRollingShieldProj != null) {
				float decAmount = damager.damage * 2;
				rockmanX.chargedRollingShieldProj.decAmmo(decAmount);
				var bytes = BitConverter.GetBytes(decAmount);
				Global.serverClient?.rpc(RPC.decShieldAmmo, (byte)hitChar.player.id, bytes[0], bytes[1], bytes[2], bytes[3]);
			} else {
				float overrideDamage = weakness ? (damager.damage * Damager.headshotModifier) : damager.damage;
				if (weapon is AssassinBullet && weakness) {
					overrideDamage = Damager.ohkoDamage;
				}
				//DevConsole.log("Weakness: " + weakness.ToString() + ",bd:" + damager.damage.ToString() + ",");
				damager.applyDamage(victim, false, weapon, this, projId, overrideDamage: overrideDamage);
				if (hitChar != null) {
					if (weakness) {
						//hitChar.addDamageText("Headshot!", false);
						playSound("hurt");
					}
				}
			}
		}

		dist += deltaPos.magnitude;
		if (dist >= maxDist) {
			destroySelf();
			return;
		}

		base.update();
	}

	public void increasePassCount(int amount) {
		if (isHyper) return;
		if (!ownedByLocalPlayer) return;

		bool damageChanged = false;
		if (doubleDamageBonus) {
			doubleDamageBonus = false;
			damager.damage /= 2;
			damageChanged = true;
		}
		passCount += amount;
		if (passCount >= 5) {
			passCount = 0;
			powerDecrements++;
			damager.damage *= 0.5f;
			damageChanged = true;
			vel = vel.times(0.5f);
		}

		if (damageChanged) {
			updateDamager();
		}

		if (powerDecrements > 2) {
			destroySelf();
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		increasePassCount(1);
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		increasePassCount(1);
	}
}

public class SniperMissileProj : Projectile, IDamagable {
	public Character? target;
	public float health = 6;
	public float maxHealth = 6;
	public float blinkTime;
	public float maxBlinkTime = 1;
	float? holdStartAngle;
	const float maxTimeConst = 5f;
	float turnSpeed = 1;
	Axl? axl;

	public SniperMissileProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool rpc = false) :
		base(weapon, pos, 1, 150, 0, player, "snipermissile_proj", 0, 0f, netProjId, player.ownedByLocalPlayer) {
		maxTime = maxTimeConst;
		axl = player.character as Axl;

		if (axl?.isWhiteAxl() == true) {
			maxTime = 1000;
			health = 12;
			speed *= 1.5f;
			turnSpeed = 2;
		}
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		projId = (int)ProjIds.SniperMissile;
		blinkTime = maxBlinkTime;
		updateAngle();

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public void updateAngle() {
		angle = vel.angle;
	}

	public void updateVel() {
		if (angle != null) {
			vel.x = Helpers.cosd(angle.Value) * speed;
			vel.y = Helpers.sind(angle.Value) * speed;
		}
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		speed += (Global.spf * 25);

		if (blinkTime > 0) {
			blinkTime -= Global.spf;
			if (blinkTime < 0) {
				blinkTime = maxBlinkTime * (1 - (time / maxTime));
				addRenderEffect(RenderEffectType.Hit, 3, 6);
			}
		}

		if (!ownedByLocalPlayer) return;

		if (owner.isMainPlayer && time > 0.25f && axl != null) {
			if (Options.main.axlAimMode == 2) {
				if (pos.distanceTo(axl.axlCursorWorldPos) > 25) {
					//vel = Point.lerp(vel, pos.directionToNorm(owner.axlCursorWorldPos).times(speed), Global.spf * 2.5f);

					float destAngle = pos.directionToNorm(axl.axlCursorWorldPos).angle;
					if (angle != null) {
						if (MathF.Abs(angle.Value - destAngle) > 3) {
							angle = Helpers.moveAngle(angle.Value, destAngle, Global.spf * 200 * turnSpeed);
						}
					}
				}
				updateVel();
			} else {
				if (owner.input.isHeld(Control.Up, owner) && angle != null) {
					int sign = -1;
					if (holdStartAngle == null) holdStartAngle = angle;
					if (holdStartAngle < 90 || holdStartAngle > 270) {
						sign = 1;
					}
					angle -= sign * Global.spf * 150 * turnSpeed;
					angle = Helpers.to360(angle.Value);
					updateVel();
				} else if (owner.input.isHeld(Control.Down, owner) && angle != null) {
					int sign = -1;
					if (holdStartAngle == null) holdStartAngle = angle;
					if (holdStartAngle < 90 || holdStartAngle > 270) {
						sign = 1;
					}
					angle += sign * Global.spf * 150 * turnSpeed;
					angle = Helpers.to360(angle.Value);
					updateVel();
				} else {
					holdStartAngle = null;
				}
			}
		}

		if (time > 0.5f && (owner.input.isPressed(Control.Shoot, owner) || owner.input.isPressed(Control.Special1, owner))) {
			destroySelf();
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		destroySelf();
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (axl != null && axl.sniperMissileProj == this) {
			axl.playSound("grenadeExplode");
		}
		new SniperMissileExplosionProj(
			weapon, pos, xDir,
			Helpers.clamp(MathF.Ceiling(1 + time), 2, 1000),
			owner, owner.getNextActorNetId()
		);
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) destroySelf();
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return owner.alliance == healerAlliance;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
		health += healAmount;
		if (health > maxHealth) health = maxHealth;
	}

	public bool isPlayableDamagable() {
		return false;
	}
}

public class SniperMissileExplosionProj : Projectile {
	public IDamagable? directHit;
	public int directHitXDir;
	Axl? axl;

	public SniperMissileExplosionProj(Weapon weapon, Point pos, int xDir, float damage, Player player, ushort netProjId) :
		base(weapon, pos, xDir, 0, damage, player, "axl_grenade_explosion2", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer) {
		this.xDir = xDir;
		destroyOnHit = false;
		projId = (int)ProjIds.SniperMissileBlast;
		playSound("grenadeExplode");
		shouldShieldBlock = false;
		axl = player.character as Axl;

		if (ownedByLocalPlayer) {
			rpcCreate(pos, owner, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (isAnimOver()) {
			destroySelf();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
		if (axl != null) {
			axl.sniperMissileProj = null;
		}
	}

	public override DamagerMessage? onDamage(IDamagable damagable, Player attacker) {
		Character? character = damagable as Character;

		if (character != null) {
			bool directHit = this.directHit == character;
			int directHitXDir = this.directHitXDir;
			float ownAxlFactor = 1;

			var victimCenter = character.getCenterPos();
			var bombCenter = pos;
			if (directHit) {
				bombCenter.x = victimCenter.x - (directHitXDir * 5);
			}
			var dirTo = bombCenter.directionTo(victimCenter);
			var distFactor = Helpers.clamp01(1 - (bombCenter.distanceTo(victimCenter) / 60f));

			character.vel.y = dirTo.y * 25 * distFactor * ownAxlFactor;
			if (character == attacker.character) {
				character.xSwingVel = dirTo.x * 12 * distFactor * ownAxlFactor;
				float damage = damager.damage;
				if (axl?.isWhiteAxl() == true) {
					damage = 0;
				}
				return new DamagerMessage() {
					damage = damage,
					flinch = 0
				};
			} else {
				character.xPushVel = dirTo.x * 25 * distFactor * ownAxlFactor;
			}
		}

		return null;
	}
}

