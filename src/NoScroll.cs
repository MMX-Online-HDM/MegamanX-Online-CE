using System.Collections.Generic;

namespace MMXOnline;

public enum ScrollFreeDir {
	None,
	Up,
	Down,
	Left,
	Right
}

public class NoScroll {
	public Shape shape;
	public ScrollFreeDir freeDir;
	public bool snap;
	public string name;
	public NoScroll(string name, Shape shape, ScrollFreeDir dir, bool snap) {
		this.name = name;
		this.shape = shape;
		freeDir = dir;
		this.snap = snap;
	}
}

public class CameraZone : Geometry {
	public CameraZone(string name, List<Point> points) : base(name, points) {
	}
}
