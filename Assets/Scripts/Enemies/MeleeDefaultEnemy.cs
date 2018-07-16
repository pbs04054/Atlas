﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

public class MeleeDefaultEnemy : Enemy
{
    [SerializeField]
    float searchRange; //탐색 범위

    [SerializeField]
    float attackRange; //공격 범위

    [SerializeField]
    GameObject hitEffect;

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
            Player targetPlayer = GetPlayerByDistance();

            if (targetPlayer == null)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }
            
            if (Vector3.Distance(targetPlayer.transform.position, transform.position) >= attackRange)
                yield return StartCoroutine("Move");
            else
                yield return StartCoroutine("Attack");

            yield return null;
        }

    }

    IEnumerator Move()
    {
        while (true)
        {
            Player targetPlayer = GetPlayerByDistance();
            if (!CanMove || targetPlayer == null|| targetPlayer.CompareTag("Cloaking") || !targetPlayer.IsAlive)
            {
                Agent.isStopped = true;
                yield break;
            }
            float dist = Vector3.Distance(targetPlayer.transform.position, transform.position);
            if (dist > searchRange + 1f) // Exit
            {
                yield break;
            }
            if (dist < attackRange)
            {            
                yield return StartCoroutine("Attack");
            }

            if (!CanMove || targetPlayer == null || targetPlayer.CompareTag("Cloaking") || !targetPlayer.IsAlive)
            {
                Agent.isStopped = true;
                yield break;
            }
            
            MoveToLocation(targetPlayer.transform.position);

            yield return null;
        }
    }

    IEnumerator Attack()
    {
        Player targetPlayer = GetPlayerByDistance();
        if (!CanAttack || targetPlayer == null|| targetPlayer.CompareTag("Cloaking") || !targetPlayer.IsAlive)
            yield break;
        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
        transform.LookAt(targetPlayer.transform);
        targetPlayer.GetDamaged(Damage, targetPlayer.netId, transform.rotation);
        yield return new WaitForSeconds(AttackSpeed);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, searchRange);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    public override void GetDamaged(IBullet bullet)
    {
        base.GetDamaged(bullet);
        Instantiate(hitEffect, transform.position, bullet.Transform.rotation);
        lastBulletRotation = bullet.Transform.rotation;
    }
}