using System;
using System.Collections.Generic;
using Godot;

/// <summary>Who fights in a battle. Built from the Session for a real match, or by hand for tests.</summary>
public sealed class Fighter
{
	public WizardData Wizard;
	/// <summary>Player slot 0-3, or -1 for the computer.</summary>
	public int Slot;
	public int Team = Teams.NoTeam;
	/// <summary>Which of the stage's spawn points to start on (in Teams, rearranged by side).</summary>
	public int Spawn;
	/// <summary>Shown over the wizard and used as the result: P1-P4, or CPU.</summary>
	public string Label = "CPU";
	/// <summary>A device to read, or null for the computer.</summary>
	public IInputSource Input;
	/// <summary>The computer's skill, when <see cref="Input"/> is null.</summary>
	public Difficulty Difficulty = Difficulty.Normal;
}

public sealed class BattleSetup
{
	public StageData Stage;
	public bool TeamsActive;
	/// <summary>What wins a round: knock everyone out, or hold the rune (<see cref="RuneHill"/>).</summary>
	public Goal Goal = Goal.Knockout;
	public readonly List<Fighter> Fighters = new();

	/// <summary>
	/// The match Character Select set up. A lone player gets a computer wizard (one he is not
	/// playing); Teams applies only when <see cref="Session.TeamsPlayable"/>.
	/// </summary>
	public static BattleSetup FromSession()
	{
		BattleSetup setup = new() { Stage = Content.Stages[Mathf.Clamp(Session.StageIndex, 0, Content.Stages.Length - 1)], TeamsActive = Session.TeamsPlayable, Goal = Session.Goal };
		foreach (PlayerSlot slot in Session.Slots)
		{
			if (!slot.Joined) continue;
			setup.Fighters.Add(new Fighter
			{
				Wizard = Content.Wizards[slot.WizardIndex],
				Slot = slot.Index,
				Spawn = slot.Index,
				Label = slot.Label,
				Team = setup.TeamsActive ? slot.Team : Teams.NoTeam,
				Input = new DeviceInputSource(slot.Device),
			});
		}
		if (setup.Fighters.Count == 1) setup.Fighters.Add(new Fighter { Wizard = ComputerWizard(setup.Fighters[0].Wizard), Slot = -1, Spawn = setup.Stage.ComputerSpawn, Difficulty = Session.CpuDifficulty });
		return setup;
	}

	/// <summary>A wizard the human is not playing, so the matchup is not a mirror.</summary>
	public static WizardData ComputerWizard(WizardData avoid)
	{
		List<WizardData> options = new();
		foreach (WizardData wizard in Content.Wizards) if (wizard != avoid) options.Add(wizard);
		return options[GD.RandRange(0, options.Count - 1)];
	}
}

/// <summary>
/// A match in the arena: builds the stage, spawns the wizards, runs the countdown, keeps the
/// registries the effects query (wizards, projectiles, patches, flames, rock walls), and decides
/// the winner. Effects reach it through <see cref="Current"/>; there is only ever one battle.
/// </summary>
public partial class Battle : Node2D
{
	/// <summary>Inner edge of the arena walls, in Unity units (Park walls at x ±10.7, y ±6.5).</summary>
	public static readonly Vector2 ArenaHalf = new(10.2f, 5.95f);
	const float ResultSeconds = 5.0f, RoundResultSeconds = 3.5f;

	public static Battle Current { get; private set; }

	public double Time { get; private set; }
	/// <summary>False during the countdown and after the result: wizards ignore input.</summary>
	public bool Fighting { get; private set; }
	public bool Over { get; private set; }
	/// <summary>Ultimates cast this battle, for the soak test's balance report.</summary>
	public int UltimatesCast { get; set; }
	/// <summary>When the result came, in battle seconds (0 while fighting).</summary>
	public double EndedAt => Over ? resultAt : 0.0;

	public readonly List<Wizard> Wizards = new();
	public readonly List<Projectile> Projectiles = new();
	public readonly List<GroundPatch> Patches = new();
	public readonly List<FireSource> FireSources = new();
	public readonly List<RockWall> RockWalls = new();
	/// <summary>The stage's lava or bog, if it has one.</summary>
	public StageHazard Hazard { get; private set; }
	/// <summary>King of the Rune's circle, or null in a knock-out battle.</summary>
	public RuneHill Rune { get; private set; }
	public BattleSetup Setup => setup;

	/// <summary>Ends the round with this result (King of the Rune decides its own rounds).</summary>
	public void EndRound(string result)
	{
		if (!Over) EndMatch(result);
	}

	/// <summary>Adds a control over the arena, on the same layer as the countdown and results.</summary>
	public void AddOverlay(Control control) => overlay.AddChild(control);

	/// <summary>Raised once when the result screen has finished, with the result text.</summary>
	public event Action<string> Finished;
	/// <summary>The result, once there is one: a label like "P2", "CPU", "GOLD", or "DRAW".</summary>
	public string Result { get; private set; }

	BattleSetup setup;
	Node2D effects;
	CanvasLayer overlay;
	double countdownStart, resultAt;
	int countdownShown = -1;
	bool finishedRaised;
	TextureRect countdownImage;
	readonly List<Projectile> spentProjectiles = new();
	CircleShape2D probe;
	Camera2D camera;
	float shakeStrength, shakeLeft, shakeLength;
	PhysicsShapeQueryParameters2D query;

	/// <summary>Skip the 3-2-1 (tests and soak runs).</summary>
	public bool SkipCountdown;

	/// <summary>The match this battle is a round of, or null (tests and soak runs play single battles).</summary>
	public Match Match;

	bool RoundsMatter => Match != null && Match.RoundsToWin > 1;

	public static Battle Create(BattleSetup setup)
	{
		// Y-sorted, so wizards and cover pieces draw in depth order: stand behind a stump and
		// the stump hides you.
		// Pausable, so the pause menu (and GameRoot, which runs always) can freeze it.
		Battle battle = new() { setup = setup, Name = "Battle", YSortEnabled = true, ProcessMode = ProcessModeEnum.Pausable };
		return battle;
	}

	public override void _EnterTree() => Current = this;

	public override void _ExitTree()
	{
		if (Current == this) Current = null;
		query?.Dispose();
		probe?.Dispose();
		query = null;
		probe = null;
	}

	public override void _Ready()
	{
		BuildArena();
		effects = new Node2D { Name = "Effects" };
		AddChild(effects);
		SpawnWizards();

		overlay = new CanvasLayer { Layer = 10 };
		AddChild(overlay);
		if (setup.Goal == Goal.KingOfTheRune)
		{
			Rune = RuneHill.Create(this);
			AddChild(Rune);
		}
		countdownImage = Centred(overlay, null, Vector2.Zero);
		if (RoundsMatter) ShowRoundTitle();

		Sfx.Music("res://assets/music/battle.wav");
		countdownStart = 0.0;
		if (SkipCountdown) { Fighting = true; countdownImage.Visible = false; }
	}

	// ---- Arena ----

	void BuildArena()
	{
		StageData stage = setup.Stage;
		camera = new Camera2D { Name = "Camera" };
		AddChild(camera);

		// The background covers the whole view for every common screen shape: with "expand" the
		// view is 1920 wide up to 1440 tall (4:3), or 1080 tall up to 2560 wide (21:9). The
		// scale is fixed rather than fitted to the window so the floor art sits in the same
		// place on every screen, which is what cover placement relies on.
		Sprite2D background = new() { Texture = stage.Background, ZIndex = -100 };
		background.Scale = Vector2.One * BackgroundScale(stage.Background.GetSize());
		AddChild(background);

		StaticBody2D walls = new() { Name = "Walls", CollisionLayer = 1u << 0, CollisionMask = 0 };
		Vector2 half = Units.U(ArenaHalf.X, -ArenaHalf.Y); // positive x and y in pixels
		const float thickness = 400.0f;
		AddWall(walls, new Vector2(0, -half.Y - thickness * 0.5f), new Vector2(half.X * 2 + thickness * 2, thickness));
		AddWall(walls, new Vector2(0, half.Y + thickness * 0.5f), new Vector2(half.X * 2 + thickness * 2, thickness));
		AddWall(walls, new Vector2(-half.X - thickness * 0.5f, 0), new Vector2(thickness, half.Y * 2 + thickness * 2));
		AddWall(walls, new Vector2(half.X + thickness * 0.5f, 0), new Vector2(thickness, half.Y * 2 + thickness * 2));
		AddChild(walls);

		// Outside the walls the floor is dimmed: on a 16:9 screen the arena almost fills the view, but
		// a taller (16:10, 3:2) or wider one shows floor nobody can reach, and it should look it.
		Color dim = new(0.02f, 0.01f, 0.05f, 0.55f);
		Vector2 outer = CoveredView;
		AddDim(new Rect2(-outer.X, -outer.Y, outer.X * 2, outer.Y - half.Y), dim);
		AddDim(new Rect2(-outer.X, half.Y, outer.X * 2, outer.Y - half.Y), dim);
		AddDim(new Rect2(-outer.X, -half.Y, outer.X - half.X, half.Y * 2), dim);
		AddDim(new Rect2(half.X, -half.Y, outer.X - half.X, half.Y * 2), dim);

		if (stage.HazardKind.Length > 0)
		{
			Hazard = StageHazard.Create(stage, this);
			AddChild(Hazard);
		}
		// The floors are only ground; each stage's cover is the one thing that blocks wizards and shots.
		// Each piece gets its own variant (on Storm Plateau, its own rune).
		for (int i = 0; i < stage.Cover.Length; i++) AddChild(Cover.Create(stage.CoverKind, Units.U(stage.Cover[i]), i));
	}

	/// <summary>The view the background must cover, in pixels: 21:9 wide and 4:3 tall at once.</summary>
	public static readonly Vector2 CoveredView = new(2560, 1440);

	/// <summary>Scale for a stage background so it covers <see cref="CoveredView"/>.</summary>
	public static float BackgroundScale(Vector2 textureSize) => Mathf.Max(CoveredView.X / textureSize.X, CoveredView.Y / textureSize.Y);

	void AddDim(Rect2 rect, Color colour) =>
		AddChild(new Polygon2D { Polygon = new[] { rect.Position, new Vector2(rect.End.X, rect.Position.Y), rect.End, new Vector2(rect.Position.X, rect.End.Y) }, Color = colour, ZIndex = -90 });

	static void AddWall(StaticBody2D body, Vector2 position, Vector2 size) =>
		body.AddChild(new CollisionShape2D { Position = position, Shape = new RectangleShape2D { Size = size } });

	/// <summary>
	/// Whether a circle overlaps anything solid: an arena wall, an obstacle, a rock wall or a
	/// lightning rod (everything on the solids layer). Wizards are not solid for this.
	/// </summary>
	public bool IsInsideSolid(Vector2 point, float radius)
	{
		// One probe shape and query per battle, reused for every call (this runs many times a
		// frame) and released in _ExitTree. Leaving them to the .NET garbage collector meant
		// their physics shapes could still be alive at exit.
		probe ??= new CircleShape2D();
		query ??= new PhysicsShapeQueryParameters2D { Shape = probe, CollisionMask = 1u << 0, CollideWithBodies = true, CollideWithAreas = false };
		probe.Radius = Mathf.Max(1.0f, radius);
		query.Transform = new Transform2D(0.0f, point);
		return GetWorld2D().DirectSpaceState.IntersectShape(query, 1).Count > 0;
	}

	/// <summary>Whether nothing solid (cover, a rock wall, a rod) stands between two points.</summary>
	public bool HasLineOfSight(Vector2 from, Vector2 to)
	{
		using PhysicsRayQueryParameters2D ray = PhysicsRayQueryParameters2D.Create(from, to, 1u << 0);
		return GetWorld2D().DirectSpaceState.IntersectRay(ray).Count == 0;
	}

	// ---- Wizards ----

	void SpawnWizards()
	{
		StageData stage = setup.Stage;
		Vector2[] spawns = setup.TeamsActive ? TeamSpawns(stage) : stage.Spawns;
		foreach (Fighter fighter in setup.Fighters)
		{
			spawnPoints[fighter] = Units.U(spawns[fighter.Spawn % spawns.Length]);
			Spawn(fighter);
		}
	}

	/// <summary>Where each fighter starts, and comes back to in King of the Rune.</summary>
	readonly Dictionary<Fighter, Vector2> spawnPoints = new();
	readonly List<(Fighter fighter, double at)> respawns = new();
	/// <summary>Seconds a knocked-out wizard waits to come back, in King of the Rune.</summary>
	public const double RespawnSeconds = 3.0;

	Wizard Spawn(Fighter fighter)
	{
		CpuInputSource cpu = fighter.Input == null ? new CpuInputSource(fighter.Difficulty) : null;
		Wizard wizard = Wizard.Create(this, fighter.Wizard, fighter.Slot, fighter.Label, fighter.Team, (IInputSource)cpu ?? fighter.Input, spawnPoints[fighter]);
		cpu?.Attach(wizard);
		Wizards.Add(wizard);
		AddChild(wizard);
		return wizard;
	}

	/// <summary>King of the Rune: knocked-out wizards come back at their start after a short wait.</summary>
	void Respawn()
	{
		for (int i = respawns.Count - 1; i >= 0; i--)
		{
			if (Time < respawns[i].at) continue;
			Wizard wizard = Spawn(respawns[i].fighter);
			respawns.RemoveAt(i);
			wizard.Modulate = new Color(1, 1, 1, 0);
			wizard.CreateTween().TweenProperty(wizard, "modulate:a", 1.0f, 0.35);
			Sfx.Play("res://assets/sfx/speedup.wav", -8.0f);
		}
	}

	/// <summary>
	/// Teammates start together: Gold takes the player 1 and 3 spawns and Pink the player 2 and 4
	/// spawns; a team with more than two players spills into whatever the other side left.
	/// </summary>
	Vector2[] TeamSpawns(StageData stage)
	{
		List<Vector2>[] sides = { new() { stage.Spawns[0], stage.Spawns[2] }, new() { stage.Spawns[1], stage.Spawns[3] } };
		Vector2[] spawns = (Vector2[])stage.Spawns.Clone();
		List<Fighter> overflow = new();
		foreach (Fighter fighter in setup.Fighters)
		{
			List<Vector2> side = sides[fighter.Team == Teams.Gold ? 0 : 1];
			if (side.Count == 0) { overflow.Add(fighter); continue; }
			spawns[fighter.Spawn] = side[0];
			side.RemoveAt(0);
		}
		foreach (Fighter fighter in overflow)
		{
			List<Vector2> side = sides[0].Count > 0 ? sides[0] : sides[1];
			spawns[fighter.Spawn] = side[0];
			side.RemoveAt(0);
		}
		return spawns;
	}

	public void OnWizardDied(Wizard wizard)
	{
		if (Over) return;
		if (setup.Goal == Goal.KingOfTheRune)
		{
			foreach (Fighter fighter in setup.Fighters)
				if (fighter.Label == wizard.Label) respawns.Add((fighter, Time + RespawnSeconds));
			return;
		}
		List<Wizard> alive = new();
		foreach (Wizard w in Wizards) if (w.Alive) alive.Add(w);

		if (setup.TeamsActive)
		{
			HashSet<int> teams = new();
			foreach (Wizard w in alive) teams.Add(w.Team);
			if (teams.Count > 1) return;
			EndMatch(teams.Count == 0 ? "DRAW" : Teams.Name(alive[0].Team).ToUpper());
			return;
		}

		if (alive.Count > 1) return;
		EndMatch(alive.Count == 0 ? "DRAW" : alive[0].Label);
	}

	void EndMatch(string result)
	{
		Over = true;
		Fighting = false;
		Result = result;
		resultAt = Time;
		int round = Match?.Round ?? 1;
		Match?.Record(result);
		GD.Print($"Battle: result {result} after {Time:0.0}s" + (RoundsMatter ? $" (round {round}, match {(Match.Decided ? Match.Winner : "goes on")})" : ""));
		ShowResult(result, round);
	}

	/// <summary>An image centred on screen at a fixed size, whatever the window shape.</summary>
	static TextureRect Centred(Node parent, Texture2D texture, Vector2 size)
	{
		CenterContainer centre = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		TextureRect image = new()
		{
			Texture = texture,
			CustomMinimumSize = size,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		};
		centre.AddChild(image);
		parent.AddChild(centre);
		return image;
	}

	void ShowResult(string result, int round)
	{
		Control panel = new();
		panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		overlay.AddChild(panel);
		// Dim the arena behind the result so it stands out.
		ColorRect dim = new() { Color = new Color(0, 0, 0, 0.45f), MouseFilter = Control.MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panel.AddChild(dim);

		// Partway through a match: who took the round, and the score so far.
		if (RoundsMatter && !Match.Decided)
		{
			string name = result.StartsWith("CPU") ? "THE COMPUTER" : result == "GOLD" || result == "PINK" ? result + " TEAM" : result;
			string line = result == "DRAW" ? $"ROUND {round}: DRAW" : $"{name} WINS ROUND {round}";
			panel.AddChild(Banner.Create(line, result == "DRAW" ? new Color(0.85f, 0.55f, 1.0f) : Match.ColourOf(result), 96));
			panel.AddChild(Scoreboard());
			return;
		}

		// The match is over. The four players have authored win art; the computer, teams and draws are drawn.
		if (RoundsMatter) panel.AddChild(Scoreboard());
		if (result.Length == 2 && result[0] == 'P')
		{
			Centred(panel, Content.Texture($"res://assets/art/ui/p{result[1]}-wins.png"), new Vector2(1050, 300));
			return;
		}

		string text = result switch
		{
			"DRAW" => "DRAW",
			_ when result.StartsWith("CPU") => "THE COMPUTER WINS",
			_ => result + " TEAM WINS",
		};
		Color colour = result == "GOLD" ? Teams.Colour(Teams.Gold) : result == "PINK" ? Teams.Colour(Teams.Pink) : new Color(0.85f, 0.55f, 1.0f);
		panel.AddChild(Banner.Create(text, colour));
	}

	/// <summary>"ROUND 2 - BEST OF 3" across the top during the countdown, then it fades.</summary>
	void ShowRoundTitle()
	{
		// Below King of the Rune's scoreboard when there is one.
		int drop = setup.Goal == Goal.KingOfTheRune ? 110 : 0;
		Label title = Ui.Heading($"ROUND {Match.Round}", 64, Ui.Gold);
		title.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		title.OffsetTop = 40 + drop;
		title.OffsetBottom = 130 + drop;
		overlay.AddChild(title);
		Label best = Ui.Label($"BEST OF {Match.RoundsToWin * 2 - 1}", 26, new Color(1, 1, 1, 0.8f));
		best.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		best.OffsetTop = 128 + drop;
		best.OffsetBottom = 170 + drop;
		overlay.AddChild(best);
		Tween fade = CreateTween();
		fade.TweenInterval(3.3);
		fade.TweenProperty(title, "modulate:a", 0.0f, 0.4);
		fade.Parallel().TweenProperty(best, "modulate:a", 0.0f, 0.4);
	}

	/// <summary>
	/// The match score under the result: a pill per side in its colour (wizard or team) with its
	/// round wins as pips, and "FIRST TO 2" above.
	/// </summary>
	Control Scoreboard()
	{
		CenterContainer centre = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		VBoxContainer column = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 12);
		centre.AddChild(column);
		column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 330) });
		Label first = Ui.Heading($"FIRST TO {Match.RoundsToWin}", 26, Ui.Gold);
		column.AddChild(first);
		HBoxContainer row = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 18);
		column.AddChild(row);
		foreach (string side in Match.Sides)
		{
			int wins = Match.Wins(side);
			string pips = new string('●', wins) + new string('○',Mathf.Max(0, Match.RoundsToWin - wins));
			Panel pill = Ui.Pill(new Rect2(0, 0, 200, 56), $"{(side.StartsWith("CPU") ? "CPU" : side)}  {pips}", 24, Match.ColourOf(side), Ui.Ink, heading: true);
			pill.CustomMinimumSize = pill.Size;
			row.AddChild(pill);
		}
		return centre;
	}

	// ---- Camera shake ----

	/// <summary>
	/// Shakes the view for a moment, strongest at the start: meteor impacts, ultimates going off,
	/// an earthquake's jolts. A stronger shake overrides a weaker one still running.
	/// </summary>
	public void Shake(float pixels, float seconds)
	{
		if (shakeLeft > 0.0f && pixels * seconds < shakeStrength * shakeLeft) return;
		shakeStrength = pixels;
		shakeLeft = shakeLength = seconds;
	}

	public override void _Process(double delta)
	{
		if (camera == null) return;
		shakeLeft = Mathf.Max(0.0f, shakeLeft - (float)delta);
		float amount = shakeLeft > 0.0f ? shakeStrength * shakeLeft / shakeLength : 0.0f;
		camera.Offset = new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0)) * amount;
	}

	// ---- Frame ----

	public override void _PhysicsProcess(double delta)
	{
		Time += delta;
		Wizards.RemoveAll(w => !GodotObject.IsInstanceValid(w));
		Projectiles.RemoveAll(p => !GodotObject.IsInstanceValid(p) || p.IsQueuedForDeletion());

		if (!Fighting && !Over && !SkipCountdown) RunCountdown();
		CheckPause();
		if (Fighting && !Over) Respawn();
		if (Over && !finishedRaised && Time - resultAt >= (RoundsMatter && !Match.Decided ? RoundResultSeconds : ResultSeconds))
		{
			finishedRaised = true;
			Finished?.Invoke(Result);
		}
	}

	public void OpenPause(int device, string who) => PauseMenu.Open(overlay, device, who);

	/// <summary>Start from any player in the battle pauses it, and that player drives the menu.</summary>
	void CheckPause()
	{
		if (Over) return;
		foreach (Fighter fighter in setup.Fighters)
		{
			if (fighter.Input is not DeviceInputSource device || !Devices.Get(device.Device).StartPressed) continue;
			OpenPause(device.Device, fighter.Label);
			return;
		}
	}

	// Three, two, one - a door slam each - then "Fight!" while the battle starts.
	void RunCountdown()
	{
		int step = (int)(Time - countdownStart);
		if (step == countdownShown) return;
		countdownShown = step;
		if (step >= 3)
		{
			Fighting = true;
			countdownImage.Texture = Content.Texture("res://assets/art/ui/fight.png");
			countdownImage.CustomMinimumSize = new Vector2(651, 160);
			Sfx.Play("res://assets/sfx/doorslam.wav");
			Tween hide = CreateTween();
			hide.TweenInterval(0.7);
			hide.TweenProperty(countdownImage, "modulate:a", 0.0f, 0.3);
			return;
		}
		countdownImage.Texture = Content.Texture($"res://assets/art/ui/count-{3 - step}.png");
		countdownImage.CustomMinimumSize = new Vector2(216, 278);
		Sfx.Play("res://assets/sfx/doorslam.wav");
	}

	// ---- Registries ----

	public void AddEffect(Node node) => effects.AddChild(node);

	/// <summary>Effect nodes still in the arena (projectiles, patches, flames, walls, ultimates).</summary>
	public int EffectCount => effects.GetChildCount();

	public string DescribeEffects()
	{
		List<string> names = new();
		foreach (Node child in effects.GetChildren()) names.Add(child.GetType().Name);
		return string.Join(", ", names);
	}

	public void AddProjectile(Projectile projectile)
	{
		Projectiles.Add(projectile);
		effects.AddChild(projectile);
	}

	public void RemoveProjectile(Projectile projectile) { /* swept from the list next frame */ }

	public void AddPatch(GroundPatch patch) { if (!Patches.Contains(patch)) Patches.Add(patch); }
	public void RemovePatch(GroundPatch patch) => Patches.Remove(patch);
}
