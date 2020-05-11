

var GlobalHandler : GameObject;
var GlobalHandlerScript : MonoBehaviour;
var three : GameObject;
var two : GameObject;
var one : GameObject;
var go : GameObject;
var startTimer;

var oneRef;
var twoRef;
var threeRef;
var goRef;
var p1;
var p2;
var p3;
var p4;

function Awake(){
  	GlobalHandler = GameObject.Find("GlobalHandler");
  	GlobalHandlerScript = GlobalHandler.GetComponent("GlobalHandler");
  	GlobalHandlerScript.playersAlive = 0;
  	GlobalHandler.transform.Find("menuMusic").GetComponent.<AudioSource>().Stop();
  	//start countdown variables
  	startCountDown();

  	if ( GlobalHandlerScript.player1Joined == true ){

		//instantiate player
		p1 = Instantiate(GlobalHandlerScript.tempPlayer1);	
		p1.transform.localScale.x = .15;
		p1.transform.localScale.y = .15;
		GlobalHandlerScript.player1Alive = true;
		GlobalHandlerScript.playersAlive += 1;
	}
	if ( GlobalHandlerScript.player2Joined == true ){
  
		//instantiate player
		p2 = Instantiate(GlobalHandlerScript.tempPlayer2);
		p2.transform.localScale.x = .15;
		p2.transform.localScale.y = .15;
		p2.transform.position.x = 7;
		p2.transform.position.y = 4;
		GlobalHandlerScript.player2Alive = true;
		GlobalHandlerScript.playersAlive += 1;
	}
	if ( GlobalHandlerScript.player3Joined == true ){
  
		//instantiate player
		p3 = Instantiate(GlobalHandlerScript.tempPlayer3);
		p3.transform.localScale.x = .15;
		p3.transform.localScale.y = .15;
		p3.transform.position.x = -7;
		p3.transform.position.y = -4;
		GlobalHandlerScript.player3Alive = true;
		GlobalHandlerScript.playersAlive += 1;
	}
	if ( GlobalHandlerScript.player4Joined == true ){
  
		//instantiate player
		p4 = Instantiate(GlobalHandlerScript.tempPlayer4);
		p4.transform.localScale.x = .15;
		p4.transform.localScale.y = .15;
		p4.transform.position.x = 7;
		p4.transform.position.y = -4;
		GlobalHandlerScript.player4Alive = true;
		GlobalHandlerScript.playersAlive += 1;
	}
	
	//*********************************TEST ENEMY**************************
		
		//instantiate test player
	if (GlobalHandlerScript.playersAlive == 1 ){
		p5 = Instantiate(GlobalHandlerScript.AI);
		p5.transform.position.x = 7;
		p5.transform.position.y = -4;
		p5.transform.localScale.x = .3;
		p5.transform.localScale.y = .3;
		GlobalHandlerScript.AIAlive = true;
		GlobalHandlerScript.playersAlive += 1;
		p5.GetComponent(PlayerStats).GUIEnabled = true;
	}
}

function startCountDown(){
	
	threeRef = Instantiate(three);
	yield WaitForSeconds(1);
	Destroy(threeRef);
	twoRef = Instantiate(two);
	yield WaitForSeconds(1);
	Destroy(twoRef);
	oneRef = Instantiate(one);
	yield WaitForSeconds(1);
	Destroy(oneRef);
	//fightRef = Instantiate(fight);
	gameStart();
	yield WaitForSeconds(1);
	//Destroy(fightRef);
}
	
function gameStart(){
	GlobalHandlerScript.SendMessage("battleStart");
  
}


