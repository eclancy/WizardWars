using Godot;

/// <summary>
/// King of the Rune's scores across the top of the screen: a bar per side in its colour, filling
/// toward <see cref="RuneHill.Target"/> seconds, with the side's name and its seconds. The holder's
/// bar is lit; "CONTESTED!" shows while two sides fight over the rune.
/// </summary>
public partial class RuneScoreboard : Control
{
	const float BarWidth = 220.0f, BarHeight = 26.0f, Gap = 16.0f, Top = 14.0f;

	readonly RuneHill hill;
	readonly Battle battle;
	Font font;

	public RuneScoreboard() { }

	public RuneScoreboard(RuneHill hill, Battle battle)
	{
		this.hill = hill;
		this.battle = battle;
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.TopWide);
		OffsetBottom = 110;
	}

	public override void _Ready() => font = Ui.HeadingFont;

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		if (hill == null || !GodotObject.IsInstanceValid(hill)) return;
		var sides = RuneHill.Sides(battle.Setup);
		float width = sides.Count * BarWidth + (sides.Count - 1) * Gap;
		float x = (Size.X - width) * 0.5f;
		Color ink = new(Ui.Ink, 0.85f);

		string title = hill.Holder == null ? "CONTESTED!" : $"KING OF THE RUNE - FIRST TO {RuneHill.Target:0}";
		DrawStringOutline(font, new Vector2(0, Top + 18), title, HorizontalAlignment.Center, Size.X, 20, 6, ink);
		DrawString(font, new Vector2(0, Top + 18), title, HorizontalAlignment.Center, Size.X, 20, hill.Holder == null ? new Color(1.0f, 0.35f, 0.3f) : Ui.Gold);

		float y = Top + 30;
		foreach (string side in sides)
		{
			float score = hill.Scores.TryGetValue(side, out float s) ? s : 0.0f;
			Color colour = hill.ColourOf(side);
			bool holding = hill.Holder == side;
			DrawRect(new Rect2(x - 3, y - 3, BarWidth + 6, BarHeight + 6), holding ? Colors.White : ink);
			DrawRect(new Rect2(x, y, BarWidth, BarHeight), new Color(0.08f, 0.07f, 0.12f, 0.9f));
			DrawRect(new Rect2(x, y, BarWidth * Mathf.Clamp(score / RuneHill.Target, 0.0f, 1.0f), BarHeight), colour);
			string label = $"{(side.StartsWith("CPU") && side.Length == 3 ? "CPU" : side)}   {Mathf.FloorToInt(score)}";
			DrawStringOutline(font, new Vector2(x, y + 20), label, HorizontalAlignment.Center, BarWidth, 18, 5, ink);
			DrawString(font, new Vector2(x, y + 20), label, HorizontalAlignment.Center, BarWidth, 18, Colors.White);
			x += BarWidth + Gap;
		}
	}
}
