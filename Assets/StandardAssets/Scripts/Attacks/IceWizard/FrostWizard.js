var allow1 = true;
var allow2 = true;
var allow3 = true;
var allow4 = true;
var attack1;
var attack2;
var attack3;
var attack4;
var statScript;

function getName(){
	return "FrostWizard";
}

function Start(){
	attack1 = Resources.Load("icicle");
	attack2 = Resources.Load("icePatch");
	attack3 = Resources.Load("iceWave");
	statScript = GetComponent("PlayerStats");

}

function basicAttack( args ){

	if(allow1){
		var targetVector = args[0];
		if( statScript.DepleteEnergy(33) ){
			allow1 = false;
 			var shot = Instantiate( attack1, transform.position, Quaternion.identity );
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
		if( statScript.DepleteEnergy(50) ){
			allow2 = false;
 			var shot = Instantiate( attack2, args[1], Quaternion.identity );
    		//Destroy automatically after time
    		Destroy(shot.gameObject, 3);
    		yield WaitForSeconds(3);
    		var players = GameObject.FindGameObjectsWithTag("Player");
    		for(var i = 0; i < players.Length; i++){
    			players[i].SendMessage("notSliding");
    		}
    		yield WaitForSeconds(1);
    		
    		allow2 = true;
		}
    }

}

function thirdAttack( args ){
	if(allow3){
		var targetVector = args[0];
		if( statScript.DepleteEnergy(66) ){
			allow3 = false;
 			var shot = Instantiate( attack3, transform.position, Quaternion.identity );
    		//make sure player can't hit himself with it
    		Physics2D.IgnoreCollision( shot.collider2D, GetComponent.<Collider2D>() );
    		//turn to face where it's heading
    		shot.transform.Rotate( targetVector );
    		//Destroy automatically after time
    		Destroy(shot.gameObject, .5);
    		yield WaitForSeconds(4);
    		allow3 = true;
		}
    }

}

function ultimateAttack( args ){

}
