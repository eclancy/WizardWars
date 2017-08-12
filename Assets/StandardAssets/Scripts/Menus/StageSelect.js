
var currentStage : GameObject;
var highlighter : GameObject;
var Stage1 : GameObject;
var Stage2 : GameObject;
var Stage3 : GameObject;
var Stage4 : GameObject;

var p1CursorObject : GameObject;
var p2CursorObject : GameObject;
var p3CursorObject : GameObject;
var p4CursorObject : GameObject;
var p1cursor;
var p2cursor;
var p3cursor;
var p4cursor;
var cursor = false;


var GlobalHandlerObject ;
var GlobalHandlerScript;


function Awake(){
	GlobalHandlerObject = GameObject.Find("GlobalHandler");
  	GlobalHandlerScript = GlobalHandlerObject.GetComponent("GlobalHandler");
  	startToPlay = GameObject.Find("StartToBattle");
}
  
function Start () {
	
	currentStage = Stage1;
	//keep going until a joined player is successfully, randomly selected
	while( cursor == false ){
		//random from 0-99
		var randomSelect = Random.value * 99;
		//0-24
		if ( GlobalHandlerScript.player1Joined == true && randomSelect <= 25 ){
			p1cursor = Instantiate(p1CursorObject);
			cursor = true;
		} 
		//25-49
		if ( GlobalHandlerScript.player2Joined == true && randomSelect >= 25 && randomSelect < 50 ){
			p2cursor = Instantiate(p2CursorObject);
			cursor = true;
		}
		//50-74
		if (  GlobalHandlerScript.player3Joined == true && randomSelect >= 50 && randomSelect < 75 ){
			p3cursor = Instantiate(p3CursorObject);
			cursor = true;
		}
		//75-99
		if (  GlobalHandlerScript.player4Joined == true && randomSelect >= 75 && randomSelect < 100 ){
			p4cursor = Instantiate(p4CursorObject);
			cursor = true;
		}
	}
}

function Update () {
 	
 	
 	if ( p1cursor ){
		currentStage = FindSelection(p1cursor);
	} 
	if ( p2cursor ){
		currentStage = FindSelection(p2cursor);
	}
	if ( p3cursor ){
		currentStage = FindSelection(p3cursor);
	}
	if ( p4cursor ){
		currentStage = FindSelection(p4cursor);
	}
 	
 	
 	
 	if ( Input.GetButtonDown("A_1") && p1cursor ){
		if(currentStage == Stage1){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage2){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage3){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage4){
			Application.LoadLevel("Park");
		}
	} 
	if ( Input.GetButtonDown("A_2") && p2cursor ){
			if(currentStage == Stage1){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage2){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage3){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage4){
			Application.LoadLevel("Park");
		}
	}
	if ( Input.GetButtonDown("A_3") && p3cursor ){
			if(currentStage == Stage1){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage2){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage3){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage4){
			Application.LoadLevel("Park");
		}
	}
	if ( Input.GetButtonDown("A_4") && p4cursor ){
			if(currentStage == Stage1){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage2){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage3){
			Application.LoadLevel("Park");
		}
		if(currentStage == Stage4){
			Application.LoadLevel("Park");
		}
	}
	

	if(cursor == true && currentStage != null){
		highlighter.transform.position = currentStage.transform.position;
	}


}






function FindSelection( cursor ) {
	gos = GameObject.FindGameObjectsWithTag("Stage");
    distance = 20;
    for ( var go in gos ) {

    	var diff = go.transform.position - cursor.transform.position;
    	var curDistance = diff.sqrMagnitude;
    	if (curDistance < distance) {
    		closestSelection = go;
    		distance = curDistance;
    	}
 	}
    return closestSelection;
}





