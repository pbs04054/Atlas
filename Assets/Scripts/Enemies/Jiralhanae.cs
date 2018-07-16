using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Jiralhanae : Enemy, IDamageable
{
    
    public float ChargingValue { get; private set; }

    float attackLength = 7, attackWidth = 4, attackCharging = 0.5f;
    float roarRadius = 10, roarAngle = 90, roarCharging = 3f;
    float rollerCharging = 0.5f;
    float earthquakeRadius = 15f, earthquakeCharging = 0.5f;
    float tackleCharging = 2f;

    float attackCoolTime, roarCoolTime, tackleCoolTime, rollerCoolTime, earthquakeCoolTime;

    Player targetPlayer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine("Idle");
    }
    
    
    /*
     * Todo : Cloaking 상태인 플레이어 무시 기능 추가, 좀 더 다듬어야함 
     */
    
    IEnumerator Idle()
    {
        Agent.isStopped = true;
        while (true)
        {
            targetPlayer = FindObjectsOfType<Player>().Random();
            if (targetPlayer == null || targetPlayer.gameObject.CompareTag("Cloaking"))
            {
                yield return new WaitForSeconds(1f);
                continue;
            }
            
            if (earthquakeCoolTime == 0 && CurHealth / MaxHealth <= 0.2f)
                yield return StartCoroutine("Earthquake");

            if (rollerCoolTime == 0 && CurHealth / MaxHealth <= 0.6f)
                yield return StartCoroutine("Roller");

            if (tackleCoolTime == 0 && CurHealth / MaxHealth <= 0.7f)
                yield return StartCoroutine("Tackle");

            if (roarCoolTime == 0)
                yield return StartCoroutine("Roar");
            
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) >= attackLength - 1)
                yield return StartCoroutine("Move");
            else if(attackCoolTime == 0)
                yield return StartCoroutine("Attack");

            yield return null;
        }
    }

    IEnumerator Move()
    {
        Debug.Log("Jiralhanae : Move");
        float timer = 0;
        while (true)
        {
            if (timer >= 3)
                yield break;
            
            timer += Time.deltaTime;
            float dist = Vector3.Distance(targetPlayer.transform.position, transform.position);
            if (dist < 5f)
                yield break;
            
            MoveToLocation(targetPlayer.transform.position);

            yield return null;
        }
    }

    IEnumerator Attack()
    {
        Debug.Log("Jiralhanae : Attack");
        /*
         기본공격
             0.5초간 차징 후 범위 내의 플레이어에게 대미지를 줌
                 쿨타임 3초
         */
        List<Coroutine> coroutines = new List<Coroutine>()
        {
            AttackArea.CreateBox(transform, attackLength, attackWidth, attackCharging),
            StartCoroutine("Charging", attackCharging)
        };

        foreach (Coroutine coroutine in coroutines)
        {
            yield return coroutine;
        }

        foreach (Player player in GetObjectsByBox<Player>(attackLength, attackWidth))
        {
            player.GetDamaged(Damage);
        }

        StartCoroutine(CoolTimeUpdator(cooltime => attackCoolTime = cooltime, 1f));
    }

    IEnumerator Roar()
    {
        Debug.Log("Jiralhanae : Roar");
        /*
        울부짖기
            3초간 차징 후 범위 내의 플레이어에게 10초의 공포 상태이상을 걺
                첫 등장시 바로 사용
                쿨타임 15초, 범위 내에 Damagable이 있으면 사용
         */

        List<Coroutine> coroutines = new List<Coroutine>
        {
            AttackArea.CreateArc(transform, roarRadius, roarAngle, roarCharging),
            StartCoroutine("Charging", roarCharging)
        };

        foreach (Coroutine coroutine in coroutines)
        {
            yield return coroutine;
        }

        foreach (Player player in GetObjectsByArc<Player>(roarRadius, roarAngle))
        {
            player.AddDebuff(new Debuff(Debuff.DebuffTypes.Fear, 10f));
        }

        StartCoroutine(CoolTimeUpdator(cooltime => roarCoolTime = cooltime, 15f));
    }

    IEnumerator Tackle()
    {
        Debug.Log("Jiralhanae : Tackle");
        /*
        몸통박치기
            플레이어 하나를 목표 지정하여 2초간 차징 후 해당 플레이어의 방향으로 벽에 부딪힐때까지 돌진.
            부딪히는 모든 IDamagable에게 300의 데미지를 줌 
                체력이 70%일때부터 사용 쿨타임 10초, 울부짖기 히트시 쿨 상관없이 무조건 연계(이건 체력 40%부터)되고 쿨 처음부터 다시 채움
                목표 지정 순위:울부짖기 피격된 Damagable>최저 체력>최고 딜량
         */
        targetPlayer = FindObjectsOfType<Player>().Random();

        List<Coroutine> coroutines = new List<Coroutine>()
        {
            StartCoroutine("Charging", 2f),
            StartCoroutine(LookAtTarget(targetPlayer.transform, 2f))
        };

        foreach (Coroutine coroutine in coroutines)
        {
            yield return coroutine;
        }

        List<IDamageable> damageableHolder = new List<IDamageable>();
        Vector3 velocity = transform.forward * Speed;
        while (true)
        {
            Collider[] wallCols = Physics.OverlapBox(transform.position + transform.forward * 1f, new Vector3(2f, 1f, 0.5f), Quaternion.LookRotation(transform.forward));
            if (wallCols.Any(col => col.gameObject.layer == LayerMask.NameToLayer("Wall")))
                break;
            Collider[] damageableCol = Physics.OverlapSphere(transform.position, 3f);
            foreach (Collider col in damageableCol)
            {
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable == null || damageable == GetComponent<IDamageable>() || damageableHolder.Contains(damageable))
                    continue;
                damageable.GetDamaged(300f);
                damageableHolder.Add(damageable);
            }
            Agent.velocity = velocity;
            yield return null;
        }
        StartCoroutine(CoolTimeUpdator(cooltime => tackleCoolTime = cooltime, 10f));
    }

    IEnumerator Roller()
    {
        Debug.Log("Jiralhanae : Roller");
        /*
        롤러
            돌하나를 랜덤한 방향으로 3번 굴림.
            돌은 벽에 부딪힐시 튕겨남.
            돌에 부딪힐시 IDamagable에게 50의 데미지를 줌.
            돌은 체력이 있으며(500), 파괴 가능
                체력 60%부터 사용, 쿨 15초
         */
        
        
        yield return StartCoroutine("Charging", rollerCharging);

        for (int i = 0; i < 3; i++)
        {
            Instantiate(Resources.Load<Roller>("Prefabs/EnemyProjectile/Jiralhanae_Roller"), transform.position, Quaternion.Euler(0, UnityEngine.Random.Range(0.0f, 360.0f), 0));
            yield return new WaitForSeconds(1f);
        }
        StartCoroutine(CoolTimeUpdator(cooltime => rollerCoolTime = cooltime, 15f));
    }

    IEnumerator Earthquake()
    {
        Debug.Log("Jiralhanae : Earthquake");
        /*
         대지파괴
             점프한 후, 3초 뒤 랜덤한 지역에 착지합니다.
             범위 내의 IDamageable은 1000의 데미지를 받으며, 거리에 따라 100%, 50%, 25%로 데미지가 감소합니다.
                 쿨 8초, 체력 20%부터 사용.
         */
        yield return StartCoroutine("Charging", earthquakeCharging);

        Agent.enabled = false;
        float timer = 0;
        while (true)
        {
            if (timer >= 1)
                break;
            timer += Time.deltaTime;
            RigidBody.MovePosition(transform.position + transform.up * Time.deltaTime * 500);
            yield return null;
        }

        Vector3 randomPos;
        while (true)
        {
            randomPos = UnityEngine.Random.insideUnitSphere * 90;
            randomPos.y = 0.1f;

            
            Collider[] cols = Physics.OverlapSphere(randomPos, 2);
            bool isWall = false, isGround = false;
            foreach (Collider col in cols)
            {
                if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    isGround = true;
                    continue;
                }

                if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
                {
                    isWall = true;
                    continue;
                }
            }

            if (!isWall && isGround)
                break;
            yield return null;
        }

        AttackArea.CreateCircle(randomPos, earthquakeRadius, 3f);
        transform.position = new Vector3(randomPos.x, transform.position.y, randomPos.z);
        Vector3 originPos = transform.position;
        timer = 0;
        while (true)
        {
            if (timer >= 3f)
                break;
            RigidBody.MovePosition(new Vector3(originPos.x, Mathf.Lerp(originPos.y, 0, timer/3f) ,originPos.z));
            timer += Time.deltaTime;
            yield return null;
        }
        Agent.enabled = true;

        foreach (IDamageable damageable in GetObjectsByCircle<IDamageable>(earthquakeRadius))
        {
            if (damageable == GetComponent<IDamageable>())
                continue;
            damageable.GetDamaged(1000 * Vector3.Distance(transform.position, damageable.Transform.position) / earthquakeRadius);
        }
        StartCoroutine(CoolTimeUpdator(cooltime => earthquakeCoolTime = cooltime, 8f));
    }

    IEnumerator Charging(float time)
    {
        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
        float timer = 0;
        while (true)
        {
            if (timer >= time)
                break;

            timer += Time.deltaTime;
            ChargingValue = timer / time;
            yield return null;
        }
        ChargingValue = 0;
        Agent.isStopped = false;
    }

    IEnumerator CoolTimeUpdator(Action<float> coolTimeResult, float value)
    {
        float coolTime = value;
        coolTimeResult(coolTime);
        while (true)
        {
            if (coolTime <= 0)
                break;
            coolTime -= Time.deltaTime;
            coolTimeResult(coolTime);
            yield return null;
        }
        coolTime = 0;
        coolTimeResult(coolTime);
    }

}

/*

미구현
    패시브
        IDamagable을 빈사상태로 만들경우, 체력을 1000 회복함
    Roller
        돌 데미지 안받음

*/