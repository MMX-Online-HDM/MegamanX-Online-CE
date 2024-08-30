using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

// Everything strongly related to actor collision should go here
public partial class Actor {
	private Collider? _globalCollider;

	// One of the possible colliders of an actor.
	// This is typically used for a collider shared across multiple sprites an actor can be.
	// Typically used for chars, mavericks, rides, etc.
	public Collider? globalCollider {
		get {
			return _globalCollider;
		}
		set {
			Global.level.removeFromGrid(this);
			_globalCollider = value;
			Global.level.addGameObjectToGrid(this);
		}
	}

	public void changeGlobalColliderWithoutGridChange(Collider? newGlobalCollider) {
		_globalCollider = newGlobalCollider;
	}

	public void changeGlobalCollider(List<Point> newPoints) {
		Global.level.removeFromGrid(this);
		_globalCollider._shape.points = newPoints;
		Global.level.addGameObjectToGrid(this);
	}

	// Gets the bounding box of all the hitboxes this actor has. Used for properly updating the grid.
	// Returning null means DON'T put it in the grid, which helps with optimization.
	// This will be called a lot so optimize it
	public Shape? getAllCollidersShape() {
		var allColliders = getAllColliders();
		if (allColliders.Count == 0) {
			return null;
		}
		if (allColliders.Count == 1) {
			return allColliders[0].shape;
		}

		bool found = false;
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		foreach (Collider collider in allColliders) {
			foreach (Point point in collider.shape.points) {
				if (point.x < minX) minX = point.x;
				if (point.x > maxX) maxX = point.x;
				if (point.y < minY) minY = point.y;
				if (point.y > maxY) maxY = point.y;
				found = true;
			}
		}

		if (!found) return null;
		return new Rect(minX, minY, maxX, maxY).getShape();
	}

	public Collider collider {
		get {
			Collider? stdColl = getAllColliders().FirstOrDefault();
			if (stdColl == null) {
				return null;
			}
			Collider? lvColl = getTerrainCollider();
			if (lvColl != null) {
				return lvColl;
			}
			return stdColl;
		}
	}

	public Collider? standartCollider => getAllColliders().FirstOrDefault();

	public Collider? physicsCollider {
		get {
			return getAllColliders().FirstOrDefault(c => !c.isTrigger);
		}
	}

	public List<Collider> getAllColliders() {
		var colliders = new List<Collider>();
		if (globalCollider != null) {
			colliders.Add(globalCollider);
		}
		foreach (var collider in sprite.hitboxes) {
			colliders.Add(collider);
		}
		foreach (var collider in sprite.frameHitboxes[frameIndex]) {
			colliders.Add(collider);
		}
		return colliders;
	}

	public void renderHitboxes() {
		if (!Global.showHitboxes) {
			return;
		}
		bool hasNonAttackColider = false;
		foreach (Collider allCollider in getAllColliders()) {
			
			if (allCollider._shape.points.Count == 4) {
				Color hitboxColor = new Color(50, 100, 255, 50);
				Color outlineColor = new Color(0, 0, 255, 200);
				if (allCollider.isAttack()) {
					hitboxColor = new Color(255, 0, 0, 100);
					outlineColor = new Color(255, 0, 0, 200);
				} else {
					hasNonAttackColider = true;
				}
				Rect rect = allCollider.shape.getRect();
				rect.x1 += 1;
				rect.y1 += 1;
				rect.x2 -= 1;
				rect.y2 -= 1;
				DrawWrappers.DrawRect(
					rect.x1, rect.y1, rect.x2, rect.y2,
					true, hitboxColor, 1, zIndex + 1, true,
					outlineColor
				);
			} else {
				Color hitboxColor = new Color(173, 216, 230, 200);
				if (allCollider.isAttack()) {
					hitboxColor = new Color(byte.MaxValue, 0, 0, 100);
					if (destroyed) {
						hitboxColor = new Color(128, 0, 255, 100);
					}
				} else {
					hasNonAttackColider = true;
				}
				DrawWrappers.DrawPolygon(allCollider.shape.points, hitboxColor, fill: true, zIndex + 1);
			}
		}
		if (hasNonAttackColider) {
			Collider? terrainCollider = getTerrainCollider();
			if (terrainCollider == null) {
				return;
			}
			Rect rect = terrainCollider.shape.getRect();
			rect.x1 += 1;
			rect.y1 += 1;
			rect.x2 -= 1;
			rect.y2 -= 1;
			DrawWrappers.DrawPolygon(
				rect.getPoints(),
				new Color(0, 255, 0, 150),
				fill: false, zIndex + 1, true
			);
		}
		//DrawWrappers.DrawCircle(collider.shape, Color.Blue, true, zIndex + 1, false, true);
	}

	public HashSet<Tuple<Collider, Collider>> collidedInFrame = new HashSet<Tuple<Collider, Collider>>();
	public void registerCollision(CollideData collideData) {
		var tuple = new Tuple<Collider, Collider>(collideData.myCollider, collideData.otherCollider);
		if (!collidedInFrame.Contains(tuple)) {
			collidedInFrame.Add(tuple);
			onCollision(collideData);
		}
	}

	public virtual void onCollision(CollideData other) {
	}

	// Terrain colider.
	// This is used for stage collision.
	// If this is Null it defaults to the Global collider.
	// Generally if a global collider does not exist this is disabled too.
	public virtual Collider? getTerrainCollider() {
		return null;
	}

	// The default global collider. This can be thought of the one that is used most often, in the most sprites.
	public virtual Collider? getGlobalCollider() {
		return null;
	}

	// The spriteToCollider dict is a streamlined way to be able to change global colliders based on a sprite name.
	// The key of this dictionary is the sprite name and the value is the collider.
	// Use * in the key to match section of string against anything,
	// allowing for streamlined application to multiple sprites at once
	// If the sprite key is not found, the global collider will use the default global collider.
	// If it's null, it is removed.
	public Dictionary<string, Collider?> spriteToCollider = new();

	public void changeGlobalColliderOnSpriteChange(string newSpriteName) {
		if (spriteToColliderMatch(newSpriteName, out Collider overrideGlobalCollider)) {
			changeGlobalColliderWithoutGridChange(overrideGlobalCollider);
		} else {
			changeGlobalColliderWithoutGridChange(getGlobalCollider());
		}
	}

	public bool spriteToColliderMatch(string spriteName, out Collider? overrideGlobalCollider) {
		int underscorePos = spriteName.IndexOf('_');
		if (underscorePos >= 0) {
			spriteName = spriteName.Substring(underscorePos + 1);
		}

		foreach ((string name, Collider? colliderValue) in spriteToCollider) {
			string spriteKey = name;
			if (spriteKey.Contains("*")) {
				spriteKey = spriteKey.Replace("*", "");
				if (spriteName.StartsWith(spriteKey)) {
					overrideGlobalCollider = colliderValue;
					return true;
				}
			} else {
				if (spriteName == spriteKey) {
					overrideGlobalCollider = colliderValue;
					return true;
				}
			}
		}

		overrideGlobalCollider = null;
		return false;
	}

	// Returns a dictionary whose key is the projectile id and value is a function returning the projectiles that should be created
	// Use sparingly, getProjFromHitboxBase should be preferred for most things
	public virtual Dictionary<int, Func<Projectile>> getGlobalProjs() {
		return new Dictionary<int, Func<Projectile>>();
	}

	public virtual Projectile? getProjFromHitboxBase(Collider hitbox) {
		Point centerPoint = hitbox.shape.getRect().center();
		Projectile? proj = getProjFromHitbox(hitbox, centerPoint);
		if (proj != null) {
			proj.hitboxActor = this;
			proj.globalCollider = hitbox.clone();
		}
		return proj;
	}

	public virtual Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		int meleeId = getHitboxMeleeId(hitbox);
		if (meleeId == -1) {
			return null;
		}
		Projectile? proj = getMeleeProjById(meleeId, centerPoint);
		if (proj != null) {
			proj.meleeId = meleeId;
			proj.owningActor = this;
		}
		return proj;
	}

	public virtual int getHitboxMeleeId(Collider hitbox) {
		return -1;
	}

	public virtual Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return null;
	}

	public virtual void updateProjFromHitbox(Projectile proj) {

	}

	public void incPos(Point amount) {
		Global.level.removeFromGrid(this);
		pos.inc(amount);
		Global.level.addGameObjectToGrid(this);
	}

	public void changePos(Point newPos) {
		Global.level.removeFromGrid(this);
		pos = newPos;
		Global.level.addGameObjectToGrid(this);
	}

	public CollideData? sweepTest(Point offset) {
		Point inc = offset.clone();
		var collideData = Global.level.checkCollisionActor(this, inc.x, inc.y);
		if (collideData != null) {
			return collideData;
		}
		return null;
	}

	public void moveToPos(Point dest, float speed) {
		move(pos.directionToNorm(dest).times(speed));
	}

	public void moveToXPos(Point dest, float speed) {
		var dir = pos.directionToNorm(dest);
		dir.y = 0;
		move(dir.times(speed));
	}

	public bool tryMoveExact(Point amount, out CollideData hit) {
		hit = Global.level.checkCollisionActor(this, amount.x, amount.y);
		if (hit != null) {
			return false;
		}
		move(amount, false);
		return true;
	}

	public bool tryMove(Point amount, out CollideData hit) {
		hit = Global.level.checkCollisionActor(this, amount.x * Global.spf * 2, amount.y * Global.spf * 2);
		if (hit != null) {
			return false;
		}
		move(amount, true);
		return true;
	}

	public void moveMaxDist(Point amount, Point origin, float maxDist) {
		move(amount, true);

		float xDiff = pos.x - origin.x;
		float yDiff = pos.y - origin.y;
		if (MathF.Abs(xDiff) > maxDist) {
			changePos(new Point(origin.x + (maxDist * MathF.Sign(xDiff)), pos.y));
		}
		if (MathF.Abs(yDiff) > maxDist) {
			changePos(new Point(pos.x, origin.y + (maxDist * MathF.Sign(yDiff))));
		}
	}

	public void move(
		Point amount, bool useDeltaTime = true, bool pushIncline = true,
		bool useIce = true, MoveClampMode clampMode = MoveClampMode.None
	) {
		var times = useDeltaTime ? Global.spf : 1;

		if (grounded && groundedIce && useIce && (
			this is Character || this is Maverick || this is RideArmor
		)) {
			if (amount.x > 0) {
				if (xIceVel < amount.x) {
					xIceVel += amount.x * Global.spf * 5;
				}
			} else {
				if (xIceVel > amount.x) {
					xIceVel += amount.x * Global.spf * 5;
				}
			}
			return;
		}

		Point moveAmount = amount.times(times);

		//No collider: just move
		if (physicsCollider == null) {
			incPos(moveAmount);
		}
		// Regular collider: need to detect collision incrementally
		// and stop moving past a collider if that's the case
		else {
			freeFromCollision();

			var inc = amount.clone();
			var incAmount = inc.multiply(times);

			// Hack to make it not get stuck sometimes
			if (this is RideChaser) {
				incPos(incAmount);
				freeFromCollision();
				return;
			}

			var mtv = Global.level.getMtvDir(this, incAmount.x, incAmount.y, incAmount, pushIncline);
			if (mtv != null && mtv?.magnitude > 10) {
				mtv = Global.level.getMtvDir(this, incAmount.x, incAmount.y, null, false);
			}
			incPos(incAmount);
			if (mtv != null) {
				incPos(mtv.Value.unitInc(0.01f));
			}

			//This shouldn't be needed, but sometimes getMtvDir doesn't free properly or isn't returned
			freeFromCollision();
		}
	}

	public void freeFromCollision() {
		//Already were colliding in first place: free with path of least resistance
		var currentCollideDatas = Global.level.checkTerrainCollision(this, 0, 0, null);
 
		foreach (var collideData in currentCollideDatas) {
			if (this is Character chara && collideData.gameObject is Character otherChara) {
				chara.insideCharacter = true;
				otherChara.insideCharacter = true;
				continue;
			}

			Point? freeVec = null;
			if (this is RideChaser rc && physicsCollider != null) {
				// Hack to make ride chasers not get stuck on inclines
				freeVec = physicsCollider.shape.getMinTransVectorDir(collideData.otherCollider.shape, new Point(0, -1));
			}

			if ((freeVec == null || freeVec.Value.magnitude > 20) && physicsCollider != null) {
				freeVec = physicsCollider.shape.getMinTransVector(collideData.otherCollider.shape);
			}

			if (freeVec == null) {
				return;
			}
			if (this is Character && freeVec.Value.magnitude > 20) {
				return;
			}
			incPos(freeVec.Value.unitInc(0.01f));
		}
	}

	public Point? getGroundHit(float dist = 1000) {
		var ground = Global.level.raycast(pos, pos.addxy(0, dist), new List<Type>() { typeof(Wall) });
		return ground?.hitData?.hitPoint;
	}

	public void setGlobalColliderTrigger(bool boolValue) {
		if (globalCollider != null) {
			globalCollider.isTrigger = boolValue;
		}
	}

	public void stopOnCeilingHit() {
		if (vel.y < 0 && Global.level.checkCollisionActor(this, 0, -1) != null) {
			vel.y = 0;
		}
	}

	public CollideData checkCollision(float incX, float incY) {
		return Global.level.checkCollisionActor(this, incX, incY, autoVel: true);
	}
}
