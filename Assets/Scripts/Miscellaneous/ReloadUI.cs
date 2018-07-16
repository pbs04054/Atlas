using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReloadUI : WorldFollowUI
{

    Player targetPlayer;
    Image reloadFillImage;
    
    public static ReloadUI Create(Player player)
    {
		return Instantiate(Resources.Load<ReloadUI>("Prefabs/UI/Reload"), GameObject.Find("FollowHolder").transform).Init(player);
    }

    protected override void Awake()
    {
        base.Awake();
        reloadFillImage = transform.GetChild(0).GetComponent<Image>();
    }

    public ReloadUI Init(Player player)
    {
        targetPlayer = player;
        Init(player.transform, 100);
        StartCoroutine("ReloadUIUpdator");
        return this;
    }

    IEnumerator ReloadUIUpdator()
    {
        while (true)
        {
            if (!targetPlayer.IsAlive)
                break;
            float reloadPercent = GunState.reload.reloadPercent;
            if (reloadPercent >= 1)
                break;

            UpdateUIPosition();
            reloadFillImage.fillAmount = reloadPercent;
            yield return null;
        }
        Stop();
    }

}