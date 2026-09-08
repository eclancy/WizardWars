using UnityEngine;
using UnityEngine.SceneManagement;
using WizardWars.LocalMultiplayer;

public class StageSelect : MonoBehaviour
{
    public GameObject currentStage;
    public GameObject highlighter;
    public GameObject Stage1;
    public GameObject Stage2;
    public GameObject Stage3;
    public GameObject Stage4;
    public GameObject p1CursorObject;
    public GameObject p2CursorObject;
    public GameObject p3CursorObject;
    public GameObject p4CursorObject;
    public GameObject p1cursor;
    public GameObject p2cursor;
    public GameObject p3cursor;
    public GameObject p4cursor;
    public bool cursor;

    private GlobalHandler globalHandler;

    private void Awake()
    {
        GameObject globalHandlerObject = GameObject.Find("GlobalHandler");
        if (globalHandlerObject != null) globalHandler = globalHandlerObject.GetComponent<GlobalHandler>();
        EnsureLocalMultiplayerRuntime();
    }

    private void Start()
    {
        currentStage = Stage1;
        if (globalHandler == null) return;

        int selectedPlayer = Random.Range(1, 5);
        for (int attempt = 0; attempt < 4 && !cursor; attempt++)
        {
            int playerNumber = (selectedPlayer + attempt - 1) % 4 + 1;
            if (IsPlayerJoined(playerNumber)) CreateCursor(playerNumber);
        }
    }

    private void Update()
    {
        if (p1cursor != null) currentStage = FindSelection(p1cursor);
        if (p2cursor != null) currentStage = FindSelection(p2cursor);
        if (p3cursor != null) currentStage = FindSelection(p3cursor);
        if (p4cursor != null) currentStage = FindSelection(p4cursor);

        bool confirmed = (Input.GetButtonDown("A_1") && p1cursor != null) ||
                         (Input.GetButtonDown("A_2") && p2cursor != null) ||
                         (Input.GetButtonDown("A_3") && p3cursor != null) ||
                         (Input.GetButtonDown("A_4") && p4cursor != null);
        if (confirmed && currentStage != null)
        {
            int stageNumber = currentStage == Stage1 ? 1 : currentStage == Stage2 ? 2 : currentStage == Stage3 ? 3 : 4;
            globalHandler.setSelectedStage(stageNumber);
            LegacyGlobalStateBridge bridge = FindBridge();
            if (bridge != null) bridge.SetSelectedStage(stageNumber);
            SceneManager.LoadScene("Park");
        }

        if (cursor && currentStage != null && highlighter != null) highlighter.transform.position = currentStage.transform.position;
    }

    private GameObject FindSelection(GameObject selectionCursor)
    {
        GameObject closest = currentStage;
        float distance = 20f;
        foreach (GameObject stage in GameObject.FindGameObjectsWithTag("Stage"))
        {
            float currentDistance = (stage.transform.position - selectionCursor.transform.position).sqrMagnitude;
            if (currentDistance < distance) { closest = stage; distance = currentDistance; }
        }
        return closest;
    }

    private bool IsPlayerJoined(int playerNumber)
    {
        return playerNumber == 1 ? globalHandler.player1Joined : playerNumber == 2 ? globalHandler.player2Joined : playerNumber == 3 ? globalHandler.player3Joined : globalHandler.player4Joined;
    }

    private void CreateCursor(int playerNumber)
    {
        GameObject prefab = playerNumber == 1 ? p1CursorObject : playerNumber == 2 ? p2CursorObject : playerNumber == 3 ? p3CursorObject : p4CursorObject;
        GameObject created = prefab == null ? null : Instantiate(prefab);
        if (playerNumber == 1) p1cursor = created;
        if (playerNumber == 2) p2cursor = created;
        if (playerNumber == 3) p3cursor = created;
        if (playerNumber == 4) p4cursor = created;
        if (created != null) { created.layer = 3; cursor = true; }
    }

    private void EnsureLocalMultiplayerRuntime()
    {
        GameObject bootstrapObject = GameObject.Find("LocalMultiplayerBootstrap");
        if (bootstrapObject == null) bootstrapObject = new GameObject("LocalMultiplayerBootstrap");
        if (bootstrapObject.GetComponent<LocalMultiplayerBootstrap>() == null) bootstrapObject.AddComponent<LocalMultiplayerBootstrap>();
        if (bootstrapObject.GetComponent<LegacyGlobalStateBridge>() == null) bootstrapObject.AddComponent<LegacyGlobalStateBridge>();
        if (bootstrapObject.GetComponent<LocalPlayerManager>() == null) bootstrapObject.AddComponent<LocalPlayerManager>();
        if (bootstrapObject.GetComponent<MatchManager>() == null) bootstrapObject.AddComponent<MatchManager>();
    }

    private LegacyGlobalStateBridge FindBridge()
    {
        GameObject bootstrapObject = GameObject.Find("LocalMultiplayerBootstrap");
        return bootstrapObject == null ? null : bootstrapObject.GetComponent<LegacyGlobalStateBridge>();
    }
}