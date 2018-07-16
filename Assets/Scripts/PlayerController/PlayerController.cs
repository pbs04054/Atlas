using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour
{

	public Player player;
	public PlayerCommandHub playerCommandHub = null;
	public Gun gun; //guns[0] is main, guns[1] is sub
	public bool isMain = true; //true if player holds main weapon, false if player holds sub weapon

	public SkillManager sm;
	private Vector2 moveDir;
	public PlayerKeyControl playerKey;
	public ClassController classController;
    public Animator animator;
    public NetworkAnimator networkAnimator;

	public GameObject[] bulletPrefabs = new GameObject[3];
	public int bulletPrefabIndex = 0;
    public float interactDistance;

    public AudioClip rollSFX;
    [SerializeField] AudioClip[] steps;
    [SerializeField] GameObject shield;
    Coroutine stepUpdator;

    [SyncVar] bool disableMouseLook;
    [SyncVar] bool disableMove;
    [SyncVar] bool disableShot;
    [SyncVar] bool disableSpecialMove;
    
    public bool DisableSpecialMove { get { return disableSpecialMove; } set { disableSpecialMove = value; } }
    public bool DisalbeMouseLook { get { return disableMouseLook;} set { disableMouseLook = value; } }
    public bool DisableMove { get { return disableMove; } set { disableMove = value; } }
    public bool DisableShot { get { return disableShot; } set { disableShot = value; } }

    private Vector2 moveDirection = new Vector2();
    public bool isMoving { get { return moveDirection.magnitude != 0; } }
    //Test Variables
    public Transform gunFirePoint;

    [SerializeField]
    public List<int> expInfo;

    public event EventHandler KillEnemy;
    public event EventHandler GunShot;

    private IInteractor closestInteractor = null;

    public Queue<Perk> PerkAvailable = new Queue<Perk>();

    public SkinnedMeshRenderer[] Renderers { get; private set; }

    void Awake()
    {
        player = gameObject.GetComponent<Player>();
        playerCommandHub = GetComponent<PlayerCommandHub>();
        playerKey = PlayerKeyControl.inst;
        expInfo = JsonHelper.LoadExpInfo();
        sm = gameObject.AddComponent<SkillManager>();
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        Renderers = new[]
        {
            transform.Find("Model").Find("Mesh").GetComponent<SkinnedMeshRenderer>()
        };
    }
	
	void Start ()
	{
	    switch (player.playerClass)
	    {
	        case PlayerClass.ASSAULT:
	            classController = gameObject.AddComponent<AssaultController>();
	            Debug.Log("AssaultController");
	            break;
	        case PlayerClass.SNIPER:
	            classController = gameObject.AddComponent<SniperController>();
	            Debug.Log("SniperController");
	            break;
	        case PlayerClass.GUARD:
	            classController = gameObject.AddComponent<GuardController>();
	            break;
	        case PlayerClass.DOCTOR:
	            classController = gameObject.AddComponent<DoctorController>();
	            break;
	    }
	    
        if (!isLocalPlayer)
        {
	        if(isServer)
		        player.playerInitialize();
            return;
        }
	    
        GameManager.inst.playerController = this;
        player.playerInitialize();

		PlayerState.Transition(PlayerState.idle);
        GameManager.inst.shopManager.ShopInitialize();
        sm.SkillManagerInit();
    }

	public void PlayerControllerUpdate()
	{
	    if (!isLocalPlayer)
	        return;
	    if(PlayerState.curState != null)
		    PlayerState.curState.StateUpdate();
		if (gun != null)
			gun.GunUpdate();
        sm.SkillManagerUpdate();

        IInteractor temp = FindClosestInteractor();
        if (closestInteractor != temp)
        {
            if (closestInteractor != null)
                closestInteractor.DeHighLighting();
            closestInteractor = temp;
            if (closestInteractor != null)
                closestInteractor.HighLighting();
        }
        
	    animator.SetFloat("Velocity", player.Agent.velocity.sqrMagnitude);
	    animator.SetFloat("MoveX", Vector3.Cross(player.Agent.velocity.normalized, transform.forward).y);
	    animator.SetFloat("MoveY", Vector3.Dot(player.Agent.velocity.normalized, transform.forward));
	    if(isMain && gun != null && gun.shotSFX != null)
	        animator.SetInteger("GunType", 2);
	    else if(!isMain && gun != null && gun.shotSFX != null)
	        animator.SetInteger("GunType", 1);
	    else
	        animator.SetInteger("GunType", 0);
	    animator.SetBool("IsFatal", !player.IsAlive);
	}


    public void PlayerMovementControl()
    {	   
        if (!isLocalPlayer)
            return;
        if (DisableMove == false && player.IsStun == false)
        {
            Vector2 moveDir = new Vector2();
            if (Input.GetKey(playerKey.up))
            {
                moveDir += player.IsConfusion == false ? new Vector2(1, 1) : new Vector2(-1, -1);
            }
            else if (Input.GetKey(playerKey.down))
            {
                moveDir += player.IsConfusion == false ? new Vector2(-1, -1) : new Vector2(1, 1);
            }
            if (Input.GetKey(playerKey.left))
            {
                moveDir += player.IsConfusion == false ? new Vector2(-1, 1) : new Vector2(1, -1);
            }
            else if (Input.GetKey(playerKey.right))
            {
                moveDir += player.IsConfusion == false ? new Vector2(1, -1) : new Vector2(-1, 1);
            }
            moveDir = moveDir.normalized;
            moveDirection = moveDir;

            if (Input.GetKey(playerKey.roll) && PlayerState.curState == PlayerState.idle)
            {
                StartCoroutine(Rolling(moveDir));
                return;
            }
            else
            {
                player.Move(moveDir);
            }
        }
        else
        {
            moveDirection = new Vector2();
            player.Move(Vector2.zero);
        }

        if (DisalbeMouseLook == false)
            LookAt();
    }

    public void GunControl()
    {
        if (!isLocalPlayer)
            return;
        if (DisableShot)
            return;

        if (Input.GetMouseButton(0)) //L Click
        {
            if (gun != null)
            {
                gun.GunShot();
            }
        }

        if (Input.GetKeyDown(playerKey.reload))
        {
            if (gun != null)
            {
                gun.Reload();
            }
        }

        if (Input.GetKeyDown(playerKey.switchWeapon))
        {
            if (GunState.curState != GunState.idle)
                return;
            SwitchWeapon();
        }
    }

    public void SKillControl()
    {
        if (!isLocalPlayer)
            return;
        if (Input.GetKeyDown(playerKey.skill1))
        {
            if (sm.classSkill.skills[0].useable)
                sm.classSkill.skills[0].Use();
        }
        else if (Input.GetKeyDown(playerKey.skill2))
        {
            if (sm.classSkill.skills[1].useable)
                sm.classSkill.skills[1].Use();
        }
        else if (Input.GetKeyDown(playerKey.skill3))
        {
            if (sm.classSkill.skills[2].useable)
                sm.classSkill.skills[2].Use();
        }
        else if (Input.GetKeyDown(playerKey.skill4))
        {
            if (sm.classSkill.skills[3].useable)
                sm.classSkill.skills[3].Use();
        }
    }

    public void InteractControl()
    {
        if (!isLocalPlayer)
            return;
        if (Input.GetKeyDown(playerKey.interact))
        {
            if (closestInteractor != null)
                closestInteractor.Interact();
        }
    }

    public void OtherControl()
    {
        if (!isLocalPlayer)
            return;
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (PerkAvailable.Count != 0)
            {
                GameManager.inst.inGameUIManager.OpenPerkSelection(PerkAvailable.Dequeue());
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            player.IsRadarActive = !player.IsRadarActive;
        }
    }

    private IInteractor FindClosestInteractor()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, interactDistance);
        IInteractor interactor = null;
        float minDistance = interactDistance;
        foreach (var col in cols)
        {
            if (col.gameObject == gameObject)
                continue;
            if (col.GetComponent<IInteractor>() == null)
                continue;
            else
            {
                float dis;
                if ((dis = Vector3.Distance(transform.position, col.transform.position)) < minDistance)
                {
                    minDistance = dis;
                    interactor = col.GetComponent<IInteractor>();
                }
            }
        }
        return interactor;
    }

    void LookAt()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        float rayDistance;
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            Vector3 heightCorrectPoint = new Vector3(point.x, transform.position.y, point.z);
            Quaternion rotation = Quaternion.LookRotation(heightCorrectPoint - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 10f);
            //transform.LookAt(heightCorrectPoint);
        }
    }

    public void SwitchWeapon()
	{
		isMain = !isMain;
        if (isMain)
        {
            gun.GunStart(gun.mainGunInfo);
            gun.GunInterfacesStart(gun.mainGunInfo);
            GameManager.inst.playerController.playerCommandHub.CmdSetPlayerBullet(gun.mainGunInfo, GameManager.inst.playerController.netId);
        }
        else
        {
            gun.GunStart(gun.subGunInfo);
            gun.GunInterfacesStart(gun.subGunInfo);
            GameManager.inst.playerController.playerCommandHub.CmdSetPlayerBullet(gun.subGunInfo, GameManager.inst.playerController.netId);
        }
        GunState.Transition(GunState.idle);
        GameManager.inst.inGameUIManager.UpdateBullet();
        SoundManager.inst.PlaySFX(gameObject, Resources.Load<AudioClip>("Sounds/WeaponSwap"));
        DoWeaponCheck();
	}

    public void DoWeaponCheck()
    {
        networkAnimator.SetTrigger("DoWeaponCheck");
    }

	public IEnumerator Rolling(Vector2 dir)
	{
        //gameObject.layer = LayerMask.NameToLayer("Rolling");
        //SoundManager.inst.PlaySFX(Camera.main.gameObject, rollSFX);
        PlayerState.Transition(PlayerState.roll);
        if (dir.magnitude != 0)
		{
		    transform.LookAt(transform.position + new Vector3(dir.x, 0, dir.y));
            networkAnimator.SetTrigger("DoRolling");
            animator.ResetTrigger("DoRolling");
            SoundManager.inst.PlaySFX(gameObject, netId, rollSFX);
            if (gun != null)
			    gun.CurBulletSpreadAngle = Mathf.Min(gun.CurBulletSpreadAngle + 50, gun.MaxBulletSpreadAngle);
			dir = dir.normalized;
			for (int i = 0; i < 30; ++i)
			{
				GetComponent<NavMeshAgent>().velocity = new Vector3(dir.x, 0, dir.y) * (30 - i);
				yield return new WaitForSeconds(1 / 60f);
			}
			GetComponent<Rigidbody>().velocity = new Vector2();
		}
		PlayerState.Transition(PlayerState.idle);
		//gameObject.layer = LayerMask.NameToLayer("Player");

	}

    [Command]
    public void CmdRescuePlayer(NetworkInstanceId playerNetId)
    {
        Player player = GameManager.inst.players[playerNetId];
	    player.RescueTimer = 0;
        Debug.Log(player.MaxHealth / 2);
        player.GetDamaged(-player.MaxHealth / 2);
    }

    public virtual void OnKillEnemy()
    {
        if (KillEnemy != null)
        {
            KillEnemy(this, EventArgs.Empty);
        }
    }

    public virtual void OnGunShot()
    {
        if (GunShot != null)
        {
            GunShot(this, EventArgs.Empty);
        }
    }

    public void EnableShot(float time)
    {
        StartCoroutine(EnableShooting(time));
    }

    private IEnumerator EnableShooting(float time)
    {
        yield return new WaitForSeconds(time);
        DisableShot = false;
    }

    void Step()
    {
        if (stepUpdator == null)
            stepUpdator = StartCoroutine("StepUpdator");
    }

    IEnumerator StepUpdator()
    {
        SoundManager.inst.PlaySFX(gameObject, steps.Random(), 0.3f);
        if(isMain && gun != null)
            yield return new WaitForSeconds(0.20f);
        else if(!isMain && gun != null)
            yield return new WaitForSeconds(0.20f);
        else
            yield return new WaitForSeconds(0.25f);
        stepUpdator = null;
    }

    public void ActivateShield(bool active)
    {
        shield.SetActive(active);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}