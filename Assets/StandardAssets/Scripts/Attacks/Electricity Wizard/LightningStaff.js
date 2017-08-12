

var electricitySprite : Transform;
var anim : Animator;

function Start () {
	electricitySprite = transform.Find("rodShock");
	electricitySprite.GetComponent.<Renderer>().enabled = false;
	lastDamage = Time.time; //doesn't strike as soon as it's set
	anim = electricitySprite.GetComponent(Animator);
}

var damageDelay = .5f;
var lastDamage = -10.0;


function Update () {

 	if(Time.time > damageDelay+lastDamage){ //if a second past
 	
        lastDamage = Time.time;
        shock();
    }
}

function shock(){

	electricitySprite.GetComponent.<Renderer>().enabled = true;
	var colliders = Physics2D.OverlapCircleAll( transform.position, 1.8f );
	GetComponent.<AudioSource>().Play();
	
	electricitySprite.GetComponent.<Renderer>().enabled = true;
	anim.Play("rodShock", -1, 0f);
	anim.speed = 7;
		
	 for(var col : Collider2D in colliders){
	 	if( col.transform.root.tag == "Player"){
            col.transform.root.GetComponent("PlayerStats").SendMessage("ApplyDamage", 3);
        }
     }
     yield WaitForSeconds(.3);
     electricitySprite.GetComponent.<Renderer>().enabled = false;
}

