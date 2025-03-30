using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class Sprite {
	public AnimData animData;
	public string name;
	public Collider[] hitboxes;
	public Collider[][] frameHitboxes;

	public static Texture[] xArmorBootsBitmap = new Texture[4];
	public static Texture[] xArmorBodyBitmap = new Texture[4];
	public static Texture[] xArmorHelmetBitmap = new Texture[4];
	public static Texture[] xArmorArmBitmap = new Texture[4];
	public static Texture[] xSaberBitmap = new Texture[2];
	public static Texture axlArmBitmap = null!;

	public float time;
	public int frameIndex;
	public float frameSpeed = 1;
	public int loopStartFrame;
	public bool doesLoop;

	public Texture? overrideTexture;

	public float frameSeconds {
		get => frameTime * Global.secondsFrameDuration;
		set => frameTime = value / Global.secondsFrameDuration;
	}
	public float animSeconds {
		get => animTime * Global.secondsFrameDuration;
		set => animTime = value / Global.secondsFrameDuration;
	}
	public float frameTime;
	public float animTime;

	public int loopCount = 0;
	public bool visible = true;
	public bool reversed;

	public float tempOffY = 0;
	public float tempOffX = 0;

	public int totalFrameNum => (animData.frames.Length);

	public Sprite(string spritename) {
		animData = Global.sprites[spritename];
		name = animData.name;
		doesLoop = animData.loop;
		loopStartFrame = animData.loopStartFrame;

		hitboxes = new Collider[animData.hitboxes.Length];
		for (int i = 0; i < hitboxes.Length; i++) {
			hitboxes[i] = animData.hitboxes[i].clone();
		}
		frameHitboxes = new Collider[animData.frames.Length][];
		for (int i = 0; i < animData.frames.Length; i++) {
			int length = animData.frames[i].hitboxes.Length;
			frameHitboxes[i] = new Collider[length];
			for (int j = 0; j < length; j++) {
				frameHitboxes[i][j] = animData.frames[i].hitboxes[j].clone();
			}
		}
	}

	public bool update() {
		frameTime += Global.speedMul * frameSpeed;
		animTime += Global.speedMul * frameSpeed;
		time += Global.spf;
		Frame? currentFrame = getCurrentFrame();
		if (currentFrame != null && frameTime >= currentFrame.duration) {
			bool onceEnd = !animData.loop && frameIndex == animData.frames.Length - 1;
			if (!onceEnd) {
				frameTime = 0;
				frameIndex++;
				if (frameIndex >= animData.frames.Length) {
					frameIndex = animData.loopStartFrame;
					animTime = 0;
					loopCount++;
				}
				return true;
			}
		}
		return false;
	}

	public void restart() {
		frameIndex = 0;
		frameTime = 0;
		animTime = 0;
		loopCount = 0;
	}

	public Point getAlignOffset(int frameIndex, int flipX, int flipY) {
		Frame frame = animData.frames[frameIndex];
		Rect rect = frame.rect;
		Point offset = frame.offset;
		return getAlignOffsetHelper(rect, offset, flipX, flipY);
	}

	// Draws a sprite immediately in screen coords.
	// Good for HUD sprites whose z-index must be more fine grain controlled
	public void drawToHUD(int frameIndex, float x, float y, float alpha = 1) {
		if (!animData.frames.InRange(frameIndex)) {
			return;
		}
		Frame currentFrame = animData.frames[frameIndex];
		frameTime = 0;

		float cx = animData.baseAlignmentX;
		float cy = animData.baseAlignmentY;

		cx = cx * currentFrame.rect.w();
		cy = cy * currentFrame.rect.h();

		cx += animData.alignOffX - tempOffX - currentFrame.offset.x;
		cy += animData.alignOffY - tempOffY - currentFrame.offset.y;

		cx = MathF.Floor(cx);
		cy = MathF.Floor(cy);

		DrawWrappers.DrawTextureHUD(
			animData.bitmap,
			currentFrame.rect.x1, currentFrame.rect.y1,
			currentFrame.rect.w(), currentFrame.rect.h(),
			x - cx, y - cy,
			alpha
		);
	}

	public List<Trail> lastFiveTrailDraws = new List<Trail>();
	public List<Trail> lastTwoBkTrailDraws = new List<Trail>();

	
	public void drawSimple(Point pos, int xDir, long zIndex, float alpha = 1, Actor? actor = null) {
		draw(
			frameIndex, pos.x, pos.y, xDir, 1,
			null, alpha, 1, 1, zIndex,
			null, 0, actor: actor, useFrameOffsets: true
		);
	}

	public void draw(
		int frameIndex, float x, float y, int flipX, int flipY,
		HashSet<RenderEffectType>? renderEffects, float alpha,
		float scaleX, float scaleY, long zIndex,
		List<ShaderWrapper>? shaders = null,
		float angle = 0,
		Actor? actor = null,
		bool useFrameOffsets = false
	) {
		if (!visible) return;
		if (actor != null) {
			if (!actor.shouldDraw()) return;
		}

		// Character-specific draw section
		int[]? armors = null;
		bool drawXSaber = false;
		bool drawAxlArms = true;
		bool hyperBusterReady = false;
		bool isUPX = false;
		bool isUltX = false;
		Character? character = actor as Character;
		if (character != null) {
			if (character is MegamanX mmx) {
				armors = new int[] {
					(int)mmx.legArmor,
					(int)mmx.chestArmor,
					(int)mmx.helmetArmor,
					(int)mmx.armArmor
				};
				drawXSaber = !mmx.hasAnyArmor && mmx.specialButtonMode == 1 || mmx.hasFullHyperMaxArmor;
			}
			if (character.flattenedTime > 0) {
				scaleY = 0.5f;
			}
			if (character.player.isAxl && character.player.axlWeapon != null) {
				drawAxlArms = !character.player.axlWeapon.isTwoHanded(true);
			}
			isUPX = character is RagingChargeX;
			isUltX = character is MegamanX { hasUltimateArmor: true };
		}

		if (name == "mmx_unpo_grab" || name == "mmx_unpo_grab2") zIndex = ZIndex.MainPlayer;

		Frame currentFrame = getCurrentFrame(frameIndex);
		if (currentFrame == null) return;

		float cx = animData.baseAlignmentX;
		float cy = animData.baseAlignmentY;

		cx = cx * currentFrame.rect.w();
		cy = cy * currentFrame.rect.h();

		cx += animData.alignOffX - tempOffX;
		cy += animData.alignOffY - tempOffY;

		cx = MathF.Floor(cx);
		cy = MathF.Floor(cy);

		if (scaleY == -1 && (
				actor is MagnaCentipede ms ||
				name.Contains("magnac_teleport") ||
				name.Contains("magnac_notail_teleport")
			)
		) {
			cy -= MagnaCentipede.constHeight;
		}

		float frameOffsetX = 0;
		float frameOffsetY = 0;

		if (useFrameOffsets) {
			frameOffsetX = currentFrame.offset.x * flipX;
			frameOffsetY = currentFrame.offset.y * flipY;
		}

		if (shaders == null) shaders = new List<ShaderWrapper>();

		if (renderEffects != null) {
			ShaderWrapper? shader = null;
			if (renderEffects.Contains(RenderEffectType.Hit)) {
				shader = Global.shaderWrappers.GetValueOrDefault("hit");
				if (shaders.Count > 1) shaders.RemoveAt(1);
			} else if (renderEffects.Contains(RenderEffectType.Flash)) {
				shader = Global.shaderWrappers.GetValueOrDefault("flash");
			} else if (renderEffects.Contains(RenderEffectType.InvisibleFlash) && alpha == 1) {
				shader = Global.shaderWrappers.GetValueOrDefault("invisible");
				shader?.SetUniform("alpha", 0.5f - (MathF.Sin(Global.level.time * 5) * 0.25f));
			} else if (renderEffects.Contains(RenderEffectType.ChargeGreen)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargeGreen");
			} else if (renderEffects.Contains(RenderEffectType.ChargeOrange)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargeOrange");
			} else if (renderEffects.Contains(RenderEffectType.ChargePink)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargePink");
			} else if (renderEffects.Contains(RenderEffectType.ChargeYellow)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargeYellow");
			} else if (renderEffects.Contains(RenderEffectType.ChargeBlue)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargeBlue");
			} else if (renderEffects.Contains(RenderEffectType.StealthModeBlue)) {
				shader = Global.shaderWrappers.GetValueOrDefault("stealthmode_blue");
			} else if (renderEffects.Contains(RenderEffectType.StealthModeRed)) {
				shader = Global.shaderWrappers.GetValueOrDefault("stealthmode_red");
			}
			if (shader != null) {
				shaders.Add(shader);
			}

			if (renderEffects.Contains(RenderEffectType.Shake)) {
				frameOffsetX += Helpers.randomRange(-1, 1);
				frameOffsetY += Helpers.randomRange(-1, 1);
			}
		}

		float xDirArg = flipX * scaleX;
		float yDirArg = flipY * scaleY;

		Texture bitmap = animData.bitmap;

		bool isCompositeSprite = false;
		List<Texture> compositeBitmaps = new();
		float extraY = 0;
		float extraYOff = 0;
		float extraW = 0;
		float flippedExtraW = 0;
		float extraXOff = 0;

		if (isUltX) {
			extraYOff = 3;
			extraY = 3;
			armors[0] = 4;
			armors[1] = 4;
			armors[2] = 4;
			armors[3] = 4;
		}

		if (isUPX) {
			bitmap = Global.textures["XUP"];
			drawXSaber = true;
		}

		if (armors != null && animData.isXSprite) {
			bool isShootSprite = needsX3BusterCorrection();
			/*
			if (isShootSprite) {
				if (name.Contains("mmx_wall_slide_shoot")) {
					flippedExtraW = 5;
					extraW = flippedExtraW;
					extraXOff = -flippedExtraW * flipX;
				} else {
					extraW = 5;
				}
			} */
			if (armors[2] == 2) {
				extraYOff = 0;
				extraY = 2;
			}
			var x3ArmShaders = new List<ShaderWrapper>(shaders);
			if (hyperBusterReady) {
				if (Global.isOnFrameCycle(5)) {
					if (Global.shaderWrappers.ContainsKey("hit")) {
						x3ArmShaders.Add(Global.shaderWrappers["hit"]);
					}
				}
			}

			compositeBitmaps.Add(bitmap);
			if (armors[2] > 0) {
				compositeBitmaps.Add(xArmorHelmetBitmap[armors[2] - 1]);
			}
			if (armors[0] > 0) {
				compositeBitmaps.Add(xArmorBootsBitmap[armors[0] - 1]);
			}
			if (armors[1] > 0) {
				compositeBitmaps.Add(xArmorBodyBitmap[armors[1] - 1]);
			}
			if (armors[3] > 0) {
				compositeBitmaps.Add(xArmorArmBitmap[armors[3] - 1]);
			}
			if (compositeBitmaps.Count > 1) {
				isCompositeSprite = true;
			}
		}

		if ((armors != null || drawXSaber) && animData.isXSprite) {
			bool isShootSprite = needsX3BusterCorrection();
			/*
			if (isShootSprite) {
				if (name.Contains("mmx_wall_slide_shoot")) {
					flippedExtraW = 5;
					extraW = flippedExtraW;
					extraXOff = -flippedExtraW * flipX;
				} else {
					extraW = 5;
				}
			} */
			if (armors[2] == 2 || drawXSaber) {
				extraYOff = 0;
				extraY = 2;
			}
			if (drawXSaber) {
				flippedExtraW += 1;
				extraXOff = 1 * flipX;
			}
			var x3ArmShaders = new List<ShaderWrapper>(shaders);
			if (hyperBusterReady) {
				if (Global.isOnFrameCycle(5)) {
					if (Global.shaderWrappers.ContainsKey("hit")) {
						x3ArmShaders.Add(Global.shaderWrappers["hit"]);
					}
				}
			}
			if (drawXSaber) {
				compositeBitmaps.Add(xSaberBitmap[0]);
			}
			compositeBitmaps.Add(bitmap);
			if (armors[2] > 0) {
				compositeBitmaps.Add(xArmorHelmetBitmap[armors[2] - 1]);
			}
			if (armors[0] > 0) {
				compositeBitmaps.Add(xArmorBootsBitmap[armors[0] - 1]);
			}
			if (armors[1] > 0) {
				compositeBitmaps.Add(xArmorBodyBitmap[armors[1] - 1]);
			}
			if (armors[3] > 0) {
				compositeBitmaps.Add(xArmorArmBitmap[armors[3] - 1]);
			}
			if (drawXSaber) {
				compositeBitmaps.Add(xSaberBitmap[1]);
			}
			if (compositeBitmaps.Count > 1) {
				isCompositeSprite = true;
			}
		}

		if (renderEffects != null && !renderEffects.Contains(RenderEffectType.Invisible)) {
			if (alpha >= 1 && (
				renderEffects.Contains(RenderEffectType.BlueShadow) ||
				renderEffects.Contains(RenderEffectType.RedShadow) ||
				renderEffects.Contains(RenderEffectType.GreenShadow)
			)) {
				ShaderWrapper? outlineShader = null;
				if (renderEffects.Contains(RenderEffectType.BlueShadow)) {
					outlineShader = Helpers.cloneShaderSafe("outline_blue");
				} else if (renderEffects.Contains(RenderEffectType.RedShadow)) {
					outlineShader = Helpers.cloneShaderSafe("outline_red");
				} else if (renderEffects.Contains(RenderEffectType.GreenShadow)) {
					outlineShader = Helpers.cloneShaderSafe("outline_green");
				}
				if (outlineShader != null) {
					outlineShader.SetUniform(
						"textureSize",
						new SFML.Graphics.Glsl.Vec2(currentFrame.rect.w() + 2, currentFrame.rect.h() + 2)
					);
					DrawWrappers.DrawTexture(
						bitmap,
						currentFrame.rect.x1 - 1, currentFrame.rect.y1 - 1,
						currentFrame.rect.w() + 2, currentFrame.rect.h() + 2,
						x, y, zIndex - 10,
						cx - (frameOffsetX - (1 * xDirArg)) * xDirArg,
						cy - (frameOffsetY - (1 * yDirArg)) * yDirArg,
						xDirArg, yDirArg, angle, alpha,
						[outlineShader], true
					);
					if (animData.isAxlSprite && drawAxlArms) {
						DrawWrappers.DrawTexture(
							axlArmBitmap,
							currentFrame.rect.x1 - 1, currentFrame.rect.y1 - 1,
							currentFrame.rect.w() + 2, currentFrame.rect.h() + 2,
							x, y, zIndex - 10,
							cx - (frameOffsetX - (1 * xDirArg)) * xDirArg,
							cy - (frameOffsetY - (1 * yDirArg)) * yDirArg,
							xDirArg, yDirArg, angle, alpha,
							[outlineShader], true
						);
					}
				}
			}
		
			if (name is "boomerk_dash" or "boomerk_bald_dash" && (animTime > 0 || frameIndex > 0)) {
				if (Global.isOnFrameCycle(4)) {
					var trail = lastTwoBkTrailDraws.ElementAtOrDefault(5);
					if (trail != null) {
						trail.action.Invoke(trail.time);
						trail.time -= Global.spf;
					}
				} else {
					var trail = lastTwoBkTrailDraws.ElementAtOrDefault(9);
					if (trail != null) {
						trail.action.Invoke(trail.time);
						trail.time -= Global.spf;
					}
				}

				var shaderList = new List<ShaderWrapper>();
				if (Global.shaderWrappers.ContainsKey("boomerkTrail")) {
					ShaderWrapper boomerkTrail = Global.shaderWrappers["boomerkTrail"];
					boomerkTrail.SetUniform("paletteTexture", Global.textures["boomerkTrailPalette"]);
					shaderList.Add(boomerkTrail);
				}

				if (lastTwoBkTrailDraws.Count > 10) {
					lastTwoBkTrailDraws.PopFirst();
				}
				lastTwoBkTrailDraws.Add(new Trail() {
					action = (float time) => {
						DrawWrappers.DrawTexture(
							bitmap,
							animData.frames[1].rect.x1,
							animData.frames[1].rect.y1,
							animData.frames[1].rect.w(),
							animData.frames[1].rect.h(),
							x, y, zIndex,
							cx - frameOffsetX * xDirArg,
							cy - frameOffsetY * yDirArg,
							xDirArg, yDirArg,
							angle, alpha,
							shaderList, true
						);
					},
					time = 0.25f
				});
			} else {
				lastTwoBkTrailDraws.Clear();
			}

			if (renderEffects.Contains(RenderEffectType.Trail)) {
				for (int i = lastFiveTrailDraws.Count - 1; i >= 0; i--) {
					var trail = lastFiveTrailDraws[i];
					trail.action.Invoke(trail.time);
					trail.time -= Global.spf;
				}

				var shaderList = new List<ShaderWrapper>();
				if (Global.shaderWrappers.ContainsKey("trail")) shaderList.Add(Global.shaderWrappers["trail"]);

				if (lastFiveTrailDraws.Count > 5) lastFiveTrailDraws.PopFirst();
				lastFiveTrailDraws.Add(new Trail() {
					action = (float time) => {
						DrawWrappers.DrawTexture(
							bitmap,
							currentFrame.rect.x1, currentFrame.rect.y1,
							currentFrame.rect.w(), currentFrame.rect.h(),
							x, y, zIndex,
							cx - frameOffsetX * xDirArg,
							cy - frameOffsetY * yDirArg,
							xDirArg, yDirArg, angle, alpha, shaderList, true
						);
					},
					time = 0.25f
				});
			}
			if (renderEffects.Contains(RenderEffectType.SpeedDevilTrail) && character != null && Global.shaderWrappers.ContainsKey("speedDevilTrail")) {
				for (int i = character.lastFiveTrailDraws.Count - 1; i >= 0; i--) {
					Trail trail = character.lastFiveTrailDraws[i];
					if (character.isDashing) {
						trail.action.Invoke(trail.time);
					}
					trail.time -= Global.spf;
					if (trail.time <= 0) character.lastFiveTrailDraws.RemoveAt(i);
				}

				var shaderList = new List<ShaderWrapper>();

				var speedDevilShader = character.player.speedDevilShader;
				shaderList.Add(speedDevilShader);

				if (character.lastFiveTrailDraws.Count > 1) character.lastFiveTrailDraws.PopFirst();

				character.lastFiveTrailDraws.Add(new Trail() {
					action = (float time) => {
						speedDevilShader?.SetUniform("alpha", time * 2);
						DrawWrappers.DrawTexture(
							bitmap,
							currentFrame.rect.x1, currentFrame.rect.y1,
							currentFrame.rect.w(), currentFrame.rect.h(),
							x, y, zIndex,
							cx - frameOffsetX * xDirArg,
							cy - frameOffsetY * yDirArg,
							xDirArg, yDirArg, angle, alpha, shaderList, true
						);
					},
					time = 0.125f
				});
			}
		}
		if (!isCompositeSprite) {
			DrawWrappers.DrawTexture(
				bitmap,
				currentFrame.rect.x1,
				currentFrame.rect.y1 - extraYOff,
				currentFrame.rect.w(),
				currentFrame.rect.h() + extraY,
				x, y, zIndex,
				cx - frameOffsetX * xDirArg,
				cy - (frameOffsetY - extraYOff) * yDirArg,
				xDirArg, yDirArg,
				angle, alpha, shaders, true
			);
		} else {
			DrawWrappers.DrawCompositeTexture(
				compositeBitmaps.ToArray(),
				currentFrame.rect.x1 - flippedExtraW,
				currentFrame.rect.y1 - extraYOff,
				currentFrame.rect.w() + extraW,
				currentFrame.rect.h() + extraY,
				x, y, zIndex,
				cx - (frameOffsetX - extraXOff) * xDirArg,
				cy - (frameOffsetY - extraYOff) * yDirArg,
				xDirArg, yDirArg,
				angle, alpha, shaders, true
			);
		}
		if (isUPX) {
			var upShaders = new List<ShaderWrapper>(shaders);
			if (Global.isOnFrameCycle(5)) {
				if (Global.shaderWrappers.ContainsKey("hit")) {
					upShaders.Add(Global.shaderWrappers["hit"]);
				}
			}
			DrawWrappers.DrawTexture(
				Global.textures["XUPGlow"],
				currentFrame.rect.x1, currentFrame.rect.y1,
				currentFrame.rect.w(), currentFrame.rect.h(),
				x, y, zIndex,
				cx - frameOffsetX * xDirArg,
				cy - frameOffsetY * yDirArg,
				xDirArg, yDirArg, angle, alpha, upShaders, true
			);
		}
		if (animData.isAxlSprite && drawAxlArms) {
			DrawWrappers.DrawTexture(
				axlArmBitmap,
				currentFrame.rect.x1, currentFrame.rect.y1,
				currentFrame.rect.w(),
				currentFrame.rect.h(),
				x, y, zIndex,
				cx - frameOffsetX * xDirArg,
				cy - frameOffsetY * yDirArg,
				xDirArg, yDirArg, 0, alpha, shaders, true
			);
		}
	}

	public bool needsX3BusterCorrection() {
		return name.Contains("mmx_shoot") || name.Contains("mmx_run_shoot") || name.Contains("mmx_fall_shoot") || name.Contains("mmx_jump_shoot") || name.Contains("mmx_dash_shoot") || name.Contains("mmx_ladder_shoot")
			|| name.Contains("mmx_wall_slide_shoot") || name.Contains("mmx_up_dash_shoot") || name.Contains("mmx_wall_kick_shoot");
	}

	public Frame getCurrentFrame(int frameIndex = -1) {
		if (frameIndex == -1) {
			frameIndex = this.frameIndex;
		}
		if (reversed) {
			frameIndex = totalFrameNum - 1 - frameIndex;
		}
		if (frameIndex < 0) {
			return animData.frames[0];
		}
		if (frameIndex >= animData.frames.Length) {
			return animData.frames[animData.frames.Length - 1];
		}
		return animData.frames[frameIndex];
	}

	public int getFrameIndexSafe() {
		int frameIndex = this.frameIndex;

		if (reversed) {
			frameIndex = animData.frames.Length - 1 - frameIndex;
		}
		if (frameIndex < 0) {
			return 0;
		}
		if (frameIndex >= animData.frames.Length) {
			return animData.frames.Length - 1;
		}
		return frameIndex;
	}

	public bool isAnimOver() {
		return (
			frameIndex == animData.frames.Length - 1 && frameTime >= getCurrentFrame().duration ||
			loopCount > 0
		);
	}

	public float getAnimLength() {
		float total = 0;
		foreach (Frame frame in animData.frames) {
			total += frame.duration;
		}
		return total;
	}

	public Point getAlignOffsetHelper(Rect rect, Point offset, int? flipX, int? flipY) {
		flipX = flipX ?? 1;
		flipY = flipY ?? 1;

		float x = animData.baseAlignmentX;
		float y = animData.baseAlignmentY;

		if (flipX == -1) {
			x -= 1;
			x = MathF.Abs(x);
		}
		if (flipY == -1) {
			y -= 1;
			y = MathF.Abs(y);
		}
		x *= -1;
		y *= -1;

		x *= rect.w();
		y *= rect.h();

		if (flipX > 0) x = MathF.Floor(x);
		else x = MathF.Ceiling(x);
		if (flipY > 0) y = MathF.Floor(y);
		else y = MathF.Ceiling(y);

		x += animData.alignOffX;
		y += animData.alignOffY;

		return new Point(x + offset.x * (int)flipX, y + offset.y * (int)flipY);
	}

	public void drawWindow(
		int frameIndex, float x, float y,
		int flipX, int flipY, float alpha,
		float scaleX, float scaleY, long zIndex,
		Rect drawRect,
		Actor? actor = null, bool useFrameOffsets = false
	) {
		if (!visible) return;
		if (actor != null) {
			if (!actor.shouldDraw()) return;
		}
		Frame currentFrame = getCurrentFrame(frameIndex);
		if (currentFrame == null) return;

		float cx = animData.baseAlignmentX;
		float cy = animData.baseAlignmentY;

		cx = cx * currentFrame.rect.w();
		cy = cy * currentFrame.rect.h();
		cx += animData.alignOffX - tempOffX;
		cy += animData.alignOffY - tempOffY;
		cx = MathF.Floor(cx);
		cy = MathF.Floor(cy);

		float frameOffsetX = 0;
		float frameOffsetY = 0;

		if (useFrameOffsets) {
			frameOffsetX = currentFrame.offset.x * flipX;
			frameOffsetY = currentFrame.offset.y * flipY;
		}

		float xDirArg = flipX * scaleX;
		float yDirArg = flipY * scaleY;

		Texture bitmap = animData.bitmap;

		DrawWrappers.DrawTexture(
			bitmap,
			drawRect.x1, drawRect.y1,
			drawRect.w(), drawRect.h(),
			x, y, zIndex,
			cx - frameOffsetX * xDirArg,
			cy - frameOffsetY * yDirArg,
			xDirArg, yDirArg,
			0, alpha, null, true
		);
	}
}

public class Trail {
	public Action<float> action;
	public float time;
}

public class AnimData {
	public string name;
	public string customMapName;
	public Collider[] hitboxes;
	public Frame[] frames;
	public int loopStartFrame;
	public float baseAlignmentX;
	public float baseAlignmentY;
	public float alignOffX;
	public float alignOffY;
	public bool loop;
	public Texture bitmap;

	public bool isXSprite;
	public bool isAxlSprite;

	public AnimData(string spriteJsonStr, string name, string customMapName) {
		dynamic spriteJson = JsonConvert.DeserializeObject(spriteJsonStr) ?? throw new NullReferenceException();

		this.name = name;
		this.customMapName = customMapName;
		string alignmentText = Convert.ToString(spriteJson.alignment);

		(baseAlignmentX, baseAlignmentY) = alignmentText switch {
			"topmid" => (0.5f, 0f),
			"topright" => (1f, 0f),
			"midleft" => (0f, 0.5f),
			"center" => (0.5f, 0.5f),
			"midright" => (1f, 0.5f),
			"botleft" => (0f, 1f),
			"botmid" => (0.5f, 1f),
			"botright" => (1f, 1f),
			_ => (0, 0),
		};

		alignOffX = Convert.ToInt32(spriteJson.alignOffX);
		alignOffY = Convert.ToInt32(spriteJson.alignOffY);

		string wrapMode = Convert.ToString(spriteJson.wrapMode);
		if (wrapMode == "loop") {
			loop = true;
		}
		loopStartFrame = Convert.ToInt32(spriteJson.loopStartFrame);

		string spritesheetPath = Path.GetFileName(Convert.ToString(spriteJson.spritesheetPath));
		if (!string.IsNullOrEmpty(customMapName)) {
			spritesheetPath = customMapName + ":" + spritesheetPath;
		}
		string textureName = Path.GetFileNameWithoutExtension(spritesheetPath);
		bitmap = Global.textures[textureName];

		List<Frame> frames = new();
		List<Collider> hitboxes = new();

		if (textureName == "XDefault") {
			isXSprite = true;
		}
		if (textureName == "axl") {
			isAxlSprite = true;
		}

		JArray hitboxesJson = spriteJson["hitboxes"];
		foreach (dynamic hitboxJson in hitboxesJson) {
			float width = (float)Convert.ToDouble(hitboxJson["width"]);
			float height = (float)Convert.ToDouble(hitboxJson["height"]);
			float offsetX = (float)Convert.ToDouble(hitboxJson["offset"]["x"]);
			float offsetY = (float)Convert.ToDouble(hitboxJson["offset"]["y"]);
			bool isTrigger = Convert.ToBoolean(hitboxJson["isTrigger"]);
			int flag = Convert.ToInt32(hitboxJson["flag"]);
			string hitboxName = Convert.ToString(hitboxJson["name"]);

			Collider hitbox = new Collider(
				new List<Point>() {
				  new Point(0, 0),
				  new Point(0 + width, 0),
				  new Point(0 + width, 0 + height),
				  new Point(0, 0 + height)
				},
				isTrigger ? true : false,
				null, false, false,
				(HitboxFlag)flag,
				new Point(offsetX, offsetY)
			);
			hitbox.name = hitboxName;
			hitbox.originalSprite = this.name;
			hitboxes.Add(hitbox);
		}

		JArray framesJson = spriteJson["frames"];
		foreach (dynamic frameJson in framesJson) {
			float x1 = (float)Convert.ToDouble(frameJson["rect"]["topLeftPoint"]["x"]);
			float y1 = (float)Convert.ToDouble(frameJson["rect"]["topLeftPoint"]["y"]);
			float x2 = (float)Convert.ToDouble(frameJson["rect"]["botRightPoint"]["x"]);
			float y2 = (float)Convert.ToDouble(frameJson["rect"]["botRightPoint"]["y"]);
			float offsetX = (float)Convert.ToDouble(frameJson["offset"]["x"]);
			float offsetY = (float)Convert.ToDouble(frameJson["offset"]["y"]);
			float durationSeconds = (float)Convert.ToDouble(frameJson["duration"]);
			float durationFrames = MathF.Round(durationSeconds * 60);

			if (x1 > x2) {
				(x1, x2) = (x2, x1);
			}
			if (y1 > y2) {
				(y1, y2) = (y2, y1);
			}
			// Rendertexture creation.
			int sprWidth = MathInt.Ceiling(x2 - x1);
			int sprHeight = MathInt.Ceiling(y2 - y1);
			if (sprWidth > 1024) {
				sprWidth = 1024;
				x2 = x1 + 1024;
			}
			if (sprHeight > 1024) {
				sprHeight = 1024;
				y2 = y1 + 1024;
			}

			if (sprWidth <= 0 || sprHeight <= 0) {
				throw new Exception("Error loading sprite " + name);
			}
			Frame frame = new Frame(
				new Rect(x1, y1, x2, y2),
				durationFrames,
				new Point(offsetX, offsetY)
			);

			int encodeKey = (sprWidth * 397) ^ sprHeight;
			if (Global.isLoading) {
				if (!Global.renderTextureQueueKeys.Contains(encodeKey)) {
					lock (Global.renderTextureQueueKeys) {
						Global.renderTextureQueueKeys.Add(encodeKey);
					}
					lock (Global.renderTextureQueue) {
						Global.renderTextureQueue.Add(((uint)sprWidth, (uint)sprHeight));
					}
				}
			}
			else if (!Global.renderTextures.ContainsKey(encodeKey)) {
				Global.renderTextures[encodeKey] = (
					new RenderTexture((uint)sprWidth, (uint)sprHeight),
					new RenderTexture((uint)sprWidth, (uint)sprHeight)
				);
			}
			if (frameJson["POIs"] != null) {
				List<Point> framePOIs = new();
				List<String> framePoiTags = new();

				dynamic poisJson = frameJson["POIs"];
				for (int j = 0; j < poisJson.Count; j++) {
					float poiX = (float)Convert.ToDouble(poisJson[j]["x"]);
					float poiY = (float)Convert.ToDouble(poisJson[j]["y"]);
					string tags = Convert.ToString(poisJson[j]["tags"]);
					if (tags == "h") {
						frame.headPos = new Point(poiX, poiY);
					} else {
						framePOIs.Add(new Point(poiX, poiY));
						framePoiTags.Add(tags);
					}
				}
				frame.POIs = framePOIs.ToArray();
				frame.POITags = framePoiTags.ToArray();
			}

			if (frameJson["hitboxes"] != null) {
				JArray hitboxFramesJson = frameJson["hitboxes"];
				List<Collider> frameHitboxes = new();

				for (int j = 0; j < hitboxFramesJson.Count; j++) {
					dynamic hitboxFrameJson = hitboxFramesJson[j];

					float width = (float)Convert.ToDouble(hitboxFrameJson["width"]);
					float height = (float)Convert.ToDouble(hitboxFrameJson["height"]);
					bool isTrigger = Convert.ToBoolean(hitboxFrameJson["isTrigger"]);
					int flag = Convert.ToInt32(hitboxFrameJson["flag"]);
					float offsetX2 = (float)Convert.ToDouble(hitboxFrameJson["offset"]["x"]);
					float offsetY2 = (float)Convert.ToDouble(hitboxFrameJson["offset"]["y"]);
					string hitboxName = Convert.ToString(hitboxFrameJson["name"]);

					Collider hitbox = new Collider(
						new List<Point>() {
							new Point(0, 0),
							new Point(0 + width, 0),
							new Point(0 + width, 0 + height),
							new Point(0, 0 + height)
						},
						isTrigger ? true : false,
						null, false, false,
						(HitboxFlag)flag,
						new Point(offsetX2, offsetY2)
					);
					hitbox.name = hitboxName;
					hitbox.originalSprite = this.name;
					frameHitboxes.Add(hitbox);
				}
				frame.hitboxes = frameHitboxes.ToArray();
			}
			frames.Add(frame);
		}
		this.frames = frames.ToArray();
		this.hitboxes = hitboxes.ToArray();
	}

	public void overrideAnim(AnimData overrideAnim) {
		for (int i = 0; i < overrideAnim.frames.Length; i++) {
			frames[i].rect = overrideAnim.frames[i].rect;
			frames[i].offset = overrideAnim.frames[i].offset;
		}
		bitmap = overrideAnim.bitmap;
	}

	public AnimData clone() {
		AnimData clonedSprite = (AnimData)MemberwiseClone();
		clonedSprite.hitboxes = new Collider[hitboxes.Length];
		for (int i = 0; i < hitboxes.Length; i++) {
			clonedSprite.hitboxes[i] = hitboxes[i].clone();
		}
		clonedSprite.frames = new Frame[frames.Length];
		for (int i = 0; i < frames.Length; i++) {
			clonedSprite.frames[i] = frames[i].clone();
		}
		return clonedSprite;
	}

	public void draw(
		int frameIndex, float x, float y, int flipX, int flipY,
		HashSet<RenderEffectType>? renderEffects, float alpha,
		float scaleX, float scaleY, long zIndex,
		List<ShaderWrapper>? shaders = null,
		float angle = 0,
		bool useFrameOffsets = false
	) {
		if (frameIndex >= frames.Length) {
			frameIndex = frames.Length - 1;
		}
		if (frameIndex < 0) {
			frameIndex = 0;
		}
		Frame currentFrame = frames[frameIndex];

		float cx = baseAlignmentX;
		float cy = baseAlignmentY;

		cx = cx * currentFrame.rect.w();
		cy = cy * currentFrame.rect.h();

		cx += alignOffX;
		cy += alignOffY;

		cx = MathF.Floor(cx);
		cy = MathF.Floor(cy);

		float frameOffsetX = 0;
		float frameOffsetY = 0;

		if (useFrameOffsets) {
			frameOffsetX = currentFrame.offset.x * flipX;
			frameOffsetY = currentFrame.offset.y * flipY;
		}

		if (shaders == null) shaders = new List<ShaderWrapper>();

		if (renderEffects != null) {
			ShaderWrapper? shader = null;
			if (renderEffects.Contains(RenderEffectType.Hit)) {
				shader = Global.shaderWrappers.GetValueOrDefault("hit");
				if (shaders.Count > 1) shaders.RemoveAt(1);
			} else if (renderEffects.Contains(RenderEffectType.Flash)) {
				shader = Global.shaderWrappers.GetValueOrDefault("flash");
			} else if (renderEffects.Contains(RenderEffectType.InvisibleFlash) && alpha == 1) {
				shader = Global.shaderWrappers.GetValueOrDefault("invisible");
				shader?.SetUniform("alpha", 0.5f - (MathF.Sin(Global.level.time * 5) * 0.25f));
			} else if (renderEffects.Contains(RenderEffectType.ChargeGreen)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargeGreen");
			} else if (renderEffects.Contains(RenderEffectType.ChargeOrange)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargeOrange");
			} else if (renderEffects.Contains(RenderEffectType.ChargePink)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargePink");
			} else if (renderEffects.Contains(RenderEffectType.ChargeYellow)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargeYellow");
			} else if (renderEffects.Contains(RenderEffectType.ChargeBlue)) {
				shader = Global.shaderWrappers.GetValueOrDefault("chargeBlue");
			} else if (renderEffects.Contains(RenderEffectType.StealthModeBlue)) {
				shader = Global.shaderWrappers.GetValueOrDefault("stealthmode_blue");
			} else if (renderEffects.Contains(RenderEffectType.StealthModeRed)) {
				shader = Global.shaderWrappers.GetValueOrDefault("stealthmode_red");
			}
			if (shader != null) {
				shaders.Add(shader);
			}

			if (renderEffects.Contains(RenderEffectType.Shake)) {
				frameOffsetX += Helpers.randomRange(-1, 1);
				frameOffsetY += Helpers.randomRange(-1, 1);
			}
		}

		float xDirArg = flipX * scaleX;
		float yDirArg = flipY * scaleY;

		Texture bitmap = this.bitmap;

		if (renderEffects != null && !renderEffects.Contains(RenderEffectType.Invisible)) {
			if (alpha >= 1 && (
				renderEffects.Contains(RenderEffectType.BlueShadow) ||
				renderEffects.Contains(RenderEffectType.RedShadow) ||
				renderEffects.Contains(RenderEffectType.GreenShadow)
			)) {
				ShaderWrapper? outlineShader = null;
				if (renderEffects.Contains(RenderEffectType.BlueShadow)) {
					outlineShader = Helpers.cloneShaderSafe("outline_blue");
				} else if (renderEffects.Contains(RenderEffectType.RedShadow)) {
					outlineShader = Helpers.cloneShaderSafe("outline_red");
				} else if (renderEffects.Contains(RenderEffectType.GreenShadow)) {
					outlineShader = Helpers.cloneShaderSafe("outline_green");
				}
				if (outlineShader != null) {
					outlineShader.SetUniform(
						"textureSize",
						new SFML.Graphics.Glsl.Vec2(currentFrame.rect.w() + 2, currentFrame.rect.h() + 2)
					);
					DrawWrappers.DrawTexture(
						bitmap,
						currentFrame.rect.x1 - 1, currentFrame.rect.y1 - 1,
						currentFrame.rect.w() + 2, currentFrame.rect.h() + 2,
						x, y, zIndex - 10,
						cx - (frameOffsetX - (1 * xDirArg)) * xDirArg,
						cy - (frameOffsetY - (1 * yDirArg)) * yDirArg,
						xDirArg, yDirArg, angle, alpha,
						[outlineShader], true
					);
				}
			}
		}

		float extraYOff = 0;
		DrawWrappers.DrawTexture(
			bitmap, currentFrame.rect.x1,
			currentFrame.rect.y1 - extraYOff,
			currentFrame.rect.w(), currentFrame.rect.h() + extraYOff,
			x, y, zIndex,
			cx - frameOffsetX * xDirArg,
			cy - (frameOffsetY - extraYOff) * yDirArg,
			xDirArg, yDirArg, angle, alpha, shaders, true
		);
	}

	public void drawToHUD(int frameIndex, float x, float y, float alpha = 1) {
		if (frameIndex >= frames.Length) {
			frameIndex = frames.Length - 1;
		}
		if (frameIndex < 0) {
			frameIndex = 0;
		}
		Frame currentFrame = frames[frameIndex];

		float cx = baseAlignmentX;
		float cy = baseAlignmentY;

		cx = cx * currentFrame.rect.w();
		cy = cy * currentFrame.rect.h();

		cx += alignOffX - currentFrame.offset.x;
		cy += alignOffY - currentFrame.offset.y;

		cx = MathF.Floor(cx);
		cy = MathF.Floor(cy);

		DrawWrappers.DrawTextureHUD(
			bitmap,
			currentFrame.rect.x1, currentFrame.rect.y1,
			currentFrame.rect.w(), currentFrame.rect.h(),
			x - cx, y - cy,
			alpha
		);
	}
}
