#pragma strict
import UnityEngine.SceneManagement;

function Awake () {
	DontDestroyOnLoad (this);
}

var playersAlive = 0;
var playersJoined = 0;

var basePlayer1 : GameObject;
var basePlayer2 : GameObject;
var basePlayer3 : GameObject;
var basePlayer4 : GameObject;

var tempPlayer1 : GameObject;
var tempPlayer2 : GameObject;
var tempPlayer3 : GameObject;
var tempPlayer4 : GameObject;

var player1Joined = false;
var player2Joined = false;
var player3Joined = false;
var player4Joined = false;

var player1Locked = false;
var player2Locked = false;
var player3Locked = false;
var player4Locked = false;

var player1Alive = false;
var player2Alive = false;
var player3Alive = false;
var player4Alive = false;
var AIAlive  = false;


var p1Wins : GameObject;
var p2Wins : GameObject;
var p3Wins : GameObject;
var p4Wins : GameObject;

var AI : GameObject;
var battling = false;

function Start(){


}

function OnEnable(){
	//Tell our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
	SceneManager.sceneLoaded += OnSceneLoaded;
}

function OnDisable() {
      SceneManager.sceneLoaded -= OnSceneLoaded;
  }

function Update () {

	
}

function battleStart(){
	battling = true;
}
function getBattling(){
	return battling;
}

function kill(character){
	playersAlive--;
	var winner : GameObject;	
	
	if( character == "Player1(Clone)" ){
		player1Alive = false;
	}
	if( character == "Player2(Clone)" ){
		player2Alive = false;
	}
	if( character == "Player3(Clone)" ){
		player3Alive = false;
	}
	if( character == "Player4(Clone)" ){
		player4Alive = false;
	}
	if( character == "AI(Clone)" ){
		AIAlive = false;
	}

	
	//game win
	if (playersAlive == 1){
			
		
		if( player1Alive == true ){
			winner = Instantiate(p1Wins);
		}
		if( player2Alive == true ){
			winner = Instantiate(p2Wins);
		}
		if( player3Alive == true ){
			winner = Instantiate(p3Wins);
		}
		if( player4Alive == true ){
			winner = Instantiate(p4Wins);
		}
		
		yield WaitForSeconds(5);
		Destroy( winner );
		Application.LoadLevel("Character Select");
		transform.Find("menuMusic").GetComponent.<AudioSource>().Play();
		battling = false;
	}
}

function joined( player ){
	if( player == "Player1" ){
		player1Joined = true;
		playersJoined++;
	}
	if( player == "Player2" ){
		player2Joined = true;
		playersJoined++;
	}
	if( player == "Player3" ){
		player3Joined = true;
		playersJoined++;
	}
	if( player == "Player4" ){
		player4Joined = true;
		playersJoined++;
	}
	
}

function left( player ){
	
	if( player == "Player1" ){
		player1Joined = false;
		player1Locked = false;
		player1Alive = false;
		basePlayer1.GetComponent(SpriteRenderer).sprite = null;
		tempPlayer1 = basePlayer1;
		removeScripts(tempPlayer1);
		removeScripts(basePlayer1);
		
	}
	if( player == "Player2" ){
		player2Joined = false;
		player2Locked = false;
		player2Alive = false;
		basePlayer2.GetComponent(SpriteRenderer).sprite = null;
		tempPlayer2 = basePlayer2;
		removeScripts(tempPlayer2);
		removeScripts(basePlayer2);
	}
	if( player == "Player3" ){
		player3Joined = false;
		player3Locked = false;
		player3Alive = false;
		basePlayer3.GetComponent(SpriteRenderer).sprite = null;
		tempPlayer3 = basePlayer3;
		removeScripts(tempPlayer3);
		removeScripts(basePlayer3);
	}
	if( player == "Player4" ){
		player4Joined = false;
		player4Locked = false;
		player4Alive = false;
		basePlayer4.GetComponent(SpriteRenderer).sprite = null;
		tempPlayer4 = basePlayer4;
		removeScripts(tempPlayer4);
		removeScripts(basePlayer4);
	}


}

//reset
function OnSceneLoaded(scene: Scene, mode: LoadSceneMode) {
	if (scene == 1) {
	
		left("Player1");
		left("Player2");
		left("Player3");
		left("Player4");
	
			
	}
}

function removeScripts( player : GameObject ){
	var scripts : Component[];
	scripts = player.GetComponents(MonoBehaviour);
	
	for (var currentScript : MonoBehaviour in scripts) {
		DestroyImmediate(currentScript, true);	
	}
}








