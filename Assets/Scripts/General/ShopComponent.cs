using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopComponent : MonoBehaviour{

    //public GameObject model; 3D 모델 일단 현재는 보류

    public GunInfo info;
    public Text nameText, priceText;
    public void ShopComponentStart(GunInfo info)
    {
        nameText.text = info.name;
        priceText.text = info.price.ToString();
        this.info = info;
    }
    
    public void OnShopComponentClicked()
    {
        GameManager.inst.shopManager.highLightedComponent = this;
    }

    public void HighlightComoponent()
    {
        //For Test
        GetComponent<Image>().color = new Color(0, 0.9f, 0.9f);
    }

    public void DehighLighComponent()
    {
        GetComponent<Image>().color = new Color(1, 1, 1);
    }
}
