﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DroneSpawnAbility", menuName = "ShellCore/Drone Spawn Data", order = 8)]
public class DroneSpawnData : ScriptableObject
{
    //public Sprite abilitySprite;
    public int abilitySpriteID;
    public EntityBlueprint drone;
    public float energyCost;
    public float delay;
    public float cooldown;
    public string abilityName;
    public string description;
}
