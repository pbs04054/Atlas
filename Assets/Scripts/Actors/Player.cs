using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public enum PlayerClass
{
    ASSAULT,
    SNIPER,
    GUARD,
    DOCTOR
}

public class Player : Actor, IDamageable, IInteractor
{
    public Player player { get; private set; }
    PlayerController playerController;
    [SyncVar] [SerializeField] public PlayerClass playerClass;
    [SerializeField] GameObject hitEffect;
    [SerializeField] Transform radarHolder, enemyRadar, playerRadar;

    public Gun Gun
    {
        get
        {
            return playerController.gun;
        }
    }

    public Transform GunTransform { get { return transform.Find("Gun"); } }

    InGameUIManager inGameUIManager { get { return GameManager.inst.inGameUIManager; } }

    float rescueTimer;
    public float RescueTime = 5;

    public float RescueTimer
    {
        get { return rescueTimer; }
        set
        {
            rescueTimer = value;
            GameManager.inst.inGameUIManager.ToggleRescueBar(value > 0);
        }
    }

    public bool IsRadarActive { get; set; }
    public event EventHandler LevelUP;

    [SyncVar]
    public bool isDead = false;
    
    #region Stats

    [SerializeField] [SyncVar] float baseStamina;

    [SyncVar(hook = "SyncvarUpdateMaxStamina")]
    float maxStamina;

    [SyncVar(hook = "SyncvarUpdateCurStamina")]
    float curStamina;

    [SyncVar(hook ="SyncvarUpdateMoney")] float money;
    [SerializeField] [SyncVar] float baseHealth;

    [SyncVar(hook = "SyncvarUpdateMaxHealth")]
    float maxHealth;

    [SyncVar(hook = "SyncvarUpdateCurHealth")]
    float curHealth;

    [SyncVar] float minHealth;
    [SerializeField] [SyncVar] float baseStaminaPerSecond;
    [SyncVar(hook="SyncvarUpdateLevel")] int level = 0;
    [SyncVar] int abilityPoint = -1;
    [SyncVar(hook="SyuncvarUpdateCurExp")] int curExp;
    [SyncVar] int maxExp;
    [SyncVar] float staminaPerSecond;
    [SyncVar] float staminaShield;

    #region SyncVar Hook Method

    
    void SyncvarUpdateCurHealth(float value)
    {
        if (GameManager.inst.playerController == null || GameManager.inst.playerController.player == null) return;
        curHealth = value;
        inGameUIManager.UpdateHealth();
    }

    void SyncvarUpdateMaxHealth(float value)
    {
        if (GameManager.inst.playerController == null || GameManager.inst.playerController.player == null) return;
        maxHealth = value;
        inGameUIManager.UpdateHealth();
    }

    void SyncvarUpdateCurStamina(float value)
    {
        if (GameManager.inst.playerController == null || GameManager.inst.playerController.player == null) return;
        curStamina = value;
        inGameUIManager.UpdateStamina();
    }

    void SyncvarUpdateMaxStamina(float value)
    {
        if (GameManager.inst.playerController == null || GameManager.inst.playerController.player == null) return;
        maxStamina = value;
        inGameUIManager.UpdateStamina();
    }

    void SyncvarUpdateLevel(int value)
    {
        if (GameManager.inst.playerController == null || GameManager.inst.playerController.player == null) return;
        level = value;
        inGameUIManager.UpdateLevel();
        OnPlayerLevelUp();
    }

    void SyuncvarUpdateCurExp(int value)
    {
        if (GameManager.inst.playerController == null || GameManager.inst.playerController.player == null) return;
        curExp = value;
        inGameUIManager.UpdateExp();
    }

    void SyncvarUpdateMoney(float value)
    {
        if (GameManager.inst.playerController == null || GameManager.inst.playerController.player == null) return;
        money = value;
        inGameUIManager.UpdateMoney();
    }

    #endregion

    public Transform Transform { get { return transform; } }

    public int Level
    {
        get { return level; }
        set
        {
            level = value;
            //OnPlayerLevelUp();
        }
    }

    public int CurExp
    {
        get { return curExp; }
        set
        {
            curExp = value;
            inGameUIManager.UpdateExp();
            OnExperimentGained();
        }
    }

    public int MaxExp
    {
        get { return maxExp; }
        set
        {
            maxExp = value;
            inGameUIManager.UpdateExp();
        }
    }

    public float MaxHealth
    {
        get { return maxHealth; }
        private set
        {
            maxHealth = Mathf.Clamp(value, 0, float.MaxValue);
            inGameUIManager.UpdateHealth();
        }
    }

    public float CurHealth
    {
        get { return curHealth; }
        private set
        {
            curHealth = Mathf.Clamp(value, MinHealth, MaxHealth);
            inGameUIManager.UpdateHealth();
            GameManager.inst.shopManager.UpdateHospitalHealthBar();
        }
    }

    public float BaseHealth
    {
        get { return baseHealth; }
        set
        {
            baseHealth = value;
            CalculateStat();
            inGameUIManager.UpdateHealth();
        }
    }

    public float MinHealth { get { return minHealth; } set { minHealth = value; } }

    public bool IsAlive { get { return CurHealth > 0 && PlayerState.curState != PlayerState.dead; } }

    public float MaxStamina
    {
        get { return maxStamina; }
        private set
        {
            maxStamina = Mathf.Clamp(value, 0, float.MaxValue);
            inGameUIManager.UpdateStamina();
        }
    }

    public float CurStamina
    {
        get { return curStamina; }
        set
        {
            curStamina = Mathf.Clamp(value, 0, MaxStamina);
            inGameUIManager.UpdateStamina();
        }
    }

    public float BaseStamina
    {
        get { return baseStamina; }
        set
        {
            baseStamina = value;
            CalculateStat();
            inGameUIManager.UpdateStamina();
        }
    }

    public float StaminaPerSecond { get { return staminaPerSecond; } private set { staminaPerSecond = value; } }

    public float StaminaShield { get { return staminaShield; } private set { staminaShield = value; } }

    public float Money
    {
        get { return money; }
        set
        {
            money = Mathf.Clamp(value, 0, float.MaxValue);
            inGameUIManager.UpdateMoney();
        }
    }

    #endregion

    #region Debuffs

    Buff[] weaknessBuff;
    Buff[] fearBuff;

    public bool IsStun { get { return DebuffStack[(int) Debuff.DebuffTypes.Stun] != 0; } }

    public bool IsConfusion { get { return DebuffStack[(int) Debuff.DebuffTypes.Confusion] != 0; } }

    public float BleedingDamage { get { return DebuffStack[(int) Debuff.DebuffTypes.Bleeding] * 10f; } }

    public bool IsFracture { get { return DebuffStack[(int) Debuff.DebuffTypes.Fracture] != 0; } }

    public bool IsPoisoned { get { return DebuffStack[(int) Debuff.DebuffTypes.Poisoned] != 0; } }


    #endregion

    public override void Awake()
    {
        base.Awake();
        weaknessBuff = new Buff[2];
        fearBuff = new Buff[3];
        playerController = GetComponent<PlayerController>();
        player = GetComponent<Player>();
        radarHolder = transform.Find("Radar");
        IsRadarActive = true;
    }

    public void playerInitialize()
    {
        Start();
        Agent.updateRotation = false;
        Money = 1000;
        CurStamina = MaxStamina = baseStamina;
        CurHealth = MaxHealth = baseHealth;
        Level = 0;
        CalculateStat();
        CalculateDebuff();
        MaxExp = playerController.expInfo[0];
        curExp = 0;

        if (!isServer)
        {
            Debug.Log(FindObjectOfType<ServerSimulator>());
            GameManager.inst.serverSimulator.Init(this);
        }

        if (isLocalPlayer)
            inGameUIManager.InitializeSkillIcons(playerClass);
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            UpdateRadar();
            GameManager.inst.inGameUIManager.UpdateDebuffGrid(DebuffStack.ToArray());
        }

        if (!isServer) return;
        //출혈 디버프
        GetDamaged(BleedingDamage * Time.deltaTime);
        if (!IsFracture && !playerController.classController.isSpecialMove)
            CurStamina += StaminaPerSecond * Time.deltaTime;
    }

    void UpdateRadar()
    {
        foreach (Transform transforms in radarHolder)
        {
            transforms.gameObject.SetActive(false);
        }

        if (!IsRadarActive) return;
        
        Player[] players = FindObjectsOfType<Player>();
        foreach (Player player in players)
        {
            if (player == this.player) continue;
            Vector3 dir = player.transform.position - transform.position;
            Vector3 normalized = dir.normalized;
            Transform playerRadarTransform = CreateRadar(true);
            playerRadarTransform.gameObject.SetActive(true);
            playerRadarTransform.transform.position = radarHolder.position + normalized * 2;
            playerRadarTransform.rotation = Quaternion.LookRotation(dir);
        }
        
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            Vector3 dir = enemy.transform.position - transform.position;
            Vector3 normalized = dir.normalized;
            Transform enemyRadarTransform = CreateRadar(false);
            enemyRadarTransform.gameObject.SetActive(true);
            enemyRadarTransform.transform.position = radarHolder.position + normalized;
            enemyRadarTransform.rotation = Quaternion.LookRotation(dir);
        }
    }

    Transform CreateRadar(bool player)
    {
        Transform radar;
        if (player)
        {
            foreach (Transform pool in radarHolder)
                if (pool.name == "PlayerRadar" && !pool.gameObject.activeSelf)
                    return pool;
            radar = Instantiate(playerRadar, radarHolder);
            radar.name = "PlayerRadar";
        }
        else
        {
            foreach (Transform pool in radarHolder)
                if (pool.name == "EnemyRadar" && !pool.gameObject.activeSelf)
                    return pool;
            radar = Instantiate(enemyRadar, radarHolder);
            radar.name = "EnemyRadar";
        }
        return radar;
    }

    public void PlayerUpdate()
    {

        #region Debug
        
        //Buff buff = new Buff(Buff.Stat.Health, 100);

        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //    playerController.playerCommandHub.CmdAddBuff(netId, buff);
        
        //if(Input.GetKeyDown(KeyCode.Alpha2))
        //    playerController.playerCommandHub.CmdRemoveBuff(netId, buff);

        //상태이상 테스트용
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    AddDebuff(new Debuff(Debuff.DebuffTypes.Weakness, 3f));
        //}

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Actor[] actors = FindObjectsOfType<Actor>();
            foreach (Actor actor in actors)
            {
                GameManager.inst.playerController.playerCommandHub.CmdAddDebuff(actor.netId, Debuff.DebuffTypes.Bleeding, 5f);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Actor[] actors = FindObjectsOfType<Actor>();
            foreach (Actor actor in actors)
            {
                GameManager.inst.playerController.playerCommandHub.CmdAddBuff(actor.netId, new Buff(Buff.Stat.Speed, 100));
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Actor[] actors = FindObjectsOfType<Actor>();
            foreach (Actor actor in actors)
            {
                GameManager.inst.playerController.playerCommandHub.CmdRemoveBuff(actor.netId, new Buff(Buff.Stat.Speed, 100));
            }
        }

        //if (Input.GetKeyDown(KeyCode.Alpha3))
        //{
        //    AddDebuff(new Debuff(Debuff.DebuffTypes.Fracture, 3f));
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha4))
        //{
        //    AddDebuff(new Debuff(Debuff.DebuffTypes.Confusion, 3f));
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha5))
        //{
        //    AddDebuff(new Debuff(Debuff.DebuffTypes.Bleeding, 3f));
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha6))
        //{
        //    RemoveAllDebuffs();
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha0))
        //{
        //    AttackArea.Create(transform.position, Vector3.zero, 3, 5, null);
        //}

        //레벨업 테스트용
        if (Input.GetKeyDown(KeyCode.U))
        {
            ++Level;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            CurExp += 5;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("L");
            playerController.playerCommandHub.CmdGivePlayerDamage(netId, 3000);
        }

        #endregion
    }

    public virtual void GetDamaged(float amount)
    {
        if (Mathf.Abs(amount) < 1f * Time.deltaTime)
            return;
        
        if (IsPoisoned)
            amount *= 1.5f;

        if (StaminaShield > 0)
        {
            float protectedDamage = amount * StaminaShield;
            CurHealth -= protectedDamage;
            CurStamina -= protectedDamage;
        }
        else
        {
            CurHealth -= amount;
        }

        if (CurHealth <= 0 && PlayerState.curState != PlayerState.fatal)
        {
            GameManager.inst.serverSimulator.TargetTransitionToFatal(NetworkServer.objects[netId].connectionToClient);
        }
    }

    public virtual void GetDamaged(float amount, NetworkInstanceId playerID, Quaternion rotation)
    {
        GetDamaged(amount);
        GameManager.inst.serverSimulator.RpcShowHitEffect(playerID, rotation);
    }

    public void ShowHitEffect(Quaternion rotation)
    {
        Instantiate(hitEffect, transform.position, rotation);
    }

    public void ShowHitVignette()
    {
        inGameUIManager.HitVignette();
    }
    
    public void GetHealed(float amount, bool forceHeal)
    {
        if (!isServer)
            return;
        if (CurHealth <= 0 && !forceHeal)
            return;
        Debug.Log(name + " Healed " + amount.ToString());
        player.CurHealth += amount;
    }

    public void GetStaminaHealed(float amount, bool forceHeal)
    {
        if (!isServer)
            return;
        if (CurStamina <= 0 && !forceHeal)
            return;
        player.CurStamina += amount;
    }


    public override Buff[] AddBuff(params Buff[] buffs)
    {
        if (Gun != null)
            Gun.AddBuff(buffs);
        base.AddBuff(buffs);
        CalculateStat();
        return buffs;
    }

    public override void RemoveBuff(params Buff[] buffs)
    {
        CalculateStat();
        if (Gun != null)
            Gun.RemoveBuff(buffs);
        base.RemoveBuff(buffs);
    }

    public void CalculateStat()
    {
        float health = 0;
        float staminaPerSecond = 0;
        float staminaShield = 0;
        float stamina = 0;
        float speed = 0;

        foreach (Buff buff in Buffs)
        {
            health += buff.GetBuff(Buff.Stat.Health);
            staminaPerSecond += buff.GetBuff(Buff.Stat.StaminaPerSecond);
            staminaShield += buff.GetBuff(Buff.Stat.StaminaShield);
            speed += buff.GetBuff(Buff.Stat.Speed);
        }

        float preMaxHealth = MaxHealth;
        MaxHealth = baseHealth * (1 + health * 0.01f);
        CurHealth += baseHealth * (1 + health * 0.01f) - preMaxHealth;
        StaminaPerSecond = baseStaminaPerSecond * (1 + staminaPerSecond * 0.01f);
        StaminaShield = staminaShield * 0.01f;
        MaxStamina = baseStamina * (1 + stamina * 0.01f);
        Speed = baseSpeed * (1 + speed * 0.01f);
    }

    public override void OnDebuffStart(DebuffComponent debuff)
    {
        base.OnDebuffStart(debuff);
        CalculateDebuff();
    }

    public override void OnDebuffEnd(DebuffComponent debuff)
    {
        base.OnDebuffEnd(debuff);
        CalculateDebuff();
    }

    void CalculateDebuff()
    {
        //쇠약
        RemoveBuff(weaknessBuff);
        weaknessBuff[0] = new Buff(Buff.Stat.Speed,
            -1 * Mathf.Clamp(10f * DebuffStack[(int) Debuff.DebuffTypes.Weakness], 0, 99));
        weaknessBuff[1] = new Buff(Buff.Stat.AttackSpeed, 10f * DebuffStack[(int) Debuff.DebuffTypes.Weakness]);
        AddBuff(weaknessBuff);

        //공포
        RemoveBuff(fearBuff);
        fearBuff[0] = new Buff(Buff.Stat.Speed, -1 * Mathf.Clamp(10f * DebuffStack[(int) Debuff.DebuffTypes.Fear], 0, 99));
        fearBuff[1] = new Buff(Buff.Stat.Damage, -1 * Mathf.Clamp(10f * DebuffStack[(int) Debuff.DebuffTypes.Fear], 0, 99));
        fearBuff[2] = new Buff(Buff.Stat.AvoidRate, -1 * Mathf.Clamp(10f * DebuffStack[(int) Debuff.DebuffTypes.Fear], 0, 99));
        AddBuff(fearBuff);

        //다른 디버프는 상단 Debuffs Region 확인
    }

    public void OnPlayerLevelUp()
    {
        if (level == 0)
        {
            inGameUIManager.UpdateLevel();
            return;
        }
        Debug.Log(LevelUP);
        if (LevelUP != null)
        {
            LevelUP(this, EventArgs.Empty);
        }

        inGameUIManager.UpdateLevel();
        StatUpWhenLevelUp();
    }

    public void StatUpWhenLevelUp()
    {
        BaseHealth *= Mathf.Pow(2, 0.1f);
        Speed *= Mathf.Pow(2, 0.01f);
        BaseStamina *= Mathf.Pow(2, 0.1f);
        baseStaminaPerSecond *= Mathf.Pow(2, 0.1f);
        CalculateStat();
    }

    public void OnExperimentGained()
    {
        if (MaxExp <= CurExp)
        {
            while (MaxExp <= CurExp)
            {
                ++Level;
                curExp -= MaxExp;
                if (playerController.expInfo.Count <= level)
                    maxExp = int.MaxValue;
                else
                    MaxExp = playerController.expInfo[Level];
            }
        }

        inGameUIManager.UpdateExp();
    }

    #region Interactor

    public void Interact()
    {
        if (CurHealth <= 0)
        {
            GameManager.inst.playerController.player.CmdRescue(GameManager.inst.playerController.GetComponent<NetworkIdentity>().netId, GetComponent<NetworkIdentity>().netId);
        }
    }

    [Command]
    public void CmdRescue(NetworkInstanceId self, NetworkInstanceId target)
    {
        ServerSimulator serverSimulator = FindObjectOfType<ServerSimulator>();
        serverSimulator.TargetStartRescue(NetworkServer.objects[self].connectionToClient, target);
        serverSimulator.TargetStartRescued(NetworkServer.objects[target].connectionToClient, self);
    }

    [Command]
    public void CmdStopRescue(NetworkInstanceId self, NetworkInstanceId target)
    {
        ServerSimulator serverSimulator = FindObjectOfType<ServerSimulator>();
        serverSimulator.TargetStopRescue(NetworkServer.objects[self].connectionToClient, target);
        serverSimulator.TargetStopRescued(NetworkServer.objects[target].connectionToClient, self);
    }

    public void HighLighting()
    {

    }

    public void DeHighLighting()
    {

    }

    #endregion
}