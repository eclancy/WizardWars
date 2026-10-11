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
    // The ultimate is mostly earned rather than waited out. Passive trickle is slow;
    // most of the charge comes from spending energy (fighting) and from taking damage
    // (which gives the player who is losing a way back in).
    public float ultimateChargeRate = 1.2f;
    public float ultimateChargePerEnergy = 0.3f;
    public float ultimateChargePerDamageTaken = 0.3f;
    public bool ultimateReady;
    // Teams.NoTeam in Free-for-All. Set by ParkBattle at spawn and never serialized,
    // so a prefab cannot pin a player to a team.
    [System.NonSerialized] public int team = Teams.NoTeam;

    private float damageDelay = 0.5f;
    private float lastDamage = -10f;
    // When a cast was refused for want of energy, so the bar can say so.
    private float lastFizzle = -10f;
    private const float FizzleFlashTime = 0.35f;

    // Cooldown pips for the basic, second and third abilities. The ultimate has its
    // own meter. Rather than making every ability report its cooldown length, the
    // duration is learned by watching how long the ability stayed unavailable last
    // time; the pip is seeded with a guess and self-corrects after one use. Readiness
    // itself always comes from the wizard, so a pip never claims ready early.
    private const int PipCount = 3;
    private IWizardAbilities abilities;
    private readonly float[] pipStart = new float[PipCount];
    private readonly float[] pipDuration = { 4f, 4f, 4f };
    private readonly bool[] pipWasReady = { true, true, true };

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

        TrackCooldowns();

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
        AddUltimateCharge(damage * ultimateChargePerDamageTaken);
        if (health <= 0f)
        {
            InvokeGlobal("kill", name);
            Die();
        }
    }

    public bool DepleteEnergy(float energyUse)
    {
        if (energy < energyUse)
        {
            // Tell the player why nothing happened instead of silently swallowing it.
            lastFizzle = Time.time;
            return false;
        }
        energy -= energyUse;
        AddUltimateCharge(energyUse * ultimateChargePerEnergy);
        return true;
    }

    // The wizard component is added at runtime by CharacterSelect, so it is looked up
    // lazily rather than in Awake.
    private void TrackCooldowns()
    {
        // An interface-typed reference does not use Unity's overloaded == null, so a
        // destroyed wizard would still look alive here. Compare as a MonoBehaviour to
        // catch that and re-resolve, which matters when CharacterSelect swaps wizards.
        if (abilities == null || (abilities as MonoBehaviour) == null)
        {
            abilities = GetComponent<IWizardAbilities>();
            if (abilities == null) return;
        }
        for (int i = 0; i < PipCount; i++)
        {
            bool ready = abilities.IsAbilityReady(i + 1);
            if (!ready && pipWasReady[i]) pipStart[i] = Time.time;
            else if (ready && !pipWasReady[i])
            {
                float measured = Time.time - pipStart[i];
                if (measured > 0.05f) pipDuration[i] = measured;
            }
            pipWasReady[i] = ready;
        }
    }

    private float PipFill(int index)
    {
        if (abilities == null) return 1f;
        if (abilities.IsAbilityReady(index + 1)) return 1f;
        if (pipDuration[index] <= 0f) return 0f;
        return Mathf.Clamp01((Time.time - pipStart[index]) / pipDuration[index]);
    }

    public void AddUltimateCharge(float amount)
    {
        if (ultimateReady || amount <= 0f) return;
        ultimateCharge = Mathf.Min(100f, ultimateCharge + amount);
        if (ultimateCharge >= 100f) ultimateReady = true;
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

    // Puts the player on a team and marks them with a coloured disc on the ground,
    // so which side someone is on reads in the arena and not only on the HUD.
    public void SetTeam(int newTeam)
    {
        team = newTeam;
        Transform existing = transform.Find("TeamMarker");
        if (existing != null) Destroy(existing.gameObject);
        if (team == Teams.NoTeam) return;

        GameObject marker = new GameObject("TeamMarker");
        marker.transform.SetParent(transform, false);
        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralFx.Shadow;
        Color colour = Teams.Colour(team);
        renderer.color = new Color(colour.r, colour.g, colour.b, 0.6f);
        renderer.sortingOrder = 1;
        // The player is scaled down at spawn, so size the marker in world units.
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
        marker.transform.localScale = Vector3.one * (0.8f / parentScale);
        SpriteRenderer body = GetComponent<SpriteRenderer>();
        float feet = body != null && body.sprite != null ? body.bounds.extents.y * 0.8f : 0.4f;
        marker.transform.localPosition = new Vector3(0f, -feet / parentScale, 0f);
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
        Vector2 size = new Vector2(60f, 32f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.y = Screen.height - screenPos.y - 75f;
        screenPos.x -= size.x / 2f - 3f;
        // The old target dummy draws taller than a wizard, so its bars sit higher.
        if (transform.root.name == "AI(Clone)" && GetComponent<WizardAI>() == null) { screenPos.y -= Screen.height / 50f; screenPos.x -= 2f; }
        GUI.BeginGroup(new Rect(screenPos.x, screenPos.y, size.x, size.y));

        GUI.Box(new Rect(0f, 0f, size.x, 10f), healthBarEmpty);
        GUI.BeginGroup(new Rect(0f, 0f, size.x * health / 100f, size.y)); GUI.Box(new Rect(0f, 0f, size.x, 20f), healthBarFull); GUI.EndGroup();

        // The energy bar flashes red when a cast was refused for want of energy.
        Color previous = GUI.color;
        if (Time.time < lastFizzle + FizzleFlashTime)
        {
            float flash = 1f - (Time.time - lastFizzle) / FizzleFlashTime;
            GUI.color = Color.Lerp(Color.white, new Color(1f, 0.25f, 0.2f), flash);
        }
        GUI.Box(new Rect(0f, 8f, size.x, 10f), healthBarEmpty);
        GUI.BeginGroup(new Rect(0f, 0f, size.x * energy / 100f, size.y)); GUI.Box(new Rect(0f, 8f, size.x, 20f), energyBarFull); GUI.EndGroup();
        GUI.color = previous;

        // Ultimate meter. Gold while filling, flashing white once it is ready, so a
        // 25-second investment is not invisible.
        GUI.Box(new Rect(0f, 16f, size.x, 10f), healthBarEmpty);
        GUI.color = ultimateReady
            ? Color.Lerp(new Color(1f, 0.85f, 0.2f), Color.white, (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f)
            : new Color(0.85f, 0.65f, 0.15f);
        GUI.BeginGroup(new Rect(0f, 0f, size.x * ultimateCharge / 100f, size.y)); GUI.Box(new Rect(0f, 16f, size.x, 20f), energyBarFull); GUI.EndGroup();
        GUI.color = previous;

        // One pip per ability: basic, second, third. A pip refills left to right while
        // its ability is recharging and brightens the moment it is castable again.
        const float pipTop = 25f, pipHeight = 5f, pipGap = 3f;
        float pipWidth = (size.x - pipGap * (PipCount - 1)) / PipCount;
        for (int i = 0; i < PipCount; i++)
        {
            float x = i * (pipWidth + pipGap);
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.Box(new Rect(x, pipTop, pipWidth, pipHeight), healthBarEmpty);
            float fill = PipFill(i);
            if (fill > 0f)
            {
                GUI.color = fill >= 1f ? new Color(0.55f, 0.92f, 1f) : new Color(0.3f, 0.45f, 0.55f);
                GUI.BeginGroup(new Rect(x, pipTop, pipWidth * fill, pipHeight));
                GUI.Box(new Rect(0f, 0f, pipWidth, pipHeight), energyBarFull);
                GUI.EndGroup();
            }
            GUI.color = previous;
        }

        GUI.EndGroup();

        // A strip in the team colour down the left edge of the bars.
        if (team != Teams.NoTeam)
        {
            Color outer = GUI.color;
            GUI.color = Teams.Colour(team);
            GUI.DrawTexture(new Rect(screenPos.x - 6f, screenPos.y, 4f, 30f), Texture2D.whiteTexture);
            GUI.color = outer;
        }
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