using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : WorldFollowUI
{

    Enemy targetEnemy;
    Image hpBarImage;

    public static HPBar Create(Enemy enemy)
    {
        return Instantiate(Resources.Load<HPBar>("Prefabs/UI/HPBar"), GameObject.Find("FollowHolder").transform).Init(enemy);
    }

    protected override void Awake()
    {
        base.Awake();    
        hpBarImage = GetComponent<Image>();
    }

    public HPBar Init(Enemy enemy)
    {
        targetEnemy = enemy;
        Init(enemy.transform, 50);
        StartCoroutine("HPBarUpdator");
        return this;
    }

    IEnumerator HPBarUpdator()
    {
        while (true)
        {
            if (!targetEnemy.IsAlive)
                break;
            UpdateUIPosition();
            hpBarImage.fillAmount = targetEnemy.CurHealth / targetEnemy.MaxHealth;
            yield return null;
        }
        Stop();
    }


}
