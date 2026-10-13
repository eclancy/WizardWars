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
}

public sealed class BattleSetup
{
	public StageData Stage;
	public bool TeamsActive;
	public readonly List<Fighter> Fighters = new();

	/// <summary>
	/// The match Character Select set up. A lone player gets a computer wizard (one he is not
	/// playing); Teams applies only when <see cref="Session.TeamsPlayable"/>.
	/// </summary>
	public static BattleSetup FromSession()
	{
		BattleSetup setup = new() { Stage = Content.Stages[Mathf.Clamp(Session.StageIndex, 0, Content.Stages.Length - 1)], TeamsActive = Session.TeamsPlayable };
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
		if (setup.Fighters.Count == 1) setup.Fighters.Add(new Fighter { Wizard = ComputerWizard(setup.Fighters[0].Wizard), Slot = -1, Spawn = setup.Stage.ComputerSpawn });
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
	const float ResultSeconds = 5.0f;

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

	/// <summary>Raised once when the result screen has finished, with the result text.</summary>
	public event Action<string> Finished;
	/// <summary>The result, once there is one: a label like "P2", "CPU", "RED", or "DRAW".</summary>
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
	PhysicsShapeQueryParameters2D query;

	/// <summary>Skip the 3-2-1 (tests and soak runs).</summary>
	public bool SkipCountdown;

	public static Battle Create(BattleSetup setup)
	{
		Battle battle = new() { setup = setup, Name = "Battle" };
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
		countdownImage = Centred(overlay, null, Vector2.Zero);

		Sfx.Music("res://assets/music/battle.wav");
		countdownStart = 0.0;
		if (SkipCountdown) { Fighting = true; countdownImage.Visible = false; }
	}

	// ---- Arena ----

	void BuildArena()
	{
		StageData stage = setup.Stage;
		AddChild(new Camera2D { Name = "Camera" });

		// Cover the whole screen (the window can be wider than 16:9 with "expand").
		Sprite2D background = new() { Texture = stage.Background, ZIndex = -100 };
		Vector2 size = stage.Background.GetSize();
		float cover = Mathf.Max(Units.U(Units.ArenaSize.X) * 1.15f / size.X, Units.U(Units.ArenaSize.Y) * 1.15f / size.Y);
		background.Scale = Vector2.One * cover;
		AddChild(background);

		StaticBody2D walls = new() { Name = "Walls", CollisionLayer = 1u << 0, CollisionMask = 0 };
		Vector2 half = Units.U(ArenaHalf.X, -ArenaHalf.Y); // positive x and y in pixels
		const float thickness = 400.0f;
		AddWall(walls, new Vector2(0, -half.Y - thickness * 0.5f), new Vector2(half.X * 2 + thickness * 2, thickness));
		AddWall(walls, new Vector2(0, half.Y + thickness * 0.5f), new Vector2(half.X * 2 + thickness * 2, thickness));
		AddWall(walls, new Vector2(-half.X - thickness * 0.5f, 0), new Vector2(thickness, half.Y * 2 + thickness * 2));
		AddWall(walls, new Vector2(half.X + thickness * 0.5f, 0), new Vector2(thickness, half.Y * 2 + thickness * 2));
		AddChild(walls);

		foreach (Vector2 rock in stage.Rocks) AddObstacle("res://assets/art/stages/rock.png", Units.U(rock), Units.U(0.55f), Units.U(0.05f));
		foreach (Vector2 cactus in stage.Cacti) AddObstacle("res://assets/art/stages/cactus.png", Units.U(cactus), Units.U(0.5f), Units.U(0.1f));
	}

	static void AddWall(StaticBody2D body, Vector2 position, Vector2 size) =>
		body.AddChild(new CollisionShape2D { Position = position, Shape = new RectangleShape2D { Size = size } });

	void AddObstacle(string art, Vector2 position, float radius, float lowerBy)
	{
		StaticBody2D obstacle = new() { Position = position, CollisionLayer = 1u << 0, CollisionMask = 0, ZIndex = 2 };
		obstacle.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = radius }, Position = new Vector2(0, lowerBy) });
		obstacle.AddChild(new Sprite2D { Texture = Content.Texture(art), Scale = Vector2.One * Units.SpriteScale(1.0f) });
		AddChild(obstacle);
	}

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

	// ---- Wizards ----

	void SpawnWizards()
	{
		StageData stage = setup.Stage;
		Vector2[] spawns = setup.TeamsActive ? TeamSpawns(stage) : stage.Spawns;
		foreach (Fighter fighter in setup.Fighters)
		{
			Vector2 unitySpawn = spawns[fighter.Spawn % spawns.Length];
			CpuInputSource cpu = fighter.Input == null ? new CpuInputSource() : null;
			Wizard wizard = Wizard.Create(this, fighter.Wizard, fighter.Slot, fighter.Label, fighter.Team, (IInputSource)cpu ?? fighter.Input, Units.U(unitySpawn));
			cpu?.Attach(wizard);
			Wizards.Add(wizard);
			AddChild(wizard);
		}
	}

	/// <summary>
	/// Teammates start together: Red takes the player 1 and 3 spawns and Blue the player 2 and 4
	/// spawns; a team with more than two players spills into whatever the other side left.
	/// </summary>
	Vector2[] TeamSpawns(StageData stage)
	{
		List<Vector2>[] sides = { new() { stage.Spawns[0], stage.Spawns[2] }, new() { stage.Spawns[1], stage.Spawns[3] } };
		Vector2[] spawns = (Vector2[])stage.Spawns.Clone();
		List<Fighter> overflow = new();
		foreach (Fighter fighter in setup.Fighters)
		{
			List<Vector2> side = sides[fighter.Team == Teams.Red ? 0 : 1];
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
		GD.Print($"Battle: result {result} after {Time:0.0}s");
		ShowResult(result);
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

	void ShowResult(string result)
	{
		Control panel = new();
		panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		overlay.AddChild(panel);

		// The four players have authored win art; the computer, teams and draws are drawn.
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
		Color colour = result == "RED" ? Teams.Colour(Teams.Red) : result == "BLUE" ? Teams.Colour(Teams.Blue) : new Color(0.85f, 0.55f, 1.0f);
		panel.AddChild(Banner.Create(text, colour));
	}

	// ---- Frame ----

	public override void _PhysicsProcess(double delta)
	{
		Time += delta;
		Wizards.RemoveAll(w => !GodotObject.IsInstanceValid(w));
		Projectiles.RemoveAll(p => !GodotObject.IsInstanceValid(p) || p.IsQueuedForDeletion());

		if (!Fighting && !Over && !SkipCountdown) RunCountdown();
		if (Over && !finishedRaised && Time - resultAt >= ResultSeconds)
		{
			finishedRaised = true;
			Finished?.Invoke(Result);
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
