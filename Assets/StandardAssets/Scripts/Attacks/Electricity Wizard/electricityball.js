
function Start () {
}

function FixedUpdate () {
	//move forward, the vector is the speed
    transform.Translate( Vector3(.9,0,0) * Time.deltaTime * 10);
}

function OnTriggerEnter2D( coll : Collider2D) {
	//hit something, do damage and destroy
	if (coll.gameObject.tag == "Player"){
		coll.gameObject.SendMessage("ApplyDamage", 20);
	}
	//if it hit another basic attack, destroy it
	else if(coll.gameObject.name == "fireball(Clone)" ||
		coll.gameObject.name == "mudball(Clone)" ||
		coll.gameObject.name == "icicle(Clone)"){
		coll.SendMessage("disappear");
	}
	disappear();
}

function disappear(){
	gameObject.GetComponent(CircleCollider2D).enabled = false;
 	gameObject.GetComponent(SpriteRenderer).enabled = false;
	yield WaitForSeconds(GetComponent.<AudioSource>().clip.length);
	Destroy(gameObject);
}