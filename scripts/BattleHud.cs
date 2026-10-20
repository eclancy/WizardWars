using System.Collections.Generic;
using Godot;

/// <summary>
/// Each fighter's state in a strip along the bottom of the screen, below the arena, so nothing
/// over the wizards hides the fight. One panel per fighter, edged in their wizard's colour (and
/// in Teams, a strip of their team's): a portrait, the player label and wizard, health with its
/// number, energy (flashing red when a cast is refused for want of it), and a chip per ability
/// named by its button. A chip fills as its cooldown runs, dims when there is not enough energy
/// to cast it, and the ultimate chip (X) fills with charge and flashes when ready.
///
/// Panels find their wizard by label every frame, so a King of the Rune respawn (a new wizard
/// node) carries on in the same panel. A knocked-out fighter's panel greys out, and counts down
/// to the respawn when there is one.
/// </summary>
public partial class BattleHud : Control
{
	/// <summary>Screen pixels the strip takes at the bottom; the battle camera keeps the arena above it (Battle._Process).</summary>
	public const float Height = 132.0f;

	const float PanelHeight = 114.0f;
	const float MaxPanelWidth = 440.0f;
	const float Gap = 16.0f;
	const float FizzleFlash = 0.35f;

	static readonly string[] Buttons = { "RT", "LT", "RB", "LB", "X" };
	static readonly Color Back = new(0.02f, 0.01f, 0.05f, 0.9f);
	static readonly Color HealthColour = new(0.9f, 0.18f, 0.2f);
	static readonly Color HealthLow = new(1.0f, 0.45f, 0.2f);
	static readonly Color EnergyColour = new(0.25f, 0.55f, 1.0f);
	static readonly Color ChipReady = new(0.3f, 0.62f, 0.78f);
	static readonly Color ChipCharging = new(0.2f, 0.3f, 0.4f);
	static readonly Color UltimateColour = new(0.85f, 0.65f, 0.15f);

	readonly Battle battle;
	readonly List<Fighter> fighters = new();

	public BattleHud() { }

	public BattleHud(Battle battle, IEnumerable<Fighter> fighters)
	{
		this.battle = battle;
		this.fighters.AddRange(fighters);
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.BottomWide);
		OffsetTop = -Height;
		OffsetBottom = 0.0f;
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		if (battle == null || fighters.Count == 0) return;
		int n = fighters.Count;
		float width = Mathf.Min(MaxPanelWidth, (Size.X - 32.0f - Gap * (n - 1)) / n);
		float left = (Size.X - (width * n + Gap * (n - 1))) * 0.5f;
		float top = Size.Y - PanelHeight - 8.0f;
		for (int i = 0; i < n; i++)
			DrawPanel(fighters[i], new Rect2(left + i * (width + Gap), top, width, PanelHeight));
	}

	Wizard Find(Fighter fighter)
	{
		Wizard found = null;
		foreach (Wizard wizard in battle.Wizards)
			if (IsInstanceValid(wizard) && wizard.Label == fighter.Label) found = wizard;
		return found;
	}

	void DrawPanel(Fighter fighter, Rect2 rect)
	{
		Wizard wizard = Find(fighter);
		bool knockedOut = wizard == null || !wizard.Alive;
		double now = battle.Time;
		Font heading = Ui.HeadingFont, body = Ui.BodyFont;
		Color colour = fighter.Wizard.Colour;

		DrawStyleBox(Ui.Style(new Color(Ui.PanelColour, 0.92f), knockedOut ? new Color(colour, 0.35f) : colour, 3, 14), rect);
		if (fighter.Team != Teams.NoTeam)
			DrawRect(new Rect2(rect.Position + new Vector2(3, 14), new Vector2(7, rect.Size.Y - 28)), Teams.Colour(fighter.Team));

		// The portrait: the wizard's hat and face, cropped from the full figure.
		Texture2D portrait = fighter.Wizard.Portrait;
		if (portrait != null)
		{
			Vector2 size = portrait.GetSize();
			Rect2 crop = new(size.X * 0.27f, size.Y * 0.09f, size.X * 0.5f, size.Y * 0.52f);
			Rect2 frame = new(rect.Position + new Vector2(16, 10), new Vector2(86, 94));
			DrawRect(frame, new Color(colour, knockedOut ? 0.08f : 0.2f));
			DrawTextureRectRegion(portrait, frame, crop, knockedOut ? new Color(0.45f, 0.45f, 0.5f, 0.7f) : Colors.White);
		}

		float x = rect.Position.X + 114.0f, right = rect.End.X - 14.0f, w = right - x;
		float y = rect.Position.Y + 10.0f;

		// The player, then the wizard in its colour.
		Color labelColour = knockedOut ? new Color(1, 1, 1, 0.5f) : Colors.White;
		DrawString(heading, new Vector2(x, y + 24), fighter.Label, HorizontalAlignment.Left, -1, 24, labelColour);
		float labelWidth = heading.GetStringSize(fighter.Label, HorizontalAlignment.Left, -1, 24).X;
		string name = fighter.Wizard.DisplayName.Replace(" Wizard", "").ToUpper();
		DrawString(body, new Vector2(x + labelWidth + 10, y + 23), name, HorizontalAlignment.Left, w - labelWidth - 10, 18, knockedOut ? new Color(colour, 0.5f) : colour.Lightened(0.25f));
		y += 32.0f;

		if (knockedOut)
		{
			double back = battle.RespawnIn(fighter);
			string text = back > 0.0 ? $"BACK IN {Mathf.CeilToInt((float)back)}" : "KNOCKED OUT";
			DrawString(heading, new Vector2(x, y + 44), text, HorizontalAlignment.Center, w, 26, new Color(1, 1, 1, 0.55f));
			return;
		}

		// Health, with its number.
		float health = wizard.Health / Wizard.MaxHealth;
		Bar(new Rect2(x, y, w, 22), health, health < 0.3f ? HealthColour.Lerp(HealthLow, (Mathf.Sin((float)now * 9.0f) + 1.0f) * 0.5f) : HealthColour);
		DrawString(heading, new Vector2(x, y + 18), Mathf.CeilToInt(wizard.Health).ToString(), HorizontalAlignment.Right, w - 6, 17, Colors.White);
		y += 26.0f;

		float fizzle = (float)(now - wizard.LastFizzle);
		Color energy = fizzle < FizzleFlash ? EnergyColour.Lerp(new Color(1.0f, 0.25f, 0.2f), 1.0f - fizzle / FizzleFlash) : EnergyColour;
		Bar(new Rect2(x, y, w, 9), wizard.Energy / Wizard.MaxEnergy, energy);
		y += 14.0f;

		// One chip per ability, named by its button; the last is the ultimate.
		const float chipGap = 6.0f;
		float chipWidth = (w - chipGap * (Buttons.Length - 1)) / Buttons.Length;
		for (int i = 0; i < Buttons.Length; i++)
		{
			Rect2 chip = new(x + i * (chipWidth + chipGap), y, chipWidth, 24);
			Ability ability = wizard.Abilities[i];
			bool ultimate = i == WizardData.UltimateSlot;
			float fill;
			Color fillColour;
			bool usable;
			if (ultimate)
			{
				fill = wizard.UltimateCharge / 100.0f;
				fillColour = wizard.UltimateReady ? new Color(1.0f, 0.85f, 0.2f).Lerp(Colors.White, (Mathf.Sin((float)now * 8.0f) + 1.0f) * 0.5f) : UltimateColour;
				usable = wizard.UltimateReady;
			}
			else
			{
				bool affordable = wizard.Energy >= ability.Data.Cost;
				fill = ability.IsReady ? 1.0f : ability.CooldownFill;
				usable = ability.IsReady && affordable;
				fillColour = ability.IsReady ? (affordable ? ChipReady : ChipCharging) : ChipCharging;
			}
			Bar(chip, fill, fillColour);
			DrawString(heading, chip.Position + new Vector2(0, 19), Buttons[i], HorizontalAlignment.Center, chip.Size.X, 16,
				usable ? (ultimate ? Ui.Ink : Colors.White) : new Color(1, 1, 1, 0.45f));
		}
	}

	void Bar(Rect2 rect, float fraction, Color colour)
	{
		DrawRect(rect, Back);
		DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X * Mathf.Clamp(fraction, 0.0f, 1.0f), rect.Size.Y)), colour);
	}
}
