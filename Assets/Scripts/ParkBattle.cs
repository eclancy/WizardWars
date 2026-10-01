using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizardWars.LocalMultiplayer;

public class ParkBattle : MonoBehaviour
{
    public GameObject GlobalHandler;
    public GlobalHandler GlobalHandlerScript;
    public GameObject three;
    public GameObject two;
    public GameObject one;
    public GameObject go;
    public float startTimer;
    public GameObject oneRef;
    public GameObject twoRef;
    public GameObject threeRef;
    public GameObject goRef;
    public GameObject p1;
    public GameObject p2;
    public GameObject p3;
    public GameObject p4;
    public GameObject p5;
    public Vector3 p1Spawn;
    public Vector3 p2Spawn;
    public Vector3 p3Spawn;
    public Vector3 p4Spawn;
    public Vector3 aiSpawn;
    public GameObject rockObstaclePrefab;
    public GameObject cactusObstaclePrefab;
    public Sprite stage1Background;
    public Sprite stage2Background;
    public Sprite stage3Background;
    public Sprite stage4Background;

    private void Awake()
    {
        GlobalHandler = GameObject.Find("GlobalHandler");
        if (GlobalHandler != null) GlobalHandlerScript = GlobalHandler.GetComponent<GlobalHandler>();
        EnsureLocalMultiplayerRuntime();
        if (GlobalHandlerScript == null) return;
        GlobalHandlerScript.playersAlive = 0;
        Transform music = GlobalHandler.transform.Find("menuMusic");
        if (music != null) music.GetComponent<AudioSource>()?.Stop();
        rockObstaclePrefab = Resources.Load<GameObject>("RockObstacle");
        cactusObstaclePrefab = Resources.Load<GameObject>("CactusObstacle");
        ConfigureStageVariant();
        GlobalHandlerScript.BeginMatch();
        if (GlobalHandlerScript.teamsActive) ArrangeTeamSpawns();
        SpawnPlayers();
        StartCoroutine(StartCountDown());
        LegacyGlobalStateBridge bridge = FindBridge();
        if (bridge != null) bridge.StartMatchForLegacyStage();
    }

    private void SpawnPlayers()
    {
        SpawnPlayer(GlobalHandlerScript.player1Joined, GlobalHandlerScript.tempPlayer1, p1Spawn, ref p1, 1);
        SpawnPlayer(GlobalHandlerScript.player2Joined, GlobalHandlerScript.tempPlayer2, p2Spawn, ref p2, 2);
        SpawnPlayer(GlobalHandlerScript.player3Joined, GlobalHandlerScript.tempPlayer3, p3Spawn, ref p3, 3);
        SpawnPlayer(GlobalHandlerScript.player4Joined, GlobalHandlerScript.tempPlayer4, p4Spawn, ref p4, 4);
        if (GlobalHandlerScript.playersAlive == 1 || GetRuntimeParticipantCount() <= 1)
        {
            p5 = GlobalHandlerScript.AI == null ? null : Instantiate(GlobalHandlerScript.AI, aiSpawn, Quaternion.identity);
            if (p5 != null) { p5.transform.localScale = Vector3.one * 0.3f; GlobalHandlerScript.AIAlive = true; GlobalHandlerScript.playersAlive++; PlayerStats stats = p5.GetComponent<PlayerStats>(); if (stats != null) stats.GUIEnabled = true; }
        }
    }

    private void SpawnPlayer(bool joined, GameObject source, Vector3 position, ref GameObject result, int playerNumber)
    {
        if (!joined || source == null) return;
        result = Instantiate(source, position, Quaternion.identity);
        result.transform.localScale = Vector3.one * 0.15f;
        PlayerStats stats = result.GetComponent<PlayerStats>();
        if (stats != null) stats.SetTeam(GlobalHandlerScript.teamsActive ? GlobalHandlerScript.GetTeam(playerNumber - 1) : Teams.NoTeam);
        if (playerNumber == 1) GlobalHandlerScript.player1Alive = true;
        if (playerNumber == 2) GlobalHandlerScript.player2Alive = true;
        if (playerNumber == 3) GlobalHandlerScript.player3Alive = true;
        if (playerNumber == 4) GlobalHandlerScript.player4Alive = true;
        GlobalHandlerScript.playersAlive++;
    }

    private void ConfigureStageVariant()
    {
        p1Spawn = new Vector3(-7f, 4f, 0f); p2Spawn = new Vector3(7f, 4f, 0f); p3Spawn = new Vector3(-7f, -4f, 0f); p4Spawn = new Vector3(7f, -4f, 0f); aiSpawn = p4Spawn;
        int stage = GlobalHandlerScript.getSelectedStage();
        Sprite background = stage == 2 ? stage2Background : stage == 3 ? stage3Background : stage == 4 ? stage4Background : stage1Background;
        if (background != null) { SpriteRenderer renderer = GetComponent<SpriteRenderer>(); if (renderer != null) renderer.sprite = background; }
        if (stage == 2) { p1Spawn = new Vector3(-8, 0, 0); p2Spawn = new Vector3(8, 0, 0); p3Spawn = new Vector3(0, 5, 0); p4Spawn = new Vector3(0, -5, 0); aiSpawn = p2Spawn; SpawnObstacle(rockObstaclePrefab, new Vector3(-3, 2.5f, 0)); SpawnObstacle(rockObstaclePrefab, new Vector3(3, -2.5f, 0)); }
        if (stage == 3) { p1Spawn = new Vector3(-7, 0, 0); p2Spawn = new Vector3(7, 0, 0); p3Spawn = new Vector3(0, 5, 0); p4Spawn = new Vector3(0, -5, 0); aiSpawn = p2Spawn; SpawnObstacle(cactusObstaclePrefab, new Vector3(-3, 2.5f, 0)); SpawnObstacle(cactusObstaclePrefab, new Vector3(3, -2.5f, 0)); SpawnObstacle(rockObstaclePrefab, new Vector3(3, 2.5f, 0)); }
        if (stage == 4) { p1Spawn = new Vector3(-8, 4.5f, 0); p2Spawn = new Vector3(8, 4.5f, 0); p3Spawn = new Vector3(-8, -4.5f, 0); p4Spawn = new Vector3(8, -4.5f, 0); aiSpawn = p4Spawn; SpawnObstacle(rockObstaclePrefab, new Vector3(-4, 0, 0)); SpawnObstacle(rockObstaclePrefab, new Vector3(4, 0, 0)); }
    }

    // Teammates start together: Red takes the player 1 and 3 spawns, Blue takes the
    // player 2 and 4 spawns. A team with more than two players spills over into
    // whatever spawns the other side left unused.
    private void ArrangeTeamSpawns()
    {
        List<Vector3>[] sides = { new List<Vector3> { p1Spawn, p3Spawn }, new List<Vector3> { p2Spawn, p4Spawn } };
        Vector3[] spawns = { p1Spawn, p2Spawn, p3Spawn, p4Spawn };
        List<int> overflow = new List<int>();
        for (int slot = 0; slot < 4; slot++)
        {
            if (!GlobalHandlerScript.IsJoined(slot)) continue;
            List<Vector3> side = sides[GlobalHandlerScript.GetTeam(slot) == Teams.Red ? 0 : 1];
            if (side.Count == 0) { overflow.Add(slot); continue; }
            spawns[slot] = side[0];
            side.RemoveAt(0);
        }
        foreach (int slot in overflow)
        {
            List<Vector3> side = sides[0].Count > 0 ? sides[0] : sides[1];
            spawns[slot] = side[0];
            side.RemoveAt(0);
        }
        p1Spawn = spawns[0]; p2Spawn = spawns[1]; p3Spawn = spawns[2]; p4Spawn = spawns[3];
    }

    private void SpawnObstacle(GameObject prefab, Vector3 position) { if (prefab != null) Instantiate(prefab, position, Quaternion.identity); }
    private IEnumerator StartCountDown() { threeRef = Instantiate(three); yield return new WaitForSeconds(1); Destroy(threeRef); twoRef = Instantiate(two); yield return new WaitForSeconds(1); Destroy(twoRef); oneRef = Instantiate(one); yield return new WaitForSeconds(1); Destroy(oneRef); GlobalHandlerScript.battleStart(); }
    private int GetRuntimeParticipantCount() { LegacyGlobalStateBridge bridge = FindBridge(); return bridge == null ? 0 : bridge.GetJoinedPlayerCount(); }
    private LegacyGlobalStateBridge FindBridge() { GameObject bootstrap = GameObject.Find("LocalMultiplayerBootstrap"); return bootstrap == null ? null : bootstrap.GetComponent<LegacyGlobalStateBridge>(); }
    private void EnsureLocalMultiplayerRuntime() { GameObject bootstrap = GameObject.Find("LocalMultiplayerBootstrap"); if (bootstrap == null) bootstrap = new GameObject("LocalMultiplayerBootstrap"); if (bootstrap.GetComponent<LocalMultiplayerBootstrap>() == null) bootstrap.AddComponent<LocalMultiplayerBootstrap>(); if (bootstrap.GetComponent<LegacyGlobalStateBridge>() == null) bootstrap.AddComponent<LegacyGlobalStateBridge>(); if (bootstrap.GetComponent<LocalPlayerManager>() == null) bootstrap.AddComponent<LocalPlayerManager>(); if (bootstrap.GetComponent<MatchManager>() == null) bootstrap.AddComponent<MatchManager>(); }
}