using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunState : IStateMachine {

    public static Gun gun { get { return GameManager.inst.playerController.gun; } }
    public static GunState curState;

    public static GunStatePrePare prepare = new GunStatePrePare();
    public static GunStateIdle idle = new GunStateIdle();
    public static GunStateFire fire = new GunStateFire();
    public static GunStateReload reload = new GunStateReload();

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void StateUpdate() { }
    public virtual void HandleInput() { }

    public static void Transition(GunState nextState)
    {
        if (curState != null)
            curState.Exit();
        curState = nextState;
        nextState.Enter();
    }
}

public class GunStatePrePare : GunState
{
    private float timer;
    public override void Enter()
    {
        timer = 0.5f;
    }
    public override void Exit()
    {
        
    }
    public override void StateUpdate()
    {
        timer -= Time.deltaTime;
        if (timer < 0)
            Transition(idle);
    }
    public override void HandleInput()
    {

    }
}

public class GunStateIdle : GunState
{
    public override void Enter()
    {
        //Debug.Log("GunState : Idle");
    }
    public override void Exit()
    {

    }
    public override void StateUpdate()
    {
        if (gun.RemainedBullets == 0)
        {
            gun.Reload();
        }
    }
}

public class GunStateFire : GunState
{
    public float gunShotTimer;
    public GunFireMode gunFireMode;
    private bool isMouseButtonUp = false;

    public override void Enter()
    {
        SoundManager.inst.PlaySFX(GameManager.inst.playerController.gameObject, GameManager.inst.playerController.netId, gun.shotSFX);
        gunFireMode = gun.gunFireMode;
        gunFireMode.GunShot();
        gunShotTimer = gun.GunShotInterval;
        GameManager.inst.cameraController.CameraShaking(gun.Recoil / 50, gun.GunShotInterval);
        GameManager.inst.playerController.OnGunShot();
    }
    public override void Exit()
    {

    }
    public override void StateUpdate()
    {
        gunShotTimer -= Time.deltaTime;
        if (gunFireMode.GetType() != typeof(AutoFire))
        {
            isMouseButtonUp = !Input.GetMouseButton(0);
        }
        else
        {
            isMouseButtonUp = true;
        }
        if (gunShotTimer < 0 && isMouseButtonUp)
            Transition(idle);
    }
    public override void HandleInput()
    {

    }
}

public class GunStateReload : GunState
{
    public GunReload gunReload;
    public float reloadTimer;
    public float reloadPercent;

    public override void Enter()
    {
        SoundManager.inst.PlaySFX(GameManager.inst.playerController.gameObject, GameManager.inst.playerController.netId, gun.reloadStartSFX);
        gunReload = gun.gunReload;
        reloadTimer = gun.ReloadInterval;
        reloadPercent = 0;
        GameManager.inst.playerController.networkAnimator.SetTrigger("DoReload");
        ReloadUI.Create(GameManager.inst.playerController.player);
        //Debug.Log("GunState : Reload");
    }

    public override void Exit()
    {
        
    }
    public override void StateUpdate()
    {
        reloadTimer -= Time.deltaTime;
        reloadPercent = 1 - reloadTimer / gun.ReloadInterval;
        if (reloadTimer < 0)
        {
            if (gun.RemainedBullets == gun.MaxBullets)
            {
                if (gun.reloadingSFX != null)
                {
                    GameManager.inst.Coroutine(ReloadComplete());
                }
                reloadPercent = 1;
                Transition(idle);
                return;
            }

            if (gunReload.GetType() == typeof(CartridgeReload))
            {
                gun.RemainedBullets = gun.MaxBullets;
                reloadTimer = 0;
            }
            else if (gunReload.GetType() == typeof(BulletReload))
            {
                gun.RemainedBullets++;
                reloadTimer = gun.RemainedBullets != gun.MaxBullets ? gun.ReloadInterval : 0;
                reloadPercent = 0;
            }
            SoundManager.inst.PlaySFX(GameManager.inst.playerController.gameObject, GameManager.inst.playerController.netId, gun.reloadingSFX);
        } 
    }

    private IEnumerator ReloadComplete()
    {
        yield return new WaitForSeconds(0.7f);
        SoundManager.inst.PlaySFX(GameManager.inst.playerController.gameObject, GameManager.inst.playerController.netId, gun.reloadCompleteSFX);
        GameManager.inst.playerController.networkAnimator.SetTrigger("ReloadFinish");
    }
}