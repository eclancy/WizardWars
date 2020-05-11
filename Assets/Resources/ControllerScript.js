
var baseSpeed: float = 4.0;
var activeSpeed : float =  4.0;
var speed;
var targeterLoad : GameObject;
var targeter;
var L_XAxis;
var L_YAxis;
var R_XAxis;
var R_YAxis;
var Triggers;
var RB;
var LB;
var characterScript;
var cantmove = false;//used for sliding

var targeterRange = 6;
var GlobalHandler;
var GlobalHandlerScript;
var xAxis;
var yAxis;

var slowPercentage = 0;
var speedPercentage = 0;


function Start(){
	xAxis = 0;
	yAxis = 0;
	GlobalHandler = GameObject.Find("GlobalHandler");
  	GlobalHandlerScript = GlobalHandler.GetComponent("GlobalHandler");
}

function setBaseSpeed(percent){
	baseSpeed = baseSpeed * percent / 100;
	activeSpeed = baseSpeed;
}


function slow( percent ){
	slowPercentage += percent;
}
function removeSlow( percent ){
	slowPercentage -= percent;
}

function speedUp( percent ){
	speedPercentage += percent;
}
function removeSpeed( percent ){
	speedPercentage -= percent;

}


function sliding(){
	if(speed != 0){
		cantmove = true;
	}
}
function notSliding(){
	cantmove = false;
}


function LateUpdate() {
	
	if(slowPercentage < 0){
		slowPercentage = 0;
	}
	if(speedPercentage < 0){
		speedPercentage = 0;
	}
	if( GlobalHandlerScript.getBattling() == true){
		if(LB == null ){
			if(transform.name == "Player1(Clone)"){
				L_XAxis = "L_XAxis_1";
				L_YAxis = "L_YAxis_1";
				R_XAxis = "R_XAxis_1";
				R_YAxis = "R_YAxis_1";
				Triggers = "Triggers_1";
				RB = "RB_1";
				LB = "LB_1";
			}
	
			if(transform.name == "Player2(Clone)"){
				L_XAxis = "L_XAxis_2";
				L_YAxis = "L_YAxis_2";
				R_XAxis = "R_XAxis_2";
				R_YAxis = "R_YAxis_2";
				Triggers = "Triggers_2";
				RB = "RB_2";
				LB = "LB_2";
			}
	
			if(transform.name == "Player3(Clone)"){
				L_XAxis = "L_XAxis_3";
				L_YAxis = "L_YAxis_3";
				R_XAxis = "R_XAxis_3";
				R_YAxis = "R_YAxis_3";
				Triggers = "Triggers_3";
				RB = "RB_3";
				LB = "LB_3";
			}
	
			if(transform.name == "Player4(Clone)"){
				L_XAxis = "L_XAxis_4";
				L_YAxis = "L_YAxis_4";
				R_XAxis = "R_XAxis_4";
				R_YAxis = "R_YAxis_4";
				Triggers = "Triggers_4";
				RB = "RB_4";
				LB = "LB_4";
			}
			
			var characterSprites : Sprite[];
			var spriteSheet;
			
			//to get targeter and sprites based on character
			if(transform.root.GetComponent(SpriteRenderer).sprite.name == "Fire Wizard"){
				targeterLoad = Resources.Load("RedTargeter");
				targeter = Instantiate(targeterLoad, transform.position, transform.rotation);
				
 				//characterSprites = AssetDatabase.LoadAll( "Assets/Textures/RedWizardSpriteSheet", Sprite ).ToArray();
			
			
			/*
				spritesheet = Resources.Load("RedWizardSpritesheet");
				characterSprites[0] = spritesheet.spri
				*/
				
			}
			else if(transform.root.GetComponent(SpriteRenderer).sprite.name == "Frost Wizard"){
				targeterLoad = Resources.Load("BlueTargeter");
				targeter = Instantiate(targeterLoad, transform.position, transform.rotation);
				//characterSprites = Resources.LoadAll("BlueWizardSpritesheet");
			}
			else if(transform.root.GetComponent(SpriteRenderer).sprite.name == "Electricity Wizard"){
				targeterLoad = Resources.Load("YellowTargeter");
				targeter = Instantiate(targeterLoad, transform.position, transform.rotation);
				//characterSprites = Resources.LoadAll("PurpleWizardSpritesheet");
			}
			else if(transform.root.GetComponent(SpriteRenderer).sprite.name == "Earth Wizard"){
				targeterLoad = Resources.Load("GreenTargeter");
				targeter = Instantiate(targeterLoad, transform.position, transform.rotation);
				//characterSprites = Resources.LoadAll("BrownWizardSpritesheet");
			}
			
			
		}
	
	 /*************************************MOVE PLAYER*****************************************/
	 	else {
	 	
	 		activeSpeed = baseSpeed - (baseSpeed*slowPercentage/100) + (baseSpeed*speedPercentage/100);
	 		
	 		if(cantmove == false){//used for sliding
	 			// Determine the angle of the left thumbstick
	 			xAxis = Input.GetAxis(L_XAxis);
	 			yAxis = Input.GetAxis(L_YAxis);
	 		}
	 		//reset rotation so rigidbodies don't get flipped
	 		transform.rotation = Quaternion.identity;
	 			
	 		//find the target to move towards
	 		var xtarget = transform.position.x + xAxis;
	 		var ytarget = transform.position.y + yAxis;
	 		var movetarget = Vector3(xtarget, ytarget, 0);
	 		/*
	 		if(yAxis > 0){
	 			transform.root.GetComponent(SpriteRenderer).sprite = characterSprites[0];
	 		}
	 		else{
	 			transform.root.GetComponent(SpriteRenderer).sprite = characterSprites[1];
	 		}*/
	 		// The step size is equal to speed times frame time.
	 		//This finds how hard the player is pushing the joystick, and changes speed
	 		speed = Vector2(xAxis, yAxis).magnitude * activeSpeed;
	 		var step = speed * Time.deltaTime;
	 		
	 		// Move our position a step closer to the target.
	 		transform.position = Vector3.MoveTowards(transform.position, movetarget, step);
	 		
	 		/****END MOVE PLAYER****/
	 		
	 		
	 		/*****************************************PLAYER AIM AND ATTACKS***********************************************/
 			
 			var xAim = Input.GetAxis(R_XAxis);
 			var yAim = Input.GetAxis(R_YAxis);
 			// Calculate the angle of the thumbstick
 			var targetAngle : float = Mathf.Atan2(-yAim, xAim) * Mathf.Rad2Deg;
 			var targetVector = Vector3(0, 0, targetAngle);
 			
 			//move the targeter
 			targeter.transform.position = transform.position;
 			targeter.transform.position.x += xAim*targeterRange;
 			targeter.transform.position.y += -yAim*targeterRange;
 			
 			var targetPoint = targeter.transform.position;
 			
 			//call the appropriate attack - Each attack either accepts nothing, a vector, or a point.
 			//pass an array of the args and the attacks should accept that, using what they need
 			
 			var args = new Array ();
 			args.Push (targetVector);
 			args.Push (targetPoint);
 			
 			
 			//basic attack
 			if( Input.GetAxis(Triggers) > .5 ){
 			    gameObject.SendMessage("basicAttack", args);
 			}
 			
 			//second
 			 if( Input.GetAxis(Triggers) < -.5 ){
 			 	gameObject.SendMessage("secondAttack", args);
 			}
 			
 			//third
 			if( Input.GetButtonDown(RB) ){
 				args.Push(RB);
 				args.Push(R_XAxis);
 				args.Push(R_YAxis);
 			    gameObject.SendMessage("thirdAttack", args);
 			}
 			
 			//ultimate
 			if( Input.GetButtonDown(LB) ){
 			    gameObject.SendMessage("ultimateAttack", args);
 			}
		}
		//reset the crowd control checks
		activeSpeed = baseSpeed;
		//cantmove = false;
	}
}				




