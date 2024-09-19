using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

//Umbrella class for walls, nav meshes, ladders, etc.
public class Geometry : GameObject {
	public string name { get; set; }
	public Collider collider { get; set; }
	public float localSpeedMul { get; set; } = 1;
	public bool useTerrainGrid { get; set; } = true;
	public bool useActorGrid { get; set; } = false;

	public Geometry(string name, List<Point> points) {
		this.name = name;
		collider = new Collider(points, false, null, true, true, 0, new Point(0, 0));
	}

	public virtual void preUpdate() {

	}

	public virtual void update() {

	}

	public virtual void statePreUpdate() { }
	public virtual void stateUpdate() { }
	public virtual void statePostUpdate() { }

	public virtual void netUpdate() {

	}

	public virtual void render(float x, float y) {
		if (Global.showHitboxes && this is Wall) {
			List<Point> points = collider.shape.clone(x, y).points;
			if (points.Count == 4) {
				points[0] = points[0].addxy(1, 1);
				points[1] = points[1].addxy(-1, 1);
				points[2] = points[2].addxy(-1, -1);
				points[3] = points[3].addxy(1, -1);

				DrawWrappers.DrawPolygon(
					points, new Color(178, 0, 216, 100),
					true, ZIndex.HUD + 100, true,
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
		else if (Global.showHitboxes && this is not BackwallZone and not JumpZone) {
			List<Point> points = collider.shape.clone(x, y).points;
			if (points.Count == 4) {
				points[0] = points[0].addxy(1, 1);
				points[1] = points[1].addxy(-1, 1);
				points[2] = points[2].addxy(-1, -1);
				points[3] = points[3].addxy(1, -1);

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

	public virtual void onCollision(CollideData other) {

	}

	public void onStart() {
	}

	public void postUpdate() {
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
}
