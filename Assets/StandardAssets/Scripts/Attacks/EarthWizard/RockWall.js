#pragma strict

function Start () {

}

function Update () {

}

function OnTriggerStay2D(coll: Collider2D) {
	if (coll.gameObject.transform.root.tag == "destroyableAttack"){
		coll.gameObject.SendMessage("disappear");
	}
	
	if (coll.gameObject.transform.root.name == "flameThrower(Clone)"){
		Destroy(coll.gameObject);
	}
}

function disappear(){
	gameObject.GetComponent(BoxCollider2D).enabled = false;
 	gameObject.GetComponent(SpriteRenderer).enabled = false;
	yield WaitForSeconds(GetComponent.<AudioSource>().clip.length);
	Destroy(gameObject);
}