using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardWars.LocalMultiplayer;

public class GlobalHandler : MonoBehaviour
{
    public int playersAlive;
    public int playersJoined;
    public GameObject basePlayer1;
    public GameObject basePlayer2;
    public GameObject basePlayer3;
    public GameObject basePlayer4;
    public GameObject tempPlayer1;
    public GameObject tempPlayer2;
    public GameObject tempPlayer3;
    public GameObject tempPlayer4;
    public bool player1Joined;
    public bool player2Joined;
    public bool player3Joined;
    public bool player4Joined;
    public bool player1Locked;
    public bool player2Locked;
    public bool player3Locked;
    public bool player4Locked;
    public bool player1Alive;
    public bool player2Alive;
    public bool player3Alive;
    public bool player4Alive;
    public bool AIAlive;
    public GameObject p1Wins;
    public GameObject p2Wins;
    public GameObject p3Wins;
    public GameObject p4Wins;
    public GameObject AI;
    public bool battling;
    public int selectedStage = 1;
    public bool runtimeBridgeReady;

    // Teams mode. teamsMode is the choice made on Character Select and carries over
    // between matches; teamsActive says whether the battle in progress is actually
    // being played as teams, which also needs two players on two different teams.
    // None of this is serialized, so the scene cannot pin a default.
    [System.NonSerialized] public bool teamsMode;
    [System.NonSerialized] public bool teamsActive;
    [System.NonSerialized] public int player1Team = Teams.DefaultForSlot(0);
    [System.NonSerialized] public int player2Team = Teams.DefaultForSlot(1);
    [System.NonSerialized] public int player3Team = Teams.DefaultForSlot(2);
    [System.NonSerialized] public int player4Team = Teams.DefaultForSlot(3);
    private bool matchOver;
    private int winningTeam = Teams.NoTeam;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureLocalMultiplayerRuntime();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start() => EnsureLocalMultiplayerRuntime();

    public void battleStart() => battling = true;

    public int GetTeam(int slot) => slot == 0 ? player1Team : slot == 1 ? player2Team : slot == 2 ? player3Team : player4Team;

    public void SetTeam(int slot, int team)
    {
        if (slot == 0) player1Team = team; else if (slot == 1) player2Team = team; else if (slot == 2) player3Team = team; else player4Team = team;
    }

    public bool IsJoined(int slot) => slot == 0 ? player1Joined : slot == 1 ? player2Joined : slot == 2 ? player3Joined : player4Joined;

    // Teams needs at least two players and somebody on each side. A lone player in
    // Teams mode falls back to the usual Free-for-All against the AI.
    public bool TeamsPlayable()
    {
        if (!teamsMode) return false;
        bool red = false, blue = false;
        for (int slot = 0; slot < 4; slot++)
        {
            if (!IsJoined(slot)) continue;
            if (GetTeam(slot) == Teams.Red) red = true; else blue = true;
        }
        return red && blue;
    }

    // Called by ParkBattle as it spawns the players, so the match knows its rules
    // before anyone can be killed.
    public void BeginMatch()
    {
        teamsActive = TeamsPlayable();
        matchOver = false;
        winningTeam = Teams.NoTeam;
    }

    public void setSelectedStage(int stageNumber)
    {
        selectedStage = stageNumber;
        LegacyGlobalStateBridge bridge = FindBridge();
        if (bridge != null) bridge.SetSelectedStage(stageNumber);
    }

    public int getSelectedStage() => selectedStage;
    public bool getBattling() => battling;

    public void joined(string player)
    {
        if (player == "Player1" && !player1Joined) { player1Joined = true; playersJoined++; }
        if (player == "Player2" && !player2Joined) { player2Joined = true; playersJoined++; }
        if (player == "Player3" && !player3Joined) { player3Joined = true; playersJoined++; }
        if (player == "Player4" && !player4Joined) { player4Joined = true; playersJoined++; }
        SyncLegacyStateToRuntime();
    }

    public void left(string player)
    {
        if (player == "Player1") ResetPlayer(1, ref player1Joined, ref player1Locked, ref player1Alive, basePlayer1, ref tempPlayer1);
        if (player == "Player2") ResetPlayer(2, ref player2Joined, ref player2Locked, ref player2Alive, basePlayer2, ref tempPlayer2);
        if (player == "Player3") ResetPlayer(3, ref player3Joined, ref player3Locked, ref player3Alive, basePlayer3, ref tempPlayer3);
        if (player == "Player4") ResetPlayer(4, ref player4Joined, ref player4Locked, ref player4Alive, basePlayer4, ref tempPlayer4);
        SyncLegacyStateToRuntime();
    }

    public void kill(string character)
    {
        playersAlive--;
        if (character == "Player1(Clone)") player1Alive = false;
        if (character == "Player2(Clone)") player2Alive = false;
        if (character == "Player3(Clone)") player3Alive = false;
        if (character == "Player4(Clone)") player4Alive = false;
        if (character == "AI(Clone)") AIAlive = false;
        if (matchOver) return;
        if (teamsActive ? AliveTeamCount() <= 1 : playersAlive == 1)
        {
            matchOver = true;
            StartCoroutine(teamsActive ? FinishTeamMatch() : FinishMatch());
        }
    }

    private int AliveTeamCount()
    {
        bool red = false, blue = false;
        bool[] alive = { player1Alive, player2Alive, player3Alive, player4Alive };
        for (int slot = 0; slot < 4; slot++)
        {
            if (!alive[slot]) continue;
            if (GetTeam(slot) == Teams.Red) red = true; else blue = true;
        }
        return (red ? 1 : 0) + (blue ? 1 : 0);
    }

    private IEnumerator FinishMatch()
    {
        GameObject winner = null;
        string winningPlayerName = string.Empty;
        if (player1Alive) { winner = Instantiate(p1Wins); winningPlayerName = "Player1"; }
        if (player2Alive) { winner = Instantiate(p2Wins); winningPlayerName = "Player2"; }
        if (player3Alive) { winner = Instantiate(p3Wins); winningPlayerName = "Player3"; }
        if (player4Alive) { winner = Instantiate(p4Wins); winningPlayerName = "Player4"; }
        LegacyGlobalStateBridge bridge = FindBridge();
        if (bridge != null && winningPlayerName != string.Empty) bridge.RegisterWinner(winningPlayerName);
        yield return new WaitForSeconds(5f);
        if (winner != null) Destroy(winner);
        ReturnToCharacterSelect();
    }

    // There is no authored "team wins" art, so the result is drawn by OnGUI below.
    private IEnumerator FinishTeamMatch()
    {
        bool[] alive = { player1Alive, player2Alive, player3Alive, player4Alive };
        for (int slot = 0; slot < 4; slot++) if (alive[slot]) { winningTeam = GetTeam(slot); break; }
        LegacyGlobalStateBridge bridge = FindBridge();
        if (bridge != null && winningTeam != Teams.NoTeam) bridge.RegisterWinningTeam(winningTeam);
        yield return new WaitForSeconds(5f);
        winningTeam = Teams.NoTeam;
        ReturnToCharacterSelect();
    }

    private void OnGUI()
    {
        if (winningTeam == Teams.NoTeam) return;
        GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(32, Screen.height / 9), fontStyle = FontStyle.Bold };
        Rect rect = new Rect(0f, Screen.height * 0.3f, Screen.width, Screen.height * 0.25f);
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.Label(new Rect(rect.x + 4f, rect.y + 4f, rect.width, rect.height), Teams.Name(winningTeam).ToUpper() + " TEAM WINS", style);
        GUI.color = Teams.Colour(winningTeam);
        GUI.Label(rect, Teams.Name(winningTeam).ToUpper() + " TEAM WINS", style);
        GUI.color = previous;
    }

    private void ReturnToCharacterSelect()
    {
        teamsActive = false;
        SceneManager.LoadScene("Character Select");
        Transform menuMusic = transform.Find("menuMusic");
        if (menuMusic != null) menuMusic.GetComponent<AudioSource>()?.Play();
        battling = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Character Select") return;
        LegacyGlobalStateBridge bridge = FindBridge();
        if (bridge != null) bridge.ResetRuntimeState();
        left("Player1"); left("Player2"); left("Player3"); left("Player4");
    }

    private void ResetPlayer(int number, ref bool joinedState, ref bool locked, ref bool alive, GameObject basePlayer, ref GameObject tempPlayer)
    {
        if (joinedState) playersJoined--;
        joinedState = false; locked = false; alive = false;
        SetTeam(number - 1, Teams.DefaultForSlot(number - 1));
        if (basePlayer != null)
        {
            SpriteRenderer renderer = basePlayer.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sprite = null;
            tempPlayer = basePlayer;
        }
    }

    private void EnsureLocalMultiplayerRuntime()
    {
        if (runtimeBridgeReady) return;
        GameObject bootstrapObject = GameObject.Find("LocalMultiplayerBootstrap");
        if (bootstrapObject == null) bootstrapObject = new GameObject("LocalMultiplayerBootstrap");
        if (bootstrapObject.GetComponent<LocalMultiplayerBootstrap>() == null) bootstrapObject.AddComponent<LocalMultiplayerBootstrap>();
        if (bootstrapObject.GetComponent<LocalPlayerManager>() == null) bootstrapObject.AddComponent<LocalPlayerManager>();
        if (bootstrapObject.GetComponent<MatchManager>() == null) bootstrapObject.AddComponent<MatchManager>();
        if (bootstrapObject.GetComponent<LegacyGlobalStateBridge>() == null) bootstrapObject.AddComponent<LegacyGlobalStateBridge>();
        runtimeBridgeReady = true;
    }

    private void SyncLegacyStateToRuntime()
    {
        EnsureLocalMultiplayerRuntime();
        LegacyGlobalStateBridge bridge = FindBridge();
        if (bridge == null) return;
        bridge.RegisterLegacySlot(0, player1Joined, player1Locked, "", "Player1");
        bridge.RegisterLegacySlot(1, player2Joined, player2Locked, "", "Player2");
        bridge.RegisterLegacySlot(2, player3Joined, player3Locked, "", "Player3");
        bridge.RegisterLegacySlot(3, player4Joined, player4Locked, "", "Player4");
        playersJoined = bridge.GetJoinedPlayerCount();
    }

    private LegacyGlobalStateBridge FindBridge()
    {
        GameObject bootstrapObject = GameObject.Find("LocalMultiplayerBootstrap");
        return bootstrapObject == null ? null : bootstrapObject.GetComponent<LegacyGlobalStateBridge>();
    }
}