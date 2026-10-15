using Godot;

/// <summary>
/// The bars over a wizard's head, as in Unity: health, energy (flashing red when a cast is
/// refused for want of energy), a gold ultimate meter that flashes white when ready, and four
/// cooldown pips for the basic, second, third and fourth abilities. In Teams a strip in the team colour
/// runs down the left edge. New in the port: the player's label, so P1-P4 and CPU are obvious.
///
/// Pips read their fill straight from the ability's cooldown; Unity had to learn cooldown
/// lengths by timing them because nothing reported them.
/// </summary>
public partial class WizardHud : Node2D
{
	const float Width = 78.0f;
	const float BarHeight = 7.0f;
	const float Gap = 2.0f;
	const float FizzleFlash = 0.35f;

	static readonly Color Back = new(0.08f, 0.08f, 0.1f, 0.85f);
	static readonly Color HealthColour = new(0.85f, 0.15f, 0.15f);
	static readonly Color EnergyColour = new(0.25f, 0.55f, 1.0f);
	static readonly Color UltimateColour = new(0.85f, 0.65f, 0.15f);
	static readonly Color PipReady = new(0.55f, 0.92f, 1.0f);
	static readonly Color PipCharging = new(0.3f, 0.45f, 0.55f);

	readonly Wizard wizard;
	Font font;

	public WizardHud() { }

	public WizardHud(Wizard wizard)
	{
		this.wizard = wizard;
		ZIndex = 50;
		ZAsRelative = false;
		Position = new Vector2(-Width * 0.5f, -Units.U(1.3f));
	}

	public override void _Ready() => font = Ui.HeadingFont;

	public override void _Process(double delta)
	{
		// Against the top wall the bars would be off screen; drop them below the wizard instead.
		bool nearTop = wizard != null && wizard.GlobalPosition.Y < -Units.U(Battle.ArenaHalf.Y) + Units.U(1.6f);
		Position = new Vector2(-Width * 0.5f, nearTop ? Units.U(1.05f) : -Units.U(1.3f));
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (wizard == null || !wizard.Alive) return;
		double now = wizard.Battle.Time;
		float y = 0.0f;

		Color labelColour = wizard.Team != Teams.NoTeam ? Teams.Colour(wizard.Team) : Colors.White;
		// A dark outline keeps the label readable on any floor, snow included.
		DrawStringOutline(font, new Vector2(0.0f, -4.0f), wizard.Label, HorizontalAlignment.Center, Width, 17, 6, new Color(Ui.Ink, 0.9f));
		DrawString(font, new Vector2(0.0f, -4.0f), wizard.Label, HorizontalAlignment.Center, Width, 17, labelColour);

		Bar(y, wizard.Health / Wizard.MaxHealth, HealthColour);
		y += BarHeight + Gap;

		float fizzle = (float)(now - wizard.LastFizzle);
		Color energy = fizzle < FizzleFlash ? EnergyColour.Lerp(new Color(1.0f, 0.25f, 0.2f), 1.0f - fizzle / FizzleFlash) : EnergyColour;
		Bar(y, wizard.Energy / Wizard.MaxEnergy, energy);
		y += BarHeight + Gap;

		Color ultimate = wizard.UltimateReady
			? new Color(1.0f, 0.85f, 0.2f).Lerp(Colors.White, (Mathf.Sin((float)now * 8.0f) + 1.0f) * 0.5f)
			: UltimateColour;
		Bar(y, wizard.UltimateCharge / 100.0f, ultimate);
		y += BarHeight + Gap + 1.0f;

		const float pipGap = 3.0f, pipHeight = 5.0f;
		const int pips = WizardData.UltimateSlot; // one per ability before the ultimate
		float pipWidth = (Width - pipGap * (pips - 1)) / pips;
		for (int i = 0; i < pips; i++)
		{
			Ability ability = wizard.Abilities[i];
			float x = i * (pipWidth + pipGap);
			DrawRect(new Rect2(x, y, pipWidth, pipHeight), Back);
			float fill = ability.IsReady ? 1.0f : ability.CooldownFill;
			if (fill > 0.0f) DrawRect(new Rect2(x, y, pipWidth * fill, pipHeight), ability.IsReady ? PipReady : PipCharging);
		}

		if (wizard.Team != Teams.NoTeam)
			{
			DrawRect(new Rect2(-8.0f, -1.0f, 7.0f, y + pipHeight + 2.0f), new Color(Ui.Ink, 0.9f));
			DrawRect(new Rect2(-7.0f, 0.0f, 5.0f, y + pipHeight), Teams.Colour(wizard.Team));
		}
	}

	void Bar(float y, float fraction, Color colour)
	{
		DrawRect(new Rect2(0.0f, y, Width, BarHeight), Back);
		DrawRect(new Rect2(0.0f, y, Width * Mathf.Clamp(fraction, 0.0f, 1.0f), BarHeight), colour);
	}
}
