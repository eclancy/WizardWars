#pragma strict
import System.Collections.Generic;

private var spawnTime = 0;
private var inRangeList = new List. < GameObject > ();

function Start () {
	spawnTime = Time.time;

}

function Update () {
	if(Time.time > spawnTime + 3){
	
		for(character in inRangeList){
			var script = character.transform.root.GetComponent("ControllerScript");
			script.SendMessage("removeSlow", 50 );
		}
		Destroy(gameObject);
	}
}

function OnTriggerEnter2D( coll : Collider2D) {
	//hit something, do damage and destroy
	if (coll.gameObject.transform.root.tag == "Player" ){
		var script = coll.transform.root.GetComponent("ControllerScript");
		script.SendMessage("slow", 50);
		script.SendMessage("putOutFire");
		inRangeList.Add(coll.transform.root.gameObject);
	}
	
	if (coll.gameObject.name == "ringOfFire(Clone)"){
		Destroy(coll.gameObject);
	}
}

function OnTriggerExit2D( coll : Collider2D ) {
	//hit something, do damage and destroy
	if (coll.gameObject.transform.root.tag == "Player"){
		var script = coll.transform.root.GetComponent("ControllerScript");
		script.SendMessage("removeSlow", 50 );
		inRangeList.Remove(coll.transform.root.gameObject);
	}
}

function disappear(){
	gameObject.GetComponent(PolygonCollider2D).enabled = false;
 	gameObject.GetComponent(SpriteRenderer).enabled = false;
	yield WaitForSeconds(GetComponent.<AudioSource>().clip.length);
	Destroy(gameObject);
}