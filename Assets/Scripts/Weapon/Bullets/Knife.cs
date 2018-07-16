using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Knife : Bullet
{

    const float EnemyDetectDistance = 3;
    private bool isPierceable = false;
    private bool isExplode = false;
    private bool isDivided = false;

    const float explodeRange = 5.0f;

    public event EventHandler KillEnemy;

    public List<Enemy> hitEnemy =new List<Enemy>();

    public void Init(float damage, Vector3 force, bool pierceable, bool explode, bool divide, NetworkInstanceId id)
    {
        Init(damage, force, id);
        rigidBody.AddForce(force);
        isPierceable = pierceable;
        isExplode = explode;
        isDivided = divide;
    }

    protected override void OnTriggerEnter(Collider col)
    {
        Enemy enemy = col.GetComponent<Enemy>();
        if (enemy != null && !hitEnemy.Contains(enemy))
        {
            hitEnemy.Add(enemy);
            GameManager.inst.playerController.playerCommandHub.CmdAddDebuff(enemy.netId, Debuff.DebuffTypes.Bleeding, 5);
            GameManager.inst.playerController.playerCommandHub.CmdAddDebuff(enemy.netId, Debuff.DebuffTypes.Poisoned, 5);
            if (isExplode)
            {
                Instantiate(Resources.Load<GameObject>("Effects/ExplosionEffect"),transform.position, Quaternion.identity);
                foreach (var collider in Physics.OverlapSphere(transform.position, explodeRange))
                {
                    Enemy e = collider.GetComponent<Enemy>();
                    if (e != null)
                    {
                        e.GetDamaged(Damage * 2, ID);
                        if (!e.IsAlive)
                        {
                            if (KillEnemy != null)
                            {
                                KillEnemy(this, EventArgs.Empty);
                            }
                        }
                    }
                }
                isExplode = false;
            }
            else
            {
                enemy.GetDamaged(this);
                if (!enemy.IsAlive)
                {
                    if (KillEnemy != null)
                    {
                        KillEnemy(this, EventArgs.Empty);
                    }
                }
            }

            if (isDivided)
            {
                GameObject obj0 = Instantiate(gameObject, transform.position, transform.rotation);
                obj0.GetComponent<Rigidbody>().velocity = Quaternion.Euler(0, 15, 0) * rigidBody.velocity;
                GameObject obj1 = Instantiate(gameObject, transform.position, transform.rotation);
                obj1.GetComponent<Rigidbody>().velocity = Quaternion.Euler(0, -15, 0) * rigidBody.velocity;
            }

            if (!isPierceable)
            {
                Destroy(gameObject);
            }
            return;
        }
        if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            GetComponent<Rigidbody>().velocity = new Vector3();
            GetComponent<Collider>().enabled = false;
            StartCoroutine(FadeOut());
        }
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(2);
        Destroy(gameObject);
    }
}
