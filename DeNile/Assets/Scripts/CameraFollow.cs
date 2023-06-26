using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float followSpeed = 0.1f;
    [SerializeField] private Vector3 cameraOffset;
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject); //Sets the camera to not be destroyed for changeding between levels
    }

    // Update is called once per frame
    void Update()
    {
        //Sets the camera's pos to the player's with an offset on the Z axis
        transform.position = Vector3.Lerp(transform.position, PlayerController.Instance.transform.position + cameraOffset, followSpeed);
    }
}
