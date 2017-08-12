#pragma strict

var fireSprite;
function Start () {
	fireSprite = Resources.Load("OnFire");
	
	transform.Find("Particles").GetComponent.<ParticleSystem>().GetComponent.<Renderer>().sortingOrder = 3;

}

function Update () {

}
var burnDuration = 2.0;
var damagePerTick = 1.0;
var lastDamage = 0; 
function OnTriggerStay2D(coll : Collider2D){
	
	if (coll.gameObject.transform.root.tag == "Player"){
		var script = coll.transform.root.GetComponent("PlayerStats");
		if(Time.time > 1 + lastDamage){ //if a half second has passed
			coll.gameObject.SendMessage("ApplyDamage", 3);
    		lastDamage = Time.time;
    	
			var args = new Array ();
			args.Push (burnDuration);
			args.Push (damagePerTick);
			coll.transform.root.SendMessage("setFire", args);
			
		}
    }
}
