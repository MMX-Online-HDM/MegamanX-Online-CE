using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public partial class Actor : GameObject {
	public Sprite sprite; //Current sprite
	public bool useTerrainGrid { get; set; } = false;
	public bool useActorGrid { get; set; } = true;
	public bool iDestroyed => destroyed;

	public int frameIndex {
		get => sprite.frameIndex;
		set => sprite.frameIndex = value;
	}
	public float frameSpeed {
		get => sprite.frameSpeed;
		set => sprite.frameSpeed = value;
	}
	public float frameSeconds {
		get => sprite.frameSeconds;
		set => sprite.frameSeconds = value;
	}
	public float animSeconds {
		get => sprite.animSeconds;
		set => sprite.animSeconds = value;
	}
	public float frameTime {
		get => sprite.frameTime;
		set => sprite.frameTime = value;
	}
	public float animTime {
		get => sprite.animTime;
		set => sprite.animTime = value;
	}
	public int loopCount => sprite.loopCount;

	public void setFrameIndexSafe(int newFrameIndex) {
		if (sprite.animData.frames.InRange(newFrameIndex)) {
			sprite.frameIndex = newFrameIndex;
		}
	}

	public float speedMul { get; set; } = 1;
	public bool useFrameProjs;
	public Dictionary<string, List<Projectile>> spriteFrameToProjs = new Dictionary<string, List<Projectile>>();
	public List<Projectile> globalProjs = new List<Projectile>();
	public Dictionary<string, string> spriteFrameToSounds = new Dictionary<string, string>();

	public int xDir; //-1 or 1
	public int yDir;
	public Point pos; //Current location
	public Point prevPos;
	public Point deltaPos;
	public Point vel;
	public float xPushVel;
	public float xIceVel;
	public float xSwingVel;
	public float landingVelY;
	public bool immuneToKnockback;
	public bool isPlatform;
	public bool slideOnIce;
	public bool cachedUndewater;
	public Point cachedUndewaterPos = new Point(float.MinValue, float.MinValue);

	public float xFlinchPushVel = 0;

	public float yPushVel = 0;

	public Dictionary<int, SoundWrapper> netSounds = new Dictionary<int, SoundWrapper>();
	public string startSound = "";
	public bool isStatic;
	public bool startMethodCalled;
	// Angle stuff.
	// We a 256 for a full turn to make easier to send over netcode.
	public float? _byteAngle;
	public bool customAngleRendering;
	public bool useGravity;
	public bool gravityWellable { get { return this is Character || this is RideArmor || this is Maverick || this is RideChaser; } }
	public float gravityWellTime;
	public bool canBeGrounded = true;
	public bool grounded;
	public bool groundedIce;
	public string name { get; set; }
	public Dictionary<RenderEffectType, RenderEffect> renderEffects = new Dictionary<RenderEffectType, RenderEffect>();
	public long zIndex;
	public bool visible = true;
	public bool timeSlow;
	public bool destroyed;
	public long destroyedOnFrame;
	public ShaderWrapper? genericShader;
	public virtual List<ShaderWrapper>? getShaders() { return genericShader != null ? new List<ShaderWrapper> { genericShader } : null; }
	public float alpha = 1;
	public float xScale = 1;
	public float yScale = 1;
	public float gravityModifier = 1;
	public bool reversedGravity;
	public float gravityWellModifier = 1;
	public Dictionary<string, float> projectileCooldown { get; set; } = new Dictionary<string, float>();
	public Dictionary<string, float> flinchCooldown { get; set; } = new Dictionary<string, float>();
	public Dictionary<int, float> globalFlinchCooldown { get; set; } = new Dictionary<int, float>();

	public MusicWrapper? musicSource;
	public bool checkLadderDown = false;
	public List<DamageText> damageTexts = new List<DamageText>();
	//public ShaderWrapper invisibleShader;
	public List<DamageEvent> damageHistory = new List<DamageEvent>();
	public NetcodeModel? netcodeOverride;

	public bool ownedByLocalPlayer;
	public bool locallyControlled {
		get { return (canBeLocal || ownedByLocalPlayer); }
	}
	public bool canBeLocal;
	public bool forceNetUpdateNextFrame;

	public ushort? netId;

	public float? netXPos;
	public float? netYPos;
	public Point netIncPos;
	public int? netSpriteIndex;
	public int? netFrameIndex;
	public int? netXDir;
	public int? netYDir;
	public float? netAngle;
	public bool stopSyncingNetPos;
	public bool syncScale;
	public Point? targetNetPos;
	public bool interplorateNetPos = true;

	private Point? lastPos;
	private int? lastSpriteIndex;
	private int? lastFrameIndex;
	private int? lastXDir;
	private int? lastYDir;
	private float? lastAngle;
	private bool? lastVisible;
	public float lastNetUpdate;
	public int lastNetFrame;

	public NetActorCreateId netActorCreateId;
	public Player? netOwner;
	float createRpcTime;

	public bool splashable;
	private Anim _waterWade = null!;
	public Anim waterWade {
		get {
			if (_waterWade == null) {
				_waterWade = new Anim(pos, "wade", 1, null, false);
			}
			return _waterWade;
		}
	}

	public float lastWaterY;

	public float underwaterTime;
	public float bubbleTime;
	public float bigBubbleTime;
	public float waterTime;

	public const float timeStopThreshold = 15;
	public float timeStopTime;
	public bool highPiority;
	public bool lowPiority;
	public bool movedUpOnFrame;

	public Actor(
		string spriteName, Point pos, ushort? netId, bool ownedByLocalPlayer, bool addToLevel
	) {
		// Intialize sprites as soon as posible to prevent crashes.
		if (spriteName is not null and not "") {
			initalizeSprite(spriteName);
			// Crash if spriteName was provided but does not exist.
			if (sprite == null) {
				string typeName = GetType().ToString().Replace("MMXOnline.", "");
				throw new Exception(
					"Null sprite at object" + typeName +
					"with spritename variable \"" + spriteName + "\""
				);
			}
		} else {
			// Default to empty if no sprite was provided.
			sprite = new Sprite("empty");
			sprite.name = "null";
		}
		// Initalize other stuff.
		this.pos = pos;
		prevPos = pos;
		/*
		if (Global.debug && Global.serverClient != null && netId != null
			&& Global.level.getActorByNetId(netId.Value) != null
		) {
			string netIdDump = Global.level.getNetIdDump();
			Helpers.WriteToFile("netIdDump.txt", netIdDump);
			throw new Exception(
				"The netId " + netId.ToString() + " (sprite " + spriteName + " ) was already used."
			);
		}
		*/
		this.netId = netId;
		this.ownedByLocalPlayer = ownedByLocalPlayer;
		vel = new Point(0, 0);
		useGravity = true;
		frameIndex = 0;
		frameSpeed = 1;
		frameTime = 0;
		name = "";
		xDir = 1;
		yDir = 1;
		grounded = false;
		zIndex = ++Global.level.autoIncActorZIndex;
		lastNetUpdate = Global.time;

		if (netId is not null) {
			Global.level.actorsById[netId.Value] = this;
		}

		if (!addToLevel) {
			Global.level.addGameObject(this);
		}

		if (isWading() || isUnderwater()) {
			waterTime = 10;
			underwaterTime = 10;
		}
	}

	public bool isUnderwater() {
		if (cachedUndewaterPos == pos) {
			return cachedUndewater;
		}
		cachedUndewaterPos = pos;

		float colliderHeight;
		if (globalCollider == null) colliderHeight = 10;
		else colliderHeight = globalCollider.shape.maxY - globalCollider.shape.minY;

		// May need a new overridable method to get "visual" height for situations like these
		if (sprite?.name?.StartsWith("sigma2_viral_") == true) {
			colliderHeight = 50;
		}

		foreach (Rect waterRect in Global.level.waterRects) {
			if (pos.x > waterRect.x1 && pos.x < waterRect.x2 &&
				pos.y - colliderHeight > waterRect.y1 && pos.y < waterRect.y2
			) {
				lastWaterY = waterRect.y1;
				cachedUndewater = true;
				return true;
			}
		}
		cachedUndewater = false;
		return false;
		//if (Global.level.levelData.waterY == null) return false;
		//if (Global.level.levelData.name == "forest2" && pos.x > 1415 && pos.x < 1888 && pos.y < 527) return false;
	}

	public bool isWading() {
		foreach (var waterRect in Global.level.waterRects) {
			if (pos.x > waterRect.x1 && pos.x < waterRect.x2 && pos.y > waterRect.y1 && pos.y < waterRect.y2) {
				lastWaterY = waterRect.y1;
				return true;
			}
		}
		return false;
	}

	public void createActorRpc(int playerId) {
		if (netId == null) return;
		if (!ownedByLocalPlayer) return;

		byte[] xBytes = BitConverter.GetBytes(pos.x);
		byte[] yBytes = BitConverter.GetBytes(pos.y);
		byte[] netProjIdByte = BitConverter.GetBytes(netId.Value);

		var bytes = new List<byte>()
			{
					(byte)netActorCreateId,
					xBytes[0], xBytes[1], xBytes[2], xBytes[3],
					yBytes[0], yBytes[1], yBytes[2], yBytes[3],
					(byte)playerId,
					netProjIdByte[0], netProjIdByte[1],
					(byte)(xDir + 128)
				};

		Global.serverClient?.rpc(RPC.createActor, bytes.ToArray());
	}

	public void changeSpriteIfDifferent(string spriteName, bool resetFrame) {
		if (sprite.name == spriteName) return;
		changeSprite(spriteName, resetFrame);
	}

	public virtual void changeSprite(string spriteName, bool resetFrame) {
		string oldSpriteName = sprite.name;
		if (spriteName == null) {
			return;
		}
		if (sprite.name == spriteName) {
			if (this is not Character && resetFrame) {
				sprite.restart();
			}
			return;
		}

		if (!Global.sprites.ContainsKey(spriteName)) return;
		Global.level.removeFromGrid(this);

		int oldFrameIndex = sprite?.frameIndex ?? 0;
		float oldFrameTime = sprite?.frameSeconds ?? 0;
		float oldAnimTime = sprite?.animSeconds ?? 0;

		sprite = new Sprite(spriteName);

		changeGlobalColliderOnSpriteChange(spriteName);

		foreach (var hitbox in sprite.hitboxes) {
			hitbox.actor = this;
		}
		foreach (var frame in sprite.frameHitboxes) {
			foreach (var hitbox in frame) {
				hitbox.actor = this;
			}
		}

		if (resetFrame) {
			frameIndex = 0;
			frameTime = 0;
			animTime = 0;
		} else {
			frameIndex = oldFrameIndex;
			frameSeconds = oldFrameTime;
			animSeconds = oldAnimTime;
		}

		if (frameIndex >= sprite.totalFrameNum) {
			frameIndex = 0;
			frameTime = 0;
			animTime = 0;
		}

		Global.level.addToGrid(this);

		if ((this is Character || this is Maverick) && spriteName != oldSpriteName) {
			if (spriteName.EndsWith("_warp_in") && !Global.level.mainPlayer.readyTextOver) {
				Global.level.delayedActions.Add(new DelayedAction(() => {
					playOverrideVoice(spriteName);
				}, Player.maxReadyTime));
			} else if ((spriteName != "sigma_die" && spriteName != "sigma2_die" && spriteName != "sigma3_die") || visible) {
				playOverrideVoice(spriteName);
			}
		}
	}

	private void initalizeSprite(string spriteName) {
		if (!Global.sprites.ContainsKey(spriteName)) return;
		sprite = new Sprite(spriteName);
		changeGlobalColliderOnSpriteChange(spriteName);

		foreach (var hitbox in sprite.hitboxes) {
			hitbox.actor = this;
		}
		foreach (var frame in sprite.frameHitboxes) {
			foreach (var hitbox in frame) {
				hitbox.actor = this;
			}
		}
		Global.level.addToGrid(this);
	}

	public void playOverrideVoice(string spriteName) {
		Character? chr = this as Character;
		int charNum = chr != null ? chr.player.charNum : 4;
		spriteName = spriteName.Replace("_na_", "_")
							   .Replace("_bald_", "_")
							   .Replace("_notail_", "_")
							   .Replace("tongue2", "tongue")
							   .Replace("tongue3", "tongue")
							   .Replace("tongue4", "tongue")
							   .Replace("tongue5", "tongue")
							   .Replace("_bk", "")
							   .Replace("_mc", "")
							   .Replace("_rb", "")
							   .Replace("_ag", "")
							   .Replace("_mb", "");

		var matchingVoice = Helpers.getRandomMatchingVoice(Global.voiceBuffers, spriteName, charNum);

		// If vile mk2 and mk5 sounds were not found, use the vile ones
		if (matchingVoice == null && (spriteName.StartsWith("vilemk2_") || spriteName.StartsWith("vilemk5_"))) {
			spriteName = spriteName.Replace("vilemk2_", "vile_").Replace("vilemk5_", "vile_");
			matchingVoice = Helpers.getRandomMatchingVoice(Global.voiceBuffers, spriteName, charNum);
		}

		if (matchingVoice != null) {
			playSound(matchingVoice);
		}
	}

	
	public float? angle {
		get {
			return _byteAngle * 1.40625f;
		}
		set {
			if (value == null) {
				return;
			}
			angleSet = true;
			_byteAngle = (value / 1.40625f) % 256;
			if (_byteAngle < 0) {
				_byteAngle += 256;
			}
		}
	}

	public bool angleSet;
	public float byteAngle {
		get {
			return _byteAngle ?? 0;
		}
		set {
			angleSet = true;
			_byteAngle = value % 256;
			if (_byteAngle < 0) {
				_byteAngle += 256;
			}
		}
	}


	public void setzIndex(long val) {
		this.zIndex = val;
	}

	public Frame currentFrame => sprite.getCurrentFrame();

	public float framePercent {
		get {
			float entireDuration = 0;
			foreach (var frame in sprite.animData.frames) {
				entireDuration += frame.duration;
			}
			return animSeconds / entireDuration;
		}
	}

	public virtual void onStart() {
		if (!string.IsNullOrEmpty(startSound)) {
			playSound(startSound);
		}
	}

	public virtual void preUpdate() {
		collidedInFrame.Clear();
		deltaPos = pos.subtract(prevPos);
		prevPos = pos;

		if (locallyControlled && sprite.name != "null") {
			int oldFrameIndex = sprite.frameIndex;
			sprite.update();

			if (sprite.frameIndex != oldFrameIndex) {
				string spriteFrameKey = sprite.name + "/" + sprite.frameIndex.ToString(CultureInfo.InvariantCulture);
				if (spriteFrameToSounds.ContainsKey(spriteFrameKey)) {
					playSound(spriteFrameToSounds[spriteFrameKey], sendRpc: true);
				}
			}
		}

		updateHitboxes();
	}

	public void updateHitboxes() {
		if (!useFrameProjs) {
			return;
		}
		// Frame-based hitbox projectile section
		string spriteKey = sprite.name + "_" + sprite.frameIndex.ToString();
		// Frame hitboxes.
		List<Collider> hitboxes = sprite.frameHitboxes[frameIndex].ToList();
		// Global hitboxes.
		hitboxes.AddRange(sprite.hitboxes);

		// Delete old stuff.
		foreach (string key in spriteFrameToProjs.Keys) {
			if (spriteFrameToProjs[key] != null) {
				foreach (Projectile proj in spriteFrameToProjs[key]) {
					proj.destroySelf();
				}
			}
			spriteFrameToProjs.Remove(key);
		}
		// Move if same frame.
		if (spriteFrameToProjs.GetValueOrDefault(spriteKey) != null) {
			foreach (var proj in spriteFrameToProjs[spriteKey]) {
				proj.incPos(deltaPos);
				updateProjFromHitbox(proj);
			}
		}
		// Get new if new frame.
		else if (hitboxes != null) {
			foreach (var hitbox in hitboxes) {
				Projectile? proj = getProjFromHitboxBase(hitbox);
				if (proj != null) {
					if (spriteFrameToProjs.GetValueOrDefault(spriteKey) == null) {
						spriteFrameToProjs[spriteKey] = new List<Projectile>();
					}
					spriteFrameToProjs[spriteKey].Add(proj);
				}
			}
		}

		// ----------------------------------------
		// Global hitbox projectile section
		foreach (var proj in globalProjs) {
			proj.incPos(deltaPos);
			updateProjFromHitbox(proj);
		}

		// Get misc. projectiles based on conditions (i.e. headbutt, awakened zero aura)
		Dictionary<int, Func<Projectile>> projToCreateDict = getGlobalProjs();

		// If the projectile id wasn't returned, remove it from current globalProj list.
		for (int i = globalProjs.Count - 1; i >= 0; i--) {
			if (!projToCreateDict.ContainsKey(globalProjs[i].projId)) {
				//globalProjs[i].destroyFrames = 2;
				globalProjs[i].destroySelf();
				globalProjs.RemoveAt(i);
			}
			else if (globalProjs[i].destroyed) {
				globalProjs.RemoveAt(i);
			}
		}

		// For all projectiles to create, add to the global proj list ONLY if the proj id doesn't already exist
		foreach (var kvp in projToCreateDict) {
			int projIdToCreate = kvp.Key;
			Func<Projectile> projFunction = kvp.Value;
			if (!globalProjs.Any(p => p.projId == projIdToCreate)) {
				Projectile newlyCreatedProj = projFunction();
				globalProjs.Add(newlyCreatedProj);
			}
		}
	}

	public void addGravity(ref float yVar) {
		float maxVelY = Physics.MaxFallSpeed;
		float gravity = getGravity();

		if (isUnderwater()) {
			maxVelY = Physics.MaxUnderwaterFallSpeed;
			gravity *= 0.5f;
		}

		yVar += Global.speedMul * gravity;
		if (yVar > maxVelY) yVar = maxVelY;
	}

	public virtual void update() {
		if (immuneToKnockback) {
			stopMovingS();
		}

		foreach (var key in netSounds.Keys.ToList()) {
			if (!Global.sounds.Contains(netSounds[key])) {
				netSounds.Remove(key);
			}
		}

		if (!startMethodCalled) {
			onStart();
			startMethodCalled = true;
		}

		if (ownedByLocalPlayer && netOwner != null) {
			createRpcTime += Global.spf;
			if (createRpcTime > 1) {
				createRpcTime = 0;
				createActorRpc(netOwner.id);
			}
		}

		var renderEffectsToRemove = new HashSet<RenderEffectType>();
		foreach (var kvp in renderEffects) {
			kvp.Value.time -= Global.speedMul;
			if (kvp.Value.time <= 0) {
				renderEffectsToRemove.Add(kvp.Key);
			}
		}
		foreach (var renderEffect in renderEffectsToRemove) {
			renderEffects.Remove(renderEffect);
		}

		if (!locallyControlled) {
			frameSpeed = 0;
			timeStopTime = 0;
			sprite.time += Global.spf;
		}
		if (timeStopTime > 0) {
			timeStopTime--;
			if (timeStopTime <= 0) {
				timeStopTime = 0;
			}
		};

		bool wading = isWading();
		bool underwater = isUnderwater();

		var chr = this as Character;
		var ra = this as RideArmor;

		if (locallyControlled) {
			localUpdate(underwater);
		}

		if (this is RideChaser && isWading()) {
			grounded = true;
			if (vel.y > 0) vel.y = 0;
		}

		bool isRaSpawning = (ra != null && ra.isSpawning());
		bool isChrSpawning = (chr != null && chr.isSpawning());
		if (splashable && !isChrSpawning && !isRaSpawning) {
			if (wading || underwater) {
				if (waterTime == 0) {
					new Anim(new Point(pos.x, lastWaterY), "splash", 1, null, true);
					playSound("splash");
					vel.y = 0;
				}
				waterTime += Global.spf;
			} else {
				if (waterTime > 0) {
					new Anim(new Point(pos.x, lastWaterY), "splash", 1, null, true);
					playSound("splash");
				}
				waterTime = 0;
			}

			if (wading && !underwater) {
				waterWade.visible = true;
				if (waterWade.pos.x != pos.x) {
					waterWade.changeSprite("wade_move", false);
				} else {
					waterWade.changeSprite("wade", false);
				}
				waterWade.pos = new Point(pos.x, lastWaterY);
			} else {
				waterWade.visible = false;
			}

			if (underwater) {
				underwaterTime += Global.spf;
				if (chr != null) {
					bubbleTime += Global.spf;
					if (bubbleTime > 1) {
						bubbleTime = 0;
						new BubbleAnim(chr.getHeadPos() ?? chr.getCenterPos(), "bubbles");
					}
				}
				if (underwaterTime < 0.5f) {
					bigBubbleTime -= Global.spf;
					if (bigBubbleTime <= 0) {
						bigBubbleTime = 0.08f;
						var points = globalCollider?.shape.points;
						if (points != null && points.Count >= 1) {
							new BubbleAnim(
								new Point(pos.x, points[0].y), "bigbubble" + ((Global.floorFrameCount % 3) + 1)
							);
						}
					}
				}
			} else {
				underwaterTime = 0;
			}
		}

		if (gravityWellable) {
			Helpers.decrementTime(ref gravityWellTime);
			if (gravityWellTime <= 0) {
				gravityWellModifier = 1;
			}
		}

		for (int i = 0; i < damageHistory.Count - 1; i++) {
			if (Global.time - damageHistory[i].time > 15f && (damageHistory.Count > 1 || Global.level.isTraining())) {
				damageHistory.RemoveAt(i);
				i--;
			}
		}
	}

	// This code is horrible awfull confusing ugly and slow.
	// Also why the game lags so much. We need to optimize it ASAP.
	public void localUpdate(bool underwater) {
		Character? chr = this as Character;
		float grav = getGravity();
		float terminalVelDown = Physics.MaxFallSpeed;
		if (underwater) {
			terminalVelDown = Physics.MaxUnderwaterFallSpeed;
		}
		if (useGravity && !grounded) {
			// Water slowing down the fall.
			if (underwater) {
				grav *= 0.5f;
			}
			// Apply gravity only if bellow terminal vel.
			// This allows some attacks to go beyond it.
			if (grav > 0 && vel.y < terminalVelDown) {
				vel.y += grav * Global.speedMul;
				if (vel.y > terminalVelDown) {
					vel.y = terminalVelDown;
				}
			}
			// Reverse gravity stuff.
			else if (grav < 0 && vel.y > -terminalVelDown) {
				vel.y += grav * Global.speedMul;
				if (vel.y < -terminalVelDown) {
					vel.y = -terminalVelDown;
				}
			}
			// Celling bump mechanic.
			int gravDir = MathF.Sign(grav);
			if (gravDir != 0 && vel.y * gravDir < 0 &&
				Global.level.checkTerrainCollisionOnce(
					this, 0, -gravDir, checkPlatforms: false
				) != null
			) {
				vel.y = 0;
			}
		}

		if (Math.Abs(xPushVel) > 0.01f) {
			xPushVel = Helpers.lerp(xPushVel, 0, Global.spf * 5);

			var wall = Global.level.checkTerrainCollisionOnce(this, xPushVel * speedMul, 0);
			if (wall != null && wall.gameObject is Wall) {
				xPushVel = 0;
			}
		} else if (xPushVel != 0) {
			xPushVel = 0;
		}

		// Heavy Flinch Push
		if (Math.Abs(xFlinchPushVel) > 0.01f) {
			xFlinchPushVel = Helpers.lerp(xFlinchPushVel, 0f, Global.spf * 5);
		} else if (xFlinchPushVel != 0f) {
			xFlinchPushVel = 0f;
		}

		if (Math.Abs(yPushVel) > 0.01f) {
			yPushVel = Helpers.lerp(yPushVel, 0f, Global.spf * 5);
		}
		else if (yPushVel != 0f) {
			yPushVel = 0f;
		}

		if (Math.Abs(xSwingVel) > 0.01f) {
			if (grounded ||
				Global.level.checkTerrainCollisionOnce(this, xSwingVel * speedMul, 0)?.gameObject is Wall
			) {
				xSwingVel = 0;
			}
		} else if (xSwingVel != 0f) {
			xSwingVel = 0f;
		}

		if (!grounded) {
			xIceVel = 0;
		}
		if (xIceVel != 0f) {
			xIceVel = Helpers.lerp(xIceVel, 0f, Global.spf);
			if (MathF.Abs(xIceVel) < 1 / 32f) {
				xIceVel = 0f;
			} else {
				// Gacel's notes:
				// There must be a better way to do this, really.
				Point oldPos = pos;
				Point oldDeltaPos = deltaPos;
				moveXY(xIceVel, 0, useIce: false);
				if (oldPos.x == pos.x && oldPos.y == pos.y) {
					xIceVel = 0f;
				}
				pos = oldPos;
				deltaPos = oldDeltaPos;
			}
		}

		if (this is RideChaser && isWading()) {
			grounded = true;
			changePos(new Point(pos.x, lastWaterY + 1));
			if (vel.y > 0) vel.y = 0;
		}

		if (!isStatic) {
			float xExtraSpeed = xFlinchPushVel + xIceVel + xPushVel + xSwingVel;
			movePoint((vel / 60f).addxy(xExtraSpeed, 0), true, true, false);
			if (yPushVel != 0) {
				moveXY(0, yPushVel, true, false, false);
			}
		}

		float yMod = reversedGravity ? -1 : 1;
		if (chr?.charState is VileMK2Grabbed) {
			grounded = false;
		} else if (physicsCollider != null && !isStatic && (canBeGrounded || useGravity)) {
			float yDist = 1 * Global.gameSpeed;
			if (grounded && vel.y * yMod >= 0 && !movedUpOnFrame) {
				yDist = 4 * Global.gameSpeed;
			}
			yDist *= yMod;

			CollideData? collideData = Global.level.checkTerrainCollisionOnce(this, 0, yDist, checkPlatforms: true);

			var hitActor = collideData?.gameObject as Actor;
			bool isPlatform = false;
			bool tooLowOnPlatform = false;
			if (hitActor?.isPlatform == true) {
				bool dropThruWolfPaw = (
					hitActor is WolfSigmaHand &&
					chr != null && !chr.grounded &&
					chr.player.input.isHeld(Control.Down, chr.player)
				);
				if (!dropThruWolfPaw) {
					isPlatform = true;
					if (pos.y > hitActor.getTopY() + 10) {
						tooLowOnPlatform = true;
						isPlatform = false;
					}
				} else {
					collideData = null;
				}
			}

			if (this is Flag && hitActor is WolfSigmaHand) {
				isPlatform = false;
			}

			if (tooLowOnPlatform) {
				tooLowOnPlatform = false;
				collideData = Global.level.checkTerrainCollisionOnce(this, 0, yDist);
			}

			if (collideData != null && vel.y * yMod >= 0) {
				grounded = true;
				landingVelY = vel.y;
				vel.y = 0;

				var hitWall = collideData.gameObject as Wall;
				if (hitWall?.isMoving == true) {
					move(hitWall.deltaMove, useDeltaTime: false);
				} else if (hitWall != null && hitWall.moveX != 0) {
					if (this is RideChaser rc) {
						rc.addXMomentum(hitWall.moveX);
					} else {
						move(new Point(hitWall.moveX, 0));
					}
				}
				if (isPlatform && hitActor != null) {
					move(hitActor.deltaPos, useDeltaTime: false);
				}

				groundedIce = false;
				if (hitWall != null && hitWall.slippery) {
					groundedIce = true;
				}

				//If already grounded, snap to ground further
				CollideData? collideDataCloseCheck = Global.level.checkTerrainCollisionOnce(this, 0, 0.05f * yMod);
				if (collideDataCloseCheck == null) {
					var yVel = new Point(0, yDist);
					var mtv = Global.level.getMtvDir(
						this, 0, yDist, yVel, false, new List<CollideData>() { collideData }
					);
					if (mtv != null) {
						incPos(yVel);
						incPos(mtv.Value.unitInc(0.01f));
					}
				}
			} else {
				grounded = false;
				groundedIce = false;
			}
		}
		movedUpOnFrame = false;
	}

	public float getTopY() {
		var collider = this.standartCollider;

		float cx = sprite.animData.baseAlignmentX;
		float cy = sprite.animData.baseAlignmentY;
		
		cx = cx * currentFrame.rect.w();
		cy = cy * currentFrame.rect.h();

		if (collider == null) {
			return pos.y;
		}
		return pos.y - (collider.shape.getRect().h() * cy);
	}

	public float getYMod() {
		if (reversedGravity) return -1;
		return 1;
	}

	public void reverseGravity() {
		vel = new Point(0, 0);
		gravityModifier *= -1;
		reversedGravity = !reversedGravity;
		yScale *= -1;
	}

	// The code here needs to work for non-owners too. So all variables in it needs to be synced.
	public virtual bool shouldDraw() {
		return visible;
	}

	public void getKillerAndAssister(
		Player? ownPlayer, ref Player? killer, ref Player? assister, ref int? weaponIndex,
		ref int? assisterProjId, ref int? assisterWeaponId
	) {
		if (damageHistory.Count > 0) {
			for (int i = damageHistory.Count - 1; i >= 0; i--) {
				var lastAttacker = damageHistory[i];
				if (lastAttacker.envKillOnly && weaponIndex != null) continue;
				if (Global.time - lastAttacker.time > 10) continue;
				killer = lastAttacker.attacker;
				weaponIndex = lastAttacker.weapon;
				break;
			}
		}
		if (damageHistory.Count > 0) {
			for (int i = damageHistory.Count - 1; i >= 0; i--) {
				var secondLastAttacker = damageHistory[i];
				if (secondLastAttacker.attacker == killer) continue;

				// Non-suicide case: prevent assists aggressively
				if (killer != ownPlayer && (
						secondLastAttacker.envKillOnly && weaponIndex != null ||
						Global.time - secondLastAttacker.time > (Global.level?.server?.customMatchSettings?.assistTime ?? 2) ||	
						Damager.unassistable(secondLastAttacker.projId)
					)
				) {
					continue;
				}
				// Suicide case: grant assists liberally to "punish" suicider more
				else if (Global.time - secondLastAttacker.time > 10) {
					continue;
				}

				assister = secondLastAttacker.attacker;
				assisterProjId = secondLastAttacker.projId;
				assisterWeaponId = secondLastAttacker.weapon;
			}
		}
	}

	/// <summary>
	///  Indicates whether this player's attacks should be defender-favored to everyone else
	/// </summary>
	public bool isDefenderFavored() {
		if (Global.isOffline) {
			return false;
		}
		if (netcodeOverride != null) {
			if (netcodeOverride == NetcodeModel.FavorDefender) {
				return true;
			} else {
				return false;
			}
		}
		if (this is Character chr) {
			return chr.player?.isDefenderFavored == true;
		}
		if (netOwner != null) {
			return netOwner.isDefenderFavored;
		}
		if (this is Projectile proj) {
			return proj.owner?.isDefenderFavored == true;
		}
		return false;
	}

	public bool isDefenderFavoredAndOwner() {
		return isDefenderFavored() && ownedByLocalPlayer;
	}

	// This can be used if a certain effect should be done by only the attacker or defender (if defender favor is on)
	public bool isRunByLocalPlayer() {
		if (Global.isOffline) return true;
		if (!isDefenderFavored()) {
			if (!ownedByLocalPlayer) return false;
		} else {
			if (ownedByLocalPlayer) return false;
		}
		return true;
	}

	public virtual void postUpdate() {

	}

	public bool destroyPosSet;
	public float destroyPosTime;
	public float maxDestroyTime;
	public void moveToPosThenDestroy(Point destroyPos, float speed) {
		destroyPosSet = true;
		vel = pos.directionToNorm(destroyPos).times(speed);
		float distToDestroyPos = pos.distanceTo(destroyPos);
		maxDestroyTime = Math.Max(distToDestroyPos / speed, 0.1f);
		if (maxDestroyTime <= 0) maxDestroyTime = 1;
	}

	public void netUpdate() {
			if (netId == null) return;
		if (destroyPosSet) {
			destroyPosTime += Global.spf;
			incPos(vel.times(Global.spf));
			if (destroyPosTime > maxDestroyTime) {
				destroySelf();
			}
			return;
		}
		if (canBeLocal && !forceNetUpdateNextFrame) {
			return;
		}
		forceNetUpdateNextFrame = false;
		if (ownedByLocalPlayer) {
			if (!Global.level.isSendMessageFrame()) return;
			sendActorNetData();
		} else {
			// 5 seconds since last net update: destroy the object
			if (Global.time - lastNetUpdate > 5 && cleanUpOnNoResponse()) {
				destroySelf(disableRpc: true);
				return;
			}

			float frameSmooth = Global.floorFrameCount - lastNetFrame + 1;
			if (frameSmooth < 1) { frameSmooth = 1; }

			if (frameSmooth > 1 && interplorateNetPos) {
				if (targetNetPos != null) {
					changePos(targetNetPos.Value);
					targetNetPos = null;
				}
				if (!stopSyncingNetPos && (netXPos != null || netYPos != null)) {
					var netPos = pos;
					if (netXPos != null && !stopSyncingNetPos) {
						netPos.x = (float)netXPos;
					}
					if (netYPos != null && !stopSyncingNetPos) {
						netPos.y = (float)netYPos;
					}

					var incPos = netPos.subtract(pos).times(1f / frameSmooth);
					var framePos = pos.add(incPos);

					netXPos = null;
					netYPos = null;
					if (pos.distanceTo(framePos) > 0.001f) {
						changePos(framePos);
						netXPos = null;
						netYPos = null;
						targetNetPos = netPos;
					} else {
						changePos(netPos);
						targetNetPos = null;
					}
				}
			} else if (!stopSyncingNetPos) {
				var netPos = pos;
				bool posChanged = false;
				if (netXPos != null) {
					netPos.x = (float)netXPos;
					posChanged = true;
				}
				if (netYPos != null) {
					netPos.y = (float)netYPos;
					posChanged = true;
				}
				if (posChanged) {
					changePos(netPos);
				}
			}

			
			int spriteIndex = -1;
			if (Global.spriteIndexByName.ContainsKey(sprite.name)) {
				spriteIndex = Global.spriteIndexByName[sprite.name];
			}
			if (netSpriteIndex != null && netSpriteIndex != spriteIndex) {
				int index = (int)netSpriteIndex;
				if (index >= 0 && index < Global.spriteCount) {
					string spriteName = Global.spriteNameByIndex[index];
					changeSprite(spriteName, true);
				}
			}
			if (netFrameIndex != null && frameIndex != netFrameIndex) {
				if (netFrameIndex >= 0 && netFrameIndex < sprite.totalFrameNum) {
					frameIndex = (int)netFrameIndex;
				}
			}

			if (netXDir != null && xDir != netXDir) {
				xDir = (int)netXDir;
			}
			if (netYDir != null && yDir != netYDir) {
				yDir = (int)netYDir;
			}
			if (netAngle != null && netAngle != byteAngle) {
				byteAngle = netAngle.Value;
			}
		}
	}

	public bool isRollingShield() {
		return this is RollingShieldProj;
	}

	public virtual bool shouldRender(float x, float y) {
		if (destroyed) {
			return false;
		}
		// Don't draw things without sprites or with the "null" sprite.
		if (sprite.name == "null" || currentFrame == null) {
			return false;
		}
		// Don't draw actors out of the screen for optimization
		var alignOffset = sprite.getAlignOffset(frameIndex, xDir, yDir);
		var rx = pos.x + x + alignOffset.x;
		var ry = pos.y + y + alignOffset.y;
		var rect = new Rect(
			rx,
			ry,
			rx + currentFrame.rect.w(),
			ry + currentFrame.rect.h()
		);
		var camRect = new Rect(
			Global.level.camX - 50, Global.level.camY - 50,
			Global.level.camX + Global.viewScreenW + 50,
			Global.level.camY + Global.viewScreenH + 50
		);
		if (!rect.overlaps(camRect)) {
			return false;
		}

		return true;
	}

	public virtual void render(float x, float y) {
		if (!shouldRender(x, y)) {
			return;
		}

		//console.log(this.pos.x + "," + this.pos.y);

		float drawX = MathF.Round(pos.x);
		float drawY = MathF.Round(pos.y);

		if (customAngleRendering) {
			renderFromAngle(x, y);
		} else {
			sprite.draw(
				frameIndex, drawX, drawY, xDir, yDir,
				getRenderEffectSet(), alpha, xScale, yScale, zIndex,
				getShaders(), angle: angle ?? 0, actor: this, useFrameOffsets: true
			);
		}

		renderHitboxes();
	}

	public void commonHealLogic(Player healer, float healAmount, float currentHealth, float maxHealth, bool drawHealText) {
		commonHealLogic(healer, (decimal)healAmount, (decimal)currentHealth, (decimal)maxHealth, drawHealText);
	}

	public void commonHealLogic(Player healer, decimal healAmount, decimal currentHealth, decimal maxHealth, bool drawHealText) {
		if (drawHealText && ownedByLocalPlayer) {
			float reportAmount = (float)Helpers.clampMax(healAmount, maxHealth - currentHealth);
			if (reportAmount == 0) {
				bool hasSubtankCapacity = false;
				if (this is Character chr && chr.player.hasSubtankCapacity()) hasSubtankCapacity = true;
				if (this is Maverick maverick && maverick.player.hasSubtankCapacity()) hasSubtankCapacity = true;
				if (hasSubtankCapacity) {
					RPC.playSound.sendRpc("subtankFill", netId);
				}
			} else {
				healer.creditHealing(reportAmount);
				addDamageTextHelper(healer, -reportAmount, 16, sendRpc: true);
			}
		}
	}

	public void addDamageTextHelper(
		Player? attacker, float damage, float maxHealth, bool sendRpc
	) {
		if (attacker == null) return;

		float reportDamage = Helpers.clampMax(damage, maxHealth);
		if (damage == Damager.ohkoDamage && damage >= maxHealth) {
			if (Helpers.randomRange(0, 20) != 10) {
				addDamageText("Instakill!", (int)FontType.RedishOrange);
			} else {
				addDamageText("Fatality!", (int)FontType.RedishOrange);
			}
		} else if (attacker.isMainPlayer) {
			addDamageText(reportDamage);
		} else if (ownedByLocalPlayer && sendRpc) {
			RPC.addDamageText.sendRpc(attacker.id, netId, reportDamage);
		}
	}

	public void addDamageText(float damage) {
		int xOff = 0;
		int yOff = 0;
		for (int i = damageTexts.Count - 1; i >= 0; i--) {
			if (damageTexts[i].time < 0.1f) {
				yOff -= 8;
			}
		}
		string text = damage.ToString();
		FontType color = FontType.Red;
		if (damage < 0) {
			text = (damage * -1).ToString();
			color = FontType.Green;
		}
		if (damage != 0 && damage < 1 && damage > -1) {
			text = text[1..];
		}
		damageTexts.Add(new DamageText(text, 0, pos, new Point(xOff, yOff), (int)color));
	}

	public void addDamageText(string text, int color) {
		int xOff = 0;
		int yOff = 0;
		for (int i = damageTexts.Count - 1; i >= 0; i--) {
			if (damageTexts[i].time < 6) {
				yOff -= (6 - (int)damageTexts[i].time);
			}
		}
		damageTexts.Add(new DamageText(text, 0, pos, new Point(xOff, yOff), color));
	}

	public void renderDamageText(float yOff) {
		for (int i = damageTexts.Count - 1; i >= 0; i--) {
			var dt = damageTexts[i];
			dt.time += 1;
			if (dt.time >= 46) {
				damageTexts.RemoveAt(i);
			}
			dt.offset.x += (Helpers.randomRange(-4, 4) / 4f);
			dt.offset.y -= 1;

			float textPosX = dt.pos.x;
			if (dt.offset.x >= 0) {
				textPosX += MathInt.Ceiling(dt.offset.x);
			} else {
				textPosX += MathInt.Floor(dt.offset.x);
			}
			float textPosY = dt.pos.y - yOff + dt.offset.y;
			FontType color = (FontType)dt.color;
			Fonts.drawText(
				color, dt.text, textPosX, textPosY,
				Alignment.Center, isWorldPos: true, depth: ZIndex.HUD
			);
		}
	}

	public virtual void renderHUD() {

	}

	public HashSet<RenderEffectType> getRenderEffectSet() {
		var renderEffectSet = new HashSet<RenderEffectType>();
		foreach (var kvp in renderEffects) {
			if (!kvp.Value.isFlashing()) {
				renderEffectSet.Add(kvp.Key);
			}
		}
		return renderEffectSet;
	}

	public virtual void renderFromAngle(float x, float y) {
		sprite.draw(0, pos.x + x, pos.y + y, 1, 1, getRenderEffectSet(), 1, 1, 1, zIndex);
	}

	public bool isAnimOver() {
		return (
			frameIndex == sprite.totalFrameNum - 1 && frameTime >= currentFrame.duration ||
			frameIndex >= sprite.totalFrameNum
		);
	}

	public void takeOwnership() {
		ownedByLocalPlayer = true;
		frameSpeed = 1;
	}

	public void addRenderEffect(
		RenderEffectType type, float flashTime = 0,
		float time = float.MaxValue, float cycleTime = -1
	) {
		if (renderEffects.ContainsKey(type)) return;
		renderEffects[type] = new RenderEffect(type, flashTime, time, cycleTime);
	}

	public void addRenderEffect(RenderEffectType type) {
		if (renderEffects.ContainsKey(type)) return;
		renderEffects[type] = new RenderEffect(type, 0, float.MaxValue);
	}


	public void removeRenderEffect(RenderEffectType type) {
		renderEffects.Remove(type);
	}

	// It's important to configure actors properly for cleanup. The ones here indicate which ones to cleanup if no net response in 5 seconds
	public bool cleanUpOnNoResponse() {
		return this is Anim || this is Projectile || this is RaySplasherTurret;
	}

	// These are ones that should be cleaned up when the player leaves, but too important to be deleted if no response in 5 seconds
	public bool cleanUpOnPlayerLeave() {
		return this is Maverick || this is RideArmor || this is WolfSigmaHand || this is WolfSigmaHead;
	}

	public virtual void onDestroy() {
		if (netId != null && netOwner != null) {
			Global.level.recentlyDestroyedNetActors[netId.Value] = 0;
		}
	}

	// Optionally take in a sprite to draw when destroyed
	public virtual void destroySelf(
		string spriteName = "", string fadeSound = "",
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {
		// These should never be destroyed and can break the match if so
		if (this is Flag || this is FlagPedestal || this is ControlPoint || this is VictoryPoint) {
			return;
		}

		if (!destroyed) {
			destroyed = true;
			destroyedOnFrame = Global.floorFrameCount;
			if (Global.serverClient != null &&
				netId is not null &&
				Global.level.actorsById.ContainsKey(netId.Value)
			) {
				if (Global.level.actorsById[netId.Value] == this) {
					Global.level.actorsById.Remove(netId.Value);
					Global.level.destroyedActorsById[netId.Value] = this;
				}
			}
			onDestroy();
		} else {
			return;
		}

		//console.log("DESTROYING")
		Global.level.removeGameObject(this);
		ushort spriteNameIndex = ushort.MaxValue;
		if (!String.IsNullOrEmpty(spriteName)) {
			var anim = new Anim(getCenterPos(), spriteName, xDir, null, true);
			// TODO: Fix this. WTF GM19.
			if (spriteName != "explosion") {
				anim.byteAngle = byteAngle;
				if (anim.angle != null) {
					anim.xDir = 1;
				}
			}

			anim.xScale = xScale;
			anim.yScale = yScale;
			spriteNameIndex = Global.spriteIndexByName[spriteName];
		}
		ushort fadeSoundIndex = ushort.MaxValue;
		if (!String.IsNullOrEmpty(fadeSound)) {
			fadeSound = fadeSound.ToLowerInvariant();
			playSound(fadeSound);
			fadeSoundIndex = Global.soundIndexByName[fadeSound];
		}

		// Character should not run destroy RPC. The destroyCharacter RPC handles that already
		if (this is not Character) {
			if ((ownedByLocalPlayer || doRpcEvenIfNotOwned) && netId != null && !disableRpc) {
				float speed = vel.magnitude;
				if (speed == 0) speed = deltaPos.magnitude / Global.spf;
				RPC.destroyActor.sendRpc(
					netId.Value, spriteNameIndex,
					fadeSoundIndex, pos, favorDefenderProjDestroy, speed
				);
			}
		}

		if (_waterWade != null) {
			_waterWade.destroySelf();
		}

		foreach (var projs in spriteFrameToProjs.Values) {
			foreach (var proj in projs) {
				proj?.destroySelf();
			}
		}

		foreach (Projectile proj in globalProjs) {
			proj.destroySelf();
		}

		destroyMusicSource();
	}

	public void shakeCamera(bool sendRpc = false) {
		Point originPoint = Global.level.getSoundListenerOrigin();
		var dist = originPoint.distanceTo(pos);
		float distFactor = ownedByLocalPlayer ? Global.screenW : Global.screenW * 0.5f;
		var percent = Helpers.clamp01(1 - (dist / (distFactor)));
		Global.level.shakeY = percent * 24f;
		if (sendRpc) {
			RPC.actorToggle.sendRpc(netId, RPCActorToggleType.ShakeCamera);
		}
	}

	public float getSoundVolume() {
		if (Global.level == null || Global.level.is1v1()) return 100 * Options.main.soundVolume;

		Point originPoint = Global.level.getSoundListenerOrigin();

		var dist = originPoint.distanceTo(pos);
		var volume = 1f - (dist / Global.screenW);

		volume = volume * 100 * Options.main.soundVolume;
		volume = Helpers.clamp(volume, 0, 100);

		return volume;
	}

	public float getSoundDist() {
		if (Global.level == null || Global.level.is1v1()) return 100 * Options.main.soundVolume;

		Point originPoint = Global.level.getSoundListenerOrigin();

		return originPoint.distanceTo(pos);
	}

	public Point getSoundPos() {
		Point originPoint = Global.level.getSoundListenerOrigin();
		float xPos = (pos.x - originPoint.x) / Global.halfScreenW;
		float yPos = (pos.y - originPoint.y) / Global.halfScreenH;
		return new Point(xPos, yPos);
	}


	public SoundWrapper? playSound(string soundKey, bool forcePlay = false, bool sendRpc = false) {
		soundKey = soundKey.ToLowerInvariant();
		if (!Global.soundBuffers.ContainsKey(soundKey)) {
			throw new Exception($"Attempted playing missing sound with name \"{soundKey}\"");
		}
		return playSound(Global.soundBuffers[soundKey], forcePlay: forcePlay, sendRpc: sendRpc);
	}

	public SoundWrapper createSoundWrapper(SoundBufferWrapper soundBuffer) {
		if (this is Character chara) {
			string charName = chara switch {
				MegamanX or RagingChargeX => "mmx",
				Zero => "zero",
				PunchyZero => "pzero",
				BusterZero => "dzero",
				Vile => "vile",
				Axl => "axl",
				CmdSigma => "sigma",
				NeoSigma => "neosigma",
				Doppma => "doppma",
				_ => "error"
			};

			var overrideSoundBuffer = Global.charSoundBuffers.GetValueOrDefault(soundBuffer.soundKey + "." + charName);
			if (overrideSoundBuffer != null) {
				return new SoundWrapper(overrideSoundBuffer, this);
			}
		}
		return new SoundWrapper(soundBuffer, this);
	}

	public SoundWrapper? playSound(SoundBufferWrapper soundBuffer, bool forcePlay = false, bool sendRpc = false) {
		var recentClipCount = Global.level.recentClipCount;
		if (recentClipCount.ContainsKey(soundBuffer.soundKey) &&
			recentClipCount[soundBuffer.soundKey].Count > 1
		) {
			if (!forcePlay) {
				return null;
			}
		}
		if (!recentClipCount.ContainsKey(soundBuffer.soundKey)) {
			recentClipCount[soundBuffer.soundKey] = new List<float>();
		}
		if (getSoundVolume() > 0) {
			recentClipCount[soundBuffer.soundKey].Add(0);
		}
		if (getSoundDist() > 600) {
			return null;
		}

		int? charNum = null;
		if (this is Character || this is Maverick) {
			charNum = this is Character chr ? chr.player.charNum : 4;
		}

		SoundWrapper sound = createSoundWrapper(soundBuffer);
		sound.play();

		if (charNum != null && soundBuffer.soundPool != SoundPool.Voice) {
			var matchingVoice = Helpers.getRandomMatchingVoice(
				Global.voiceBuffers, soundBuffer.soundKey, charNum.Value
			);
			if (matchingVoice != null) {
				playSound(matchingVoice);
			}
		}

		if (sendRpc && ownedByLocalPlayer) {
			RPC.playSound.sendRpc(soundBuffer.soundKey, netId);
		}

		return sound;
	}

	public bool withinX(Actor other, float amount) {
		return Math.Abs(pos.x - other.pos.x) <= amount;
	}

	public bool withinY(Actor other, float amount) {
		return Math.Abs(pos.y - other.pos.y) <= amount;
	}

	public bool isFacing(Actor other) {
		return ((pos.x < other.pos.x && xDir == 1) || (pos.x >= other.pos.x && xDir == -1));
	}

	// GMTODO: must be more generic, account for other alignments
	// Then find all places using this and ajust as necessary
	public virtual Point getCenterPos() {
		if (standartCollider == null) {
			return pos;
		}
		Rect? rect = standartCollider.shape.getNullableRect();
		if (rect == null) {
			return pos;
		}
		if (sprite.animData.alignOffY == 1) {
			return pos.addxy(0, -rect.Value.h() / 2);
		}
		return pos;
	}

	public void breakFreeze(Player player, Point? pos = null, bool sendRpc = false) {
		if (pos == null) pos = getCenterPos();
		if (!player.ownedByLocalPlayer) sendRpc = false;

		Anim.createGibEffect("shotgun_ice_break", pos.Value, player, sendRpc: sendRpc);

		playSound("iceBreak", sendRpc: sendRpc);
	}

	public void updateProjectileCooldown() {
		foreach (string key in projectileCooldown.Keys.ToList()) {
			float cooldown = projectileCooldown[key];
			if (cooldown > 0) {
				projectileCooldown[key] = Helpers.clampMin(cooldown - Global.gameSpeed, 0);
			}
		}
		foreach (string key in flinchCooldown.Keys.ToList()) {
			float cooldown = flinchCooldown[key];
			if (cooldown > 0) {
				flinchCooldown[key] = Helpers.clampMin(cooldown - Global.gameSpeed, 0);
			}
		}
		foreach (int key in globalFlinchCooldown.Keys.ToList()) {
			float cooldown = globalFlinchCooldown[key];
			if (cooldown > 0) {
				globalFlinchCooldown[key] = Helpers.clampMin(cooldown - Global.gameSpeed, 0);
			}
		}
	}

	public void turnToPos(Point lookPos) {
		if (lookPos.x > pos.x) xDir = 1;
		else xDir = -1;
	}

	public void turnToInput(Input input, Player player) {
		int dir = input.getXDir(player);
		if (dir != 0) {
			xDir = dir;
		}
	}

	public void stopMovingS() {
		xIceVel = 0;
		xPushVel = 0;
		yPushVel = 0;
		xSwingVel = 0;
		vel.x = 0;
		vel.y = 0;
	}

	public void stopMoving() {
		vel.x = 0;
		vel.y = 0;
	}

	public void unstickFromGround() {
		useGravity = false;
		grounded = false;
		vel.y = -1;
	}

	public bool stopCeiling() {
		if (vel.y < 0 && Global.level.checkTerrainCollisionOnce(this, 0, -1) != null) {
			vel.y = 0;
			return true;
		}
		return false;
	}

	public Point getPoiOrigin() {
		if (!reversedGravity) return pos;
		if (this is MagnaCentipede ms) return pos.addxy(0, -ms.height);
		return pos;
	}

	public Point getPOIPos(Point poi) {
		return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
	}

	public Point getFirstPOIOrDefault(int index = 0) {
		if (sprite.getCurrentFrame()?.POIs?.Length > 0) {
			Point poi = sprite.getCurrentFrame().POIs[index];
			return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
		}
		return getCenterPos();
	}

	public Point getFirstPOIOrDefault(string tag, int? frameIndex = null) {
		Frame frame = sprite.getCurrentFrame();
		if (frame.POIs?.Length > 0) {
			int poiIndex = frame.POITags.FindIndex(t => t == tag);
			if (poiIndex >= 0) {
				Point poi = frame.POIs[poiIndex];
				return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
			}
		}
		return getCenterPos();
	}

	public Point? getFirstPOI(int index = 0) {
		if (sprite.getCurrentFrame().POIs.Length > 0) {
			Point poi = sprite.getCurrentFrame().POIs[index];
			return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
		}
		return null;
	}

	public Point? getFirstPOI(string tag) {
		if (sprite.getCurrentFrame().POIs.Length > 0) {
			int poiIndex = sprite.getCurrentFrame().POITags.FindIndex(t => t == tag);
			if (poiIndex >= 0) {
				Point poi = sprite.getCurrentFrame().POIs[poiIndex];
				return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
			}
		}
		return null;
	}

	public Point? getFirstPOIOffsetOnly(int index = 0) {
		if (sprite.getCurrentFrame().POIs.Length > 0) {
			Point poi = sprite.getCurrentFrame().POIs[index];
			return poi;
		}
		return null;
	}

	public Point getFtdPredictedPos() {
		if (netOwner == null) return pos;
		float combinedPing = (netOwner.ping ?? 0) + (Global.level.mainPlayer.ping ?? 0);
		float pingSeconds = combinedPing / 1000;
		return pos.add(deltaPos.times(pingSeconds / Global.spf));
	}

	public void addMusicSource(string musicName, Point worldPos, bool moveWithActor, bool loop = true) {
		var ms = Global.musics[musicName].clone();
		ms.musicSourcePos = worldPos;
		ms.musicSourceActor = this;
		ms.volume = 0;
		ms.moveWithActor = moveWithActor;
		ms.loop = loop;
		ms.play();
		Global.level.musicSources.Add(ms);
		musicSource = ms;
	}

	public void destroyMusicSource() {
		if (musicSource != null) {
			Global.level.musicSources.Remove(musicSource);
			musicSource.destroy();
			musicSource = null;
		}
	}

	public float getDistFromGround() {
		var ground = Global.level.raycast(pos, pos.addxy(0, 1000), new List<Type>() { typeof(Wall) });
		if (ground?.hitData?.hitPoint != null) {
			return MathF.Abs(pos.y - ground.hitData.hitPoint.Value.y);
		}
		return -1;
	}

	public void moveWithMovingPlatform() {
		if (!Global.level.hasMovingPlatforms) isStatic = true;
		var collideDatas = Global.level.getTerrainTriggerList(
			this, new Point(0, 1), typeof(Wall), typeof(MovingPlatform)
		);
		foreach (var collideData in collideDatas) {
			var hitWall = collideData?.gameObject as Wall;
			if (hitWall != null && hitWall.isMoving) {
				move(hitWall.deltaMove, useDeltaTime: false);
				break;
			}
		}
	}

	public const int labelCursorOffY = 5;
	public const int labelWeaponIconOffY = 18;
	public const int labelKillFeedIconOffY = 9;
	public const int labelAxlAimModeIconOffY = 15;
	public const int labelCooldownOffY = 15;
	public const int labelSubtankOffY = 17;
	public const int labelStatusOffY = 20;
	public const int labelHealthOffY = 6;
	public const int labelNameOffY = 10;

	public float currentLabelY;

	public void deductLabelY(float amount) {
		currentLabelY -= amount;
		// DrawWrappers.DrawLine(pos.x - 10, pos.y + currentLabelY, pos.x + 10, pos.y + currentLabelY, Color.Red, 1, ZIndex.HUD);
	}

	public void drawFuelMeter(float healthPct, float sx, float sy) {
		float healthBarInnerWidth = 30;
		Color color = new Color();
		float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
		if (healthPct > 0.66) color = Color.Green;
		else if (healthPct <= 0.66 && healthPct >= 0.33) color = Color.Yellow;
		else if (healthPct < 0.33) color = Color.Red;

		DrawWrappers.DrawRect(pos.x - 47 + sx, pos.y - 16 + sy, pos.x - 42 + sx, pos.y + 16 + sy, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
		DrawWrappers.DrawRect(pos.x - 46 + sx, pos.y + 15 - width + sy, pos.x - 43 + sx, pos.y + 15 + sy, true, color, 0, ZIndex.HUD - 1);
	}

	public CollideData? getHitWall(float x, float y) {
		return Global.level.checkTerrainCollisionOnce(this, x, y, checkPlatforms: true);
	}

	public void setRaColorShader() {
		if (sprite.name == "neutralra_pieces") {
			genericShader = Helpers.cloneGenericPaletteShader("paletteEG01");
			genericShader?.SetUniform("palette", 1);
		} else if (sprite.name == "kangaroo_pieces") {
			genericShader = Helpers.cloneGenericPaletteShader("paletteKangaroo");
			genericShader?.SetUniform("palette", 1);
		} else if (sprite.name == "hawk_pieces") {
			genericShader = Helpers.cloneGenericPaletteShader("paletteHawk");
			genericShader?.SetUniform("palette", 1);
		} else if (sprite.name == "frog_pieces") {
			genericShader = Helpers.cloneGenericPaletteShader("paletteFrog");
			genericShader?.SetUniform("palette", 1);
		}
	}

	public virtual void statePreUpdate() {
	}

	public virtual void stateUpdate() {
		// For object specific state-machine code.
	}

	public virtual void statePostUpdate() {
	}

	public virtual float getGravity() {
		return Global.level.gravity * gravityModifier * gravityWellModifier;
	}

	public Actor[] getCloseActors(
		int distance, bool isRequesterAI = false,
		bool checkWalls = false, bool includeAllies = false
	) {
		HashSet<Actor> closeActors = new();
		int halfDist = MathInt.Floor(distance / 2f);

		Point checkPos = new Point(MathF.Round(pos.x), MathF.Round(pos.y));
		Shape shape = Rect.createFromWH(
			pos.x - halfDist, pos.y - halfDist,
			distance, distance
		).getShape();
		var hits = Global.level.checkCollisionsShape(shape, null);
		int alliance = -1;
		if (!includeAllies) {
			alliance = this switch {
				Character selfChar => selfChar.player.alliance,
				Projectile selfProj => selfProj.damager.owner.alliance,
				Maverick selfMvrk => selfMvrk.player.alliance,
				_ => -1
			};
		}
		foreach (CollideData hit in hits) {
			if (hit.gameObject is not Actor actor || actor == this) {
				continue;
			}
			if (!includeAllies && alliance != -1) {
				if (actor is Character character && character.player.alliance == alliance ||
					actor is Projectile proj && proj.damager?.owner.alliance == alliance ||
					actor is Maverick maverick && maverick.player.alliance == alliance ||
					actor is DarkHoldProj
				) {
					continue;
				}
			}
			if (!closeActors.Contains(actor)) {
				closeActors.Add(actor);
			}
		}
		return closeActors.ToArray();
	}

	public string getActorTypeName() {
		return GetType().ToString().RemovePrefix("MMXOnline.");
	}
}
