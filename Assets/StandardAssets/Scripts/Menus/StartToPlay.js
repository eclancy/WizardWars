#pragma strict

function Start () {

}

function Update () {

	if( Input.GetButtonDown("Start_1") || 
		Input.GetButtonDown("Start_2") || 
		Input.GetButtonDown("Start_3") || 
		Input.GetButtonDown("Start_4") ) {
		
		Application.LoadLevel("Character Select");
	
	}
}

function OnGUI() {
	var boxWidth = 430;
	var boxHeight = 140;
	var boxX = 20;
	var boxY = Screen.height - boxHeight - 20;

	GUI.Box(Rect(boxX, boxY, boxWidth, boxHeight), "Keyboard Controls (Player 1)");
	GUI.Label(Rect(boxX + 12, boxY + 28, boxWidth - 24, 22), "Move: W A S D      Aim: Arrow Keys");
	GUI.Label(Rect(boxX + 12, boxY + 50, boxWidth - 24, 22), "Basic: J      Second: K      Third: E      Ultimate: Q");
	GUI.Label(Rect(boxX + 12, boxY + 72, boxWidth - 24, 22), "Join / Start: Enter      Back / Leave: Backspace");
	GUI.Label(Rect(boxX + 12, boxY + 102, boxWidth - 24, 22), "Controller support remains enabled for all players.");
}