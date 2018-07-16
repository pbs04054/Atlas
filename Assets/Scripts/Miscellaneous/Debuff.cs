using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debuff
{

    public enum DebuffTypes
    {
        Weakness, Stun, Confusion, Bleeding, Fracture, Fear, Poisoned
    }

    public DebuffTypes DebuffType { get; private set; }
    public float Time { get; private set; }
    
    public Debuff(DebuffTypes debuffType, float time)
    {
        DebuffType = debuffType;
        Time = time;
    }

}

public class DebuffComponent : MonoBehaviour
{

    public delegate void DebuffEvent(DebuffComponent debuff);
    public DebuffEvent OnDebuffStart;
    public DebuffEvent OnDebuffEnd;

    public Debuff Debuff { get; private set; }
    public float Timer { get; private set; }

    void Awake()
    {
        OnDebuffStart = delegate { };
        OnDebuffEnd = delegate { };
    }

    public DebuffComponent Init(Debuff debuff)
    {
        Debuff = debuff;
        Timer = Debuff.Time;
        StartCoroutine("DebuffUpdator");
        return this;
    }

    IEnumerator DebuffUpdator()
    {
        OnDebuffStart(this);
        while (true)
        {
            if (Timer <= 0)
                break;

            Timer -= Time.deltaTime;
            yield return null;
        }
        OnDebuffEnd(this);
        Destroy(this);
    }

    public void Remove()
    {
        Timer = 0;
    }

}