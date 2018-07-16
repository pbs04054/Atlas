using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Networking;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class Actor : NetworkBehaviour
{
    #region Stats

    [SyncVar] private float defense;
    [SyncVar] private float speed;
    [SyncVar] private float avoidrate;

    public float Defense
    {
        get { return defense; }
        private set { defense = value; }
    }

    public float Speed
    {
        get { return speed; }
        protected set { speed = value; }
    }

    public float AvoidRate
    {
        get { return avoidrate; }
        private set { avoidrate = value; }
    }

    #endregion
    
    public NavMeshAgent Agent { get; private set; }
    public Rigidbody RigidBody { get; private set; }
    public List<Buff> Buffs { get; private set; }

    public DebuffComponent[] Debuffs
    {
        get { return GetComponents<DebuffComponent>(); }
    }

    [SerializeField] protected float baseDefense, baseSpeed, baseAvoidRate;

    public SyncListInt DebuffStack= new SyncListInt();
    

    public virtual void Awake()
    {
        RigidBody = GetComponent<Rigidbody>();
        Agent = GetComponent<NavMeshAgent>();
        Buffs = new List<Buff>();
        DebuffStack.InitializeBehaviour(this, (int)netId.Value);
        foreach (Debuff.DebuffTypes debuffType in System.Enum.GetValues(typeof(Debuff.DebuffTypes)))
        {
            DebuffStack.Add(0);
        }
    }

    protected virtual void Start()
    {
        CalculateBuff();
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
        float defense = 0;
        float speed = 0;
        float avoidRate = 0;

        foreach (Buff buff in Buffs)
        {
            defense += buff.GetBuff(Buff.Stat.Defense);
            speed += buff.GetBuff(Buff.Stat.Speed);
            avoidRate += buff.GetBuff(Buff.Stat.AvoidRate);
        }

        Defense = baseDefense * (1 + defense * 0.01f);
        Speed = baseSpeed * (1 + speed * 0.01f);
        AvoidRate = avoidRate * (1 + avoidRate * 0.01f);
    }
    
    public virtual Debuff[] AddDebuff(params Debuff[] debuffs)
    {
        foreach (Debuff debuff in debuffs)
        {
            DebuffComponent debuffComponent = gameObject.AddComponent<DebuffComponent>();
            debuffComponent.OnDebuffStart += OnDebuffStart;
            debuffComponent.OnDebuffEnd += OnDebuffEnd;
            debuffComponent.Init(debuff);
        }

        return debuffs;
    }

    public virtual void RemoveAllDebuffs()
    {
        foreach (DebuffComponent debuff in Debuffs)
        {
            debuff.Remove();
        }
    }

    public void Move(Vector2 dir)
    {
        //transform.position += new Vector3 (dir.x * speed, 0 , dir.y * speed);
        //RigidBody.MovePosition(RigidBody.transform.position + new Vector3(dir.x * speed, 0, dir.y * speed)); //움직일 때 물리 작용을 받기 위해서는 Rigidbody의 메소드를 이용해야 함.
        Agent.velocity = new Vector3(dir.x * Speed, 0, dir.y * Speed); // NavMeshAgent를 이용함.
    }

    void Reset()
    {
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        rigidbody.constraints = (RigidbodyConstraints) 80; //Actor를 상속받는 컴포넌트를 부착 시 Rigidbody의 Rotation X, Z축을 Freeze 함.
        rigidbody.isKinematic = true; // NavMeshAgent와의 사용을 위함.
    }

    [Obsolete]
    public Player[] GetPlayersByRange(float range)
    {
        List<Player> players = new List<Player>();
        Collider[] cols = Physics.OverlapSphere(transform.position, range, 1 << LayerMask.NameToLayer("Player"));
        foreach (Collider col in cols)
        {
            players.Add(col.GetComponent<Player>());
        }

        return players.ToArray();
    }

    public Player GetPlayerByDistance()
    {
        Player[] players = FindObjectsOfType<Player>();
        float distance = float.MaxValue;
        Player target = null;
        foreach (Player player in players)
        {
            if (player.CompareTag("Cloaking") || !player.IsAlive) continue;
            float calculate = Vector3.Distance(transform.position, player.transform.position);
            if (calculate < distance)
            {
                distance = calculate;
                target = player;
            }
        }

        return target;
    }

    [Obsolete]
    public Enemy[] GetEnemiesByRange(float range)
    {
        List<Enemy> enemies = new List<Enemy>();
        Collider[] cols = Physics.OverlapSphere(transform.position, range, 1 << LayerMask.NameToLayer("Enemy"));
        foreach (Collider col in cols)
        {
            enemies.Add(col.GetComponent<Enemy>());
        }

        return enemies.ToArray();
    }

    public T[] GetObjectsByArc<T>(float radius, float angle)
    {
        List<T> objects = new List<T>();
        for (float theta = (90 - angle * 0.5f) / 180f * Mathf.PI; theta < (90 + angle * 0.5f) / 180f * Mathf.PI; theta += 0.025f)
        {
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);
            Vector3 dir = transform.rotation * new Vector3(x, 0, z);

            objects.AddRange
            (
                Physics.RaycastAll(new Ray(transform.position, dir), radius)
                    .Select(hit => hit.collider.GetComponent<T>())
                    .Where(t => t != null)
            );
        }

        return objects.ToArray();
    }

    public T[] GetObjectsByCircle<T>(float radius)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius);
        return (from col in cols let t = col.GetComponent<T>() where t != null && col.gameObject != gameObject select t).ToArray();
    }
    
    public T[] GetObjectsByCircle<T>(Vector3 position, float radius)
    {
        Collider[] cols = Physics.OverlapSphere(position, radius);
        return (from col in cols let t = col.GetComponent<T>() where t != null && col.gameObject != gameObject select t).ToArray();
    }
    
    public T[] GetObjectsByBox<T>(float length, float width)
    {
        Collider[] cols = Physics.OverlapBox(transform.position + transform.forward * length * 0.5f, new Vector3(width * 0.5f, 1f, length * 0.5f), Quaternion.LookRotation(transform.forward));
        return (from col in cols let t = col.GetComponent<T>() where t != null && col.gameObject != gameObject select t).ToArray();
    }

    public virtual void OnDebuffStart(DebuffComponent debuff)
    {
        DebuffStack[(int)debuff.Debuff.DebuffType]++;
    }

    public virtual void OnDebuffEnd(DebuffComponent debuff)
    {
        debuff.OnDebuffStart = null;
        debuff.OnDebuffEnd = null;
        DebuffStack[(int)debuff.Debuff.DebuffType]--;
    }
}