using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class ClassController : NetworkBehaviour
{

    public PlayerController PlayerController { get; private set; }
    public float Stamina { get { return PlayerController.player.CurStamina; } set { PlayerController.player.CurStamina = value; } }
    public abstract bool useable { get; }

    private bool _isSpcialMove = false;
    public bool isSpecialMove
    {
        get
        {
            return _isSpcialMove;
        }
        set
        {
            _isSpcialMove = value;
        }
    }

    public void InputHandler()
    {
        if (!isLocalPlayer)
            return;
        if (Input.GetMouseButtonDown(1) && !PlayerController.DisableSpecialMove && ((useable && !isSpecialMove) || isSpecialMove))
        {
            _isSpcialMove = !_isSpcialMove;
            if (_isSpcialMove)
            {
                if(PlayerController.gun != null)
                    GameManager.inst.playerController.playerCommandHub.CmdEnterSpecialMove(netId);
            }
            else
            {
                if (PlayerController.gun != null)
                    GameManager.inst.playerController.playerCommandHub.CmdExitSpecialMove(netId);;
            }
        }
    }

    void Awake()
    {
        PlayerController = GetComponent<PlayerController>();
    }

    protected void AddBuff(NetworkInstanceId id, params Buff[] buffs)
    {
        foreach (Buff buff in buffs)
        {
            PlayerController.playerCommandHub.CmdAddBuff(id, buff);
        }
    }

    protected void RemoveBuff(NetworkInstanceId id, params Buff[] buffs)
    {
        foreach (Buff buff in buffs)
        {
            PlayerController.playerCommandHub.CmdRemoveBuff(id, buff);
        }
    }

    public abstract void EnterSpecialMove(NetworkInstanceId id);
    public abstract void ExitSpecialMove(NetworkInstanceId id);
}

public class AssaultController : ClassController
{

    Coroutine currentUpdator;
    Buff speedBuff;
    Buff avoidBuff;

    public override bool useable
    {
        get
        {
            return Stamina >= 20f;
        }
    }

    public override void EnterSpecialMove(NetworkInstanceId id)
    {
         if (currentUpdator != null)
        {
            StopCoroutine(currentUpdator);
            currentUpdator = null; //코루틴이 이미 실행되어있으면
        }

        currentUpdator = StartCoroutine("SpecialMoveUpdator", id);
    }

    public override void ExitSpecialMove(NetworkInstanceId id)
    {
        if (currentUpdator != null)
        {
            StopCoroutine(currentUpdator);
            currentUpdator = null;
        }
        isSpecialMove = false;
        RemoveBuff(id, speedBuff, avoidBuff);
    }

    IEnumerator SpecialMoveUpdator(NetworkInstanceId id)
    {
        isSpecialMove = true;
        Stamina -= 20f;
        
        speedBuff = new Buff(Buff.Stat.Speed, 50f);
        avoidBuff = new Buff(Buff.Stat.AvoidRate, 10f);

        AddBuff(id, speedBuff, avoidBuff);

        while (true)
        {
            if (Stamina <= 0)
            {
                ExitSpecialMove(id);
                yield break;
            }
            Stamina -= 10f * Time.deltaTime;
            yield return null;
        }
    }

}

public class SniperController : ClassController
{
    Buff attackSpeedBuff;
    Buff damageBuff;
    Buff bulletSpreadBuff;
    Coroutine currentUpdator;

    public override bool useable
    {
        get
        {
            return Stamina >= 40f && !GameManager.inst.playerController.sm.classSkill.skills[0].isUsing;
        }
    }

    public override void EnterSpecialMove(NetworkInstanceId id)
    {
        GameManager.inst.cameraController.CameraSizeChange(NetworkServer.objects[id], 0.1f, 18);

        attackSpeedBuff = new Buff(Buff.Stat.AttackSpeed, -33);
        damageBuff = new Buff(Buff.Stat.Damage, 20);
        bulletSpreadBuff = new Buff(Buff.Stat.BulletSpreadAngle, -100);

        AddBuff(id, attackSpeedBuff, damageBuff, bulletSpreadBuff);

        if (currentUpdator != null)
        {
            StopCoroutine(currentUpdator);
            currentUpdator = null; //코루틴이 이미 실행되어있으면
        }

        PlayerController.DisableMove = true;
        currentUpdator = StartCoroutine("SpecialMoveUpdator", id);
    }

    public override void ExitSpecialMove(NetworkInstanceId id)
    {
        GameManager.inst.cameraController.CameraSizeChange(NetworkServer.objects[id], 0.1f, 10);

        RemoveBuff(id, attackSpeedBuff, damageBuff, bulletSpreadBuff);

        if (currentUpdator != null)
        {
            StopCoroutine(currentUpdator);
            currentUpdator = null;
        }
        PlayerController.DisalbeMouseLook = false;
        PlayerController.DisableMove = false;
        isSpecialMove = false;
    }

    IEnumerator SpecialMoveUpdator(NetworkInstanceId id)
    {
        isSpecialMove = true;
        Stamina -= 40f;
        while (true)
        {
            if(Stamina <= 0)
            {
                ExitSpecialMove(id);
                yield break;
            }

            Stamina -= 5f * Time.deltaTime;
            yield return null;
        }
    }

}

public class DoctorController : ClassController
{
    Coroutine currentUpdator;
    BioGas gas;
    public override bool useable
    {
        get { return Stamina > 0; }
    }
    public override void EnterSpecialMove(NetworkInstanceId id)
    {
        if (currentUpdator != null)
        {
            StopCoroutine(currentUpdator);
            currentUpdator = null; //코루틴이 이미 실행되어있으면
        }
        currentUpdator = StartCoroutine("SpecialMoveUpdator", id);
    }

    public override void ExitSpecialMove(NetworkInstanceId id)
    {
        if (currentUpdator != null)
        {
            StopCoroutine(currentUpdator);
            currentUpdator = null;
        }
        PlayerController.playerCommandHub.CmdActiveStaminaShield(id, false);
        isSpecialMove = false;
        PlayerController.DisableMove = false;
        NetworkServer.Destroy(gas.gameObject);
    }
    
    IEnumerator SpecialMoveUpdator(NetworkInstanceId id)
    {
        gas = Instantiate(Resources.Load<BioGas>("Prefabs/BioGas"), PlayerController.transform.position, Quaternion.identity);
        gas.Init(10f, id);
        NetworkServer.Spawn(gas.gameObject);
        isSpecialMove = true;
        PlayerController.DisableMove = true;
        while (true)
        {
            if (Stamina <= 0)
            {
                ExitSpecialMove(id);
                yield break;
            }

            gas.localScale = Vector3.ClampMagnitude(gas.localScale + Vector3.one * Time.deltaTime, 20f);
            Stamina -= 2.5f * Time.deltaTime;
            yield return null;
        }
    }
    
}

public class GuardController : ClassController
{

    Coroutine currentUpdator;
    Buff staminaShieldBuff;
    Buff speedBuff;

    public override bool useable
    {
        get
        {
            return Stamina > 0;
        }
    }

    public override void EnterSpecialMove(NetworkInstanceId id)
    {
        if (currentUpdator != null)
        {
            StopCoroutine(currentUpdator);
            currentUpdator = null; //코루틴이 이미 실행되어있으면
        }
        PlayerController.playerCommandHub.CmdActiveStaminaShield(id, true);
        currentUpdator = StartCoroutine("SpecialMoveUpdator", id);
    }

    public override void ExitSpecialMove(NetworkInstanceId id)
    {
        if (currentUpdator != null)
        {
            StopCoroutine(currentUpdator);
            currentUpdator = null;
        }
        PlayerController.playerCommandHub.CmdActiveStaminaShield(id, false);
        RemoveBuff(id, staminaShieldBuff, speedBuff);
        isSpecialMove = false;
    }

    IEnumerator SpecialMoveUpdator(NetworkInstanceId id)
    {
        isSpecialMove = true;
        staminaShieldBuff = new Buff(Buff.Stat.StaminaShield, 20f);
        speedBuff = new Buff(Buff.Stat.Speed, -20f);

        AddBuff(id, staminaShieldBuff, speedBuff);

        while (true)
        {
            if (Stamina <= 0)
            {
                ExitSpecialMove(id);
                yield break;
            }
            Stamina -= 2.5f * Time.deltaTime;
            yield return null;
        }
    }
}
