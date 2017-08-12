
var color; 

function Update () {

     if (Time.time > 2 && color.a < 1){
         color.a += 0.01f;
         GetComponent.<Renderer>().material.color = color;
     }
 }
 
 function Start(){
 	color = GetComponent.<Renderer>().material.color;
    color.a = 0.0f;
    GetComponent.<Renderer>().material.color = color;
 }