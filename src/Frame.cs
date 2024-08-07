using System.Collections.Generic;

namespace MMXOnline;

public class Frame {
	public Rect rect;
	public float duration;
	public Point offset;
	public Collider[] hitboxes;
	public Point[] POIs;
	public string[] POITags;
	public Point? headPos;

	public Frame(Rect rect, float duration, Point offset) {
		this.rect = rect;
		this.duration = duration;
		this.offset = offset;
		hitboxes = new Collider[0];
		POIs = new Point[0];
		POITags = new string[0];
	}

	public Point? getBusterOffset() {
		if (POIs.Length > 0)
			return POIs[0];
		return null;
	}

	public Frame clone() {
		var clonedFrame = (Frame)MemberwiseClone();
		clonedFrame.hitboxes = new Collider[hitboxes.Length];
		for (int i = 0; i < hitboxes.Length; i++){
			clonedFrame.hitboxes[i] = hitboxes[i].clone();
		}
		return clonedFrame;
	}
}
