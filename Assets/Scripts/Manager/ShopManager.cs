using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SortOption
{
    ID,
    NAME,
    PRICEL, //낮은가격순
    PRICEH  //높은가격순
}

public class ShopManager : MonoBehaviour {

    public List<GunInfo> mainWeaponList, subWeaponList;
    public List<GameObject> shopComponents = new List<GameObject>();
    public List<GameObject> poolList = new List<GameObject>();
    private int count = 0;
    public GameObject shopComponentPrefab;
    public RectTransform shopComponentParent;
    private ShopComponent _highLightedComponent = null;
    public ShopComponent highLightedComponent {
        get { return _highLightedComponent; }
        set
        {
            if (_highLightedComponent != null)
                _highLightedComponent.DehighLighComponent();
            _highLightedComponent = value;
            highLightedComponentView.ShopComponentStart(value.info);
            _highLightedComponent.HighlightComoponent();
        }
    }
    public ShopComponent highLightedComponentView;

    [SerializeField]
    GameObject gunShopPanel, hospitalPanel;
    [SerializeField]
    Text healPriceText, hospitalHealthText;
    [SerializeField]
    Slider hospitalHealthBar;

    public GameObject CreateNew()
    {
        GameObject obj = Instantiate(shopComponentPrefab, shopComponentParent);
        obj.name = "ShopComponent" + count++.ToString();
        return obj;
    }
	public void PushToPool(GameObject item)
    {
        poolList.Add(item);
        item.SetActive(false);
    }
    public GameObject PopFromPool()
    {
        if (poolList.Count == 0)
        {
            poolList.Add(CreateNew());
        }
        GameObject obj = poolList[poolList.Count - 1];
        poolList.RemoveAt(poolList.Count - 1);
        return obj;
    }

    public void ShopInitialize()
    {
        //JsonHelper.ClassGunList cgl = JsonHelper.LoadGunInfo(PlayerController.inst.player.playerClass);
        JsonHelper.ClassGunList cgl = JsonHelper.LoadGunInfo(GameManager.inst.playerController.player.playerClass);
        mainWeaponList = cgl.mainWeaponList;
        subWeaponList = cgl.subWeaponList;

        mainWeaponList.Sort(CompareInfoByPrice);
        subWeaponList.Sort(CompareInfoByPrice);

        int i;
        for (i = 0; i < mainWeaponList.Count; ++i)
        {
            if (mainWeaponList[i].price >= 0)
                break;
        }
        if (i > 0)
            mainWeaponList.RemoveRange(0, i);
        for (i = 0; i < subWeaponList.Count; ++i)
        {
            if (subWeaponList[i].price >= 0)
                break;
        }
        if (i > 0)
            subWeaponList.RemoveRange(0, i);

        foreach (var item in mainWeaponList)
        {
            GameObject obj = CreateNew();
            obj.GetComponent<ShopComponent>().ShopComponentStart(item);
            shopComponents.Add(obj);
        }
        gameObject.GetComponent<Canvas>().enabled = false;
    }
    public void OpenShop()
    {
        gameObject.GetComponent<Canvas>().enabled = true;
        PlayerState.Transition(PlayerState.wait);
        GameManager.inst.inGameUIManager.GetComponent<Canvas>().enabled = false;
        Cursor.visible = true;
        //TODO : PlayerState, GunState transition to StateShopping
    }

    public void CloseShop()
    {
        gameObject.GetComponent<Canvas>().enabled = false;
        PlayerState.Transition(PlayerState.idle);
        GameManager.inst.inGameUIManager.GetComponent<Canvas>().enabled = true;
        Cursor.visible = false;
        //TODO : PlayerState, GunState transition to Idle
    }

    public void OnExitButtonClicked()
    {
        CloseShop();
    }

    public void OnBuyButtonClicked()
    {
        if (highLightedComponent == null)
            return;
        GunInfo info = highLightedComponent.info;
        if (GameManager.inst.playerController.player.Money >= info.price)
        {
            //info.attack = (GameManager.inst.playerController.abilityDictionary[AbilityType.ATTACK] as AttackAbility).CalculateAttack(info.attack);

            if (mainWeaponList.Contains(info))
            {
                GameManager.inst.playerController.gun.mainGunInfo = info;
                GameManager.inst.playerController.isMain = true;
            }

            else
            {
                GameManager.inst.playerController.gun.subGunInfo = info;
                GameManager.inst.playerController.isMain = false;
            }
            GameManager.inst.playerController.gun.GunStart(info);
            GameManager.inst.playerController.gun.CmdGunStart(info);
            GameManager.inst.playerController.gun.GunInterfacesStart(info);
            GameManager.inst.playerController.DoWeaponCheck();
            GameManager.inst.playerController.playerCommandHub.CmdSetPlayerBullet(info, GameManager.inst.playerController.netId);

            GunState.Transition(GunState.idle);
            GameManager.inst.playerController.playerCommandHub.CmdUseMoney(GameManager.inst.playerController.netId, highLightedComponent.info.price);
            GameManager.inst.inGameUIManager.UpdateBullet();
        }
        //highLightedComponent 에서 정보를 가져와 PlayerController Gun의 정보를 바꿈
    }

    public void OnMainWeaponButtonClicked()
    {
        gunShopPanel.SetActive(true);
        hospitalPanel.SetActive(false);

        if (_highLightedComponent != null)
            _highLightedComponent.DehighLighComponent();
        _highLightedComponent = null;

        if (mainWeaponList.Count - shopComponents.Count >= 0)
        {
            int count = mainWeaponList.Count - shopComponents.Count;
            for (int i = 0; i < count; ++i)
            {
                GameObject obj = PopFromPool();
                obj.SetActive(true);
                shopComponents.Add(obj);
            }
        }
        else
        {
            int count = shopComponents.Count - mainWeaponList.Count;
            for (int i = 0; i < count; ++i)
            {
                PushToPool(shopComponents[shopComponents.Count - 1]);
                shopComponents.Remove(shopComponents[shopComponents.Count - 1]);
            }
        }

        for (int i = 0; i < mainWeaponList.Count; ++i)
        {
            Debug.Log(mainWeaponList[i].price);
            shopComponents[i].GetComponent<ShopComponent>().ShopComponentStart(mainWeaponList[i]);
        }
    }

    public void OnSubWeaponButtonClicked()
    {
        gunShopPanel.SetActive(true);
        hospitalPanel.SetActive(false);
        
        if (_highLightedComponent != null)
            _highLightedComponent.DehighLighComponent();
        _highLightedComponent = null;

        if (subWeaponList.Count - shopComponents.Count >= 0)
        {
            int count = subWeaponList.Count - shopComponents.Count;
            for (int i = 0; i < count; ++i)
            {
                GameObject obj = PopFromPool();
                obj.SetActive(true);
                shopComponents.Add(obj);
            }
        }
        else
        {
            int count = shopComponents.Count - subWeaponList.Count;
            for (int i = 0; i < count; ++i)
            {
                PushToPool(shopComponents[shopComponents.Count - 1]);
                shopComponents.Remove(shopComponents[shopComponents.Count - 1]);
            }
        }

        for (int i = 0; i < subWeaponList.Count; ++i)
        {
            shopComponents[i].GetComponent<ShopComponent>().ShopComponentStart(subWeaponList[i]);   
        }
    }

    public void OnHospitalButtonClicked()
    {
        hospitalPanel.SetActive(true);
        gunShopPanel.SetActive(false);
    }

    public void OnHealButtonClicked(float percent)
    {
        float price = percent * 10;
        if (GameManager.inst.playerController.player.Money < price || GameManager.inst.playerController.player.CurHealth == GameManager.inst.playerController.player.MaxHealth)
            return;
        GameManager.inst.playerController.playerCommandHub.CmdUseMoney(GameManager.inst.playerController.netId, price);
        GameManager.inst.playerController.playerCommandHub.CmdGiveHealToPlayer(GameManager.inst.playerController.netId, GameManager.inst.playerController.player.MaxHealth * (percent / 100), false);
    }

    public void OnPointerEnterHealButton(int price)
    {
        healPriceText.text = price.ToString();
    }

    public void OnReviveAllButtonClicked(float price)
    {
        if (GameManager.inst.playerController.player.Money < price)
            return;
        foreach(var player in GameManager.inst.players)
        {
            if (player.Value.isDead)
            {
                GameManager.inst.playerController.playerCommandHub.CmdPlayerRevive(player.Key);
            }
            else
            {
                GameManager.inst.playerController.playerCommandHub.CmdGiveHealToPlayer(player.Key, player.Value.MaxHealth, true);
            }
        }
        GameManager.inst.playerController.playerCommandHub.CmdUseMoney(GameManager.inst.playerController.netId, price);
    }

    public void UpdateHospitalHealthBar()
    {
        hospitalHealthBar.value = GameManager.inst.playerController.player.CurHealth / GameManager.inst.playerController.player.MaxHealth;
        hospitalHealthText.text = GameManager.inst.playerController.player.CurHealth.ToString("0") + "/" + GameManager.inst.playerController.player.MaxHealth.ToString("0");
    }

    public void Sorting(SortOption sortOption)
    {
        switch (sortOption)
        {
            case SortOption.NAME:
                shopComponents.Sort(CompareByName);
                break;
            case SortOption.PRICEL:
                shopComponents.Sort(CompareByPrice);
                break;
            case SortOption.PRICEH:
                shopComponents.Sort(CompareByPrice);
                shopComponents.Reverse();
                break;
        }
    }

    public static int CompareByName(GameObject A, GameObject B)
    {
        return string.Compare(A.GetComponent<ShopComponent>().nameText.text, B.GetComponent<ShopComponent>().nameText.text);
    }

    public static int CompareByPrice(GameObject A, GameObject B)
    {
        return string.Compare(A.GetComponent<ShopComponent>().priceText.text, B.GetComponent<ShopComponent>().priceText.text);
    }

    public static int CompareInfoByPrice(GunInfo A, GunInfo B)
    {
        if (A.price > B.price)
        {
            return 1;
        }
        else if (A.price == B.price)
        {
            return 0;
        }
        else
        {
            return -1;
        }
    }
}
