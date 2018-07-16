using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

public class Field : MonoBehaviour {

    List<Player> inPlayer = new List<Player>();
    List<Enemy> inEnemy = new List<Enemy>();
    List<EnemyProjectile> enemyBullets = new List<EnemyProjectile>();
    PlayerController controller;
    float constTime;
    int drainCount;
    Perk skillPerk;
    NetworkInstanceId playerID;

    float slowTime = 3;

    // Use this for initialization
	void Start () {
        controller = GameManager.inst.playerController;
	}
	
	// Update is called once per frame
	void Update () {
        
	}

    public void Init(NetworkInstanceId id, float time, Perk p, float scale)
    {
        playerID = id;
        transform.localScale *= scale;
        constTime = time;
        skillPerk = p;
        GameManager.inst.Coroutine(Remaining());
    }

    public void End()
    {
        foreach (EnemyProjectile enemyProjectile in enemyBullets)
        {
            enemyProjectile.ReturnSpeed();
        }

        foreach (Enemy enemy in inEnemy)
        {
            
        }

        if(skillPerk == Perk.U1_1)
        {
            foreach (Collider enemy in Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius))
            {
                enemy.GetComponent<Enemy>().GetDamaged(10 * drainCount, playerID);
            }
        }

        Destroy(gameObject);
    }

    IEnumerator Remaining()
    {
        float curTime = constTime;
        while(curTime > 0)
        {
            curTime -= Time.deltaTime;
            yield return null;
        }
        End();

    }

    void OnTriggerEnter(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if(enemy != null)
        {
            Vector3 knockBackDir = enemy.transform.position - transform.position;
            knockBackDir = knockBackDir.normalized;
            GameManager.inst.playerController.playerCommandHub.CmdKnockBackEnemy(enemy.netId, knockBackDir * 2f);
            
            if(skillPerk != Perk.U1_2)
            {
                Vector3 dir = transform.position - other.transform.position;
                Vector3 knockBack = new Vector3(dir.x, 0, dir.z).normalized * 0.3f / other.GetComponent<Rigidbody>().mass;
                controller.playerCommandHub.CmdKnockBackEnemy(other.GetComponent<NetworkIdentity>().netId, -knockBack);
            }
            if(skillPerk == Perk.U1_2)
            {
                GameManager.inst.playerController.playerCommandHub.CmdAddDebuff(enemy.netId, Debuff.DebuffTypes.Stun, 2f);
                inEnemy.Add(enemy);
            }

        }
        
        EnemyProjectile projectile = other.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {        
            if (skillPerk != Perk.U1_1)
            {
                projectile.speed /= slowTime;
                enemyBullets.Add(projectile);
            }
            if(skillPerk == Perk.U1_1)
            {
                drainCount++;
                Destroy(other.gameObject);
            }
        }

        if(other.GetComponent<Player>() != null)
        {
            inPlayer.Add(other.GetComponent<Player>());
        }
    }

    void OnTriggerExit(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if(enemy != null)
        {
            if (skillPerk == Perk.U1_1)
            {
                Vector3 dir = transform.position - enemy.transform.position;
                Vector3 knockBack = new Vector3(dir.x, 0, dir.z).normalized * 0.3f / enemy.GetComponent<Rigidbody>().mass;
                controller.playerCommandHub.CmdKnockBackEnemy(enemy.netId, -knockBack);
            }
            
            if(skillPerk == Perk.U1_2)
            {
                inEnemy.Remove(enemy);
            }
        }

        EnemyProjectile projectile = other.GetComponent<EnemyProjectile>();
        if(projectile != null)
        {
            projectile.ReturnSpeed();
            enemyBullets.Remove(projectile);
        }

        if(other.GetComponent<Player>() != null)
        {

        }
    }
}
