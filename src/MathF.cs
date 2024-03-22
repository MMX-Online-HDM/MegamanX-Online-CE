using System;
namespace MMXOnline;

public class MathInt {
	public static int Round(float num) {
		return (int)MathF.Round(num);
	}

	public static int Clamp(int val, int min, int max) {
		if (val < min) val = min;
		if (val > max) val = max;
		return val;
	}

	public static int Ceiling(float num) {
		return (int)MathF.Ceiling(num);
	}

	public static int Floor(float num) {
		return (int)MathF.Floor(num);
	}

	public static int Round(decimal num) {
		return (int)Math.Round(num);
	}

	public static int Ceiling(decimal num) {
		return (int)Math.Ceiling(num);
	}

	public static int Floor(decimal num) {
		return (int)Math.Floor(num);
	}
	
	public static int Ceiling(double num) {
		return (int)Math.Ceiling(num);
	}

	public static int Floor(double num) {
		return (int)Math.Floor(num);
	}
}
