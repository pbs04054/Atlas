using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour {

    public ClassSkill classSkill;
    public PerkInfo[] perkInfoList;
    public Dictionary<Perk, bool> perks = new Dictionary<Perk, bool>();

	// Use this for initialization
	public void SkillManagerInit () {
        switch (GameManager.inst.playerController.player.playerClass)
        {
            case PlayerClass.ASSAULT:
                classSkill = new AssaultSkill();
                perkInfoList = JsonUtility.FromJson<ClassPerkList>(Resources.Load<TextAsset>("AssaultPerk").text).perkList;
                break;
            case PlayerClass.SNIPER:
                classSkill = new SniperSkill();
                perkInfoList = JsonUtility.FromJson<ClassPerkList>(Resources.Load<TextAsset>("SniperPerk").text).perkList;
                break;
            case PlayerClass.GUARD:
                classSkill = new GuardSkill();
                perkInfoList = JsonUtility.FromJson<ClassPerkList>(Resources.Load<TextAsset>("GuardPerk").text).perkList;
                break;
            case PlayerClass.DOCTOR:
                classSkill = new DoctorSkill();
                perkInfoList = JsonUtility.FromJson<ClassPerkList>(Resources.Load<TextAsset>("DoctorPerk").text).perkList;
                break;
        }
        for (Perk i = Perk.F1_1; i <= Perk.U1_2; ++i)
        {
            perks.Add(i, false);
        }
        classSkill.ClassSkillInitial();
        GameManager.inst.playerController.player.LevelUP += CheckPerkAvailableOnLevelUP;
	}
	
	// Update is called once per frame
	public void SkillManagerUpdate ()
    {
        if (classSkill == null) return;
		foreach(var skill in classSkill.skills)
        {
            if (skill != null)
            {
                skill.SkillTimerUpdate();
            }
        }
	}

    public void CheckPerkAvailableOnLevelUP(object obj, EventArgs arg)
    {
        PlayerController pc = GameManager.inst.playerController;
        Player player = pc.player;
        if (player.Level == 5)
        {
            pc.PerkAvailable.Enqueue(Perk.F1_1); //1차 스킬 1번째 퍽 F1_1 & F1_2
        }
        if (player.Level == 7)
        {
            pc.PerkAvailable.Enqueue(Perk.F2_1);
        }
        if (player.Level == 10)
        {
            pc.PerkAvailable.Enqueue(Perk.S1_1);
        }
        if (player.Level == 12)
        {
            pc.PerkAvailable.Enqueue(Perk.F3_1);
        }
        if (player.Level == 15)
        {
            pc.PerkAvailable.Enqueue(Perk.T1_1);
        }
        if (player.Level == 18)
        {
            pc.PerkAvailable.Enqueue(Perk.S2_1);
        }
        if (player.Level == 20)
        {
            pc.PerkAvailable.Enqueue(Perk.T2_1);
        }
        if (player.Level == 23)
        {
            pc.PerkAvailable.Enqueue(Perk.U1_1);
        }
        GameManager.inst.inGameUIManager.UpdatePerk();
    }
}
