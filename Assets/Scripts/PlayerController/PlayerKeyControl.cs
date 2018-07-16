using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerKeyControl : SingletonBehaviour<PlayerKeyControl> {

    public KeyCode up, down, left, right;
    public KeyCode skill1, skill2, skill3, skill4;
    public KeyCode switchWeapon;
    public KeyCode reload, roll, interact;

    private void Start()
    {
        SetStatic();
        up = KeyCode.W;
        down = KeyCode.S;
        left = KeyCode.A;
        right = KeyCode.D;
        switchWeapon = KeyCode.Tab;
        interact = KeyCode.E;
        skill1 = KeyCode.Alpha1;
        skill2 = KeyCode.Alpha2;
        skill3 = KeyCode.Alpha3;
        skill4 = KeyCode.Alpha4;
        reload = KeyCode.R;
        roll = KeyCode.LeftShift;
    }
}
