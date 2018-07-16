using UnityEngine;
using System.Collections;

public interface IDamageable
{
    Transform Transform { get; }
    float MaxHealth { get; }
    float CurHealth { get; }

    void GetDamaged(float amount);

}