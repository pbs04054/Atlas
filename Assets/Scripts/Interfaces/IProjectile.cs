using UnityEngine;

public interface IProjectile
{
    Transform Transform { get; }
    void Init(float damage, Vector3 force);
}