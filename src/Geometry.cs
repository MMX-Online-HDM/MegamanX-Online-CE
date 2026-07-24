using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

//Umbrella class for walls, nav meshes, ladders, etc.
public class Geometry : GameObject {
	public bool destroyed;
	public bool iDestroyed => destroyed;
	public bool disabled;
	public bool iDisabled => disabled;
	public string name { get; set; }
	public Collider collider { get; set; }
	public float speedMul { get; set; } = 1;
	public bool useTerrainGrid { get; set; } = true;
	public bool useActorGrid { get; set; } = false;
	public bool hasStateMachine { get; set; } = false;

	public Geometry(string name, List<Point> points) {
		this.name = name;
		collider = new Collider(points, false, null, true, true, 0, new Point(0, 0));
	}

	public virtual void preUpdate() {}
	public virtual void update() {}
	public virtual void physicsUpdate() {}
	public virtual void statePreUpdate() { }
	public virtual void stateUpdate() { }
	public virtual void statePostUpdate() { }
	public virtual void netUpdate() { }
	public void postUpdate() { }
	public virtual void onCollision(CollideData other) { }
	public void onStart() { }

	public virtual void render(float x, float y) {
		if (Global.showHitboxes && this is Wall) {
			List<Point> points = collider.shape.clone(x, y).points;
			if (points.Count == 4 && collider.shape.isRect()) {
				Rect rect = collider.shape.clone(x, y).getRect();
				rect.x1 = MathF.Floor(rect.x1) + 1;
				rect.y1 = MathF.Floor(rect.y1) + 1;
				rect.x2 = MathF.Ceiling(rect.x2) - 1;
				rect.y2 = MathF.Ceiling(rect.y2) - 1;

				DrawWrappers.DrawRect(
					rect.x1, rect.y1, rect.x2, rect.y2, true,
					new Color(178, 0, 216, 75), 1, ZIndex.HUD, true,
					new Color(178, 0, 216, 200)
				);
			} else {
				DrawWrappers.DrawPolygon(
					points, new Color(50, 50, 255, 125),
					true, ZIndex.HUD + 100, true
				);
			}
		}
		else if (Global.showAIDebug && this is JumpZone) {
			DrawWrappers.DrawPolygon(
				collider.shape.clone(x, y).points, new Color(0, 0, 255, 50),
				true, ZIndex.HUD + 100, true
			);
		}
		else if (Global.showHitboxes && this is not Wall and not BackwallZone and not JumpZone) {
			List<Point> points = collider.shape.clone(x, y).points;
			if (points.Count == 4) {
				points[0] = new Point(MathF.Floor(points[0].x + 1), MathF.Floor(points[0].y + 1));
				points[1] = new Point(MathF.Ceiling(points[1].x - 1), MathF.Floor(points[1].y + 1));
				points[2] = new Point(MathF.Ceiling(points[2].x - 1), MathF.Ceiling(points[2].y - 1));
				points[3] = new Point(MathF.Floor(points[3].x + 1), MathF.Ceiling(points[3].y - 1));

				DrawWrappers.DrawPolygon(
					points, new Color(255, 150, 0, 50),
					true, ZIndex.HUD + 100, true,
					new Color(255, 100, 0, 200)
				);
			} else {
				DrawWrappers.DrawPolygon(
					points, new Color(255, 100, 0, 125),
					true, ZIndex.HUD + 100, true
				);
			}
		}
	}

	public List<Collider> getAllColliders() {
		if (collider != null) {
			return new List<Collider> { collider };
		}
		return new List<Collider>();
	}

	public Shape? getAllCollidersShape() {
		return collider?.shape;
	}

	public void registerCollision(CollideData collideData) {
		onCollision(collideData);
	}

	public virtual void destroySelf() {
		if (!destroyed) {
			Global.level.removeGameObject(this);
			destroyed = true;
		}
	}
}
