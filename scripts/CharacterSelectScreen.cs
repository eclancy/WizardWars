using System.Collections.Generic;
using Godot;

/// <summary>
/// Character Select. Any device joins the next free slot by pressing Start (or A / Space); the
/// slot keeps that device from then on. Each player moves a cursor over the four wizards with
/// the stick or d-pad (WASD / arrows) and locks one in with A (Space). B (Backspace) unlocks,
/// or leaves when not locked. Back/View (Tab) toggles Free-for-All and Teams; Y (T) switches
/// team. Holding RS (H) shows the how-to-play panel. Start begins once everyone is locked in.
///
/// The text is the Unity overlay's: a status line saying what the room is waiting for, and
/// under each player PICK A WIZARD or LOCKED IN, the team, and the locked wizard's kit by button.
/// </summary>
public partial class CharacterSelectScreen : Control
{
	const float CardWidth = 380.0f, CardTop = 150.0f, CardHeight = 420.0f, CardGap = 40.0f;
	const float SlotTop = 620.0f, SlotHeight = 430.0f;

	Label modeLabel, hintLabel, statusLabel;
	readonly Panel[] cards = new Panel[4];
	readonly Control[] cursorRows = new Control[4];
	readonly Control[] slotPanels = new Control[4];
	Control help;

	static float CardX(int index) => (1920.0f - (CardWidth * 4 + CardGap * 3)) * 0.5f + index * (CardWidth + CardGap);

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(Ui.Screen("Background"));

		AddChild(Ui.Panel(new Rect2(560, 10, 800, 120)));
		modeLabel = Ui.Label("", 34);
		Ui.Place(modeLabel, 560, 14, 800, 44);
		hintLabel = Ui.Label("", 18, new Color(0.85f, 0.85f, 0.9f));
		Ui.Place(hintLabel, 560, 56, 800, 28);
		statusLabel = Ui.Label("", 22);
		Ui.Place(statusLabel, 560, 88, 800, 34);
		AddChild(modeLabel);
		AddChild(hintLabel);
		AddChild(statusLabel);

		WizardData[] wizards = Content.Wizards;
		for (int i = 0; i < 4; i++)
		{
			float x = CardX(i);
			cards[i] = Ui.Panel(new Rect2(x, CardTop, CardWidth, CardHeight));
			AddChild(cards[i]);
			AddChild(Ui.Image(wizards[i].Portrait, new Rect2(x + 30, CardTop + 20, CardWidth - 60, CardHeight - 120)));
			Label name = Ui.Label(wizards[i].DisplayName, 30, wizards[i].Colour);
			Ui.Place(name, x, CardTop + CardHeight - 95, CardWidth, 44);
			AddChild(name);
			cursorRows[i] = new Control { Position = new Vector2(x, CardTop + CardHeight - 50), Size = new Vector2(CardWidth, 44) };
			AddChild(cursorRows[i]);
		}

		for (int i = 0; i < 4; i++)
		{
			slotPanels[i] = new Control { Position = new Vector2(CardX(i), SlotTop), Size = new Vector2(CardWidth, SlotHeight) };
			AddChild(slotPanels[i]);
		}

		help = BuildHelp();
		help.Visible = false;
		AddChild(help);

		Sfx.Music("res://assets/music/title.mp3");
		Refresh();
	}

	public override void _PhysicsProcess(double delta)
	{
		bool changed = false;
		bool helpHeld = false;
		foreach (int device in Devices.All)
		{
			InputState state = Devices.Get(device);
			helpHeld |= state.HelpHeld;
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
			Sfx.Play("res://assets/sfx/doorslam.wav", -14.0f);
			return;
		}
	}

	static bool HandlePlayer(PlayerSlot slot, InputState state)
	{
		bool changed = false;
		if (!slot.Locked && state.NavPressed.X != 0)
		{
			slot.WizardIndex = (slot.WizardIndex + state.NavPressed.X + 4) % 4;
			changed = true;
		}
		if (state.ConfirmPressed && !slot.Locked)
		{
			slot.Locked = true;
			Sfx.Play(Content.Wizards[slot.WizardIndex].Basic.Sound, -6.0f);
			changed = true;
		}
		else if (state.BackPressed)
		{
			if (slot.Locked) slot.Locked = false; else slot.Leave();
			changed = true;
		}
		if (state.TeamPressed && Session.TeamsMode)
		{
			slot.Team = slot.Team == Teams.Red ? Teams.Blue : Teams.Red;
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
		modeLabel.Text = Session.TeamsMode ? "Mode: TEAMS" : "Mode: FREE-FOR-ALL";
		hintLabel.Text = "Back/View or Tab: change mode" + (Session.TeamsMode ? "     Y or T: switch team" : "") + "     Hold RS or H: how to play";
		statusLabel.Text = RoomStatus();
		statusLabel.AddThemeColorOverride("font_color", EveryoneReady ? Ui.Done : Ui.Waiting);

		for (int i = 0; i < 4; i++)
		{
			foreach (Node child in cursorRows[i].GetChildren()) child.QueueFree();
			int hovering = 0;
			foreach (PlayerSlot slot in Session.Slots)
				if (slot.Joined && slot.WizardIndex == i) hovering++;
			float tagWidth = 70.0f, x = (CardWidth - hovering * (tagWidth + 6)) * 0.5f;
			bool anyHere = false;
			foreach (PlayerSlot slot in Session.Slots)
			{
				if (!slot.Joined || slot.WizardIndex != i) continue;
				anyHere = true;
				Panel tag = Ui.Panel(new Rect2(x, 0, tagWidth, 40), Ui.PlayerColours[slot.Index], slot.Locked ? 0.95f : 0.6f);
				Label label = Ui.Label(slot.Label, 24, Ui.PlayerColours[slot.Index]);
				Ui.Place(label, 0, 0, tagWidth, 40);
				tag.AddChild(label);
				cursorRows[i].AddChild(tag);
				x += tagWidth + 6;
			}
			StyleBoxFlat style = (StyleBoxFlat)cards[i].GetThemeStylebox("panel").Duplicate();
			style.BorderColor = anyHere ? Content.Wizards[i].Colour : new Color(1, 1, 1, 0.15f);
			cards[i].AddThemeStyleboxOverride("panel", style);
		}

		for (int i = 0; i < 4; i++) DrawSlot(Session.Slots[i], slotPanels[i]);
	}

	static string RoomStatus()
	{
		if (Session.JoinedCount == 0) return "Press Start (Enter) to join - up to 4 players";
		List<string> picking = new();
		foreach (PlayerSlot slot in Session.Slots) if (slot.Joined && !slot.Locked) picking.Add(slot.Label);
		if (picking.Count > 0) return "Waiting for " + string.Join(", ", picking) + " to pick a wizard (A or Space)";
		if (Session.TeamsUnbalanced) return "Both teams need at least one player";
		return Session.JoinedCount == 1 ? "Press Start to fight the computer wizard!" : "Press Start to fight!";
	}

	static readonly string[] PadButtons = { "RT", "LT", "RB", "X" };
	static readonly string[] KeyButtons = { "1", "2", "3", "5" };

	static void DrawSlot(PlayerSlot slot, Control area)
	{
		foreach (Node child in area.GetChildren()) child.QueueFree();
		Color colour = Ui.PlayerColours[slot.Index];
		area.AddChild(Ui.Panel(new Rect2(0, 0, CardWidth, SlotHeight), slot.Joined ? colour : null, slot.Joined ? 0.82f : 0.45f));

		if (!slot.Joined)
		{
			Label join = Ui.Label($"{slot.Label}\nPress Start to join", 28, new Color(1, 1, 1, 0.55f));
			Ui.Place(join, 0, 0, CardWidth, SlotHeight);
			area.AddChild(join);
			return;
		}

		Label title = Ui.Label($"{slot.Label}  -  {Devices.Describe(slot.Device)}", 26, colour);
		Ui.Place(title, 0, 10, CardWidth, 36);
		area.AddChild(title);

		Label state = Ui.Label(slot.Locked ? "LOCKED IN" : "PICK A WIZARD", 30, slot.Locked ? Ui.Done : Ui.Waiting);
		Ui.Place(state, 0, 50, CardWidth, 40);
		area.AddChild(state);

		float y = 95.0f;
		if (Session.TeamsMode)
		{
			Label team = Ui.Label(Teams.Name(slot.Team).ToUpper() + " TEAM", 28, Teams.Colour(slot.Team));
			Ui.Place(team, 0, y, CardWidth, 38);
			area.AddChild(team);
			y += 42.0f;
		}

		WizardData wizard = Content.Wizards[slot.WizardIndex];
		Label name = Ui.Label(wizard.DisplayName, 24, wizard.Colour);
		Ui.Place(name, 0, y, CardWidth, 32);
		area.AddChild(name);
		y += 40.0f;

		if (!slot.Locked)
		{
			Label hint = Ui.Label("Left / right to choose\nA or Space to lock in", 20, new Color(1, 1, 1, 0.7f));
			Ui.Place(hint, 0, y, CardWidth, 64);
			area.AddChild(hint);
			return;
		}

		// The kit by button. Keyboard players see the number keys.
		string[] buttons = slot.Device == Devices.Keyboard ? KeyButtons : PadButtons;
		AbilityData[] abilities = wizard.Abilities;
		for (int i = 0; i < 4; i++)
		{
			Label row = Ui.Label($"{buttons[i],-3}  {abilities[i].DisplayName}", 21, i == 3 ? new Color(1.0f, 0.85f, 0.2f) : Colors.White, HorizontalAlignment.Left);
			Ui.Place(row, 40, y + i * 34, CardWidth - 60, 32);
			area.AddChild(row);
		}
		Label back = Ui.Label("B or Backspace to change", 17, new Color(1, 1, 1, 0.5f));
		Ui.Place(back, 0, SlotHeight - 40, CardWidth, 30);
		area.AddChild(back);
	}

	static readonly string[] HowToPlay =
	{
		"Move with the left stick (WASD). Aim with the right stick (arrow keys).",
		"Basic attack: RT (1). It is free; you can always fire it.",
		"Second and third: LT (2) and RB (3). They cost energy - the blue bar.",
		"Ultimate: X (5) once the gold bar is full. It fills as you fight and as you get hit.",
		"The pips under your bars show cooldowns. A red flash means not enough energy.",
		"Free-for-All: last wizard standing wins.",
		"Teams: knock out the other team. Your spells never hurt your teammates.",
		"Playing alone? You face a computer-controlled wizard.",
	};

	static Control BuildHelp()
	{
		Control root = new() { Position = Vector2.Zero, Size = new Vector2(1920, 1080), MouseFilter = MouseFilterEnum.Ignore };
		root.AddChild(Ui.Panel(new Rect2(330, 240, 1260, 560), Ui.Waiting, 0.95f));
		Label title = Ui.Label("HOW TO PLAY", 40, Ui.Waiting);
		Ui.Place(title, 330, 260, 1260, 56);
		root.AddChild(title);
		for (int i = 0; i < HowToPlay.Length; i++)
		{
			Label line = Ui.Label(HowToPlay[i], 26, Colors.White, HorizontalAlignment.Left);
			Ui.Place(line, 380, 335 + i * 56, 1180, 50);
			root.AddChild(line);
		}
		return root;
	}
}
