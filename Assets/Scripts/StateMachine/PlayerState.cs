using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerState
{
    public static PlayerState curState = null;

    public static PlayerController pc { get { return GameManager.inst.playerController; } }
    public static PlayerStateIdle idle = new PlayerStateIdle();
    public static PlayerStateRoll roll = new PlayerStateRoll();
    public static PlayerStateWait wait = new PlayerStateWait();
    public static PlayerStateFatal fatal = new PlayerStateFatal();
    public static PlayerStateRescue rescue = new PlayerStateRescue();
    public static PlayerStateDead dead = new PlayerStateDead();

    public virtual void Enter() { }
    public virtual void Exit(PlayerState nextState) { }
    public virtual void StateUpdate() { }

    public static void Transition(PlayerState nextState)
    {
        if (curState != null)
        {
            Debug.Log(curState.ToString() + "->" + nextState.ToString());
            curState.Exit(nextState);
        }
        curState = nextState;
        nextState.Enter();
    }
}

public class PlayerStateIdle : PlayerState
{
    public override void Enter()
    {

    }
    public override void Exit(PlayerState nextState)
    {
        
    }
    public override void StateUpdate()
    {
        pc.PlayerMovementControl();
        pc.GunControl();
        pc.InteractControl();
        pc.SKillControl();
        pc.OtherControl();

        pc.classController.InputHandler();
    }
}

public class PlayerStateRoll : PlayerState
{
    public override void Enter()
    {

    }
}

public class PlayerStateWait : PlayerState //상점, 옵션 창등을 열었을때의 State
{
    public override void Enter()
    {
        pc.player.Agent.isStopped = true;
        pc.player.Agent.velocity = Vector3.zero;
    }

    public override void Exit(PlayerState nextState)
    {
        pc.player.Agent.isStopped = false;
    }

}

public class PlayerStateFatal : PlayerState
{
    private float timer;
    private float rescueTimer;

    public float RescueTimer
    {
        get { return rescueTimer; }
        set
        {
            rescueTimer = value;
            pc.player.RescueTimer = value;
        }
    }

    Buff fatalSpeedDebuff = new Buff(Buff.Stat.Speed, -90);

    public override void Enter()
    {
        timer = 20f;
        //스피드 감소
        pc.playerCommandHub.CmdAddBuff(pc.netId, fatalSpeedDebuff);
        GameManager.inst.inGameUIManager.FatalVignette(true);

        GameManager.inst.inGameUIManager.GetComponent<Canvas>().enabled = true;
        GameManager.inst.shopManager.GetComponent<Canvas>().enabled = false;

        pc.playerCommandHub.CmdPlayerFatal();
    }

    public override void StateUpdate()
    {
        RescueTimer -= Time.deltaTime;
        timer -= Time.deltaTime;
        GameManager.inst.inGameUIManager.UpdateRescueTime();
        //움직임만 가능
        pc.PlayerMovementControl();
        pc.OtherControl();

        if (pc.player.CurHealth > 0)
        {
            Transition(idle);
        }
        if (timer < 0)
        {
            Transition(dead);
        }
    }

    public override void Exit(PlayerState nextState)
    {
        RescueTimer = 0;
        //속도 복구
        pc.playerCommandHub.CmdRemoveBuff(pc.netId, fatalSpeedDebuff);
        GameManager.inst.inGameUIManager.FatalVignette(false);
    }
}

public class PlayerStateRescue : PlayerState
{
    
    private float rescueTimer;
    public float RescueTimer { get { return rescueTimer; } set { rescueTimer = value;
        pc.player.RescueTimer = value;
    } }
    private Player _player;
    public Player player { get { return _player; } set { _player = value; } }
    public override void Enter()
    {

    }

    public override void StateUpdate()
    {
        GameManager.inst.inGameUIManager.UpdateRescueTime();
        if (Input.anyKeyDown)
        {   
            pc.player.CmdStopRescue(pc.GetComponent<NetworkIdentity>().netId, player.GetComponent<NetworkIdentity>().netId);
            return;
        }
        RescueTimer -= Time.deltaTime;
        if (RescueTimer < 0)
        {
            pc.CmdRescuePlayer(player.GetComponent<NetworkIdentity>().netId);
            Transition(idle);
        }
    }

    public override void Exit(PlayerState nextState)
    {
        RescueTimer = 0;
    }
}

public class PlayerStateDead : PlayerState
{
    public override void Enter()
    {
        pc.playerCommandHub.CmdPlayerDead(pc.netId);
    }

    public override void StateUpdate()
    {
        if (pc.player.CurHealth > 0)
        {
            Transition(idle);
        }
    }
}