using System;
using System.Collections.Generic;

namespace MMXOnline;

public enum VileBallType {
	None = -1,
	ExplosiveRound,
	SpreadShot,
	PeaceOutRoller,
}
public class VileBall : Weapon {
	public float vileAmmoUsage;
	public VileBall() : base() {
		index = (int)WeaponIds.VileBomb;
		weaponBarBaseIndex = 27;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 15;
	}
}
public class ExplosiveRound : VileBall {
	public static ExplosiveRound netWeapon = new();
	public ExplosiveRound() : base() {
		type = (int)VileBallType.ExplosiveRound;
		displayName = "Explosive Round";
		fireRate = 60;
		vileAmmoUsage = 8;
		vileWeight = 3;
		ammousage = vileAmmoUsage;
		damage = "2";
		hitcooldown = "0.2";
		effect = "Splits on ground.";
	}
	public override void vileShoot(Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new BallAttacks(this), true);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage);
		new VileBombProj(
			character.pos, character.xDir, 0, character, character.player,
			character.player.getNextActorNetId(), rpc: true
		);
	}
}
public class SpreadShot : VileBall {
	public static SpreadShot netWeapon = new();
	public SpreadShot() : base() {
		type = (int)VileBallType.SpreadShot;
		displayName = "Spread Shot";
		vileAmmoUsage = 5;
		fireRate = 60;
		killFeedIndex = 55;
		vileWeight = 3;
		ammousage = vileAmmoUsage;
		damage = "1";
		effect = "Stuns Enemies. CD: 2";
	}
	public override void vileShoot(Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new BallAttacks(this), true);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage);
		int num = args[0];
		new StunShotProj2(
			character.pos, character.xDir, num,
			0, character, character.player,
			character.player.getNextActorNetId(), rpc: true
		);
	}
}
public class PeaceOutRoller : VileBall {
	public static PeaceOutRoller netWeapon = new();
	public PeaceOutRoller() : base() {
		type = (int)VileBallType.PeaceOutRoller;
		displayName = "Peace Out Roller";
		vileAmmoUsage = 16;
		fireRate = 75;
		killFeedIndex = 80;
		vileWeight = 3;
		ammousage = vileAmmoUsage;
		damage = "3";
		hitcooldown = "0.5";
		flinch = "6";
		effect = "Splits on ground.";
	}
	public override void vileShoot(Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < vileAmmoUsage) return;
		vile.changeState(new BallAttacks(this), true);
	}
	public override void shoot(Character character, int[] args) {
		if (character is not Vile vava) return;
		vava.setVileShootTime(this);
		vava.tryUseVileAmmo(vileAmmoUsage);
		new PeaceOutRollerProj(
			character.pos, character.xDir, 0, character, character.player,
			character.player.getNextActorNetId(), rpc: true
		);
	}
}
public class NoneBall : VileBall {
	public static NoneBall netWeapon = new();
	public NoneBall() : base() {
		type = (int)VileBallType.None;
		displayName = "None";
		killFeedIndex = 126;
	}
}
#region States
public class BallAttacks : VileState {
	public string sound = "FireNappalmMK2";
	public bool soundPlayed;
	public int soundFrame = 2;
	public bool shot;
	public int shootFrame = 2;
	public VileBall weapon;
	public int bombNum;
	public BallAttacks(VileBall weapon) : base("crouch_nade") {
		airSprite = "air_bomb_attack";
		useDashJumpSpeed = true;
		this.weapon = weapon;
	}

	public override void update() {
		base.update();

		if (vile.energy.ammo < weapon.vileAmmoUsage) {
			character.changeToCrouchOrFall();
			return;
		}

		if (bombNum > 0 && player.input.isPressed(Control.Special1, player)) {
			character.changeToCrouchOrFall();
			return;
		}

		if (character.sprite.frameIndex >= soundFrame && !soundPlayed) {
			character.playSound(sound, forcePlay: false, sendRpc: true);
			soundPlayed = true;
		}

		if (character.frameIndex >= shootFrame) {
			character.turnToInput(player.input, player);
			if (weapon is ExplosiveRound) {
				float[] shootTimes = { 7f / 60f, 20f / 60f, 34f / 60f };
				if (bombNum < shootTimes.Length && stateTime > shootTimes[bombNum]) {
					weapon.shoot(vile, []);
					bombNum++;
				}
				if (stateTime > 44f / 60f) character.changeToCrouchOrFall();
			} else if (weapon is SpreadShot) {
				for (int i = 0; i < 7; i++) {
					if (stateTime > i * 0.1f && bombNum == i) {
						bombNum++;
						weapon.shoot(vile, [i + 1]);
					}
				}
				if (stateTime > 44f / 60f) character.changeToCrouchOrFall();
			} else if (weapon is PeaceOutRoller && !shot) {
				weapon.shoot(vile, []);
				shot = true;
			}
		}

		if (shot && stateTime > 14f / 60f) {
			character.changeToCrouchOrFall();
		}

		if (stateTime > 60f / 60f) character.changeToCrouchOrFall();

	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.turnToInput(player.input, player);
		if (!character.grounded) {
			sprite = "air_bomb_attack";
			character.changeSpriteFromName(sprite, true);
			character.useGravity = false;
			character.vel = new Point();
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
#endregion
public class VileElectricBomb : Weapon {
	public static VileElectricBomb netWeapon = new();
	public VileElectricBomb() : base() {
		fireRate = 60;
		index = (int)WeaponIds.VileBomb;
		weaponBarBaseIndex = 55;
		weaponBarIndex = 55;
		killFeedIndex = 55;
	}
}
#region Projectiles
public class VileBombProj : Projectile {
	public int type = 0;
	bool splitOnce;
	public VileBombProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "vile_bomb_air", netId, player
	) {
		weapon = ExplosiveRound.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 12;
		this.type = type;
		if (type == 0) {
			vel = new Point(100 * xDir, 0);
			maxTime = 0.45f;
		}
		if (type == 1) {
			vel = new Point(100 * xDir, -200);
			maxTime = 0.7f;
		}
		projId = (int)ProjIds.VileBomb;
		destroyOnHit = true;
		useGravity = true;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
		projId = (int)ProjIds.VileBombSplit;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new VileBombProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}
	public override void onHitWall(CollideData other) {
		destroySelf(disableRpc: true);
		if (!ownedByLocalPlayer) return;
		split();
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) {
			split();
		}
		base.onHitDamagable(damagable);
	}

	public void split() {
		if (!ownedByLocalPlayer) return;
		if (!splitOnce) splitOnce = true;
		else return;
		new VileBombSplitProj(
			pos, xDir, 0, this, owner,
			owner.getNextActorNetId(), rpc: true);
		new VileBombSplitProj(
			pos, xDir, 1, this, owner,
			owner.getNextActorNetId(),  rpc: true);
	}
}
public class VileBombSplitProj : Projectile {
	public int type = 0;
	public VileBombSplitProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "vile_bomb_ground", netId, player
	) {
		weapon = ExplosiveRound.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 12;
	    maxTime = 0.3f;
		projId = (int)ProjIds.VileBombSplit;
		fadeSprite = "vile_stun_shot_fade";
		fadeSound = "explosion";
		destroyOnHit = true;
		useGravity = false;
		this.type = type;
		if (type == 0) vel = new Point(250 , 0);
		if (type == 1) vel = new Point(-250 , 0);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new VileBombSplitProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}
}
public class PeaceOutRollerProj : Projectile {
	public int type = 0;
	bool splitOnce;
	public PeaceOutRollerProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "ball_por_proj", netId, player
	) {
		weapon = PeaceOutRoller.netWeapon;
		damager.damage = 3f;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 30;
		this.type = type;
		if (type == 0) {
			vel = new Point(75 * xDir, 50);
			maxTime = 0.5f;
		}
		if (type == 1) {
			vel = new Point(100 * xDir, -150);
			maxTime = 1;
			zIndex = ZIndex.Character;
		}
		gravityModifier = 0.5f;
		projId = (int)ProjIds.PeaceOutRoller;
		destroyOnHit = false;
		useGravity = true;
		xScale = 0.75f;
		yScale = 0.75f;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
		projId = (int)ProjIds.PeaceOutRollerSplit;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new PeaceOutRollerProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}
	public override void onHitWall(CollideData other) {
		destroySelf(disableRpc: true);
		if (!ownedByLocalPlayer) return;
		split();
	}
	public void split() {
		if (!ownedByLocalPlayer) return;
		if (!splitOnce) splitOnce = true;
		else return;
		playSound("ballPOR", sendRpc: true);
		new PeaceOutRollerSplitProj(
			pos, xDir, 0, this, owner,
			owner.getNextActorNetId(), rpc: true);
		new PeaceOutRollerSplitProj(
			pos, xDir, 1, this, owner,
			owner.getNextActorNetId(),  rpc: true);
	}

}
public class PeaceOutRollerSplitProj : Projectile {
	public int type = 0;
	public PeaceOutRollerSplitProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "ball_por_proj", netId, player
	) {
		weapon = PeaceOutRoller.netWeapon;
		damager.damage = 3f;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 30;
	    maxTime = 0.4f;
		projId = (int)ProjIds.PeaceOutRollerSplit;
		destroyOnHit = false;
		useGravity = false;
		this.type = type;
		xScale = 0.75f;
		yScale = 0.75f;
		if (type == 0) {
			setupWallCrawl(new Point(250, 250));
		} else if (type == 1) {
			setupWallCrawl(new Point(-250, -250));
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new PeaceOutRollerSplitProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		updateWallCrawl();
	}
}
#endregion
/*
public class VileBall : Weapon {
	public float vileAmmoUsage;
	public static VileBall netWeaponER = new VileBall(VileBallType.ExplosiveRound);
	public static VileBall netWeaponSS = new VileBall(VileBallType.SpreadShot);
	public static VileBall netWeaponPR = new VileBall(VileBallType.PeaceOutRoller);
	public VileBall(VileBallType vileBallType) : base() {
		fireRate = 60;
		index = (int)WeaponIds.VileBomb;
		weaponBarBaseIndex = 27;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 15;
		type = (int)vileBallType;
		if (vileBallType == VileBallType.None) {
			displayName = "None";
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0; 
		} 
		else if (vileBallType == VileBallType.NoneNapalm) {
			displayName = "None(NAPALM)";
			description = new string[] { "Do not equip a Ball.", "NAPALM will be used instead." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
		} else if (vileBallType == VileBallType.NoneFlamethrower) {
			displayName = "None(FLAMETHROWER)";
			description = new string[] { "Do not equip a Ball.", "FLAMETHROWER will be used instead." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
		} else if (vileBallType == VileBallType.ExplosiveRound) {
			displayName = "Explosive Round";
			fireRate = 60;
			vileAmmoUsage = 8;
			description = new string[] { "These bombs split into two", "upon contact with the ground." };
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "2";
			hitcooldown = "0.2";
			effect = "Splits on ground.";
		} else if (vileBallType == VileBallType.SpreadShot) {
			displayName = "Spread Shot";
			vileAmmoUsage = 5;
			fireRate = 60;
			description = new string[] { "Unleash a fan of energy shots", "that stun enemies in their tracks." };
			killFeedIndex = 55;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "1";
			effect = "Stuns Enemies. CD: 2";
		} else if (vileBallType == VileBallType.PeaceOutRoller) {
			displayName = "Peace Out Roller";
			vileAmmoUsage = 16;
			fireRate = 75;
			description = new string[] { "This electric bombs splits into two upon", "upon contact with the ground." };
			killFeedIndex = 80;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "3";
			hitcooldown = "0.5";
			flinch = "6";
			effect = "Splits on ground.";
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		return vileAmmoUsage;
	}
	public static bool isGrenadeType(Vile vile) {
		if (vile.grenadeWeapon.type == (int)VileBallType.PeaceOutRoller) return true;		
		if (vile.grenadeWeapon.type == (int)VileBallType.SpreadShot) return true;		
		return (vile.grenadeWeapon.type == (int)VileBallType.ExplosiveRound);
	}
	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (vile.energy.ammo < 8) return;
		if (vile.grenadeWeapon.type == (int)VileBallType.None) return;
		if (isGrenadeType(vile)) {
			vile.changeState(new AirBombAttack(), true);
		} else if (vile.grenadeWeapon.type == (int)VileBallType.NoneFlamethrower) {
			if (vile.flamethrowerWeapon.type != (int)VileFlamethrowerType.None)
				if (vile.tryUseVileAmmo(vileAmmoUsage)) {
					vile.changeState(new FlamethrowerState(), true);
				}
		} else if (vile.grenadeWeapon.type == (int)VileBallType.NoneNapalm) {
			if (vile.napalmWeapon.type > -1)
				if (vile.tryUseVileAmmo(vileAmmoUsage)) {
					//vile.changeState(new AirBombNapalm(), true);
				}
		}
	}
}


public class AirBombAttack : VileState {
	int bombNum;
	public AirBombAttack(
		string transitionSprite = ""
	) : base(
		"air_bomb_attack", "", "", transitionSprite
	) {
		useDashJumpSpeed = true;
		useGravity = false;
		exitOnLanding = true;
	}

	public override void update() {
		base.update();
		if (vile.grenadeWeapon.type == (int)VileBallType.ExplosiveRound) {
			if (bombNum > 0 && player.input.isPressed(Control.Special1, player)) {
				character.changeState(character.getFallState(), true);
				return;
			}
			if (stateTime > 0f && bombNum == 0) {
				ExplosiveRoundProj();
			} else if (stateTime > 0.23f && bombNum == 1) {
				ExplosiveRoundProj();
			} else if (stateTime > 0.45f && bombNum == 2) {
				ExplosiveRoundProj();
			}
			if (stateTime > 0.68f) {
				character.changeToIdleOrFall();
			}
		} else if (vile.grenadeWeapon.type == (int)VileBallType.SpreadShot) {
			if (bombNum > 0 && player.input.isPressed(Control.Special1, player)) {
				character.changeToIdleOrFall();
				return;
			}
			for (int i = 0; i < 7; i++) {
				if (stateTime > i * 0.1f && bombNum == i) {
					if (!vile.tryUseVileAmmo(vile.grenadeWeapon.getAmmoUsage(0))) {
						character.changeToIdleOrFall();
						return;
					}
					bombNum++;
					new StunShotProj2(
						character.pos, character.xDir, i + 1, 0, vile,
						character.player, character.player.getNextActorNetId(), rpc: true
					);
					vile.setVileShootTime(vile.grenadeWeapon);
				}
			}
			if (stateTime > 0.66f) {
				character.changeToIdleOrFall();
			}
		} else if (vile.grenadeWeapon.type == (int)VileBallType.PeaceOutRoller) {
			if (stateTime > 0f && bombNum == 0) {
				bombNum++;
				new PeaceOutRollerProj(
					character.pos, character.xDir, 0, vile, player,
					character.player.getNextActorNetId(), rpc: true
				);
				vile.setVileShootTime(vile.grenadeWeapon);
			}

			if (stateTime > 0.25f) {
				character.changeToIdleOrFall();
			}
		}
	}
	public void ExplosiveRoundProj() {
		var inputDir = player.input.getInputDir(player);
		if (inputDir.x == 0) inputDir.x = character.xDir;
		if (!vile.tryUseVileAmmo(4)) {
			character.changeState(character.getFallState(), true);
			return;
		}
		bombNum++;
		new VileBombProj(
			character.pos, (int)inputDir.x, 0, vile, player,
			character.player.getNextActorNetId(), rpc: true
		);
		vile.setVileShootTime(vile.grenadeWeapon);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
	}
}

/*
public class AirBombNapalm : NapalmAttackTypes {
	public AirBombNapalm() : base("air_bomb_attack") {
		sound = "FireNappalmMK2";
		soundFrame = 2;
		useDashJumpSpeed = true;
	}
	public override void update() {
		base.update();
		if (!shot && character.sprite.frameIndex == 2) {
			shot = true;
			vile.setVileShootTime(vile.napalmWeapon);
			if (vile.napalmWeapon.type == (int)NapalmType.RumblingBang) {
				new NapalmGrenadeProj(
					character.pos, character.xDir, vile, character.player,
					character.player.getNextActorNetId(), rpc: true
				);
			} else if (vile.napalmWeapon.type == (int)NapalmType.FireGrenade) {
				new MK2NapalmGrenadeProj(
					character.pos, character.xDir, vile, character.player,
					character.player.getNextActorNetId(), rpc: true
				);
			} else if (vile.napalmWeapon.type == (int)NapalmType.SplashHit) {
				new SplashHitGrenadeProj(
					character.pos, character.xDir, vile, character.player,
					character.player.getNextActorNetId(), rpc: true
				);
			}
		} 
		if (stateTime > 0.25f) {
			character.changeToIdleOrFall();
		}

	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = new Point();
	}
	public override bool canEnter(Character character) {
		return base.canEnter(character);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
*/