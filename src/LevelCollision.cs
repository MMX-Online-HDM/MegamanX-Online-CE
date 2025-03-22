using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public struct Cell {
	public int x;
	public int y;
	public List<GameObject> gameobjects;
	public Cell(int x, int y, List<GameObject> gameobjects) {
		this.x = x;
		this.y = y;
		this.gameobjects = gameobjects;
	}
}

public partial class Level {
	public void setupGrid(float cellWidth) {
		this.cellWidth = cellWidth;
		int width = this.width;
		int height = this.height;
		int xCellCount = MathInt.Ceiling((decimal)width / (decimal)cellWidth);
		int yCellCount = MathInt.Ceiling((decimal)height / (decimal)cellWidth);
		//console.log("Creating grid with width " + hCellCount + " and height " + vCellCount);
		grid = new List<GameObject>[xCellCount, yCellCount];
		terrainGrid = new List<GameObject>[xCellCount, yCellCount];

		for (var x = 0; x < xCellCount; x++) {
			for (var y = 0; y < yCellCount; y++) {
				grid[x, y] = new List<GameObject>();
				terrainGrid[x, y] = new List<GameObject>();
			}
		}
	}

	//Optimize this function, it will be called a lot
	public List<Cell> getGridCells(Shape shape) {
		var cells = new List<Cell>();
		int startX = MathInt.Floor(shape.minX);
		int endX = MathInt.Floor(shape.minX);
		int startY = MathInt.Ceiling(shape.maxY);
		int endY = MathInt.Ceiling(shape.maxY);

		//Line case
		if (shape.points.Count == 2) {
			var point1 = shape.points[0];
			var point2 = shape.points[1];
			var dir = point1.directionToNorm(point2);
			var curX = point1.x;
			var curY = point1.y;
			float dist = 0;
			var maxDist = point1.distanceTo(point2);
			//var mag = maxDist / (this.cellWidth/2);
			float mag = cellWidth / 2;
			HashSet<int> usedCoords = new HashSet<int>();
			while (dist <= maxDist) {
				int x = MathInt.Floor(curX / cellWidth);
				int y = MathInt.Floor(curY / cellWidth);
				curX += dir.x * mag;
				curY += dir.y * mag;
				dist += mag;
				if (y < 0 || x < 0 || y >= grid.GetLength(1) || x >= grid.GetLength(0)) continue;
				int gridCoordKey = Helpers.getGridCoordKey((ushort)x, (ushort)y);
				if (usedCoords.Contains(gridCoordKey)) continue;
				usedCoords.Add(gridCoordKey);
				cells.Add(new Cell(x, y, grid[x, y]));
			}
			return cells;
		}

		int minX = MathInt.Floor(shape.minX / cellWidth);
		int maxX = MathInt.Floor(shape.maxX / cellWidth);
		int minY = MathInt.Floor(shape.minY / cellWidth);
		int maxY = MathInt.Floor(shape.maxY / cellWidth);

		minX = Math.Clamp(minX, 0, grid.GetLength(0) - 1);
		maxX = Math.Clamp(maxX, 0, grid.GetLength(0) - 1);
		minY = Math.Clamp(minY, 0, grid.GetLength(1) - 1);
		maxY = Math.Clamp(maxY, 0, grid.GetLength(1) - 1);

		for (int i = minX; i <= maxX; i++) {
			for (int j = minY; j <= maxY; j++) {
				cells.Add(new Cell(i, j, grid[i, j]));
			}
		}
		return cells;
	}

	public Rect getGridCellsPos(Shape shape) {
		int minX = MathInt.Floor(shape.minX / cellWidth);
		int maxX = MathInt.Floor(shape.maxX / cellWidth);
		int minY = MathInt.Floor(shape.minY / cellWidth);
		int maxY = MathInt.Floor(shape.maxY / cellWidth);

		minX = Math.Clamp(minX, 0, grid.GetLength(0) - 1);
		maxX = Math.Clamp(maxX, 0, grid.GetLength(0) - 1);
		minY = Math.Clamp(minY, 0, grid.GetLength(1) - 1);
		maxY = Math.Clamp(maxY, 0, grid.GetLength(1) - 1);

		return new Rect(minX, minY, maxX, maxY);
	}

	// Called a lot
	public List<GameObject> getGameObjectsInSameCell(Shape shape) {
		List<Cell> cells = getGridCells(shape);
		HashSet<GameObject> retGameobjects = new();
		foreach (Cell cell in cells) {
			if (cell.gameobjects == null) continue;
			foreach (GameObject go in cell.gameobjects) {
				if (!retGameobjects.Contains(go)) {
					retGameobjects.Add(go);
				}
			}
		}
		List<GameObject> arr = new();
		foreach (var go in retGameobjects) {
			arr.Add(go);
		}
		return arr;
	}

	// Should be called on hitbox changes.
	private void removeFromActorGrid(GameObject go) {
		int hash = go.GetHashCode();
		if (!gridsPopulatedByGo.ContainsKey(hash)) {
			return;
		}
		Rect dataPos = gridsPopulatedByGo[hash];
		for (int x = (int)dataPos.x1; x <= dataPos.x2; x++) {
			for (int y = (int)dataPos.y1; y <= dataPos.y2; y++) {
				grid[x, y].Remove(go);
				if (grid[x, y].Count == 0) {
					populatedGrids.Remove((x, y));
				}
			}
		}
		gridsPopulatedByGo.Remove(hash);
	}

	private void addToActorGrid(GameObject go) {
		if (!gameObjects.Contains(go)) {
			return;
		}
		if (gridsPopulatedByGo.ContainsKey(go.GetHashCode())) {
			removeFromActorGrid(go);
		}
		Shape? allCollidersShape = go.getAllCollidersShape();
		if (!allCollidersShape.HasValue) {
			return;
		}
		if (go is Actor actor) {
			Collider? terrainCollider = actor.getTerrainCollider();
			if (terrainCollider != null) {
				allCollidersShape = terrainCollider.shape;
			}
		}
		foreach (Cell cell in getGridCells(allCollidersShape.Value)) {
			if (!grid[cell.x, cell.y].Contains(go)) {
				if (grid[cell.x, cell.y].Count == 0) {
					populatedGrids.Add((cell.x, cell.y));
				}
				grid[cell.x, cell.y].Add(go);
			}
		}
		gridsPopulatedByGo[go.GetHashCode()] = getGridCellsPos(allCollidersShape.Value);
	}

	public Point getGroundPos(Point pos, float depth = 60) {
		var hit = Global.level.raycast(pos, pos.addxy(0, depth), new List<Type> { typeof(Wall) });
		if (hit == null) return pos;
		return hit.hitData.hitPoint.Value.addxy(0, -1);
	}

	public Point? getGroundPosNoKillzone(Point pos, float depth = 60) {
		var kzHit = Global.level.raycast(pos, pos.addxy(0, depth), new List<Type> { typeof(KillZone) });
		if (kzHit != null) return null;

		var hit = Global.level.raycast(pos, pos.addxy(0, depth), new List<Type> { typeof(Wall) });
		if (hit == null) return null;

		return hit.hitData.hitPoint.Value.addxy(0, -1);
	}

	public Point? getGroundPosWithNull(Point pos, float depth = 60) {
		var hit = Global.level.raycast(pos, pos.addxy(0, depth), new List<Type> { typeof(Wall) });
		if (hit == null) return null;
		return hit.hitData.hitPoint.Value.addxy(0, -1);
	}

	public int getGridCount() {
		int gridItemCount = 0;
		for (int x = 0; x < grid.GetLength(0); x++) {
			for (int y = 0; y < grid.GetLength(1); y++) {
				if (grid[x, y].Count > 0) {
					gridItemCount += grid[x, y].Count;
				}
			}
		}
		return gridItemCount;
	}

	public int getTGridCount() {
		int gridItemCount = 0;
		for (int x = 0; x < terrainGrid.GetLength(0); x++) {
			for (int y = 0; y < terrainGrid.GetLength(1); y++) {
				if (grid[x, y].Count > 0) {
					gridItemCount += terrainGrid[x, y].Count;
				}
			}
		}
		return gridItemCount;
	}

	public void getTotalCountInGrid() {
		var count = 0;
		var orphanedCount = 0;
		var width = this.width;
		var height = this.height;
		var hCellCount = Math.Ceiling(width / cellWidth);
		var vCellCount = Math.Ceiling(height / cellWidth);
		for (var i = 0; i < vCellCount; i++) {
			for (var j = 0; j < hCellCount; j++) {
				count += grid[j, i].Count;
				var set = grid[j, i];
				foreach (var go in set) {
					if (!gameObjects.Contains(go)) {
						//this.grid[i][j].delete(go);
						orphanedCount++;
					}
				}
			}
		}
		debugString = count.ToString();
		debugString2 = orphanedCount.ToString();
	}

	public bool hasGameObject(GameObject go) {
		return gameObjects.Contains(go);
	}

	public void addGameObject(GameObject go) {
		gameObjects.Add(go);
		addToGrid(go);
	}

	public void removeGameObject(GameObject go) {
		removeFromGrid(go);
		gameObjects.Remove(go);
	}

	public void modifyObjectGridGroups(GameObject obj, bool isActor, bool isTerrain) {
		if (isActor) {
			addToActorGrid(obj);
			obj.useActorGrid = true;
		} else {
			removeFromActorGrid(obj);
			obj.useActorGrid = false;
		}
		if (isTerrain) {
			addTerrainToGrid(obj);
			obj.useTerrainGrid = true;
		} else {
			removeFromTerrainGrid(obj);
			obj.useTerrainGrid = false;
		}
	}


	public List<GameObject> getGameObjectArray() {
		return new List<GameObject>(gameObjects);
	}

	//Should actor collide with gameobject?
	//Note: return true to indicate NOT to collide, and instead only trigger
	public bool shouldTrigger(
		Actor actor, GameObject gameObject, Collider actorCollider, Collider gameObjectCollider,
		Point intersection, bool otherway = false
	) {
		if (actor is Character && gameObject is Character) {
			var actorChar = actor as Character;
			var goChar = gameObject as Character;

			if (actorChar.isCrystalized || goChar.isCrystalized) return false;
			//if (actorChar.sprite.name.Contains("frozen") || goChar.sprite.name.Contains("frozen")) return false;
			return true;
		}

		/*if (actor is Character chr3 && (chr3.player.isViralSigma() || chr3.player.isKaiserViralSigma()) && gameObject is Ladder) {
			return true;
		}*/

		if (actorCollider.isTrigger == false && gameObject is Ladder) {
			if (actor.pos.y < gameObject.collider.shape.getRect().y1 && intersection.y > 0) {
				if (!actor.checkLadderDown) {
					return false;
				}
			}
		}

		if (actorCollider.disabled || gameObjectCollider.disabled) return false;
		if (actorCollider.isTrigger || gameObjectCollider.isTrigger) return true;

		if (actor is ShotgunIceProjSled sled && gameObject is Character chr && sled.damager.owner == chr.player) {
			return false;
		}

		if (actor is Character chr2 && gameObject is ShotgunIceProjSled sled2 && sled2.damager.owner == chr2.player) {
			return false;
		}

		if (actorCollider.wallOnly && gameObject is not Wall) return true;

		if (gameObject is Actor) {
			if (gameObjectCollider.wallOnly) return true;
		}

		if (actor is Character && gameObject is RideArmor) return true;
		if (actor is RideArmor && gameObject is Character) return true;
		if (actor is RideArmor && gameObject is RideArmor) return true;

		/*
		if (actor is Character && gameObject is Character && ((Character)actor).player.alliance == ((Character)gameObject).player.alliance) 
		{
			return true;
		}
		if (actor is Character && gameObject is Character && ((Character)actor).player.alliance != ((Character)gameObject).player.alliance && (((Character)actor).isStingCharged || ((Character)gameObject).isStingCharged)) 
		{
			return true;
		}
		if (actor is Character && gameObject is Character && ((Character)actor).player.alliance != ((Character)gameObject).player.alliance && (((Character)actor).insideCharacter || ((Character)gameObject).insideCharacter))
		{
			return true;
		}
		*/
		var ra = gameObject as RideArmor;
		if (actor is ShotgunIceProjSled && ra != null && (ra.character == null || ra.character.player.alliance == (actor as ShotgunIceProjSled).damager.owner.alliance)) {
			return true;
		}
		if (actor is ShotgunIceProjSled && gameObject is Projectile) {
			return true;
		}

		//Must go both ways
		if (gameObject is Actor && !otherway) {
			var otherWay = shouldTrigger((Actor)gameObject, actor, gameObjectCollider, actorCollider, intersection.times(-1), true);
			return otherWay;
		}

		return false;
	}

	public Point? getMtvDir(
		Actor actor, float inX, float inY, Point? vel,
		bool pushIncline, List<CollideData>? overrideCollideDatas = null
	) {
		Collider? terrainCollider = actor.getTerrainCollider() ?? actor.physicsCollider ?? actor.collider;

		if (terrainCollider == null) {
			return null;
		}

		List<CollideData> collideDatas;
		if (overrideCollideDatas == null) {
			collideDatas = Global.level.checkTerrainCollision(actor, inX, inY, vel);
		} else {
			collideDatas = overrideCollideDatas;
		}
		bool onlyWalls = collideDatas.Where(cd => !(cd.gameObject is Wall)).Count() == 0;
		Shape actorShape = terrainCollider.shape.clone(inX, inY);
		Point? pushDir = null;

		if (vel != null) {
			pushDir = vel?.times(-1).normalize();
			if (collideDatas.Count > 0) {
				foreach (var collideData in collideDatas) {
					if (collideData.hitData != null && collideData.hitData.normal != null &&
						((Point)collideData.hitData.normal).isAngled() && pushIncline && onlyWalls
					) {
						pushDir = new Point(0, -1);
					}
				}
			}
		}

		if (collideDatas.Count > 0) {
			float maxMag = 0;
			Point? maxMtv = null;
			foreach (CollideData collideData in collideDatas) {
				actor.registerCollision(collideData);
				int hash = GetHashCode() ^ collideData.gameObject.GetHashCode();
				if (!Global.level.collidedGObjs.Contains(hash)) {
					Global.level.collidedGObjs.Add(hash);
				};
				Point? mtv = pushDir == null ?
					actorShape.getMinTransVector(collideData.otherCollider.shape) :
					actorShape.getMinTransVectorDir(collideData.otherCollider.shape, (Point)pushDir);

				if (mtv != null && ((Point)mtv).magnitude >= maxMag) {
					maxMag = ((Point)mtv).magnitude;
					maxMtv = ((Point)mtv);
				}
			}
			return maxMtv;
		} else {
			return null;
		}
	}

	public CollideData? checkCollisionPoint(Point point, List<GameObject> exclusions) {
		var points = new List<Point>();
		points.Add(point);
		points.Add(point.addxy(1, 0));
		points.Add(point.addxy(1, 1));
		points.Add(point.addxy(0, 1));
		Shape shape = new Shape(points);
		return checkCollisionShape(shape, exclusions);
	}

	public CollideData? checkCollisionShape(Shape? shape, List<GameObject>? exclusions) {
		if (shape == null) {
			return null;
		}
		var gameObjects = getTerrainInSameCell(shape.Value);
		foreach (var go in gameObjects) {
			if (go.collider == null) continue;
			if (go is not Actor && go.collider.isTrigger) continue;
			if (go is Actor && (go.collider.isTrigger || go.collider.wallOnly)) continue;
			if (exclusions != null && exclusions.Contains(go)) continue;
			var hitData = shape.Value.intersectsShape(go.collider.shape);
			if (hitData != null) {
				return new CollideData(null, go.collider, null, false, go, hitData);
			}
		}
		return null;
	}

	public List<CollideData> checkCollisionsShape(Shape shape, List<GameObject>? exclusions) {
		var hitDatas = new List<CollideData>();
		var gameObjects = getGameObjectsInSameCell(shape);
		foreach (var go in gameObjects) {
			if (go.collider == null) continue;
			if (exclusions != null && exclusions.Contains(go)) continue;
			var hitData = shape.intersectsShape(go.collider.shape);
			if (hitData != null) {
				hitDatas.Add(new CollideData(null, go.collider, null, false, go, hitData));
			}
		}

		return hitDatas;
	}

	// Checks for collisions and returns the first one collided.
	// A collision requires at least one of the colliders not to be a trigger.
	// The vel parameter ensures we return normals that make sense, that are against the direction of vel.
	public CollideData? checkCollisionActorOnce(
		Actor? actor, float incX, float incY, Point? vel = null, bool autoVel = false, bool checkPlatforms = false
	) {
		return checkCollisionsActor(
			actor, incX, incY, vel, autoVel, returnOne: true, checkPlatforms: checkPlatforms
		).FirstOrDefault();
	}

	public List<CollideData> checkCollisionsActor(
		Actor actor, float incX, float incY, Point? vel = null, bool autoVel = false,
		bool returnOne = false, bool checkPlatforms = false
	) {
		List<CollideData> collideDatas = new List<CollideData>();
		// Use custom terrain collider by default.
		Collider? terrainCollider = actor.getTerrainCollider();
		// If terrain collider is not used or is null we use the default colliders.
		if (terrainCollider == null) {
			terrainCollider = actor.standartCollider;
		}
		if (actor.spriteToCollider.ContainsKey(actor.sprite.name) &&
			actor.spriteToCollider[actor.sprite.name] == null
		) {
			return collideDatas;
		}
		// If there is no collider we return.
		if (actor.standartCollider == null) {
			return collideDatas;
		}
		if (autoVel && vel == null) {
			vel = new Point(incX, incY);
		}
		var actorShape = actor.collider.shape.clone(incX, incY);
		var gameObjects = getGameObjectsInSameCell(actorShape);
		foreach (var go in gameObjects) {
			if (go == actor) continue;
			if (go.collider == null) continue;
			var isTrigger = shouldTrigger(actor, go, actor.collider, go.collider, new Point(incX, incY));
			if (go is Actor goActor && goActor.isPlatform && checkPlatforms) {
				isTrigger = false;
			}
			if (isTrigger) continue;
			var hitData = actorShape.intersectsShape(go.collider.shape, vel);
			if (hitData != null) {
				collideDatas.Add(new CollideData(actor.collider, go.collider, vel, isTrigger, go, hitData));
				if (returnOne) {
					return collideDatas;
				}
			}
		}

		return collideDatas;
	}

	public List<CollideData> getTriggerList(
		Actor actor, float incX, float incY, Point? vel = null, params Type[] classTypes
	) {
		var triggers = new List<CollideData>();
		var myColliders = actor.getAllColliders();
		if (myColliders.Count == 0) return triggers;

		foreach (var myCollider in myColliders) {
			var myActorShape = myCollider.shape.clone(incX, incY);
			var gameObjects = getGameObjectsInSameCell(myActorShape);
			foreach (var go in gameObjects) {
				if (go == actor) continue;
				if (classTypes.Length > 0 && !classTypes.Contains(go.GetType())) continue;
				var otherColliders = go.getAllColliders();
				if (otherColliders.Count == 0) continue;

				foreach (Collider otherCollider in otherColliders) {
					var isTrigger = shouldTrigger(actor, go, myCollider, otherCollider, new Point(incX, incY));
					if (!isTrigger) continue;
					var hitData = myActorShape.intersectsShape(otherCollider.shape, vel);
					if (hitData != null) {
						triggers.Add(new CollideData(myCollider, otherCollider, vel, isTrigger, go, hitData));
					}
				}
			}
		}

		return triggers;
	}

	public List<CollideData> getTriggerList(Shape shape, params Type[] classTypes) {
		var triggers = new List<CollideData>();
		var gameObjects = getGameObjectsInSameCell(shape);
		foreach (var go in gameObjects) {
			if (classTypes.Length > 0 && !classTypes.Contains(go.GetType())) continue;
			var otherColliders = go.getAllColliders();
			if (otherColliders.Count == 0) continue;

			foreach (var otherCollider in otherColliders) {
				var isTrigger = otherCollider.isTrigger;
				if (!isTrigger) continue;
				var hitData = shape.intersectsShape(otherCollider.shape, null);
				if (hitData != null) {
					triggers.Add(new CollideData(null, otherCollider, null, isTrigger, go, hitData));
				}
			}
		}
		return triggers;
	}

	public List<CollideData> getTerrainTriggerList(Shape shape, params Type[] classTypes) {
		var triggers = new List<CollideData>();
		var gameObjects = getTerrainInSameCell(shape);
		foreach (var go in gameObjects) {
			if (classTypes.Length > 0 && !classTypes.Contains(go.GetType())) continue;
			var otherColliders = go.getAllColliders();
			if (otherColliders.Count == 0) continue;

			foreach (var otherCollider in otherColliders) {
				var isTrigger = otherCollider.isTrigger;
				if (!isTrigger) continue;
				var hitData = shape.intersectsShape(otherCollider.shape, null);
				if (hitData != null) {
					triggers.Add(new CollideData(null, otherCollider, null, isTrigger, go, hitData));
				}
			}
		}
		return triggers;
	}


	public List<CollideData> getTerrainTriggerList(
		Actor actor, Point posIncrease, params Type[] classTypes
	) {
		List<CollideData> triggers = new();
		Collider? collider = actor.getTerrainCollider();
		if (collider == null) {
			return triggers;
		}
		Shape shape =  collider.shape.clone(posIncrease.x, posIncrease.y);
		var gameObjects = getTerrainInSameCell(shape);

		foreach (GameObject go in gameObjects) {
			if (go == actor) {
				continue;
			}
			if (classTypes.Length > 0 && !classTypes.Contains(go.GetType())) {
				continue;
			}
			var otherColliders = go.getAllColliders();
			if (otherColliders.Count == 0) {
				continue;
			}
			foreach (Collider otherCollider in otherColliders) {
				var isTrigger = shouldTrigger(actor, go, collider, otherCollider, posIncrease);
				if (!isTrigger) { continue; }
				var hitData = shape.intersectsShape(otherCollider.shape, posIncrease);
				if (hitData != null) {
					triggers.Add(new CollideData(collider, otherCollider, posIncrease, isTrigger, go, hitData));
				}
			}
		}

		return triggers;
	}

	public bool isOfClass(object go, List<Type> classNames) {
		return Helpers.isOfClass(go, classNames);
	}

	public List<CollideData> raycastAll(Point pos1, Point pos2, List<Type> classNames, bool isChargeBeam = false) {
		var hits = new List<CollideData>();
		var shape = new Shape(new List<Point>() { pos1, pos2 });
		List<GameObject> gameObjects = getTerrainInSameCell(shape);
		foreach (var go in gameObjects) {
			if (go.collider == null) continue;
			if (!isOfClass(go, classNames)) continue;
			var goCollider = go.collider;

			// Fix a one-off case where charge beam wouldn't lock onto Kaiser's head
			if (isChargeBeam && go is Character chr && chr.player.isKaiserNonViralSigma()) {
				goCollider = go.getAllColliders().FirstOrDefault(c => c.name == "head");
				if (goCollider == null) continue;
			}

			var collideDatas = goCollider.shape.getLineIntersectCollisions(new Line(pos1, pos2));

			CollideData closestCollideData = null;
			float minDist = float.MaxValue;
			foreach (var collideData in collideDatas) {
				float? dist = collideData.hitData.hitPoint?.distanceTo(pos1);
				if (dist == null) continue;
				if (dist.Value < minDist) {
					minDist = dist.Value;
					closestCollideData = collideData;
				}
			}

			if (closestCollideData != null) {
				closestCollideData.otherCollider = goCollider;
				closestCollideData.gameObject = go;
				hits.Add(closestCollideData);
			}
		}
		return hits;
	}

	public List<CollideData> raycastAllSorted(Point pos1, Point pos2, List<Type> classNames) {
		var results = raycastAll(pos1, pos2, classNames);
		results.Sort((cd1, cd2) => {
			float d1 = pos1.distanceTo(cd1.getHitPointSafe());
			float d2 = pos1.distanceTo(cd2.getHitPointSafe());
			if (d1 < d2) return -1;
			else if (d1 > d2) return 1;
			else return 0;
		});
		return results;
	}

	public CollideData raycast(Point pos1, Point pos2, List<Type> classNames) {
		var hits = raycastAll(pos1, pos2, classNames);

		float minDist = float.MaxValue;
		CollideData best = null;
		foreach (var collideData in hits) {
			float? dist = collideData.hitData.hitPoint?.distanceTo(pos1);
			if (dist == null) continue;
			if (dist.Value < minDist) {
				minDist = dist.Value;
				best = collideData;
			}
		}

		return best;
	}

	public List<Actor> getTargets(
		Point pos, int alliance, bool checkWalls,
		float? aMaxDist = null, bool isRequesterAI = false,
		bool includeAllies = false, Actor callingActor = null
	) {
		float maxDist = aMaxDist ?? Global.screenW * 0.75f;
		var targets = new List<Actor>();
		Shape shape = Rect.createFromWH(
			pos.x - Global.halfScreenW, pos.y - (Global.screenH * 0.75f),
			Global.screenW, Global.screenH).getShape();
		//DrawWrappers.DrawRectWH(
		//pos.x - Global.halfScreenW, pos.y - (Global.screenH * 0.75f),
		//Global.screenW, Global.screenH, true, new Color(0, 0, 255, 128), 1, ZIndex.HUD
		//);
		var hits = Global.level.checkCollisionsShape(shape, null);
		foreach (var hit in hits) {
			var damagable = hit.gameObject as IDamagable;
			Actor actor = damagable?.actor();
			if (actor == null) continue;
			if (actor.pos.distanceTo(pos) > maxDist) continue;
			if (checkWalls && !noWallsInBetween(pos, actor.getCenterPos())) continue;
			if (actor == callingActor) continue;

			if (hit.gameObject is Character character) {
				if (character.player.isDead) continue;
				if (!includeAllies && character.player.alliance == alliance) continue;
				if (character.player.alliance != alliance &&
					character.player.isDisguisedAxl && gameMode.isTeamMode
				) {
					if (!isRequesterAI) continue;
					else if (!character.disguiseCoverBlown) continue;
				}
				if (character.isStealthy(alliance)) continue;
			}

			if (!includeAllies && !damagable.canBeDamaged(alliance, null, null)) continue;

			targets.Add(actor);
		}

		targets = targets.OrderBy(actor => {
			return actor.pos.distanceTo(pos);
		}).ToList();

		return targets;
	}

	public Actor? getClosestTarget(
		Point pos, int alliance, bool checkWalls,
		float? aMaxDist = null, bool isRequesterAI = false,
		bool includeAllies = false, Actor callingActor = null
	) {
		List<Type> filters = new List<Type>() {
			typeof(Character), typeof(Maverick),
			typeof(RaySplasher), typeof(Mechaniloid)
		};
		var targets = getTargets(
			pos, alliance, checkWalls, aMaxDist,
			isRequesterAI, includeAllies, callingActor
		);
		for (int i = 0; i < targets.Count; i++) {
			if (isOfClass(targets[i], filters)) {
				return targets[i];
			}
		}
		return null;
	}

	public bool noWallsInBetween(Point pos1, Point pos2) {
		var hits = raycastAll(pos1, pos2, new List<Type>() { typeof(Wall) });
		if (hits.Count == 0) {
			return true;
		}
		return false;
	}

	public void addToGrid(GameObject obj) {
		if (obj.useActorGrid) {
			addToActorGrid(obj);
		}
		if (obj.useTerrainGrid) {
			addTerrainToGrid(obj);
		}
	}

	public void removeFromGrid(GameObject obj) {
		if (obj.useActorGrid) {
			removeFromActorGrid(obj);
		}
		if (obj.useTerrainGrid) {
			removeFromTerrainGrid(obj);
		}
	}

	private void addTerrainToGrid(GameObject go) {
		if (!gameObjects.Contains(go)) {
			return;
		}
		if (terrainGridsPopulatedByGo.ContainsKey(go.GetHashCode())) {
			removeFromTerrainGrid(go);
		}
		Shape? allCollidersShape = go.getAllCollidersShape();
		if (!allCollidersShape.HasValue) {
			return;
		}
		foreach (Cell cell in getGridCells(allCollidersShape.Value)) {
			if (!terrainGrid[cell.x, cell.y].Contains(go)) {
				terrainGrid[cell.x, cell.y].Add(go);
			}
		}
		terrainGridsPopulatedByGo[go.GetHashCode()] = getGridCellsPos(allCollidersShape.Value);
	}

	private void removeFromTerrainGrid(GameObject go) {
		int hash = go.GetHashCode();
		if (!terrainGridsPopulatedByGo.ContainsKey(hash)) {
			return;
		}
		Rect dataPos = terrainGridsPopulatedByGo[hash];
		for (int x = (int)dataPos.x1; x <= dataPos.x2; x++) {
			for (int y = (int)dataPos.y1; y <= (int)dataPos.y2; y++) {
				terrainGrid[x, y].Remove(go);
				/*if (terrainGrid[x, y].Count == 0) {
					populatedTerrainGrids.Remove((x, y));
				}*/
			}
		}
		terrainGridsPopulatedByGo.Remove(hash);
	}

	public CollideData? checkTerrainCollisionOnce(
		Actor actor, float incX, float incY, Point? vel = null, bool autoVel = false,
		bool checkPlatforms = false
	) {
		return checkTerrainCollision(actor, incX, incY, vel, autoVel, true, checkPlatforms).FirstOrDefault();
	}

	public List<CollideData> checkTerrainCollision(
		Actor actor, float incX, float incY, Point? vel = null, bool autoVel = false,
		bool returnOne = false, bool checkPlatforms = false
	) {
		List<CollideData> collideDatas = new List<CollideData>();
		// Use custom terrain collider by default.
		Collider? terrainCollider = actor.getTerrainCollider() ?? actor.physicsCollider ?? actor.collider;
		// If there is no collider we return.
		if (terrainCollider == null) {
			return collideDatas;
		}
		if (autoVel && vel == null) {
			vel = new Point(incX, incY);
		}
		Shape actorShape = terrainCollider.shape.clone(incX, incY);
		List<GameObject> gameObjects = getTerrainInSameCell(actorShape);
		foreach (GameObject go in gameObjects) {
			if (go == actor) continue;
			if (go.collider == null) continue;
			bool isTrigger = shouldTrigger(actor, go, terrainCollider, go.collider, new Point(incX, incY));
			if (go is Actor goActor && goActor.isPlatform && checkPlatforms) {
				isTrigger = false;
			}
			if (isTrigger) continue;
			HitData? hitData = actorShape.intersectsShape(go.collider.shape, vel);
			if (hitData != null) {
				collideDatas.Add(new CollideData(terrainCollider, go.collider, vel, isTrigger, go, hitData));
				if (returnOne) {
					return collideDatas;
				}
			}
		}

		return collideDatas;
	}

	public List<GameObject> getTerrainInSameCell(Shape shape) {
		List<Cell> cells = getTerrainCells(shape);
		List<GameObject> retGameobjects = new();
		HashSet<GameObject> gameobjectsChecked = new();
		foreach (Cell cell in cells) {
			if (cell.gameobjects == null) continue;
			foreach (GameObject go in cell.gameobjects) {
				if (!gameobjectsChecked.Contains(go)) {
					gameobjectsChecked.Add(go);
					retGameobjects.Add(go);
				}
			}
		}
		return retGameobjects;
	}

	//Optimize this function, it will be called a lot
	public List<Cell> getTerrainCells(Shape shape) {
		var cells = new List<Cell>();
		int startX = MathInt.Floor(shape.minX);
		int endX = MathInt.Floor(shape.minX);
		int startY = MathInt.Ceiling(shape.maxY);
		int endY = MathInt.Ceiling(shape.maxY);

		//Line case
		if (shape.points.Count == 2) {
			var point1 = shape.points[0];
			var point2 = shape.points[1];
			var dir = point1.directionToNorm(point2);
			var curX = point1.x;
			var curY = point1.y;
			float dist = 0;
			var maxDist = point1.distanceTo(point2);
			//var mag = maxDist / (this.cellWidth/2);
			float mag = cellWidth / 2;
			HashSet<int> usedCoords = new HashSet<int>();
			while (dist <= maxDist) {
				int y = MathInt.Floor(curY / cellWidth);
				int x = MathInt.Floor(curX / cellWidth);
				curX += dir.x * mag;
				curY += dir.y * mag;
				dist += mag;
				if (y < 0 || x < 0 || y >= terrainGrid.GetLength(1) || x >= terrainGrid.GetLength(0)) continue;
				int gridCoordKey = Helpers.getGridCoordKey((ushort)x, (ushort)y);
				if (usedCoords.Contains(gridCoordKey)) continue;
				usedCoords.Add(gridCoordKey);
				cells.Add(new Cell(x, y, terrainGrid[x, y]));
			}
			return cells;
		}

		int minY = MathInt.Floor(shape.minY / cellWidth);
		int minX = MathInt.Floor(shape.minX / cellWidth);
		int maxY = MathInt.Floor(shape.maxY / cellWidth);
		int maxX = MathInt.Floor(shape.maxX / cellWidth);

		minX = Math.Clamp(minX, 0, terrainGrid.GetLength(0) - 1);
		maxX = Math.Clamp(maxX, 0, terrainGrid.GetLength(0) - 1);
		minY = Math.Clamp(minY, 0, terrainGrid.GetLength(1) - 1);
		maxY = Math.Clamp(maxY, 0, terrainGrid.GetLength(1) - 1);

		for (int x = minX; x <= maxX; x++) {
			for (int y = minY; y <= maxY; y++) {
				cells.Add(new Cell(x, y, terrainGrid[x, y]));
			}
		}
		return cells;
	}

	public bool checkLossyCollision(GameObject first, GameObject c) {
		Shape? myColliderShape = first.getAllCollidersShape();
		Shape? otherColliderShape = first.getAllCollidersShape();

		if (myColliderShape == null || otherColliderShape == null) {
			return false;
		}

		if (myColliderShape.Value.minX > otherColliderShape.Value.maxX ||
			myColliderShape.Value.maxX < otherColliderShape.Value.minX ||
			myColliderShape.Value.minY > otherColliderShape.Value.maxY ||
			myColliderShape.Value.maxY < otherColliderShape.Value.minY			
		) {
			return false;
		}
		return true;
	}

	public (List<CollideData>, List<CollideData>) getTriggerExact(GameObject firstObj, GameObject secondObj) {
		List<CollideData> triggers1 = new();
		List<CollideData> triggers2 = new();
		List<Collider> collidersOne = firstObj.getAllColliders();
		List<Collider> collidersTwo = secondObj.getAllColliders();
		Actor? firstActor = firstObj as Actor;
		Actor? secondActor = secondObj as Actor;
		if (collidersOne.Count == 0 || collidersTwo.Count == 0) {
			return (triggers1, triggers2);
		}
		foreach (Collider collider1 in collidersOne) {
			foreach (Collider collider2 in collidersTwo) {
				bool isTrigger1 = true;
				if (firstActor != null) {
					isTrigger1 = shouldTrigger(firstActor, secondObj, collider1, collider2, new Point(0, 0));
				}
				bool isTrigger2 = true;
				if (secondActor != null) {
					isTrigger2 = shouldTrigger(secondActor, secondObj, collider1, collider2, new Point(0, 0));
				}
				if (!isTrigger1 || !isTrigger2) {
					continue;
				}
				HitData? hitData = collider1.shape.intersectsShape(collider2.shape);
				if (hitData != null) {
					triggers1.Add(new CollideData(collider1, collider2, null, isTrigger1, secondObj, hitData));
					triggers2.Add(new CollideData(collider2, collider1, null, isTrigger2, firstObj, hitData));
				}
			}
		}

		return (triggers1, triggers2);;
	}

	public (CollideData?, CollideData?) getTriggerTerrain(Actor actor, Geometry geometry) {
		CollideData? triggerActor = null;
		CollideData? triggerTerrain = null;
		Collider? actorCollider = actor.getTerrainCollider() ?? actor.physicsCollider ?? actor.collider;
		if (actorCollider == null) {
			return (triggerActor, triggerTerrain);
		}
		Collider geometryCollider = geometry.collider;
		if (geometryCollider == null) {
			return (triggerActor, triggerTerrain);
		}
		bool isTrigger = shouldTrigger(actor, geometry, actorCollider, geometryCollider, new Point(0, 0));
		HitData? hitData = actorCollider.shape.intersectsShape(geometryCollider.shape);
		if (hitData != null) {
			triggerActor = new CollideData(actorCollider, geometryCollider, null, isTrigger, geometry, hitData);
			triggerTerrain = new CollideData(geometryCollider, actorCollider, null, isTrigger, actor, hitData);
		}
		return (triggerActor, triggerTerrain);
	}

	public List<CollideData> organizeTriggers(List<CollideData> triggerList) {
		// Prioritize certain colliders over others, running them first
		return triggerList.OrderBy(trigger => {
			if (trigger.gameObject is GenericMeleeProj && trigger.otherCollider.flag == (int)HitboxFlag.None &&
				(trigger.otherCollider.originalSprite == "sigma_block" || trigger.otherCollider.originalSprite == "zero_block")) {
				return 0;
			} else if (trigger.otherCollider.originalSprite.StartsWith("kaisersigma") == true && trigger.otherCollider.name == "head") {
				return 0;
			} else if (trigger.gameObject is GenericMeleeProj && trigger.otherCollider.flag == (int)HitboxFlag.None && trigger.otherCollider.originalSprite == "drdoppler_absorb") {
				return 0;
			}
			return 1;
		}).ToList();
	}
}
