using Godot;

/// <summary>
/// The computer opponent, ported from Unity's WizardAI. It produces an <see cref="InputState"/>
/// like any pad, so the wizard it drives plays by exactly the same rules - and unlike Unity it
/// can hold a button, so a computer Fire wizard uses the flamethrower.
///
/// It keeps a fighting distance, strafes, sidesteps incoming shots, and casts with a reaction
/// delay, aim error, and abilities held back at random, so it can be beaten. Distances are in
/// Unity units.
/// </summary>
public class CpuInputSource : IInputSource
{
	public float PreferredRange = 5.5f;
	public float ReactionDelay = 0.35f;
	public float AimErrorDegrees = 9.0f;
	public float DodgeChance = 0.6f;
	/// <summary>Seconds between decisions (random in this range).</summary>
	public float ThinkMin = 0.18f, ThinkMax = 0.4f;
	/// <summary>How readily it spends abilities when it could: 1 is normal, higher is keener.</summary>
	public float Eagerness = 1.0f;
	/// <summary>Seconds between wiggles when frozen.</summary>
	public float WiggleMin = 0.16f, WiggleMax = 0.3f;

	public CpuInputSource() { }

	/// <summary>
	/// A computer at a skill level. Easy reacts late, aims loosely, rarely dodges and holds its
	/// abilities back. Hard reacts fast, aims tightly, nearly always dodges and spends freely.
	/// Normal is the tuning the soak test measures.
	/// </summary>
	public CpuInputSource(Difficulty difficulty)
	{
		switch (difficulty)
		{
			case Difficulty.Easy:
				ReactionDelay = 0.7f; AimErrorDegrees = 22.0f; DodgeChance = 0.15f;
				ThinkMin = 0.4f; ThinkMax = 0.75f; Eagerness = 0.55f; WiggleMin = 0.3f; WiggleMax = 0.5f;
				break;
			case Difficulty.Hard:
				ReactionDelay = 0.18f; AimErrorDegrees = 4.0f; DodgeChance = 0.9f;
				ThinkMin = 0.1f; ThinkMax = 0.22f; Eagerness = 1.5f; WiggleMin = 0.1f; WiggleMax = 0.17f;
				break;
		}
	}
	const float TargeterRange = 4.5f;
	const float ProjectileSpeed = 9.0f;

	Wizard self;
	Wizard target;
	Vector2 lastTargetPosition, targetVelocity;
	double targetSeenAt, nextThink, nextStrafeFlip, dodgeUntil, nextDodgeScan;
	Vector2 strafe = Vector2.Down, dodge, aimPoint;
	bool channelling;

	public void Attach(Wizard wizard) => self = wizard;

	Battle Battle => self.Battle;
	double Now => Battle.Time;
	static float U(float units) => Units.U(units);

	double nextWiggle;
	float wiggleSide = 1.0f;

	/// <summary>Frozen in ice: rock the stick from side to side to break out, a few times a second.</summary>
	InputState Wiggle()
	{
		if (Now >= nextWiggle)
		{
			nextWiggle = Now + GD.RandRange(WiggleMin, WiggleMax);
			wiggleSide = -wiggleSide;
		}
		return new InputState { Move = new Vector2(wiggleSide, 0.0f) };
	}

	public InputState Poll()
	{
		if (self == null || !GodotObject.IsInstanceValid(self) || !self.Alive || !Battle.Fighting) return InputState.None;
		if (self.Frozen) return Wiggle();

		AcquireTarget();
		InputState state = new() { Move = Steer() };
		if (target == null) return state;

		// Keep the aim on the target between decisions, so a held flame tracks it.
		if (channelling) aimPoint = target.GlobalPosition;
		state.Aim = AimToward(aimPoint);

		if (channelling)
		{
			bool keepBurning = Distance < U(3.6f) && self.Energy > 4.0f;
			state.ThirdHeld = keepBurning;
			if (!keepBurning) channelling = false;
			return state;
		}

		if (self.UltimateActive || Now < nextThink) return state;
		// Think a few times a second, with jitter, so it never fires the instant a cooldown ends.
		nextThink = Now + GD.RandRange(ThinkMin, ThinkMax);
		Decide(ref state);
		return state;
	}

	// ---- Targeting ----

	void AcquireTarget()
	{
		Wizard nearest = null;
		float best = float.MaxValue;
		foreach (Wizard wizard in Battle.Wizards)
		{
			if (!Combat.CanAffect(self, self.Team, wizard)) continue;
			float distance = wizard.GlobalPosition.DistanceSquaredTo(self.GlobalPosition);
			if (distance < best) { best = distance; nearest = wizard; }
		}

		if (nearest != target)
		{
			target = nearest;
			targetVelocity = Vector2.Zero;
			channelling = false;
			if (target != null) { lastTargetPosition = target.GlobalPosition; aimPoint = target.GlobalPosition; }
			// Noticing a new target takes a moment, like a human looking over.
			nextThink = Now + ReactionDelay;
			return;
		}
		if (target == null) return;

		double dt = Now - targetSeenAt;
		if (dt > 0.05)
		{
			Vector2 velocity = (target.GlobalPosition - lastTargetPosition) / (float)dt;
			targetVelocity = targetVelocity.Lerp(velocity, 0.5f);
			lastTargetPosition = target.GlobalPosition;
			targetSeenAt = Now;
		}
	}

	float Distance => target == null ? float.MaxValue : target.GlobalPosition.DistanceTo(self.GlobalPosition);

	/// <summary>Where a shot should go to meet the target, with lead and aim error.</summary>
	Vector2 LeadPoint()
	{
		Vector2 toTarget = target.GlobalPosition - self.GlobalPosition;
		float travel = toTarget.Length() / U(ProjectileSpeed);
		Vector2 lead = toTarget + targetVelocity * travel * (float)GD.RandRange(0.4, 1.0);
		float error = Mathf.DegToRad((float)GD.RandRange(-AimErrorDegrees, AimErrorDegrees));
		return self.GlobalPosition + lead.Rotated(error);
	}

	/// <summary>A stick deflection that puts the targeter on the point (clamped to its range).</summary>
	Vector2 AimToward(Vector2 point)
	{
		Vector2 offset = point - self.GlobalPosition;
		float length = offset.Length();
		if (length < 1.0f) return self.AimDirection * 0.25f;
		// Never below the aiming threshold, so a close target still turns the aim.
		return offset / length * Mathf.Clamp(length / U(TargeterRange), 0.25f, 1.0f);
	}

	// ---- Movement ----

	Vector2 Steer()
	{
		Vector2 desired = Vector2.Zero;
		if (target != null)
		{
			Vector2 toTarget = target.GlobalPosition - self.GlobalPosition;
			float distance = toTarget.Length();
			Vector2 toward = distance > 0.01f ? toTarget / distance : Vector2.Right;
			// Close in or back off toward the preferred range, strafing across it.
			float rangeError = Mathf.Clamp((distance - U(PreferredRange)) / U(2.0f), -1.0f, 1.0f);
			if (channelling) rangeError = Mathf.Clamp((distance - U(2.0f)) / U(1.0f), -1.0f, 1.0f);
			if (Now >= nextStrafeFlip)
			{
				strafe = GD.Randf() < 0.5f ? new Vector2(-toward.Y, toward.X) : new Vector2(toward.Y, -toward.X);
				nextStrafeFlip = Now + GD.RandRange(0.9, 2.4);
			}
			desired = toward * rangeError + strafe * 0.8f;
		}

		ScanForIncomingShots();
		if (Now < dodgeUntil) desired = dodge;
		desired += StayInside();
		Vector2 move = desired.LimitLength(1.0f);

		// Walking into a wall or obstacle: go along it instead.
		if (move.LengthSquared() > 0.01f && Battle.IsInsideSolid(self.GlobalPosition + move * U(0.8f), U(0.3f)))
		{
			move = new Vector2(-move.Y, move.X);
			strafe = -strafe;
		}
		return AvoidHazard(move);
	}

	/// <summary>
	/// Keeps out of lava and bog: standing in it, head for the nearest way out; about to walk
	/// in, turn aside (or stop, if every way is bad).
	/// </summary>
	Vector2 AvoidHazard(Vector2 move)
	{
		StageHazard hazard = Battle.Hazard;
		if (hazard == null) return move;
		Vector2 feet = self.Feet;
		if (hazard.Contains(feet))
		{
			for (float reach = 0.6f; reach <= 4.0f; reach += 0.6f)
				for (int k = 0; k < 16; k++)
				{
					Vector2 direction = Vector2.FromAngle(k * Mathf.Tau / 16.0f);
					if (!hazard.Contains(feet + direction * U(reach))) return direction;
				}
			return move;
		}
		if (move.LengthSquared() < 0.01f || !hazard.Contains(feet + move.Normalized() * U(0.9f))) return move;
		foreach (float turn in new[] { 45.0f, -45.0f, 90.0f, -90.0f, 135.0f, -135.0f })
		{
			Vector2 turned = move.Rotated(Mathf.DegToRad(turn));
			if (!hazard.Contains(feet + turned.Normalized() * U(0.9f))) return turned;
		}
		return Vector2.Zero;
	}

	Vector2 StayInside()
	{
		Vector2 limit = Units.U(Battle.ArenaHalf.X - 1.4f, -(Battle.ArenaHalf.Y - 1.2f));
		Vector2 position = self.GlobalPosition;
		Vector2 push = Vector2.Zero;
		if (position.X > limit.X) push.X = -1; else if (position.X < -limit.X) push.X = 1;
		if (position.Y > limit.Y) push.Y = -1; else if (position.Y < -limit.Y) push.Y = 1;
		return push * 1.5f;
	}

	// A shot heading straight at the AI and about to arrive triggers a sidestep, some of the time.
	void ScanForIncomingShots()
	{
		if (Now < nextDodgeScan || Now < dodgeUntil) return;
		nextDodgeScan = Now + 0.12;
		foreach (Projectile shot in Battle.Projectiles)
		{
			if (!GodotObject.IsInstanceValid(shot) || !Combat.CanAffect(shot.Caster, shot.CasterTeam, self)) continue;
			Vector2 fromShot = self.GlobalPosition - shot.GlobalPosition;
			float distance = fromShot.Length();
			if (distance < U(0.4f) || distance > U(3.5f)) continue;
			Vector2 heading = Vector2.FromAngle(shot.Rotation);
			if (heading.Dot(fromShot / distance) < 0.93f) continue;
			if (GD.Randf() > DodgeChance) return;
			dodge = GD.Randf() < 0.5f ? new Vector2(-heading.Y, heading.X) : new Vector2(heading.Y, -heading.X);
			dodgeUntil = Now + 0.35;
			return;
		}
	}

	// ---- Abilities ----

	void Decide(ref InputState state)
	{
		float distance = Distance;
		string id = self.Data.Id;
		Ability second = self.Abilities[WizardData.SecondSlot], third = self.Abilities[WizardData.ThirdSlot];
		Ability fourth = self.Abilities[WizardData.FourthSlot];

		if (self.UltimateReady && UltimateInRange(id, distance))
		{
			aimPoint = target.GlobalPosition;
			state.Aim = AimToward(aimPoint);
			state.UltimatePressed = true;
			return;
		}

		if (fourth.IsReady && GD.Randf() < 0.5f * Eagerness && FourthWorthIt(id, distance, ref state)) return;
		if (third.IsReady && GD.Randf() < 0.5f * Eagerness && ThirdWorthIt(id, distance, ref state)) return;

		if (second.IsReady && distance < U(TargeterRange + 1.5f) && self.Energy > 45.0f && GD.Randf() < 0.45f * Eagerness)
		{
			aimPoint = target.GlobalPosition + targetVelocity * 0.5f;
			state.Aim = AimToward(aimPoint);
			state.SecondHeld = true;
			return;
		}

		// Don't waste shots into cover: wait for a clear line (strafing will find one).
		if (self.Abilities[WizardData.BasicSlot].IsReady && distance < U(10.0f) && Battle.HasLineOfSight(self.GlobalPosition, target.GlobalPosition))
		{
			aimPoint = LeadPoint();
			state.Aim = AimToward(aimPoint);
			state.BasicHeld = true;
		}
	}

	static bool UltimateInRange(string id, float distance) => id switch
	{
		"FrostWizard" => distance < U(6.5f),        // Absolute Zero reaches 8
		"EarthWizard" => distance < U(5.5f),        // Earthquake reaches 6.5
		"ElectricityWizard" => distance < U(5.0f),  // Overcharge sprays around the caster
		_ => true,                                   // Meteor Storm covers the arena
	};

	bool ThirdWorthIt(string id, float distance, ref InputState state)
	{
		switch (id)
		{
			case "FrostWizard":
				// Ice Wave sweeps out about seven units in a 70 degree arc.
				if (distance > U(6.0f) || self.Energy < 40.0f) return false;
				aimPoint = target.GlobalPosition;
				break;
			case "EarthWizard":
				// A rock wall between the AI and a nearby enemy blocks their shots.
				if (distance > U(5.0f) || self.Energy < 30.0f) return false;
				aimPoint = self.GlobalPosition.Lerp(target.GlobalPosition, 0.45f);
				break;
			case "ElectricityWizard":
				// Speed Up to escape when hurt, or to close a long gap.
				if (!(self.Health < Wizard.MaxHealth * 0.4f && distance < U(4.0f)) && distance < U(8.0f)) return false;
				if (self.Energy < 30.0f) return false;
				break;
			default:
				// Fire: breathe on anyone close enough, and keep the button held.
				if (distance > U(3.2f) || self.Energy < 30.0f) return false;
				aimPoint = target.GlobalPosition;
				channelling = true;
				state.ThirdHeld = true;
				break;
		}
		state.Aim = AimToward(aimPoint);
		state.ThirdPressed = true;
		return true;
	}

	// The fourth abilities: when each one is worth its 30-35 energy.
	bool FourthWorthIt(string id, float distance, ref InputState state)
	{
		if (self.Energy < 40.0f) return false;
		switch (id)
		{
			case "FireWizard":
				// Meteor Strike lands on the targeter after 1.2 s: lead the target a little.
				if (distance > U(TargeterRange + 1.0f)) return false;
				aimPoint = target.GlobalPosition + targetVelocity * 0.6f;
				break;
			case "FrostWizard":
				// Deep Freeze lands on the targeter after a short gather.
				if (distance > U(TargeterRange + 0.5f)) return false;
				aimPoint = target.GlobalPosition + targetVelocity * 0.3f;
				break;
			case "EarthWizard":
				// Tremor only reaches four units around the caster.
				if (distance > U(3.5f)) return false;
				aimPoint = target.GlobalPosition;
				break;
			default:
				// Chain Lightning finds its own targets within six units.
				if (distance > U(5.5f)) return false;
				aimPoint = target.GlobalPosition;
				break;
		}
		state.Aim = AimToward(aimPoint);
		state.FourthPressed = true;
		return true;
	}
}
