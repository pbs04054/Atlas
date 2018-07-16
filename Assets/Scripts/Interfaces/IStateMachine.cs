using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStateMachine {

    void Enter();
    void Exit();
    void StateUpdate();
}
