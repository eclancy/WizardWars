using Godot;

/// <summary>
/// A drawing of the pad's Select button (Back on older pads, View on Xbox ones): a small round
/// button with its two overlapping windows, for on-screen hints that name it. Sized by the
/// control; draws itself centred.
/// </summary>
public partial class SelectButtonIcon : Control
{
	public SelectButtonIcon() => MouseFilter = MouseFilterEnum.Ignore;

	public override void _Draw()
	{
		Vector2 c = Size * 0.5f;
		float r = Mathf.Min(Size.X, Size.Y) * 0.5f - 1.0f;
		// The button: a dark face in a light rim, with a dark outline round it all.
		DrawCircle(c, r, Ui.Ink);
		DrawCircle(c, r - 2.5f, Colors.White);
		DrawCircle(c, r - 5.0f, new Color(0.25f, 0.22f, 0.35f));
		// Its symbol: two windows, one tucked behind the other.
		float s = r * 0.42f;
		Rect2 back = new(c + new Vector2(-s * 0.55f, -s * 0.85f), new Vector2(s * 1.35f, s * 1.1f));
		Rect2 front = new(c + new Vector2(-s * 0.8f, -s * 0.25f), new Vector2(s * 1.35f, s * 1.1f));
		DrawRect(back, Colors.White, false, 2.0f);
		DrawRect(front, new Color(0.25f, 0.22f, 0.35f));
		DrawRect(front, Colors.White, false, 2.0f);
	}
}
