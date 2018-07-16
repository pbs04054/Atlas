using UnityEngine;
using System.Collections;

public class WorldFollowUI : MonoBehaviour
{

    [SerializeField]
    protected Transform target;

    [SerializeField]
    protected float offset;

    protected RectTransform canvas, rectTransform;

    protected virtual void Awake()
    {
        canvas = GameObject.Find("InGameUIManager").GetComponent<RectTransform>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Init(Transform target, float offset)
    {
        this.target = target;
        this.offset = offset;
    }

    public void UpdateUIPosition()
    {
        if (target != null && canvas != null && rectTransform != null)
            WorldToViewport.WorldToUIPosition(canvas, rectTransform, target, offset);
    }

    public void Stop()
    {
        Destroy(gameObject);
    }

}
