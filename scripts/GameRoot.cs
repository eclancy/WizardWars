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
///   --soak[=N]            play N computer-only matches headless (default 6) and check for leaks
///   --flowtest            drive every menu and a Teams match with scripted pads, asserting each step
///   --abilities           cast all 16 abilities at an enemy and at a teammate, checking each
///   --teams               with --soak or --demo: 2v2 Teams
///   --screen=NAME         start on title | select | stage | battle | demo
///   --shot=PATH           save a screenshot to PATH after --shot-after seconds (default 3), then quit
/// </summary>
public partial class GameRoot : Node2D
{
	public static GameRoot Instance { get; private set; }

	Node current;
	public Node CurrentScreen => current;
	CanvasLayer ui;
	readonly Dictionary<string, string> flags = new();

	public override void _Ready()
	{
		Instance = this;
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			string[] parts = arg.TrimStart('-').Split('=', 2);
			flags[parts[0]] = parts.Length > 1 ? parts[1] : "";
		}

		if (flags.ContainsKey("checks"))
		{
			int failures = RegressionChecks.RunAll();
			GetTree().Quit(failures == 0 ? 0 : 1);
			return;
		}

		Sfx.Attach(this);
		ui = new CanvasLayer { Name = "UI", Layer = 5 };
		AddChild(ui);
		Devices.Update();

		if (flags.ContainsKey("flowtest")) { ShowTitle(); AddChild(new FlowTest()); return; }
		if (flags.ContainsKey("abilities")) { AddChild(new AbilityTest()); return; }
		if (flags.ContainsKey("soak")) { AddChild(new SoakTest(flags.TryGetValue("soak", out string n) && int.TryParse(n, out int count) ? count : 6, flags.ContainsKey("teams"))); return; }
		if (flags.TryGetValue("shot", out string shotPath)) ScheduleScreenshot(shotPath, flags.TryGetValue("shot-after", out string after) ? after.ToFloat() : 3.0f);

		switch (flags.TryGetValue("screen", out string screen) ? screen : "title")
		{
			case "select": DemoJoin(); ShowCharacterSelect(); break;
			case "stage": DemoJoin(); ShowStageSelect(); break;
			case "battle": DemoJoin(); ShowBattle(); break;
			case "demo": ShowDemo(flags.ContainsKey("teams")); break;
			default: ShowTitle(); break;
		}
	}

	public override void _PhysicsProcess(double delta) => Devices.Update();

	// ---- Screens ----

	void Swap(Node next, bool onUi)
	{
		current?.QueueFree();
		current = next;
		if (onUi) ui.AddChild(next); else AddChild(next);
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
	void ShowDemo(bool teams)
	{
		BattleSetup setup = new() { Stage = Content.Stages[1], TeamsActive = teams };
		for (int i = 0; i < 4; i++)
			setup.Fighters.Add(new Fighter { Wizard = Content.Wizards[i], Slot = -1, Spawn = i, Label = "CPU" + (i + 1), Team = teams ? Teams.DefaultForSlot(i) : Teams.NoTeam });
		StartBattle(setup, skipCountdown: false);
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
