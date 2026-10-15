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
///   --soak[=N]            play N computer-only matches headless (default 6) and check for leaks
///   --flowtest            drive every menu and a Teams match with scripted pads, asserting each step
///   --abilities           cast all 16 abilities at an enemy and at a teammate, checking each
///   --teams               with --soak, --screen=demo or --screen=select: Teams mode
///   --players=N           with --soak: 2-4 computer wizards in Free-for-All (default 4)
///   --screen=NAME         start on title | select | stage | battle | demo (--stage=0-3 picks its arena)
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
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			string[] parts = arg.TrimStart('-').Split('=', 2);
			flags[parts[0]] = parts.Length > 1 ? parts[1] : "";
		}

		if (flags.ContainsKey("make-title-glow")) { GetTree().Quit(TitleGlowTool.Run()); return; }
		if (flags.ContainsKey("make-icon")) { GetTree().Quit(IconTool.Run()); return; }
		if (flags.ContainsKey("make-stages")) { GetTree().Quit(StageArtTool.Run()); return; }
		if (flags.ContainsKey("checks"))
		{
			int failures = RegressionChecks.RunAll();
			GetTree().Quit(failures == 0 ? 0 : 1);
			return;
		}

		Sfx.Attach(this);
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
			AddChild(new SoakTest(count, flags.ContainsKey("teams"), players));
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
			case "demo": ShowDemo(flags.ContainsKey("teams"), flags.TryGetValue("stage", out string stage) && int.TryParse(stage, out int s) ? s : 1); break;
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
		GetViewport().SetInputAsHandled();
	}

	// ---- Screens ----

	void Swap(Node next, bool onUi)
	{
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

	public void ShowBattle() => StartBattle(BattleSetup.FromSession(), skipCountdown: false);

	public Battle StartBattle(BattleSetup setup, bool skipCountdown)
	{
		Battle battle = Battle.Create(setup);
		battle.SkipCountdown = skipCountdown;
		// Back to Character Select afterwards, with everyone still joined and locked in, so a
		// rematch is one press of Start. (Unity made everyone join again every match.)
		battle.Finished += _ => CallDeferred(MethodName.ShowCharacterSelect);
		Swap(battle, false);
		return battle;
	}

	/// <summary>A computer-only match for screenshots and attract mode.</summary>
	void ShowDemo(bool teams, int stage)
	{
		BattleSetup setup = new() { Stage = Content.Stages[Mathf.Clamp(stage, 0, Content.Stages.Length - 1)], TeamsActive = teams };
		for (int i = 0; i < 4; i++)
			setup.Fighters.Add(new Fighter { Wizard = Content.Wizards[i], Slot = -1, Spawn = i, Label = "CPU" + (i + 1), Team = teams ? Teams.DefaultForSlot(i) : Teams.NoTeam });
		Battle battle = StartBattle(setup, skipCountdown: false);
		// --charged: everyone starts with a full ultimate, to see them without a long wait.
		if (flags.ContainsKey("charged")) foreach (Wizard wizard in battle.Wizards) wizard.AddUltimateCharge(100.0f);
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
