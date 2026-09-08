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

        bool ready = GlobalHandlerScript.playersJoined > 0 &&
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