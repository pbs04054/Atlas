using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyManager : NetworkBehaviour
{

	public List<Enemy> enemies = new List<Enemy>();
	[SyncVar(hook = "HookEnemyCounter")] private int enemyCounter;
	public int EnemyCounter
	{
		get { return enemyCounter; }
		set
		{
			enemyCounter = value;
			GameManager.inst.inGameUIManager.UpdateEnemyRemain();
		}
	}

	public EnemySpawner[] spawners;
	public int waveCount = 2;
	[SyncVar(hook = "HookWaveTimer")] private float waveTimer = 0f;
	public float WaveTimer
	{
		get { return waveTimer; }
		set
		{
			waveTimer = value;
			GameManager.inst.inGameUIManager.UpdateWaveTime();
		}
	}

	private float waveTimerDelay;
	private WaveList _wl;

	public WaveList wl { get { return _wl; } set { _wl = value; } }

	[SerializeField] private GameObject[] enemyPrefabs;

	public const int FinalWave = 20;

	public override void OnStartServer()
	{
		spawners = FindObjectsOfType<EnemySpawner>();
		wl = JsonHelper.LoadWaveList();
		GameState.Transition(GameState.ready);
        GameState.wave.OnGameStateWaveEnter += SpawnWave;
		StartCoroutine("Updator");
	}

	public override void OnStartClient()
	{
		waveTimerDelay = Time.time;
	}

	IEnumerator Updator()
	{
		while (true)
		{
			GameState.curState.StateUpdate();
			yield return null;
		}
	}

	public void RemoveEnemy(Enemy enemy)
	{
		enemies.Remove(enemy);
        UpdateEnemyRemain();
        if (EnemyCounter == 0) // 모든 적을 제거함
		{
            if (waveCount == FinalWave)
            {
                GameState.end.Exit();
            }
            else
            {
                GameState.Transition(GameState.ready);
            }
		}
	}

	public void SpawnWave(object obj, EventArgs arg)
	{
		StartCoroutine(SpawningWave(waveCount++));
	}

	IEnumerator SpawningWave(int wave)
	{
		int spawnerCount = spawners.Length;
		WaveInfo info = wl.waves[wave];
		foreach (var wg in info.wave)
		{
			if (wg.id > enemyPrefabs.Length)
			{
				Debug.LogError("ID is out of EnemyPrefabArray");
				continue;
			}

			for (int i = 0; i < wg.amount; ++i)
			{
				GameObject obj = spawners[UnityEngine.Random.Range(0, spawnerCount)].Spawn(enemyPrefabs[wg.id]);
				obj.GetComponent<Enemy>().EnemyStart(wg.id, wg.multiple);
				NetworkServer.Spawn(obj);
				yield return new WaitForSeconds(0.1f);
			}
		}
	}

	public IEnumerator ClientTimer(float timer)
	{
		if (isServer) yield break;
		while (true)
		{
			if (timer <= 0)
				break;
			timer -= Time.deltaTime;
			WaveTimer = timer;
			yield return null;
		}
		WaveTimer = 0;
	}

	public void UpdateEnemyRemain()
	{
		EnemyCounter = enemies.Count;
	}

	void HookEnemyCounter(int remain)
	{
		EnemyCounter = remain;
	}

	void HookWaveTimer(float time)
	{
		if (isServer)
			WaveTimer = time;
		else
		{
			waveTimerDelay = Time.time - waveTimerDelay;
			time -= waveTimerDelay;
			waveTimerDelay = Time.time;

			if (Mathf.Abs(WaveTimer - time) < 5f)
				return;
			
			StopCoroutine("ClientTimer");
			StartCoroutine("ClientTimer", time);
			
		}

	}

}