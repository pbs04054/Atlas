using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ServerSimulator : NetworkBehaviour
{
    //public static ServerSimulator inst
    //{
    //    get
    //    {
    //        if (_inst != null)
    //            return _inst;
    //        else
    //        {
    //            if (FindObjectOfType<ServerSimulator>() != null)
    //            {
    //                _inst = FindObjectOfType<ServerSimulator>();
    //            }
    //            else
    //            {
    //                GameObject obj = new GameObject("ServerSimulator");
    //                _inst = obj.AddComponent<ServerSimulator>();
    //            }
    //            return _inst;
    //        }
    //    }
    //}
    //private static ServerSimulator _inst = null;

    private Player localPlayer;
    private PlayerController localPlayerController;
    private EnemyManager enemyManager;


    void Awake()
    {
        enemyManager = FindObjectOfType<EnemyManager>();
    }

    public void Init(Player player)
    {
        localPlayer = player;
        localPlayerController = player.GetComponent<PlayerController>();
    }

    #region Gun

    [ClientRpc]
    public void RpcRifleShot(Vector3 point, NetworkInstanceId id)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<global::PlayerController>();
        if (playerController == GameManager.inst.playerController)
            return;
        Vector3 playerPos = playerController.transform.position;
        Vector3 dir;

        if (playerController.DisalbeMouseLook == false)
        {
            Vector3 randomPos = point.RotatePointAroundPivot(playerPos,
                new Vector3(0,
                    Random.Range(-playerController.gun.CurBulletSpreadAngle / 2.000f,
                        playerController.gun.CurBulletSpreadAngle / 2.000f), 0));
            dir = randomPos - playerPos;
        }
        else
        {
            dir = playerController.transform.forward;
            dir.y = 0;
        }

        dir = dir.normalized;

        GameObject obj = Instantiate(playerController.gun.bulletPrefab, playerController.gunFirePoint.position,
            playerController.transform.rotation);
        IBullet bullet = obj.GetComponent<IBullet>();
        bullet.Init(playerController.gun.Damage, dir * playerController.gun.BulletSpeed, id);
    }

    [ClientRpc]
    public void RpcShotgunShot(Vector3 point, NetworkInstanceId id)
    {
        PlayerController playerController =
            ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController)
            return;
        Vector3 playerPos = playerController.transform.position;
        for (int i = 0; i < playerController.gun.BulletPerShot; ++i)
        {
            Vector3 anglePos = point.RotatePointAroundPivot(playerPos,
                new Vector3(0,
                    playerController.gun.CurBulletSpreadAngle / playerController.gun.BulletPerShot * i -
                    playerController.gun.CurBulletSpreadAngle * 0.5f, 0));
            Vector3 dir = anglePos - playerPos;
            dir = dir.normalized;

            GameObject obj = Instantiate(playerController.gun.bulletPrefab, playerController.gunFirePoint.position,
                playerController.transform.rotation);
            IBullet bullet = obj.GetComponent<IBullet>();
            bullet.Init(playerController.gun.Damage, dir * playerController.gun.BulletSpeed, id);
            Destroy(obj, 2);
        }
    }

    [ClientRpc]
    public void RpcBioShot(Vector3 point, NetworkInstanceId id, bool isHeal, bool isMania)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController)
            return;
        Vector3 playerPos = playerController.transform.position;
        Vector3 randomPos = point.RotatePointAroundPivot(playerPos,new Vector3(0, Random.Range(-playerController.gun.CurBulletSpreadAngle / 2.000f,playerController.gun.CurBulletSpreadAngle / 2.000f), 0));
        GameObject obj = Instantiate(playerController.gun.bulletPrefab, playerController.gunFirePoint.position,playerController.transform.rotation);
        BioBullet bullet = obj.GetComponent<BioBullet>();
        bullet.Init(playerController.gun.Damage, randomPos, playerController.netId, isHeal, isMania);
    }

    [ClientRpc]
    public void RpcActiveStaminaShield(NetworkInstanceId playerID, bool active)
    {
        ClientScene.FindLocalObject(playerID).GetComponent<PlayerController>().ActivateShield(active);
    }

    [ClientRpc]
    public void RpcSetPlayerBullet(GunInfo gunInfo, NetworkInstanceId id)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        try
        {
            playerController.gun.bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullets/" + gunInfo.bullet);
        }
        catch
        {
            playerController.gun.bulletPrefab = null;
        }
    }

    #endregion

    #region Enemy

/*
    [ClientRpc]
    public void RpcProjectileAttack(string projectileName, float damage, float speed, NetworkInstanceId id)
    {
        Transform enemyTransform = ClientScene.FindLocalObject(id).transform;
        Instantiate(Resources.Load<EnemyProjectile>("Prefabs/EnemyProjectile/"+projectileName), enemyTransform.position, enemyTransform.rotation).Init(damage, speed);
    }
    */

    [ClientRpc]
    public void RpcDreadnoughtExplosionEffect(NetworkInstanceId id, Vector3 position)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController) return;
        Instantiate(Resources.Load("Effects/DreadnoughtHitEffect"), position, Quaternion.identity);
    }

    [ClientRpc]
    public void RpcShowHitEffect(NetworkInstanceId targetId, Quaternion rotation)
    {
        PlayerController playerController = ClientScene.FindLocalObject(targetId).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController)
        {
            playerController.player.ShowHitEffect(rotation);
            playerController.player.ShowHitVignette();;
        }
        else
        {
            playerController.player.ShowHitEffect(rotation);
        }
    }
    
    #endregion

    #region UI

    [ClientRpc]
    public void RpcStartTimer(float time)
    {
        enemyManager.StartCoroutine(enemyManager.ClientTimer(time));
    }

    #endregion

    #region Player

    [TargetRpc]
    public void TargetStartRescue(NetworkConnection self, NetworkInstanceId target)
    {
        PlayerState.rescue.player = ClientScene.FindLocalObject(target).GetComponent<Player>();
        PlayerState.rescue.RescueTimer = 5f;
        PlayerState.Transition(PlayerState.rescue);
    }

    [TargetRpc]
    public void TargetStartRescued(NetworkConnection target, NetworkInstanceId self)
    {
        PlayerState.fatal.RescueTimer = 5f;
    }

    [TargetRpc]
    public void TargetStopRescue(NetworkConnection self, NetworkInstanceId target)
    {
        PlayerState.Transition(PlayerState.idle);
    }

    [TargetRpc]
    public void TargetStopRescued(NetworkConnection target, NetworkInstanceId self)
    {
        PlayerState.fatal.RescueTimer = 0;
    }

    [TargetRpc]
    public void TargetCameraSizeChange(NetworkConnection target, float time, float size)
    {
        GameManager.inst.cameraController.StartCoroutine(GameManager.inst.cameraController.CamSizeChangeUpdator(time, size));
    }

    [TargetRpc]
    public void TargetTransitionToFatal(NetworkConnection target)
    {
        PlayerState.Transition(PlayerState.fatal);
    }

    #endregion

    #region AssultSkill


    [ClientRpc]
    public void RpcKnifeThrowSkill(NetworkInstanceId id, Vector3 force, bool pierceable, bool explode, bool divide )
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController) return;
        Vector3 playerPos = playerController.transform.position;
        Knife knife = Instantiate(Resources.Load<Knife>("Prefabs/Knife"), playerPos + new Vector3(0, 0.5f, 0), playerController.transform.rotation);
        knife.transform.Rotate(new Vector3(-90, playerController.transform.rotation.y, 0));
        knife.Init(0, force, pierceable, explode, divide, id); 
    }
    
    [ClientRpc]
    public void RpcAttackAreaCircle(NetworkInstanceId id, Vector3 position, float radius, float time)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController) return;
        AttackArea.CreateCircle(position, radius, time);
    }
    
    [ClientRpc]
    public void RpcEffect(NetworkInstanceId id, Vector3 position, string effect, float destroyTime)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController) return;
        if (destroyTime != 0)
            Destroy(Instantiate(Resources.Load("Effects/" + effect), position, Quaternion.identity), destroyTime);
        else
            Instantiate(Resources.Load("Effects/" + effect), position, Quaternion.identity);
    }

    #endregion

    #region SniperSkill
    
    [ClientRpc]
    public void RpcHideSkillCloakingEffect(NetworkInstanceId id, bool toggle)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController) return;
        foreach (SkinnedMeshRenderer renderer in playerController.Renderers)
        {
            renderer.material.shader = Shader.Find(toggle ? "Atlas/Cloaking" : "Atlas/Full(Silhouette, Normal)");
        }
    }

    [ClientRpc]
    public void RpcPacifistSkill(NetworkInstanceId id, NetworkInstanceId targetEnemyID)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        Enemy enemy = ClientScene.FindLocalObject(targetEnemyID).GetComponent<Enemy>();
        if (playerController == GameManager.inst.playerController || enemy == null) return;
        GameObject line = new GameObject("BulletLine");
        LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.material = Resources.Load<Material>("Materials/BulletLine");
        lineRenderer.material.color = new Color(1, 1, 0);
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.SetPosition(0, playerController.transform.position);
        lineRenderer.SetPosition(1, enemy.transform.position);
        StartCoroutine("PacifistSkillFadeOut", line);
    }
    
    IEnumerator PacifistSkillFadeOut(GameObject obj)
    {
        Color oriColor = obj.GetComponent<Renderer>().material.color;
        for (int i = 0; i < 20; ++i)
        {
            obj.GetComponent<Renderer>().material.color = new Color(oriColor.r, oriColor.g, oriColor.b, (20 - i) / 20f);
            yield return new WaitForSeconds(1 / 60f);
        }
        Destroy(obj);
    }

    #endregion
    
    #region GuardSkill
    
    [ClientRpc]
    public void RpcCreateField(NetworkInstanceId id, Perk perk, float time, float scale)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController) return;
        Field field = Instantiate(Resources.Load<Field>("Prefabs/Field"), playerController.transform.position, Quaternion.identity);
        field.transform.localScale *= scale;
        Destroy(field.gameObject, time);
    }
    
    [ClientRpc]
    public void RpcConquerorArea(NetworkInstanceId id, float time, float radius)
    {
        PlayerController playerController = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController) return;
        GameObject area = new GameObject("area");
        MeshFilter areaMeshFilter = area.AddComponent<MeshFilter>();
        MeshRenderer areaMeshRenderer = area.AddComponent<MeshRenderer>();
        Mesh areaMesh = new Mesh();
        areaMeshFilter.mesh = areaMesh;
        areaMeshRenderer.material = Resources.Load<Material>("Materials/Circle64");
        areaMesh.DrawCircle(radius, 360);
        StartCoroutine(FollowConquerorArea(playerController.transform, area, time));
    }

    IEnumerator FollowConquerorArea(Transform target, GameObject area, float time)
    {
        float timer = 0;
        while (true)
        {
            if (timer >= time)
                break;

            area.transform.position = target.position;
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(area);
    }
    
    #endregion

    #region DoctorSkill
    [ClientRpc]
    public void RpcInjectorThrowSkill(NetworkInstanceId id, Vector3 force, float damage, bool fragile, bool adrenaline, bool poisonGas, bool crazy)
    {
        PlayerController pc = ClientScene.FindLocalObject(id).GetComponent<PlayerController>();
        if (pc == GameManager.inst.playerController)
            return;
        Vector3 playerPos = pc.transform.position;
        GameObject injectorPrefab = Resources.Load<GameObject>("Prefabs/Bullets/Injector");
        Injector injector = GameObject.Instantiate(injectorPrefab, playerPos + new Vector3(0, 0.5f, 0), GameManager.inst.playerController.transform.rotation).GetComponent<Injector>();
        injector.Init(damage, force, pc.netId, fragile, adrenaline, poisonGas, crazy);
    }
    #endregion

    #region GameFlow

    [ClientRpc]
    public void RpcGameOver()
    {
        GameState.Transition(GameState.over);
    }
    
    #endregion
    
    #region Miscellaneous

    [ClientRpc]
    public void RpcPlaySound(NetworkInstanceId localPlayerID, NetworkInstanceId targetID, string audioID)
    {
        PlayerController playerController = ClientScene.FindLocalObject(localPlayerID).GetComponent<PlayerController>();
        if (playerController == GameManager.inst.playerController) return;
        SoundManager.inst.PlaySFX(ClientScene.FindLocalObject(targetID), audioID);
    }
    
    #endregion

}