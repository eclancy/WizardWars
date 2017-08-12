#pragma strict

function Start () {

}

function Update () {

}

function OnTriggerStay2D( coll : Collider2D) {
	if (coll.gameObject.transform.root.tag == "Player" ){
		var script = coll.transform.root.GetComponent("ControllerScript");
		script.SendMessage("sliding");
		script.SendMessage("putOutFire");
	}
	
	if (coll.gameObject.name == "mudPatch(Clone)"){
		Destroy(coll.gameObject);
	}
}
function OnTriggerExit2D( coll : Collider2D) {
	//hit something, do damage and destroy
	if (coll.gameObject.transform.root.tag == "Player"){
	
		var script = coll.transform.root.GetComponent("ControllerScript");
		script.SendMessage("notSliding");
	}
}

function disappear(){
	gameObject.GetComponent(PolygonCollider2D).enabled = false;
 	gameObject.GetComponent(SpriteRenderer).enabled = false;
	yield WaitForSeconds(GetComponent.<AudioSource>().clip.length);
	Destroy(gameObject);
}