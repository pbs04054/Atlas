using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CameraController : MonoBehaviour
{

    [SerializeField]
    Transform target = null;
    Vector3 velocity = Vector3.zero;

    void Start()
    {
        
    }

    public void CameraStart()
    {
        target = GameManager.inst.playerController.player.transform;
        transform.position = target.position;
    }

    void LateUpdate()
    {
        if (target == null)
            return;
        Vector3 point = Camera.main.WorldToViewportPoint(target.position);
        Vector3 delta = target.position - Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, point.z));
        Vector3 destination = transform.position + delta;
        destination.y = 15;
        transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, 0.1f);
    }

    public void CameraShaking(float amount, float time)
    {
        StartCoroutine(CamShaking(amount, time));
    }

    IEnumerator CamShaking(float amount, float time)
    {
        float oriTime = time, oriAmount = amount;
        while (time > 0)
        {
            Vector3 shakeVec = Random.insideUnitSphere * amount;
            Camera.main.transform.localPosition += shakeVec;
            amount = oriAmount * (time / oriTime);
            time -= Time.deltaTime;
            yield return null;
        }
    }

    public void CameraSizeChange(NetworkIdentity networkIdentity, float time, float size)
    {
        GameManager.inst.playerController.playerCommandHub.CmdCameraSizeChange(networkIdentity, time, size);
    }

    public IEnumerator CamSizeChangeUpdator(float time, float size)
    {
        float oriSize = Camera.main.orthographicSize;
        float oriTime = time;
        while (time > 0)
        {
            Camera.main.orthographicSize = Mathf.Lerp(oriSize, size, (oriTime - time) / oriTime);
            time -= Time.deltaTime;
            yield return null;
        }
        Camera.main.orthographicSize = size;
    }
}
