using UnityEngine;
using UnityEngine.SceneManagement;

public class StartToPlay : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetButtonDown("Start_1") ||
            Input.GetButtonDown("Start_2") ||
            Input.GetButtonDown("Start_3") ||
            Input.GetButtonDown("Start_4"))
        {
            SceneManager.LoadScene("Character Select");
        }
    }

    private void OnGUI()
    {
        const float boxWidth = 460f;
        const float boxHeight = 180f;
        float boxX = 20f;
        float boxY = Screen.height - boxHeight - 20f;

        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "Keyboard Controls (Player 1)");
        GUI.Label(new Rect(boxX + 12f, boxY + 28f, boxWidth - 24f, 22f), "Move: W A S D      Aim: Arrow Keys");
        GUI.Label(new Rect(boxX + 12f, boxY + 50f, boxWidth - 24f, 22f), "Basic: J      Second: K      Third: E");
        GUI.Label(new Rect(boxX + 12f, boxY + 72f, boxWidth - 24f, 22f), "Fourth: Q      Ultimate: R");
        GUI.Label(new Rect(boxX + 12f, boxY + 94f, boxWidth - 24f, 22f), "Join / Start: Enter      Back / Leave: Backspace");
        GUI.Label(new Rect(boxX + 12f, boxY + 116f, boxWidth - 24f, 22f), "Controller: LB = Fourth      RB = Third      X = Ultimate");
        GUI.Label(new Rect(boxX + 12f, boxY + 138f, boxWidth - 24f, 22f), "Controller support remains enabled for all players.");
    }
}