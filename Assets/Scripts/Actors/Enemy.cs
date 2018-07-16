using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class Enemy : Actor, IDamageable
{

    #region Stats

    public float energy;
    public int exp;
    [SerializeField]
    float baseHealth;
    [SerializeField]
    float baseAttackSpeed; // ex) 2 : 2초에 한번씩 공격
    [SerializeField]
    float baseDamage;
    public Transform Transform { get { return transform; } }
    public float Damage { get; private set; }
    public float AttackSpeed { get; private set; }
    public float MaxHealth { get { return maxHealth; } private set { maxHealth = value; } }
    public float CurHealth { get { return curHealth; } private set { curHealth = value; } }
    public bool IsAlive { get { return CurHealth > 0; } }
    public float Multiple { get; private set; }

    bool initHPBar;
    bool isDead;
    
    protected bool CanAttack { get { return IsAlive && !IsStun; } }
    protected bool CanMove { get { return IsAlive && !IsStun; } }
    
    #endregion

    #region Debuffs

    Buff[] weaknessBuff;
    Buff[] fearBuff;
    public bool IsStun { get { return DebuffStack[(int)Debuff.DebuffTypes.Stun] != 0; } }
    public bool IsConfusion { get { return DebuffStack[(int)Debuff.DebuffTypes.Confusion] != 0; } }
    public float BleedingDamage { get { return DebuffStack[(int)Debuff.DebuffTypes.Bleeding] * 10f; } }
    public bool IsFracture { get { return DebuffStack[(int)Debuff.DebuffTypes.Fracture] != 0; } }
    public bool IsPoisoned { get { return DebuffStack[(int)Debuff.DebuffTypes.Poisoned] != 0; } }

    
    #endregion
    
    #region Network

    [SyncVar] Vector3 syncPos = default(Vector3);
    [SyncVar] Quaternion syncRot = default(Quaternion);
    [SyncVar] float curHealth;
    [SyncVar] float maxHealth;
    float lerpRate = 5f;
    int networkSendRate = 9;

    NetworkInstanceId lastHit = NetworkInstanceId.Invalid; //마지막으로 공격한 플레이어

    void FixedUpdate()
    {
        if (isServer)
            return;
        LerpPosition();
        CheckHP();
    }

    void LerpPosition()
    {
        if (syncPos == default(Vector3) || syncRot == default(Quaternion)) return;
        transform.position = Vector3.Lerp(transform.position, syncPos, Time.deltaTime * lerpRate);
        transform.rotation = Quaternion.Lerp(transform.rotation, syncRot, Time.deltaTime * lerpRate);
    }

    void CheckHP()
    {
        if (!initHPBar && curHealth < maxHealth)
        {
            HPBar.Create(this);
            initHPBar = true;
        }

        if (!isDead && curHealth <= 0)
            StartCoroutine("DeadUpdator");
    }

    IEnumerator SyncUpdator()
    {
        while (true)
        {
            RpcSendTransform(transform.position, transform.rotation);
            yield return new WaitForSeconds(1 / (float)networkSendRate);
        }
    }

    IEnumerator NetworkSendRateUpdator()
    {
        while (true)
        {
            foreach (Player player in GameManager.inst.players.Values)
            {
                if (Vector3.Distance(transform.position, player.transform.position) <= 40)
                {
                    networkSendRate = 9;
                    break;
                }
                networkSendRate = 1;

            }
            yield return null;
        }
    }

    [ClientRpc]
    void RpcSendTransform(Vector3 position, Quaternion rotation)
    {
        if (isServer) return;
        syncPos = position;
        syncRot = rotation;
    }

    [ClientRpc]
    public void RpcGiveExp()
    {
        GameManager.inst.playerController.player.CurExp += exp;
    }

    #endregion

    public Quaternion LastBulletRotation { get; private set; }

    public event EventHandler EnemyDeadEvent;

    public override void Awake()
    {
        base.Awake();
        weaknessBuff = new Buff[2];
        fearBuff = new Buff[3];
        initHPBar = false;
    }

    public override void OnStartServer()
    {
        Start();
        StartCoroutine("SyncUpdator");
        StartCoroutine("NetworkSendRateUpdator");
    }

	protected virtual void Update ()
	{
	    if (!isServer && isClient)
	        return;
	   
        Agent.speed = Speed;
        GetDamaged(BleedingDamage * Time.deltaTime);
    }

    public virtual void EnemyStart(int id, float multiple)
    {
        Multiple = multiple;
        CalculateMultiple(id);
        CalculateBuff();
        CalculateDebuff();
        CurHealth = MaxHealth;
        GameManager.inst.enemyManager.enemies.Add(this);
        GameManager.inst.enemyManager.UpdateEnemyRemain();
        transform.localScale = new Vector3(1 + 0.01f * multiple, 1 + 0.01f * multiple, 1 + 0.01f * multiple);
    }

    public virtual void GetDamaged(float amount)
    {
        if (!IsAlive || Mathf.Abs(amount) < 0.01f * Time.deltaTime)
        {
            return;
        }
        
        if (!initHPBar)
        {
            HPBar.Create(this);
            initHPBar = true;
        }

        if (IsPoisoned)
            amount *= 1.5f;

        CurHealth -= amount;
        CurHealth = Mathf.Clamp(CurHealth, 0, MaxHealth);

        if (CurHealth <= 0)
        {
            EnemyDead();
        }
        else
        {
            SoundManager.inst.PlaySFX(gameObject, Resources.LoadAll<AudioClip>("Sounds/EnemyHit").Random());
        }
    }

    public virtual void GetDamaged(IBullet bullet)
    {
        if (bullet.ID != GameManager.inst.playerController.netId && !isServer)
        {
            LastBulletRotation = ClientScene.FindLocalObject(bullet.ID).transform.rotation;
            return;
        }
        if (isServer)
        {
            LastBulletRotation = bullet.Transform.rotation;
            lastHit = bullet.ID;
            GetDamaged(bullet.Damage);
        }
        else
        {
            if (curHealth - bullet.Damage <= 0 && !isDead)
            {
                LastBulletRotation = ClientScene.FindLocalObject(bullet.ID).transform.rotation;
                StartCoroutine("DeadUpdator");
            }
            GameManager.inst.playerController.playerCommandHub.CmdGiveDamageWithoutCollide(GameManager.inst.playerController.netId, netId, bullet.Damage);
        }
    }

    public virtual void GetDamaged(float damage, NetworkInstanceId playerID)
    {
        if (playerID != GameManager.inst.playerController.netId && !isServer)
        {
            LastBulletRotation = ClientScene.FindLocalObject(playerID).transform.rotation;
            return;
        }
        if (isServer)
        {
            lastHit = playerID;
            LastBulletRotation = ClientScene.FindLocalObject(playerID).transform.rotation;
            GetDamaged(damage);
        }
        else
        {
            if (curHealth - damage <= 0)
            {
                LastBulletRotation = ClientScene.FindLocalObject(playerID).transform.rotation;
                StartCoroutine(DeadUpdator());
            }
            GameManager.inst.playerController.playerCommandHub.CmdGiveDamageWithoutCollide(GameManager.inst.playerController.netId, netId, damage);
        }
    }

    public void MoveToLocation(Vector3 targetPoint)
    {
        if (!IsAlive || IsStun)
            return;
        if (IsConfusion)
            Agent.destination = transform.position + new Vector3(Random.insideUnitSphere.x, transform.position.y, Random.insideUnitSphere.y) * 5f;
        else
            Agent.destination = targetPoint;
        Agent.isStopped = false;
    }

    public virtual void EnemyDead()
    {
        isDead = true;
        StopAllCoroutines();
        GameManager.inst.enemyManager.RemoveEnemy(this);

        if (EnemyDeadEvent != null)
        {
            EnemyDeadEvent(this, EventArgs.Empty);
        }

        if (lastHit != NetworkInstanceId.Invalid && NetworkServer.FindLocalObject(lastHit) != null && isServer)
        {
            GameObject playerObject = NetworkServer.FindLocalObject(lastHit);
            playerObject.GetComponent<PlayerController>().OnKillEnemy();
            playerObject.GetComponent<Player>().Money += energy;
        }
        
        foreach (Player player in GameManager.inst.players.Values)
        {
            player.CurExp += exp;
        }

        StartCoroutine("DeadUpdator");
    }

    IEnumerator DeadUpdator()
    {
        isDead = true;
        SoundManager.inst.PlaySFX(gameObject, Resources.LoadAll<AudioClip>("Sounds/Enemy").Random());
        Agent.velocity = Vector3.zero;
        Agent.isStopped = true;

        GetComponent<Collider>().enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Ragdoll");
        MeshRenderer originMeshRenderer = GetComponentInChildren<MeshRenderer>();
        originMeshRenderer.enabled = false;

        /*
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer meshRenderer in renderers)
        {
            meshRenderer.material.shader = Shader.Find("Atlas/Full");
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.gameObject.layer = LayerMask.NameToLayer("Ragdoll");
            meshRenderer.transform.SetParent(null);
            meshRenderer.transform.rotation = LastBulletRotation;
            meshRenderer.gameObject.AddComponent<Rigidbody>().AddForce(-transform.forward * 500f);
            meshRenderer.gameObject.AddComponent<BoxCollider>();
            meshRenderer.enabled = false;
            StartCoroutine(meshRenderer.gameObject.AddComponent<TriangleExplosion>().SplitMesh());
        }
        */
        MeshRenderer[] renderers = new MeshRenderer[2];
        for(int i = 0; i < 2; i++)
        {
            GameObject ragdoll = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ragdoll.transform.position = transform.position;
            MeshRenderer renderer = ragdoll.GetComponent<MeshRenderer>();

            renderer.material = originMeshRenderer.material;
            renderer.material.shader = Shader.Find("Atlas/Full");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.gameObject.layer = LayerMask.NameToLayer("Ragdoll");
            renderer.transform.SetParent(null);
            renderer.transform.rotation = LastBulletRotation;
            renderer.gameObject.AddComponent<Rigidbody>().AddForce(-transform.forward * 500f);
            renderer.gameObject.AddComponent<BoxCollider>();
            renderer.enabled = false;


            StartCoroutine(ragdoll.AddComponent<TriangleExplosion>().SplitMesh());
            renderers[i] = renderer;
        }



        float timer = 0;
        while (true)
        {
            if (timer >= 3f)
                break;

            foreach (MeshRenderer meshRenderer in renderers)
            {
                meshRenderer.material.SetFloat("_Cut", Mathf.Lerp(0.3f, 1, timer/3f));
            }
            
            timer += Time.deltaTime;
            yield return null;
        }

        foreach (MeshRenderer meshRenderer in renderers)
        {
            Destroy(meshRenderer.gameObject);
        }
        Destroy(gameObject);
    }

    public override Buff[] AddBuff(params Buff[] buffs)
    {
        CalculateBuff();
        return base.AddBuff(buffs);
    }

    void CalculateMultiple(int id)
    {
        switch (id)
        {
            case 0:
                AddBuff(new Buff(Buff.Stat.Health_Sum, 10 * Multiple), new Buff(Buff.Stat.Damage_Sum, 2 * Multiple), new Buff(Buff.Stat.Speed_Sum, 0.1f * Multiple));
                break;
            case 1:
                AddBuff(new Buff(Buff.Stat.Health_Sum, 4 * Multiple), new Buff(Buff.Stat.Damage_Sum, 4 * Multiple), new Buff(Buff.Stat.Speed_Sum, 0.5f * Multiple));
                break;
            case 2:
                AddBuff(new Buff(Buff.Stat.Health_Sum, 10 * Multiple), new Buff(Buff.Stat.Damage_Sum, 5 * Multiple));
                break;
            case 3:
                AddBuff(new Buff(Buff.Stat.Health_Sum, 5 * Multiple), new Buff(Buff.Stat.Damage_Sum, 4 * Multiple));
                break;
            case 4:
                AddBuff(new Buff(Buff.Stat.Health_Sum, 50 * Multiple), new Buff(Buff.Stat.Damage_Sum, 1 * Multiple), new Buff(Buff.Stat.ProjectileRadius_Sum, 0.1f * Multiple));
                break;
        }
    }

    protected virtual void CalculateBuff()
    {
        float health = 0;
        float damage = 0;
        float attackSpeed = 0;

        float healthSum = 0;
        float damageSum = 0;
        float speedSum = 0;
        
        foreach (Buff buff in Buffs)
        {
            health += buff.GetBuff(Buff.Stat.Health);
            damage += buff.GetBuff(Buff.Stat.Damage);
            attackSpeed += buff.GetBuff(Buff.Stat.AttackSpeed);
            healthSum += buff.GetBuff(Buff.Stat.Health_Sum);
            damageSum += buff.GetBuff(Buff.Stat.Damage_Sum);
            speedSum += buff.GetBuff(Buff.Stat.Speed_Sum);
        }
        
        MaxHealth = baseHealth * (1 + health * 0.01f) + healthSum;
        Damage = baseDamage * (1 + damage * 0.01f) + damageSum;
        AttackSpeed = baseAttackSpeed * (1 + attackSpeed * 0.01f) + speedSum;
    }

    public override void OnDebuffStart(DebuffComponent debuff)
    {
        base.OnDebuffStart(debuff);
        CalculateBuff();
    }

    public override void OnDebuffEnd(DebuffComponent debuff)
    {
        base.OnDebuffEnd(debuff);
        CalculateBuff();
    }

    void CalculateDebuff()
    {
        //쇠약
        RemoveBuff(weaknessBuff);
        weaknessBuff[0] = new Buff(Buff.Stat.Speed, -1 * Mathf.Clamp(10f * DebuffStack[(int)Debuff.DebuffTypes.Weakness], 0, 99));
        weaknessBuff[1] = new Buff(Buff.Stat.AttackSpeed, 10f * DebuffStack[(int)Debuff.DebuffTypes.Weakness]);
        AddBuff(weaknessBuff);

        //공포
        RemoveBuff(fearBuff);
        fearBuff[0] = new Buff(Buff.Stat.Speed, -1 * Mathf.Clamp(10f * DebuffStack[(int)Debuff.DebuffTypes.Fear], 0, 99));
        fearBuff[1] = new Buff(Buff.Stat.Damage, -1 * Mathf.Clamp(10f * DebuffStack[(int)Debuff.DebuffTypes.Fear], 0, 99));
        fearBuff[2] = new Buff(Buff.Stat.AvoidRate, -1 * Mathf.Clamp(10f * DebuffStack[(int)Debuff.DebuffTypes.Fear], 0, 99));
        AddBuff(fearBuff);
    }

    protected void ProjectileAttack(string projectileName, float damage, float speed, NetworkInstanceId id)
    {
        EnemyProjectile enemyProjectile = Instantiate(Resources.Load<EnemyProjectile>("Prefabs/EnemyProjectile/"+projectileName), transform.position, transform.rotation);
        NetworkServer.Spawn(enemyProjectile.gameObject);
        enemyProjectile.Init(damage, speed);
    }
    
    protected void ProjectileAttack(EnemyProjectile projectile, float damage, float speed, NetworkInstanceId id)
    {
        EnemyProjectile enemyProjectile = Instantiate(projectile, transform.position, transform.rotation);
        NetworkServer.Spawn(enemyProjectile.gameObject);
        enemyProjectile.Init(damage, speed);
    }

    protected IEnumerator LookAtTarget(Transform target, float time)
    {
        float timer = 0;
        while (true)
        {
            if (timer >= time)
                break;
            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
            timer += Time.deltaTime;
            yield return null;
        }
    }
    
}