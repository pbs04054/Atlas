using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameState : IStateMachine {

    public static GameState curState;
    public static GameStateReady ready = new GameStateReady();
    public static GameStateWave wave = new GameStateWave();
    public static GameStateEnd end = new GameStateEnd();
    public static GameStateOver over = new GameStateOver();

	public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void StateUpdate() { }

    private float _timer;
    public float timer { get { return _timer; } set { _timer = value; GameManager.inst.enemyManager.WaveTimer = value; } }

    public static void Transition(GameState nextState)
    {
        if (curState != null)
            curState.Exit();
        Debug.Log(nextState);
        curState = nextState;
        nextState.Enter();
    }
}

public class GameStateWave : GameState
{
    public event EventHandler OnGameStateWaveEnter;

    public override void Enter()
    {
        if (OnGameStateWaveEnter != null)
        {
            OnGameStateWaveEnter(this, EventArgs.Empty);
        }
        if (GameManager.inst.enemyManager.waveCount == EnemyManager.FinalWave)
        {
            Transition(end);
            return;
        }
        timer = GameManager.inst.enemyManager.wl.waves[GameManager.inst.enemyManager.waveCount].waveInterval;
        //MonoBehaviour.FindObjectOfType<ServerSimulator>().RpcStartTimer(timer);
    }

    public override void Exit()
    {
        foreach ( var player in GameManager.inst.players)
        {
            player.Value.CurExp += (int)timer * 5;
        }
        //남은 timer의 시간에 따라 경험치, 보상 지급
    }

    public override void StateUpdate()
    {
        timer -= Time.deltaTime;
        if (timer < 0)
        {
            Transition(wave);
        }
        if (GameManager.inst.enemyManager.enemies.Count == 0)
        {
            Transition(ready);
        }
    }
}

public class GameStateReady : GameState
{
    public event EventHandler OnGameStateReadyEnter;

    public override void Enter()
    {
        timer = 30.0f;
        GameManager.inst.enemyManager.UpdateEnemyRemain();
        //MonoBehaviour.FindObjectOfType<ServerSimulator>().RpcStartTimer(timer);
    }

    public override void Exit()
    {

    }

    public override void StateUpdate()
    {
        timer -= Time.deltaTime;
        if (timer < 0)
        {
            Transition(wave);
        }
        
        //Debug
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Transition(wave);
        }
    }
}

public class GameStateEnd : GameState //마지막 Wave가 모두 스폰된 후, 모든 적을 죽일때 까지의 State
{
    public override void Enter()
    {
        Debug.Log("GameStateEnd Enter");
    }

    public override void Exit()
    {
        GameManager.inst.Coroutine(GameClearCoroutine());
    }

    public override void StateUpdate()
    {

    }

    private IEnumerator GameClearCoroutine()
    {
        yield return new WaitForSeconds(5);
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }
}

public class GameStateOver : GameState
{
    public override void Enter()
    {
        GameOver();
    }

    private void GameOver()
    {
        GameManager.inst.Coroutine(GameOverCoroutine());
    }

    private IEnumerator GameOverCoroutine()
    {
        yield return new WaitForSeconds(5);
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }
}