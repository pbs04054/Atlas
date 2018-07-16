using UnityEngine;
using UnityEngine.Networking;

public class PlayerCommandHub : NetworkBehaviour
{
    
    [Command]
    public void CmdSetPlayerBullet(GunInfo info, NetworkInstanceId id)
    {
        FindObjectOfType<ServerSimulator>().RpcSetPlayerBullet(info, id);
    }

    [Command]
    public void CmdSetInfiniteBullet(NetworkInstanceId id, bool value)
    {
        Player player = NetworkServer.FindLocalObject(id).GetComponent<Player>();
        if (player != null)
        {
            player.Gun.InfiniteBullet = value;
        }
    }
    
    [Command]
    public void CmdGiveDamageWithoutCollide(NetworkInstanceId playerNetID, NetworkInstanceId enemyNetID, float damage)
    {
        Enemy target = NetworkServer.FindLocalObject(enemyNetID).GetComponent<Enemy>();
        if (target != null)
        {
            target.GetDamaged(damage,playerNetID);
        }
    }

    [Command]
    public void CmdGivePlayerDamage(NetworkInstanceId playerNetID, float damage)
    {
        Player player = NetworkServer.FindLocalObject(playerNetID).GetComponent<Player>();
        if (player != null)
        {
            player.GetDamaged(damage);
        }
    }

    [Command]
    public void CmdKnockBackEnemy(NetworkInstanceId enemyNetID, Vector3 dir)
    {
        Enemy target = NetworkServer.FindLocalObject(enemyNetID).GetComponent<Enemy>();
        if (target != null)
        {
            target.transform.position += dir;
            target.Agent.velocity = new Vector3();
        }
    }

    [Command]
    public void CmdKinfeShot(Vector3 point, NetworkInstanceId id)
    {
        
    }

    [Command]
    public void CmdAddDebuff(NetworkInstanceId id, Debuff.DebuffTypes debuffTypes, float time)
    {
        Actor targetActor = NetworkServer.FindLocalObject(id).GetComponent<Actor>();
        targetActor.AddDebuff(new Debuff(debuffTypes, time));
    }

    [Command]
    public void CmdAddBuff(NetworkInstanceId id, Buff buff)
    {
        Actor targetActor = NetworkServer.FindLocalObject(id).GetComponent<Actor>();
        targetActor.AddBuff(buff);
    }

    [Command]
    public void CmdRemoveBuff(NetworkInstanceId id, Buff buff)
    {
        Actor targetActor = NetworkServer.FindLocalObject(id).GetComponent<Actor>();
        targetActor.RemoveBuff(buff);
    }

    [Command]
    public void CmdEnterSpecialMove(NetworkInstanceId id)
    {
        ClassController classController = NetworkServer.FindLocalObject(id).GetComponent<ClassController>();
        classController.EnterSpecialMove(id);
    }

    [Command]
    public void CmdExitSpecialMove(NetworkInstanceId id)
    {
        ClassController classController = NetworkServer.FindLocalObject(id).GetComponent<ClassController>();
        classController.ExitSpecialMove(id);
    }

    [Command]
    public void CmdCameraSizeChange(NetworkIdentity identity, float time, float size)
    {
        FindObjectOfType<ServerSimulator>().TargetCameraSizeChange(identity.connectionToClient, time, size);
    }
    
    [Command]
    public void CmdGiveHealToPlayer(NetworkInstanceId playerID, float healAmount, bool forceHeal)
    {
        NetworkServer.FindLocalObject(playerID).GetComponent<Player>().GetHealed(healAmount, forceHeal);
    }

    [Command]
    public void CmdGiveStaminaToPlayer(NetworkInstanceId playerID, float staminaAmount, bool forceGive)
    {
        NetworkServer.FindLocalObject(playerID).GetComponent<Player>().GetStaminaHealed(staminaAmount, forceGive);
    }

    [Command]
    public void CmdActiveStaminaShield(NetworkInstanceId playerID, bool active)
    {
        FindObjectOfType<ServerSimulator>().RpcActiveStaminaShield(playerID, active);
    }
    
    #region AssultSkill

    [Command]
    public void CmdKnifeThrowSkill(NetworkInstanceId id, Vector3 force, bool pierceable, bool explode, bool divide )
    {
        FindObjectOfType<ServerSimulator>().RpcKnifeThrowSkill(id, force, pierceable, explode, divide);
    }

    [Command]
    public void CmdAttackAreaCircle(NetworkInstanceId id, Vector3 position, float radius, float time)
    {
        FindObjectOfType<ServerSimulator>().RpcAttackAreaCircle(id, position, radius, time);
    }

    [Command]
    public void CmdEffect(NetworkInstanceId id, Vector3 position, string effect, float destroyTime)
    {
        FindObjectOfType<ServerSimulator>().RpcEffect(id, position, effect, destroyTime);
    }
    
    [Command]
    public void CmdPlayerFatal()
    {
        GameManager.inst.CheckIsGameOver();
    }

    [Command]
    public void CmdPlayerDead(NetworkInstanceId playerID)
    {
        //FindObjectOfType<ServerSimulator>().RpcPlayerDead(playerID);
        GameManager.inst.CheckIsGameOver();
        NetworkServer.FindLocalObject(playerID).GetComponent<NetworkAnimator>().SetTrigger("DoDeath");
        NetworkServer.FindLocalObject(playerID).GetComponent<Player>().isDead = true;
    }

    [Command]
    public void CmdPlayerRevive(NetworkInstanceId playerID)
    {
        GameObject playerObj = NetworkServer.FindLocalObject(playerID);
        playerObj.GetComponent<Player>().GetHealed(playerObj.GetComponent<Player>().MaxHealth, true);
        NetworkServer.FindLocalObject(playerID).GetComponent<NetworkAnimator>().SetTrigger("DoAlive");
        playerObj.GetComponent<Player>().isDead = false;
    }

    [Command]
    public void CmdUseMoney(NetworkInstanceId playerID, float amount)
    {
        NetworkServer.FindLocalObject(playerID).GetComponent<Player>().Money -= amount;
    }

    #endregion
    
    #region SniperSkill

    [Command]
    public void CmdHideSkillCloakingEffect(NetworkInstanceId id, bool toggle)
    {
        FindObjectOfType<ServerSimulator>().RpcHideSkillCloakingEffect(id, toggle);
    }
    
    [Command]
    public void CmdPacifistSkill(NetworkInstanceId id, NetworkInstanceId targetEnemyID)
    {
        FindObjectOfType<ServerSimulator>().RpcPacifistSkill(id, targetEnemyID);
    }

    #endregion
    
    #region GuardSkill

    [Command]
    public void CmdCreateField(NetworkInstanceId id, Perk perk, float time, float scale)
    {
        PlayerController playerController = NetworkServer.FindLocalObject(id).GetComponent<PlayerController>();
        if (GameManager.inst.playerController.netId != id)
        {
            Field field = Instantiate(Resources.Load<Field>("Prefabs/Field"), playerController.transform.position, Quaternion.identity);
            field.Init(id, time, perk, scale);
        }
        else
        {
            FindObjectOfType<ServerSimulator>().RpcCreateField(GameManager.inst.playerController.netId, perk, time, scale);   
        }            
    }

    [Command]
    public void CmdConquerorArea(NetworkInstanceId id, float time, float radius)
    {
        FindObjectOfType<ServerSimulator>().RpcConquerorArea(id, time, radius);
    }
    
    #endregion

    #region DoctorSkill

    [Command]
    public void CmdInjectorThrow(NetworkInstanceId id, Vector3 force, float damage, bool fragile, bool adrenaline, bool poisonGas, bool crazy)
    {
        FindObjectOfType<ServerSimulator>().RpcInjectorThrowSkill(id, force, damage, fragile, adrenaline, poisonGas, crazy);
    }

    [Command]
    public void CmdSpawnPoisonGas(NetworkInstanceId playerID, float time, float radius, float damage, bool isHeal, Vector3 position)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>("Prefabs/BioGas"), position, Quaternion.identity);
        obj.GetComponent<BioGas>().Init(time, damage, radius, isHeal, playerID);
        NetworkServer.Spawn(obj);
    }
    #endregion

    #region Enemy

    [Command]
    public void CmdDreadnoughtExplosionEffect(NetworkInstanceId id, Vector3 position)
    {
        FindObjectOfType<ServerSimulator>().RpcDreadnoughtExplosionEffect(id, position);
    }

    #endregion
    
    #region Miscellaneous

    [Command]
    public void CmdPlaySound(NetworkInstanceId localPlayerID, NetworkInstanceId targetID, string audioID)
    {
        FindObjectOfType<ServerSimulator>().RpcPlaySound(localPlayerID, targetID, audioID);
    }
    
    #endregion
}