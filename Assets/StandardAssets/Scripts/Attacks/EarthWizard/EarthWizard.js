var allow1 = true;
var allow2 = true;
var allow3 = true;
var allow4 = true;
var attack1;
var attack2;
var attack3Vertical;
var attack3Horizontal;
var attack4;
var statScript;



function getName(){
	return "EarthWizard";
}

function Start(){
	attack1 = Resources.Load("mudball");
	attack2 = Resources.Load("mudPatch");
	attack3Vertical = Resources.Load("RockWallVertical");
	attack3Horizontal = Resources.Load("RockWallHorizontal");
	statScript = GetComponent("PlayerStats");

}

function basicAttack( args ){

	if(allow1){
		var targetVector = args[0];
		if( statScript.DepleteEnergy(33) ){
			allow1 = false;
 			var shot = attack1.Instantiate( attack1, transform.position, Quaternion.identity );
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
    		yield WaitForSeconds(4);
    		allow2 = true;
		}
    }	

}

function thirdAttack( args ){
	if(allow3){
	var shot;
		if( statScript.DepleteEnergy(25) ){
			allow3 = false;
			
			if( Mathf.Abs( Mathf.Abs(args[1].y) - Mathf.Abs( transform.position.y) ) > 
				Mathf.Abs( Mathf.Abs(args[1].x) - Mathf.Abs( transform.position.x) ) ){
 				shot = Instantiate( attack3Horizontal, args[1], Quaternion.identity );
 				
 			}else{
 				shot = Instantiate( attack3Vertical, args[1], Quaternion.identity );
				shot.transform.Rotate(0,0,270);
 			} 			
 			
    		//Destroy automatically after time
    		Destroy(shot.gameObject, 3);
    		yield WaitForSeconds(4);
    		allow3 = true;
		}
    }
	

}

function ultimateAttack( args ){

}


