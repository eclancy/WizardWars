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