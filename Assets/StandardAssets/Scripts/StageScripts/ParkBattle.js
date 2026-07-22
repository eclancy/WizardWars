

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
var p5;

var p1Spawn : Vector3;
var p2Spawn : Vector3;
var p3Spawn : Vector3;
var p4Spawn : Vector3;
var aiSpawn : Vector3;

var rockWallHorizontal;
var rockWallVertical;

function Awake(){
  	GlobalHandler = GameObject.Find("GlobalHandler");
  	GlobalHandlerScript = GlobalHandler.GetComponent("GlobalHandler");
  	GlobalHandlerScript.playersAlive = 0;
  	GlobalHandler.transform.Find("menuMusic").GetComponent.<AudioSource>().Stop();
	rockWallHorizontal = Resources.Load("RockWallHorizontal");
	rockWallVertical = Resources.Load("RockWallVertical");
	configureStageVariant();
  	//start countdown variables
  	startCountDown();

  	if ( GlobalHandlerScript.player1Joined == true ){

		//instantiate player
		p1 = Instantiate(GlobalHandlerScript.tempPlayer1);	
		p1.transform.localScale.x = .15;
		p1.transform.localScale.y = .15;
		p1.transform.position = p1Spawn;
		GlobalHandlerScript.player1Alive = true;
		GlobalHandlerScript.playersAlive += 1;
	}
	if ( GlobalHandlerScript.player2Joined == true ){
  
		//instantiate player
		p2 = Instantiate(GlobalHandlerScript.tempPlayer2);
		p2.transform.localScale.x = .15;
		p2.transform.localScale.y = .15;
		p2.transform.position = p2Spawn;
		GlobalHandlerScript.player2Alive = true;
		GlobalHandlerScript.playersAlive += 1;
	}
	if ( GlobalHandlerScript.player3Joined == true ){
  
		//instantiate player
		p3 = Instantiate(GlobalHandlerScript.tempPlayer3);
		p3.transform.localScale.x = .15;
		p3.transform.localScale.y = .15;
		p3.transform.position = p3Spawn;
		GlobalHandlerScript.player3Alive = true;
		GlobalHandlerScript.playersAlive += 1;
	}
	if ( GlobalHandlerScript.player4Joined == true ){
  
		//instantiate player
		p4 = Instantiate(GlobalHandlerScript.tempPlayer4);
		p4.transform.localScale.x = .15;
		p4.transform.localScale.y = .15;
		p4.transform.position = p4Spawn;
		GlobalHandlerScript.player4Alive = true;
		GlobalHandlerScript.playersAlive += 1;
	}
	
	//*********************************TEST ENEMY**************************
		
		//instantiate test player
	if (GlobalHandlerScript.playersAlive == 1 ){
		p5 = Instantiate(GlobalHandlerScript.AI);
		p5.transform.position = aiSpawn;
		p5.transform.localScale.x = .3;
		p5.transform.localScale.y = .3;
		GlobalHandlerScript.AIAlive = true;
		GlobalHandlerScript.playersAlive += 1;
		p5.GetComponent(PlayerStats).GUIEnabled = true;
	}
}

function configureStageVariant(){
	// Default open arena layout.
	p1Spawn = Vector3(-7, 4, 0);
	p2Spawn = Vector3(7, 4, 0);
	p3Spawn = Vector3(-7, -4, 0);
	p4Spawn = Vector3(7, -4, 0);
	aiSpawn = Vector3(7, -4, 0);

	var stageVariant = GlobalHandlerScript.getSelectedStage();

	if(stageVariant == 2){
		p1Spawn = Vector3(-8, 0, 0);
		p2Spawn = Vector3(8, 0, 0);
		p3Spawn = Vector3(0, 5, 0);
		p4Spawn = Vector3(0, -5, 0);
		aiSpawn = Vector3(8, 0, 0);
		spawnWall(rockWallVertical, Vector3(-2, 0, 0), Quaternion.identity);
		spawnWall(rockWallVertical, Vector3(2, 0, 0), Quaternion.identity);
	}

	if(stageVariant == 3){
		p1Spawn = Vector3(-7, 0, 0);
		p2Spawn = Vector3(7, 0, 0);
		p3Spawn = Vector3(0, 5, 0);
		p4Spawn = Vector3(0, -5, 0);
		aiSpawn = Vector3(7, 0, 0);
		spawnWall(rockWallHorizontal, Vector3(0, 2, 0), Quaternion.identity);
		spawnWall(rockWallHorizontal, Vector3(0, -2, 0), Quaternion.identity);
		spawnWall(rockWallVertical, Vector3(0, 0, 0), Quaternion.identity);
	}

	if(stageVariant == 4){
		p1Spawn = Vector3(-8, 4.5, 0);
		p2Spawn = Vector3(8, 4.5, 0);
		p3Spawn = Vector3(-8, -4.5, 0);
		p4Spawn = Vector3(8, -4.5, 0);
		aiSpawn = Vector3(8, -4.5, 0);
		spawnWall(rockWallHorizontal, Vector3(-3.5, 0, 0), Quaternion.identity);
		spawnWall(rockWallHorizontal, Vector3(3.5, 0, 0), Quaternion.identity);
		spawnWall(rockWallVertical, Vector3(0, 2.5, 0), Quaternion.identity);
		spawnWall(rockWallVertical, Vector3(0, -2.5, 0), Quaternion.identity);
	}
}

function spawnWall(prefab, position : Vector3, rotation : Quaternion){
	if(prefab != null){
		Instantiate(prefab, position, rotation);
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


