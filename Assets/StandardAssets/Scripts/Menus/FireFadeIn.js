
var color;

function Update () {
 
     if (Time.time > 0 && color.a < 1){
         color.a += 0.02f;
         GetComponent.<Renderer>().material.color = color;
     }
 }
 
 function Start(){
    color = GetComponent.<Renderer>().material.color;
    color.a = 0;
    GetComponent.<Renderer>().material.color = color;
 }