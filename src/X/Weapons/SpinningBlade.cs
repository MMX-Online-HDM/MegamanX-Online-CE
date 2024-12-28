using System;
using System.Collections.Generic;
using SFML.Audio;
using SFML.Graphics;

namespace MMXOnline;

public class SpinningBlade : Weapon {
	public static SpinningBlade netWeapon = new();

	public SpinningBlade() : base() {
		shootSounds = new string[] { "", "", "", "spinningBladeCharged" };
		fireRate = 75;
		index = (int)WeaponIds.SpinningBlade;
		weaponBarBaseIndex = 20;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 20;
		killFeedIndex = 43;
		weaknessIndex = (int)WeaponIds.TriadThunder;
		damage = "2/2";
		effect = "Goes back after some time on screen.";
		hitcooldown = "0/0.5";
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
			player.setNextActorNetId(player.getNextActorNetId());
			new SpinningBladeProj(this, pos, xDir, 0, player, player.getNextActorNetId(true), true);
			new SpinningBladeProj(this, pos, xDir, 1, player, player.getNextActorNetId(true), true);
		} else if (character is MegamanX mmx) {
			var csb = new SpinningBladeProjCharged(this, pos, xDir, player, player.getNextActorNetId(), true);
			if (mmx.ownedByLocalPlayer) {
				mmx.chargedSpinningBlade = csb;
			}
		}
	}
}

public class SpinningBladeProj : Projectile {
	Sound? spinSound;
	bool once;

	public SpinningBladeProj(
		Weapon weapon, Point pos, int xDir, int type, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 250, 2, player, "spinningblade_proj", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 2f;
		projId = (int)ProjIds.SpinningBlade;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		/*try {
			spinSound = new Sound(Global.soundBuffers["spinningBlade"].soundBuffer);
			spinSound.Volume = 50f;
		} catch {
			// GM19:
			// Sometimes code above throws for some users with
			// "External component has thrown an exception." error,
			// could investigate more on why
			// Gacel Notes:
			// WTF GM19?
			// You know this is because you use it at object creation.
			// I'm moving this to on onStart().
		}*/
		vel.y = (type == 0 ? -37 : 37);
		if (type == 0) {
			yScale = -1;
		}
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpinningBladeProj(
			SpinningBlade.netWeapon, arg.pos, arg.xDir,
			arg.extraData[0], arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!once && time > 0.1f && spinSound != null) {
			spinSound.Play();
			once = true;
		}
		if (spinSound != null) {
			spinSound.Volume = getSoundVolume() * 0.5f;
			if (spinSound.Volume < 0.1) {
				spinSound.Stop();
				spinSound.Dispose();
				spinSound = null;
			}
		}
		if (MathF.Abs(vel.x) < 400) {
			vel.x -= Global.spf * 450 * xDir;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		spinSound?.Stop();
		spinSound?.Dispose();
		spinSound = null;
		//int randFlipX = Helpers.randomRange(0, 1) == 0 ? -1 : 1;
		float randFlipX = Helpers.randomRange(0.75f, 1.5f);
		new Anim(pos, "spinningblade_piece1", xDir, null, false) { useGravity = true, vel = new Point(-100 * xDir * randFlipX, Helpers.randomRange(-100, -50)), ttl = 2 };
		new Anim(pos, "spinningblade_piece2", xDir, null, false) { useGravity = true, vel = new Point(100 * xDir * randFlipX, Helpers.randomRange(-100, -50)), ttl = 2 };
	}

	public override void onStart() {
		base.onStart();
		spinSound = new Sound(Global.soundBuffers["spinningblade"].soundBuffer);
		spinSound.Volume = 50f;
	}
}

public class SpinningBladeProjCharged : Projectile {
	public MegamanX? mmx;
	public float xDist;
	const float maxXDist = 90;
	public float spinAngle;
	bool retracted;
	bool soundPlayed;
	public SpinningBladeProjCharged(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 250, 2, player, "spinningblade_charged", 
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.SpinningBladeCharged;
		shouldShieldBlock = false;
		destroyOnHit = false;
		mmx = (player.character as MegamanX);
		shouldVortexSuck = false;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpinningBladeProjCharged(
			SpinningBlade.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (time > 0.25f && !soundPlayed) {
			soundPlayed = true;
			playSound("spinningBlade");
		}

		if (!ownedByLocalPlayer) return;

		if (mmx == null || mmx.destroyed) {
			destroySelf();
			return;
		}

		if (time > 2) retracted = true;

		if (!retracted) {
			if (mmx.chargedSpinningBlade != this) {
				if (MathF.Abs(xDist - 50) < 4 * speedMul) {
					xDist = 50;
				} else if (xDist < 50) {
					xDist += 4 * speedMul;
				} else if (xDist > 50) {
					xDist -= 4 * speedMul;
				}
			} else {
				if (xDist < maxXDist) {
					xDist += Global.spf * 240;
				} else {
					xDist = maxXDist;
				}
			}
		} else {
			if (xDist > 0) {
				xDist -= Global.spf * 240;
			} else {
				xDist = 0;
				destroySelf();
			}
		}

		float xOff = Helpers.cosd(spinAngle) * xDist;
		float yOff = Helpers.sind(spinAngle) * xDist;
		changePos(mmx.getShootPos().addxy(xDir * xOff, yOff));

		if (mmx.charState.attackCtrl && !mmx.stockedBuster &&
			mmx.player.input.isPressed(Control.Shoot, mmx.player) && xDist >= maxXDist
		) {
			retracted = true;
		}

		if (mmx.chargedSpinningBlade != this) {
			spinAngle -= Global.spf * 360 * 1.5f;
		}
		else if (mmx.player.input.isHeld(Control.Up, mmx.player) ) {
			spinAngle -= Global.spf * 360;
		} else if (mmx.player.input.isHeld(Control.Down, mmx.player)) {
			spinAngle += Global.spf * 360;
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		Point sPos = mmx.getShootPos();
		DrawWrappers.DrawLine(sPos.x, sPos.y, pos.x, pos.y, new Color(0, 224, 0), 3, zIndex - 100);
		DrawWrappers.DrawLine(sPos.x, sPos.y, pos.x, pos.y, new Color(224, 224, 96), 1, zIndex - 100);
		Global.sprites["spinningblade_base"].draw(MathInt.Round(Global.frameCount * 0.25f) % 3, sPos.x, sPos.y, 1, 1, null, 1, 1, 1, zIndex);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
		if (mmx.chargedSpinningBlade == this) {
			mmx.chargedSpinningBlade = null;
		}
	}
}
