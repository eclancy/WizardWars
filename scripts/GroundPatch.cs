using Godot;

/// <summary>
/// An ice or mud patch on the ground. Each one first plays a formation telegraph
/// (<see cref="Form"/>) and does nothing until it finishes, so a patch can never catch anyone
/// before it visibly exists.
///
/// While active, every wizard whose feet are inside gets this patch's modifier (slide or slow),
/// keyed to the patch itself, and loses it on stepping out. When the patch is freed its
/// modifiers are pruned automatically (see <see cref="StatusEffects"/>) - Unity's IcePatch had
/// to release players by hand because destroying a trigger never fires its own exit event.
///
/// Teammates of the caster are spared the slide or slow, but the ice and mud still put their
/// fire out. The caster is affected by his own patch, as in Unity.
/// </summary>
public abstract partial class GroundPatch : Node2D
{
	/// <summary>Half-size of the patch art in Unity units at scale 1 (561 x 305 px at 100 ppu).</summary>
	static readonly Vector2 ArtHalfUnits = new(5.61f * 0.5f, 3.05f * 0.5f);
	/// <summary>The Unity polygon colliders hugged the art, a little inside its edge.</summary>
	const float ColliderInset = 0.85f;

	public Wizard Caster { get; private set; }
	public int CasterTeam { get; private set; }
	public bool Active { get; private set; }
	protected float UnityScale = 1.0f;
	protected Sprite2D Art;
	protected float FormElapsed;
	float lifetime, age;
	bool removed;

	protected abstract string ArtPath { get; }
	protected abstract Modifier EffectOn { get; }

	/// <summary>Advances the formation telegraph; returns true once the patch is fully formed.</summary>
	protected abstract bool Form(float dt);

	protected void Setup(Wizard caster, Vector2 position, float unityScale, float activeLifetime)
	{
		Caster = caster;
		CasterTeam = caster.Team;
		UnityScale = unityScale;
		lifetime = activeLifetime;
		Position = position;
		ZIndex = 0;
		Art = new Sprite2D { Texture = Content.Texture(ArtPath), Scale = Vector2.One * Units.SpriteScale(unityScale), Visible = false };
		AddChild(Art);
	}

	public override void _Ready() => Battle.Current.AddPatch(this);

	public override void _ExitTree() => Battle.Current?.RemovePatch(this);

	/// <summary>Radii of the patch's effective ellipse, in pixels.</summary>
	public Vector2 Radii => ArtHalfUnits * UnityScale * Units.PixelsPerUnit * ColliderInset;

	public bool Contains(Vector2 point)
	{
		Vector2 radii = Radii;
		Vector2 d = point - GlobalPosition;
		return d.X * d.X / (radii.X * radii.X) + d.Y * d.Y / (radii.Y * radii.Y) <= 1.0f;
	}

	/// <summary>Whether two patches overlap, approximated by either centre being inside the other.</summary>
	public bool Overlaps(GroundPatch other) => Contains(other.GlobalPosition) || other.Contains(GlobalPosition);

	public override void _PhysicsProcess(double delta)
	{
		if (removed) return;
		float dt = (float)delta;
		if (!Active)
		{
			FormElapsed += dt;
			if (Form(dt)) Active = true;
			return;
		}

		age += dt;
		if (age >= lifetime) { Remove(); return; }

		foreach (Wizard wizard in Battle.Current.Wizards)
		{
			if (!wizard.Alive) continue;
			bool inside = Contains(wizard.Feet);
			bool teammate = wizard != Caster && Teams.Allied(CasterTeam, wizard.Team);
			if (inside) wizard.PutOutFire();
			if (inside && !teammate) wizard.Status.Set(this, EffectOn);
			else wizard.Status.Remove(this);
		}
		Interact();
	}

	/// <summary>What this patch does to other patches each frame.</summary>
	protected virtual void Interact() { }

	public void Remove()
	{
		if (removed) return;
		removed = true;
		Active = false;
		foreach (Wizard wizard in Battle.Current.Wizards) wizard.Status.Remove(this);
		QueueFree();
	}
}
