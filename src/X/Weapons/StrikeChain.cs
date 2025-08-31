using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class StrikeChain : Weapon {
	public static StrikeChain netWeapon = new();

	public StrikeChain() : base() {
		displayName = "Strike Chain";
		shootSounds = new string[] { "strikeChain", "strikeChain", "strikeChain", "strikeChainCharged" };
		fireRate = 45;
		index = (int)WeaponIds.StrikeChain;
		weaponBarBaseIndex = 14;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 14;
		killFeedIndex = 20 + (index - 9);
		weaknessIndex = (int)WeaponIds.SonicSlicer;
		switchCooldown = 20;
		damage = "2/4";
		effect = "Both:Hooks enemies and items.\nPull yourself towards walls.\nBe Spider-Man.";
		hitcooldown = "30";
		flinch = "Hooked Time";
		flinchCD = "0";
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		int upOrDown = player.input.getYDir(player);

		Projectile proj;
		if (chargeLevel >= 3) {
			proj = new StrikeChainProjCharged(pos, xDir, mmx, player, player.getNextActorNetId(), upOrDown, true);
		} else {
			proj = new StrikeChainProj(pos, xDir, mmx, player, player.getNextActorNetId(), upOrDown, true);
		}

		mmx.strikeChainProj = proj;
	}
}

public class StrikeChainPullToWall : CharState {
	Projectile scp;
	public bool isUp;
	public StrikeChainPullToWall(
		Projectile scp, string lastSprite, bool isUp
	) : base(
		string.IsNullOrEmpty(lastSprite) ? "shoot" : lastSprite, "fall_shoot", "", ""
	) {
		this.scp = scp;
		this.isUp = isUp;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		if (scp == null || scp.destroyed) {
			character.changeToLandingOrFall();
			return;
		}
	}

	public override bool canEnter(Character character) {
		if (character.isStatusImmune()) return false;
		return base.canEnter(character);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel.y = 0;
		character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
		if (scp != null && !scp.destroyed) scp.destroySelf();
	}
}

public class StrikeChainHooked : CharState {
	float stunTime;
	Projectile scp;
	Point lastScpPos;
	bool isDone;
	bool flinch;
	bool isWireSponge;
	public StrikeChainHooked(
		Projectile scp, bool flinch
	) : base(
		flinch ? "hurt" : "fall", flinch ? "" : "fall_shoot", flinch ? "" : "fall_attack"
	) {
		this.scp = scp;
		this.flinch = flinch;
		isGrabbedState = true;
		lastScpPos = scp.pos;
		isWireSponge = scp is WSpongeSideChainProj;
	}

	public override bool canEnter(Character character) {
		if (!character.ownedByLocalPlayer) return false;
		if (!base.canEnter(character)) return false;
		if (character.isStatusImmune()) return false;
		return !character.charState.invincible;
	}

	public override bool canExit(Character character, CharState newState) {
		if (newState is Die) return true;
		return isDone;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel.y = 0;
		if (player.character is Vile vile) {
			vile.rideArmorPlatform = null;
		}
		character.isStrikeChainState = true;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
		character.isStrikeChainState = false;
	}

	public override void update() {
		base.update();

		Character? scpChar = scp.damager?.owner?.character;
		if (scp is StrikeChainProj && scpChar != null && !isWireSponge) {
			if (scpChar.getShootXDir() == 1 && character.pos.x < scpChar.pos.x + 15) {
				stateTime = 5;
			} else if (scpChar.getShootXDir() == -1 && character.pos.x > scpChar.pos.x - 15) {
				stateTime = 5;
			}
		}

		if (scp.destroyed || stateTime >= 5) {
			character.useGravity = true;
			stunTime += Global.spf;
			if (!flinch || stunTime > 0.375f) {
				isDone = true;
				character.changeToLandingOrFall();
				return;
			}
		} else if (scpChar != null) {
			if (!isWireSponge) {
				Point newPos = character.pos.add(scp.deltaPos);
				if (newPos.distanceTo(scpChar.pos) < character.pos.distanceTo(scpChar.pos)) {
					character.move(scp.deltaPos, useDeltaTime: false);
				}
			} else {
				if (Math.Sign(scp.deltaPos.x) != scp.xDir) {
					character.move(scp.deltaPos, useDeltaTime: false);
				}
			}
		}
	}
}


public class StrikeChainProj : Projectile {

	int upOrDown;
	int startDir;
	MegamanX mmx;
	Player player;
	float dist;
	float distRetracted;
	float maxDist = 120;
	Point reverseVel;
	bool reversed; //Used for all cases (Hooking actors, pulling to walls or just returning to X)
	bool toWall;
	Point toWallVel;
	Actor? hookedActor;
	bool toActor;
	float hookWaitTime;

	public StrikeChainProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, int upOrDown = 0, bool rpc = false
	) : base(
		pos, xDir, owner, "strikechain_proj", netId, player	
	) {
		weapon = StrikeChain.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		vel = new Point(400 * xDir, 0);
		projId = (int)ProjIds.StrikeChain;
		destroyOnHit = false;
		maxTime = 4;
		startDir = xDir;
		//xScale = 1;

		//Set character and player
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		mmx.strikeChainProj = this;
		this.player = player;

		//Reduce range if carrying a flag.
		if (mmx.flag != null) maxDist /= 2;

		//Set angle and speed.
		this.upOrDown = upOrDown;
		byteAngle = upOrDown * 32;
		if (xDir < 0) byteAngle = -byteAngle + 128;
		//if (byteAngle < 0) byteAngle += 256;
		vel = Point.createFromByteAngle(byteAngle).times(400);

		canBeLocal = false;

		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new StrikeChainProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		//We destroy the chain if X faces to the opposite xDir
		if (mmx.xDir != startDir && mmx.charState is not WallSlide) {
			destroySelf();
			return;
		}

		// Hooked character? Wait for them to become hooked before pulling back. Wait a max of 200 ms
		var hookedChar = hookedActor as Character;
		if (hookedChar != null && !hookedChar.ownedByLocalPlayer && !hookedChar.isStrikeChainState) {
			hookWaitTime += Global.speedMul;
			if (hookWaitTime < 12) return;
		}

		//If pulled towards a wall
		if (toWall) {
			mmx.move(toWallVel);
			var collision = Global.level.checkTerrainCollisionOnce(
				mmx, toWallVel.x * Global.spf, toWallVel.y * Global.spf
			);
			if (collision?.gameObject is Wall) {
				destroySelf();
				float momentum = 0.25f * (distRetracted / maxDist);
				mmx.xSwingVel = toWallVel.x * (0.25f + momentum) * 0.5f / 60f;
				//Yes, X2 Boots increase it.
				if (mmx.isDashing && mmx.legArmor == ArmorId.Giga && mmx.flag == null) {
					mmx.xSwingVel *= 1.1f;
				}
				mmx.vel.y = toWallVel.y;
			}
		} 
		//Actor hooked
		//This only runs if hookedActor is a Pickup.
		//Characters uses StrikeChainHooked state.
		else if (toActor) {
			if (hookedActor != null && !(hookedActor is Character)) {
				if (!hookedActor.ownedByLocalPlayer) {
					hookedActor.takeOwnership();
					RPC.clearOwnership.sendRpc(hookedActor.netId);
				}
				hookedActor.useGravity = false;
				hookedActor.grounded = false;
				hookedActor.move(hookedActor.pos.directionTo(mmx.getCenterPos()).normalize().times(400));
			}
			if (distRetracted >= dist + 10) {
				if (hookedActor != null && !(hookedActor is Character)) {
					hookedActor.changePos(mmx.getCenterPos());
					hookedActor.useGravity = true;
				}
				destroySelf();
			}
		}
		else {
			//If the chain reaches max range and didn't catch anything, it turns back.
			if (!reversed) {
				dist += Math.Abs(Global.speedMul * vel.magnitude) / 60;
				if (dist >= maxDist) reverse(vel);
			}
		}

		// We check if the chain came back to X or viceversa
		if (reversed) {
			distRetracted += Math.Abs(Global.speedMul * reverseVel.magnitude) / 60;
			if (distRetracted >= dist) {
				destroySelf();
				return;
			} 
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		//Syncs with current buster position.
		Point shootPos = mmx.getShootPos();
		changePos(
			new Point(
				shootPos.x + (dist - distRetracted) * Helpers.cosb(byteAngle), 
				shootPos.y + (dist - distRetracted) * Helpers.sinb(byteAngle)
			)
		);
	}

	public void reverse(Point newVel) {
		vel.x *= -1;
		vel.y *= -1;
		reversed = true;
		reverseVel = newVel;
	}

	public void hookActor(Actor? actor) {
		toActor = true;
		hookedActor = actor;
		reverse(vel);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (hookedActor != null) hookedActor.useGravity = true;
		mmx.strikeChainProj = null;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (reversed) return;

		var wall = other.gameObject as Wall;

		if (wall != null && wall.collider.isClimbable && !toWall) {
			toWall = true;
			toWallVel = vel;
			if (mmx.flag != null) toWallVel.multiply(0.5f);
			stopMoving();
			reverse(toWallVel);
			if (mmx.grounded) mmx.incPos(new Point(0, -4));
			mmx.changeState(new StrikeChainPullToWall(this, mmx.charState.shootSpriteEx, toWallVel.y < 0), true);
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer || hookedActor != null || reversed) return;
	
		var actor = other.gameObject as Actor;

		if (actor != null) {
			var chr = actor as Character;
			var pickup = actor as Pickup;
			if (chr == null && pickup == null) {
				//Lo quitamo' o miedo- I mean
				//It doesn't hook neither Mavericks nor Ride Armors.
				if (actor is Maverick || actor is RideArmor) {
					hookActor(null);
				}
				return;
			}
			//Character specific code.
			if (chr != null) {
				if (!chr.canBeDamaged(player.alliance, player.id, projId)) return;
				hookActor(actor);

				if (Global.serverClient != null) {
					RPC.commandGrabPlayer.sendRpc(netId, chr.netId, CommandGrabScenario.StrikeChain, false);
				}
				chr.hook(this);
			}
			//If chr is null, then it hooks a pickup
			else if (pickup != null) hookActor(actor);
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);

		float length = dist - distRetracted;
		int pieceSize = 8;
		int maxI = MathInt.Floor(length / pieceSize);
		int chainFrame = Helpers.clamp((int)(5 * length / maxDist), 0, 5);
		float xOff = (length) * Helpers.cosb(byteAngle);
		float yOff = (length) * Helpers.cosb(byteAngle);

		DrawWrappers.DrawLine(
			pos.x, pos.y, mmx.getShootPos().x, mmx.getShootPos().y,
			new Color(206, 123, 239, 255), 4, ZIndex.Character - 2
		);

		for (int i = 0; i < maxI; i++) {
			xOff = (length - (pieceSize * i)) * Helpers.cosb(byteAngle);
			yOff = (length - (pieceSize * i)) * Helpers.sinb(byteAngle);
			
			Global.sprites["strikechain_chain"].draw(
				chainFrame, pos.x - xOff, pos.y - yOff, 
				xDir, yDir, getRenderEffectSet(), 1, 1, 1, 
				ZIndex.Character - 1, angle: byteAngle * 1.40625f
			);
		}
	}
}


public class StrikeChainProjCharged : Projectile {

	int upOrDown;
	int startDir;
	MegamanX mmx;
	Player player;
	float dist;
	float distRetracted;
	float maxDist = 180;
	Point reverseVel;
	bool reversed; //Used for all cases (Hooking actors, pulling to walls or just returning to X)
	bool toWall;
	Point toWallVel;
	Actor? hookedActor;
	bool toActor;
	float hookWaitTime;

	public StrikeChainProjCharged(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, int upOrDown = 0, bool rpc = false
	) : base(
		pos, xDir, owner, "strikechain_charged", netId, player	
	) {
		weapon = StrikeChain.netWeapon;
		damager.damage = 4;
		damager.hitCooldown = 30;
		vel = new Point(600 * xDir, 0);
		projId = (int)ProjIds.StrikeChain;
		destroyOnHit = false;
		maxTime = 4;
		startDir = xDir;
		//xScale = 1;

		//Set character and player
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		this.player = player;

		//Reduce range if carrying a flag.
		if (mmx.flag != null) maxDist /= 2;

		//Set angle and speed.
		this.upOrDown = upOrDown;
		byteAngle = upOrDown * 32;
		if (xDir < 0) byteAngle = -byteAngle + 128;
		//if (byteAngle < 0) byteAngle += 256;
		vel = Point.createFromByteAngle(byteAngle).times(600);

		canBeLocal = false;

		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new StrikeChainProjCharged(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		//We destroy the chain if X faces to the opposite xDir
		if (mmx.xDir != startDir && mmx.charState is not WallSlide) {
			destroySelf();
			return;
		}

		// Hooked character? Wait for them to become hooked before pulling back. Wait a max of 200 ms
		var hookedChar = hookedActor as Character;
		if (hookedChar != null && !hookedChar.ownedByLocalPlayer && !hookedChar.isStrikeChainState) {
			hookWaitTime += Global.speedMul;
			if (hookWaitTime < 12) return;
		}
		
		//If pulled towards a wall
		if (toWall) {
			mmx.move(toWallVel);
			var collision = Global.level.checkTerrainCollisionOnce(mmx, (toWallVel.x * Global.spf), (toWallVel.y * Global.spf));
			if (collision?.gameObject is Wall) {
				destroySelf();
				float momentum = 0.25f * (distRetracted / maxDist);
				mmx.xSwingVel = toWallVel.x * (0.25f + momentum) * 0.5f;
				if (mmx.isDashing && mmx.legArmor == ArmorId.Giga && mmx.flag == null) {
					mmx.xSwingVel *= 1.1f;
				}
				mmx.vel.y = toWallVel.y;
				//Yes, X2 Boots increase it.
			}	
		} 
		//Actor hooked
		//This only runs if hookedActor is a Pickup.
		//Characters uses StrikeChainHooked state.
		else if (toActor) {
			if (hookedActor != null && !(hookedActor is Character)) {
				if (!hookedActor.ownedByLocalPlayer) {
					hookedActor.takeOwnership();
					RPC.clearOwnership.sendRpc(hookedActor.netId);
				}
				hookedActor.useGravity = false;
				hookedActor.grounded = false;
				hookedActor.move(hookedActor.pos.directionTo(mmx.getCenterPos()).normalize().times(600));
			}
			if (distRetracted >= dist + 10) {
				if (hookedActor != null && !(hookedActor is Character)) {
					hookedActor.changePos(mmx.getCenterPos());
					hookedActor.useGravity = true;
				}
				destroySelf();
			}
		}
		else {
			//If the chain reaches max range and didn't catch anything, it turns back.
			if (!reversed) {
				dist += Math.Abs(Global.speedMul * vel.magnitude) / 60;
				if (dist >= maxDist) reverse(vel);
			}
		}

		// We check if the chain came back to X or viceversa
		if (reversed) {
			distRetracted += Math.Abs(Global.speedMul * reverseVel.magnitude) / 60;
			if (distRetracted >= dist) {
				destroySelf();
				return;
			} 
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		//Syncs with current buster position.
		Point shootPos = mmx.getShootPos();
		changePos(
			new Point(
				shootPos.x + (dist - distRetracted) * Helpers.cosb(byteAngle), 
					shootPos.y + (dist - distRetracted) * Helpers.sinb(byteAngle)
			)
		);
	}

	public void reverse(Point newVel) {
		vel.x *= -1;
		vel.y *= -1;
		reversed = true;
		reverseVel = newVel;
	}

	public void hookActor(Actor? actor) {
		toActor = true;
		hookedActor = actor;
		reverse(vel);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (hookedActor != null) hookedActor.useGravity = true;
		if (mmx != null && mmx.strikeChainProj == this) {
			mmx.strikeChainProj = null;
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (reversed) return;

		var wall = other.gameObject as Wall;

		if (wall != null && wall.collider.isClimbable && !toWall) {
			toWall = true;
			toWallVel = vel;
			if (mmx.flag != null) toWallVel.multiply(0.5f);
			stopMoving();
			reverse(toWallVel);
			if (mmx.grounded) mmx.incPos(new Point(0, -4));
			mmx.changeState(new StrikeChainPullToWall(this, mmx.charState.shootSpriteEx, toWallVel.y < 0), true);
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer || hookedActor != null || reversed) return;
	
		var actor = other.gameObject as Actor;

		if (actor != null) {
			var chr = actor as Character;
			var pickup = actor as Pickup;
			if (chr == null && pickup == null) {
				//Lo quitamo' o miedo- I mean
				//It doesn't hook neither Mavericks nor Ride Armors.
				if (actor is Maverick || actor is RideArmor) {
					hookActor(null);
				}
				return;
			}
			//Character specific code.
			if (chr != null) {
				if (!chr.canBeDamaged(player.alliance, player.id, projId)) return;
				hookActor(actor);

				if (Global.serverClient != null) {
					RPC.commandGrabPlayer.sendRpc(netId, chr.netId, CommandGrabScenario.StrikeChain, false);
				}
				chr.hook(this);
			}
			//If chr is null, then it hooks a pickup
			else if (pickup != null) hookActor(actor);
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);

		float length = dist - distRetracted;
		int pieceSize = 8;
		int maxI = MathInt.Floor(length / pieceSize);
		int chainFrame = Helpers.clamp((int)(5 * length / maxDist), 0, 5);
		float xOff = (length) * Helpers.cosb(byteAngle);
		float yOff = (length) * Helpers.cosb(byteAngle);

		DrawWrappers.DrawLine(
			pos.x, pos.y, mmx.getShootPos().x, mmx.getShootPos().y,
			new Color(206, 123, 239, 255), 4, ZIndex.Character - 2
		);

		for (int i = 0; i < maxI; i++) {
			xOff = (length - (pieceSize * i)) * Helpers.cosb(byteAngle);
			yOff = (length - (pieceSize * i)) * Helpers.sinb(byteAngle);
			
			Global.sprites["strikechain_charged_chain"].draw(
				chainFrame, pos.x - xOff, pos.y - yOff, 
				xDir, yDir, getRenderEffectSet(), 1, 1, 1, 
				ZIndex.Character - 1, angle: byteAngle * 1.40625f
			);
		}
	}
}
