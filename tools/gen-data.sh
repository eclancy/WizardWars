#!/usr/bin/env bash
# Regenerates data/abilities, data/wizards and data/stages from the Unity numbers below.
#
# The .tres files are the source of truth once they exist and can be tuned in the Godot
# inspector; this script is the record of where every starting value came from (the Unity
# scripts and docs), and a quick way to rebuild them. Re-running it overwrites any tuning
# done in the editor, so only run it on purpose.
set -eu
cd "$(dirname "$0")/.."
mkdir -p data/abilities data/wizards data/stages

ability() {
	local id=$1 sound=$2
	shift 2
	local f=data/abilities/$id.tres steps=2
	if [ -n "$sound" ]; then steps=3; fi
	{
		echo "[gd_resource type=\"Resource\" script_class=\"AbilityData\" load_steps=$steps format=3]"
		echo
		echo "[ext_resource type=\"Script\" path=\"res://scripts/AbilityData.cs\" id=\"1_script\"]"
		if [ -n "$sound" ]; then echo "[ext_resource type=\"AudioStream\" path=\"res://assets/sfx/$sound.wav\" id=\"2_sound\"]"; fi
		echo
		echo "[resource]"
		echo "script = ExtResource(\"1_script\")"
		echo "Id = \"$id\""
		local kv
		for kv in "$@"; do echo "$kv"; done
		if [ -n "$sound" ]; then echo "Sound = ExtResource(\"2_sound\")"; fi
	} > "$f"
}

# --- Fire (FireWizard.cs, Fireball.cs, FireSource.cs, FlameThrower.cs) ---
ability fireball fireball 'DisplayName = "Fireball"' 'Description = "A fast bolt of fire. Free to cast."' 'Cooldown = 0.55' 'Damage = 20.0' 'Speed = 9.0' 'Duration = 2.0'
ability ring-of-fire ringoffire 'DisplayName = "Ring of Fire"' 'Description = "Twelve flames burst up in a ring at the target. Anyone touching one catches fire."' 'Cost = 35.0' 'Cooldown = 4.0' 'Radius = 2.6' 'Count = 12' 'Duration = 4.0' 'Duration2 = 6.0' 'Damage2 = 3.0'
ability flamethrower flamethrower 'DisplayName = "Flamethrower"' 'Description = "Hold to breathe fire. Drains energy while held and slows you a little."' 'Cost = 10.0' 'Drain = 10.0' 'SlowPercent = 15.0' 'Duration2 = 1.5' 'Range = 2.5' 'Damage = 3.0' 'Interval = 1.0' 'Duration = 2.0' 'Damage2 = 1.0'
ability meteor-storm fire 'DisplayName = "Meteor Storm"' 'Description = "Meteors rain across the whole arena. They never hit you or your team."' 'Count = 10' 'Interval = 0.45' 'Range = 9.0' 'Radius = 3.0' 'Damage = 28.0' 'Damage2 = 5.0' 'Duration = 0.9'

# --- Frost (FrostWizard.cs, Icicle.cs, IcePatch.cs, IceWaveShard.cs) ---
ability icicle icicle 'DisplayName = "Icicle"' 'Description = "A fast shard of ice. Free to cast."' 'Cooldown = 0.55' 'Damage = 20.0' 'Speed = 9.0' 'Duration = 2.0'
ability ice-patch icepatch 'DisplayName = "Ice Patch"' 'Description = "Freezes the ground at the target. Anyone moving on it slides and cannot steer."' 'Cost = 30.0' 'Cooldown = 4.83' 'Duration = 2.0' 'Radius = 0.95'
ability ice-wave icewave 'DisplayName = "Ice Wave"' 'Description = "A fan of ice sweeps outward and freezes anyone it touches solid."' 'Cost = 35.0' 'Cooldown = 4.0' 'Count = 9' 'Range = 7.0' 'Duration = 0.55' 'Duration2 = 1.5' 'Damage = 8.0' 'Radius = 0.4'
ability absolute-zero icewave 'DisplayName = "Absolute Zero"' 'Description = "Enemies near you are slowed and their energy stops recharging."' 'Radius = 8.0' 'SlowPercent = 35.0' 'Duration = 5.0'

# --- Earth (EarthWizard.cs, Mudball.cs, MudPatch.cs, RockWall.cs) ---
ability mudball mudball 'DisplayName = "Mudball"' 'Description = "A heavy ball of mud. Free to cast."' 'Cooldown = 0.55' 'Damage = 20.0' 'Speed = 9.0' 'Duration = 2.0'
ability mud-patch mudpuddle 'DisplayName = "Mud Patch"' 'Description = "Turns the ground to mud. Anyone in it is slowed, and fire goes out."' 'Cost = 30.0' 'Cooldown = 4.75' 'Duration = 3.0' 'SlowPercent = 50.0' 'Radius = 1.5'
ability rock-wall rockwall 'DisplayName = "Rock Wall"' 'Description = "Raises a wall that blocks movement and stops projectiles."' 'Cost = 20.0' 'Cooldown = 4.0' 'Duration = 3.0'
ability earthquake rockwall 'DisplayName = "Earthquake"' 'Description = "The ground shakes around you, slowing enemies for as long as they stay close."' 'Radius = 6.5' 'SlowPercent = 55.0' 'Duration = 5.0' 'Duration2 = 2.0' 'Interval = 0.6'

# --- Electricity (ElectricityWizard.cs, ElectricityBall.cs, LightningStaff.cs) ---
ability electricity-ball electricityball 'DisplayName = "Electricity Ball"' 'Description = "A crackling ball of lightning. Free to cast."' 'Cooldown = 0.55' 'Damage = 20.0' 'Speed = 9.0' 'Duration = 2.0'
ability lightning-rod rodlightning 'DisplayName = "Lightning Rod"' 'Description = "Plants a rod that shocks everyone nearby, you included, every half second."' 'Cost = 30.0' 'Cooldown = 3.5' 'Duration = 6.0' 'Interval = 0.5' 'Radius = 1.8' 'Damage = 3.0'
ability speed-up speedup 'DisplayName = "Speed Up"' 'Description = "Run half again as fast for a few seconds."' 'Cost = 25.0' 'Cooldown = 2.5' 'Duration = 2.5' 'SlowPercent = 50.0'
ability overcharge rodlightning 'DisplayName = "Overcharge"' 'Description = "Sprays lightning in every direction and scatters short-lived rods around you."' 'Duration = 4.0' 'Interval = 0.3' 'Range = 5.0' 'Duration2 = 2.5'

# --- Fourth abilities (LB), restored 2026-10-15 ---
# Unity cut these on 2026-09-25 (commit ae8e3bc): they cost 80-85 energy and shared the third
# ability's cooldown flag. The original numbers are in the comments; the values here bring them
# in line with the 20-35 energy abilities.
# Meteor Strike (Unity: 80 energy, 1.2 s marker, 35 damage + 4 s burn at 2/tick within 3 units,
# then a ring of fire at the impact; ready again 5 s after landing).
ability meteor-strike fire 'DisplayName = "Meteor Strike"' 'Description = "Calls one meteor down on the target. It burns everyone it hits and leaves the ground on fire."' 'Cost = 35.0' 'Cooldown = 6.2' 'Duration = 1.2' 'Radius = 3.0' 'Damage = 35.0' 'Damage2 = 35.0' 'Duration2 = 4.0' 'BurnPerTick = 2.0' 'Count = 6'
# Deep Freeze (Unity: 85 energy, instant 4 s stun within 2 units of the target, ready after 6 s).
ability deep-freeze icepatch 'DisplayName = "Deep Freeze"' 'Description = "Frost gathers on the target, then freezes everyone caught there solid."' 'Cost = 35.0' 'Cooldown = 6.0' 'Duration = 0.6' 'Radius = 2.0' 'Duration2 = 2.0' 'Damage = 10.0'
# Tremor - the Unity fourth-slot Earthquake, renamed because Earthquake is the ultimate now
# (Unity: 85 energy, a 0.15 s stagger to everyone within 12 units, no damage, ready after 4 s).
ability tremor rockwall 'DisplayName = "Tremor"' 'Description = "Stamp the ground: enemies close to you are hurt and staggered."' 'Cost = 30.0' 'Cooldown = 5.0' 'Radius = 4.0' 'Damage = 12.0' 'Duration2 = 0.75'
# Chain Lightning (Unity: 85 energy, 3 damage to the three nearest players at any range, 0.1 s
# between hops, ready after 4.3 s).
ability chain-lightning rodlightning 'DisplayName = "Chain Lightning"' 'Description = "Lightning leaps from enemy to enemy, up to three of them."' 'Cost = 35.0' 'Cooldown = 4.5' 'Range = 6.0' 'Radius = 4.0' 'Count = 3' 'Interval = 0.1' 'Damage = 18.0'

wizard() {
	local file=$1 id=$2 name=$3 art=$4 targeter=$5 colour=$6 a1=$7 a2=$8 a3=$9 a4=${10} a5=${11}
	{
		echo "[gd_resource type=\"Resource\" script_class=\"WizardData\" load_steps=9 format=3]"
		echo
		echo "[ext_resource type=\"Script\" path=\"res://scripts/WizardData.cs\" id=\"1_script\"]"
		echo "[ext_resource type=\"Texture2D\" path=\"res://assets/art/wizards/$art.png\" id=\"2_portrait\"]"
		echo "[ext_resource type=\"Texture2D\" path=\"res://assets/art/ui/targeter-$targeter.png\" id=\"3_targeter\"]"
		echo "[ext_resource type=\"Resource\" path=\"res://data/abilities/$a1.tres\" id=\"4_basic\"]"
		echo "[ext_resource type=\"Resource\" path=\"res://data/abilities/$a2.tres\" id=\"5_second\"]"
		echo "[ext_resource type=\"Resource\" path=\"res://data/abilities/$a3.tres\" id=\"6_third\"]"
		echo "[ext_resource type=\"Resource\" path=\"res://data/abilities/$a4.tres\" id=\"7_fourth\"]"
		echo "[ext_resource type=\"Resource\" path=\"res://data/abilities/$a5.tres\" id=\"8_ultimate\"]"
		echo
		echo "[resource]"
		echo "script = ExtResource(\"1_script\")"
		echo "Id = \"$id\""
		echo "DisplayName = \"$name\""
		echo "Portrait = ExtResource(\"2_portrait\")"
		echo "Targeter = ExtResource(\"3_targeter\")"
		echo "Colour = Color($colour)"
		echo "Basic = ExtResource(\"4_basic\")"
		echo "Second = ExtResource(\"5_second\")"
		echo "Third = ExtResource(\"6_third\")"
		echo "Fourth = ExtResource(\"7_fourth\")"
		echo "Ultimate = ExtResource(\"8_ultimate\")"
	} > data/wizards/$file.tres
}

wizard fire FireWizard "Fire Wizard" fire red "0.95, 0.4, 0.15, 1" fireball ring-of-fire flamethrower meteor-strike meteor-storm
wizard frost FrostWizard "Frost Wizard" frost blue "0.45, 0.75, 1, 1" icicle ice-patch ice-wave deep-freeze absolute-zero
wizard earth EarthWizard "Earth Wizard" earth green "0.6, 0.45, 0.25, 1" mudball mud-patch rock-wall tremor earthquake
wizard electricity ElectricityWizard "Electricity Wizard" electricity yellow "0.72, 0.5, 1, 1" electricity-ball lightning-rod speed-up chain-lightning overcharge

# Spawns from ParkBattle.ConfigureStageVariant (Unity units, +y up). Unity also scattered small
# rocks and cacti that looked solid but blocked nothing; they are gone. The cover (kind and
# positions) is new in the Godot version: each stage has its own solid pieces to hide behind,
# placed in balanced pairs so no spawn is favoured. Woodland's stumps sit on open grass, clear
# of its path (its floor art is hand-drawn; the other floors are plain ground, see StageArtTool).
stage() {
	local file=$1 name=$2 spawns=$3 computer=$4 coverkind=$5 cover=$6
	{
		echo "[gd_resource type=\"Resource\" script_class=\"StageData\" load_steps=3 format=3]"
		echo
		echo "[ext_resource type=\"Script\" path=\"res://scripts/StageData.cs\" id=\"1_script\"]"
		echo "[ext_resource type=\"Texture2D\" path=\"res://assets/art/stages/$file.png\" id=\"2_background\"]"
		echo
		echo "[resource]"
		echo "script = ExtResource(\"1_script\")"
		echo "Id = \"$file\""
		echo "DisplayName = \"$name\""
		echo "Background = ExtResource(\"2_background\")"
		echo "Spawns = PackedVector2Array($spawns)"
		echo "ComputerSpawn = $computer"
		echo "CoverKind = \"$coverkind\""
		echo "Cover = PackedVector2Array($cover)"
	} > data/stages/$file.tres
}

stage woodland "Woodland" "-7, 4, 7, 4, -7, -4, 7, -4" 3 stump "1.9, 2.4, -1.9, -2.4, -5.4, 0.8, 5, -0.8"
stage frost-tundra "Frost Tundra" "-8, 0, 8, 0, 0, 5, 0, -5" 1 crystal "-4, 0, 4, 0, 0, 2.2, 0, -2.2"
stage scorched-canyon "Scorched Canyon" "-7, 0, 7, 0, 0, 5, 0, -5" 1 saguaro "-4, -1.6, 4, 1.6, -1.6, 1.8, 1.6, -1.8"
stage storm-plateau "Storm Plateau" "-8, 4.5, 8, 4.5, -8, -4.5, 8, -4.5" 3 obelisk "-2.5, 1.6, 2.5, 1.6, -2.5, -1.6, 2.5, -1.6"

echo "Generated $(ls data/abilities | wc -l) abilities, $(ls data/wizards | wc -l) wizards, $(ls data/stages | wc -l) stages."
