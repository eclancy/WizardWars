using Godot;

public enum ProjectileKind { Fireball, Icicle, Mudball, ElectricityBall }

/// <summary>
/// A basic-attack bolt. It flies straight, and stops at the first thing it meets:
///  - an enemy wizard (damage, gone);
///  - another wizard's projectile (both gone - every element cancels every other, as in Unity);
///  - anything solid: a wall, an obstacle, a rock wall, a lightning rod.
/// It passes through its caster and the caster's teammates.
///
/// Hits are tested with geometry each frame rather than physics callbacks, so they behave
/// identically headless and there are no collision masks to get wrong.
/// </summary>
public partial class Projectile : Node2D
{
	/// <summary>Art and hit circle per kind, from the Unity prefabs (sizes in Unity units).</summary>
	readonly struct Look
	{
		public readonly string Sheet;
		public readonly int Frames;
		public readonly float UnityScale;
		public readonly float HitRadius;
		public readonly float HitForward;

		public Look(string sheet, int frames, float unityScale, float hitRadius, float hitForward)
		{
			Sheet = sheet;
			Frames = frames;
			UnityScale = unityScale;
			HitRadius = hitRadius;
			HitForward = hitForward;
		}
	}

	static Look LookFor(ProjectileKind kind) => kind switch
	{
		ProjectileKind.Fireball => new Look("res://assets/art/attacks/fireball-sheet.png", 4, 0.0523f, 0.30f, 0.45f),
		ProjectileKind.Mudball => new Look("res://assets/art/attacks/mudball-sheet.png", 4, 0.0523f, 0.30f, 0.45f),
		ProjectileKind.ElectricityBall => new Look("res://assets/art/attacks/electricity-ball-sheet.png", 4, 0.0523f, 0.30f, 0.02f),
		_ => new Look("res://assets/art/attacks/icicle.png", 1, 0.0291f, 0.16f, 0.5f),
	};

	public Wizard Caster { get; private set; }
	public int CasterTeam { get; private set; }
	float damage, speed, life, hitRadius, hitForward;
	Vector2 direction;
	bool spent;

	public static Projectile Fire(Wizard caster, AbilityData data, ProjectileKind kind, Vector2 direction)
	{
		Look look = LookFor(kind);
		Projectile shot = new()
		{
			Caster = caster,
			CasterTeam = caster.Team,
			damage = data.Damage,
			speed = Units.U(data.Speed),
			life = data.Duration,
			hitRadius = Units.U(look.HitRadius),
			hitForward = Units.U(look.HitForward),
			direction = direction.Normalized(),
			Position = caster.GlobalPosition,
			Rotation = direction.Angle(),
			ZIndex = 3,
		};

		if (look.Frames > 1)
		{
			AnimatedSprite2D sprite = new() { SpriteFrames = Sheets.Column(look.Sheet, look.Frames, 10.0f), Scale = Vector2.One * Units.SpriteScale(look.UnityScale) };
			sprite.Play("default");
			shot.AddChild(sprite);
		}
		else
		{
			shot.AddChild(new Sprite2D { Texture = Content.Texture(look.Sheet), Scale = Vector2.One * Units.SpriteScale(look.UnityScale) });
		}

		caster.Battle.AddProjectile(shot);
		return shot;
	}

	Vector2 HitPoint => GlobalPosition + direction * hitForward;

	public override void _PhysicsProcess(double delta)
	{
		if (spent) return;
		Battle battle = Battle.Current;
		float dt = (float)delta;
		life -= dt;
		if (life <= 0.0f) { Vanish(); return; }

		Position += direction * speed * dt;
		Vector2 point = HitPoint;

		foreach (Wizard wizard in battle.Wizards)
		{
			if (!Combat.CanAffect(Caster, CasterTeam, wizard) || !wizard.HitTest(point, hitRadius)) continue;
			wizard.TakeDamage(damage);
			Vanish();
			return;
		}

		foreach (Projectile other in battle.Projectiles)
		{
			if (other == this || other.spent || other.Caster == Caster) continue;
			// A teammate's shot is not an enemy shot to cancel against.
			if (Teams.Allied(CasterTeam, other.CasterTeam)) continue;
			float reach = hitRadius + other.hitRadius;
			if (point.DistanceSquaredTo(other.HitPoint) > reach * reach) continue;
			other.Vanish();
			Vanish();
			return;
		}

		if (battle.IsInsideSolid(point, hitRadius * 0.5f)) Vanish();
	}

	public void Vanish()
	{
		if (spent) return;
		spent = true;
		Battle.Current?.RemoveProjectile(this);
		QueueFree();
	}
}

/// <summary>The four basic attacks: a free projectile on a short cooldown, fired while held.</summary>
public sealed class ProjectileAbility : Ability
{
	readonly ProjectileKind kind;

	public ProjectileAbility(Wizard owner, AbilityData data, ProjectileKind kind) : base(owner, data) => this.kind = kind;

	protected override void Cast() => Projectile.Fire(Owner, Data, kind, Owner.AimDirection);
}
