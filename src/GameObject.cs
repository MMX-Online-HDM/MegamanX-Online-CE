using System.Collections.Generic;

namespace MMXOnline;

public interface GameObject {
	string name { get; set; }
	void onStart();
	void preUpdate();
	void update();
	void postUpdate();
	void render(float x, float y);
	Collider? collider { get; }
	List<Collider> getAllColliders();
	Shape? getAllCollidersShape();
	void onCollision(CollideData other);
	void netUpdate();
	void statePreUpdate();
	void stateUpdate();
	void statePostUpdate();
}

