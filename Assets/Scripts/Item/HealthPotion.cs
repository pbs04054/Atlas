using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPotion : IItem {

    private float healAmount;

    public HealthPotion(float healAmount)
    {
        this.healAmount = healAmount;
    }

    public void Use()
    {
        //GameManager.inst.playerController.playerCommandHub.CmdGiveHealToPlayer(GameManager.inst.playerController.netId, healAmount);
    }
}
