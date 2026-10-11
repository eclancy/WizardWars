using UnityEngine;
using UnityEngine.SceneManagement;
using WizardWars.LocalMultiplayer;

public class CharacterSelect : MonoBehaviour
{
    public GameObject GlobalHandlerObject;
    public GlobalHandler GlobalHandlerScript;
    public GameObject p1;
    public GameObject p2;
    public GameObject p3;
    public GameObject p4;
    public GameObject p1CursorObject;
    public GameObject p2CursorObject;
    public GameObject p3CursorObject;
    public GameObject p4CursorObject;
    public GameObject p1cursor;
    public GameObject p2cursor;
    public GameObject p3cursor;
    public GameObject p4cursor;
    public GameObject p1join;
    public GameObject p2join;
    public GameObject p3join;
    public GameObject p4join;
    public GameObject startToPlay;

    private GameObject[] selections = new GameObject[4];

    private void Awake()
    {
        GlobalHandlerObject = GameObject.Find("GlobalHandler");
        if (GlobalHandlerObject != null) GlobalHandlerScript = GlobalHandlerObject.GetComponent<GlobalHandler>();
        EnsureLocalMultiplayerRuntime();
        if (startToPlay == null) startToPlay = GameObject.Find("StartToBattle");
        SetGui(false);
        // Remember the wizard portraits so the Park can dress the AI opponent.
        WizardRoster.Capture();
    }

    private void Start()
    {
        if (GlobalHandlerScript == null) return;
        GlobalHandlerScript.playersJoined = 0;
        GlobalHandlerScript.player1Joined = GlobalHandlerScript.player2Joined = GlobalHandlerScript.player3Joined = GlobalHandlerScript.player4Joined = false;
        GlobalHandlerScript.player1Locked = GlobalHandlerScript.player2Locked = GlobalHandlerScript.player3Locked = GlobalHandlerScript.player4Locked = false;
        SyncSlot(0, false, false, ""); SyncSlot(1, false, false, ""); SyncSlot(2, false, false, ""); SyncSlot(3, false, false, "");
    }

    private void Update()
    {
        if (GlobalHandlerScript == null) return;
        SetJoinIndicator(p1join, GlobalHandlerScript.player1Joined);
        SetJoinIndicator(p2join, GlobalHandlerScript.player2Joined);
        SetJoinIndicator(p3join, GlobalHandlerScript.player3Joined);
        SetJoinIndicator(p4join, GlobalHandlerScript.player4Joined);

        HandleJoin(0, "Start_1", "Player1", ref p1, ref p1cursor, p1CursorObject, -7.437364f);
        HandleJoin(1, "Start_2", "Player2", ref p2, ref p2cursor, p2CursorObject, -1.995348f);
        HandleJoin(2, "Start_3", "Player3", ref p3, ref p3cursor, p3CursorObject, 3.446668f);
        HandleJoin(3, "Start_4", "Player4", ref p4, ref p4cursor, p4CursorObject, 8.888684f);
        HandleLeave(0, "B_1", ref p1, ref p1cursor); HandleLeave(1, "B_2", ref p2, ref p2cursor);
        HandleLeave(2, "B_3", ref p3, ref p3cursor); HandleLeave(3, "B_4", ref p4, ref p4cursor);
        HandleSelect(0, "A_1", p1cursor, ref selections[0], p1);
        HandleSelect(1, "A_2", p2cursor, ref selections[1], p2);
        HandleSelect(2, "A_3", p3cursor, ref selections[2], p3);
        HandleSelect(3, "A_4", p4cursor, ref selections[3], p4);
        HandleModeToggle();
        HandleTeamSwitch(0, "Y_1"); HandleTeamSwitch(1, "Y_2"); HandleTeamSwitch(2, "Y_3"); HandleTeamSwitch(3, "Y_4");

        bool ready = GlobalHandlerScript.playersJoined > 0 && !TeamsUnbalanced() &&
                     (!GlobalHandlerScript.player1Joined || GlobalHandlerScript.player1Locked) &&
                     (!GlobalHandlerScript.player2Joined || GlobalHandlerScript.player2Locked) &&
                     (!GlobalHandlerScript.player3Joined || GlobalHandlerScript.player3Locked) &&
                     (!GlobalHandlerScript.player4Joined || GlobalHandlerScript.player4Locked);
        SetJoinIndicator(startToPlay, ready);
        if (ready && (Input.GetButtonDown("Start_1") || Input.GetButtonDown("Start_2") || Input.GetButtonDown("Start_3") || Input.GetButtonDown("Start_4")))
        {
            for (int i = 0; i < selections.Length; i++) if (selections[i] != null) AttachWizard(GetPlayer(i), selections[i].name);
            SetGui(true);
            SceneManager.LoadScene("Stage Select");
        }
    }

    private void HandleJoin(int slot, string input, string playerName, ref GameObject preview, ref GameObject cursorObject, GameObject cursorPrefab, float x)
    {
        if (!Input.GetButtonDown(input) || IsJoined(slot) || GlobalHandlerScript == null) return;
        GlobalHandlerScript.joined(playerName);
        SyncSlot(slot, true, false, "");
        GameObject basePlayer = GetBasePlayer(slot);
        preview = basePlayer == null ? null : Instantiate(basePlayer);
        if (preview != null) { preview.GetComponent<SpriteRenderer>().enabled = false; preview.transform.position = new Vector3(x, -4.139224f, preview.transform.position.z); preview.transform.localScale = Vector3.one * 0.3f; }
        cursorObject = cursorPrefab == null ? null : Instantiate(cursorPrefab);
        if (cursorObject != null) cursorObject.layer = 3;
    }

    private void HandleLeave(int slot, string input, ref GameObject preview, ref GameObject cursorObject)
    {
        if (!Input.GetButtonDown(input) || !IsJoined(slot)) return;
        if (IsLocked(slot)) { selections[slot] = null; SetLocked(slot, false); SyncSlot(slot, true, false, ""); return; }
        GlobalHandlerScript.left("Player" + (slot + 1)); SyncSlot(slot, false, false, "");
        if (preview != null) Destroy(preview); if (cursorObject != null) Destroy(cursorObject);
    }

    private void HandleSelect(int slot, string input, GameObject cursorObject, ref GameObject selection, GameObject preview)
    {
        if (!Input.GetButtonDown(input) || !IsJoined(slot) || cursorObject == null) return;
        GameObject selected = FindCharacter(cursorObject, selection);
        if (selected == null) return;
        selection = selected;
        SpriteRenderer source = selected.GetComponent<SpriteRenderer>();
        if (source != null && preview != null) preview.GetComponent<SpriteRenderer>().sprite = source.sprite;
        if (preview != null) preview.GetComponent<SpriteRenderer>().enabled = true;
        GameObject basePlayer = GetBasePlayer(slot);
        if (basePlayer != null && source != null) basePlayer.GetComponent<SpriteRenderer>().sprite = source.sprite;
        SetLocked(slot, true); SyncSlot(slot, true, true, selected.name);
    }

    // Any player's Back/View button (Tab on the keyboard) flips the match between
    // Free-for-All and Teams.
    private void HandleModeToggle()
    {
        if (Input.GetButtonDown("Back_1") || Input.GetButtonDown("Back_2") || Input.GetButtonDown("Back_3") || Input.GetButtonDown("Back_4"))
            GlobalHandlerScript.teamsMode = !GlobalHandlerScript.teamsMode;
    }

    // Y (T on the keyboard) moves a joined player to the other team. It works before
    // or after locking in a wizard.
    private void HandleTeamSwitch(int slot, string input)
    {
        if (!GlobalHandlerScript.teamsMode || !Input.GetButtonDown(input) || !IsJoined(slot)) return;
        GlobalHandlerScript.SetTeam(slot, GlobalHandlerScript.GetTeam(slot) == Teams.Red ? Teams.Blue : Teams.Red);
    }

    // Teams with two or more players needs somebody on each side. One player alone
    // plays the AI as usual, whatever the mode.
    private bool TeamsUnbalanced() => GlobalHandlerScript.teamsMode && GlobalHandlerScript.playersJoined > 1 && !GlobalHandlerScript.TeamsPlayable();

    // Ability names in button order: basic (RT), second (LT), third (RB), ultimate (X).
    private static readonly string[][] Kits =
    {
        new[] { "FireWizard", "Fireball", "Ring of Fire", "Flamethrower (hold)", "Meteor Storm" },
        new[] { "FrostWizard", "Icicle", "Ice Patch", "Ice Wave", "Absolute Zero" },
        new[] { "EarthWizard", "Mudball", "Mud Patch", "Rock Wall", "Earthquake" },
        new[] { "ElectricityWizard", "Electricity Ball", "Lightning Rod", "Speed Up", "Overcharge" },
    };

    private static readonly Color Waiting = new Color(1f, 0.85f, 0.3f);
    private static readonly Color Done = new Color(0.45f, 1f, 0.5f);

    // All of Character Select's text: the mode banner and what the room is waiting
    // for, each joined player's status, team and wizard kit, and the how-to-play panel
    // while RS (H on the keyboard) is held.
    private void OnGUI()
    {
        if (GlobalHandlerScript == null) return;
        float scale = Mathf.Max(0.75f, Screen.height / 1080f);
        Color previous = GUI.color;

        GUIStyle title = Style(22, scale, true);
        GUIStyle hint = Style(14, scale, false);
        float width = 560f * scale, x = (Screen.width - width) / 2f, line = 26f * scale;
        GUI.Box(new Rect(x, 8f, width, line * 3.4f), GUIContent.none);
        GUI.Label(new Rect(x, 10f, width, line * 1.2f), GlobalHandlerScript.teamsMode ? "Mode: TEAMS" : "Mode: FREE-FOR-ALL", title);
        string controls = "Back/View or Tab: change mode" + (GlobalHandlerScript.teamsMode ? "     Y or T: switch team" : "") + "     Hold RS or H: how to play";
        GUI.Label(new Rect(x, 10f + line * 1.2f, width, line), controls, hint);
        string status = RoomStatus(out bool ready);
        GUI.color = ready ? Done : Waiting;
        GUI.Label(new Rect(x, 10f + line * 2.2f, width, line), status, Style(16, scale, true));
        GUI.color = previous;

        if (Camera.main != null) DrawSlots(scale);
        if (Input.GetButton("RS_1") || Input.GetButton("RS_2") || Input.GetButton("RS_3") || Input.GetButton("RS_4")) DrawHowToPlay(scale);
    }

    private static GUIStyle Style(int size, float scale, bool bold) =>
        new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(size * scale), fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, wordWrap = false };

    // One line saying what has to happen before the battle can start.
    private string RoomStatus(out bool ready)
    {
        ready = false;
        if (GlobalHandlerScript.playersJoined == 0) return "Press Start (Enter) to join - up to 4 players";
        string unlocked = "";
        for (int slot = 0; slot < 4; slot++)
            if (IsJoined(slot) && !IsLocked(slot)) unlocked += (unlocked.Length > 0 ? ", " : "") + "P" + (slot + 1);
        if (unlocked.Length > 0) return "Waiting for " + unlocked + " to pick a wizard (A or Space)";
        if (TeamsUnbalanced()) return "Both teams need at least one player";
        ready = true;
        return GlobalHandlerScript.playersJoined == 1 ? "Press Start to fight the computer wizard!" : "Press Start to fight!";
    }

    // Under each joined player's preview: picking or locked in, their team, and once
    // a wizard is locked, what each button does with it.
    private void DrawSlots(float scale)
    {
        GameObject[] previews = { p1, p2, p3, p4 };
        Color previous = GUI.color;
        float width = 260f * scale, line = 22f * scale;
        for (int slot = 0; slot < 4; slot++)
        {
            if (!IsJoined(slot) || previews[slot] == null) continue;
            Vector3 screen = Camera.main.WorldToScreenPoint(new Vector3(previews[slot].transform.position.x, -5.2f, 0f));
            float x = screen.x - width / 2f;
            float y = Screen.height - screen.y;
            // Keep the whole block on screen whatever the aspect ratio.
            y = Mathf.Min(y, Screen.height - line * 7.2f);

            bool locked = IsLocked(slot);
            GUI.color = locked ? Done : Waiting;
            GUI.Label(new Rect(x, y, width, line), locked ? "LOCKED IN" : "PICK A WIZARD", Style(18, scale, true));
            y += line;
            if (GlobalHandlerScript.teamsMode)
            {
                int team = GlobalHandlerScript.GetTeam(slot);
                GUI.color = Teams.Colour(team);
                GUI.Label(new Rect(x, y, width, line), Teams.Name(team).ToUpper() + " TEAM", Style(18, scale, true));
                y += line;
            }
            GUI.color = previous;

            string[] kit = locked && selections[slot] != null ? KitFor(selections[slot].name) : null;
            if (kit == null) continue;
            // Player 1 can be on the keyboard, so show both.
            string[] buttons = slot == 0 ? new[] { "RT / 1", "LT / 2", "RB / 3", "X / 5" } : new[] { "RT", "LT", "RB", "X" };
            GUIStyle row = Style(14, scale, false);
            row.alignment = TextAnchor.MiddleLeft;
            GUI.Box(new Rect(x, y + 2f, width, line * 4f + 6f), GUIContent.none);
            for (int i = 0; i < 4; i++)
            {
                GUI.color = i == 3 ? new Color(1f, 0.85f, 0.2f) : Color.white;
                GUI.Label(new Rect(x + 10f * scale, y + 4f + line * i, width - 20f * scale, line), buttons[i] + "    " + kit[i + 1], row);
            }
            GUI.color = previous;
        }
    }

    private static string[] KitFor(string wizardName)
    {
        foreach (string[] kit in Kits) if (kit[0] == wizardName) return kit;
        return null;
    }

    private static readonly string[] HowToPlay =
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

    private void DrawHowToPlay(float scale)
    {
        float width = 760f * scale, line = 30f * scale;
        float height = line * (HowToPlay.Length + 2f);
        Rect panel = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.95f);
        GUI.Box(panel, GUIContent.none);
        GUI.Box(panel, GUIContent.none);
        GUI.color = previous;
        GUI.Label(new Rect(panel.x, panel.y + line * 0.3f, width, line), "HOW TO PLAY", Style(24, scale, true));
        GUIStyle row = Style(17, scale, false);
        row.alignment = TextAnchor.MiddleLeft;
        for (int i = 0; i < HowToPlay.Length; i++)
            GUI.Label(new Rect(panel.x + 30f * scale, panel.y + line * (i + 1.5f), width - 60f * scale, line), HowToPlay[i], row);
    }

    private GameObject FindCharacter(GameObject cursorObject, GameObject previous)
    {
        GameObject closest = null; float distance = 6f;
        foreach (GameObject character in GameObject.FindGameObjectsWithTag("Character"))
        {
            float current = (character.transform.position - cursorObject.transform.position).sqrMagnitude;
            if (current < distance) { closest = character; distance = current; }
        }
        return closest == previous ? null : closest;
    }

    private void AttachWizard(GameObject player, string wizardName)
    {
        if (player == null) return;
        foreach (Component component in player.GetComponents<MonoBehaviour>())
            if (component.GetType().Name.EndsWith("Wizard")) DestroyImmediate(component, true);
        if (wizardName == "FireWizard") player.AddComponent<FireWizard>();
        else if (wizardName == "FrostWizard") player.AddComponent<FrostWizard>();
        else if (wizardName == "EarthWizard") player.AddComponent<EarthWizard>();
        else if (wizardName == "ElectricityWizard") player.AddComponent<ElectricityWizard>();
    }

    private void SetGui(bool enabled)
    {
        GameObject[] players = { GlobalHandlerScript == null ? null : GlobalHandlerScript.tempPlayer1, GlobalHandlerScript == null ? null : GlobalHandlerScript.tempPlayer2, GlobalHandlerScript == null ? null : GlobalHandlerScript.tempPlayer3, GlobalHandlerScript == null ? null : GlobalHandlerScript.tempPlayer4 };
        foreach (GameObject player in players) if (player != null) { PlayerStats stats = player.GetComponent<PlayerStats>(); if (stats != null) stats.GUIEnabled = enabled; }
    }

    private void SetJoinIndicator(GameObject target, bool visible) { if (target != null) { SpriteRenderer renderer = target.GetComponent<SpriteRenderer>(); if (renderer != null) renderer.enabled = !visible; } }
    private bool IsJoined(int slot) => slot == 0 ? GlobalHandlerScript.player1Joined : slot == 1 ? GlobalHandlerScript.player2Joined : slot == 2 ? GlobalHandlerScript.player3Joined : GlobalHandlerScript.player4Joined;
    private bool IsLocked(int slot) => slot == 0 ? GlobalHandlerScript.player1Locked : slot == 1 ? GlobalHandlerScript.player2Locked : slot == 2 ? GlobalHandlerScript.player3Locked : GlobalHandlerScript.player4Locked;
    private void SetLocked(int slot, bool value) { if (slot == 0) GlobalHandlerScript.player1Locked = value; else if (slot == 1) GlobalHandlerScript.player2Locked = value; else if (slot == 2) GlobalHandlerScript.player3Locked = value; else GlobalHandlerScript.player4Locked = value; }
    private GameObject GetPlayer(int slot) => slot == 0 ? GlobalHandlerScript.tempPlayer1 : slot == 1 ? GlobalHandlerScript.tempPlayer2 : slot == 2 ? GlobalHandlerScript.tempPlayer3 : GlobalHandlerScript.tempPlayer4;
    private GameObject GetBasePlayer(int slot) => slot == 0 ? GlobalHandlerScript.basePlayer1 : slot == 1 ? GlobalHandlerScript.basePlayer2 : slot == 2 ? GlobalHandlerScript.basePlayer3 : GlobalHandlerScript.basePlayer4;
    private void SyncSlot(int slot, bool joined, bool locked, string wizard) { GameObject bootstrap = GameObject.Find("LocalMultiplayerBootstrap"); LegacyGlobalStateBridge bridge = bootstrap == null ? null : bootstrap.GetComponent<LegacyGlobalStateBridge>(); if (bridge != null) bridge.RegisterLegacySlot(slot, joined, locked, wizard, "Player" + (slot + 1)); }
    private void EnsureLocalMultiplayerRuntime() { GameObject bootstrap = GameObject.Find("LocalMultiplayerBootstrap"); if (bootstrap == null) bootstrap = new GameObject("LocalMultiplayerBootstrap"); if (bootstrap.GetComponent<LocalMultiplayerBootstrap>() == null) bootstrap.AddComponent<LocalMultiplayerBootstrap>(); if (bootstrap.GetComponent<LegacyGlobalStateBridge>() == null) bootstrap.AddComponent<LegacyGlobalStateBridge>(); if (bootstrap.GetComponent<LocalPlayerManager>() == null) bootstrap.AddComponent<LocalPlayerManager>(); if (bootstrap.GetComponent<MatchManager>() == null) bootstrap.AddComponent<MatchManager>(); }
}