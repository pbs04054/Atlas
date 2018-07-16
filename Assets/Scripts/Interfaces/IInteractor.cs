using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractor {
    void Interact();
    /// <summary>
    /// 유저가 Interact하게 될 오브젝트를 하이라이팅한다.
    /// </summary>
    void HighLighting();
    void DeHighLighting();
}
