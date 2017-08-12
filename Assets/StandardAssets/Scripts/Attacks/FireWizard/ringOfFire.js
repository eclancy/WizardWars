
var burnDuration = 6.0;
var damagePerTick = 3.0;
var fireSprite;

function Start () {
	fireSprite = Resources.Load("OnFire");
}

function Update () {

}


function OnTriggerStay2D( coll : Collider2D) {

	if (coll.gameObject.transform.root.tag == "Player"){
		var args = new Array ();
		args.Push (burnDuration);
		args.Push (damagePerTick);
		coll.transform.root.SendMessage("setFire", args);
		
	}
	if (coll.gameObject.name == "icePatch(Clone)"){
		Destroy(coll.gameObject);
	}
}

function disappear(){
	gameObject.GetComponent(PolygonCollider2D).enabled = false;
 	gameObject.GetComponent(SpriteRenderer).enabled = false;
	yield WaitForSeconds(GetComponent.<AudioSource>().clip.length);
	Destroy(gameObject);
}