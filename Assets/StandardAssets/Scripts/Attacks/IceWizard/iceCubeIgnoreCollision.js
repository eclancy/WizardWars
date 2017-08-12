#pragma strict

function Start () {
	//make sure player can't hit himself with it
    Physics2D.IgnoreCollision( GetComponent.<Collider2D>(), transform.root.GetComponent.<Collider2D>() );

}

function Update () {

}