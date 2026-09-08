using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float health = 100f;
    public float energy = 100f;
    public float recoveryRate = 20f;
    public GameObject GlobalHandler;
    public Component GlobalHandlerScript;
    public ControllerScript controllerScript;
    public Texture2D healthBarEmpty;
    public Texture2D healthBarFull;
    public Texture2D energyBarFull;
    public bool rechargeEnabled = true;
    public GameObject fireSprite;
    public GameObject onFireObject;
    public bool stunned;
    public bool onFire;
    public float burnDuration;
    public float burnDamage;
    public float stunDuration;
    public bool GUIEnabled;
    public float ultimateCharge;
    public float ultimateChargeRate = 4f;
    public bool ultimateReady;

    private float damageDelay = 0.5f;
    private float lastDamage = -10f;

    private void Awake()
    {
        GlobalHandler = GameObject.Find("GlobalHandler");
        if (GlobalHandler != null) GlobalHandlerScript = GlobalHandler.GetComponent("GlobalHandler");
        controllerScript = GetComponent<ControllerScript>();
        healthBarEmpty = Resources.Load<Texture2D>("BarEmpty");
        healthBarFull = Resources.Load<Texture2D>("HealthBarFull");
        energyBarFull = Resources.Load<Texture2D>("EnergyBarFull");
        fireSprite = Resources.Load<GameObject>("OnFire");
    }

    private void Update()
    {
        if (burnDuration > 0f)
        {
            if (Time.time > damageDelay + lastDamage)
            {
                ApplyDamage((int)burnDamage);
                lastDamage = Time.time;
                burnDuration -= 0.5f;
            }
        }
        else if (onFire)
        {
            if (onFireObject != null) Destroy(onFireObject);
            onFire = false;
        }

        if (stunDuration > 0f) stunDuration -= Time.deltaTime;
        else if (stunned)
        {
            Transform iceCube = transform.Find("iceCube(Clone)");
            if (iceCube != null) Destroy(iceCube.gameObject);
            if (controllerScript != null) controllerScript.enabled = true;
            stunned = false;
        }

        if (energy < 100f && rechargeEnabled) energy = Mathf.Min(100f, energy + recoveryRate * Time.deltaTime);
        if (!ultimateReady)
        {
            ultimateCharge = Mathf.Min(100f, ultimateCharge + ultimateChargeRate * Time.deltaTime);
            if (ultimateCharge >= 100f) ultimateReady = true;
        }
    }

    public bool TryUseUltimate()
    {
        if (!ultimateReady) return false;
        ultimateReady = false;
        ultimateCharge = 0f;
        return true;
    }

    public void ApplyDamage(int damage)
    {
        health -= damage;
        if (health <= 0f)
        {
            InvokeGlobal("kill", name);
            Die();
        }
    }

    public bool DepleteEnergy(float energyUse)
    {
        if (energy < energyUse) return false;
        energy -= energyUse;
        return true;
    }

    public void stun(float stunTime)
    {
        stunDuration = stunTime;
        stunned = true;
        if (controllerScript != null) controllerScript.enabled = false;
        Transform fire = transform.Find("OnFire(Clone)");
        if (fire != null) Destroy(fire.gameObject);
    }

    public void setFire(object[] args)
    {
        if (!onFire && fireSprite != null)
        {
            onFireObject = Instantiate(fireSprite, transform.position, Quaternion.identity);
            onFireObject.transform.SetParent(transform.root);
            StartCoroutine(RotateObject(onFireObject));
            onFire = true;
        }
        if (args != null && args.Length >= 2)
        {
            burnDuration = System.Convert.ToSingle(args[0]);
            burnDamage = System.Convert.ToSingle(args[1]);
        }
    }

    public void Die()
    {
        if (controllerScript != null && controllerScript.targeter != null) Destroy(controllerScript.targeter);
        Destroy(gameObject);
    }

    public bool getOnFire() => onFire;
    public bool getStunned() => stunned;
    public float getEnergy() => energy;
    public void putOutFire() { onFire = false; burnDuration = 0f; }
    public void noRecharge() => rechargeEnabled = false;
    public void recharge() => rechargeEnabled = true;

    private void OnGUI()
    {
        if (!GUIEnabled || Camera.main == null) return;
        Vector2 size = new Vector2(60f, 20f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.y = Screen.height - screenPos.y - 75f;
        screenPos.x -= size.x / 2f - 3f;
        if (transform.root.name == "AI(Clone)") { screenPos.y -= Screen.height / 50f; screenPos.x -= 2f; }
        GUI.BeginGroup(new Rect(screenPos.x, screenPos.y, size.x, size.y));
        GUI.Box(new Rect(0f, 0f, size.x, size.y / 2f), healthBarEmpty);
        GUI.BeginGroup(new Rect(0f, 0f, size.x * health / 100f, size.y)); GUI.Box(new Rect(0f, 0f, size.x, size.y), healthBarFull); GUI.EndGroup();
        GUI.Box(new Rect(0f, 8f, size.x, size.y / 2f), healthBarEmpty);
        GUI.BeginGroup(new Rect(0f, 0f, size.x * energy / 100f, size.y)); GUI.Box(new Rect(0f, 8f, size.x, size.y), energyBarFull); GUI.EndGroup();
        GUI.EndGroup();
    }

    private void InvokeGlobal(string methodName, string argument)
    {
        if (GlobalHandlerScript != null) GlobalHandlerScript.SendMessage(methodName, argument, SendMessageOptions.DontRequireReceiver);
    }

    private IEnumerator RotateObject(GameObject target)
    {
        while (target != null)
        {
            target.transform.Rotate(0f, 180f, 0f);
            yield return new WaitForSeconds(0.3f);
        }
    }
}