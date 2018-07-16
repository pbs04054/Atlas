using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DoctorSkill : ClassSkill
{
    public override void ClassSkillInitial()
    {
        skills[0] = new JanusSkill();
        skills[1] = new VaccinationSkill();
        skills[2] = new SuicidingBombSkill();
        skills[3] = new ManiaSkill();

        foreach (var skill in skills)
        {
            if (skill != null)
            {
                skill.Init();
            }
        }
    }

    public override bool CheckSkillUsage()
    {
        return false;
    }
}

public class JanusSkill : Skill
{
    public static bool isHeal = false;
    private float overloadTimer = 5;
    private float overloadStack = 0;
    private Buff overloadBuff = new Buff();
    private float versatilityTimer = 0; //다재다능 퍽 관련
    private int versatilityStack = 0;
    private Buff versatilityBuff = new Buff();

    public override void Init()
    {
        coolTime = 1;
        Timer = 0;
        pc.playerCommandHub.CmdAddBuff(pc.netId, versatilityBuff);
        pc.playerCommandHub.CmdAddBuff(pc.netId, overloadBuff);
    }

    public override void SkillTimerUpdate()
    {
        if (Timer > 0)
        {
            Timer -= Time.deltaTime;
        }
        if (versatilityTimer > 0)
        {
            versatilityTimer -= Time.deltaTime;
            if (versatilityTimer <= 0)
            {
                versatilityStack = 0;
                pc.playerCommandHub.CmdRemoveBuff(pc.netId, versatilityBuff);
                versatilityBuff = new Buff();
                pc.playerCommandHub.CmdAddBuff(pc.netId, versatilityBuff);
            }
        }
        if (sm.perks[Perk.F3_1] && overloadTimer > 0)
        {
            overloadTimer -= Time.deltaTime;
        }
        if (overloadTimer < 0 && overloadStack < 10 && sm.perks[Perk.F3_1])
        {
            ++overloadStack;
            pc.playerCommandHub.CmdRemoveBuff(pc.netId, overloadBuff);
            overloadBuff = new Buff( overloadBuff.stat, overloadBuff.value + 10);
            pc.playerCommandHub.CmdAddBuff(pc.netId, overloadBuff);
            overloadTimer = 5;
        }
    }

    public override void Use()
    {
        if (!useable)
            return;
        isHeal = !isHeal;
        Debug.Log(isHeal);
        Timer = sm.perks[Perk.F1_1] ? coolTime - 5 : coolTime;
        if (sm.perks[Perk.F1_1])
        {
            GameManager.inst.Coroutine(AddSpeedBuff());
        }
        if (sm.perks[Perk.F1_2])
        {
            GameManager.inst.Coroutine(AddDamageBuff());
        }
        if (sm.perks[Perk.F2_1])
        {
            for (int i = 1; i < 4; ++i)
            {
                sm.classSkill.skills[i].Timer -= 2;
            }
        }
        if (sm.perks[Perk.F2_2])
        {
            GameManager.inst.Coroutine(BionicGas(pc.transform.position));
        }
        if (sm.perks[Perk.F3_1])
        {
            overloadStack = 0;
            pc.playerCommandHub.CmdRemoveBuff(pc.netId, overloadBuff);
            overloadBuff = new Buff();
            pc.playerCommandHub.CmdAddBuff(pc.netId, overloadBuff);
        }
        if (sm.perks[Perk.F3_2] && versatilityStack < 10)
        {
            ++versatilityStack;
            pc.playerCommandHub.CmdRemoveBuff(pc.netId, versatilityBuff);
            versatilityBuff = new Buff(versatilityBuff.stat, versatilityBuff.value + 10);
            pc.playerCommandHub.CmdAddBuff(pc.netId, versatilityBuff);
            versatilityTimer = 15f;
        }
    }

    private IEnumerator AddSpeedBuff()
    {
        Buff speedBuff = new Buff(Buff.Stat.Speed, 30);
        pc.playerCommandHub.CmdAddBuff(pc.netId, speedBuff);
        yield return new WaitForSeconds(3);
        pc.playerCommandHub.CmdRemoveBuff(pc.netId, speedBuff);
    }

    private IEnumerator AddDamageBuff()
    {
        Buff damagebuff = new Buff(Buff.Stat.Damage, 20);
        pc.playerCommandHub.CmdAddBuff(pc.netId, damagebuff);
        yield return new WaitForSeconds(5);
        pc.playerCommandHub.CmdRemoveBuff(pc.netId, damagebuff);
    }

    private IEnumerator BionicGas(Vector3 pos)
    {
        for (int i = 0; i < 10; ++i)
        {
            AttackArea.CreateCircle(pos, 5, 1);
            yield return new WaitForSeconds(1);
            foreach(var col in Physics.OverlapSphere(pos, 5))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.GetDamaged(Mathf.Min(enemy.CurHealth * 0.05f, 50), pc.netId);
                }
                Player player = col.GetComponent<Player>();
                if (player != null)
                {
                    pc.playerCommandHub.CmdGiveHealToPlayer(player.GetComponent<NetworkIdentity>().netId, player.MaxHealth * 0.05f, false);
                }
            }
        }
    }
}

public class VaccinationSkill : Skill
{
    private int _stock;
    public int stock { get { return _stock; } set { _stock = value; GameManager.inst.inGameUIManager.UpdateSkillStockText(1, stock); } }
    float damage = 25;

    public override bool useable
    {
        get
        {
            return base.useable || (sm.perks[Perk.S1_2] && stock > 0 && !isLocked);
        }
    }

    public override void Init()
    {
        coolTime = 15;
        Timer = 0;
    }

    public override void SkillTimerUpdate()
    {
        if (Timer > 0)
        {
            Timer -= Time.deltaTime;
        }
        if (sm.perks[Perk.S1_2] && Timer <= 0 && stock < 3)
        {
            ++stock;
            if (stock < 3)
                Timer = coolTime;
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

        GameObject line0 = new GameObject();
        line0.AddComponent<LineRenderer>().material = Resources.Load<Material>("Materials/ChargingEffect");
        line0.GetComponent<LineRenderer>().startWidth = 0.2f;
        line0.GetComponent<LineRenderer>().endWidth = 0.2f;

        while (true)
        {
            Vector3 playerPos = GameManager.inst.playerController.transform.position;
            Plane plane = new Plane(Vector3.up, playerPos);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float dist;
            plane.Raycast(ray, out dist);
            Vector3 point = ray.GetPoint(dist);

            line0.GetComponent<LineRenderer>().SetPosition(0, playerPos);
            line0.GetComponent<LineRenderer>().SetPosition(1, playerPos + (point - playerPos).normalized * 10);


            if (Input.GetMouseButtonDown(0))
            {

                point = point - playerPos;
                point *= 100f;

                GameObject injectorPrefab = Resources.Load<GameObject>("Prefabs/Bullets/Injector");

                GameObject obj = GameObject.Instantiate(injectorPrefab, playerPos + new Vector3(0, 0.5f, 0), GameManager.inst.playerController.transform.rotation);
                obj.transform.Rotate(new Vector3(-90, pc.transform.rotation.y, 0));
                obj.GetComponent<Injector>().Init(damage, point.normalized * 2000, pc.netId, sm.perks[Perk.S1_1], sm.perks[Perk.S1_2], sm.perks[Perk.S2_2],sm.perks[Perk.S2_1]);
                SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, Resources.Load<AudioClip>("Sounds/ThrowKnife"));

                pc.playerCommandHub.CmdInjectorThrow(pc.netId, point.normalized * 2000, damage, sm.perks[Perk.S1_1], sm.perks[Perk.S1_2], sm.perks[Perk.S2_2], sm.perks[Perk.S2_1]);

                if (!sm.perks[Perk.S1_2])
                {
                    Timer = coolTime;
                }
                else
                {
                    --stock;
                    if (Timer < 0)
                    {
                        Timer = coolTime;
                    }
                }

                break;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                break;
            }
            yield return null;
        }
        isUsing = false;
        pc.EnableShot(0.2f);
        GameObject.Destroy(line0);
    }
}

public class SuicidingBombSkill : Skill
{
    const float radius = 10;
    float sodomyConst
    {
        get { return sm.perks[Perk.T1_2] ? 1.5f : 1; }
    }
    const float placeboConst = 0.05f;

    public override bool useable
    {
        get
        {
            return base.useable && pc.player.CurHealth > 1;
        }
    }

    public override void Init()
    {
        coolTime = 0;
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
        AttackArea.CreateCircle(pc.transform, radius, 1);
        yield return new WaitForSeconds(1);
        float healthUsedAmount = pc.player.CurHealth / 2;
        pc.playerCommandHub.CmdGivePlayerDamage(pc.netId, pc.player.CurHealth / 2);
        Timer = coolTime;
        GameObject.Instantiate(Resources.Load<GameObject>("Effects/SuicidingBomb"), pc.transform.position, Quaternion.identity);
        pc.playerCommandHub.CmdEffect(pc.netId, pc.transform.position, "SuicidingBomb", 0);

        foreach (var col in Physics.OverlapSphere(pc.transform.position, radius))
        {
            if (col.GetComponent<Enemy>() != null)
            {
                Enemy enemy = col.GetComponent<Enemy>();
                enemy.GetDamaged(healthUsedAmount * 0.5f * sodomyConst);
                enemy.AddDebuff(new Debuff(Debuff.DebuffTypes.Poisoned, 10));
                if (sm.perks[Perk.T2_1]) //플라시보 퍽
                {
                    pc.playerCommandHub.CmdGiveHealToPlayer(pc.netId, healthUsedAmount * 0.5f * sodomyConst * placeboConst, false);
                }
                if (sm.perks[Perk.T1_1]) //히포크라테스 넉백
                {
                    pc.playerCommandHub.CmdKnockBackEnemy(enemy.GetComponent<NetworkIdentity>().netId, (enemy.transform.position - pc.transform.position).normalized * (radius - Vector3.Distance(pc.transform.position, enemy.transform.position)));
                }
                if (sm.perks[Perk.T1_2] && !enemy.IsAlive)
                {
                    Timer -= 3;
                }
            }
            if (col.GetComponent<Player>() != null && col.GetComponent<Player>() != pc.player)
            {
                Player player = col.GetComponent<Player>();
                Debug.Log(player.name);
                if (player.CurHealth > 0)
                {
                    pc.playerCommandHub.CmdGiveHealToPlayer(player.GetComponent<NetworkIdentity>().netId, healthUsedAmount * 0.25f, false);
                }
                else if (sm.perks[Perk.T1_1]) //히포크라테스 퍽
                {
                    pc.playerCommandHub.CmdGiveHealToPlayer(player.GetComponent<NetworkIdentity>().netId, healthUsedAmount * 0.1f, true);
                }
            }
        }
        if (sm.perks[Perk.T2_2])
        {
            pc.playerCommandHub.CmdSpawnPoisonGas(pc.netId, 10, radius, pc.gun.Damage, JanusSkill.isHeal, pc.transform.position);
        }

        isUsing = false;
    }
}

public class ManiaSkill : Skill
{
    private float skillRemainTimer = 15.0f;
    public override void Init()
    {
        coolTime = 120;
        Timer = 0;
        pc.KillEnemy += OnKillEnemy;
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
            GameManager.inst.StartCoroutine(Using());
            Timer = coolTime;
        }
    }

    private IEnumerator Using()
    {
        isUsing = true;
        Buff speedBuff = new Buff(Buff.Stat.Speed, 50);
        Buff damageBuff = new Buff(Buff.Stat.Damage, 50);
        pc.playerCommandHub.CmdAddBuff(pc.netId, speedBuff);
        if (sm.perks[Perk.U1_1])
        {
            pc.playerCommandHub.CmdAddDebuff(pc.netId, Debuff.DebuffTypes.Poisoned, 5);
            GameManager.inst.Coroutine(AddSpeedBuff(-30, 5));
            pc.playerCommandHub.CmdAddBuff(pc.netId, damageBuff);
        }
        while (true)
        {
            skillRemainTimer -= Time.deltaTime;
            if (skillRemainTimer <= 0)
            {
                break;
            }
            yield return null;
        }
        pc.player.RemoveBuff(speedBuff);
        if (sm.perks[Perk.U1_1])
        {
            pc.player.RemoveBuff(damageBuff);
        }
        isUsing = false;
    }

    private IEnumerator AddSpeedBuff(float amount, float time)
    {
        Buff buff = new Buff(Buff.Stat.Speed, amount);
        pc.playerCommandHub.CmdAddBuff(pc.netId, buff);
        yield return new WaitForSeconds(time);
        pc.playerCommandHub.CmdRemoveBuff(pc.netId, buff);
    }

    public void OnKillEnemy(object obj, EventArgs args)
    {
        if (sm.perks[Perk.U1_2])
        {
            skillRemainTimer += 1f;
        }
    }
}