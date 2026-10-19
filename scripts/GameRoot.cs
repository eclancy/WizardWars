using System.Collections.Generic;
using Godot;

/// <summary>
/// The root of the game. Screens (title, character select, stage select, battle) are swapped
/// in and out as children, so nothing has to survive a scene change; match state lives in
/// <see cref="Session"/>. Devices are polled here once per physics step, before any screen or
/// wizard reads them.
///
/// Command-line flags, passed after "--":
///   --checks              run RegressionChecks, exit with the failure count
///   --make-title-glow     rebuild assets/art/ui/title-start-glow.png from title.png (see TitleGlowTool)
///   --make-icon           rebuild icon.png and icon.ico: a "WW" from the logo above a fireball (see IconTool)
///   --make-stages         repaint the Frost Tundra, Scorched Canyon and Storm Plateau floors (see StageArtTool)
///   --make-sounds         synthesise the newer sound effects into assets/sfx (see SoundTool)
///   --soak[=N]            play N computer-only matches headless (default 6) and check for leaks
///   --flowtest            drive every menu and a Teams match with scripted pads, asserting each step
///   --abilities           cast all 20 abilities at an enemy and at a teammate, checking each
///   --teams               with --soak, --screen=demo or --screen=select: Teams mode
///   --king                with --soak or --screen=demo: King of the Rune instead of knock-out
///   --players=N           with --soak: 2-4 computer wizards in Free-for-All (default 4)
///   --screen=NAME         start on title | select | stage | battle | demo | pause | options (--stage=0-3 picks its arena)
///   --charged             with --screen=demo: every wizard starts with a full ultimate
///   --shot=PATH           save a screenshot to PATH after --shot-after seconds (default 3), then quit;
///                         --shot-size=WxH sets the window (default 1920x1080)
///   --no-update           an exported Windows build skips its update check (see docs/releasing.md)
/// </summary>
public partial class GameRoot : Node2D
{
	public static GameRoot Instance { get; private set; }

	Node current;
	public Node CurrentScreen => current;
	CanvasLayer ui;
	ColorRect fade;
	Tween fadeTween;
	readonly Dictionary<string, string> flags = new();

	public override void _Ready()
	{
		Instance = this;
		// GameRoot keeps running while a battle is paused, so devices are still read for the menu.
		ProcessMode = ProcessModeEnum.Always;
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			string[] parts = arg.TrimStart('-').Split('=', 2);
			flags[parts[0]] = parts.Length > 1 ? parts[1] : "";
		}

		if (flags.ContainsKey("make-title-glow")) { GetTree().Quit(TitleGlowTool.Run()); return; }
		if (flags.ContainsKey("make-icon")) { GetTree().Quit(IconTool.Run()); return; }
		if (flags.ContainsKey("make-stages")) { GetTree().Quit(StageArtTool.Run()); return; }
		if (flags.ContainsKey("make-sounds")) { GetTree().Quit(SoundTool.Run()); return; }
		if (flags.ContainsKey("checks"))
		{
			int failures = RegressionChecks.RunAll();
			GetTree().Quit(failures == 0 ? 0 : 1);
			return;
		}

		// A real run loads the player's options; tests and screenshots keep the defaults.
		bool testing = flags.ContainsKey("flowtest") || flags.ContainsKey("abilities") || flags.ContainsKey("soak") || flags.ContainsKey("shot");
		if (!testing) Settings.Load();
		Sfx.Attach(this);
		Settings.ApplyWindow();
		ui = new CanvasLayer { Name = "UI", Layer = 5 };
		AddChild(ui);
		// Above everything: a black sheet that fades away each time the screen changes.
		CanvasLayer fadeLayer = new() { Name = "Fade", Layer = 100 };
		fade = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		fadeLayer.AddChild(fade);
		AddChild(fadeLayer);
		Devices.Update();

		if (flags.ContainsKey("flowtest")) { ShowTitle(); AddChild(new FlowTest()); return; }
		if (flags.ContainsKey("abilities")) { AddChild(new AbilityTest()); return; }
		if (flags.ContainsKey("soak"))
		{
			int count = flags.TryGetValue("soak", out string n) && int.TryParse(n, out int parsed) ? parsed : 6;
			int players = flags.TryGetValue("players", out string p) && int.TryParse(p, out int parsedPlayers) ? parsedPlayers : 4;
			AddChild(new SoakTest(count, flags.ContainsKey("teams"), players, flags.ContainsKey("king")));
			return;
		}
		if (flags.TryGetValue("shot", out string shotPath))
		{
			// Screenshots are a 1920x1080 window by default, whatever the monitor: the game itself
			// starts fullscreen, which would capture at the screen's own resolution. --shot-size
			// picks another shape (1620x1080 is 3:2, 2520x1080 is 21:9) to check layouts on it.
			Vector2I shotSize = new(1920, 1080);
			if (flags.TryGetValue("shot-size", out string sizeText) && sizeText.Split('x') is [string w, string h] && int.TryParse(w, out int sw) && int.TryParse(h, out int sh))
				shotSize = new Vector2I(sw, sh);
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			DisplayServer.WindowSetSize(shotSize);
			ScheduleScreenshot(shotPath, flags.TryGetValue("shot-after", out string after) ? after.ToFloat() : 3.0f);
		}

		switch (flags.TryGetValue("screen", out string screen) ? screen : "title")
		{
			case "select": DemoJoin(); Session.TeamsMode = flags.ContainsKey("teams"); ShowCharacterSelect(); break;
			case "stage": DemoJoin(); ShowStageSelect(); break;
			case "battle": DemoJoin(); ShowBattle(); break;
			case "pause": DemoJoin(); ShowBattle(); OpenPauseLater(); break;
			case "options": DemoJoin(); ShowCharacterSelect(); OptionsPanel.Open(current, Devices.Keyboard, null); break;
			case "demo": ShowDemo(flags.ContainsKey("teams"), flags.ContainsKey("king"), flags.TryGetValue("stage", out string stage) && int.TryParse(stage, out int s) ? s : 1); break;
			// A downloaded copy checks ecec.dev for a newer version before anything else.
			default:
				if (UpdateScreen.ShouldCheck()) Swap(new UpdateScreen { Name = "Update" }, true);
				else ShowTitle();
				break;
		}
	}

	public override void _PhysicsProcess(double delta) => Devices.Update();

	// The game starts fullscreen (project setting). F11 or Alt+Enter flips to a window and back.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
		bool toggle = key.PhysicalKeycode == Key.F11 || (key.PhysicalKeycode == Key.Enter && key.AltPressed);
		if (!toggle) return;
		bool fullscreen = DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
		DisplayServer.WindowSetMode(fullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
		Settings.NoteWindowMode(!fullscreen);
		GetViewport().SetInputAsHandled();
	}

	// ---- Screens ----

	void Swap(Node next, bool onUi)
	{
		GetTree().Paused = false;
		current?.QueueFree();
		current = next;
		// A screen must fill the real view. Its anchors are set before it enters the tree:
		// setting them later, in its own _Ready, left the screen with zero size.
		if (next is Control screen) screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		if (onUi) ui.AddChild(next); else AddChild(next);

		// Every new screen fades in from black.
		fadeTween?.Kill();
		fade.Modulate = Colors.White;
		fadeTween = CreateTween();
		fadeTween.TweenProperty(fade, "modulate:a", 0.0f, 0.35).SetTrans(Tween.TransitionType.Sine);
	}

	public void ShowTitle() => Swap(new TitleScreen { Name = "Title" }, true);
	public void ShowCharacterSelect() => Swap(new CharacterSelectScreen { Name = "CharacterSelect" }, true);
	public void ShowStageSelect() => Swap(new StageSelectScreen { Name = "StageSelect" }, true);

	/// <summary>The match being played: its rounds, score and fighters.</summary>
	Match match;

	/// <summary>A new match from Character Select and Stage Select: best of 1, 3 or 5 (<see cref="Session.RoundsToWin"/>).</summary>
	public void ShowBattle()
	{
		match = new Match(BattleSetup.FromSession(), Session.RoundsToWin);
		StartRound();
	}

	/// <summary>The next round of the match, on the same stage with the same fighters.</summary>
	public void StartRound() => StartBattle(match.Setup, skipCountdown: false, match);

	/// <summary>The pause menu's Rematch: the whole match again from round 1.</summary>
	public void Rematch()
	{
		if (match == null) { ShowBattle(); return; }
		match = new Match(match.Setup, match.RoundsToWin);
		CallDeferred(MethodName.StartRound);
	}

	public Battle StartBattle(BattleSetup setup, bool skipCountdown, Match ofMatch = null)
	{
		Battle battle = Battle.Create(setup);
		battle.SkipCountdown = skipCountdown;
		battle.Match = ofMatch;
		// The next round, or once the match is decided, back to Character Select with everyone
		// still joined and locked in, so a rematch is one press of Start. (Unity made everyone
		// join again every match.)
		battle.Finished += _ => CallDeferred(ofMatch != null && !ofMatch.Decided ? MethodName.StartRound : MethodName.ShowCharacterSelect);
		Swap(battle, false);
		return battle;
	}

	/// <summary>A computer-only match for screenshots and attract mode.</summary>
	void ShowDemo(bool teams, bool king, int stage)
	{
		BattleSetup setup = new() { Stage = Content.Stages[Mathf.Clamp(stage, 0, Content.Stages.Length - 1)], TeamsActive = teams, Goal = king ? Goal.KingOfTheRune : Goal.Knockout };
		for (int i = 0; i < 4; i++)
			setup.Fighters.Add(new Fighter { Wizard = Content.Wizards[i], Slot = -1, Spawn = i, Label = "CPU" + (i + 1), Team = teams ? Teams.DefaultForSlot(i) : Teams.NoTeam });
		Battle battle = StartBattle(setup, skipCountdown: false);
		// --charged: everyone starts with a full ultimate, to see them without a long wait.
		if (flags.ContainsKey("charged")) foreach (Wizard wizard in battle.Wizards) wizard.AddUltimateCharge(100.0f);
	}

	/// <summary>For --screen=pause: the pause menu, opened by player 1 once the countdown is over.</summary>
	async void OpenPauseLater()
	{
		await ToSignal(GetTree().CreateTimer(3.5), SceneTreeTimer.SignalName.Timeout);
		if (current is Battle battle) battle.OpenPause(Devices.Keyboard, "P1");
	}

	/// <summary>For --screen shots of the menus: the keyboard joins as player 1 with the Fire wizard locked in.</summary>
	static void DemoJoin()
	{
		PlayerSlot slot = Session.Slots[0];
		slot.Joined = true;
		slot.Device = Devices.Keyboard;
		slot.Locked = true;
		slot.WizardIndex = 0;
	}

	async void ScheduleScreenshot(string path, float seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		Image image = GetViewport().GetTexture().GetImage();
		Error error = image.SavePng(path);
		GD.Print(error == Error.Ok ? $"Screenshot: {path}" : $"Screenshot failed: {error}");
		GetTree().Quit();
	}
}
