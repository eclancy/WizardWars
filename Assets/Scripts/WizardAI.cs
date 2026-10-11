using System.Collections.Generic;
using UnityEngine;

// The solo opponent. It used to be a target dummy that never moved or attacked; now
// ParkBattle builds it from a player template, gives it a wizard, and this component
// plays that wizard: it keeps a fighting distance, strafes, sidesteps incoming shots,
// and casts the same abilities a human would, through the same entry points.
//
// It deliberately plays a little worse than it could. It reacts after a short delay,
// aims with some error, and holds abilities back at random, so a new player can beat
// it and a practised one has to keep moving.
//
// ControllerScript stays on the AI so slows, speed-ups, ice sliding and stuns keep
// working, but its input loop does nothing here: the clone is named "AI(Clone)", which
// maps to no input suffix. This component moves the AI itself using the same speed
// formula ControllerScript uses.
public class WizardAI : MonoBehaviour
{
    public float preferredRange = 5.5f;
    public float reactionDelay = 0.35f;
    public float aimErrorDegrees = 9f;
    // The player's targeter reaches 4.5 units, so placed abilities are held to that.
    public float placementRange = 4.5f;
    public float dodgeChance = 0.6f;

    // Arena interior the AI stays inside, a little within the spawn points.
    private static readonly Vector2 Bounds = new Vector2(8.6f, 4.7f);

    private ControllerScript controller;
    private PlayerStats stats;
    private IWizardAbilities wizard;
    private string wizardName = "";
    private GlobalHandler globalHandler;

    private GameObject target;
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private float targetSeenAt;

    private Vector2 strafe = Vector2.up;
    private float nextStrafeFlip;
    private Vector2 dodge;
    private float dodgeUntil;
    private float nextDodgeScan;
    private Vector2 moveDirection;
    private float nextAbilityThink;

    private void Start()
    {
        controller = GetComponent<ControllerScript>();
        stats = GetComponent<PlayerStats>();
        GameObject handler = GameObject.Find("GlobalHandler");
        if (handler != null) globalHandler = handler.GetComponent<GlobalHandler>();
    }

    private bool Battling => globalHandler == null || globalHandler.getBattling();

    private void Update()
    {
        if (!Battling || controller == null || stats == null) return;
        if (wizard == null || (wizard as MonoBehaviour) == null)
        {
            wizard = GetComponent<IWizardAbilities>();
            if (wizard != null) wizardName = wizard.GetType().Name;
        }
        // A stun disables the ControllerScript; the AI is frozen exactly as long.
        if (!controller.enabled) return;

        AcquireTarget();
        Move();
        if (target != null && !controller.ultimateActive && Time.time >= nextAbilityThink) UseAbilities();
    }

    // ---- Targeting ----

    private void AcquireTarget()
    {
        GameObject nearest = null;
        float best = float.MaxValue;
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == gameObject || Teams.AreAllies(gameObject, player)) continue;
            float distance = (player.transform.position - transform.position).sqrMagnitude;
            if (distance < best) { best = distance; nearest = player; }
        }

        if (nearest != target)
        {
            target = nearest;
            targetVelocity = Vector3.zero;
            if (target != null) lastTargetPosition = target.transform.position;
            // Noticing a new target takes a moment, like a human looking over.
            nextAbilityThink = Time.time + reactionDelay;
            return;
        }
        if (target == null) return;

        float dt = Time.time - targetSeenAt;
        if (dt > 0.05f)
        {
            Vector3 velocity = (target.transform.position - lastTargetPosition) / dt;
            targetVelocity = Vector3.Lerp(targetVelocity, velocity, 0.5f);
            lastTargetPosition = target.transform.position;
            targetSeenAt = Time.time;
        }
    }

    private float TargetDistance => target == null ? float.MaxValue : (target.transform.position - transform.position).magnitude;

    // Where a shot of the given speed should go to meet the target, plus aim error.
    private Vector3 AimDirection(float projectileSpeed)
    {
        Vector3 toTarget = target.transform.position - transform.position;
        float travelTime = toTarget.magnitude / Mathf.Max(0.1f, projectileSpeed);
        Vector3 lead = toTarget + targetVelocity * travelTime * Random.Range(0.4f, 1f);
        float angle = Mathf.Atan2(lead.y, lead.x) * Mathf.Rad2Deg + Random.Range(-aimErrorDegrees, aimErrorDegrees);
        return new Vector3(0f, 0f, angle);
    }

    private Vector3 PlacementPoint(Vector3 point)
    {
        Vector3 offset = Vector3.ClampMagnitude(point - transform.position, placementRange);
        return transform.position + offset;
    }

    // The same argument shape ControllerScript sends: an aim rotation and a point.
    private object[] Args(Vector3 aim, Vector3 point) => new object[] { aim, point };

    // ---- Movement ----

    private void Move()
    {
        if (!controller.cantmove)
        {
            Vector2 desired = Vector2.zero;
            if (target != null)
            {
                Vector2 toTarget = target.transform.position - transform.position;
                float distance = toTarget.magnitude;
                Vector2 toward = distance > 0.01f ? toTarget / distance : Vector2.right;
                // Close in or back off toward the preferred range, strafing across it.
                float rangeError = Mathf.Clamp((distance - preferredRange) / 2f, -1f, 1f);
                if (Time.time >= nextStrafeFlip)
                {
                    strafe = Random.value < 0.5f ? new Vector2(-toward.y, toward.x) : new Vector2(toward.y, -toward.x);
                    nextStrafeFlip = Time.time + Random.Range(0.9f, 2.4f);
                }
                desired = toward * rangeError + strafe * 0.8f;
            }

            ScanForIncomingShots();
            if (Time.time < dodgeUntil) desired = dodge;

            desired += StayInside();
            moveDirection = Vector2.ClampMagnitude(desired, 1f);

            // Walking into a wall or obstacle: slide along it instead.
            Vector2 ahead = (Vector2)transform.position + moveDirection * 0.6f;
            if (moveDirection.sqrMagnitude > 0.01f && ProceduralFx.IsInsideSolid(ahead, 0.3f))
            {
                moveDirection = new Vector2(-moveDirection.y, moveDirection.x);
                strafe = -strafe;
            }
        }
        // While sliding on ice the AI keeps its last direction, as a player does.

        float activeSpeed = controller.baseSpeed - controller.baseSpeed * controller.slowPercentage / 100f + controller.baseSpeed * controller.speedPercentage / 100f;
        controller.speed = moveDirection.magnitude * activeSpeed;
        controller.xAxis = moveDirection.x;
        controller.yAxis = moveDirection.y;
        transform.rotation = Quaternion.identity;
        transform.position = Vector3.MoveTowards(transform.position, transform.position + (Vector3)moveDirection, controller.speed * Time.deltaTime);
    }

    private Vector2 StayInside()
    {
        Vector2 push = Vector2.zero;
        Vector2 position = transform.position;
        if (position.x > Bounds.x) push.x = -1f; else if (position.x < -Bounds.x) push.x = 1f;
        if (position.y > Bounds.y) push.y = -1f; else if (position.y < -Bounds.y) push.y = 1f;
        return push * 1.5f;
    }

    // Basic projectiles all fly along their own right vector. One heading at the AI
    // and about to arrive triggers a sidestep, some of the time.
    private void ScanForIncomingShots()
    {
        if (Time.time < nextDodgeScan || Time.time < dodgeUntil) return;
        nextDodgeScan = Time.time + 0.12f;
        foreach (Transform shot in IncomingShots())
        {
            Vector2 fromShot = transform.position - shot.position;
            float distance = fromShot.magnitude;
            if (distance < 0.4f || distance > 3.5f) continue;
            Vector2 heading = shot.right;
            if (Vector2.Dot(heading, fromShot / distance) < 0.93f) continue;
            if (Random.value > dodgeChance) return;
            dodge = Random.value < 0.5f ? new Vector2(-heading.y, heading.x) : new Vector2(heading.y, -heading.x);
            dodgeUntil = Time.time + 0.35f;
            return;
        }
    }

    private static IEnumerable<Transform> IncomingShots()
    {
        foreach (Fireball shot in FindObjectsOfType<Fireball>()) yield return shot.transform;
        foreach (Icicle shot in FindObjectsOfType<Icicle>()) yield return shot.transform;
        foreach (Mudball shot in FindObjectsOfType<Mudball>()) yield return shot.transform;
        foreach (ElectricityBall shot in FindObjectsOfType<ElectricityBall>()) yield return shot.transform;
    }

    // ---- Abilities ----

    private void UseAbilities()
    {
        // Think a few times a second rather than every frame, with jitter, so the AI
        // never fires the instant a cooldown ends.
        nextAbilityThink = Time.time + Random.Range(0.18f, 0.4f);
        if (wizard == null) return;
        float distance = TargetDistance;
        Vector3 targetPoint = target.transform.position + targetVelocity * 0.5f;

        if (stats.ultimateReady && UltimateInRange(distance))
        {
            SendMessage("ultimateAttack", Args(AimDirection(9f), PlacementPoint(targetPoint)), SendMessageOptions.DontRequireReceiver);
            return;
        }

        if (wizard.IsAbilityReady(3) && Random.value < 0.5f && TryThird(distance, targetPoint)) return;

        if (wizard.IsAbilityReady(2) && distance < placementRange + 1.5f && stats.energy > 45f && Random.value < 0.45f)
        {
            SendMessage("secondAttack", Args(AimDirection(9f), PlacementPoint(targetPoint)), SendMessageOptions.DontRequireReceiver);
            return;
        }

        if (wizard.IsAbilityReady(1) && distance < 10f)
            SendMessage("basicAttack", Args(AimDirection(9f), PlacementPoint(targetPoint)), SendMessageOptions.DontRequireReceiver);
    }

    private bool UltimateInRange(float distance)
    {
        switch (wizardName)
        {
            case "FrostWizard": return distance < 6.5f;       // Absolute Zero reaches 8
            case "EarthWizard": return distance < 5.5f;       // Earthquake reaches 6.5
            case "ElectricityWizard": return distance < 5f;   // Overcharge sprays around the caster
            default: return true;                             // Meteor Storm covers the arena
        }
    }

    private bool TryThird(float distance, Vector3 targetPoint)
    {
        switch (wizardName)
        {
            case "FrostWizard":
                // Ice Wave sweeps out to about seven units in a 70 degree arc.
                if (distance > 6f || stats.energy < 40f) return false;
                break;
            case "EarthWizard":
                // A rock wall between the AI and a nearby enemy blocks their shots.
                if (distance > 5f || stats.energy < 30f) return false;
                targetPoint = Vector3.Lerp(transform.position, target.transform.position, 0.45f);
                break;
            case "ElectricityWizard":
                // Speed Up to escape when hurt, or to close a long gap.
                if (!(stats.health < 40f && distance < 4f) && distance < 8f) return false;
                if (stats.energy < 30f) return false;
                break;
            default:
                // The flamethrower is channelled by holding a button, which the AI has
                // no input axis for. Fire plays without it.
                return false;
        }
        SendMessage("thirdAttack", Args(AimDirection(9f), PlacementPoint(targetPoint)), SendMessageOptions.DontRequireReceiver);
        return true;
    }
}

// The four wizards' portraits, so a scene that never showed the character grid can
// still dress a wizard. CharacterSelect records them from its "Character" objects.
public static class WizardRoster
{
    public static readonly string[] Names = { "FireWizard", "FrostWizard", "EarthWizard", "ElectricityWizard" };
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public static void Capture()
    {
        foreach (GameObject character in GameObject.FindGameObjectsWithTag("Character"))
        {
            SpriteRenderer renderer = character.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) sprites[character.name] = renderer.sprite;
        }
    }

    public static Sprite SpriteFor(string wizardName) => sprites.TryGetValue(wizardName, out Sprite sprite) ? sprite : null;

    public static void Attach(GameObject player, string wizardName)
    {
        if (wizardName == "FireWizard") player.AddComponent<FireWizard>();
        else if (wizardName == "FrostWizard") player.AddComponent<FrostWizard>();
        else if (wizardName == "EarthWizard") player.AddComponent<EarthWizard>();
        else if (wizardName == "ElectricityWizard") player.AddComponent<ElectricityWizard>();
    }
}
