var baseSpeed : float = 12.0;


function Update() {

	 /*************************************Move Cursor*****************************************/
	 
	 // Determine the angle of the left thumbstick
 	var xAxis = Input.GetAxis("L_XAxis_3");
 	var yAxis = Input.GetAxis("L_YAxis_3");
 	
 	
 	//find the target to move towards
 	var xtarget = transform.position.x + xAxis;
 	var ytarget = transform.position.y + yAxis;
 	var target = Vector3(xtarget, ytarget, 0);
 	
 	// The step size is equal to speed times frame time.
 	//This finds how hard the player is pushing the joystick, and changes speed
 	speed = Vector2(xAxis, yAxis).magnitude * baseSpeed ;
 	var step = speed * Time.deltaTime;
 	
 	// Move our position a step closer to the target.
 	transform.position = Vector3.MoveTowards(transform.position, target, step);
 	
 }