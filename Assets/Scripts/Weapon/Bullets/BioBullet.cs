using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BioBullet : NetworkBehaviour, IBullet
{

    public Transform Transform
    {
        get { return transform; }
    }
    public float Damage { get; private set; }
    public NetworkInstanceId ID { get; private set; }
    [SerializeField] float speed, radius;
    
    public static Color AttackColor = new Color(0, 246/255f, 134/255f);
    public static Color HealColor = new Color(246/255f, 229/255f, 0);

    bool isHeal = true;
    bool isMania = false;

    public void Init(float damage, Vector3 force)
    {
        Damage = damage;
        StartCoroutine("BioUpdator", force);
    }
    public void Init(float damage, Vector3 force, NetworkInstanceId id)
    {
        ID = id;
        Init(damage, force);
    }
    public void Init(float damage, Vector3 force, NetworkInstanceId id, bool isHeal, bool isMania)
    {
        this.isHeal = isHeal;
        this.isMania = isMania;
        Init(damage, force, id);
    }

    IEnumerator BioUpdator(Vector3 endPos)
    {
        ParticleSystem.MainModule mainModule = transform.Find("BioEffect").GetComponent<ParticleSystem>().main;
        if (isHeal)
        {
            mainModule.startColor = HealColor;
        }
        else
        {
            mainModule.startColor = AttackColor;
        }
        Vector3 startPos = transform.position;
        endPos.y = 1;
        float percent = 0;
        bool isStopped = false;
        while (!isStopped)
        {
            if (percent >= 1)
                break;
            percent += (Time.deltaTime * speed) / Vector3.Distance(startPos, endPos);
            transform.position = Vector3.Lerp(startPos, endPos, percent);
            foreach (var col in Physics.OverlapSphere(transform.position, radius / 5))
            {
                if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
                {
                    isStopped = true;
                    break;
                }
            }
            yield return null;
        }

        StartCoroutine("BioActive");
        yield return new WaitUntil(() => transform.childCount == 0);
        Destroy(gameObject);
    }

    IEnumerator BioActive()
    {
        while (true)
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider col in cols)
            {
                //if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
                //{
                //    Destroy(gameObject);
                //    continue;
                //}

                if (isHeal || isMania)
                {
                    Player player = col.GetComponent<Player>();
                    if (player != null)
                    {
                        if(player != GameManager.inst.playerController.player)
                            GameManager.inst.playerController.playerCommandHub.CmdUseMoney(GameManager.inst.playerController.netId, -Damage);
                        GameManager.inst.playerController.playerCommandHub.CmdGiveHealToPlayer(player.netId, Damage * 0.5f, false);
                    }
                }
                if (!isHeal || isMania)
                {
                    Enemy enemy = col.GetComponent<Enemy>();
                    if (enemy != null)
                        enemy.GetDamaged(this);
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
