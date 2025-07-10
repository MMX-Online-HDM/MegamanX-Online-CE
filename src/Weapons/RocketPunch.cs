using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SFML.Graphics;

namespace MMXOnline;

public enum RocketPunchType {
	None = -1,
	GoGetterRight,
	SpoiledBrat,
	InfinityGig,
}

public class RocketPunch : Weapon {
	public float vileAmmoUsage;
	public string projSprite;
	public static RocketPunch netWeaponGGR = new RocketPunch(RocketPunchType.GoGetterRight);
	public static RocketPunch netWeaponSB = new RocketPunch(RocketPunchType.SpoiledBrat);
	public static RocketPunch netWeaponIG = new RocketPunch(RocketPunchType.InfinityGig);
	public RocketPunch(RocketPunchType rocketPunchType) : base() {
		index = (int)WeaponIds.RocketPunch;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 31;
		weaponSlotIndex = 45;
		type = (int)rocketPunchType;
		projSprite = "rocket_punch_proj";

		if (rocketPunchType == RocketPunchType.None) {
			displayName = "None";
			description = new string[] { "Do not equip a Rocket Punch." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
		} else if (rocketPunchType == RocketPunchType.GoGetterRight) {
			fireRate = 60;
			displayName = "Go-Getter Right";
			vileAmmoUsage = 8;
			projSprite = "rocket_punch_proj";
			description = new string[] { "A rocket punch sends your fist", "flying to teach enemies a lesson." };
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "3";
			hitcooldown = "0.5";
			Flinch = "13";
			FlinchCD = "1";
			effect = "Won't destroy on hit.";
		} else if (rocketPunchType == RocketPunchType.SpoiledBrat) {
			fireRate = 12;
			displayName = "Spoiled Brat";
			vileAmmoUsage = 8;
			projSprite = "rocket_punch_sb_proj";
			description = new string[] { "Though lacking in power, this", "rocket punch offers intense speed." };
			killFeedIndex = 77;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "2";
			hitcooldown = "0.1";
			Flinch = "13";
			FlinchCD = "1";
			effect = "Destroys on hit.";
		}
		if (rocketPunchType == RocketPunchType.InfinityGig) {
			fireRate = 60;
			displayName = "Infinity Gig";
			vileAmmoUsage = 16;
			projSprite = "rocket_punch_ig_proj";
			description = new string[] { "Advanced homing technology can be", "difficult to get a handle on." };
			killFeedIndex = 78;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "3";
			hitcooldown = "0.5";
			Flinch = "13";
			FlinchCD = "1";
			effect = "Homing,Travels further.";
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (vile.rocketPunchWeapon.type == (int)RocketPunchType.None) return;
		if (vile.charState is RocketPunchAttack && type != (int)RocketPunchType.SpoiledBrat) return;

		if (shootCooldown == 0 && vile.charState is not Dash && vile.charState is not AirDash) {
			if (vile.tryUseVileAmmo(vileAmmoUsage)) {
				vile.setVileShootTime(this);
				if (vile.charState is RocketPunchAttack rpa) {
					rpa.shoot();
				} else {
					vile.changeState(new RocketPunchAttack(), true);
				}
			}
		}
	}
}

public class RocketPunchProj : Projectile {
	public bool reversed;
	public bool returned;
	public float maxReverseTime;
	public float minTime;
	public float smokeTime;
	public Actor? target;
	public int type = 0;
	public int num = 0;
	public RocketPunchProj(
		Point pos, int xDir, int num, string sprite,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, sprite , netId, player
	) {
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 30;
		vel = new Point (getSpeed(type) * xDir, 0);
		destroyOnHit = false;
		shouldShieldBlock = false;
		if (ownerPlayer.character != null) setzIndex(ownerPlayer.character.zIndex - 100);
		minTime = 0.2f;
		maxReverseTime = 0.4f;
		damager.flinch = Global.halfFlinch;
		this.num = num;
		if (num == (int)RocketPunchType.SpoiledBrat) {
			weapon = RocketPunch.netWeaponSB;
			damager.damage = 2;
			damager.hitCooldown = 6;
			maxTime = 0.25f;
			destroyOnHit = true;
			projId = (int)ProjIds.SpoiledBrat;
			sprite = "rocket_punch_sb_proj";
			type = 1;
		} else if (num == (int)RocketPunchType.InfinityGig) {
			weapon = RocketPunch.netWeaponIG;
			projId = (int)ProjIds.InfinityGig;
			sprite = "rocket_punch_ig_proj";
			type = 2;
		} else if (num == (int)RocketPunchType.GoGetterRight) {
			weapon = RocketPunch.netWeaponGGR;
			maxReverseTime = 0.3f;
			projId = (int)ProjIds.RocketPunch;
			type = 0;
			sprite = "rocket_punch_proj";
		}
		if (rpc) {
			List<Byte> extraBytes = new List<Byte> {
			};
			extraBytes.Add((byte)num);
			extraBytes.AddRange(Encoding.ASCII.GetBytes(sprite));
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraBytes.ToArray());

		}
		canBeLocal = false;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		string sprite = Encoding.ASCII.GetString(args.extraData[1..]);
		return new RocketPunchProj(
			args.pos, args.xDir, args.extraData[0], sprite, args.owner, args.player, args.netId
		);
	}

	public bool ownerExists => (owner.character?.destroyed == false);

	public override void update() {
		base.update();
		if (ownedByLocalPlayer && !ownerExists) {
			destroySelf("explosion", "explosion");
			return;
		}
		smokeTime += Global.spf;
		if (smokeTime > 0.08f) {
			smokeTime = 0;
			var smoke = new Anim(pos, "torpedo_smoke", xDir, null, true);
			smoke.setzIndex(zIndex - 100);
		}

		if (ownedByLocalPlayer && !reversed && reflectCount == 0 &&
			type == (int)RocketPunchType.InfinityGig
		) {
			if (target == null && owner.character != null) {
				var targets = Global.level.getTargets(owner.character.pos, damager.owner.alliance, true);
				foreach (var t in targets) {
					if (isFacing(t) && MathF.Abs(t.pos.y - owner.character.pos.y) < 120) {
						target = t;
						break;
					}
				}
			} else if (target != null && target.destroyed) {
				vel.x = 500 * xDir;
			} else if (target != null) {
				vel = new Point(0, 0);
				Point targetPos = target.getCenterPos();
				move(pos.directionToNorm(targetPos).times(500));
				if (pos.distanceTo(targetPos) < 5) {
					reversed = true;
				}
				forceNetUpdateNextFrame = true;
			}
		}
		if (!reversed && type == (int)RocketPunchType.GoGetterRight && damager.owner?.character is Vile vile) {
			if (vile.player.input.isHeld(Control.Up, vile.player)) {
				incPos(new Point(0, -300 * Global.spf));
			} else if (vile.player.input.isHeld(Control.Down, vile.player)) {
				incPos(new Point(0, 300 * Global.spf));
			}
		}
		if (!reversed && time > maxReverseTime) {
			reversed = true;
			vel.x = getSpeed(type) * -xDir;
		}
		if (reversed && owner.character != null) {
			vel = new Point(0, 0);
			if (pos.x > owner.character.pos.x) {
				xDir = -1;
			} else {
				xDir = 1;
			}
			Point returnPos = owner.character.getCenterPos();

			move(pos.directionToNorm(returnPos).times(getSpeed(type)));
			if (pos.distanceTo(returnPos) < 10) {
				returned = true;
				destroySelf();
			}
		}
	}

	/*
	public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;
		reversed = true;
	}
	*/

	public static float getSpeed(int type) {
		return type switch {
			(int)RocketPunchType.SpoiledBrat => 600,
			(int)RocketPunchType.InfinityGig => 500,
			_ => 500
		};
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (locallyControlled) {
			reversed = true;
		}
		if (isRunByLocalPlayer()) {
			reversed = true;
			RPC.actorToggle.sendRpc(netId, RPCActorToggleType.ReverseRocketPunch);
		}
	}
}

public class RocketPunchAttack : VileState {
	bool shot = false;
	RocketPunchProj? proj;
	float specialPressTime;

	public RocketPunchAttack(string transitionSprite = "") : base("rocket_punch", "", "", transitionSprite) {
	}

	public override void update() {
		base.update();

		Helpers.decrementTime(ref specialPressTime);

		if (proj != null && !player.input.isHeld(Control.Special1, player) && proj.time >= proj.minTime) {
			proj.reversed = true;
		}

		if (!shot && character.sprite.frameIndex == 1) {
			shoot();
		}
		if (proj != null) {
			if (vile.rocketPunchWeapon.type == (int)RocketPunchType.SpoiledBrat) {
				if (player.input.isPressed(Control.Special1, player)) {
					specialPressTime = 0.25f;
				}

				if (specialPressTime > 0 && (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player))) {
					character.frameIndex = 1;
					character.frameTime = 0;
				} else if (character.isAnimOver()) {
					character.changeToIdleOrFall();
					return;
				}
			} else {
				if (proj.returned || proj.destroyed) {
					character.changeToIdleOrFall();
					return;
				}
			}
		}
	}

	public void shoot() {
		shot = true;
		character.playSound("rocketPunch", sendRpc: true);
		character.frameIndex = 1;
		character.frameTime = 0;
		var poi = character.sprite.getCurrentFrame().POIs[0];
		poi.x *= character.xDir;
		if (vile.rocketPunchWeapon.type == (int)RocketPunchType.GoGetterRight) {
			proj = new RocketPunchProj(
				character.pos.add(poi), character.xDir, 0, vile.rocketPunchWeapon.projSprite,
				vile, character.player, character.player.getNextActorNetId(), rpc: true
			);
		}
		else if (vile.rocketPunchWeapon.type == (int)RocketPunchType.SpoiledBrat) {
			proj = new RocketPunchProj(
				character.pos.add(poi), character.xDir, 1, vile.rocketPunchWeapon.projSprite,
				vile, character.player, character.player.getNextActorNetId(), rpc: true
			);
		}
		else if (vile.rocketPunchWeapon.type == (int)RocketPunchType.InfinityGig) {
			proj = new RocketPunchProj(
				character.pos.add(poi), character.xDir, 2, vile.rocketPunchWeapon.projSprite,
				vile, character.player, character.player.getNextActorNetId(), rpc: true
			);
		}
	}

	public void reset() {
		character.frameIndex = 0;
		stateTime = 0;
		shot = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}
}
