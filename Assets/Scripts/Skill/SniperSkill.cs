using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AI;

public class SniperSkill : ClassSkill
{
    public override void ClassSkillInitial()
    {
        skills[0] = new HeadShotSkill();
        skills[1] = new HideSkill();
        skills[2] = new RailGunSkill();
        skills[3] = new PacifistSkill();
        foreach (var skill in skills)
        {
            if (skill != null)
                skill.Init();
        }
    }

    public override bool CheckSkillUsage()
    {
        return base.CheckSkillUsage() || GameManager.inst.playerController.classController.isSpecialMove;
    }
}

public class HeadShotSkill : Skill
{
    const float oriCoolTime = 5.0f;
    float oriConst = 3;

    const float AimingConstPerSec = 0.5f;
    public override bool useable { get { return base.useable && (PlayerState.curState == PlayerState.idle) && pc.isMain; } }

    float aimingConst = 1; // 관음증

    float ProConst //프로
    {
        get {
            if (sm.perks[Perk.F1_1] && (PlayerState.curState == PlayerState.idle) && pc.player.Agent.velocity.magnitude == 0)
                return 1.3f;
            else
                return 1;
        }
    }

    public float AmateurConst //아마추어
    {
        get {
            if (sm.perks[Perk.F1_2])
                return 2;
            else
                return 1;
        }
    }

    public int shotStack = 0;

    public float ShotStackConst //청부업자
    {
        get {
            if (sm.perks[Perk.F3_2])
                return 1 + 0.05f * shotStack;
            else
                return 1;
        }
    }

    private int remainedShotCount = 1;

    public override void Init()
    {
        coolTime = 5.0f;
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

    private IEnumerator Using()
    {
        isUsing = true;

        GameManager.inst.inGameUIManager.DisableCrossHair();

        GameObject line = new GameObject("Line");
        LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.material = Resources.Load<Material>("Materials/ChargingEffect");
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        GameObject circle = new GameObject("Circle");
        Mesh circleMesh = new Mesh();
        circle.AddComponent<MeshFilter>().mesh = circleMesh;
        circle.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/ChargingEffect");
        circleMesh.DrawCircle(1, 20);

        Enemy aimingEnemy = null;

        //GameManager.inst.cameraController.CameraFOVChange(pc.GetComponent<NetworkIdentity>(), 0.1f, 75); // 시야각 증가

        if (sm.perks[Perk.F2_2])
        {
            remainedShotCount = 3;
        }
        else
        {
            remainedShotCount = 1;
        }

        aimingConst = 1;

        while (true)
        {
            pc.DisableShot = true;

            #region DrawUI

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Enemy enemy = null;

            Physics.Raycast(ray, out hit);
            if (hit.collider != null && hit.collider.GetComponent<Enemy>() != null)
            {
                enemy = hit.collider.GetComponent<Enemy>();
            }

            lineRenderer.SetPosition(0, pc.transform.position - new Vector3(0, pc.transform.position.y - 0.01f));
            if (enemy != null)
            {
                lineRenderer.SetPosition(1, enemy.transform.position - new Vector3(0, enemy.transform.position.y - 0.01f));
                circle.transform.position = enemy.transform.position + new Vector3(0, 0.01f, 0);
            }
            else
            {
                Plane plane = new Plane(Vector3.up, pc.transform.position);
                ;
                float distance;
                plane.Raycast(ray, out distance);
                lineRenderer.SetPosition(1, ray.GetPoint(distance) + new Vector3(0, 0.01f, 0));
                circle.transform.position = ray.GetPoint(distance) + new Vector3(0, 0.01f, 0);
            }

            #endregion

            if (sm.perks[Perk.F3_1]) //관음증
            {
                if (aimingEnemy == enemy)
                {
                    aimingConst = Mathf.Min(aimingConst + AimingConstPerSec * Time.deltaTime, 2);
                }
                else
                {
                    aimingConst = 1;
                }
            }

            aimingEnemy = enemy;

            if (Input.GetMouseButtonDown(0) && aimingEnemy != null)
            {
                float WilliamConst = 1; //윌리엄델 퍽
                if (sm.perks[Perk.F2_1])
                {
                    WilliamConst = Mathf.Clamp(Vector3.Distance(pc.transform.position, aimingEnemy.transform.position) * 0.25f, 1, 5);
                }

                bool missed = false;
                if (sm.perks[Perk.F1_2])
                {
                    missed = UnityEngine.Random.Range(0f, 1f) > 0.7f;
                }

                if (!missed)
                {
                    aimingEnemy.GetDamaged(pc.player.Gun.Damage * ProConst * AmateurConst * oriConst * ShotStackConst * WilliamConst * aimingConst, pc.netId);
                }

                Debug.Log("William : " + WilliamConst.ToString() + " Pro : " + ProConst.ToString() + " ShotStack : " + ShotStackConst.ToString() + " Aiming : " + aimingConst.ToString());
                SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, Resources.Load<AudioClip>("Sounds/headshot"));

                if (sm.perks[Perk.F3_2]) //청부업자
                {
                    shotStack = (shotStack + 1) % 6;
                }

                GameManager.inst.cameraController.CameraShaking(1.5f, pc.gun.GunShotInterval);

                if (--remainedShotCount <= 0)
                {
                    if (ProConst > 1)
                    {
                        Timer = coolTime / 2;
                    }
                    else
                    {
                        Timer = coolTime;
                    }

                    break;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (sm.perks[Perk.F2_2] && remainedShotCount < 3)
                {
                    Timer = coolTime;
                }

                break;
            }

            yield return null;
        }

        //GameManager.inst.cameraController.CameraFOVChange(pc.GetComponent<NetworkIdentity>(), 0.1f, 60); //시야각 복구

        GameObject.Destroy(line);
        GameObject.Destroy(circle);

        pc.EnableShot(pc.gun.GunShotInterval);

        GameManager.inst.inGameUIManager.EnableCrossHair();
        isUsing = false;
    }
}

public class HideSkill : Skill
{
    const float healthPerSec = 10;
    const float staminaPerSec = 10;
    private int shotCount = 0;

    public override void Init()
    {
        coolTime = 15;
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

    private IEnumerator Using()
    {
        isUsing = true;

        float hideTimer = 10;
        const int radius = 3;
        const float moveDistance = 4;
        shotCount = 0;

        //로컬
        foreach (SkinnedMeshRenderer renderer in pc.Renderers)
        {
            renderer.material.shader = Shader.Find("Atlas/Cloaking");
        }
        
        //동기화
        pc.playerCommandHub.CmdHideSkillCloakingEffect(pc.netId, true);
        
        Buff damageBuff = new Buff(Buff.Stat.Damage, 200);
        Buff speedBuff = new Buff(Buff.Stat.Speed, 50);
        pc.playerCommandHub.CmdAddBuff(pc.netId, damageBuff);
        
        if (sm.perks[Perk.S1_1]) //닌자
        {
            pc.playerCommandHub.CmdAddBuff(pc.netId,speedBuff);
        }

        pc.gameObject.layer = LayerMask.NameToLayer("Cloaking");

        if (sm.perks[Perk.S2_1]) //연막
        {
            foreach (var col in Physics.OverlapSphere(pc.transform.position, radius))
            {
                if (col.GetComponent<Enemy>() != null)
                {
                    //col.GetComponent<Enemy>().AddDebuff(new Debuff(Debuff.DebuffTypes.Confusion, 3));
                    pc.playerCommandHub.CmdAddDebuff(col.GetComponent<Enemy>().netId, Debuff.DebuffTypes.Confusion, 3);
                }
            }
            Plane plane = new Plane(Vector3.up, pc.transform.position);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float distance;
            plane.Raycast(ray, out distance);
            Vector3 point = ray.GetPoint(distance);
            if (Vector3.Distance(point, pc.transform.position) < moveDistance)
            {
                pc.transform.position = point;
            }
            else
            {
                pc.transform.position += (point - pc.transform.position).normalized * moveDistance;
            }
        }
        pc.tag = "Cloaking";
        pc.GunShot += OnGunShot;

        while (true)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer < 0)
                break;

            if (sm.perks[Perk.S2_2]) //유령 퍽
            {
                if (shotCount > 2)
                    break;
            }
            else if(shotCount > 0)
            {
                break;
            }

            if (sm.perks[Perk.S1_2]) //휴식 퍽
            {
                pc.player.GetDamaged(-healthPerSec * Time.deltaTime);
                pc.player.CurStamina += staminaPerSec * Time.deltaTime;
            }

            yield return null;
        }

        if (sm.perks[Perk.S1_1])
        {
            pc.playerCommandHub.CmdRemoveBuff(pc.netId, speedBuff);
        }

        pc.tag = "Player";
        pc.GunShot -= OnGunShot;
        pc.gameObject.layer = LayerMask.NameToLayer("Player");
        Timer = coolTime;
        
        //로컬
        foreach (SkinnedMeshRenderer renderer in pc.Renderers)
        {
            renderer.material.shader = Shader.Find("Atlas/Full(Silhouette, Normal)");
        }
        
        //동기화
        pc.playerCommandHub.CmdHideSkillCloakingEffect(pc.netId, false);
        
        yield return new WaitForSeconds(pc.gun.GunShotInterval - 0.01f);

        isUsing = false;
        pc.playerCommandHub.CmdRemoveBuff(pc.netId, damageBuff);
    }

    public void OnGunShot(object o, EventArgs e)
    {
        ++shotCount;
    }
}

public class RailGunSkill : Skill
{
    const float oriCoolTime = 40.0f;
    const float shotRange = 30.0f;
    private int opportunity = 0;
    private float shotIntervalTimer;

    public override bool useable
    {
        get
        {
            return base.useable && GunState.curState == GunState.idle && pc.gun.RemainedBullets > 0 && pc.isMain;
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

    }

    public override void Use()
    {
        if (useable)
        {
            if (sm.perks[Perk.T2_2]) //효율성 퍽
                opportunity = 2;
            else
                opportunity = 1;

            GameManager.inst.Coroutine(Using());
        }
    }

    IEnumerator Using()
    {
        isUsing = true;
        GameObject line = new GameObject("line");
        LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.material = Resources.Load<Material>("Materials/ChargingEffect");
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        Dictionary<Enemy, int> hitEnemies = new Dictionary<Enemy, int>();

        if (sm.perks[Perk.T2_2])
            Timer = coolTime; //효율성 퍽에의해 사용하자 마자 쿨타임이 돌기 시작

        pc.gun.reloadEvent += OnReload;

        while (true)
        {
            pc.DisableShot = true;
            Vector3 playerPos = pc.transform.position;

            #region DrawUI
            Plane plane = new Plane(Vector3.up, playerPos);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float dist;
            plane.Raycast(ray, out dist);
            Vector3 point = ray.GetPoint(dist);

            lineRenderer.SetPosition(0, playerPos - new Vector3(0, playerPos.y - 0.01f, 0));
            lineRenderer.SetPosition(1, playerPos - new Vector3(0, playerPos.y - 0.01f, 0) + Vector3.Normalize(point - playerPos) * shotRange);

            List<RaycastHit> rayHitList = new List<RaycastHit>();
            foreach (var hit in Physics.RaycastAll(new Ray(playerPos + new Vector3(0,0.01f,0), pc.transform.forward), shotRange)/*Physics.BoxCastAll(playerPos, new Vector3(0.2f, 0.2f, 0.2f), point - playerPos, Quaternion.identity, shotRange)*/)
            {
                if (hit.collider.GetComponent<Player>() != null)
                    continue;
                rayHitList.Add(hit);
            }
            rayHitList.Sort(CompareRayHitByDistance);

            foreach (var hit in rayHitList)
            {
                if (!sm.perks[Perk.T1_2] && hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                {
                    lineRenderer.SetPosition(1, hit.point - new Vector3 (0, hit.point.y - 0.01f, 0));
                    break;
                }
            }
            #endregion

            if (Input.GetMouseButton(0) && pc.gun.RemainedBullets > 0 && shotIntervalTimer <= 0)
            {
                foreach (var hit in rayHitList)
                {
                    if (hit.collider == null)
                        continue;
                    if (hit.collider.GetComponent<Enemy>() != null)
                    {
                        if (sm.perks[Perk.T1_1]) //천공탄 퍽
                        {
                            Vector3 dir = hit.collider.transform.position - playerPos;
                            Vector3 knockBack = new Vector3(dir.x, 0, dir.z).normalized * 3.0f;
                            pc.playerCommandHub.CmdKnockBackEnemy(hit.collider.GetComponent<NetworkIdentity>().netId, knockBack);
                        }
                        if (hitEnemies.ContainsKey(hit.collider.GetComponent<Enemy>()))
                        {
                            int count = hitEnemies[hit.collider.GetComponent<Enemy>()];
                            hitEnemies.Remove(hit.collider.GetComponent<Enemy>());
                            hitEnemies.Add(hit.collider.GetComponent<Enemy>(), count + 1);
                        }
                        else
                        {
                            hitEnemies.Add(hit.collider.GetComponent<Enemy>(), 1);
                        }

                        float confirmConst = 1;
                        if (sm.perks[Perk.T2_1]) //확인사살 퍽
                        {
                            confirmConst = Mathf.Pow(2, (hitEnemies[hit.collider.GetComponent<Enemy>()] - 1) % 2 );
                        }
                        hit.collider.GetComponent<Enemy>().GetDamaged(pc.gun.Damage * confirmConst, pc.netId);
                    }
                    if (!sm.perks[Perk.T1_2] && hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall")) //관찰자 퍽이 없을 경우 벽을 뚫지 못함
                    {
                        break;
                    }
                }
                GameObject obj = GameObject.Instantiate(line);
                obj.GetComponent<Renderer>().material.color = new Color(0, 1, 1);
                GameManager.inst.Coroutine(FadeOut(obj));
                SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, pc.gun.shotSFX);

                shotIntervalTimer = pc.gun.GunShotInterval;
                --pc.gun.RemainedBullets;
            }

            if (Input.GetKeyDown(PlayerKeyControl.inst.reload) && GunState.curState == GunState.idle && pc.gun.RemainedBullets < pc.gun.MaxBullets)
            {
                pc.gun.Reload();
            }
            if (opportunity == 0)
                break;
            shotIntervalTimer -= Time.deltaTime;
            yield return null;
        }
        if (!sm.perks[Perk.T2_2])
            Timer = coolTime;
        pc.gun.reloadEvent -= OnReload;
        pc.EnableShot(pc.gun.GunShotInterval);
        isUsing = false;
        GameObject.Destroy(line);
    }

    public void OnReload(object obj, EventArgs args)
    {
        --opportunity;
    }

    IEnumerator FadeOut(GameObject obj)
    {
        Color oriColor = obj.GetComponent<Renderer>().material.color;
        for (int i = 0; i < 10; ++i)
        {
            obj.GetComponent<Renderer>().material.color = new Color(oriColor.r, oriColor.g, oriColor.b, (10 - i) / 10f);
            yield return new WaitForSeconds(1 / 60f);
        }
        GameObject.Destroy(obj);
    }

    public int CompareRayHitByDistance(RaycastHit x, RaycastHit y)
    {
        if (x.distance > y.distance)
            return 1;
        else if (x.distance == y.distance)
            return 0;
        else
            return -1;
    }

}

public class PacifistSkill : Skill
{
    const float oriCoolTime = 200.0f; /* 200.0f */
    const float angle = 90;
    const float radius = 20;

    private float shotInterval
    {
        get
        {
            if (sm.perks[Perk.U1_1])
            {
                return pc.gun.GunShotInterval / 4;
            }
            else if (sm.perks[Perk.U1_2])
            {
                return pc.gun.GunShotInterval;
            }
            else
            {
                return pc.gun.GunShotInterval / 2;
            }
        }
    }

    public override bool useable
    {
        get
        {
            return base.useable && GunState.curState == GunState.idle;
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
    }

    public override void Use()
    {
        GameManager.inst.Coroutine(Using());
    }

    IEnumerator Using()
    {
        isUsing = true;

        GameObject arc = new GameObject("Arc");
        GameObject line = new GameObject("Line");
        GameObject circle = new GameObject("Circle");

        Mesh arcMesh = new Mesh();
        arc.AddComponent<MeshFilter>().mesh = arcMesh;
        arc.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/ChargingEffect");
        arcMesh.DrawArc(arc.transform, radius, angle);

        LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.material = Resources.Load<Material>("Materials/ChargingEffect");
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        Mesh circleMesh = new Mesh();
        circle.AddComponent<MeshFilter>().mesh = circleMesh;
        circle.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/ChargingEffect");
        circleMesh.DrawCircle(1, 20);

        float skillRemainTimer = 10;


        float shotIntervalTimer = 0;

        while (true)
        {
            pc.DisableShot = true;

            arc.transform.position = pc.transform.position;
            arc.transform.rotation = pc.transform.rotation;

            Enemy nearestEnemy = null;
            Vector3 playerPos = pc.transform.position;

            List<RaycastHit> enemiesHit = new List<RaycastHit>();
            for (float theta = (90 - (angle / 2)) * Mathf.PI / 180; theta <= (90 + (angle / 2)) * Mathf.PI / 180; theta += 0.025f)
            {
                float x = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta);
                Vector3 dir = pc.transform.rotation * new Vector3(x, 0, z);

                foreach (var hit in Physics.RaycastAll(new Ray(playerPos, dir), radius))
                {
                    if (hit.collider.GetComponent<Enemy>() != null)
                    {
                        enemiesHit.Add(hit);
                    }
                }
            }

            enemiesHit.Sort(CompareRayHitByDistance);

            if (enemiesHit.Count != 0)
            {
                nearestEnemy = enemiesHit[0].collider.GetComponent<Enemy>();
            }
            
            if (nearestEnemy != null)
            {
                line.gameObject.SetActive(true);
                lineRenderer.SetPosition(0, nearestEnemy.transform.position - new Vector3(0, nearestEnemy.transform.position.y - 0.01f, 0));
                lineRenderer.SetPosition(1, pc.transform.position - new Vector3(0, pc.transform.position.y - 0.01f, 0));

                circle.gameObject.SetActive(true);
                circle.transform.position = nearestEnemy.transform.position - new Vector3(0, nearestEnemy.transform.position.y - 0.01f, 0);
            }
            else
            {
                line.gameObject.SetActive(false);
                circle.gameObject.SetActive(false);
            }

            if (Input.GetMouseButtonDown(0) && nearestEnemy != null && shotIntervalTimer <= 0 && GunState.curState == GunState.idle && pc.gun.RemainedBullets > 0)
            {
                PacifistShot(nearestEnemy);
                shotIntervalTimer = shotInterval;
            }
            /*
            if (Input.GetKeyDown(PlayerKeyControl.inst.reload))
            {
                pc.gun.Reload();
            }
            */
            shotIntervalTimer -= Time.deltaTime;
            skillRemainTimer -= Time.deltaTime;

            if (skillRemainTimer < 0)
                break;

            yield return null;
        }
        pc.EnableShot(pc.gun.GunShotInterval);
        isUsing = false;

        Timer = coolTime;
        GameObject.Destroy(arc);
        GameObject.Destroy(line);
        GameObject.Destroy(circle);
    }

    private void PacifistShot(Enemy aimedEnemy)
    {
        //로컬
        GameObject line = new GameObject("BulletLine");
        LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.material = Resources.Load<Material>("Materials/BulletLine");
        lineRenderer.material.color = new Color(1, 1, 0);
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.SetPosition(0, pc.transform.position);
        lineRenderer.SetPosition(1, aimedEnemy.transform.position);
        GameManager.inst.Coroutine(FadeOut(line));
        
        pc.playerCommandHub.CmdPacifistSkill(pc.netId, aimedEnemy.netId);

        if (sm.perks[Perk.U1_2])
        {
            aimedEnemy.GetDamaged(pc.gun.Damage * 3, pc.netId);
            SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, Resources.Load<AudioClip>("Sounds/HeadShot"));
        }
        else
        {
            aimedEnemy.GetDamaged(pc.gun.Damage, pc.netId);
            SoundManager.inst.PlaySFX(pc.gameObject, pc.netId, pc.gun.shotSFX);
        }
    }

    public int CompareRayHitByDistance(RaycastHit x, RaycastHit y)
    {
        if (x.distance > y.distance)
            return 1;
        else if (x.distance == y.distance)
            return 0;
        else
            return -1;
    }

    IEnumerator FadeOut(GameObject obj)
    {
        Color oriColor = obj.GetComponent<Renderer>().material.color;
        for (int i = 0; i < 20; ++i)
        {
            obj.GetComponent<Renderer>().material.color = new Color(oriColor.r, oriColor.g, oriColor.b, (20 - i) / 20f);
            yield return new WaitForSeconds(1 / 60f);
        }
        GameObject.Destroy(obj);
    }
}