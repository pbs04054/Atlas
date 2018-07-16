using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AssaultSkill : ClassSkill
{
    public override void ClassSkillInitial()
    {
        skills[0] = new KnifeThrowSkill();
        skills[1] = new KnockBackSkill();
        skills[2] = new ShellingSKill();
        skills[3] = new BerserkSkill();
        foreach (var skill in skills)
        {
            if (skill != null)
                skill.Init();
        }
    }

    public override bool CheckSkillUsage()
    {
        for (int i = 0; i < 3; ++i)
        {
            if (skills[i] == null)
                continue;
            if (skills[i].isUsing)
                return true;
        }
        return false;
    }
}

public class KnifeThrowSkill : Skill
{
    const float oriCoolTime = 5.0f;
    const float oriDamage = 50;
    float curDamage { get { return oriDamage * (1 + 0.05f * stack); } }
    int _stock = 0;
    public int stock { get { return _stock; } set { _stock = value;  GameManager.inst.inGameUIManager.UpdateSkillStockText(0, stock); } }
    int _stack = 0;
    public int stack { get { return _stack; } set { _stack = value; OnStackChanged(); } }
    public override bool useable
    {
        get
        {
            return base.useable || (sm.perks[Perk.F2_1] && stock > 0 && !GameManager.inst.playerController.sm.classSkill.CheckSkillUsage() && !isLocked);
        }
    }


    public override void Init()
    {
        coolTime = oriCoolTime;
        Timer = 0.0f;
    }

    public override void SkillTimerUpdate()
    {
        if (Timer > 0)
            Timer -= Time.deltaTime;
        if (sm.perks[Perk.F2_1] && Timer <= 0 && stock < 4)
        {
            ++stock;
            if (stock < 4)
                Timer = coolTime;
        }
    }

    public override void Use()
    {
        if (useable)
        {
            //GameManager.inst.Coroutine(Using());
            Using();
        }
    }

    void Using()
    {
        Vector3 playerPos = GameManager.inst.playerController.transform.position;
        Plane plane = new Plane(Vector3.up, playerPos);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float dist;
        plane.Raycast(ray, out dist);
        Vector3 point = ray.GetPoint(dist);

        point = point - playerPos;
        point *= 100f;

        Knife knifeRef = Resources.Load<Knife>("Prefabs/Knife");

        float n = sm.perks[Perk.F2_1] ? 0.75f : 1;
        if (sm.perks[Perk.F1_1]) //남용 퍽
        {
            //로컬에서 쏘는것들
            Knife knife1 = UnityEngine.Object.Instantiate(knifeRef, playerPos + new Vector3(0, 0.5f, 0), pc.transform.rotation);
            knife1.transform.Rotate(new Vector3(-90, pc.transform.rotation.y + 5, 0));
            knife1.Init(curDamage * n, Quaternion.Euler(0, 15, 0) * point.normalized * 2000, sm.perks[Perk.F1_2], sm.perks[Perk.F2_2], sm.perks[Perk.F3_1], pc.netId);

            Knife knife2 = UnityEngine.Object.Instantiate(knifeRef, playerPos + new Vector3(0, 0.5f, 0), pc.transform.rotation);
            knife2.transform.Rotate(new Vector3(-90, pc.transform.rotation.y, 0));
            knife2.Init(curDamage * n, point.normalized * 2000, sm.perks[Perk.F1_2], sm.perks[Perk.F2_2], sm.perks[Perk.F3_1], pc.netId);

            Knife knife3 = UnityEngine.Object.Instantiate(knifeRef, playerPos + new Vector3(0, 0.5f, 0), pc.transform.rotation);
            knife3.transform.Rotate(new Vector3(-90, pc.transform.rotation.y - 5, 0));
            knife3.Init(curDamage * n, Quaternion.Euler(0, -15, 0) * point.normalized * 2000, sm.perks[Perk.F1_2], sm.perks[Perk.F2_2], sm.perks[Perk.F3_1], pc.netId);

            if (sm.perks[Perk.F3_2])
            {
                knife1.KillEnemy += OnKillEnemyWithKnife;
                knife2.KillEnemy += OnKillEnemyWithKnife;
                knife3.KillEnemy += OnKillEnemyWithKnife;
            }

            //동기화 시작
            pc.playerCommandHub.CmdKnifeThrowSkill(pc.netId, Quaternion.Euler(0, 5, 0) * point.normalized * 2000, sm.perks[Perk.F1_2], sm.perks[Perk.F2_2], sm.perks[Perk.F3_1]);
            pc.playerCommandHub.CmdKnifeThrowSkill(pc.netId, point.normalized * 2000, sm.perks[Perk.F1_2], sm.perks[Perk.F2_2], sm.perks[Perk.F3_1]);
            pc.playerCommandHub.CmdKnifeThrowSkill(pc.netId, Quaternion.Euler(0, -5, 0) * point.normalized * 2000, sm.perks[Perk.F1_2], sm.perks[Perk.F2_2], sm.perks[Perk.F3_1]);

        }
        else
        {
            //로컬에서 쏘는거
            Knife knife = UnityEngine.Object.Instantiate(knifeRef, playerPos + new Vector3(0, 0.5f, 0), pc.transform.rotation);
            knife.transform.Rotate(new Vector3(-90, pc.transform.rotation.y, 0));
            knife.Init(curDamage * n, point.normalized * 2000, sm.perks[Perk.F1_2], sm.perks[Perk.F2_2], sm.perks[Perk.F3_1], pc.netId);
            if (sm.perks[Perk.F3_2])
            {
                knife.KillEnemy += OnKillEnemyWithKnife;
            }

            //동기화 시작
            pc.playerCommandHub.CmdKnifeThrowSkill(pc.netId, point.normalized * 2000, sm.perks[Perk.F1_2], sm.perks[Perk.F2_2], sm.perks[Perk.F3_1]);
        }
        SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, Resources.Load<AudioClip>("Sounds/ThrowKnife"));

        if (!sm.perks[Perk.F2_1])
        {
            Timer = sm.perks[Perk.F2_2] ? coolTime * 1.5f : coolTime;
        }
        else
        {
            --stock;
            if (Timer <= 0)
            {
                Timer = coolTime;
            }
        }
    }

    void OnStackChanged()
    {
        coolTime = (1 - 0.01f * stack) * oriCoolTime;
    }

    void OnKillEnemyWithKnife(object sender, EventArgs e)
    {
        stack++;
        Debug.Log("StackUp");
    }
}

public class KnockBackSkill : Skill
{
    private float angle = 90;
    private float radius = 10;
    private float damage = 30;
    const float oriCoolTime = 15;

    public override void Init()
    {
        coolTime = oriCoolTime;
        Timer = 0;
    }

    public override void SkillTimerUpdate()
    {
        if (Timer > 0)
        {
            Timer -= Time.deltaTime;
        }
    }

    public override void Use()
    {
        if (useable)
        {
            GameManager.inst.Coroutine(Using());
        }
    }

    private IEnumerator Using()
    {
        isUsing = true;

        pc.DisableShot = true;
        angle = sm.perks[Perk.S1_1] ? 60 : sm.perks[Perk.S2_2] ? 120 : 90;
        radius = sm.perks[Perk.S1_1] ? 20 : 15;
        coolTime = sm.perks[Perk.S1_1] ? oriCoolTime / 2 : oriCoolTime;

        GameObject obj = new GameObject();
        Mesh mesh = new Mesh();
        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/ChargingEffect");
        mesh.DrawArc(obj.transform, radius, angle);
        SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, Resources.Load<AudioClip>("Sounds/Skill/AirboomReady"));

        while (true)
        {
            Vector3 playerPos = pc.transform.position;
            obj.transform.position = playerPos + new Vector3(0, 0.01f);
            obj.transform.rotation = pc.transform.rotation;

            if (Input.GetMouseButtonDown(0))
            {
                List<Enemy> enemies = new List<Enemy>();
                for (float theta = (90 - (angle / 2)) * Mathf.PI / 180; theta <= (90 + (angle / 2)) * Mathf.PI / 180; theta += 0.025f)
                {
                    float x = radius * Mathf.Cos(theta);
                    float z = radius * Mathf.Sin(theta);
                    Vector3 dir = pc.transform.rotation * new Vector3(x, 0, z);

                    foreach (var hit in Physics.RaycastAll(new Ray(playerPos, dir), radius))
                    {
                        Enemy enemy = hit.collider.GetComponent<Enemy>();
                        if (enemy != null && !enemies.Contains(enemy))
                        {
                            enemies.Add(enemy);
                        }
                    }
                }
                foreach (var enemy in enemies)
                {
                    enemy.GetDamaged(damage, pc.netId);
                    Vector3 dir = enemy.transform.position - playerPos;
                    pc.playerCommandHub.CmdKnockBackEnemy(enemy.GetComponent<NetworkIdentity>().netId, dir.normalized * (radius - dir.magnitude));
                    if (sm.perks[Perk.S2_1]) //싸늘하다 퍽
                    {
                        pc.playerCommandHub.CmdAddDebuff(enemy.netId, Debuff.DebuffTypes.Bleeding, 5);
                        pc.playerCommandHub.CmdAddDebuff(enemy.netId, Debuff.DebuffTypes.Fracture, 5); //테스트 안해봄
//                      enemy.AddDebuff(new Debuff(Debuff.DebuffTypes.Bleeding, 5));
//                      enemy.AddDebuff(new Debuff(Debuff.DebuffTypes.Fracture, 5));
                    }
                }
                SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, Resources.Load<AudioClip>("Sounds/Skill/Airboom"));

                if (sm.perks[Perk.S2_2]) //몰이사냥 퍽
                    coolTime -= 0.01f * enemies.Count;
                Timer = coolTime;
                break;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                break;
            }
            yield return null;
        }

        GameObject.Destroy(obj);
        pc.EnableShot(0.5f);
        isUsing = false;
    }
}

public class ShellingSKill : Skill
{
    private int _stock = 0;
    public int stock { get { return _stock; } set { _stock = value; GameManager.inst.inGameUIManager.UpdateSkillStockText(3, _stock); } }
    private int stack = 0;
    private float radius = 5;
    private float damageConst = 1;
    const float damage = 100;

    public override bool useable
    {
        get
        {
            return base.useable || (sm.perks[Perk.T2_2] && stock > 0 && !GameManager.inst.playerController.sm.classSkill.CheckSkillUsage() && !isLocked);
        }
    }

    public override void Init()
    {
        coolTime = 40;
    }

    public override void SkillTimerUpdate()
    {
        if (Timer > 0)
            Timer -= Time.deltaTime;
        if (sm.perks[Perk.T2_2] && Timer <= 0 && stock < 4)
        {
            ++stock;
            if (stock < 4)
                Timer = coolTime;
        }
    }

    public override void Use()
    {
        GameManager.inst.Coroutine(Using());
    }

    IEnumerator Using()
    {
        isUsing = true;
        GameManager.inst.inGameUIManager.DisableCrossHair();

        radius = sm.perks[Perk.T2_1] ? 7.5f : 5;

        GameObject obj = new GameObject();
        Mesh mesh = new Mesh();
        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/ChargingEffect");
        mesh.DrawCircle(radius, 100);

        while (true)
        {
            Plane plane = new Plane(Vector3.up, pc.transform.position);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float distance;
            plane.Raycast(ray, out distance);
            Vector3 point = ray.GetPoint(distance);
            obj.transform.position = point + new Vector3(0, 0.01f, 0);

            if (Input.GetMouseButtonDown(0))
            {

                if (sm.perks[Perk.T2_2])
                {
                    GameManager.inst.Coroutine(Shelling(point));
                    if (--stock <= 0)
                    {
                        break;
                    }
                    if (Timer < 0)
                    {
                        Timer = coolTime;
                    }
                }
                else
                {
                    GameManager.inst.Coroutine(Shelling(point));
                    Timer = coolTime;
                    break;
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                break;
            }

            yield return null;
        }
        GameObject.Destroy(obj);
        GameManager.inst.inGameUIManager.EnableCrossHair();
        isUsing = false;
    }

    IEnumerator Shelling(Vector3 position)
    {        
        int kill = 0;
        if (sm.perks[Perk.T2_1])
        {
            Dictionary<Enemy, int> enemyHitCount = new Dictionary<Enemy, int>();
            for (int i = 0; i < 4; ++i)
            {
                //로컬에서 쏘는거
                AttackArea.CreateCircle(position + new Vector3(0, 0.01f), radius, 1);
                
                //동기화
                pc.playerCommandHub.CmdAttackAreaCircle(pc.netId, position + Vector3.up * 0.01f, radius, 1);
                
                yield return new WaitForSeconds(1);
                foreach (var col in Physics.OverlapSphere(position, radius))
                {
                    Enemy enemy = col.GetComponent<Enemy>();
                    if (enemy == null)
                        continue;
                    if (enemyHitCount.ContainsKey(enemy))
                    {
                        int temp = enemyHitCount[enemy] + 1;
                        enemyHitCount.Remove(enemy);
                        enemyHitCount.Add(enemy, temp);
                    }
                    else
                    {
                        enemyHitCount.Add(enemy, 0);
                    }

                    if (enemy != null)
                    {
                        enemy.GetDamaged(damage * damageConst * (0.5f + enemyHitCount[enemy] * 0.25f), pc.netId);
                        if (!enemy.IsAlive)
                        {
                            ++kill;
                        }
                    }
                }
                GameObject.Instantiate(Resources.Load<GameObject>("Effects/ExplosionEffect_Big"),position, Quaternion.identity);
                pc.playerCommandHub.CmdEffect(pc.netId, position, "ExplosionEffect_Big", 0);
                SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, Resources.Load<AudioClip>("Sounds/headshot"));
            }
        }
        else
        {
            //로컬에서 쏘는거
            AttackArea.CreateCircle(position + new Vector3(0, 0.01f), radius, 1);
            
            //동기화
            pc.playerCommandHub.CmdAttackAreaCircle(pc.netId, position + Vector3.up * 0.01f, radius, 1);
            
            yield return new WaitForSeconds(1);
            foreach (var col in Physics.OverlapSphere(position, radius))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.GetDamaged(damage * damageConst, pc.netId);
                    if (!enemy.IsAlive)
                    {
                        ++kill;
                    }
                }
            }
            GameObject.Instantiate(Resources.Load<GameObject>("Effects/ExplosionEffect_Big"),position, Quaternion.identity);
            pc.playerCommandHub.CmdEffect(pc.netId, position, "ExplosionEffect_Big", 0);
            SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, Resources.Load<AudioClip>("Sounds/headshot"));
        }
        
        damageConst = 1;
        if (sm.perks[Perk.T1_1]) // 마이클 베이 퍽
        {
            damageConst += 0.05f * kill;
        }
        else if (sm.perks[Perk.T1_2]) //격려 퍽
        {
            Timer -= coolTime * 0.05f * kill;
        }
    }
}

public class BerserkSkill : Skill
{
    const float oriCoolTime = 100;
    public override bool useable
    {
        get
        {
            return Timer <= 0 && !isUsing && !isLocked;
        }
    }

    public override void Init()
    {
        coolTime = oriCoolTime;
        Timer = 0;
        pc.KillEnemy += KillEnemy;
    }

    public override void SkillTimerUpdate()
    {
        if (Timer > 0)
        {
            Timer -= Time.deltaTime;
        }
    }

    public override void Use()
    {
        if (useable)
            GameManager.inst.Coroutine(Using());
    }

    private IEnumerator Using()
    {
        isUsing = true;

        float remainedTime = 10;
        Player player = pc.player;
        player.MinHealth = 1;
        Buff buff = null;

        coolTime = oriCoolTime;

        while (true)
        {
            if (buff != null)
                pc.playerCommandHub.CmdRemoveBuff(pc.netId, buff);
            buff = new Buff(Buff.Stat.Damage, ((1 - player.CurHealth) / player.MaxHealth));
            pc.playerCommandHub.CmdAddBuff(pc.netId, buff);
            remainedTime -= Time.deltaTime;
            if (remainedTime < 0)
                break;
            yield return null;
        }
        if (sm.perks[Perk.U1_1])
        {
            pc.player.GetDamaged(-pc.player.MaxHealth / 2);
            pc.playerCommandHub.CmdAddBuff(pc.netId, new Buff(Buff.Stat.Damage, 50));
        }
        player.MinHealth = 0;
        Timer = coolTime;
        isUsing = false;
    }

    void KillEnemy(object sender, EventArgs e)
    {
        if (!isUsing || !sm.perks[Perk.U1_2])
            return;
        pc.player.GetDamaged(-0.01f * pc.player.MaxHealth);
        coolTime -= oriCoolTime * 0.01f;
    }
}