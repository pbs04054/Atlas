using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class GuardShield : MonoBehaviour
{

    MeshRenderer meshRenderer;
    PlayerController playerController;

    static Color defaultColor = new Color(92/255f,194/255f,1,1);
    static Color hitColor = new Color(19/255f, 0, 1, 1);

    void Awake()
    {
        playerController = transform.parent.GetComponent<PlayerController>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        EnemyProjectile enemyProjectile = other.GetComponent<EnemyProjectile>();
        if (enemyProjectile == null) return;

        playerController.playerCommandHub.CmdGivePlayerDamage(playerController.netId, enemyProjectile.Damage * 0.25f);
        playerController.playerCommandHub.CmdGiveStaminaToPlayer(playerController.netId, -enemyProjectile.Damage * 0.1f, false);

        Vector3 dir = enemyProjectile.transform.position - transform.position;
        dir = dir.normalized;

        Instantiate(Resources.Load("Effects/ShieldImpact"), enemyProjectile.transform.position, Quaternion.LookRotation(dir));
        StopAllCoroutines();
        StartCoroutine("ShieldUpdator");

        playerController.playerCommandHub.CmdUseMoney(playerController.netId, -enemyProjectile.Damage);

        NetworkServer.Destroy(enemyProjectile.gameObject);
    }

    IEnumerator ShieldUpdator()
    {
        meshRenderer.material.SetColor("_Tint", hitColor);
        float timer = 0;
        while (true)
        {
            if (timer > 0.2f)
                break;


            meshRenderer.material.SetColor("_Tint", Color.Lerp(hitColor, defaultColor, timer / 0.2f));
            timer += Time.deltaTime;
            yield return null;
        }
    }

}
