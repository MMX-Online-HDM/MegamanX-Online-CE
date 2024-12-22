using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using static SFML.Window.Keyboard;

namespace MMXOnline;

class Program {
	#if WINDOWS
	[STAThread]
	#endif
	static void Main(string[] args) {
		if (args.Length > 0 && args[0] == "-relay") {
		#if WINDOWS
			AllocConsole();
		#endif
			RelayServer.ServerMain(args);
		} else {
			int mode = 0;
			if (args.Length > 0 && args[0] == "-server") {
				mode = 1;
				args = new string[] { };
			}
			if (args.Length >= 2 && args[0] == "-connect") {
				mode = 2;
				args = args[1..];
			} else {
				args = new string[] { };
			}
			GameMain(args, mode);
		}
		if (Global.localServer != null && (
			Global.localServer.s_server.Status == NetPeerStatus.Running ||
			Global.localServer.s_server.Status == NetPeerStatus.Starting
		)) {
			Global.localServer.shutdown("Host closed the game.");
		}
		Environment.Exit(0);
	}

#if WINDOWS
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool AllocConsole();
#endif

	static void GameMain(string[] args, int mode) {
		if (Debugger.IsAttached) {
			Run(args, mode);
		} else {
			try {
				Run(args, mode);
			} catch (Exception e) {
				/*
				string crashDump = e.Message + "\n\n" +
					e.StackTrace + "\n\nInner exception: " +
					e.InnerException?.Message + "\n\n" +
					e.InnerException?.StackTrace;
				Helpers.showMessageBox(crashDump.Truncate(1000), "Fatal Error!");
				throw;
				*/
				Logger.LogFatalException(e);
				Logger.logException(e, false, "Fatal exception", true);
				Thread.Sleep(1000);
				throw;
			}
		}
	}

	static void Run(string[] args, int mode) {
#if MAC
		Global.assetPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "/";
		Global.writePath = Global.assetPath;
#endif
		Global.Init();
		if (Enum.GetNames(typeof(WeaponIds)).Length > 256) {
			throw new Exception("Too many weapon ids, max 256");
		}

		if (Global.debug) {
			Global.promptDebugSettings();
		}

		/*
		if (!Global.debug || Global.testDocumentsInDebug) {
			string baseDocumentsPath = Helpers.getBaseDocumentsPath();
			string mmxodDocumentsPath = Helpers.getMMXODDocumentsPath();

			#if WINDOWS
			if (string.IsNullOrEmpty(mmxodDocumentsPath) &&
				!string.IsNullOrEmpty(baseDocumentsPath) &&
				!Options.main.autoCreateDocFolderPromptShown
			) {
				Options.main.autoCreateDocFolderPromptShown = true;
				if (Helpers.showMessageBoxYesNo(
					"Auto-create MMXOD folder in Documents folder?\n" +
					"This will be used to store settings, controls, logs and more " +
					"and will persist across updates.", "MMXOD folder not found in Documents"
				)) {
					try {
						Directory.CreateDirectory(baseDocumentsPath + "/MMXOD");
						mmxodDocumentsPath = Helpers.getMMXODDocumentsPath();
					} catch (Exception e) {
						Helpers.showMessageBox(
							"Could not create MMXOD folder in Documents. Error details:\n\n" +
							e.Message, "Error creating MMXOD folder"
						);
					}
				}
			}
			#endif
			if (!string.IsNullOrEmpty(mmxodDocumentsPath)) {
				Global.writePath = mmxodDocumentsPath;
				if (Directory.Exists(mmxodDocumentsPath + "/assets")) {
					Global.assetPath = Global.writePath;
				}
			}
		}
		*/

		if (!checkSystemRequirements()) {
			return;
		}

		Global.initMainWindow(Options.main);
		RenderWindow window = Global.window;

		window.Closed += new EventHandler(onClosed);
		window.Resized += new EventHandler<SizeEventArgs>(onWindowResized);
		window.KeyPressed += new EventHandler<KeyEventArgs>(onKeyPressed);
		window.KeyReleased += new EventHandler<KeyEventArgs>(onKeyReleased);
		window.MouseMoved += new EventHandler<MouseMoveEventArgs>(onMouseMove);
		window.MouseButtonPressed += new EventHandler<MouseButtonEventArgs>(onMousePressed);
		window.MouseButtonReleased += new EventHandler<MouseButtonEventArgs>(onMouseReleased);
		window.MouseWheelScrolled += new EventHandler<MouseWheelScrollEventArgs>(onMouseScrolled);

		// Loading starts here.
		// We load font first as we are gonna render these.
		Fonts.loadFontSizes();
		Fonts.loadFontSprites();

		List<string> loadText = new();
		loadText.Add("NOM BIOS v" + Global.version + ", An Energy Sunstar Ally");
		loadText.Add("Copyright ©2114, NOM Corporation");
		loadText.Add("");
		loadText.Add("MMXOD " + Global.shortForkName + " " + Global.versionName + " " + Global.subVersionName);
		loadText.Add("");
		if (String.IsNullOrEmpty(Options.main.playerName)) {
			loadText.Add("User: Dr. Cain");
		} else {
			loadText.Add("User: " + Options.main.playerName);
		}
		// Get CPU name here.
		loadText.Add("CPU: " + getCpuName());
		loadText.Add("Memory: " + (GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024) + "kb");
		loadText.Add("");

		// Input
		Global.input = new Input(false);
		setupControllers(window);

		if (Options.main.areShadersDisabled() == false) {
			loadText.Add("Shaders OK.");
			loadShaders();
		} else {
			loadText.Add("Shaders disabled, skipping.");
		}

		// Loading with GUI.
		string urlText = "Using local conection.";
		bool hasServerOnlineUrl = false;

		loadText.Add("Getting Master Server URL...");
		loadMultiThread(loadText, window, MasterServerData.updateMasterServerURL);
		if (MasterServerData.serverIp != "127.0.0.1") {
			urlText = "All IPs OK.";
			hasServerOnlineUrl = true;
		}
		loadText[loadText.Count - 1] = "Getting local IPs...";
		loadMultiThread(loadText, window, getRadminIP);
		if (Global.radminIP != "" && hasServerOnlineUrl) {
			urlText += " Radmin detected.";
		}
		loadText[loadText.Count - 1] = urlText;

		loadText.Add("Loading Sprites...");
		loadMultiThread(loadText, window, loadImages);
		loadText[loadText.Count - 1] = $"Loaded {Global.textures.Count} Sprites.";

		loadText.Add("Loading Sprite JSONS...");
		loadMultiThread(loadText, window, loadSprites);
		loadText[loadText.Count - 1] = $"Loaded {Global.realSpriteCount} Sprite JSONs.";

		loadText.Add("Loading Maps...");
		loadMultiThread(loadText, window, loadLevels);
		loadText[loadText.Count - 1] = $"Loaded {Global.levelDatas.Count} Maps.";

		loadText.Add("Loading SFX...");
		loadMultiThread(loadText, window, loadSounds);
		loadText[loadText.Count - 1] = $"Loaded {Global.soundCount} SFX files.";

		loadText.Add("Loading Music...");
		loadMultiThread(loadText, window, loadMusics);
		loadText[loadText.Count - 1] = $"Loaded {Global.musics.Count} Songs.";

		if (Global.renderTextureQueue.Count > 0) {
			loadText.Add("Creating render textures...");
			int textureCount = Global.renderTextureQueue.Count;
			loadLoopRTexture(loadText, window, Global.renderTextureQueue.ToArray());
			Global.renderTextureQueue.Clear();
			Global.renderTextureQueueKeys.Clear();
			loadText[loadText.Count - 1] = $"Created {textureCount} render textures.";
		}

		loadText.Add("Calculating checksum...");
		loadMultiThread(loadText, window, Global.computeChecksum);
		loadText[loadText.Count - 1] = "Checksum OK.";

		/*if (!Helpers.FileExists("region.json")) {
			Helpers.WriteToFile("region.json", regionJson);
		}*/

		// Only used to initialize the Global.ignoreUpgradeChecks variable
		var primeRegions = Global.regions;

		Global.regionPingTask = Task.Run(() => {
			foreach (var region in Global.regions) {
				region.computePing();
			}
		});

		drawLoadST(loadText, window);

		GC.Collect();
		GC.WaitForPendingFinalizers();

		// Force startup config to be fetched
		Menu.change(new MainMenu());
		//Global.changeMusic(Global.level.levelData.getTitleTheme());
		switch(Helpers.randomRange(1, 15)) {
			// Stage Selects
			case 1:
				Global.changeMusic("stageSelect_X1");
				break;
			case 2:
				Global.changeMusic("stageSelect2_X1");
				break;
			case 3:
				Global.changeMusic("stageSelect_X2");
				break;
			case 4:
				Global.changeMusic("stageSelect2_X2");
				break;
			case 5:
				Global.changeMusic("stageSelect_X3");
				break;
			case 6:
				Global.changeMusic("stageSelect2_X3");
				break;
			// Introduction
			case 7:
				Global.changeMusic("opening_X3");
				break;
			case 8:
				Global.changeMusic("opening_X2");
				break;
			// Extra
			case 9:
				Global.changeMusic("ending_X3");
				break;
			case 10:
				Global.changeMusic("ending_X2");
				break;
			case 11:
				Global.changeMusic("ending_X1");
				break;
			case 12:
				Global.changeMusic("credits_X1");
				break;
			case 13:
				Global.changeMusic("demo_X2");
				break;
			case 14:
				Global.changeMusic("demo_X3");
				break;
			case 15:
				Global.changeMusic("laboratory_X2");
				break;
			default:
				Global.changeMusic("stageSelect_X1");
				break;
		}
		
		if (mode == 1) {
			HostMenu menu = new HostMenu(new MainMenu(), null, false, false, true);
			Menu.change(menu);
			menu.completeAction();
		} else if (mode == 2) {
			// TODO: Fix this.y
			// Somehow we need to get the data before we connect.
			Menu.change(new JoinMenuP2P(true));
			var me = new ServerPlayer(
				Options.main.playerName, 0, false,
				SelectCharacterMenu.playerData.charNum, null, Global.deviceId, null, 0
			);
			Global.serverClient = ServerClient.CreateDirect(
				args[0], int.Parse(args[1]), me,
				out JoinServerResponse joinServerResponse, out string error
			);
			if (joinServerResponse != null && error == null) {
				Menu.change(new WaitMenu(new MainMenu(), joinServerResponse.server, false));
			} else {
				Menu.change(new ErrorMenu(error, new MainMenu()));
			}
		}
		while (window.IsOpen) {
			mainLoop(window);
		}
	}

	static long getPacketsReceived() {
		return Global.serverClient?.packetsReceived ?? 0;
	}

	static long getBytesPerFrame() {
		if (Global.serverClient?.client?.ServerConnection?.Statistics != null) {
			long downloadedBytes = Global.serverClient.client.ServerConnection.Statistics.ReceivedBytes;
			long uploadedBytes = Global.serverClient.client.ServerConnection.Statistics.SentBytes;
			return (downloadedBytes + uploadedBytes);
		}
		return 0;
	}

	static void setupControllers(Window window) {
		// Set up joysticks
		window.JoystickButtonPressed += new EventHandler<JoystickButtonEventArgs>(onJoystickButtonPressed);
		window.JoystickButtonReleased += new EventHandler<JoystickButtonEventArgs>(onJoystickButtonReleased);
		window.JoystickMoved += new EventHandler<JoystickMoveEventArgs>(onJoystickMoved);
		window.JoystickConnected += new EventHandler<JoystickConnectEventArgs>(onJoystickConnected);
		window.JoystickDisconnected += new EventHandler<JoystickConnectEventArgs>(onJoystickDisconnected);
		Joystick.Update();
		if (Joystick.IsConnected(0)) {
			joystickConnectedHelper(0);
		}

	}

	private static void update() {
		if (Global.levelStarted()) {
			Global.level.update();
			if (Global.serverClient != null && Global.level.nonSkippedframeCount % 300 == 0) {
				new Task(Global.level.clearOldActors).Start();
			}
		}
		Menu.update();
		if (Global.leaveMatchSignal != null) {
			if (Global.level == null) {
				Global.leaveMatchSignal = null;
				Menu.change(new MainMenu());
				return;
			}

			string disconnectMessage = "";
			switch (Global.leaveMatchSignal.leaveMatchScenario) {
				case LeaveMatchScenario.LeftManually:
					disconnectMessage = "Manually left";
					break;
				case LeaveMatchScenario.MatchOver:
					disconnectMessage = "Match over";
					break;
				case LeaveMatchScenario.ServerShutdown:
					disconnectMessage = "Server was shut down, or you disconnected.";
					break;
				case LeaveMatchScenario.Recreate:
					disconnectMessage = "Recreate";
					break;
				case LeaveMatchScenario.RecreateMS:
					disconnectMessage = "RecreateMS";
					break;
				case LeaveMatchScenario.Rejoin:
					disconnectMessage = "Rejoin";
					break;
				case LeaveMatchScenario.Kicked:
					disconnectMessage = "Kicked";
					break;
			}

			Global.serverClient?.disconnect(disconnectMessage);
			Global.level.destroy();

			if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.Recreate) {
				Global.leaveMatchSignal.createNewServer();
			} else if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.Rejoin) {
				Global.leaveMatchSignal.rejoinNewServer();
			} else if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.ServerShutdown) {
				Menu.change(new ErrorMenu(disconnectMessage, new MainMenu()));
			} else if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.Kicked) {
				Menu.change(new ErrorMenu(new string[] {
					"You were kicked from the server.", "Reason: " + Global.leaveMatchSignal.kickReason
					},
					new MainMenu()
				));
			} else if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.RecreateMS) {
				Global.leaveMatchSignal.reCreateMS();
			} else if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.RejoinMS) {
				Global.leaveMatchSignal.rejoinNewServerMS();
			} else {
				Menu.change(new MainMenu());
			}

			Global.view.Center = new Vector2f(0, 0);
			Global.music?.stop();

			Global.leaveMatchSignal = null;
		}

		bool isPaused = false; //(Global.menu != null || Global.dialogBox != null);
		if (!isPaused) {
			Global.frameCount++;
			Global.time += Global.spf;
			Global.calledPerFrame = 0;

			if (!Global.paused) {
				if (Global.debug) {
					Global.cheats();
				}
				if (Options.main.isDeveloperConsoleEnabled() && Menu.chatMenu != null) {
					if (Global.input.isPressed(Key.F10)) {
						DevConsole.toggleShow();
					}
				}
				for (int i = Global.sounds.Count - 1; i >= 0; i--) {
					Global.sounds[i].update();
					if (Global.sounds[i].deleted || Global.sounds[i].sound.Status == SoundStatus.Stopped) {
						Global.sounds[i].sound.Dispose();
						Global.sounds[i].deleted = true;
						Global.sounds.RemoveAt(i);
					}
				}
				Global.music?.update();
			}

			Global.input.clearInput();
		}
	}

	private static void render() {
		if (Global.levelStarted()) {
			Global.level.render();
		} else {
			Menu.render();
		}
		// TODO: Make this work for errors.
		//if (Global.debug) {
			//Draw debug strings
			//Global.debugString1 = ((int)Math.Round(1.0f / Global.spf2)).ToString();
			/*if (Global.level != null && Global.level.character != null) {
				Global.debugString2 = Mathf.Floor(Global.level.character.pos.x / 8).ToString("0") + "," +
				Mathf.Floor(Global.level.character.pos.y / 8).ToString("0");
			}*/
			/*Fonts.drawText(FontType.Red, Global.debugString1, 20, 20);
			Fonts.drawText(FontType.Red, Global.debugString2, 20, 30);
			Fonts.drawText(FontType.Red, Global.debugString3, 20, 40);
			*/
		//}
	}

	/// <summary>
	/// Function called when the window is closed
	/// </summary>
	private static void onClosed(object sender, EventArgs e) {
		var openClients = new List<NetClient>();
		if (Global.serverClient?.client != null) {
			openClients.Add(Global.serverClient.client);
		}
		var regions = Global.regions.Concat(Global.lanRegions);
		foreach (var region in regions) {
			if (region.getPingClient() != null) {
				openClients.Add(region.getPingClient());
			}
		}

		foreach (var client in openClients) {
			client.Shutdown("user quit application");
			client.FlushSendQueue();
		}

		while (true) {
			int tries = 0;
			Thread.Sleep(10);
			tries++;
			if (tries > 200) break;
			if (openClients.All(c => c.ServerConnection == null)) {
				break;
			}
		}

		RenderWindow window = (RenderWindow)sender;
		window.Close();
	}

	private static void onWindowResized(object sender, SizeEventArgs e) {
		// Compares the aspect ratio of the window to the aspect ratio of the view,
		// and sets the view's viewport accordingly in order to archieve a letterbox effect.
		float windowRatio = Global.window.Size.X / (float)Global.window.Size.Y;
		float viewRatio = Global.view.Size.X / Global.view.Size.Y;
		float sizeX = 1;
		float sizeY = 1;
		float posX = 0;
		float posY = 0;

		bool horizontalSpacing = true;
		if (windowRatio < viewRatio) {
			horizontalSpacing = false;
		}
		// If horizontalSpacing is true, the black bars will appear on the left and right side.
		// Otherwise, the black bars will appear on the top and bottom.
		if (horizontalSpacing) {
			sizeX = viewRatio / windowRatio;
			posX = (1f - sizeX) / 2f;
		} else {
			sizeY = windowRatio / viewRatio;
			posY = (1f - sizeY) / 2f;
		}
		Global.view.Viewport = new FloatRect(posX, posY, sizeX, sizeY);
		DrawWrappers.hudView.Viewport = new FloatRect(posX, posY, sizeX, sizeY);
	}

	/// <summary>
	/// Function called when a key is pressed
	/// </summary>
	private static void onKeyPressed(object sender, KeyEventArgs e) {
		RenderWindow window = (RenderWindow)sender;
		//if (e.Code == Keyboard.Key.Escape)
		//    window.Close();

		Global.input.keyPressed[e.Code] = !Global.input.keyHeld.ContainsKey(e.Code) || !Global.input.keyHeld[e.Code];
		Global.input.keyHeld[e.Code] = true;
		Global.input.setLastUpdateTime();
		if (Global.input.keyPressed[e.Code]) {
			Global.input.mashCount++;
		}

		// Check for AI takeover
		if (e.Code == Key.F12 && Global.level?.mainPlayer != null) {
			if (Global.level.isTraining() && Global.serverClient == null) {
				if (AI.trainingBehavior == AITrainingBehavior.Default) AI.trainingBehavior = AITrainingBehavior.Idle;
				else if (AI.trainingBehavior == AITrainingBehavior.Idle) AI.trainingBehavior = AITrainingBehavior.Attack;
				else if (AI.trainingBehavior == AITrainingBehavior.Attack) AI.trainingBehavior = AITrainingBehavior.Jump;
				else if (AI.trainingBehavior == AITrainingBehavior.Jump) AI.trainingBehavior = AITrainingBehavior.Default;
			} else {
				if (Global.level.isTraining()) {
					if (!Global.level.mainPlayer.isAI) {
						AI.trainingBehavior = AITrainingBehavior.Attack;
						Global.level.mainPlayer.aiTakeover = true;
						Global.level.mainPlayer.isAI = true;
						Global.level.mainPlayer.character?.addAI();
					} else {
						if (AI.trainingBehavior == AITrainingBehavior.Attack) {
							AI.trainingBehavior = AITrainingBehavior.Jump;
						} else if (AI.trainingBehavior == AITrainingBehavior.Jump) {
							AI.trainingBehavior = AITrainingBehavior.Default;
						} else if (AI.trainingBehavior == AITrainingBehavior.Default) {
							AI.trainingBehavior = AITrainingBehavior.Idle;
							Global.level.mainPlayer.aiTakeover = false;
							Global.level.mainPlayer.isAI = false;
							if (Global.level.mainPlayer.character != null) Global.level.mainPlayer.character.ai = null;
						}
					}
				} else {
					if (!Global.level.mainPlayer.isAI) {
						Global.level.mainPlayer.aiTakeover = true;
						Global.level.mainPlayer.isAI = true;
						Global.level.mainPlayer.character?.addAI();
					} else {
						if (Global.level.isTraining()) {
							AI.trainingBehavior = AITrainingBehavior.Idle;
						}
						Global.level.mainPlayer.aiTakeover = false;
						Global.level.mainPlayer.isAI = false;
						if (Global.level.mainPlayer.character != null) Global.level.mainPlayer.character.ai = null;
					}
				}
			}
		}
		if (e.Code == Key.F12) {
			return;
		}

		ControlMenu controlMenu = Menu.mainMenu as ControlMenu;
		if (controlMenu != null && controlMenu.listenForKey && controlMenu.bindFrames == 0) {
			controlMenu.bind((int)e.Code);
		}
	}

	private static void onKeyReleased(object sender, KeyEventArgs e) {
		Global.input.keyHeld[e.Code] = false;
		Global.input.keyPressed[e.Code] = false;
	}

	static void onMouseMove(object sender, MouseMoveEventArgs e) {
		Input.mouseDeltaX = e.X - Global.halfScreenW;
		Input.mouseDeltaY = e.Y - Global.halfScreenH;
		Global.input.setLastUpdateTime();
	}

	static void onMousePressed(object sender, MouseButtonEventArgs e) {
		if (Global.debug && Global.level == null) {
			if (e.Button == Mouse.Button.Middle) {
				Global.debugString1 = (e.X / Options.main.windowScale) + "," + (e.Y / Options.main.windowScale);
			} else {
				Global.debugString1 = "";
			}
		}
		Input.mousePressed[e.Button] = true;
		Input.mouseHeld[e.Button] = true;
		Global.input.setLastUpdateTime();
		Global.input.mashCount++;
	}

	static void onMouseReleased(object sender, MouseButtonEventArgs e) {
		int button = (int)e.Button;
		Input.mousePressed[e.Button] = false;
		Input.mouseHeld[e.Button] = false;
	}

	static void onMouseScrolled(object sender, MouseWheelScrollEventArgs e) {
		if (e.Delta > 0) Input.mouseScrollUp = true;
		else if (e.Delta < 0) Input.mouseScrollDown = true;
		Global.input.setLastUpdateTime();
	}

	private static void onJoystickButtonPressed(object sender, JoystickButtonEventArgs e) {
		int button = (int)e.Button;
		buttonPressedHelper(e.JoystickId, button);
		Global.input.mashCount++;
		Global.input.setLastUpdateTime();
	}

	private static void buttonPressedHelper(uint joystickId, int button) {
		var buttonPressed = Global.input.buttonPressed;
		var buttonHeld = Global.input.buttonHeld;
		buttonPressed[button] = !buttonHeld.ContainsKey(button) || !buttonHeld[button];
		buttonHeld[button] = true;

		ControlMenu controlMenu = Menu.mainMenu as ControlMenu;
		if (controlMenu != null && controlMenu.listenForKey && controlMenu.bindFrames == 0) {
			controlMenu.bind(button);
		}
	}

	private static void onJoystickButtonReleased(object sender, JoystickButtonEventArgs e) {
		int button = (int)e.Button;
		buttonReleasedHelper(e.JoystickId, button);
	}

	private static void buttonReleasedHelper(uint joystickId, int button) {
		var buttonPressed = Global.input.buttonPressed;
		var buttonHeld = Global.input.buttonHeld;
		buttonHeld[button] = false;
		buttonPressed[button] = false;
	}

	private static void onJoystickMoved(object sender, JoystickMoveEventArgs e) {
		Global.input.setLastUpdateTime();

		Player currentPlayer = Global.level?.mainPlayer;

		int threshold = 70;
		int rawAxisNum = (int)e.Axis;
		int axisNum = 1000 + rawAxisNum;   //1000 = x, 1001 = y

		var cMap = Control.getControllerMapping(currentPlayer?.charNum ?? -1, Options.main.axlAimMode);
		if (cMap != null) {
			int? rightAxis = cMap.GetValueOrDefault(Control.AimRight);
			int? downAxis = cMap.GetValueOrDefault(Control.AimDown);

			if (rightAxis != null && axisNum == rightAxis) {
				Input.lastAimX = Input.aimX;
				Input.aimX = e.Position;
			}
			if (downAxis != null && axisNum == downAxis) {
				Input.lastAimY = Input.aimY;
				Input.aimY = e.Position;
			}
		}

		if (Math.Abs(e.Position) < threshold - 5) {
			buttonReleasedHelper(e.JoystickId, -axisNum);
			buttonReleasedHelper(e.JoystickId, axisNum);
		} else if (e.Position < -threshold) {
			buttonPressedHelper(e.JoystickId, -axisNum);
		} else if (e.Position > threshold) {
			buttonPressedHelper(e.JoystickId, axisNum);
		}
	}

	private static void onJoystickConnected(object sender, JoystickConnectEventArgs e) {
		joystickConnectedHelper(e.JoystickId);
	}

	private static void joystickConnectedHelper(uint joystickId) {
		if (Control.isJoystick()) return;
		string controllerName = Joystick.GetIdentification(joystickId).Name;

		Global.input.buttonPressed = new Dictionary<int, bool>();
		Global.input.buttonHeld = new Dictionary<int, bool>();

		if (Control.joystick == null) {
			Control.joystick = new JoystickInfo(joystickId);
		}

		if (!Control.controllerNameToMapping.ContainsKey(controllerName)) {
			Control.controllerNameToMapping[Control.getControllerName()] = Control.getGenericMapping();
		}
	}

	private static void onJoystickDisconnected(object sender, JoystickConnectEventArgs e) {
		if (Control.joystick != null && Control.joystick.id == e.JoystickId) {
			Control.joystick = null;
		}
	}

	static void loadImages() {
		string spritesheetPath = "assets/spritesheets";
		if (Options.main.shouldUseOptimizedAssets()) spritesheetPath += "_optimized";
		var spritesheets = Helpers.getFiles(Global.assetPath + spritesheetPath, false, "png", "psd");

		var menuImages = Helpers.getFiles(Global.assetPath + "assets/menu", true, "png", "psd");
		var fontSprites = Helpers.getFiles(Global.assetPath + "assets/fonts", true, "png", "psd");
		spritesheets.AddRange(menuImages);
		spritesheets.AddRange(fontSprites);

		for (int i = 0; i < spritesheets.Count; i++) {
			string path = spritesheets[i];
			Texture texture = new Texture(path);
			Global.textures[Path.GetFileNameWithoutExtension(path)] = texture;
		}

		var mapSpriteImages = Helpers.getFiles(Global.assetPath + "assets/maps_custom", true, "png", "psd");
		foreach (var mapSpriteImage in mapSpriteImages) {
			var pieces = mapSpriteImage.Split("/sprites/");
			if (pieces.Length == 2 && pieces[1].EndsWith(".png")) {
				string spriteImageName = pieces[1].Replace(".png", "");
				Texture texture = new Texture(mapSpriteImage);
				string mapName = mapSpriteImage.Replace("/sprites/" + pieces[1], "").Split("/").ToList().Pop();
				Global.textures[mapName + ":" + spriteImageName] = texture;
			}
		}
	}

	static string getFileBlobMD5(Dictionary<string, string> fileNamesToContents) {
		string entireBlob = "";
		var keys = fileNamesToContents.Keys.ToList();
		keys.Sort(Helpers.invariantStringCompare);

		foreach (var key in keys) {
			entireBlob += key.ToLowerInvariant() + " " + fileNamesToContents[key];
		}
		var md5 = System.Security.Cryptography.MD5.Create();
		return BitConverter.ToString(
			md5.ComputeHash(
				System.Text.Encoding.UTF8.GetBytes(entireBlob)
			)
		).Replace(
			"-", String.Empty
		);
	}

	static void addToFileChecksumBlob(Dictionary<string, string> fileNamesToContents) {
		string entireBlob = "";
		var keys = fileNamesToContents.Keys.ToList();
		keys.Sort(Helpers.invariantStringCompare);

		foreach (var key in keys) {
			entireBlob += key.ToLowerInvariant() + " " + fileNamesToContents[key];
		}
		Global.fileChecksumBlob += entireBlob + "|";
	}

	static void loadLevels() {
		var levelPaths = Helpers.getFiles(Global.assetPath + "assets/maps", true, "json");

		var fileChecksumDict = new Dictionary<string, string>();
		var invertedMaps = new HashSet<string>();
		foreach (string levelPath in levelPaths) {
			string levelText = File.ReadAllText(levelPath);
			string levelIniText = "";
			string levelIniLocation = Path.GetDirectoryName(levelPath) + "/mapData.ini";
			if (File.Exists(levelIniLocation)) {
				levelIniText = File.ReadAllText(levelIniLocation);
			}
			var levelData = new LevelData(levelText, levelIniText, false);

			var pathPieces = levelPath.Split('/').ToList();
			string fileName = pathPieces.Pop();
			string folderName = pathPieces.Pop();
			fileChecksumDict[folderName + "/" + fileName] = levelText;

			if (levelData.name.EndsWith("_inverted")) {
				invertedMaps.Add(levelData.name.Replace("_inverted", ""));
				continue;
			} else if (levelData.name.EndsWith("_mirrored")) {
				levelData.name = levelData.name.Replace("_inverted", "");
				Global.levelDatas.Add(levelData.name, levelData);
				levelData.isMirrored = true;
				levelData.name = levelData.name.Replace("_mirrored", "");
			} else {
				Global.levelDatas.Add(levelData.name, levelData);
			}
		}
		Global.fileChecksumBlob += "-" + getFileBlobMD5(fileChecksumDict);

		var customLevelPaths = Helpers.getFiles(Global.assetPath + "assets/maps_custom", true, "json");
		foreach (string levelPath in customLevelPaths) {
			if (levelPath.Contains("/sprites/")) continue;

			string levelText = File.ReadAllText(levelPath);
			string levelIniText = "";
			string levelIniLocation = Path.GetDirectoryName(levelPath) + "/mapData.ini";
			if (File.Exists(levelIniLocation)) {
				levelIniText = File.ReadAllText(levelIniLocation);
			}
			var levelData = new LevelData(levelText, levelIniText, true);
			if (levelData.name.EndsWith("_mirrored")) {
				Global.levelDatas.Add(levelData.name, levelData);
				levelData.isMirrored = true;
				levelData.name = levelData.name.Replace("_mirrored", "");
			} else {
				Global.levelDatas.Add(levelData.name, levelData);
			}
		}

		foreach (string invertedMap in invertedMaps) {
			Global.levelDatas[invertedMap].supportsMirrored = true;
		}

		foreach (var levelData in Global.levelDatas.Values) {
			levelData.populateMirrorMetadata();
		}
	}

	static void loadSprites() {
		string spritePath = "assets/sprites";

		string[] spriteFilePaths = Helpers.getFiles(Global.assetPath + spritePath, false, "json").ToArray();
		if (spriteFilePaths.Length > 65536) {
			throw new Exception(
				"Exceeded max sprite limit of 65536. Fix actor.cs netUpdate() to support more sprites."
			);
		}

		int fileSplit = MathInt.Floor(spriteFilePaths.Count() / 6.0);
		string[][] treadedFilePaths;
		// Use multitread if loading 20 or more sprites.
		if (spriteFilePaths.Length >= 20) {
			treadedFilePaths = new string[][] {
				spriteFilePaths[..fileSplit],
				spriteFilePaths[(fileSplit)..(fileSplit*2)],
				spriteFilePaths[(fileSplit*2)..(fileSplit*3)],
				spriteFilePaths[(fileSplit*3)..(fileSplit*4)],
				spriteFilePaths[(fileSplit*4)..(fileSplit*5)],
				spriteFilePaths[(fileSplit*5)..],
			};
			string[] fileChecksums = new string[6];
			List<Task> tasks = new();
			for (int i = 0; i < treadedFilePaths.Length; i++) {
				int j = i;
				Task tempTask = new Task(() => { fileChecksums[j] = loadSpritesSub(treadedFilePaths[j]); });
				tasks.Add(tempTask);
				tempTask.ContinueWith(loadExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
				tempTask.Start();
			}
			while (tasks.Count > 0) {
				for (int i = 0; i < tasks.Count; i++) {
					if (tasks[i].Status >= TaskStatus.RanToCompletion) {
						tasks.Remove(tasks[i]);
						i = 0;
					}
				}
			}
			foreach (string fileChecksum in fileChecksums) {
				Global.fileChecksumBlob += "-" + fileChecksum;
			}
		} else {
			string fileChecksum = loadSpritesSub(spriteFilePaths);
			Global.fileChecksumBlob += "-" + fileChecksum;
		}
		// Override sprite mods
		string overrideSpriteSource = "assets/sprites_visualmods";
		if (Options.main.shouldUseOptimizedAssets()) overrideSpriteSource = "assets/sprites_optimized";

		List<string> overrideSpritePaths = Helpers.getFiles(Global.assetPath + overrideSpriteSource, false, "json");
		foreach (string overrideSpritePath in overrideSpritePaths) {
			string name = Path.GetFileNameWithoutExtension(overrideSpritePath);
			string json = File.ReadAllText(overrideSpritePath);

			AnimData sprite = new AnimData(json, name, null);
			if (Global.sprites.ContainsKey(sprite.name)) {
				Global.sprites[sprite.name].overrideAnim(sprite);
			}
		}

		// Set up aliases here
		foreach (var spriteName in Global.sprites.Keys.ToList()) {
			string alias = Global.spriteAliases.GetValueOrDefault(spriteName);
			if (!string.IsNullOrEmpty(alias)) {
				var pieces = alias.Split(',');
				foreach (var piece in pieces) {
					Global.sprites[piece] = Global.sprites[spriteName].clone();
					Global.sprites[piece].name = piece;
				}
			}
		}

		// Create sprite RPC index data for RPC purposes.
		List<String> arrayBuffer = Global.sprites.Keys.ToList();
		arrayBuffer.Sort(Helpers.invariantStringCompare);

		for (int i = 0; i < arrayBuffer.Count; i++) {
			// For HUD purposes.
			Global.realSpriteCount++;
			// Skip custom map sprites.
			if (!string.IsNullOrEmpty(Global.sprites[arrayBuffer[i]].customMapName)) {
				continue;
			}
			Global.spriteIndexByName[arrayBuffer[i]] = (ushort)i;
			Global.spriteNameByIndex[i] = arrayBuffer[i];
			Global.spriteCount++;
		}

		// Set up special sprites.
		// Mods that does not use this should remove this thing.
		Sprite.xArmorBootsBitmap[0] = Global.textures["XBoots"];
		Sprite.xArmorBodyBitmap[0] = Global.textures["XBody"];
		Sprite.xArmorHelmetBitmap[0] = Global.textures["XHelmet"];
		Sprite.xArmorArmBitmap[0] = Global.textures["XArm"];

		Sprite.xArmorBootsBitmap[1] = Global.textures["XBoots2"];
		Sprite.xArmorBodyBitmap[1] = Global.textures["XBody2"];
		Sprite.xArmorHelmetBitmap[1] = Global.textures["XHelmet2"];
		Sprite.xArmorArmBitmap[1] = Global.textures["XArm2"];

		Sprite.xArmorBootsBitmap[2] = Global.textures["XBoots3"];
		Sprite.xArmorBodyBitmap[2] = Global.textures["XBody3"];
		Sprite.xArmorHelmetBitmap[2] = Global.textures["XHelmet3"];
		Sprite.xArmorArmBitmap[2] = Global.textures["XArm3"];

		Sprite.axlArmBitmap = Global.textures["axlArm"];
	}

	static string loadSpritesSub(string[] spriteFilePaths) {
		Dictionary<string, string> fileChecksumDict = new();
		foreach (string spriteFilePath in spriteFilePaths) {
			string name = Path.GetFileNameWithoutExtension(spriteFilePath);
			string json = File.ReadAllText(spriteFilePath);
			if (String.IsNullOrEmpty(name)) {
				continue;
			}
			fileChecksumDict[name] = json;
			AnimData sprite = new AnimData(json, name, "");
			lock (Global.sprites) {
				Global.sprites[sprite.name] = sprite;
			}
		}
		return getFileBlobMD5(fileChecksumDict);
	}

	static void loadSounds() {
		var soundNames = Helpers.getFiles(Global.assetPath + "assets/sounds", true, "ogg", "wav", "mp3", "flac");
		if (soundNames.Count > 65535) {
			throw new Exception("Cannot have more than 65535 sounds.");
		}
		for (int i = 0; i < soundNames.Count; i++) {
			string file = soundNames[i];
			string name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
			if (Global.soundBuffers.ContainsKey(name)) {
				throw new Exception("Duplicated sound: " + name);
			}
			Global.soundBuffers[name] = new SoundBufferWrapper(name, file, SoundPool.Regular);
		}

		// Voices
		var voiceNames = Helpers.getFiles(Global.assetPath + "assets/voices", true, "ogg", "wav", "mp3", "flac");
		for (int i = 0; i < voiceNames.Count; i++) {
			string file = voiceNames[i];
			string name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();;
			Global.voiceBuffers[name] = new SoundBufferWrapper(name, file, SoundPool.Voice);
		}

		// Char-Specific Overrides
		var overrideNames = Helpers.getFiles(
			Global.assetPath + "assets/sounds_overrides", true, "ogg", "wav", "mp3", "flac"
		);
		for (int i = 0; i < overrideNames.Count; i++) {
			string file = overrideNames[i];
			string name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();;
			if (Global.soundBuffers.ContainsKey(name)) {
				Global.soundBuffers[name] = new SoundBufferWrapper(name, file, SoundPool.Regular);
			} else {
				Global.charSoundBuffers[name] = new SoundBufferWrapper(name, file, SoundPool.CharOverride);
			}
		}

		// Create sound-index list for RPC purposes.
		List<String> arrayBuffer = Global.soundBuffers.Keys.ToList();
		arrayBuffer.Sort(Helpers.invariantStringCompare);

		for (int i = 0; i < arrayBuffer.Count; i++) {
			Global.soundIndexByName[arrayBuffer[i]] = (ushort)i;
			Global.soundNameByIndex[i] = arrayBuffer[i];
			Global.soundCount++;
		}
	}

	static void loadMusics() {
		string path = Global.assetPath + "assets/music";
		List<string> files = Helpers.getFiles(path, true, "wav", "ogg", "flac");
		files = files.Concat(
			Helpers.getFiles(Global.assetPath + "assets/maps_custom", true, "wav", "ogg", "flac")
		).ToList();

		for (int i = 0; i < files.Count; i++) {
			bool isIniMusic = File.Exists(
				Path.GetDirectoryName(files[i]) + "/" +
				Path.GetFileNameWithoutExtension(files[i]) + ".ini"
			);
			bool isLocal = !files[i].Contains("assets/maps_custom");

			if (isIniMusic) {
				loadMusicData(files[i]);
			} else if (isLocal) {
				loadMusicWithoutData(files[i]);
			} else {
				loadMusicDataLegacy(files[i]);
			}
		}
	}
	
	static void loadMusicData(string file) {
		string name = Path.GetFileNameWithoutExtension(file);
		string iniLocation = (
			Path.GetDirectoryName(file) + "/" +
			Path.GetFileNameWithoutExtension(file) + ".ini"
		);
		Dictionary<string, object> iniData = IniParser.Parse(iniLocation);

		float startPos = 0;
		float endPos = 0;
		if (iniData.ContainsKey("loopData") && iniData["loopData"] is Dictionary<string, object> loopData) {
			if (loopData.ContainsKey("loopStart") && loopData["loopStart"] is Decimal loopStart) {
				startPos = float.Parse(loopStart.ToString());
			}
			if (loopData.ContainsKey("loopEnd") && loopData["loopEnd"] is Decimal loopEnd) {
				endPos = float.Parse(loopEnd.ToString());
			}
		}
		MusicWrapper musicWrapper = new MusicWrapper(file, startPos, endPos, true);
		if (endPos == 0) {
			musicWrapper.endPos =  musicWrapper.music.Duration.AsSeconds();
		}
		Global.musics[name] = musicWrapper;
	}

	static void loadMusicWithoutData(string file) {
		string name = Path.GetFileNameWithoutExtension(file);

		MusicWrapper musicWrapper = new MusicWrapper(file, 0, 0, true);
		musicWrapper.endPos =  musicWrapper.music.Duration.AsSeconds();

		Global.musics[name] = musicWrapper;
	}

	static void loadMusicDataLegacy(string file) {
		string name = Path.GetFileNameWithoutExtension(file);

		var pieces = name.Split('.');
		string baseName = pieces[0];
		if (file.Contains("assets/maps_custom")) {
			var filePieces = file.Split('/').ToList();
			filePieces.Pop();
			string customMapName = filePieces.Pop();
			if (baseName == "music") {
				baseName = customMapName;
			} else {
				baseName = customMapName + ":" + baseName;
			}
		}

		int pieceIndex = 1;
		double startPos = 0;
		double endPos = 0;
		if (pieceIndex < pieces.Length &&
			double.TryParse(
				pieces[pieceIndex].Replace(',', '.'), NumberStyles.Any,
				CultureInfo.InvariantCulture, out startPos
			)
		) {
			pieceIndex++;
		}
		if (pieceIndex < pieces.Length &&
			double.TryParse(
				pieces[pieceIndex].Replace(',', '.'), NumberStyles.Any,
				CultureInfo.InvariantCulture, out endPos
			)
		) {
			pieceIndex++;
		}
		string charOverride = pieceIndex < pieces.Length ? ("." + pieces[pieceIndex]) : "";
		MusicWrapper musicWrapper = new MusicWrapper(file, startPos, endPos, loop: (endPos != 0));

		Global.musics[baseName + charOverride] = musicWrapper;
	}

	static void loadShaders() {
		string path = Global.assetPath + "assets/shaders";
		List<string> files = Helpers.getFiles(path, false, "frag");
		files = files.Concat(Helpers.getFiles(Global.assetPath + "assets/maps_custom", true, "frag")).ToList();

		for (int i = 0; i < files.Count; i++) {
			if (files[i].Contains("standard.vertex")) continue;
			bool isCustomMapShader = files[i].Contains("assets/maps_custom");
			string customMapName = "";
			if (isCustomMapShader) {
				var pieces = files[i].Split('/').ToList();
				pieces.Pop();
				customMapName = pieces.Pop();
			}

			string shaderContents = File.ReadAllText(files[i]);
			string shaderName = Path.GetFileNameWithoutExtension(files[i]);
			if (isCustomMapShader) {
				shaderName = customMapName + ":" + shaderName;
			}

			Global.shaderCodes[shaderName] = shaderContents;

			try {
				Global.shaders[shaderName] = Helpers.createShader(shaderName);
				Global.shaderWrappers[shaderName] = new ShaderWrapper(shaderName);
			} catch (Exception e) {
				if (e.Message.Contains(Helpers.noShaderSupportMsg)) {
					Global.shadersNotSupported = true;
				} else {
					Global.shadersFailed.Add(shaderName);
				}
			}
		}
	}

	static bool checkSystemRequirements() {
		List<string> errors = new List<string>();

		uint maxTextureSize = Texture.MaximumSize;
		if (maxTextureSize < 1024) {
			errors.Add(
				"Your GPU max texture size (" + maxTextureSize + ") is too small. " +
				"Required is 1024. The game cannot be played as most visuals require a larger " +
				"GPU max texture size.\nAttempt to launch game anyway?"
			);
			string errorMsg = string.Join(Environment.NewLine, errors);
			bool result = Helpers.showMessageBoxYesNo(errorMsg, "System Requirements Not Met");
			return result;
		}

		if (Options.main.graphicsPreset == null) {
			OptionsMenu.inferPresetQuality(maxTextureSize);
		}

		if (Options.main.showSysReqPrompt) {
			if (Global.shadersNotSupported) {
				errors.Add(
					"Your system does not support shaders. You can still play the game, " +
					"but you will not see special effects or weapon palettes."
				);
			} else if (Global.shadersFailed.Count > 0) {
				string failedShaderStr = string.Join(",", Global.shadersFailed);
				errors.Add(
					"Failed to compile the following shaders:\n\n" + failedShaderStr +
					"\n\nYou can still play the game, but you will not see these shaders' special effects."
				);
			}
		}

		if (errors.Count > 0) {
			string errorMsg = string.Join(Environment.NewLine + Environment.NewLine, errors);
			Helpers.showMessageBox(errorMsg, "System Requirements Not Met");
		}

		return true;
	}

	public static void getRadminIP() {
		var local = NetworkInterface.GetAllNetworkInterfaces();
		foreach (NetworkInterface net in local) {
			if (net.NetworkInterfaceType != NetworkInterfaceType.Ethernet) {
				continue;
			}
			if (!net.Description.ToLowerInvariant().Contains("radmin")) {
				continue;
			}
			foreach (var uniAdress in net.GetIPProperties().UnicastAddresses) {
				IPAddress ipAddress = IPAddress.Parse(uniAdress.Address.ToString());
				if (ipAddress.AddressFamily == AddressFamily.InterNetwork) {
					Global.radminIP = ipAddress.ToString();
					return;
				}
			}
		}
	}

	// Main loop stuff.
	public static decimal deltaTimeSavings = 0;
	public static decimal lastUpdateTime = 0;

	public static void setLastUpdateTimeAsNow() {
		deltaTimeSavings = 0;
		TimeSpan timeSpam = (DateTimeOffset.UtcNow - Global.UnixEpoch);
		lastUpdateTime = timeSpam.Ticks;
	}

	// Main loop.
	// GM19 used some deltatime system.
	// This leads to massive inconsistencies on high lag.
	// We instead we use frameskip.
	public static void mainLoop(RenderWindow window) {
		// Variables for stuff.
		decimal deltaTime = 0;
		decimal deltaTimeAlt = 0;
		decimal lastAltUpdateTime = 0;
		decimal fpsLimit = (TimeSpan.TicksPerSecond / 60m);
		decimal fpsLimitAlt = (TimeSpan.TicksPerSecond / 240m);
		long lastSecondFPS = 0;
		int videoUpdatesThisSecond = 0;
		int framesUpdatesThisSecond = 0;
		bool useFrameSkip = false;
		// Debug stuff.
		bool isFrameStep = false;
		bool continueNextFrameStep = false;
		bool f5Released = true;
		bool f6Released = true;
		// WARNING DISABLE THIS FOR NON-DEBUG BUILDS
		bool frameStepEnabled = true;
		var clearColor = Color.Black;
		//if (Global.level?.levelData?.bgColor != null) {
		//	clearColor = Global.level.levelData.bgColor;
		//}

		// Main loop itself.
		while (window.IsOpen) {
			TimeSpan timeSpam = (DateTimeOffset.UtcNow - Global.UnixEpoch);
			long timeNow = timeSpam.Ticks;

			// Framerate calculations.
			deltaTime = deltaTimeSavings + ((timeNow - lastUpdateTime) / fpsLimit);
			deltaTimeAlt = ((timeNow - lastAltUpdateTime) / fpsLimitAlt);
			if (deltaTime >= 1 || deltaTimeAlt >= 1) {
				window.DispatchEvents();
				lastAltUpdateTime = timeNow;
				// Framestep works always, but offline only.
				if (frameStepEnabled && Global.serverClient == null) {
					if (Keyboard.IsKeyPressed(Key.F5)) {
						if (f5Released) {
							isFrameStep = !isFrameStep;
							f5Released = false;
						}
					} else {
						f5Released = true;
					}
					if (Keyboard.IsKeyPressed(Key.F6)) {
						if (f6Released) {
							continueNextFrameStep = true;
							f6Released = false;
						}
					} else {
						f6Released = true;
					}
				}
			}
			if (!(deltaTime >= 1)) {
				continue;
			}
			long timeSecondsNow = (long)Math.Floor(timeSpam.TotalSeconds);
			if (timeSecondsNow > lastSecondFPS) {
				Global.currentFPS = videoUpdatesThisSecond;
				Global.logicFPS = framesUpdatesThisSecond;
				lastSecondFPS = timeSecondsNow;
				videoUpdatesThisSecond = 0;
				framesUpdatesThisSecond = 0;
			}
			// For debug framestep.
			if (isFrameStep && !continueNextFrameStep) {
				lastUpdateTime = timeNow;
				continue;
			}
			// Disable frameskip in the menu.
			if (Global.level != null) {
				useFrameSkip = false;
			} else {
				useFrameSkip = false;
			}
			if (Global.isMouseLocked) {
				Mouse.SetPosition(new Vector2i((int)Global.halfScreenW, (int)Global.halfScreenH), Global.window);
			}
			if (!useFrameSkip || isFrameStep) {
				update();
				framesUpdatesThisSecond++;
				deltaTime = 0;
				deltaTimeSavings = 0;
				continueNextFrameStep = false;
			} else {
				// Frameskip limiter.
				if (deltaTime >= 10) {
					deltaTime = 10;
				}
				// Logic update happens here.
				while (deltaTime >= 1) {
					// This is to only send RPC is when not frameskipping.
					if (deltaTime >= 2) {
						Global.isSkippingFrames = true;
					} else {
						Global.isSkippingFrames = false;
					}
					// Main update loop.
					update();
					deltaTime--;
					framesUpdatesThisSecond++;
				}
			}
			if (deltaTime < 0.01m) {
				deltaTime = 0;
			}
			deltaTimeSavings = deltaTime;
			Global.isSkippingFrames = false;
			Global.input.clearInput();
			lastUpdateTime = timeNow;
			videoUpdatesThisSecond++;
			window.Clear(clearColor);
			render();
			window.Display();
			/*
			long prevPackets = 0;
			if (Global.showDiagnostics) {
				diagnosticsClock.Restart();
				prevPackets = getBytesPerFrame();
			}
			if (Global.showDiagnostics) {
				Global.lastFrameProcessTime = diagnosticsClock.ElapsedTime.AsMilliseconds();
				Global.lastFrameProcessTimes.Add(Global.lastFrameProcessTime);
				if (Global.lastFrameProcessTimes.Count > 120) Global.lastFrameProcessTimes.RemoveAt(0);

				long packetIncrease = getBytesPerFrame() - prevPackets;
				Global.lastFramePacketIncreases.Add(packetIncrease);
				if (Global.lastFramePacketIncreases.Count > 120) {
					Global.lastFramePacketIncreases.RemoveAt(0);
				}
				if (!packetDiagStopwatch.IsRunning) {
					packetDiagStopwatch.Start();
				}
				if (packetDiagStopwatch.ElapsedMilliseconds > 1000) {
					long packetTotalDelta = getPacketsReceived() - Global.packetTotal1SecondAgo;
					Global.packetTotal1SecondAgo = getPacketsReceived();
					packetDiagStopwatch.Restart();
					Global.last10SecondsPacketsReceived.Add(packetTotalDelta);
					if (Global.last10SecondsPacketsReceived.Count > 10) {
						Global.last10SecondsPacketsReceived.RemoveAt(0);
					}
				}
			}
			*/
		}
	}

	public static string getCpuName() {
		string cpuName = "Unknown";
		#if WINDOWS
			// For Windows OS.
			cpuName = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
				@"HARDWARE\DESCRIPTION\System\CentralProcessor\0\"
			)?.GetValue(
				"ProcessorNameString"
			) as String ?? "Windows";
		#endif
		#if LINUX
			cpuName = "Linux";
		#endif
		#if MACOS
			cpuName = "Darwin";
		#endif
		// Fix simbols.
		cpuName = cpuName.Replace("(R)", "®");
		cpuName = cpuName.Replace("(C)", "©");
		cpuName = cpuName.Replace("(TM)", "©"); //Todo, implement proper trademark simbol.
		return cpuName;
	}

	public static void loadMultiThread(List<String> loadText, RenderWindow window, Action loadFunct) {
		Global.isLoading = true;
		Task loadTread = new Task(loadFunct);
		loadTread.ContinueWith(loadExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
		loadTread.Start();
		loadLoop(loadText, window, loadTread);

		Global.isLoading = false;
	}

	public static void loadExceptionHandler(Task task) {
        if (task.Exception != null) {
			Logger.LogFatalException(task.Exception);
		}
    }


	public static void loadLoop(List<String> loadText, RenderWindow window, Task loadTread) {
		// Variables for stuff.
		decimal deltaTime = 0;
		decimal lastUpdateTime = 0;
		decimal fpsLimit = (TimeSpan.TicksPerSecond / 60m);
		bool exit = false;
		Color clearColor = Color.Black;

		// Main loop itself.
		while (window.IsOpen && !exit) {
			DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
			TimeSpan timeSpam = (DateTimeOffset.UtcNow - Global.UnixEpoch);
			long timeNow = timeSpam.Ticks;

			// Framerate calculations.
			deltaTime = (timeNow - lastUpdateTime) / fpsLimit;
			if (deltaTime >= 1) {
			} else {
				continue;
			}
			window.Clear(clearColor);

			for (int i = 0; i < loadText.Count; i++) {
				Fonts.drawText(FontType.Grey, loadText[i], 8, 8 + (10 * i), isLoading: true);
			}
			Fonts.drawText(
				FontType.Grey,
				UtcNow.Day + "/" + UtcNow.Month + "/" + UtcNow.Year + " " +
				UtcNow.ToString("0:hh:mm:sstt", CultureInfo.InvariantCulture),
				8, Global.screenH - 15, isLoading: true
			);

			lastUpdateTime = timeNow;
			window.DispatchEvents();
			window.Display();

			exit = (loadTread.Status >= TaskStatus.RanToCompletion);
		}
	}

	public static void loadLoopRTexture(List<String> loadText, RenderWindow window, (uint width, uint height)[] textures) {
		// Variables for stuff.
		decimal deltaTime = 0;
		decimal lastUpdateTime = 0;
		decimal fpsLimit = (TimeSpan.TicksPerSecond / 60m);
		Color clearColor = Color.Black;
		Stopwatch watch = new Stopwatch();
		int pos = textures.Length - 1;

		// Main loop itself.
		while (window.IsOpen && pos > 0) {
			DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
			TimeSpan timeSpam = (DateTimeOffset.UtcNow - Global.UnixEpoch);
			long timeNow = timeSpam.Ticks;

			// Framerate calculations.
			deltaTime = (timeNow - lastUpdateTime) / fpsLimit;
			if (deltaTime >= 1) {
			} else {
				continue;
			}
			window.Clear(clearColor);

			for (int i = 0; i < loadText.Count; i++) {
				Fonts.drawText(FontType.Grey, loadText[i], 8, 8 + (10 * i), isLoading: true);
			}
			Fonts.drawText(
				FontType.Grey,
				UtcNow.Day + "/" + UtcNow.Month + "/" + UtcNow.Year + " " +
				UtcNow.ToString("0:hh:mm:sstt", CultureInfo.InvariantCulture),
				8, Global.screenH - 15, isLoading: true
			);

			lastUpdateTime = timeNow;
			window.DispatchEvents();
			window.Display();
			watch.Restart();

			for (; pos >= 0; pos--) {
				int encodeKey = ((int)textures[pos].width * 397) ^ (int)textures[pos].height;
				if (!Global.renderTextures.ContainsKey(encodeKey)) {
					Global.renderTextures[encodeKey] = (
						new RenderTexture((uint)textures[pos].width, (uint)textures[pos].height),
						new RenderTexture((uint)textures[pos].width, (uint)textures[pos].height)
					);
				}
				if (watch.ElapsedTicks >= fpsLimit) {
					break;
				}
			}
		}
	}

	public static void drawLoadST(List<String> loadText, RenderWindow window) {
		DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
		Color clearColor = Color.Black;
		window.Clear(clearColor);

		for (int i = 0; i < loadText.Count; i++) {
			Fonts.drawText(FontType.Grey, loadText[i], 8, 8 + (10 * i), isLoading: true);
		}
		Fonts.drawText(
			FontType.Grey,
			UtcNow.Day + "/" + UtcNow.Month + "/" + UtcNow.Year + " " +
			UtcNow.ToString("0:hh:mm:sstt", CultureInfo.InvariantCulture),
			8, Global.screenH - 15, isLoading: true
		);

		window.DispatchEvents();
		window.Display();
	}
}
