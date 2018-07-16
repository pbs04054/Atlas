using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BioGas : NetworkBehaviour {

    float lifeTime, damage, radius;
    NetworkInstanceId id;
    bool isHeal;
    [SyncVar] public Vector3 localScale;

    public void Init(float time, float damage, float radius, bool isHeal, NetworkInstanceId playerID)
    {
        lifeTime = time;
        this.damage = damage;
        this.radius = radius;
        this.isHeal = isHeal;
        transform.localScale = Vector3.one * radius;
        localScale = Vector3.one * radius;
        id = playerID;

        ParticleSystem.MainModule mainModule = GetComponent<ParticleSystem>().main;
        if (isHeal)
        {
            mainModule.startColor = BioBullet.HealColor;
        }
        else
        {
            mainModule.startColor = BioBullet.AttackColor;
        }
        StartCoroutine(GasRemain());
    }

    public void Init(float damage, NetworkInstanceId playerID)
    {
        this.damage = damage;
        id = playerID;
        StartCoroutine("GasUpdator");
    }

    void Update()
    {
        transform.localScale = localScale;
    }

    private IEnumerator GasRemain()
    {
        while (true)
        {
            lifeTime -= 1;
            if (lifeTime <= 0)
                break;
            foreach (var col in Physics.OverlapSphere(transform.position, radius))
            {
                if (isHeal)
                {
                    Player player = col.GetComponent<Player>();
                    if (player != null)
                    {
                        if (player != GameManager.inst.playerController.player)
                            GameManager.inst.playerController.playerCommandHub.CmdUseMoney(GameManager.inst.playerController.netId, -damage * 10f);
                        GameManager.inst.playerController.playerCommandHub.CmdGiveHealToPlayer(player.netId, damage * 10f, false);
                    }
                }
                else
                {
                    Enemy enemy = col.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        GameManager.inst.playerController.playerCommandHub.CmdAddDebuff(enemy.netId, Debuff.DebuffTypes.Poisoned, 1);
                        enemy.GetDamaged(damage, id);
                    }
                }
            }
            yield return new WaitForSeconds(1);
        }
        Destroy(gameObject);
    }

    IEnumerator GasUpdator()
    {
        while (true)
        {
            foreach (var col in Physics.OverlapSphere(transform.position, localScale.x))
            {
                Player player = col.GetComponent<Player>();
                if (player != null)
                {
                    if (player != GameManager.inst.playerController.player)
                        GameManager.inst.playerController.playerCommandHub.CmdUseMoney(GameManager.inst.playerController.netId, -damage * 5f);
                    GameManager.inst.playerController.playerCommandHub.CmdGiveHealToPlayer(player.netId, damage * 5f, false);
                }
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null)
                    enemy.GetDamaged(damage, id);
                
            }
            
            yield return  new WaitForSeconds(1f);
        }
    }
    
}
