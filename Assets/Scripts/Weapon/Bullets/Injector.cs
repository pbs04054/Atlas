using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Injector : Bullet {

    private bool isFragile; //최초 감염자
    private bool isAdrenaline; //아드레날린
    private bool isPoisonGas; //2차 전염
    private bool isCrazy; //정신분열

    public void Init(float damage, Vector3 force, NetworkInstanceId id, bool fragile, bool adrenaline, bool poisongas, bool crazy)
    {
        Init(damage, force, id);
        isFragile = fragile;
        isAdrenaline = adrenaline;
        isPoisonGas = poisongas;
        isCrazy = crazy;
    }

    protected override void OnTriggerEnter(Collider col)
    {
        Enemy enemy = col.GetComponent<Enemy>();
        if (enemy != null)
        {
            if (isFragile)
            {
                foreach(var collider in Physics.OverlapSphere(enemy.transform.position, 5))
                {
                    if (collider.GetComponent<Enemy>() != null)
                    {
                        InjectorAffectEnemy(collider.GetComponent<Enemy>());
                    }
                }
            }
            else
            {
                InjectorAffectEnemy(enemy);
            }
            enemy.GetDamaged(this);
            Destroy(gameObject);
            return;
        }

        Player player = col.GetComponent<Player>();
        if (player != null && isAdrenaline)
        {
            GiveAdrenalineBuff(player, 10);
        }
        if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator GiveAdrenalineBuff(Player player, float time)
    {
        Buff damageBuff = new Buff(Buff.Stat.Damage, 30);
        Buff speedBuff = new Buff(Buff.Stat.Speed, 30);
        GameManager.inst.playerController.playerCommandHub.CmdAddBuff(player.GetComponent<NetworkIdentity>().netId, damageBuff);
        GameManager.inst.playerController.playerCommandHub.CmdAddBuff(player.GetComponent<NetworkIdentity>().netId, speedBuff);
        yield return new WaitForSeconds(time);
        GameManager.inst.playerController.playerCommandHub.CmdRemoveBuff(player.GetComponent<NetworkIdentity>().netId, damageBuff);
        GameManager.inst.playerController.playerCommandHub.CmdRemoveBuff(player.GetComponent<NetworkIdentity>().netId, speedBuff);
    }

    private void InjectorAffectEnemy(Enemy enemy)
    {
        GameManager.inst.playerController.playerCommandHub.CmdAddDebuff(enemy.GetComponent<NetworkIdentity>().netId, Debuff.DebuffTypes.Poisoned, 5);
        if (isPoisonGas)
        {
            enemy.EnemyDeadEvent += MakePoisonGas;
        }
        if (isCrazy){
            GameManager.inst.playerController.playerCommandHub.CmdAddDebuff(enemy.GetComponent<NetworkIdentity>().netId, Debuff.DebuffTypes.Confusion, 5);
            GameManager.inst.playerController.playerCommandHub.CmdAddDebuff(enemy.GetComponent<NetworkIdentity>().netId, Debuff.DebuffTypes.Weakness, 5);
        }
    }

    private void MakePoisonGas(object obj, EventArgs args)
    {
        Enemy enemy = (Enemy)obj;
        GameManager.inst.playerController.playerCommandHub.CmdSpawnPoisonGas(GameManager.inst.playerController.netId, 6, 2.5f, 0, false, enemy.transform.position);
    }
}
