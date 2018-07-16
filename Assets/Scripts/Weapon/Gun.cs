using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayerController = UnityEngine.Networking.PlayerController;

public interface GunFire
{
    void GunShot();
}

public class ShotgunFire : GunFire
{
    public ShotgunFire(int bps)
    {
        bulletsPerShot = bps;
    }
    public int bulletsPerShot;

    public void GunShot()
    {
        Gun gun = GameManager.inst.playerController.gun;
        Vector3 playerPos = GameManager.inst.playerController.transform.position; // transform을 호출하는 것은 비용이 크니 미리 캐싱함.
        Plane plane = new Plane(Vector3.up, playerPos);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float dist;
        plane.Raycast(ray, out dist);
        Vector3 point = ray.GetPoint(dist);
        point = point - playerPos;
        point *= 100f;
        gun.LocalShotgunShot(point);
        gun.CmdShotgunShot(point, GameManager.inst.playerController.netId);
        gun.CurBulletSpreadAngle = Mathf.Min(gun.MaxBulletSpreadAngle, gun.CurBulletSpreadAngle + gun.Recoil);
    }
}

public class RifleFire : GunFire
{
    public void GunShot()
    {
        Gun gun = GameManager.inst.playerController.gun;
        Vector3 playerPos = GameManager.inst.playerController.transform.position;
        Plane plane = new Plane(Vector3.up, playerPos);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float dist;
        plane.Raycast(ray, out dist);
        Vector3 point = ray.GetPoint(dist);
        point = point - playerPos;
        point *= 100f;
        gun.LocalRifleShot(point);
        gun.CmdRifleShot(point, GameManager.inst.playerController.netId);
        gun.CurBulletSpreadAngle = Mathf.Min(gun.MaxBulletSpreadAngle, gun.CurBulletSpreadAngle + gun.Recoil);
    }
}

public class BioFire : GunFire
{
    public void GunShot()
    {
        Gun gun = GameManager.inst.playerController.gun;
        Vector3 playerPos = GameManager.inst.playerController.transform.position;
        Plane plane = new Plane(Vector3.up, playerPos);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float dist;
        plane.Raycast(ray, out dist);
        Vector3 point = ray.GetPoint(dist);
        gun.LocalBioShot(point, JanusSkill.isHeal, GameManager.inst.playerController.sm.classSkill.skills[3].isUsing);
        gun.CmdBioShot(point, GameManager.inst.playerController.netId, JanusSkill.isHeal, GameManager.inst.playerController.sm.classSkill.skills[3].isUsing);
        gun.CurBulletSpreadAngle = Mathf.Min(gun.MaxBulletSpreadAngle, gun.CurBulletSpreadAngle + gun.Recoil);
    }
}

public interface GunReload
{

}

public class CartridgeReload : GunReload
{

}

public class BulletReload : GunReload
{

}

public interface GunFireMode
{
    void GunShot();
}

public class BurstFire : GunFireMode
{
    public GunFire gunFire;

    public BurstFire(GunFire gf)
    {
        gunFire = gf;
    }
    public void GunShot()
    {

    }
}

public class OneShotFire : GunFireMode
{
    public GunFire gunFire;
    public OneShotFire(GunFire gf)
    {
        gunFire = gf;
    }

    public void GunShot()
    {
        gunFire.GunShot();
    }
}

public class AutoFire : GunFireMode
{
    public GunFire gunFire;
    public AutoFire(GunFire gf)
    {
        gunFire = gf;
    }

    public void GunShot()
    {
        gunFire.GunShot();
    }
}

public class Gun : NetworkBehaviour
{

    #region Public Variables

    public int id { get; private set; }
    public List<Buff> Buffs { get; private set; }
    public float GunShotInterval { get; private set; }
    public float MinBulletSpreadAngle { get; private set; }
    public float MaxBulletSpreadAngle { get; private set; }

    public int RemainedBullets
    {
        get { return remainBullets; }
        set
        {
            remainBullets = value;
            GameManager.inst.inGameUIManager.UpdateBullet();
        }
    }

    public int MaxBullets { get; private set; }
    public float ReloadInterval { get; private set; }
    public bool ShotModeChangeable { get; private set; }

    public float BulletSpeed
    {
        get { return bulletSpeed; }
        private set { bulletSpeed = value; }
    }

    public float CurBulletSpreadAngle
    {
        get { return Mathf.Clamp(curBulletSpreadAngle, MinBulletSpreadAngle, MaxBulletSpreadAngle); }
        set { curBulletSpreadAngle = Mathf.Clamp(value, MinBulletSpreadAngle, MaxBulletSpreadAngle); }
    }

    public float Recoil { get; private set; }
    public float Damage { get; private set; }

    public float BaseDamage
    {
        get { return baseDamage; }
        set
        {
            baseDamage = value;
            CalculateBuff();
        }
    }

    public int BulletPerShot
    {
        get { return bulletPerShot; }
        private set { bulletPerShot = value; }
    }

    public bool InfiniteBullet { get { return infiniteBullet; } set { infiniteBullet = value; } }

    #endregion

    #region Private Variables

    [SyncVar] float baseGunShotInterval;
    [SyncVar] float baseMinBulletSpreadAngle;
    [SyncVar] float baseMaxBulletSpreadAngle;
    [SyncVar] float baseMaxBullets;
    [SyncVar] float baseDamage;
    [SyncVar] float curBulletSpreadAngle;
    [SyncVar] float curDamage;
    [SyncVar] int remainBullets;
    [SyncVar] float reloadTimer;
    [SyncVar] float recoverRecoilPerSec;
    [SyncVar] float bulletSpeed;
    [SyncVar] int bulletPerShot;
    [SyncVar] bool infiniteBullet;

    #endregion

    public AudioClip shotSFX;
    public AudioClip reloadStartSFX, reloadingSFX, reloadCompleteSFX;

    public GunFire gunFire;
    public GunReload gunReload;
    public GunFireMode gunFireMode;

    public GameObject bulletPrefab;

    public event EventHandler reloadEvent;

    public GunInfo mainGunInfo = new GunInfo(), subGunInfo = new GunInfo();

    protected virtual void Awake()
    {
        Buffs = new List<Buff>();
    }

    protected virtual void Start()
    {

    }

    /// <summary>
    /// 총의 능력치를 적용합니다.
    /// </summary>
    /// <param name="gss">gunShotInterval</param>
    /// <param name="mxb">maxBullets</param>
    /// <param name="bbsd">baseBulletSpreadAngle</param>
    /// <param name="mnbsd">minBulletSpreadAngle</param>
    /// <param name="mxbsd">maxBulletSpreadAngle</param>
    /// <param name="rrp">recoverRecoilPerSec</param>
    /// <param name="rc">recoil</param>
    /// <param name="rs">reloadInterval</param>
    /// <param name="bs">bulletSpeed</param>
    /// <param name="bullet">BulletPrefabName</param>
    protected void GunStart(float gss, int mxb, float bbsd, float mnbsd, float mxbsd, float rrp, float rc, float rs, float bs, string bullet)
    {
        baseGunShotInterval = gss;
        baseMaxBullets = RemainedBullets = mxb;
        CurBulletSpreadAngle = bbsd;
        baseMinBulletSpreadAngle = mnbsd;
        baseMaxBulletSpreadAngle = mxbsd;
        recoverRecoilPerSec = rrp;
        Recoil = rc;
        ReloadInterval = rs;
        BulletSpeed = bs;
        bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullets/" + bullet);
        GunState.Transition(GunState.idle);
        CalculateBuff();
    }

    public void GunStart(GunInfo info)
    {
        id = info.id;
        baseGunShotInterval = info.gunShotInterval;
        baseMaxBullets = RemainedBullets = info.maxBullets;
        CurBulletSpreadAngle = info.baseBulletSpreadAngle;
        baseMinBulletSpreadAngle = info.minBulletSpreadAngle;
        baseMaxBulletSpreadAngle = info.maxBulletSpreadAngle;
        baseDamage = info.attack;
        recoverRecoilPerSec = info.recoverRecoilPerSec;
        Recoil = info.recoil;
        ReloadInterval = info.reloadInterval;
        BulletSpeed = info.bulletSpeed;
        BulletPerShot = info.bulletsPerShot;
        bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullets/" + info.bullet);

        string name = info.name;
        shotSFX = Resources.Load<AudioClip>("Sounds/Shot/Shot - " + name);
        reloadStartSFX = Resources.Load<AudioClip>("Sounds/ReloadStart/ReloadStart - " + name);
        reloadingSFX = Resources.Load<AudioClip>("Sounds/Reloading/Reloading - " + name);
        reloadCompleteSFX = Resources.Load<AudioClip>("Sounds/ReloadComplete/ReloadComplete - " + name);

        CalculateBuff();
    }

    public void GunInterfacesStart(GunInfo info)
    {
        switch (info.gunFire)
        {
            case Shot.Rifle:
                gunFire = new RifleFire();
                break;
            case Shot.Shotgun:
                gunFire = new ShotgunFire(info.bulletsPerShot);
                break;
            case Shot.Bio:
                gunFire = new BioFire();
                break;
        }

        switch (info.gunFireMode)
        {
            case ShotMode.One:
                gunFireMode = new OneShotFire(gunFire);
                break;
            case ShotMode.Burst:
                gunFireMode = new BurstFire(gunFire);
                break;
            case ShotMode.Auto:
                gunFireMode = new AutoFire(gunFire);
                break;
        }

        switch (info.gunReload)
        {
            case ReloadMode.Bullet:
                gunReload = new BulletReload();
                break;
            case ReloadMode.Cartridge:
                gunReload = new CartridgeReload();
                break;
        }
    }

    public virtual void GunUpdate()
    {
        if (GunState.curState == null)
            return;
        GunState.curState.StateUpdate();
        CurBulletSpreadAngle = Mathf.Max(MinBulletSpreadAngle, CurBulletSpreadAngle - recoverRecoilPerSec * Time.deltaTime);
    }

    public void GunShot(bool forceFire = false)
    {
        if (forceFire)
        {
            GunState.Transition(GunState.fire);
            GameManager.inst.playerController.networkAnimator.SetTrigger("DoFire");
        }

        if ((InfiniteBullet || RemainedBullets > 0) && (GunState.curState == GunState.idle || GunState.curState == GunState.reload && gunReload.GetType() == typeof(BulletReload)))
        {
            GunState.Transition(GunState.fire);
            if(!InfiniteBullet)
                RemainedBullets--;
            GameManager.inst.playerController.networkAnimator.SetTrigger("DoFire");
        }
    }

    public void Reload()
    {
        if (GunState.curState == GunState.idle && RemainedBullets != MaxBullets)
        {
            GunState.Transition(GunState.reload);
            if (reloadEvent != null)
            {
                reloadEvent(this, EventArgs.Empty);
            }
        }
    }

    public virtual Buff[] AddBuff(params Buff[] buffs)
    {
        foreach (Buff buff in buffs)
        {
            Buffs.Add(buff);
        }

        CalculateBuff();
        return buffs;
    }

    public virtual void RemoveBuff(params Buff[] buffs)
    {
        foreach (Buff buff in buffs)
        {
            Buffs.Remove(buff);
        }

        CalculateBuff();
    }

    void CalculateBuff()
    {
        float minBulletSpreadAngle = 0;
        float maxBulletSpreadAngle = 0;
        float gunShotInterval = 0;
        float maxBullets = 0;
        float damage = 0;

        foreach (Buff buff in Buffs)
        {
            minBulletSpreadAngle += buff.GetBuff(Buff.Stat.BulletSpreadAngle);
            maxBulletSpreadAngle += buff.GetBuff(Buff.Stat.BulletSpreadAngle);
            gunShotInterval += buff.GetBuff(Buff.Stat.AttackSpeed);
            maxBullets += buff.GetBuff(Buff.Stat.MaxBullets);
            damage += buff.GetBuff(Buff.Stat.Damage);
        }

        MinBulletSpreadAngle = baseMinBulletSpreadAngle * (1 + minBulletSpreadAngle * 0.01f);
        MaxBulletSpreadAngle = baseMaxBulletSpreadAngle * (1 + maxBulletSpreadAngle * 0.01f);
        GunShotInterval = baseGunShotInterval * (1 + gunShotInterval * 0.01f);
        MaxBullets = Mathf.RoundToInt(baseMaxBullets * (1 + maxBullets));
        Damage = baseDamage * (1 + damage * 0.01f);
    }

    [Command]
    public void CmdRifleShot(Vector3 point, NetworkInstanceId id)
    {
        FindObjectOfType<ServerSimulator>().RpcRifleShot(point, id);
    }

    public void LocalRifleShot(Vector3 point)
    {
        global::PlayerController playerController = GetComponent<global::PlayerController>();
        Vector3 playerPos = playerController.transform.position;
        Vector3 dir;

        if (playerController.DisalbeMouseLook == false)
        {
            Vector3 randomPos = point.RotatePointAroundPivot(playerPos,
                new Vector3(0,
                    UnityEngine.Random.Range(-playerController.gun.CurBulletSpreadAngle / 2.000f,
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
        bullet.Init(playerController.gun.Damage, dir * playerController.gun.BulletSpeed, playerController.netId);
    }

    [Command]
    public void CmdShotgunShot(Vector3 point, NetworkInstanceId id)
    {
        FindObjectOfType<ServerSimulator>().RpcShotgunShot(point, id);
    }

    public void LocalShotgunShot(Vector3 point)
    {
        global::PlayerController playerController = GetComponent<global::PlayerController>();
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
            bullet.Init(playerController.gun.Damage, dir * playerController.gun.BulletSpeed, playerController.netId);
            Destroy(obj, 1);
        }
    }

    [Command]
    public void CmdBioShot(Vector3 point, NetworkInstanceId id, bool isHeal, bool isMania)
    {
        FindObjectOfType<ServerSimulator>().RpcBioShot(point, id, isHeal, isMania);
    }

    public void LocalBioShot(Vector3 point, bool isHeal, bool isMania)
    {
        global::PlayerController playerController = GetComponent<global::PlayerController>();
        Vector3 playerPos = playerController.transform.position;
        Vector3 randomPos = point.RotatePointAroundPivot(playerPos,new Vector3(0, UnityEngine.Random.Range(-playerController.gun.CurBulletSpreadAngle / 2.000f,playerController.gun.CurBulletSpreadAngle / 2.000f), 0));
        GameObject obj = Instantiate(playerController.gun.bulletPrefab, playerController.gunFirePoint.position,playerController.transform.rotation);
        BioBullet bullet = obj.GetComponent<BioBullet>();
        bullet.Init(playerController.gun.Damage, randomPos, playerController.netId, isHeal, isMania);
    }

    [Command]
    public void CmdGunStart(GunInfo info)
    {
        GunStart(info);
    }
    
}
