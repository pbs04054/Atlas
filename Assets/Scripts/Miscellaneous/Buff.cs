using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Buff : IEquatable<Buff>
{

    public enum Stat
    {
        Health, Defense, Speed, AvoidRate, //공통 스텟(Actor)
        Damage, AttackSpeed,
        StaminaPerSecond, StaminaShield,
        BulletSpreadAngle, MaxBullets, // 총 공통 스텟(Gun)
        Health_Sum, Speed_Sum, Damage_Sum, ProjectileRadius_Sum //몬스터 관련 합연산
    };

    [SerializeField] public Stat stat;
    [SerializeField] public float value;

    public Buff()
    {
        stat = default(Stat);
        value = default(float);
    }

    public Buff(Stat stat, float value)
    {
        this.stat = stat;
        this.value = value;
    }

    public bool Equals(Buff other)
    {
        if (other == null) return false;
        return other.stat == stat && other.value == value;
    }

    public float GetBuff(Stat stat)
    {
        return this.stat == stat ? value : 0;
    }
    
}
