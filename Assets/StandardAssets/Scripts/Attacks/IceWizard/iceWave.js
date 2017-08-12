
var iceCubeSprite;

function Start () {
	iceCubeSprite = Resources.Load("iceCube");
	transform.Translate( Vector2( 3, 0) );
}

function Update () {}


function OnTriggerEnter2D( coll : Collider2D) {
	//hit something, do damage and destroy
	if (coll.gameObject.transform.root.tag == "Player"){
	
		var script = coll.transform.root.GetComponent("PlayerStats");
		if (script.getStunned() == false){
			var sprite = Instantiate(iceCubeSprite, coll.transform.position, Quaternion.identity);
			sprite.transform.parent = coll.gameObject.transform.root;
			sprite.transform.position.y+=0;
			sprite.transform.position.x+=0;
		}

		script.SendMessage("stun", 1.5f );
		script.ApplyDamage(8);
		
	}
}

function disappear(){
	gameObject.GetComponent(CircleCollider2D).enabled = false;
 	gameObject.GetComponent(SpriteRenderer).enabled = false;
	yield WaitForSeconds(GetComponent.<AudioSource>().clip.length);
	Destroy(gameObject);
}