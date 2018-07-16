using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour, IInteractor {

    [SerializeField]
    GameObject PressE, LightSphere;

    //private bool useable = true;

    private void Start()
    {
        
    }

    public void Interact()
    {
        //if (!useable)
        //    return;
        GameManager.inst.shopManager.OpenShop();
    }

    public void HighLighting()
    {
        //if (useable)
            PressE.SetActive(true);
    }

    public void DeHighLighting()
    {
        PressE.SetActive(false);
    }

    //public void EnableShop(object obj, EventArgs args)
    //{
    //    useable = true;
    //    LightSphere.SetActive(true);
    //}

    //public void DisableShop(object obj, EventArgs args)
    //{
    //    useable = false;
    //    LightSphere.SetActive(false);
    //}
}
