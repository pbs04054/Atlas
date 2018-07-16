using System.Collections;
using UnityEngine;

public class Dreadnought : Enemy
{
    
    [SerializeField] float baseAttackRadius;
    public float AttackRadius { get; private set; }

    [SerializeField] GameObject hitEffect;

    [SerializeField] GameObject explosionEffect;

    Quaternion lastBulletRotation;

    public override void Awake()
    {
        base.Awake();
        lastBulletRotation = new Quaternion();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine("Idle");
    }

    IEnumerator Idle()
    {        
        Agent.isStopped = true;
        while (true)
        {
            if (!IsAlive)
                yield break;
            yield return new WaitForSeconds(AttackSpeed);
            yield return StartCoroutine("Attack");
        }
    }

    IEnumerator Attack()
    {        
        if (!CanAttack) yield break;
        Player targetPlayer = FindObjectsOfType<Player>().Random();
        Vector3 attackPos = targetPlayer.transform.position + new Vector3(Random.Range(-3.00f,3.00f), 0, Random.Range(-3.00f, 3.00f));

        //동기화
        GameManager.inst.playerController.playerCommandHub.CmdAttackAreaCircle(GameManager.inst.playerController.netId, attackPos, AttackRadius, 3f);

        //로컬에서 쏘는거
        yield return AttackArea.CreateCircle(attackPos, AttackRadius, 3f);
        Player[] players = GetObjectsByCircle<Player>(attackPos, AttackRadius);
        foreach (Player player in players)
        {
            player.GetDamaged(Damage, player.netId, player.transform.rotation);
        }

        //로컬 폭파 이펙트
        Instantiate(explosionEffect, attackPos, Quaternion.identity);

        //동기화
        GameManager.inst.playerController.playerCommandHub.CmdDreadnoughtExplosionEffect(GameManager.inst.playerController.netId, attackPos);
    }

    protected override void CalculateBuff()
    {
        base.CalculateBuff();

        float attackRadius = 0;

        foreach (Buff buff in Buffs)
        {
            attackRadius += buff.GetBuff(Buff.Stat.ProjectileRadius_Sum);
        }

        AttackRadius = baseAttackRadius + attackRadius;
        Debug.Log(attackRadius);
    }
}
