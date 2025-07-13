using System;
using System.Collections.Generic;
using SFML.Graphics;
using static SFML.Window.Keyboard;

namespace MMXOnline;
public class OptionsMenu : IMainMenu {
	public int selectedArrowPosY;
	public IMainMenu previous;

	public int startY = 40;
	public const int lineH = 9;
	public static int presetYPos = 8;
	public static int videoOffset = 10;
	public int frames;

	public List<MenuOption> menuOptions;

	public float blinkTime = 0;
	public bool isChangingName;
	public string playerName;
	public bool inGame;
	public int? charNum;
	public bool isGraphics;
	public bool isGameplay;

	public bool oldFullscreen;
	public uint oldWindowScale;
	public int oldMaxFPS;
	public bool oldDisableShaders;
	public bool oldEnablePostprocessing;
	public bool oldUseOptimizedAssets;
	private int oldParticleQuality;
	public bool oldIntegerFullscreen;
	public bool oldVsync;

	public FontType optionFontText = FontType.Blue;
	public FontType optionFontValue = FontType.Blue;

	public OptionsMenu(IMainMenu mainMenu, bool inGame, int? charNum, int selectY) {
		previous = mainMenu;
		this.inGame = inGame;
		if (selectY == 1) {
			isGameplay = true;
		}
		if (selectY == 2) {
			isGraphics = true;
		}

		oldIntegerFullscreen = Options.main.integerFullscreen;
		oldFullscreen = Options.main.fullScreen;
		oldWindowScale = Options.main.windowScale;
		oldDisableShaders = Options.main.disableShaders;
		oldMaxFPS = Options.main.maxFPS;
		oldEnablePostprocessing = Options.main.enablePostProcessing;
		oldUseOptimizedAssets = Options.main.useOptimizedAssets;
		oldParticleQuality = Options.main.particleQuality;
		oldVsync = Options.main.vsync;

		playerName = Options.main.playerName;
		this.charNum = charNum;

		if (!isGraphics && charNum == null) {
			startY = 35;
		}
		if (!inGame) {
			optionFontText = FontType.DarkBlue;
			optionFontValue = FontType.DarkBlue;
		}

		if (isGraphics) {
			menuOptions = new List<MenuOption>() {
				// Full screen
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							if (Options.main.fullScreen) {
								Options.main.fullScreen = false;
							}
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							if (!Options.main.fullScreen) {
								Options.main.fullScreen = true;
							}
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Fullscreen:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.fullScreen ? "Yes" : "No",
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Set to Yes to make the game render fullscreen."
				),
				// Windowed resolution
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.windowScale = (uint) Helpers.clamp(
								(int) Options.main.windowScale - 1, 1, 6
							);
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.windowScale = (uint) Helpers.clamp(
								(int) Options.main.windowScale + 1, 1, 6
							);
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Windowed Resolution:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, getWindowedResolution(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Change the windowed resolution of the game."
				),
				// Show FPS
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.showFPS = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.showFPS = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Show FPS:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showFPS),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Show the frames per second (FPS) in the bottom right."
				),
				// Lock FPS
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.maxFPS = 30;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.maxFPS = Global.fpsCap;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Max FPS:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.maxFPS.ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Controls the max framerate the game can run.\nLower values are more choppy but use less GPU."
				),
				// VSYNC
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						Helpers.menuLeftRightBool(ref Options.main.vsync);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Enable VSYNC:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.vsync),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Set to Yes to enable vsync.\nMakes movement/scrolling smoother, but adds input lag."
				),
				// Use optimized sprites
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						Helpers.menuLeftRightBool(ref Options.main.useOptimizedAssets);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Use optimized assets:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.useOptimizedAssets),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Set to Yes to use optimized assets.\nThis can result in better performance."
				),
				// Full screen integer
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.integerFullscreen);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Integer fullscreen:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue,Helpers.boolYesNo(Options.main.integerFullscreen),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Rounds down fullscreen pixels to the nearest integer.\n" +
					"Reduces distortion when going fullscreen."
				),
				// Small Bars
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.enableSmallBars = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.enableSmallBars = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Enable small bars:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.enableSmallBars),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Makes some of the energy bars smaller."
				),
				// Preset
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							if (Options.main.graphicsPreset > 0) {
								Options.main.graphicsPreset--;
								setPresetQuality(Options.main.graphicsPreset.Value);
							}
						}
						else if (Global.input.isPressedMenu(Control.MenuRight)) {
							if (Options.main.graphicsPreset < 3) {
								Options.main.graphicsPreset++;
								setPresetQuality(Options.main.graphicsPreset.Value);
							}
						}
					},
					(Point pos, int index) => {
						FontType color = optionFontText;
						if (!inGame) {
							color = FontType.Grey;
						}
						Fonts.drawText(
							optionFontText, "Preset Quality:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontText, qualityToString(Options.main.graphicsPreset.Value),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Choose a pre-configured set of graphics settings."
				),
				// Shaders
				new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.disableShaders = true;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.disableShaders = false;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), "-Enable shaders:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), Helpers.boolYesNo(!Options.main.disableShaders),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Enables special effects like weapon palettes.\nNot all PCs support this."
				),
				// Post processing
				new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						Helpers.menuLeftRightBool(ref Options.main.enablePostProcessing);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), "-Enable post-processing: ",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), Helpers.boolYesNo(Options.main.enablePostProcessing),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Enables special screen distortion effects.\nNot all PCs support this."
				),
				// fontType
				/*new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						Helpers.menuLeftRightInc(ref Options.main.fontType, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), "Font type:",
							pos.x + videoOffset, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), fontTypeToString(Options.main.fontType),
							pos.x + videoOffset + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Set the font type. Bitmap uses PNG, Vector uses TFF.\n" +
					"Hybrid will use Bitmap in menus and Vector in-game."
				),*/
				// particleQuality
				new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						Helpers.menuLeftRightInc(ref Options.main.particleQuality, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), "-Particle quality:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), qualityToString(Options.main.particleQuality),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Set the particle effect quality.\nLower quality results in faster performance."
				),
				// map sprites
				new MenuOption(40, startY,
					() => {
						if (inGame) return;
						if (Options.main.graphicsPreset < 3) return;
						Helpers.menuLeftRightBool(ref Options.main.enableMapSprites);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							getVideoSettingColor(), "-Enable map sprites:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							getVideoSettingColor(), Helpers.boolYesNo(Options.main.enableMapSprites),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Enable or disable map sprites.\nDisabling map sprites results in faster performance."
				),
			};
		} else if (isGameplay) {
			menuOptions = new List<MenuOption>() {
				// Preferred character
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.preferredCharacter, 0, 4);
					},
					(Point pos, int index) => {
						string preferredChar = Character.charDisplayNames[Options.main.preferredCharacter];
						Fonts.drawText(
							optionFontText, "Preferred character:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, preferredChar,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Choose a default character the game will\npre-select for you."
				),

				// Disable double-tap dash
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.disableDoubleDash = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.disableDoubleDash = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Disable double-tap dash:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.disableDoubleDash),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Disables ability to dash by quickly\ntapping LEFT or RIGHT twice."
				),

				// Kill on loadout change
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.killOnLoadoutChange);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Kill on loadout change:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.killOnLoadoutChange),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If Yes, will instantly die on loadout change mid-match.\n" +
					"If No, on next death loadout changes will apply."
				),
				// Kill on character change
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.killOnCharChange);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Kill on character change:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.killOnCharChange),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If Yes, will instantly die on character change.\n" +
					"If No, on next death character change will apply."
				),

			};
		} else if (charNum == null) {
			if (!Global.regionPingTask.IsCompleted) {
				Global.regionPingTask.Wait();
			}

			menuOptions = new List<MenuOption>() {
				// Music volume
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.musicVolume = Helpers.clamp(Options.main.musicVolume - 0.01f, 0, 1);
							Global.music.updateVolume();
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.musicVolume = Helpers.clamp(Options.main.musicVolume + 0.01f, 0, 1);
							Global.music.updateVolume();
						}
					},
					(Point pos, int index) => {
						var musicVolume100 = (int) Math.Round(Options.main.musicVolume * 100);
						Fonts.drawText(
							optionFontText, "Music Volume:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, musicVolume100.ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Adjust the game music volume."
				),
				// Sound volume
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.soundVolume = Helpers.clamp(Options.main.soundVolume - 0.01f, 0, 1);
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.soundVolume = Helpers.clamp(Options.main.soundVolume + 0.01f, 0, 1);
						}
					},
					(Point pos, int index) => {
						var soundVolume100 = (int) Math.Round(Options.main.soundVolume * 100);
						Fonts.drawText(
							optionFontText, "Sound Volume:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, soundVolume100.ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Adjust the game sound volume."
				),
				// Multiplayer Name
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (!isChangingName && (
								Global.input.isPressedMenu(Control.MenuLeft) ||
								Global.input.isPressedMenu(Control.MenuRight) ||
								Global.input.isPressedMenu(Control.MenuConfirm)
							)
						) {
							isChangingName = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Multiplayer name:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, playerName,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Your name that appears to others when you play online."
				),
				// Multiplayer region
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.regionIndex--;
							if (Options.main.regionIndex < 0) Options.main.regionIndex = 0;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.regionIndex++;
							if (Options.main.regionIndex > Global.regions.Count - 1) {
								Options.main.regionIndex = Global.regions.Count - 1;
							}
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Multiplayer region:",
							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue,
							Options.main.getRegion().name + (
								"(" + Options.main.getRegion().getDisplayPing() + " ping)"
							),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Preferred server region for hosting matches.\nChoose the one with lowest ping."
				),
				// Hide Menu Helper Text
				/*new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.showInGameMenuHUD = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.showInGameMenuHUD = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Show in-game menu keys:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showInGameMenuHUD),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Show or hide additional menu help text in\nbottom right of the in-match HUD."
				),*/
				// System requirements check
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.showSysReqPrompt = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.showSysReqPrompt = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Show startup warnings:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showSysReqPrompt),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"On launch, check for system requirements\nand other startup warnings."
				),
				// Disable Chat
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.disableChat = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.disableChat = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Disable chat:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.disableChat),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Set to Yes to disable sending and receiving\nchat messages in online matches."
				),
				// Mash progress
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.showMashProgress);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Show mash progress:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showMashProgress),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"When hit by moves that can be mashed out of,\n" +
					"shows the mash progress above your head."
				),
				// Matchmaking Timeout
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.networkTimeoutSeconds = Helpers.clamp(
								Options.main.networkTimeoutSeconds - 0.1f, 1, 5
							);
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.networkTimeoutSeconds = Helpers.clamp(
								Options.main.networkTimeoutSeconds + 0.1f, 1, 5
							);
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Matchmaking timeout:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.networkTimeoutSeconds.ToString("0.0") + " seconds",
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"How long match search will take before erroring out.\n" +
					"If always erroring out in search, try increasing this."
				),
				// Dev console.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.enableDeveloperConsole);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Enable dev console:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.enableDeveloperConsole),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If enabled, press F10 to open the dev-console in-match\n" +
					"See the game website for a list of commands."
				),
				// Dev console.
				new MenuOption(
					30, startY,
					() => {
						if (inGame) return;
						Helpers.menuLeftRightBool(ref Options.main.blackFade);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Black fade option:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.blackFade),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If enabled, a fade transition between menus will appear."
				),
			};
		} else if (charNum == 0) {
			menuOptions = new List<MenuOption>() {
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.gridModeX, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Weapon switch grid mode:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, gridModeToStr(Options.main.gridModeX),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"For weapon switch in certain or all modes.\n" +
					"Hold WEAPON L/R and use a directon to switch weapon."
				),
				// Hyper Charge slot.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.hyperChargeSlot, 0, 2);
					},
					(Point pos, int index) => {
						// ToDo: Implement "Buster" option for hypercharge like HDM.
						Fonts.drawText(
							optionFontText, "Hyper charge slot:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.hyperChargeSlot + 1).ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Weapon slot number which Hyper Charge uses."
				),
				// Down+Special Giga Attacks
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.gigaCrushSpecial);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Giga Attack down special:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.gigaCrushSpecial),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Allows to perform Giga Crush by pressing DOWN + SP,\n" +
					"but you lose the ability to switch to Giga Crush."
				),
				// Nova Strike special.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.novaStrikeSpecial);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "N.Strike side special:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.novaStrikeSpecial),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Allows to perform Nova Strike by pressing SPC,\n" +
					"but you lose the ability to switch to Nova Strike."
				),
				/*
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.xSpecialOrdering, 0, 5);
					},
					(Point pos, int index) => {
						string s = "H.Buster,G.Crush,N.Strike";
						if (Options.main.xSpecialOrdering == 1) s = "G.Crush,H.Buster,N.Strike";
						if (Options.main.xSpecialOrdering == 2) s = "G.Crush,N.Strike,H.Buster";
						if (Options.main.xSpecialOrdering == 3) s = "H.Buster,N.Strike,G.Crush";
						if (Options.main.xSpecialOrdering == 4) s = "N.Strike,G.Crush,H.Buster";
						if (Options.main.xSpecialOrdering == 5) s = "N.Strike,H.Buster,G.Crush";
						Fonts.drawText(
							optionFontText, "Special Slot Order:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, s,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					}
				)
				*/
			};
		} else if (charNum == 1) {
			menuOptions = new List<MenuOption>() {
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.swapAirAttacks);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Swap air attacks:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.swapAirAttacks),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Swaps the inputs for air slash attack,\n" +
					"and Kuuenzan (or any other air special)."
				),
				// Zero Giga cooldown.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.showGigaAttackCooldown);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Show giga cooldown:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showGigaAttackCooldown),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Shows a cooldown circle for giga attacks."
				),
			};
		} else if (charNum == 2) {
			menuOptions = new List<MenuOption>() {
				// Swap goliath inputs
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.swapGoliathInputs);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Swap goliath shoot:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.swapGoliathInputs),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"You can swap the inputs for\nGoliath buster and missiles."
				),
				// Weapon Ordering
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.weaponOrderingVile, 0, 1);
					},
					(Point pos, int index) => {
						string str = "F.Runner,Vulcan,R.Armors";
						if (Options.main.weaponOrderingVile == 1) {
							str = "Vulcan,F.Runner,R.Armors";
						}
						Fonts.drawText(
							optionFontText, "Weapon order:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, str,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Choose the order in which Vile's weapons are arranged."
				),
				// MK5 Ride control
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.mk5PuppeteerHoldOrToggle);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Vile V ride control:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.mk5PuppeteerHoldOrToggle ? "Hold" : "Simul"),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If set to Hold, Vile V will control the Ride\nonly as long as WEAPON L/R is held."
				),
				// Lock Cannon Air
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.lockInAirCannon);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Lock in air cannon:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.lockInAirCannon ? "Yes" : "No"),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If No, Front Runner and Fat Boy cannons will not\nroot Vile in the air when shot."
				),
			};
		} else if (charNum == 3) {
			menuOptions = new List<MenuOption>() {
				// Axl Use Mouse Aim
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							Options.main.axlAimMode = 0;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							Options.main.axlAimMode = 2;
						}
					},
					(Point pos, int index) => {
						string aimMode = "Directional";
						if (Options.main.axlAimMode == 1) aimMode = "Directional";
						else if (Options.main.axlAimMode == 2) aimMode = "Cursor";
						Fonts.drawText(
							optionFontText, "Aim mode:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, aimMode,
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Change Axl's aim controls to either use\nARROW KEYS (Directional) or mouse aim (Cursor)."
				),
				// Axl Mouse sensitivity
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.aimSensitivity = Helpers.clamp(Options.main.aimSensitivity - 0.01f, 0, 1);
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.aimSensitivity = Helpers.clamp(Options.main.aimSensitivity + 0.01f, 0, 1);
						}
					},
					(Point pos, int index) => {
						var str = (int)Math.Round(Options.main.aimSensitivity * 100);
						Fonts.drawText(
							optionFontText, "Aim sensitivity:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, str.ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Change aim sensitivity (for Cursor aim mode only.)"
				),
				// Axl Lock On
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.lockOnSound = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.lockOnSound = true;
						}
					},
					(Point pos, int index) => {
						//ToDo: Add an actual option for the sound.
						Fonts.drawText(
							optionFontText, "Auto aim:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.lockOnSound),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Enable/disable auto-aim-\n(For Directional aim mode only.)"
				),
				// Axl Backwards Aim Invert
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.aimAnalog = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.aimAnalog = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Analog stick aim:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.aimAnalog),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Enables 360 degree aim if binding Axl aim controls\nto a controller analog stick."
				),
				// Aim key function
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.aimKeyFunction, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Aim key function:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, aimKeyFunctionToStr(Options.main.aimKeyFunction),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Change the behavior of Axl's \"aim key\"."
				),
				// Aim key toggle
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.aimKeyToggle);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Aim key behavior:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.aimKeyToggle ? "Toggle" : "Hold"),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Change whether Axl's \"aim key\"\nis toggle or hold based."
				),
				// Diag aim movement
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.moveInDiagAim);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Move in diagonal aim:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.moveInDiagAim),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Allows Axl tomove when aiming diagonally,\notherwise he is locked in place when shooting."
				),
				// Axl Separate aim crouch
				new MenuOption(
					30, startY,
					() => {
						if (Global.input.isHeldMenu(Control.MenuLeft)) {
							Options.main.axlSeparateAimDownAndCrouch = false;
						} else if (Global.input.isHeldMenu(Control.MenuRight)) {
							Options.main.axlSeparateAimDownAndCrouch = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Aim down & crouch:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Options.main.axlSeparateAimDownAndCrouch ? "Separate" : "Mixed",
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If \"mixed\" Aim down and crounch will bind to\n" +
					"the same button and crouching will not aim down."
				),
				// Grid mode Axl
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.gridModeAxl, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "weapon switch grid mode:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, gridModeToStr(Options.main.gridModeAxl),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Enables Grid Mode for Axl,\nwhich works the same way as X's."
				),
				// Roll Cooldown HUD.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.showRollCooldown);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Show roll cooldown:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, Helpers.boolYesNo(Options.main.showRollCooldown),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If enabled, shows a cooldown circle above Axl's head\n" +
					"indicating Dodge Roll cooldown."
				),
			};
		} else if (charNum == 4) {
			menuOptions = new List<MenuOption>() {
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightInc(ref Options.main.sigmaWeaponSlot, 0, 2);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Sigma slot:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.sigmaWeaponSlot + 1).ToString(),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Changes the position of the\nSigma slot in Sigma's hotbar."
				),
				// Pupeteer control mode.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.puppeteerHoldOrToggle);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Puppeteer control:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.puppeteerHoldOrToggle ? "Hold" : "Toggle"),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If set to Hold, Puppeteer Sigma will control\na Maverick only as long as WEAPON L/R is held."
				),
				// Maverick follow start.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.maverickStartFollow);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Maverick start mode:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.maverickStartFollow ? "Follow" : "Hold Position"),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Change whether Mavericks will follow Sigma,\nor hold position, after summoned."
				),
				// Pupeteer cancel.
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.puppeteerCancel);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Puppeteer cancel:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.puppeteerCancel ? "Yes" : "No"),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"If set to Yes, Mavericks will revert to\ntheir idle state when switched to in Puppeteer mode."
				),
				// Small Bars for Pup Sigma
				new MenuOption(
					30, startY,
					() => {
						Helpers.menuLeftRightBool(ref Options.main.smallBarsEx);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							optionFontText, "Pup small energy bars:",
 							pos.x, pos.y, selected: selectedArrowPosY == index
						);
						Fonts.drawText(
							optionFontValue, (Options.main.smallBarsEx ? "Yes" : "No"),
							pos.x + 166, pos.y, selected: selectedArrowPosY == index
						);
					},
					"Makes the energy bars smaller for Puppeteer.\nRequires the \"small\" bars option to work.")
			};
		}
		for (int i = 0; i < menuOptions.Count; i++) {
			menuOptions[i].pos.y = startY + lineH * i;
		}
	}

	private string fontTypeToString(int fontType) {
		if (fontType == 0) return "Bitmap";
		else if (fontType == 1) return "Hybrid";
		else return "Vector";
	}

	public static void setPresetQuality(int graphicsPreset) {
		if (graphicsPreset >= 3) return;
		Options.main.graphicsPreset = graphicsPreset;
		Options.main.fontType = 0; //(graphicsPreset == 0 ? 0 : 1);
		Options.main.particleQuality = graphicsPreset;
		Options.main.enablePostProcessing = (graphicsPreset > 0);
		Options.main.disableShaders = (graphicsPreset == 0);
		Options.main.useOptimizedAssets = (graphicsPreset <= 1);
		Options.main.enableMapSprites = (graphicsPreset > 0);
		Options.main.saveToFile();
	}

	public static void inferPresetQuality(uint textureSize) {
		string presetMessage = (
			"Based on your detected video card texture size of {0}, " +
			"a Graphics setting of {1} has been automatically selected.\n\n" +
			"You can change this in the Settings menu."
		);
		if (textureSize <= 1024) {
			setPresetQuality(0);
			Helpers.showMessageBox(string.Format(presetMessage, textureSize, "Low"), "Graphics settings preset");
		} else if (textureSize <= 2048) {
			setPresetQuality(1);
			Helpers.showMessageBox(string.Format(presetMessage, textureSize, "Medium"), "Graphics settings preset");
		} else {
			setPresetQuality(2);
			Helpers.showMessageBox(string.Format(presetMessage, textureSize, "High"), "Graphics settings preset");
		}
	}

	private string qualityToString(int quality) {
		if (quality == 0) return "Low";
		else if (quality == 1) return "Medium";
		else if (quality == 2) return "High";
		else return "Custom";
	}

	private string aimKeyFunctionToStr(int aimKeyFunction) {
		if (aimKeyFunction == 0) return "Aim backwards/backpedal";
		else if (aimKeyFunction == 1) return "Lock position";
		else return "Lock aim";
	}

	string gridModeToStr(int gridMode) {
		if (gridMode == 0) return "No";
		if (gridMode == 1) return "1v1 Only";
		if (gridMode == 2) return "Always";
		return "Error";
	}

	public bool isRegionDisabled() {
		if (inGame) return true;
		return Global.regions == null || Global.regions.Count < 2;
	}

	public Color getColor() {
		return inGame ? Helpers.Gray : Color.White;
	}

	public FontType getVideoSettingColor() {
		if (!inGame) {
			if (Options.main.graphicsPreset < 3) {
				return FontType.Grey;
			}
			return FontType.DarkBlue; ;
		}
		if (Options.main.graphicsPreset < 3) {
			return FontType.Grey;
		}
		return FontType.Blue;
	}

	public void update() {
		if (!isGraphics && charNum == null) {
			frames++;
			if (frames > 240) {
				frames = 0;
				Global.updateRegionPings();
			}
		}

		if (isChangingName) {
			blinkTime += Global.spf;
			if (blinkTime >= 1f) {
				blinkTime = 0;
			}
			playerName = Helpers.getTypedString(playerName, Global.maxPlayerNameLength);

			if (Global.input.isPressed(Key.Enter) && !string.IsNullOrWhiteSpace(playerName.Trim())) {
				isChangingName = false;
				Options.main.playerName = Helpers.censor(playerName).Trim();
				Options.main.saveToFile();
			}

			return;
		}

		if (Global.input.isPressedMenu(Control.MenuUp)) {
			selectedArrowPosY--;
			if (selectedArrowPosY < 0) {
				selectedArrowPosY = menuOptions.Count - 1;
				if (isGraphics && Options.main.graphicsPreset < 3) {
					selectedArrowPosY = presetYPos;
				}
			}
			Global.playSound("menu");
		} else if (Global.input.isPressedMenu(Control.MenuDown)) {
			selectedArrowPosY++;
			if (selectedArrowPosY > menuOptions.Count - 1) {
				selectedArrowPosY = 0;
			}
			if (isGraphics && Options.main.graphicsPreset < 3 && selectedArrowPosY > presetYPos) {
				selectedArrowPosY = 0;
			}
			Global.playSound("menu");
		}

		menuOptions[selectedArrowPosY].update();
		helpText = menuOptions[selectedArrowPosY].configureMessage;

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			if ((Options.main.xLoadout.weapon1 == Options.main.xLoadout.weapon2 && Options.main.xLoadout.weapon1 >= 0) ||
				(Options.main.xLoadout.weapon1 == Options.main.xLoadout.weapon3 && Options.main.xLoadout.weapon2 >= 0) ||
				(Options.main.xLoadout.weapon2 == Options.main.xLoadout.weapon3 && Options.main.xLoadout.weapon3 >= 0)) {
				Menu.change(new ErrorMenu(new string[] {
					"Error: same weapon selected twice"
				}, this));
				return;
			}

			if (Options.main.axlLoadout.weapon2 == Options.main.axlLoadout.weapon3) {
				Menu.change(new ErrorMenu(new string[] {
					"Error: same weapon selected twice"
				}, this));
				return;
			}

			Options.main.saveToFile();
			if (oldWindowScale != Options.main.windowScale) {
				Global.changeWindowSize(Options.main.windowScale);
			}

			if (oldFullscreen != Options.main.fullScreen ||
				//oldWindowScale != Options.main.windowScale ||
				oldMaxFPS != Options.main.maxFPS ||
				oldDisableShaders != Options.main.disableShaders ||
				oldEnablePostprocessing != Options.main.enablePostProcessing ||
				oldUseOptimizedAssets != Options.main.useOptimizedAssets ||
				oldParticleQuality != Options.main.particleQuality ||
				//oldIntegerFullscreen != Options.main.integerFullscreen ||
				oldVsync != Options.main.vsync
			) {
				Menu.change(new ErrorMenu(new string[] {
					"Note: options were changed that",
					"require restart to apply."				
				}, previous));
			} else {
				Menu.change(previous);
			}
		}
	}

	public string getWindowedResolution() {
		return $"{Global.screenW * Options.main.windowScale}x{Global.screenH * Options.main.windowScale}";
	}

	public string helpText = "";
	public void render() {
		float cursorPos = 24;
		if (isGraphics && selectedArrowPosY > presetYPos && selectedArrowPosY < presetYPos + 6) {
			cursorPos = 24;
		}
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			DrawWrappers.DrawTextureHUD(
				Global.textures["cursor"], cursorPos, 35 + (selectedArrowPosY * 10) - 2
			);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			Global.sprites["cursor"].drawToHUD(0, cursorPos, 35 + (selectedArrowPosY * 10) + 3);
		}

		string subtitle = "GENERAL SETTINGS";
		if (isGraphics) subtitle = "GRAPHICS SETTINGS";
		else if (charNum == 0) subtitle = "X SETTINGS";
		else if (charNum == 1) subtitle = "ZERO SETTINGS";
		else if (charNum == 2) subtitle = "VILE SETTINGS";
		else if (charNum == 3) subtitle = "AXL SETTINGS";
		else if (charNum == 4) subtitle = "SIGMA SETTINGS";
		Fonts.drawText(FontType.Yellow, subtitle, Global.halfScreenW, 20, Alignment.Center);
		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change, [BACK]: Save and Back",
			Global.halfScreenW, 198, Alignment.Center
		);

		for (int i = 0; i < menuOptions.Count; i++) {
			menuOptions[i].render(new Point(32, 35 + (i * 10)), i);
		}

		float rectY = 170;
		if (!string.IsNullOrEmpty(helpText)) {
			DrawWrappers.DrawRect(
				20, rectY, Global.screenW - 20, rectY + 24, true,
				new Color(0, 0, 0, 224), 1, ZIndex.HUD, false, outlineColor: Color.White
			);
			Fonts.drawText(
				FontType.Green, helpText, Global.halfScreenW, rectY + 4,
				alignment: Alignment.Center
			);
		}
		if (isChangingName) {
			float top = Global.screenH * 0.4f;

			/*DrawWrappers.DrawRect(
				5, top - 20, Global.screenW - 5, top + 60,
				true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false
			);*/
			DrawWrappers.DrawRect(
				5, 5, Global.screenW - 5, Global.screenH - 5,
				true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false
			);
			Fonts.drawText(
				FontType.Orange, "Type in a multiplayer name",
				Global.screenW / 2, top, alignment: Alignment.Center
			);
			int xPos = MathInt.Round(Global.screenW * 0.33f);
			Fonts.drawText(FontType.Green, playerName, xPos, 20 + top);
			if (blinkTime >= 0.5f) {
				int width = Fonts.measureText(FontType.Green, playerName);
				Fonts.drawText(FontType.Grey, "<", xPos + width + 3, 20 + top);
			}

			Fonts.drawText(
				FontType.Grey, "Press Enter to continue",
				Global.screenW / 2, 40 + top, alignment: Alignment.Center
			);
		}
	}
}
