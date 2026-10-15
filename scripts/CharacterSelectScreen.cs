using System.Collections.Generic;
using Godot;

/// <summary>
/// Character Select. Any device joins the next free slot by pressing Start (or A / Space); the
/// slot keeps that device from then on. Each player moves a cursor over the four wizards with
/// the stick or d-pad (WASD / arrows) and locks one in with A (Space). A wizard can be played by
/// one player at a time: once locked in, its card greys out for everyone else, other cursors
/// skip it, and anyone still choosing on it is moved along. B (Backspace) unlocks,
/// or leaves when not locked. Back/View (Tab) toggles Free-for-All and Teams; Y (T) switches
/// team. Holding RS (H) shows the how-to-play panel. Start begins once everyone is locked in.
/// On-screen text names only pad buttons.
///
/// Layout, top to bottom: the heading; the mode switch, both modes side by side with the live
/// one lit; a status line saying what the room is waiting for; the four wizard cards, tinted in
/// their element's colour, with each player's cursor tag; a panel per player (PICK A WIZARD or
/// LOCKED IN, team, and the locked wizard's kit as button chips); and a footer of hints.
///
/// Players have no colour of their own, only a number: the colour that marks a player is the
/// colour of the wizard they locked in (unique, see above), or in Teams their team's.
/// </summary>
public partial class CharacterSelectScreen : Control
{
	const float CardWidth = 380.0f, CardTop = 205.0f, CardHeight = 345.0f, CardGap = 40.0f;
	const float SlotTop = 578.0f, SlotHeight = 450.0f;
	const float PortraitHeight = 236.0f;

	Label statusLabel, footer;
	Control modeBadge;
	bool shownTeamsMode;
	readonly Panel[] cards = new Panel[4];
	readonly Label[] names = new Label[4];
	readonly Control[] takenTags = new Control[4];
	readonly TextureRect[] portraits = new TextureRect[4];
	readonly Control[] cursorRows = new Control[4];
	readonly Control[] slotPanels = new Control[4];
	readonly bool[] hovered = new bool[4];
	readonly List<Control> pulsing = new();
	Control help;
	OptionsPanel options;
	double time;

	static float CardX(int index) => (1920.0f - (CardWidth * 4 + CardGap * 3)) * 0.5f + index * (CardWidth + CardGap);

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(Ui.Screen("Background"));
		// The layout lives in a centred 1920 x 1080 frame; the backdrop fills the real screen.
		DesignFrame frame = DesignFrame.In(this);

		Label heading = Ui.Heading("CHOOSE YOUR WIZARD", 54, Ui.Gold);
		Ui.Place(heading, 0, 4, 1920, 72);
		frame.AddChild(heading);
		modeBadge = new Control { Position = new Vector2(520, 76), Size = new Vector2(880, 76), PivotOffset = new Vector2(440, 38) };
		frame.AddChild(modeBadge);
		shownTeamsMode = Session.TeamsMode;
		statusLabel = Ui.Label("", 25);
		Ui.Place(statusLabel, 0, 158, 1920, 40);
		frame.AddChild(statusLabel);

		WizardData[] wizards = Content.Wizards;
		for (int i = 0; i < 4; i++)
		{
			float x = CardX(i);
			Color tint = wizards[i].Colour;
			cards[i] = new Panel { Position = new Vector2(x, CardTop), Size = new Vector2(CardWidth, CardHeight), MouseFilter = MouseFilterEnum.Ignore };
			cards[i].AddThemeStyleboxOverride("panel", CardStyle(tint, false));
			frame.AddChild(cards[i]);
			// An element-coloured stripe along the top of the card.
			ColorRect stripe = new() { Color = tint, Position = new Vector2(x + 14, CardTop + 10), Size = new Vector2(CardWidth - 28, 6), MouseFilter = MouseFilterEnum.Ignore };
			frame.AddChild(stripe);

			portraits[i] = Ui.Image(wizards[i].Portrait, new Rect2(x + 40, CardTop + 24, CardWidth - 80, PortraitHeight));
			portraits[i].PivotOffset = portraits[i].Size * new Vector2(0.5f, 1.0f);
			frame.AddChild(portraits[i]);
			names[i] = Ui.Heading(wizards[i].DisplayName.ToUpper(), 28, tint);
			Ui.Place(names[i], x, CardTop + 262, CardWidth, 40);
			frame.AddChild(names[i]);
			takenTags[i] = new Control { Position = new Vector2(x, CardTop + 104), Size = new Vector2(CardWidth, 64), MouseFilter = MouseFilterEnum.Ignore };
			frame.AddChild(takenTags[i]);
			cursorRows[i] = new Control { Position = new Vector2(x, CardTop + 302), Size = new Vector2(CardWidth, 34), MouseFilter = MouseFilterEnum.Ignore };
			frame.AddChild(cursorRows[i]);
		}

		for (int i = 0; i < 4; i++)
		{
			slotPanels[i] = new Control { Position = new Vector2(CardX(i), SlotTop), Size = new Vector2(CardWidth, SlotHeight), MouseFilter = MouseFilterEnum.Ignore };
			frame.AddChild(slotPanels[i]);
		}

		footer = Ui.Label("", 20, new Color(1, 1, 1, 0.6f));
		Ui.Place(footer, 0, 1038, 1920, 34);
		frame.AddChild(footer);

		// The help covers the whole real screen, so it hangs off the screen, not the frame.
		help = BuildHelp();
		help.Visible = false;
		AddChild(help);

		Sfx.Music("res://assets/music/title.mp3");
		Refresh();
	}

	static StyleBoxFlat CardStyle(Color tint, bool lit)
	{
		// The card is the element's colour, deep and dark, so the four read as fire, frost,
		// earth and lightning at a glance; a hovered card lights its border in that colour.
		Color background = new(tint.R * 0.28f + 0.05f, tint.G * 0.28f + 0.03f, tint.B * 0.28f + 0.09f, 0.95f);
		return Ui.Style(background, lit ? tint : new Color(1, 1, 1, 0.1f), lit ? 6 : 3, 18);
	}

	public override void _Process(double delta)
	{
		time += delta;
		float t = (float)time;
		for (int i = 0; i < 4; i++)
		{
			// Each portrait bobs on its own beat; a hovered one grows and its border pulses.
			float bob = Mathf.Sin(t * 2.0f + i * 1.3f) * 4.0f;
			portraits[i].Position = new Vector2(CardX(i) + 40, CardTop + 24 + bob);
			float grow = hovered[i] ? 1.07f + 0.02f * Mathf.Sin(t * 4.0f) : 1.0f;
			portraits[i].Scale = Vector2.One * Mathf.Lerp(portraits[i].Scale.X, grow, 0.2f);
			if (hovered[i]) cards[i].SelfModulate = new Color(1, 1, 1).Lerp(new Color(1.15f, 1.15f, 1.15f), (Mathf.Sin(t * 4.0f) + 1.0f) * 0.5f);
			else cards[i].SelfModulate = Colors.White;
		}
		float blink = 0.55f + 0.45f * Mathf.Sin(t * 3.0f);
		foreach (Control control in pulsing) if (IsInstanceValid(control)) control.Modulate = new Color(1, 1, 1, blink);
	}

	public override void _PhysicsProcess(double delta)
	{
		// While the options are open they have the controls.
		if (IsInstanceValid(options)) return;
		bool changed = false;
		bool helpHeld = false;
		foreach (int device in Devices.All)
		{
			InputState state = Devices.Get(device);
			helpHeld |= state.HelpHeld;
			if (state.UltimatePressed) { options = OptionsPanel.Open(this, device, null); return; }
			if (state.ModePressed) { Session.TeamsMode = !Session.TeamsMode; changed = true; }

			PlayerSlot slot = Session.SlotForDevice(device);
			if (slot == null)
			{
				if (state.StartPressed || state.ConfirmPressed) { Join(device); changed = true; }
				else if (state.BackPressed && Session.JoinedCount == 0) { GameRoot.Instance.ShowTitle(); return; }
				continue;
			}
			changed |= HandlePlayer(slot, state);
			if (state.StartPressed && EveryoneReady) { GameRoot.Instance.ShowStageSelect(); return; }
		}
		help.Visible = helpHeld;
		if (changed) Refresh();
	}

	static void Join(int device)
	{
		foreach (PlayerSlot slot in Session.Slots)
		{
			if (slot.Joined) continue;
			slot.Joined = true;
			slot.Device = device;
			slot.Locked = false;
			slot.WizardIndex = Session.FirstFree(slot.Index % 4, slot);
			Sfx.Play("res://assets/sfx/doorslam.wav", -14.0f);
			return;
		}
	}

	static bool HandlePlayer(PlayerSlot slot, InputState state)
	{
		bool changed = false;
		if (!slot.Locked && state.NavPressed.X != 0)
		{
			// Wizards someone else has locked in are skipped.
			slot.WizardIndex = Session.NextFree(slot.WizardIndex, state.NavPressed.X > 0 ? 1 : -1, slot);
			changed = true;
		}
		if (state.ConfirmPressed && !slot.Locked && Session.LockedOn(slot.WizardIndex, slot) == null)
		{
			slot.Locked = true;
			Sfx.Play(Content.Wizards[slot.WizardIndex].Basic.Sound, -6.0f);
			// Anyone still choosing on this wizard moves along to one that is free.
			foreach (PlayerSlot other in Session.Slots)
				if (other != slot && other.Joined && !other.Locked && other.WizardIndex == slot.WizardIndex)
					other.WizardIndex = Session.FirstFree(other.WizardIndex, other);
			changed = true;
		}
		else if (state.BackPressed)
		{
			if (slot.Locked) slot.Locked = false; else slot.Leave();
			changed = true;
		}
		if (state.TeamPressed && Session.TeamsMode)
		{
			slot.Team = slot.Team == Teams.Green ? Teams.Pink : Teams.Green;
			changed = true;
		}
		return changed;
	}

	static bool EveryoneReady
	{
		get
		{
			if (Session.JoinedCount == 0 || Session.TeamsUnbalanced) return false;
			foreach (PlayerSlot slot in Session.Slots) if (slot.Joined && !slot.Locked) return false;
			return true;
		}
	}

	// ---- Drawing ----

	void Refresh()
	{
		pulsing.Clear();

		DrawModeSwitch();
		if (shownTeamsMode != Session.TeamsMode)
		{
			// The switch pops when the mode changes, so nobody misses it.
			shownTeamsMode = Session.TeamsMode;
			modeBadge.Scale = Vector2.One * 1.12f;
			CreateTween().TweenProperty(modeBadge, "scale", Vector2.One, 0.25).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}

		statusLabel.Text = RoomStatus();
		statusLabel.AddThemeColorOverride("font_color", EveryoneReady ? Ui.Done : Ui.Waiting);
		footer.Text = "X: options      " + (Session.TeamsMode ? "Y: switch team      " : "") + "Hold the right stick in: how to play";

		WizardData[] wizards = Content.Wizards;
		for (int i = 0; i < 4; i++)
		{
			foreach (Node child in cursorRows[i].GetChildren()) child.QueueFree();
			List<PlayerSlot> here = new();
			foreach (PlayerSlot slot in Session.Slots) if (slot.Joined && slot.WizardIndex == i) here.Add(slot);
			const float tagWidth = 64.0f;
			float x = (CardWidth - here.Count * (tagWidth + 6) + 6) * 0.5f;
			foreach (PlayerSlot slot in here)
			{
				// Player tags are plain white: a locked player's is solid, one still choosing outlined.
				cursorRows[i].AddChild(slot.Locked
					? Ui.Pill(new Rect2(x, 0, tagWidth, 34), slot.Label, 20, Colors.White, Ui.Ink, heading: true)
					: Ui.Pill(new Rect2(x, 0, tagWidth, 34), slot.Label, 20, new Color(Ui.Ink, 0.9f), Colors.White, heading: true));
				x += tagWidth + 6;
			}

			// A wizard someone has locked in is greyed out and marked with who has it.
			PlayerSlot owner = Session.LockedOn(i);
			foreach (Node child in takenTags[i].GetChildren()) child.QueueFree();
			if (owner != null)
				takenTags[i].AddChild(Ui.Pill(new Rect2(70, 10, CardWidth - 140, 46), "TAKEN BY " + owner.Label, 24, new Color(0.12f, 0.1f, 0.16f, 0.92f), Colors.White, heading: true));
			Color grey = new(0.42f, 0.42f, 0.46f);
			portraits[i].Modulate = owner != null ? grey : Colors.White;
			names[i].Modulate = owner != null ? grey : Colors.White;
			hovered[i] = owner == null && here.Count > 0;
			cards[i].AddThemeStyleboxOverride("panel", CardStyle(owner != null ? new Color(0.3f, 0.3f, 0.34f) : wizards[i].Colour, hovered[i]));
		}

		for (int i = 0; i < 4; i++) DrawSlot(Session.Slots[i], slotPanels[i]);
	}

	/// <summary>
	/// Both modes side by side, the live one lit gold and the other dimmed, with the View button
	/// between them: which mode you are in, and how to change it, are both always in sight.
	/// </summary>
	void DrawModeSwitch()
	{
		foreach (Node child in modeBadge.GetChildren()) child.QueueFree();
		Segment(new Rect2(0, 0, 390, 76), "FREE-FOR-ALL", !Session.TeamsMode, false);
		Segment(new Rect2(490, 0, 390, 76), "TEAMS", Session.TeamsMode, true);
		modeBadge.AddChild(Ui.Pill(new Rect2(400, 21, 80, 34), "View", 17, new Color(0.25f, 0.22f, 0.35f), Colors.White, heading: true));
		Label arrows = Ui.Label("<            >", 22, new Color(1, 1, 1, 0.55f));
		Ui.Place(arrows, 390, 18, 100, 40);
		modeBadge.AddChild(arrows);
	}

	void Segment(Rect2 rect, string title, bool live, bool teams)
	{
		Panel panel = new() { Position = rect.Position, Size = rect.Size, MouseFilter = MouseFilterEnum.Ignore };
		panel.AddThemeStyleboxOverride("panel", live
			? Ui.Style(Ui.Gold, Colors.White, 4, 20)
			: Ui.Style(new Color(0.1f, 0.08f, 0.16f, 0.9f), new Color(1, 1, 1, 0.15f), 2, 20));
		modeBadge.AddChild(panel);
		Color ink = live ? Ui.Ink : new Color(1, 1, 1, 0.4f);
		Label heading = Ui.Heading(title, 28, ink);
		if (live) heading.AddThemeConstantOverride("outline_size", 0);
		Ui.Place(heading, rect.Position.X, 2, rect.Size.X, 40);
		modeBadge.AddChild(heading);
		if (!teams)
		{
			Label line = Ui.Label("Last wizard standing wins", 18, live ? new Color(Ui.Ink, 0.85f) : new Color(1, 1, 1, 0.35f));
			if (live) line.AddThemeConstantOverride("outline_size", 0);
			Ui.Place(line, rect.Position.X, 40, rect.Size.X, 28);
			modeBadge.AddChild(line);
			return;
		}
		// The team colours themselves, so Teams says which sides there are.
		float x = rect.Position.X + rect.Size.X * 0.5f;
		float alpha = live ? 1.0f : 0.35f;
		modeBadge.AddChild(Ui.Pill(new Rect2(x - 124, 42, 96, 26), "GREEN", 15, new Color(Teams.Colour(Teams.Green), alpha), Ui.Ink, heading: true));
		Label vs = Ui.Label("vs", 18, live ? Ui.Ink : new Color(1, 1, 1, 0.35f));
		if (live) vs.AddThemeConstantOverride("outline_size", 0);
		Ui.Place(vs, x - 28, 40, 56, 28);
		modeBadge.AddChild(vs);
		modeBadge.AddChild(Ui.Pill(new Rect2(x + 28, 42, 96, 26), "PINK", 15, new Color(Teams.Colour(Teams.Pink), alpha), Ui.Ink, heading: true));
	}

	static string RoomStatus()
	{
		if (Session.JoinedCount == 0) return "Press Start to join - up to 4 players";
		List<string> picking = new();
		foreach (PlayerSlot slot in Session.Slots) if (slot.Joined && !slot.Locked) picking.Add(slot.Label);
		if (picking.Count > 0) return "Waiting for " + string.Join(", ", picking) + " to pick a wizard (A)";
		if (Session.TeamsUnbalanced) return "Both teams need at least one player";
		return Session.JoinedCount == 1 ? "Press Start to fight the computer wizard!" : "Press Start to fight!";
	}

	void DrawSlot(PlayerSlot slot, Control area)
	{
		foreach (Node child in area.GetChildren()) child.QueueFree();
		// A player has no colour of their own: the panel is neutral until they lock in a wizard,
		// then wears that wizard's colour (each wizard has one player, so it is theirs alone).
		WizardData wizard = Content.Wizards[slot.WizardIndex];
		Color colour = slot.Joined && slot.Locked ? wizard.Colour : new Color(0.75f, 0.72f, 0.85f);

		if (!slot.Joined)
		{
			area.AddChild(Ui.Panel(new Rect2(0, 0, CardWidth, SlotHeight), new Color(1, 1, 1, 0.08f), 0.5f));
			Label number = Ui.Heading(slot.Label, 72, new Color(1, 1, 1, 0.3f));
			Ui.Place(number, 0, 120, CardWidth, 100);
			area.AddChild(number);
			Label join = Ui.Label("Press Start to join", 28, new Color(1, 1, 1, 0.8f));
			Ui.Place(join, 0, 225, CardWidth, 44);
			area.AddChild(join);
			pulsing.Add(join);
			return;
		}

		area.AddChild(Ui.Panel(new Rect2(0, 0, CardWidth, SlotHeight), colour, 0.9f));
		// A header in the player's colour.
		Panel header = new() { Position = new Vector2(3, 3), Size = new Vector2(CardWidth - 6, 54), MouseFilter = MouseFilterEnum.Ignore };
		StyleBoxFlat headerStyle = new() { BgColor = colour.Darkened(0.35f), CornerRadiusTopLeft = 15, CornerRadiusTopRight = 15 };
		header.AddThemeStyleboxOverride("panel", headerStyle);
		area.AddChild(header);
		Label label = Ui.Heading(slot.Label, 32, Colors.White, HorizontalAlignment.Left);
		Ui.Place(label, 20, 5, 120, 50);
		area.AddChild(label);
		Label device = Ui.Label(Devices.Describe(slot.Device), 20, new Color(1, 1, 1, 0.85f), HorizontalAlignment.Right);
		Ui.Place(device, 140, 5, CardWidth - 160, 50);
		area.AddChild(device);

		float y = 70.0f;
		area.AddChild(slot.Locked
			? Ui.Pill(new Rect2(70, y, CardWidth - 140, 40), "LOCKED IN", 22, new Color(0.18f, 0.5f, 0.24f), Colors.White, heading: true)
			: Ui.Pill(new Rect2(70, y, CardWidth - 140, 40), "PICK A WIZARD", 22, new Color(0.55f, 0.4f, 0.08f), Colors.White, heading: true));
		y += 50.0f;
		if (Session.TeamsMode)
		{
			Color team = Teams.Colour(slot.Team);
			area.AddChild(Ui.Pill(new Rect2(110, y, CardWidth - 220, 34), Teams.Name(slot.Team).ToUpper() + " TEAM", 18, team.Darkened(0.25f), Colors.White, heading: true));
			y += 44.0f;
		}

		Label name = Ui.Heading(wizard.DisplayName.ToUpper(), 24, wizard.Colour);
		Ui.Place(name, 0, y, CardWidth, 36);
		area.AddChild(name);
		y += 44.0f;

		if (!slot.Locked)
		{
			Label hint = Ui.Label("Left / right to choose\nA to lock in", 22, new Color(1, 1, 1, 0.8f));
			Ui.Place(hint, 0, y + 20, CardWidth, 70);
			area.AddChild(hint);
			pulsing.Add(hint);
			return;
		}

		// The kit, each ability beside a chip naming its pad button.
		AbilityData[] abilities = wizard.Abilities;
		for (int i = 0; i < WizardData.SlotCount; i++)
		{
			bool ultimate = i == WizardData.UltimateSlot;
			area.AddChild(Ui.Pill(new Rect2(34, y + i * 38, 58, 30), WizardData.PadButtons[i], 16,
				ultimate ? new Color(0.85f, 0.65f, 0.15f) : new Color(0.25f, 0.22f, 0.35f), ultimate ? Ui.Ink : Colors.White, heading: true));
			Label row = Ui.Label(abilities[i].DisplayName, 21, ultimate ? Ui.Gold : Colors.White, HorizontalAlignment.Left);
			Ui.Place(row, 106, y + i * 38 - 2, CardWidth - 120, 34);
			area.AddChild(row);
		}
		Label back = Ui.Label("B to change", 17, new Color(1, 1, 1, 0.5f));
		Ui.Place(back, 0, SlotHeight - 34, CardWidth, 28);
		area.AddChild(back);
	}

	static readonly string[] HowToPlay =
	{
		"Move with the left stick. Aim with the right stick.",
		"Basic attack: RT. It is free; you can always fire it.",
		"Second, third and fourth: LT, RB and LB. They cost energy - the blue bar.",
		"Ultimate: X once the gold bar is full. It fills as you fight and as you get hit.",
		"Frozen in ice? Wiggle the left stick from side to side to break out sooner.",
		"The pips under your bars show cooldowns. A red flash means not enough energy.",
		"Hide behind each stage's cover: it stops shots.",
		"Free-for-All: last wizard standing wins.",
		"Teams: knock out the other team. Your spells never hurt your teammates.",
		"Playing alone? You face a computer-controlled wizard.",
	};

	static Control BuildHelp()
	{
		Control root = new() { MouseFilter = MouseFilterEnum.Ignore };
		root.SetAnchorsPreset(LayoutPreset.FullRect);
		ColorRect dim = new() { Color = new Color(0, 0, 0, 0.55f), MouseFilter = MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(dim);
		DesignFrame frame = DesignFrame.In(root);
		frame.AddChild(Ui.Panel(new Rect2(300, 160, 1320, 740), Ui.Gold, 0.97f));
		Label title = Ui.Heading("HOW TO PLAY", 48, Ui.Gold);
		Ui.Place(title, 300, 182, 1320, 66);
		frame.AddChild(title);
		for (int i = 0; i < HowToPlay.Length; i++)
		{
			Label line = Ui.Label(HowToPlay[i], 26, Colors.White, HorizontalAlignment.Left);
			Ui.Place(line, 360, 262 + i * 60, 1220, 56);
			frame.AddChild(line);
		}
		return root;
	}
}
