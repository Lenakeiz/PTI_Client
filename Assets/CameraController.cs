using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour {

    private Camera m_Camera;

	// Use this for initialization
	void Start () {
        m_Camera = GetComponent<Camera>();
    }
	
	// Update is called once per frame
	void Update () {

#if UNITY_EDITOR || UNITY_STANDALONE
        float input = Input.GetAxis("Mouse ScrollWheel");
        m_Camera.orthographicSize += input;

        m_Camera.orthographicSize = Mathf.Max(0.4f, m_Camera.orthographicSize);
#endif

    }
}
