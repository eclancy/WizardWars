using System;
using UnityEngine;

public class ControllerScript : MonoBehaviour
{
    public float baseSpeed = 4f;
    public float activeSpeed = 4f;
    public float speed;
    public GameObject targeterLoad;
    public GameObject targeter;
    public string L_XAxis;
    public string L_YAxis;
    public string R_XAxis;
    public string R_YAxis;
    public string Triggers;
    public string RB;
    public string LB;
    public string X;
    public Component characterScript;
    public bool cantmove;
    public float targeterRange = 6f;
    public GameObject GlobalHandler;
    public Component GlobalHandlerScript;
    public float xAxis;
    public float yAxis;
    public float slowPercentage;
    public float speedPercentage;
    public bool ultimateActive;

    private void Start()
    {
        xAxis = 0f;
        yAxis = 0f;
        GlobalHandler = GameObject.Find("GlobalHandler");
        if (GlobalHandler != null)
        {
            GlobalHandlerScript = GlobalHandler.GetComponent("GlobalHandler");
        }
    }

    public void setBaseSpeed(float percent)
    {
        baseSpeed = baseSpeed * percent / 100f;
        activeSpeed = baseSpeed;
    }

    public void slow(float percent) => slowPercentage += percent;
    public void removeSlow(float percent) => slowPercentage -= percent;
    public void speedUp(float percent) => speedPercentage += percent;
    public void removeSpeed(float percent) => speedPercentage -= percent;

    public void sliding()
    {
        if (speed != 0f) cantmove = true;
    }

    public void notSliding() => cantmove = false;

    private void LateUpdate()
    {
        slowPercentage = Mathf.Max(0f, slowPercentage);
        speedPercentage = Mathf.Max(0f, speedPercentage);

        if (!IsBattling()) return;

        if (string.IsNullOrEmpty(LB))
        {
            int playerNumber = GetPlayerNumber();
            if (playerNumber == 0) return;
            L_XAxis = "L_XAxis_" + playerNumber;
            L_YAxis = "L_YAxis_" + playerNumber;
            R_XAxis = "R_XAxis_" + playerNumber;
            R_YAxis = "R_YAxis_" + playerNumber;
            Triggers = "Triggers_" + playerNumber;
            RB = "RB_" + playerNumber;
            LB = "LB_" + playerNumber;
            X = "X_" + playerNumber;
            CreateTargeter();
            return;
        }

        activeSpeed = baseSpeed - baseSpeed * slowPercentage / 100f + baseSpeed * speedPercentage / 100f;
        if (!cantmove)
        {
            xAxis = Input.GetAxis(L_XAxis);
            yAxis = Input.GetAxis(L_YAxis);
        }

        transform.rotation = Quaternion.identity;
        Vector3 moveTarget = transform.position + new Vector3(xAxis, yAxis, 0f);
        speed = new Vector2(xAxis, yAxis).magnitude * activeSpeed;
        transform.position = Vector3.MoveTowards(transform.position, moveTarget, speed * Time.deltaTime);

        if (targeter == null) return;
        float xAim = Input.GetAxis(R_XAxis);
        float yAim = Input.GetAxis(R_YAxis);
        bool aiming = new Vector2(xAim, yAim).sqrMagnitude > 0.04f;
        SpriteRenderer targeterRenderer = targeter.GetComponent<SpriteRenderer>();
        if (targeterRenderer != null) targeterRenderer.enabled = aiming;
        float targetAngle = Mathf.Atan2(-yAim, xAim) * Mathf.Rad2Deg;
        Vector3 targetVector = new Vector3(0f, 0f, targetAngle);
        targeter.transform.position = transform.position + new Vector3(xAim * targeterRange, -yAim * targeterRange, 0f);
        object[] args = { targetVector, targeter.transform.position };

        if (!ultimateActive && Input.GetAxis(Triggers) > 0.5f) SendMessage("basicAttack", args, SendMessageOptions.DontRequireReceiver);
        if (!ultimateActive && Input.GetAxis(Triggers) < -0.5f) SendMessage("secondAttack", args, SendMessageOptions.DontRequireReceiver);
        if (!ultimateActive && Input.GetButtonDown(RB)) SendMessage("thirdAttack", new object[] { targetVector, targeter.transform.position, RB, R_XAxis, R_YAxis }, SendMessageOptions.DontRequireReceiver);
        if (!ultimateActive && Input.GetButtonDown(LB)) SendMessage("fourthAttack", args, SendMessageOptions.DontRequireReceiver);
        if (!ultimateActive && Input.GetButtonDown(X)) SendMessage("ultimateAttack", args, SendMessageOptions.DontRequireReceiver);
        activeSpeed = baseSpeed;
    }

    private bool IsBattling()
    {
        if (GlobalHandlerScript == null) return true;
        var method = GlobalHandlerScript.GetType().GetMethod("getBattling");
        return method == null || Convert.ToBoolean(method.Invoke(GlobalHandlerScript, null));
    }

    private int GetPlayerNumber()
    {
        string objectName = transform.name;
        if (objectName.Contains("Player1")) return 1;
        if (objectName.Contains("Player2")) return 2;
        if (objectName.Contains("Player3")) return 3;
        if (objectName.Contains("Player4")) return 4;
        return 0;
    }

    private void CreateTargeter()
    {
        SpriteRenderer renderer = transform.root.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null) return;
        string targeterName = renderer.sprite.name == "Fire Wizard" ? "RedTargeter" :
            renderer.sprite.name == "Frost Wizard" ? "BlueTargeter" :
            renderer.sprite.name == "Electricity Wizard" ? "YellowTargeter" :
            renderer.sprite.name == "Earth Wizard" ? "GreenTargeter" : null;
        if (targeterName == null) return;
        targeterLoad = Resources.Load<GameObject>(targeterName);
        if (targeterLoad != null) targeter = Instantiate(targeterLoad, transform.position, transform.rotation);
    }
}