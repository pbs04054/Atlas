using UnityEngine;
using UnityEngine.Networking;

public interface IBullet : IProjectile
{
    NetworkInstanceId ID { get; }
    float Damage { get; }
    void Init(float damage, Vector3 force, NetworkInstanceId id);
}
