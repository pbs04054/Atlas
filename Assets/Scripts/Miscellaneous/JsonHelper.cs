using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net.Configuration;

[System.Serializable]
public enum GunType
{
    Handgun, //권총
    AssaultRifle, //돌격소총
    SniperRifle, //저격소총
    Shotgun, //샷건
    Machinegun, //기관총
    BioThrower,
    SubMachineGun, //기관단총
}

public enum Shot
{
    Shotgun,
    Rifle,
    Bio
}

public enum ShotMode
{
    One,
    Burst,
    Auto
}

public enum ReloadMode
{
    Bullet,
    Cartridge
}

[System.Serializable]
public class GunInfo
{
    public int id;
    public string name;
    public GunType type;
    public int price;
    public int attack;
    public float gunShotInterval;
    public int maxBullets;
    public float baseBulletSpreadAngle;
    public float minBulletSpreadAngle;
    public float maxBulletSpreadAngle;
    public float recoverRecoilPerSec;
    public float recoil;
    public float reloadInterval;
    public float bulletSpeed;
    public int bulletsPerShot;
    public Shot gunFire;
    public ShotMode gunFireMode;
    public ReloadMode gunReload;
    public string bullet;
    //public AudioClip shotSFX;
    //public AudioClip reloadStartSFX, reloadingSFX, reloadCompleteSFX;

    public GunInfo() { }

    public GunInfo(int id, string name, GunType type, int price, int atk, float gsi, int mb, float bbsa, float mnbsa, float mxbsa, float rrps, float rc, float ri, float bs, int bps, Shot gf, ShotMode gfm, ReloadMode gr, string bullet)
    {
        this.id = id;
        this.name = name;
        this.type = type;
        this.price = price;
        attack = atk;
        gunShotInterval = gsi;
        maxBullets = mb;
        baseBulletSpreadAngle = bbsa;
        minBulletSpreadAngle = mnbsa;
        maxBulletSpreadAngle = mxbsa;
        recoverRecoilPerSec = rrps;
        recoil = rc;
        reloadInterval = ri;
        bulletSpeed = bs;
        bulletsPerShot = bps;
        gunFire = gf;
        gunFireMode = gfm;
        gunReload = gr;
        this.bullet = bullet;
    }
}

[System.Serializable]
public class GunList
{
    public List<GunInfo> Shotgun = new List<GunInfo>();
    public List<GunInfo> Handgun = new List<GunInfo>();
    public List<GunInfo> SniperRifle = new List<GunInfo>();
    public List<GunInfo> AssaultRifle = new List<GunInfo>();
    public List<GunInfo> SubMachinegun = new List<GunInfo>();
    public List<GunInfo> Machinegun = new List<GunInfo>();
    public List<GunInfo> BioThrower = new List<GunInfo>();
    public List<GunInfo> GetList(GunType type)
    {
        switch (type)
        {
            case GunType.Handgun:
                return Handgun;
            case GunType.SubMachineGun:
                return SubMachinegun;
            case GunType.AssaultRifle:
                return AssaultRifle;
            case GunType.SniperRifle:
                return SniperRifle;
            case GunType.Shotgun:
                return Shotgun;
            case GunType.Machinegun:
                return Machinegun;
            case GunType.BioThrower:
                return BioThrower;
        }
        return null;
    }
}

[System.Serializable]
public struct WaveGroup
{
    public int id;
    public int amount;
    public float multiple; //능력치 계수

    public WaveGroup(int id, int amount, float multiple)
    {
        this.id = id;
        this.amount = amount;
        this.multiple = multiple;
    }
}

[System.Serializable]
public class WaveInfo
{
    public List<WaveGroup> wave = new List<WaveGroup>();
    public float waveInterval;
    public WaveInfo(List<WaveGroup> list, float interval)
    {
        wave = list;
        waveInterval = interval;
    }
}

[System.Serializable]
public class WaveList
{
    public List<WaveInfo> waves = new List<WaveInfo>();
}

[System.Serializable]
public class ExpInfo
{
    public List<int> neededEXP = new List<int>();
}

[System.Serializable]
public class ClassPerkList
{
    public PerkInfo[] perkList = new PerkInfo[16];
}

[System.Serializable]
public class PerkInfo
{
    public string name, description;
}

public class JsonHelper : MonoBehaviour {

    GunList gl = new GunList();
    WaveList wl = new WaveList();
    ExpInfo ei = new ExpInfo();
    ClassPerkList cpl = new ClassPerkList();

	void Start () {
        for (int i = 0; i < 16; ++i)
        {
            cpl.perkList[i] = new PerkInfo();
        }
        Debug.Log(JsonUtility.ToJson(cpl, true));
        File.WriteAllText(Application.dataPath + "/Resources/AssaultPerk.json", JsonUtility.ToJson(cpl, true));
    }

    public void MakeExpInfoToJson()
    {
        File.WriteAllText(Application.dataPath + "/Resources/ExpData.json", JsonUtility.ToJson(ei, true));
    }

    public static List<int> LoadExpInfo()
    {
        return JsonUtility.FromJson<ExpInfo>(Resources.Load<TextAsset>("ExpData").text).neededEXP;
    }

    public void MakeGunInfoToJson()
    {
        File.WriteAllText(Application.dataPath + "/Resources/GunData.json", JsonUtility.ToJson(gl, true));
    }

    public struct ClassGunList
    {
        public List<GunInfo> mainWeaponList;
        public List<GunInfo> subWeaponList;
        public ClassGunList(List<GunInfo> main, List<GunInfo> sub)
        {
            mainWeaponList = main;
            subWeaponList = sub;
        }
    }
    public static ClassGunList LoadGunInfo(PlayerClass cls)
    {
        string data = Resources.Load<TextAsset>("GunData").text;
        GunList gl = JsonUtility.FromJson<GunList>(data);
        //FindSoundsOfGun(ref gl);

        ClassGunList cgl = new ClassGunList();
        switch (cls)
        {
            case PlayerClass.ASSAULT:
                cgl.mainWeaponList = new List<GunInfo>(gl.AssaultRifle);
                cgl.mainWeaponList.AddRange(gl.Shotgun);
                cgl.subWeaponList = new List<GunInfo>(gl.Handgun);
                cgl.subWeaponList.AddRange(gl.SubMachinegun);
                break;
            case PlayerClass.SNIPER:
                cgl.mainWeaponList = gl.SniperRifle;
                cgl.subWeaponList = new List<GunInfo>(gl.Handgun);
                cgl.subWeaponList.AddRange(gl.SubMachinegun);
                break;
            case PlayerClass.GUARD:
                cgl.mainWeaponList = gl.Shotgun;
                cgl.mainWeaponList.AddRange(gl.Machinegun);
                cgl.subWeaponList = gl.Handgun;
                break;
            case PlayerClass.DOCTOR:
                cgl.mainWeaponList = gl.BioThrower;
                cgl.subWeaponList = gl.Handgun;
                break;
        }
        return cgl;
    }

    //public static void FindSoundsOfGun(ref GunList list)
    //{
    //    foreach (var info in list.Handgun)
    //    {
    //        try
    //        {
    //            string name = info.name;
    //            info.shotSFX = Resources.Load<AudioClip>("sounds/" + name + "/shot");
    //            info.reloadStartSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadstart");
    //            info.reloadingSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloading");
    //            info.reloadCompleteSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadcomplete");
    //        }
    //        catch { }
    //    }
    //    foreach (var info in list.Machinegun)
    //    {
    //        try
    //        {
    //            string name = info.name;
    //            info.shotSFX = Resources.Load<AudioClip>("sounds/" + name + "/shot");
    //            info.reloadStartSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadstart");
    //            info.reloadingSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloading");
    //            info.reloadCompleteSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadcomplete");
    //        }
    //        catch { }
    //    }
    //    foreach (var info in list.Shotgun)
    //    {
    //        try
    //        {
    //            string name = info.name;
    //            info.shotSFX = Resources.Load<AudioClip>("sounds/" + name + "/shot");
    //            info.reloadStartSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadstart");
    //            info.reloadingSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloading");
    //            info.reloadCompleteSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadcomplete");
    //        }
    //        catch { }
    //    }
    //    foreach (var info in list.SniperRifle)
    //    {
    //        try
    //        {
    //            string name = info.name;
    //            info.shotSFX = Resources.Load<AudioClip>("sounds/" + name + "/shot");
    //            info.reloadStartSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadstart");
    //            info.reloadingSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloading");
    //            info.reloadCompleteSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadcomplete");
    //        }
    //        catch { }
    //    }
    //    foreach (var info in list.SubMachinegun)
    //    {
    //        try
    //        {
    //            string name = info.name;
    //            info.shotSFX = Resources.Load<AudioClip>("sounds/" + name + "/shot");
    //            info.reloadStartSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadstart");
    //            info.reloadingSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloading");
    //            info.reloadCompleteSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadcomplete");
    //        }
    //        catch { }
    //    }
    //    foreach (var info in list.AssaultRifle)
    //    {
    //        try
    //        {
    //            string name = info.name;
    //            info.shotSFX = Resources.Load<AudioClip>("sounds/" + name + "/shot");
    //            info.reloadStartSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadstart");
    //            info.reloadingSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloading");
    //            info.reloadCompleteSFX = Resources.Load<AudioClip>("sounds/" + name + "/reloadcomplete");
    //        }
    //        catch { }
    //    }
    //}

    public void MakeWaveInfoToJson()
    {
        File.WriteAllText(Application.dataPath + "/Resources/WaveData.json", JsonUtility.ToJson(wl, true));
    }

    public static WaveList LoadWaveList()
    {
        return JsonUtility.FromJson<WaveList>(Resources.Load<TextAsset>("WaveData").text);
    }
}
