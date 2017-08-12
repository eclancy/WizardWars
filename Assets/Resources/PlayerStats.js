

var health = 100f;
var energy = 100f;
var recoveryRate  = 20;
var GlobalHandler : GameObject;
var GlobalHandlerScript : MonoBehaviour;
var controllerScript : MonoBehaviour;
var healthBarEmpty : Texture2D;
var healthBarFull : Texture2D;
var energyBarFull : Texture2D;
var rechargeEnabled = true;
var fireSprite;
var onFireObject;

private var damageDelay = .5;
private var lastDamage = -10.0;

var stunned = false;
var onFire = false;

var burnDuration = 0;
var burnDamage = 0;

var stunDuration = 0f;


function Awake(){
 	GlobalHandler = GameObject.Find("GlobalHandler");
  	GlobalHandlerScript = GlobalHandler.GetComponent("GlobalHandler");
 	controllerScript = gameObject.GetComponent("ControllerScript");
 	healthBarEmpty = Resources.Load("BarEmpty");
  	healthBarFull = Resources.Load("HealthBarFull");
 	energyBarFull = Resources.Load("EnergyBarFull");
	fireSprite = Resources.Load("OnFire");
}
 
 function Update(){
 
 	//burning damage
 	if(burnDuration > 0){
 		if(Time.time > damageDelay+lastDamage){ //if a half second has passed
        	ApplyDamage(burnDamage);
        	lastDamage = Time.time;
        	burnDuration-=.5;
    	}
 	}else if(onFire == true){
 		if(onFireObject != null){
 			Destroy(onFireObject.gameObject);
 		}
 		onFire = false;
 		burnDuration = 0;
 	}
 	
 	//character stun
 	if(stunDuration > 0){
        stunDuration -= Time.deltaTime;
 	}else if(stunned == true){
 		var iceCubeSprite = transform.Find("iceCube(Clone)");
 		if(iceCubeSprite != null){
 			Destroy(iceCubeSprite.gameObject);
 		}
 		controllerScript.enabled = true;
 		stunDuration = 0;
 		stunned = false;
 	}
    	
	if(energy<100 && rechargeEnabled == true){
		recovered = recoveryRate * Time.deltaTime;
		energy += recovered;
		
		if(energy>100){
			energy = 100;
		}
	}
 }
 
 
function ApplyDamage (damage : int) {
     health -= damage;
 
     if(health <= 0) {
     	GlobalHandlerScript.kill(name);
        Die();
     }
 }
 
 //*******************************UTILITIES*********************************
 
 function DepleteEnergy( energyUse : float) {
 
     if(energy >= energyUse) {
		energy -= energyUse;
        return true;
     }
     else{
     	return false;
     }
 }
 
 function stun( stunTime : float){
 	stunDuration = stunTime;
 	stunned = true;
 	controllerScript.enabled = false;
 	var fireSprite = transform.Find("OnFire(Clone)");
 	if(fireSprite != null){
 		Destroy(fireSprite.gameObject);
 	}
 }
 
 
 //set damage for burn
 function setFire( args ){
 	Destroy(onFireObject);
 	onFireObject = Instantiate(fireSprite, transform.position, Quaternion.identity);
	onFireObject.transform.parent = gameObject.transform.root;
	onFireObject.transform.position.y+=0;
	onFireObject.transform.position.x-=0;
				
 	burnDuration = args[0];
 	burnDamage = args[1];
 	onFire = true;
 }
 
 
 function Die(){
   Destroy(controllerScript.targeter);
   Destroy(gameObject);
 }

 
function OnGUI(){
	
	var size : Vector2 = new Vector2(60, 20);
	
	var screenPos : Vector3 = Camera.main.WorldToScreenPoint (transform.position);
 	screenPos.y = Screen.height - screenPos.y - 60;

 	screenPos.x -= size.x/2 - 3;
 	 	
 	if(gameObject.transform.root.name == "AI(Clone)" ){
 		screenPos.y -= Screen.height/50;
 		screenPos.x -= 2;
 	}
 	
    // draw the background:
    GUI.BeginGroup (new Rect (screenPos.x, screenPos.y, size.x, size.y));
    
    	//draw the empty part
        GUI.Box (Rect (0, 0, size.x, size.y/2), healthBarEmpty);
        // draw the filled-in part:
        GUI.BeginGroup (new Rect (0, 0, (size.x * health) / 100, size.y));
            GUI.Box (Rect (0,0, size.x, size.y), healthBarFull);
        GUI.EndGroup ();
        
        //draw the empty part
        GUI.Box (Rect (0,8, size.x, size.y/2), healthBarEmpty);
        //draw the filled in part
        GUI.BeginGroup (new Rect (0, 0, (size.x * energy) / 100, size.y));
            GUI.Box (Rect (0, 8, size.x, size.y), energyBarFull);
        GUI.EndGroup ();
 
    GUI.EndGroup ();
 
} 

function getOnFire(){
	return onFire;
}

function getStunned(){
	return stunned;
}

function getEnergy(){
	return energy;
}

function putOutFire(){
	onFire = false;
	burnDuration = 0;
}

function noRecharge(){
	rechargeEnabled = false;
}

function recharge(){
	rechargeEnabled = true;
}
