var allow1 = true;
var allow2 = true;
var allow3 = true;
var allow4 = true;
var attack1;
var attack2;
var attack3;
var attack4;
var statScript;
var running = false;
var controllerScript;

function Start(){
	attack1 = Resources.Load("electricityball");
	attack2 = Resources.Load("lightningRod");
	attack3 = Resources.Load("speedUpParticles");
	statScript = GetComponent("PlayerStats");
 	controllerScript = GetComponent("ControllerScript");

}

function getName(){
	return "ElectricityWizard";
}

function Update(){


}

function basicAttack( args ){

	if(allow1){
		var targetVector = args[0];
		if( statScript.DepleteEnergy(33) ){
			allow1 = false;
 			var shot = attack1.Instantiate( attack1, transform.position, Quaternion.identity );
    		//make sure player can't hit himself with it
    		Physics2D.IgnoreCollision( shot.collider2D, GetComponent.<Collider2D>() );
    		//turn to face where it's heading
    		shot.transform.Rotate( targetVector );
    		//Destroy automatically after time
    		Destroy(shot.gameObject, 2);
    		yield WaitForSeconds(.3);
    		allow1 = true;
		}
    }
}

function secondAttack( args ){
	if(allow2){
		if( statScript.DepleteEnergy(30) ){
			allow2 = false;
 			var shot = Instantiate( attack2, args[1], Quaternion.identity );
    		//Destroy automatically after time
    		Destroy(shot.gameObject, 3.5);
    		yield WaitForSeconds(1);
    		allow2 = true;
		}
    }

}

function thirdAttack( args ){
	if(allow3)
		if( statScript.DepleteEnergy(30) ){
			allow3 = false;
			controllerScript.SendMessage("speedUp", 50);
 			var shot = Instantiate( attack3, transform.position, Quaternion.identity );
 			shot.transform.parent = transform;
    		Destroy(shot.gameObject, 2.5);
    		yield WaitForSeconds(2.5);
    		controllerScript.SendMessage("removeSpeed", 50);
    		allow3 = true;
    	}
}

function ultimateAttack( args ){

}
