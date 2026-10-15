using Godot;

/// <summary>
/// The 1920 x 1080 box every menu is laid out in, kept centred in the real view.
///
/// The project stretches with aspect "expand", so the view is 1920 x 1080 only on a 16:9
/// screen: on 16:10 or 3:2 it is taller (1920 x 1280 on a 3:2 laptop), on 21:9 wider. A screen
/// that placed its parts in fixed 1080p coordinates left an empty band at the bottom. So each
/// screen puts its layout in a DesignFrame, and full-bleed things (backdrop, dimming, corner
/// labels) on the screen itself, anchored to the real edges.
/// </summary>
public partial class DesignFrame : Control
{
	public static readonly Vector2 DesignSize = new(1920, 1080);

	public DesignFrame()
	{
		Name = "Frame";
		MouseFilter = MouseFilterEnum.Ignore;
		Size = DesignSize;
	}

	public override void _Ready()
	{
		Recentre();
		GetViewport().SizeChanged += Recentre;
	}

	public override void _ExitTree() => GetViewport().SizeChanged -= Recentre;

	void Recentre() => Position = ((GetViewportRect().Size - DesignSize) * 0.5f).Floor();

	/// <summary>Adds a centred design frame to a full-screen control and returns it.</summary>
	public static DesignFrame In(Control screen)
	{
		DesignFrame frame = new();
		screen.AddChild(frame);
		return frame;
	}

	/// <summary>
	/// Puts a label's text in the bottom-right corner of the real view, inset by a margin: the
	/// label is stretched over the whole screen and its text aligned to that corner, which
	/// holds whatever the screen's shape.
	/// </summary>
	public static void BottomRight(Label label, float margin)
	{
		label.SetAnchorsPreset(LayoutPreset.FullRect);
		label.OffsetLeft = margin;
		label.OffsetTop = margin;
		label.OffsetRight = -margin;
		label.OffsetBottom = -margin;
		label.HorizontalAlignment = HorizontalAlignment.Right;
		label.VerticalAlignment = VerticalAlignment.Bottom;
	}
}
