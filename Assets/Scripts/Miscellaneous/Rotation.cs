using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotation : MonoBehaviour {

    float y = 0;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        y += 90 * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0, y, 0);
	}
}
