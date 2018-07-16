using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AI;

public enum Perk
{
    F1_1, //1차 스킬 1차 퍽
    F1_2,
    F2_1,
    F2_2,
    F3_1,
    F3_2,
    S1_1, //2차 스킬
    S1_2,
    S2_1,
    S2_2,
    T1_1, //3차 스킬
    T1_2,
    T2_1,
    T2_2,
    U1_1, //4차 스킬
    U1_2
}

public abstract class Skill {

    public float Timer { get { return timer; } set { timer = value; } }
    public float coolTime, timer;
    public virtual bool useable { get { return Timer <= 0 && !GameManager.inst.playerController.sm.classSkill.CheckSkillUsage() && !isLocked; } }
    public bool isLocked
    {
        get
        {
            switch(Array.IndexOf(GameManager.inst.playerController.sm.classSkill.skills, this))
            {
                case 0:
                    return GameManager.inst.playerController.player.Level < 0;
                case 1:
                    return GameManager.inst.playerController.player.Level < 0;
                case 2:
                    return GameManager.inst.playerController.player.Level < 0;
                case 3:
                    return GameManager.inst.playerController.player.Level < 0;
                default:
                    return true;
            }
        }
    }
    public bool isUsing = false;
    protected PlayerController pc { get { return GameManager.inst.playerController; } }
    protected SkillManager sm { get { return pc.sm; } }
    //Act function will be called when skill is used.
    //Needs to be overrided
    public abstract void Init();
    public abstract void Use();
    public abstract void SkillTimerUpdate();
}

public abstract class ClassSkill
{
    public Skill[] skills = new Skill[4];
    public virtual void ClassSkillInitial()
    {
        //GameManager.inst.playerController.player.LevelUP += CheckPerkAvailableOnLevelUP;
    }
    public virtual bool CheckSkillUsage()
    {
        foreach (var skill in skills)
        {
            if (skill == null)
                continue;
            if (skill.isUsing)
                return true;
        }
        return false;
    }
}