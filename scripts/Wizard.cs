using Godot;

/// <summary>
/// One wizard in a battle: a human player or the computer. It owns health, energy, ultimate
/// charge, burn and stun, moves itself, and dispatches its four abilities. It reads a single
/// <see cref="IInputSource"/> and cannot tell a pad, the keyboard or the AI apart.
///
/// Health, energy, speed and ultimate charge come from data/balance.tres (<see cref="BalanceData"/>);
/// distances are in Unity units (see <see cref="Units"/>).
/// </summary>
public partial class Wizard : CharacterBody2D
{
	const float TargeterRange = 4.5f;        // Unity units
	const float SpriteUnityScale = 0.15f;    // the scale ParkBattle spawned players at

	static BalanceData Balance => Content.Balance;
	public static float MaxHealth => Balance.MaxHealth;
	public static float MaxEnergy => Balance.MaxEnergy;

	/// <summary>Body hit box, from the Unity player collider (0.9 x 1.41 units at spawn scale).</summary>
	public static readonly Vector2 BodySize = new(Units.U(0.9f), Units.U(1.41f));

	public WizardData Data { get; private set; }
	public int Team { get; private set; } = Teams.NoTeam;
	/// <summary>Player slot 0-3, or -1 for the computer.</summary>
	public int SlotIndex { get; private set; }
	public bool IsComputer => SlotIndex < 0;
	/// <summary>P1-P4 or CPU: shown over the wizard, and the battle result when he wins.</summary>
	public string Label { get; private set; } = "";
	public IInputSource Controller { get; private set; }

	public float Health { get; private set; } = MaxHealth;
	public float Energy { get; private set; } = MaxEnergy;
	public float UltimateCharge { get; private set; }
	public bool UltimateReady { get; private set; }
	/// <summary>An ultimate is running; every other ability is locked out until it ends.</summary>
	public bool UltimateActive { get; set; }
	public bool Alive { get; private set; } = true;

	public readonly StatusEffects Status = new();
	public Ability[] Abilities { get; private set; }
	public InputState CurrentInput { get; private set; }

	/// <summary>Last direction aimed in (unit). Starts facing the arena centre.</summary>
	public Vector2 AimDirection { get; private set; } = Vector2.Right;
	/// <summary>Where the targeter is: the point placed abilities land on.</summary>
	public Vector2 TargetPoint { get; private set; }
	public bool Aiming { get; private set; }

	/// <summary>The time a cast was last refused for lack of energy, so the HUD can flash.</summary>
	public double LastFizzle { get; private set; } = -10.0;

	public float StunRemaining { get; private set; }
	public bool Stunned => StunRemaining > 0.0f;
	public bool OnFire => burnRemaining > 0.0f;

	float burnRemaining, burnDamage, burnTick, hurtFlash;
	Vector2 slideDirection;
	Sprite2D body, targeter, fireSprite, iceCube;
	double fireFlipAt;

	Battle battle;

	public static Wizard Create(Battle battle, WizardData data, int slotIndex, string label, int team, IInputSource input, Vector2 position)
	{
		Wizard wizard = new()
		{
			battle = battle,
			Data = data,
			SlotIndex = slotIndex,
			Label = label,
			Team = team,
			Controller = input,
			Position = position,
			Name = label,
			CollisionLayer = 1u << 1,           // wizards
			CollisionMask = (1u << 0) | (1u << 1), // solids and other wizards
			MotionMode = MotionModeEnum.Floating,
			ZIndex = 2,
		};
		wizard.Build();
		return wizard;
	}

	void Build()
	{
		// The body collider is the lower part of the sprite, so a wizard can stand with his
		// hat in front of a wall the way he could in Unity.
		AddChild(new CollisionShape2D
		{
			Shape = new CapsuleShape2D { Radius = Units.U(0.36f), Height = Units.U(1.0f) },
			Position = new Vector2(0.0f, Units.U(0.15f)),
		});

		body = new Sprite2D { Texture = Data.Portrait, Scale = Vector2.One * Units.SpriteScale(SpriteUnityScale) };
		AddChild(body);

		if (Team != Teams.NoTeam)
		{
			// A disc in the team colour under the wizard, so sides read in the arena.
			Sprite2D marker = ProceduralFx.Shadow.NewSprite(0.55f);
			Color colour = Teams.Colour(Team);
			marker.Modulate = new Color(colour.R, colour.G, colour.B, 0.75f);
			marker.Position = new Vector2(0.0f, Units.U(0.75f));
			marker.ZIndex = -1;
			AddChild(marker);
		}

		targeter = new Sprite2D
		{
			Texture = Data.Targeter,
			Scale = Vector2.One * Units.SpriteScale(0.2f),
			Visible = false,
			TopLevel = true,
			ZIndex = 1,
		};
		AddChild(targeter);

		Abilities = AbilityFactory.Create(this, Data);
		AddChild(new WizardHud(this));

		// Face the middle of the arena to start with, so a first shot goes somewhere useful.
		if (Position.LengthSquared() > 1.0f) AimDirection = -Position.Normalized();
		TargetPoint = Position;
	}

	public Battle Battle => battle;

	// ---- Combat state ----

	public bool SpendEnergy(float amount)
	{
		if (Energy < amount)
		{
			LastFizzle = battle.Time;
			return false;
		}
		Energy -= amount;
		AddUltimateCharge(amount * Balance.UltimateChargePerEnergy);
		return true;
	}

	public void TakeDamage(float amount)
	{
		if (!Alive || amount <= 0.0f) return;
		Health -= amount;
		AddUltimateCharge(amount * Balance.UltimateChargePerDamageTaken);
		hurtFlash = 0.18f;
		if (Health <= 0.0f) Die();
	}

	public void AddUltimateCharge(float amount)
	{
		if (UltimateReady || amount <= 0.0f) return;
		UltimateCharge = Mathf.Min(100.0f, UltimateCharge + amount);
		if (UltimateCharge >= 100.0f) UltimateReady = true;
	}

	public bool TryUseUltimate()
	{
		if (!UltimateReady) return false;
		UltimateReady = false;
		UltimateCharge = 0.0f;
		return true;
	}

	/// <summary>Sets the burn. A new fire replaces the old one's length and damage, as in Unity.</summary>
	public void SetFire(float duration, float damagePerTick)
	{
		if (!Alive) return;
		if (burnRemaining <= 0.0f) burnTick = 0.5f;
		burnRemaining = duration;
		burnDamage = damagePerTick;
		if (fireSprite == null)
		{
			fireSprite = new Sprite2D { Texture = Content.Texture("res://assets/art/attacks/on-fire.png"), Scale = Vector2.One * Units.SpriteScale(0.35f), ZIndex = 1 };
			AddChild(fireSprite);
		}
	}

	public void PutOutFire()
	{
		burnRemaining = 0.0f;
		fireSprite?.QueueFree();
		fireSprite = null;
	}

	/// <summary>Frozen solid: no movement or casting. Unlike Unity, a stun does not hide a burn.</summary>
	public void Stun(float seconds)
	{
		if (!Alive) return;
		StunRemaining = Mathf.Max(StunRemaining, seconds);
		if (iceCube == null)
		{
			iceCube = new Sprite2D
			{
				Texture = Content.Texture("res://assets/art/attacks/ice-cube.png"),
				Scale = new Vector2(Units.SpriteScale(0.3f), Units.SpriteScale(0.4f)),
				Position = new Vector2(0.0f, Units.U(0.1f)),
				ZIndex = 1,
				Modulate = new Color(1, 1, 1, 0.85f),
			};
			AddChild(iceCube);
		}
	}

	public void Die()
	{
		if (!Alive) return;
		Alive = false;
		Health = 0.0f;
		Status.Clear();
		Sfx.Play("res://assets/sfx/die.wav");
		foreach (Ability ability in Abilities) ability.OnOwnerDied();
		battle.OnWizardDied(this);
		targeter.QueueFree();
		QueueFree();
	}

	// ---- Geometry ----

	/// <summary>Where the wizard's feet are: what ground patches test against.</summary>
	public Vector2 Feet => GlobalPosition + new Vector2(0.0f, Units.U(0.55f));

	/// <summary>Whether a circle touches the body hit box.</summary>
	public bool HitTest(Vector2 point, float radius)
	{
		Vector2 centre = GlobalPosition + new Vector2(0.0f, Units.U(0.05f));
		Vector2 half = BodySize * 0.5f;
		Vector2 closest = new(Mathf.Clamp(point.X, centre.X - half.X, centre.X + half.X), Mathf.Clamp(point.Y, centre.Y - half.Y, centre.Y + half.Y));
		return closest.DistanceSquaredTo(point) <= radius * radius;
	}

	// ---- Frame ----

	public override void _PhysicsProcess(double delta)
	{
		if (!Alive) return;
		float dt = (float)delta;

		InputState input = Controller.Poll();
		bool fighting = battle.Fighting;
		if (!fighting || Stunned) input = InputState.None;
		CurrentInput = input;

		Status.Prune();
		TickStun(dt);
		TickBurn(dt);
		if (!Alive) return;

		if (!Status.NoRegen) Energy = Mathf.Min(MaxEnergy, Energy + Balance.EnergyRegen * dt);
		if (fighting) AddUltimateCharge(Balance.UltimateChargeRate * dt);

		// A brief red flash on every hit, so damage reads even without numbers.
		hurtFlash = Mathf.Max(0.0f, hurtFlash - dt);
		body.Modulate = hurtFlash > 0.0f ? new Color(1.0f, 0.45f, 0.45f) : Colors.White;

		UpdateAim(input);
		Move(input, dt);

		if (fighting && !Stunned) CastAbilities(input);
		foreach (Ability ability in Abilities) ability.Tick(dt, input);
	}

	void TickStun(float dt)
	{
		if (StunRemaining <= 0.0f) return;
		StunRemaining -= dt;
		if (StunRemaining <= 0.0f)
		{
			StunRemaining = 0.0f;
			iceCube?.QueueFree();
			iceCube = null;
		}
	}

	// Burn ticks every half second, as Unity did.
	void TickBurn(float dt)
	{
		if (burnRemaining <= 0.0f)
		{
			if (fireSprite != null) PutOutFire();
			return;
		}
		if (fireSprite != null && battle.Time >= fireFlipAt)
		{
			fireSprite.FlipH = !fireSprite.FlipH;
			fireFlipAt = battle.Time + 0.3;
		}
		burnTick -= dt;
		if (burnTick > 0.0f) return;
		burnTick += 0.5f;
		burnRemaining -= 0.5f;
		TakeDamage(burnDamage);
	}

	void UpdateAim(InputState input)
	{
		Aiming = input.Aim.LengthSquared() > 0.04f;
		if (Aiming) AimDirection = input.Aim.Normalized();
		// The targeter sits along the stick, as far out as the stick is pushed. A resting
		// stick puts placed abilities at the wizard's feet, as in Unity, but projectiles
		// keep flying the way you last aimed instead of always to the right.
		TargetPoint = GlobalPosition + input.Aim.LimitLength(1.0f) * Units.U(TargeterRange);
		// The computer gets no targeter, as in Unity: it would only clutter the arena.
		targeter.Visible = Aiming && Alive && !IsComputer;
		targeter.GlobalPosition = TargetPoint;
	}

	void Move(InputState input, float dt)
	{
		Vector2 direction = input.Move.LimitLength(1.0f);
		// On ice a moving wizard is committed to the way he was going.
		if (Status.Sliding && slideDirection != Vector2.Zero) direction = slideDirection;
		else slideDirection = direction;

		Velocity = direction * Units.U(Balance.MoveSpeed) * Status.SpeedMultiplier;
		MoveAndSlide();
		// Walls stop a slide rather than pinning you against them for the patch's lifetime.
		if (GetSlideCollisionCount() > 0 && Status.Sliding) slideDirection = Vector2.Zero;
	}

	void CastAbilities(InputState input)
	{
		if (UltimateActive) return;
		if (input.BasicHeld) Abilities[0].TryCast();
		if (input.SecondHeld) Abilities[1].TryCast();
		if (input.ThirdPressed) Abilities[2].TryCast();
		if (input.UltimatePressed) Abilities[3].TryCast();
	}
}
