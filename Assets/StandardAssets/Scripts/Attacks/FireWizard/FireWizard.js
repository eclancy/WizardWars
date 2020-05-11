var allow1 = true;
var allow2 = true;
var allow3 = true;
var allowFlame = true;
var allow4 = true;
var attack1;
var attack2;
var attack3;
var attack4;
var statScript;
var flameThrower = false;
var flameThrowerArgs;
var flameShot;
var rechargeFlame = false;
var lastTick = 0;
var controllerScript;

function Start(){
	attack1 = Resources.Load("fireball");
	attack2 = Resources.Load("ringOfFire");
	attack3 = Resources.Load("flameThrower");
	statScript = GetComponent("PlayerStats");
	controllerScript = GetComponent("ControllerScript");


}

function getName(){
	return "FireWizard";
}

function basicAttack( args ){

	if(allow1){
		var targetVector = args[0];
		if( statScript.DepleteEnergy(33) ){
			allow1 = false;
 			var shot = Instantiate( attack1, transform.position, Quaternion.identity );
    		//make sure player can't hit himself with it
    		Physics2D.IgnoreCollision( shot.GetComponent(Collider2D), GetComponent(Collider2D) );
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
		if( statScript.DepleteEnergy(50) ){
			allow2 = false;
 			var shot = Instantiate( attack2, args[1], Quaternion.identity );
    		//Destroy automatically after time
    		Destroy(shot.gameObject, 3);
    		yield WaitForSeconds(4);
    		allow2 = true;
		}
    }
}

function thirdAttack( args ){
	if(allow3 && rechargeFlame == false){
		controllerScript.SendMessage("slow", 25);
		flameThrower = true;
		flameThrowerArgs = args;
		flameShot = Instantiate( attack3, transform.position, Quaternion.identity );
		Physics2D.IgnoreCollision( flameShot.GetComponent(Collider2D), GetComponent(Collider2D) );
		var xAim = Input.GetAxis(flameThrowerArgs[3]);
	 	var yAim = Input.GetAxis(flameThrowerArgs[4]);
	 	// Calculate the angle of the thumbstick
	 	var targetAngle : float = Mathf.Atan2(-yAim, xAim) * Mathf.Rad2Deg;
	 	var targetVector = Vector3(0, 0, targetAngle);
	 	
	 	//move the targeter
	 	flameShot.transform.position = transform.position;
	 	flameShot.transform.rotation = Quaternion.identity;
	 	flameShot.transform.Rotate( targetVector );
	 	flameShot.transform.Translate( Vector2( 2.5, 0) );
	 	statScript.DepleteEnergy(15);
		allow3 = false;
		
	}
}

function ultimateAttack( args ){

}

function Update(){
	
	if( rechargeFlame == true && statScript.getEnergy() > 50){
		rechargeFlame = false;
	}

 	
 	else if(flameThrower == true && flameShot != null ){
		if(!Input.GetButtonUp(flameThrowerArgs[2]) && allowFlame ){
		 	var xAim = Input.GetAxis(flameThrowerArgs[3]);
	 		var yAim = Input.GetAxis(flameThrowerArgs[4]);
	 		// Calculate the angle of the thumbstick
	 		var targetAngle : float = Mathf.Atan2(-yAim, xAim) * Mathf.Rad2Deg;
	 		var targetVector = Vector3(0, 0, targetAngle);
	 		
	 		//move the targeter
	 		flameShot.transform.position = transform.position;
	 		flameShot.transform.rotation = Quaternion.identity;
	 		flameShot.transform.Rotate( targetVector );
	 		flameShot.transform.Translate( Vector2( 2.5, 0) );
	
			statScript.SendMessage("noRecharge");
			if(  Time.time > .2 + lastTick ){
				if (statScript.DepleteEnergy(.5) ){
	    			lastTick = Time.time;
	    		}
	    		//player ran out of energy
	    		else{
	    			allowFlame = false;
	    			rechargeFlame = true;
	    			statScript.SendMessage("recharge");
	    			controllerScript.SendMessage("removeSlow", 25);
	    		}
	    		
			}
		}
		//if the player stopped using flamethrower
		else {
			Destroy(flameShot.gameObject);
			flamethrowerDelay();
			flameThrower = false;
			statScript.SendMessage("recharge");
			controllerScript.SendMessage("removeSlow", 25);
		}
	}
	//flamethrower got destroyed by another ability
	else if(flameThrower == true && flameShot == null){
 		allowFlame = false;
	   	rechargeFlame = true;
	    statScript.SendMessage("recharge");
	   	controllerScript.SendMessage("removeSlow", 25);
 	} 	
 
	
}

function flamethrowerDelay(){
	yield WaitForSeconds(2);
	allowFlame = true;
	allow3 = true;
}	
			
			
			
			