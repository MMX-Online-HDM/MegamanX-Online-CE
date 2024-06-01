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

	public static int Floor(int num) {
		return (int)MathF.Floor(num);
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

	public static float Sawtooth(float arg) {
    	return MathF.Abs(((arg * 2) % 4) - 2) - 1;
	}

	public static float SquareSaw(float arg) {
		float ret = Sawtooth(arg);
		if (ret < 0.5f) {
			return ret * 2f;
		}
		return MathF.Sign(ret);
	}

	public static float SquareSinB(float arg) {
		return SquareSaw(arg / 128f);
	}

	public static float SquareCosB(float arg) {
		return SquareSaw((arg + 64f) / 128f);
	}
}
