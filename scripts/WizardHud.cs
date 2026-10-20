using Godot;

/// <summary>
/// The player's label over a wizard's head (P1-P4 or CPU), in their team's colour in Teams, so
/// everyone can find themselves. Everything else - health, energy, cooldowns, the ultimate -
/// lives in the <see cref="BattleHud"/> strip below the arena, out of the way of the fight.
/// </summary>
public partial class WizardHud : Node2D
{
	const float Width = 78.0f;

	readonly Wizard wizard;
	Font font;

	public WizardHud() { }

	public WizardHud(Wizard wizard)
	{
		this.wizard = wizard;
		ZIndex = 50;
		ZAsRelative = false;
		Position = new Vector2(-Width * 0.5f, -Units.U(1.0f));
	}

	public override void _Ready() => font = Ui.HeadingFont;

	public override void _Process(double delta)
	{
		// Against the top wall the label would be off the arena; put it below the wizard instead.
		bool nearTop = wizard != null && wizard.GlobalPosition.Y < -Units.U(Battle.ArenaHalf.Y) + Units.U(1.3f);
		Position = new Vector2(-Width * 0.5f, nearTop ? Units.U(1.15f) : -Units.U(1.0f));
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (wizard == null || !wizard.Alive) return;
		Color labelColour = wizard.Team != Teams.NoTeam ? Teams.Colour(wizard.Team) : Colors.White;
		// A dark outline keeps the label readable on any floor, snow included.
		DrawStringOutline(font, Vector2.Zero, wizard.Label, HorizontalAlignment.Center, Width, 19, 6, new Color(Ui.Ink, 0.9f));
		DrawString(font, Vector2.Zero, wizard.Label, HorizontalAlignment.Center, Width, 19, labelColour);
	}
}
