using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AI;

public class GuardSkill : ClassSkill
{
    public override void ClassSkillInitial()
    {
        skills[0] = new Rush();
        skills[1] = new Earthquake();
        skills[2] = new Conqueror();
        skills[3] = new ForceField();
        foreach(var skill in skills)
        {
            if(skill != null)
            {
                skill.Init();
            }
        }
    }
}

public class Rush : Skill
{
    private Buff squadronBuff;
    
    public override void Init()
    {
        Timer = 0.0f;
        squadronBuff = new Buff(Buff.Stat.StaminaShield, 20f);
    }

    public override void SkillTimerUpdate()
    {
        if (Timer > 0)
            Timer -= Time.deltaTime;
    }

    public override void Use()
    {
        if (useable)
        {
            GameManager.inst.Coroutine(Using());
        }

    }

    public void SpwanFire(Vector3 location)
    {

    }


    private IEnumerator Using()
    {
        coolTime = sm.perks[Perk.F1_1] ? 4.5f : 6f; //질주본능
        float rushSpeed = sm.perks[Perk.F1_2] ? 3 : 2.5f; //살아있는 폭탄 vs 노말
        isUsing = true;
        pc.DisableShot = true;
        GameObject line = new GameObject("rush");
        LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.material = Resources.Load<Material>("Materials/ChargingEffect");
        float dist;
        bool useSkill = false;

        while (true)
        {
            Vector3 playerloc = pc.transform.position;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, playerloc);
            plane.Raycast(ray, out dist);
            line.GetComponent<LineRenderer>().SetPosition(0, playerloc);
            line.GetComponent<LineRenderer>()
                .SetPosition(1, playerloc + (ray.GetPoint(dist) - playerloc).normalized * 10);

            if (Input.GetMouseButtonDown(0))
            {
                useSkill = true;
                float rushtime = sm.perks[Perk.F1_1] ? 1.5f : 1.0f;
                if (sm.perks[Perk.F3_2]) rushtime = float.MaxValue;
                GameObject.Destroy(line);
                pc.DisalbeMouseLook = !sm.perks[Perk.F3_2];

                if (sm.perks[Perk.F2_1])
                {
                    pc.playerCommandHub.CmdAddBuff(pc.netId, squadronBuff);
                }

                while (rushtime > 0)
                {
                    Vector2 rushdir = new Vector2(pc.transform.forward.x * rushSpeed, pc.transform.forward.z * rushSpeed);
                    pc.player.Move(rushdir);
                    if (sm.perks[Perk.F2_2]) //고스트라이더
                    {
                        GameObject.Instantiate(Resources.Load<GameObject>("Effects/Flame"), pc.transform.position,
                            Quaternion.identity).AddComponent<Flame>().Init(pc.netId, 1f, 3f, 5f);
                        pc.playerCommandHub.CmdEffect(pc.netId, pc.transform.position, "Flame", 5f);
                    }

                    playerloc = pc.transform.position + pc.transform.forward;
                    Collider[] colliders = Physics.OverlapSphere(playerloc, 0.7f);

                    foreach (var crushed in colliders)
                    {
                        if (crushed.gameObject.layer == LayerMask.NameToLayer("Wall"))
                        {
                            Debug.Log("crushed"); //지형충돌
                            rushtime = 0;
                            if (sm.perks[Perk.F1_2]) //살아있는 폭탄
                            {
                                foreach (var enemy in pc.player.GetObjectsByCircle<Enemy>(5))
                                {
                                    enemy.GetDamaged(50f, pc.netId);
                                }

                                GameObject.Instantiate(Resources.Load<GameObject>("Effects/ExplosionEffect"),
                                    pc.transform.position, Quaternion.identity);
                                pc.playerCommandHub.CmdEffect(pc.netId, pc.transform.position, "ExplosionEffect", 5);
                            }
                        }

                        if (crushed.GetComponent<Enemy>() != null)
                        {
                            Debug.Log("a");
                            Vector3 dir = playerloc - crushed.transform.position;
                            Vector3 knockBack = new Vector3(dir.x, 0, dir.z).normalized * 4.0f /
                                                crushed.GetComponent<Rigidbody>().mass;
                            pc.playerCommandHub.CmdKnockBackEnemy(crushed.GetComponent<NetworkIdentity>().netId,
                                -knockBack);
                        }

                    }

                    rushtime -= Time.deltaTime;
                    yield return null;
                }

                Timer = coolTime;
                Debug.Log("break");
                pc.DisalbeMouseLook = false;
                break;
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                break;
            }

            yield return null;
        }

        if (GameObject.Find("rush") != null)
        {
            GameObject.Destroy(line);
        }
        
        if (useSkill && sm.perks[Perk.F3_1])
        {
            //핵분열 (역장생성)
            Field field = GameObject.Instantiate(Resources.Load<Field>("Prefabs/Field"), pc.transform.position, Quaternion.identity);
            if (pc.isServer)
                field.Init(pc.netId, 3, Perk.F3_1, 7);
            else
            {
                field.transform.localScale *= 7;
                GameObject.Destroy(field.gameObject, 3f);
            }

            //동기화
            pc.playerCommandHub.CmdCreateField(pc.netId, Perk.F3_1, 3, 7);
        }
        
        if (useSkill && sm.perks[Perk.F2_1])
        {
            pc.playerCommandHub.CmdRemoveBuff(pc.netId, squadronBuff);
        }

        isUsing = false;
        pc.DisableShot = false;
    }
}

public class Earthquake : Skill
{
    public override void Init()
    {
        coolTime = 8.0f;
        Timer = 0.0f;
    }

    public override void SkillTimerUpdate()
    {
        if (Timer > 0)
            Timer -= Time.deltaTime;
    }

    public override void Use()
    {
        if (useable)
        {
            GameManager.inst.Coroutine(Using());
        }
    }

    IEnumerator UseAgain()
    {
        float timeLimit = 3.0f;
        while(timeLimit > 0 && canUseAgain)
        {
            timeLimit -= Time.deltaTime;
            yield return null;
        }
        canUseAgain = false;
        Timer = coolTime;
    }

    bool canUseAgain;

    IEnumerator Using()
    {
        isUsing = true;
        int rangeAngle = sm.perks[Perk.S1_2] ? 180 : 90;//강인한 팔
        GameObject meshGameObject = new GameObject();
        Mesh mesh = new Mesh();
        meshGameObject.AddComponent<MeshFilter>().mesh = mesh;
        meshGameObject.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/ChargingEffect");
        mesh.DrawArc(meshGameObject.transform, 15, rangeAngle);
        pc.DisableShot = true;

        while (true)
        {
            meshGameObject.transform.position = pc.transform.position + new Vector3(0, 0.01f);
            meshGameObject.transform.rotation = pc.transform.rotation;
            if (Input.GetMouseButtonDown(0))
            {
                SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, Resources.Load<AudioClip>("Sounds/Skill/EarthQuake"));
                Enemy[] enemies= pc.player.GetObjectsByArc<Enemy>(15, rangeAngle);
                foreach (var enemy in enemies)
                {
                    enemy.GetDamaged(10f, pc.netId);
                    pc.playerCommandHub.CmdAddDebuff(enemy.netId, Debuff.DebuffTypes.Stun, 3);
                }
                if (sm.perks[Perk.S1_1])
                {
                    pc.playerCommandHub.CmdGiveHealToPlayer(pc.netId, 5 * enemies.Length, false); //흡혈
                }
                if (sm.perks[Perk.S2_1])
                {
                    pc.playerCommandHub.CmdGiveStaminaToPlayer(pc.netId, 5 * enemies.Length, false); //의지
                }
                if (sm.perks[Perk.S2_2] && !canUseAgain)
                {
                    canUseAgain = true;
                    GameManager.inst.Coroutine(UseAgain());
                    break;
                }
                canUseAgain = false;
                Timer = coolTime;
                break;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                break;
            }
            yield return null;
        }

        pc.DisableShot = false;
        isUsing = false;
        GameObject.Destroy(meshGameObject);
    }
}

public class Conqueror : Skill
{
    public override void Init()
    {
        coolTime = 8.0f;
        Timer = 0;
    }

    public override void SkillTimerUpdate()
    {
        if (Timer > 0)
            Timer -= Time.deltaTime;
    }

    public override void Use()
    {
        if (useable)
        {
            GameManager.inst.Coroutine(Using());
        }
    }

    public GameObject MakeArea(Vector3 pos)
    {
        GameObject area = new GameObject("area");
        area.transform.position = pos;
        MeshFilter areaMeshFilter = area.AddComponent<MeshFilter>();
        MeshRenderer areaMeshRenderer = area.AddComponent<MeshRenderer>();
        Mesh areaMesh = new Mesh();
        areaMeshFilter.mesh = areaMesh;
        areaMeshRenderer.material = Resources.Load<Material>("Materials/Circle64");
        areaMesh.DrawCircle(8, 360);
        return area;
    }

    int killStack = 0;
    Dictionary<Player, int> remainedBullets = new Dictionary<Player, int>(); 

    public void OnKillEnemyinConq(object sender, EventArgs e)
    {
        killStack++;
        Debug.Log("killed");
    }

    private IEnumerator Using()
    {
        float usingTime = 15f;
        isUsing = true;
        Vector3 playerPos = pc.transform.position + Vector3.up * 0.01f; 
        GameObject conquerorArea = MakeArea(playerPos);
        pc.playerCommandHub.CmdConquerorArea(pc.netId, 15f, 8);
        killStack = 0;
        GunInfo pastgun = pc.gun.mainGunInfo;

        if (sm.perks[Perk.T1_1])//컨저커
        {
            pc.isMain = true;
            GunInfo machineGun = JsonHelper.LoadGunInfo(PlayerClass.GUARD).mainWeaponList.Find(info => info.name == "ConquerorMachinegun");
            pc.gun.GunStart(machineGun);
            pc.gun.GunInterfacesStart(machineGun);
            pc.gun.CmdGunStart(machineGun);
            pc.DoWeaponCheck();
            GunState.Transition(GunState.idle);
            GameManager.inst.inGameUIManager.UpdateBullet();
        }
        if (sm.perks[Perk.T2_2])
        {
            pc.KillEnemy += OnKillEnemyinConq;
            //killStack추가
            //치어리더
        }

        while (usingTime > 0)
        {
            playerPos = pc.transform.position;
            conquerorArea.transform.position = playerPos;
            Player[] allies = pc.player.GetObjectsByCircle<Player>(8);
            Player[] allPlayers = GameManager.inst.players.Values.ToArray();
            if (!sm.perks[Perk.S1_2])
            {
                //보호본능 false시
                pc.DisableSpecialMove = true; // 동기화 안해도 될듯
            }

            foreach (Player player in allPlayers)
            {
                pc.playerCommandHub.CmdSetInfiniteBullet(player.netId, false);
                foreach (Player ally in allies)
                {
                    if (allies.Contains(player))
                    {
                        pc.playerCommandHub.CmdSetInfiniteBullet(player.netId, true);
                        break;
                    }
                }
            }

            pc.playerCommandHub.CmdSetInfiniteBullet(pc.netId, sm.perks[Perk.T2_1]);

            
            usingTime -= Time.deltaTime;
            yield return null;
        }

        foreach (Player player in GameManager.inst.players.Values.ToArray())
        {
            pc.playerCommandHub.CmdSetInfiniteBullet(player.netId, false);
            if (sm.perks[Perk.T2_2])
            {
                pc.playerCommandHub.CmdGiveStaminaToPlayer(player.netId, killStack * 2f ,false);
            }
        }
        if (sm.perks[Perk.T1_1])
        {
            pc.gun.GunStart(pastgun);
            pc.gun.GunInterfacesStart(pastgun);
            pc.gun.CmdGunStart(pastgun);
            pc.DoWeaponCheck();
        }
        
        Debug.Log("cc");
        isUsing = false;
        Timer = coolTime;
        GameObject.Destroy(conquerorArea);
    }
} 

public class ForceField : Skill
{
    public override void Init()
    {
        coolTime = 10.0f;
        Timer = 0.0f;
    }

    public override void SkillTimerUpdate()
    {
        if(Timer > 0)
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
        GameObject newfield = Resources.Load<GameObject>("Prefabs/Field");
        GameObject field = GameObject.Instantiate(newfield, pc.transform.position, Quaternion.identity);
        field.transform.localScale *= 10;
        Component.Destroy(field.GetComponent<Field>());

        while (true)
        {
            Plane plane = new Plane(Vector3.up, pc.transform.position);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float distance;
            plane.Raycast(ray, out distance);
            Vector3 point = ray.GetPoint(distance);
            field.transform.position = point + new Vector3(0, 0.01f, 0);

            if (Input.GetMouseButtonDown(0))
            {
                if (sm.perks[Perk.U1_1])
                {
                    Field forceField = GameObject.Instantiate(newfield, field.transform.position, Quaternion.identity).GetComponent<Field>();
                    if(pc.isServer)
                        forceField.Init(pc.netId, 5.0f, Perk.U1_1, 10);
                    else
                    {
                        forceField.transform.localScale *= 10f;
                        GameObject.Destroy(forceField.gameObject, 5f);
                    }
                    pc.playerCommandHub.CmdCreateField(pc.netId, Perk.U1_1, 5, 10);
                }
                else if (sm.perks[Perk.U1_2])
                {
                    Field forceField = GameObject.Instantiate(newfield, field.transform.position, Quaternion.identity).GetComponent<Field>();
                    if(pc.isServer)
                        forceField.Init(pc.netId,5.0f, Perk.U1_2, 10);
                    else
                    {
                        forceField.transform.localScale *= 10f;
                        GameObject.Destroy(forceField.gameObject, 5f);
                    }
                    pc.playerCommandHub.CmdCreateField(pc.netId, Perk.U1_2, 5, 10);
                }
                else
                {
                    Field forceField = GameObject.Instantiate(newfield, field.transform.position, Quaternion.identity).GetComponent<Field>();
                    if(pc.isServer)
                        forceField.Init(pc.netId, 5.0f, Perk.F3_1, 10);
                    else
                    {
                        forceField.transform.localScale *= 10f;
                        GameObject.Destroy(forceField.gameObject, 5f);
                    }
                    pc.playerCommandHub.CmdCreateField(pc.netId, Perk.F3_1, 5, 10);
                }

                GameObject.Destroy(field);
                Timer = coolTime;
                break;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameObject.Destroy(field);
                break;
            }
            yield return null;
        }
        isUsing = false;
    }


}
