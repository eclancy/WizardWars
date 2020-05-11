var GlobalHandlerObject : GameObject;
var GlobalHandlerScript : GlobalHandler;
var p1;
var p2;
var p3;
var p4;
var p1CursorObject : GameObject;
var p2CursorObject : GameObject;
var p3CursorObject : GameObject;
var p4CursorObject : GameObject;
var p1cursor;
var p2cursor;
var p3cursor;
var p4cursor;
var p1join: GameObject;
var p2join: GameObject;
var p3join: GameObject;
var p4join: GameObject;
var oneselected;
var twoselected;
var threeselected;
var fourselected;
var startToPlay : GameObject;
	

function Start(){
	GlobalHandlerScript.playersJoined = 0;
	GlobalHandlerScript.player1Joined = false;
	GlobalHandlerScript.player2Joined = false;
	GlobalHandlerScript.player3Joined = false;
	GlobalHandlerScript.player4Joined = false;

	GlobalHandlerScript.player1Locked = false;
	GlobalHandlerScript.player2Locked = false;
	GlobalHandlerScript.player3Locked = false;
	GlobalHandlerScript.player4Locked = false;

}

function Awake(){
  GlobalHandlerObject = GameObject.Find("GlobalHandler");
  GlobalHandlerScript = GlobalHandlerObject.GetComponent("GlobalHandler");
  startToPlay = GameObject.Find("StartToBattle");
}

function Update(){


	//for every joined player, render the sprite they picked
	//else display something in each player's box telling them to press start to join
	if(GlobalHandlerScript.player1Joined == true ){
		p1join.GetComponent(SpriteRenderer).enabled = false;
	}
	else{
		p1join.GetComponent(SpriteRenderer).enabled = true;
	}
	if(GlobalHandlerScript.player2Joined == true ){
		p2join.GetComponent(SpriteRenderer).enabled = false;
	}
	else{
		p2join.GetComponent(SpriteRenderer).enabled = true;
	}
	if(GlobalHandlerScript.player3Joined == true ){
		p3join.GetComponent(SpriteRenderer).enabled = false;
	}
	else{
		p3join.GetComponent(SpriteRenderer).enabled = true;
	}
	if(GlobalHandlerScript.player4Joined == true ){
		p4join.GetComponent(SpriteRenderer).enabled = false;
	}
	else{
		p4join.GetComponent(SpriteRenderer).enabled = true;
	}

	//**************************player joined**********************
	if ( Input.GetButtonDown("Start_1") && GlobalHandlerScript.player1Joined == false ){
		GlobalHandlerScript.joined("Player1");
		p1 = Instantiate( GlobalHandlerScript.tempPlayer1 );
		p1.GetComponent(SpriteRenderer).enabled = false;
		p1.transform.position.x = -7.437364;
		p1.transform.position.y = -4.139224;
		p1.transform.localScale.x = .3;
		p1.transform.localScale.y = .3;
		p1cursor = Instantiate(p1CursorObject);
		p1cursor.layer = 3; 
		
	} 
	if ( Input.GetButtonDown("Start_2") && GlobalHandlerScript.player2Joined == false ){
		GlobalHandlerScript.joined("Player2");
		p2 = Instantiate( GlobalHandlerScript.tempPlayer2 );
		p2.transform.position.x = -1.995348;
		p2.transform.position.y = -4.139226;
		p2.transform.localScale.x = .3;
		p2.transform.localScale.y = .3;
		p2cursor = Instantiate(p2CursorObject);
		p2cursor.layer = 3;
		
	}
	if ( Input.GetButtonDown("Start_3") && GlobalHandlerScript.player3Joined == false ){
		GlobalHandlerScript.joined("Player3");
		p3 = Instantiate( GlobalHandlerScript.tempPlayer3 );
		p3.transform.position.x = 3.446668;
		p3.transform.position.y = -4.139226;
		p3.transform.localScale.x = .3;
		p3.transform.localScale.y = .3;
		p3cursor = Instantiate(p3CursorObject);
		p3cursor.layer = 3;
	}
	if ( Input.GetButtonDown("Start_4") && GlobalHandlerScript.player4Joined == false ){
		GlobalHandlerScript.joined("Player4");
		p4 = Instantiate( GlobalHandlerScript.tempPlayer4 );
		p4.transform.position.x = 8.888684;
		p4.transform.position.y = -4.139226;
		p4.transform.localScale.x = .3;
		p4.transform.localScale.y = .3;
		p4cursor = Instantiate(p4CursorObject);
		p4cursor.layer = 3;
	}
	
	//****************************Start Game*************************//
	
			
	//if everyone that's joined is locked
	if( ( GlobalHandlerScript.player1Locked == true || GlobalHandlerScript.player1Joined == false ) &&
		( GlobalHandlerScript.player2Locked == true || GlobalHandlerScript.player2Joined == false ) &&
		( GlobalHandlerScript.player3Locked == true || GlobalHandlerScript.player3Joined == false ) &&
		( GlobalHandlerScript.player4Locked == true || GlobalHandlerScript.player4Joined == false ) &&
		  GlobalHandlerScript.playersJoined > 0 ){
		  
		startToPlay.GetComponent(SpriteRenderer).enabled = true;
			
		//if someone presses start
		if ( Input.GetButtonDown("Start_1") && GlobalHandlerScript.player1Locked == true ||
			 Input.GetButtonDown("Start_2") && GlobalHandlerScript.player2Locked == true ||
			 Input.GetButtonDown("Start_3") && GlobalHandlerScript.player3Locked == true ||
			 Input.GetButtonDown("Start_4") && GlobalHandlerScript.player4Locked == true ){
		
			if(oneselected){
				removePreviousScripts(GlobalHandlerScript.tempPlayer1);

				if(oneselected.name == "FireWizard"){
					tempPlayer1 = GlobalHandlerScript.tempPlayer1.AddComponent(FireWizard) as MonoScript;
				}
				if(oneselected.name == "FrostWizard"){
					tempPlayer1 = GlobalHandlerScript.tempPlayer1.AddComponent(FrostWizard) as MonoScript;
				}
				if(oneselected.name == "EarthWizard"){
					tempPlayer1 = GlobalHandlerScript.tempPlayer1.AddComponent(EarthWizard) as MonoScript;
				}
				if(oneselected.name == "ElectricityWizard"){
					tempPlayer1 = GlobalHandlerScript.tempPlayer1.AddComponent(ElectricityWizard) as MonoScript;
				}
			}
			if(twoselected){
				removePreviousScripts(GlobalHandlerScript.tempPlayer2);

				if(twoselected.name == "FireWizard"){
					tempPlayer2 = GlobalHandlerScript.tempPlayer2.AddComponent(FireWizard) as MonoScript;
				}
				if(twoselected.name == "FrostWizard"){
					tempPlayer2 = GlobalHandlerScript.tempPlayer2.AddComponent(FrostWizard) as MonoScript;
				}
				if(twoselected.name == "EarthWizard"){
					tempPlayer2 = GlobalHandlerScript.tempPlayer2.AddComponent(EarthWizard) as MonoScript;
				}
				if(twoselected.name === "ElectricityWizard"){
					tempPlayer2 = GlobalHandlerScript.tempPlayer2.AddComponent(ElectricityWizard) as MonoScript;
				}
			}
			if(threeselected){
				removePreviousScripts(GlobalHandlerScript.tempPlayer3);

				if(threeselected.name == "FireWizard"){
					tempPlayer3 = GlobalHandlerScript.tempPlayer3.AddComponent(FireWizard) as MonoScript;
				}
				if(threeselected.name == "FrostWizard"){
					tempPlayer3 = GlobalHandlerScript.tempPlayer3.AddComponent(FrostWizard) as MonoScript;
				}
				if(threeselected.name == "EarthWizard"){
					tempPlayer3 = GlobalHandlerScript.tempPlayer3.AddComponent(EarthWizard) as MonoScript;
				}
				if(threeselected.name === "ElectricityWizard"){
					tempPlayer3 = GlobalHandlerScript.tempPlayer3.AddComponent(ElectricityWizard) as MonoScript;
				}
			}
			if(fourselected){
				removePreviousScripts(GlobalHandlerScript.tempPlayer3);

				if(fourselected.name == "FirWizard"){
					tempPlayer4 = GlobalHandlerScript.tempPlayer4.AddComponent(FireWizard) as MonoScript;
				}
				if(fourselected.name == "FrostWizard"){
					tempPlayer4 = GlobalHandlerScript.tempPlayer4.AddComponent(FrostWizard) as MonoScript;
				}
				if(fourselected.name == "EarthWizard"){
					tempPlayer4 = GlobalHandlerScript.tempPlayer4.AddComponent(EarthWizard) as MonoScript;
				}
				if(fourselected.name == "ElectricityWizard"){
					tempPlayer4 = GlobalHandlerScript.tempPlayer4.AddComponent(ElectricityWizard) as MonoScript;
				}
			}
			oneselected = null;
			twoselected = null;
			threeselected = null;
			fourselected = null;
			
			Application.LoadLevel("Stage Select");
			
		}
	
	}
	else{
		startToPlay.GetComponent(SpriteRenderer).enabled = false;
	}
	
	//***************************player left**************************//
	
	if ( Input.GetButtonDown("B_1") && GlobalHandlerScript.player1Joined == true ){

		//if player is unlocked, they left
		if( GlobalHandlerScript.player1Locked == false){
			//if player is not locked
			GlobalHandlerScript.left("Player1");
			Destroy(p1);
			Destroy(p1cursor);
			//disable player select script
		}
		//if player is locked, unlock sprite and let them rechoose
		else{
			p1.GetComponent(SpriteRenderer).sprite = null;
			oneselected = null;
			GlobalHandlerScript.player1Locked = false;	
		}
		
	} 
	
	
	if ( Input.GetButtonDown("B_2") && GlobalHandlerScript.player2Joined == true ){
		
		//if player is unlocked, they left
		if( GlobalHandlerScript.player2Locked == false){
			//if player is not locked
			GlobalHandlerScript.left("Player2");
			Destroy(p2);
			Destroy(p2cursor);
			//disable player select script
		}
		//if player is locked, unlock sprite and let them rechoose
		else{
			p2.GetComponent(SpriteRenderer).sprite = null;
			twoselected = null;
			GlobalHandlerScript.player2Locked = false;	
		}
	}
	if ( Input.GetButtonDown("B_3") && GlobalHandlerScript.player3Joined == true ){
		
		//if player is unlocked, they left
		if( GlobalHandlerScript.player3Locked == false){
			//if player is not locked
			GlobalHandlerScript.left("Player3");
			Destroy(p3);
			Destroy(p3cursor);
			//disable player select script
		}
		//if player is locked, unlock sprite and let them rechoose
		else{
			p3.GetComponent(SpriteRenderer).sprite = null;
			threeselected = null;
			GlobalHandlerScript.player3Locked = false;
		}
	}
	if ( Input.GetButtonDown("B_4") && GlobalHandlerScript.player4Joined == true ){
		
		//if player is unlocked, they left
		if( GlobalHandlerScript.player4Locked == false){
			//if player is not locked
			GlobalHandlerScript.left("Player1");
			Destroy(p4);
			Destroy(p4cursor);
			//disable player select script
		}
		//if player is locked, unlock sprite and let them rechoose
		else{
			p4.GetComponent(SpriteRenderer).sprite = null;
			fourselected = null;
			GlobalHandlerScript.player4Locked = false;	
		}
	}
	
	//*********************player select character**************

	if ( Input.GetButtonDown("A_1") && GlobalHandlerScript.player1Joined == true ){
		
		//find the object they selected, make it their sprite
		oneselected = FindCharacter( p1cursor );
		if( oneselected ){
			
			//update the temp character sprite. 
			var p1sprite = oneselected.GetComponent(SpriteRenderer).sprite;
			p1.GetComponent(SpriteRenderer).sprite = p1sprite;
			p1.GetComponent(SpriteRenderer).enabled = true;
			
			//now update the actual player for next scene 
			GlobalHandlerScript.tempPlayer1.GetComponent(SpriteRenderer).sprite = p1sprite;
			GlobalHandlerScript.player1Locked = true;	
		}
	} 
	if ( Input.GetButtonDown("A_2") && GlobalHandlerScript.player2Joined == true ){
	
		//find the object they selected, make it their sprite
		twoselected = FindCharacter( p2cursor );
		if( twoselected ){
		
			var p2sprite = twoselected.GetComponent(SpriteRenderer).sprite;
			p2.GetComponent(SpriteRenderer).sprite = p2sprite;
			p2.GetComponent(SpriteRenderer).enabled = true;
			
			//now update the actual player for next scene 
			GlobalHandlerScript.tempPlayer2.GetComponent(SpriteRenderer).sprite = p2sprite;
			GlobalHandlerScript.player2Locked = true;	
		
		}
	}
	if ( Input.GetButtonDown("A_3") && GlobalHandlerScript.player3Joined == true ){
	
		//find the object they selected, make it their sprite
		threeselected = FindCharacter( p3cursor );
		if( threeselected ){
			//update the temp character sprite. 
			var p3sprite = threeselected.GetComponent(SpriteRenderer).sprite;
			p3.GetComponent(SpriteRenderer).sprite = p3sprite;
			p3.GetComponent(SpriteRenderer).enabled = true;
			
			//now update the actual player for next scene 
			GlobalHandlerScript.tempPlayer3.GetComponent(SpriteRenderer).sprite = p3sprite;
			GlobalHandlerScript.player3Locked = true;		
		}
	}
	if ( Input.GetButtonDown("A_4") && GlobalHandlerScript.player4Joined == true ){
	
		//find the object they selected, make it their sprite
		fourselected = FindCharacter( p4cursor );
		if( fourselected ){
			//update the temp character sprite. 
			var p4sprite = fourselected.GetComponent(SpriteRenderer).sprite;
			p4.GetComponent(SpriteRenderer).sprite = p4sprite;
			p4.GetComponent(SpriteRenderer).enabled = true;
			
			//now update the actual player for next scene 
			GlobalHandlerScript.tempPlayer4.GetComponent(SpriteRenderer).sprite = p4sprite;
			GlobalHandlerScript.player4Locked = true;
		}
	}
	
	
	
}

function FindCharacter( cursor ) {
	gos = GameObject.FindGameObjectsWithTag("Character");
    distance = 6;
    for ( var go in gos ) {

    	var diff = go.transform.position - cursor.transform.position;
    	var curDistance = diff.sqrMagnitude;
    	if (curDistance < distance) {
    	
    		closestCharacter = go;
    		distance = curDistance;
    	}
 	}
 	if(closestCharacter == oneselected ||
 	   closestCharacter == twoselected ||
 	   closestCharacter == threeselected ||
 	   closestCharacter == fourselected ){
 	   return null;
 	}
 	else{
    	return closestCharacter;
    }
}

function removePreviousScripts(player){
	if (player.GetComponent(FireWizard)){
			DestroyImmediate(player.GetComponent(FireWizard), true);
	}
	if (player.GetComponent(ElectricityWizard)){
			DestroyImmediate(player.GetComponent(ElectricityWizard), true);
	}
	if (player.GetComponent(EarthWizard)){
			DestroyImmediate(player.GetComponent(EarthWizard), true);
	}
	if (player.GetComponent(FrostWizard)){
			DestroyImmediate(player.GetComponent(FrostWizard), true);
	}
}