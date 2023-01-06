using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetwork : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;

    private void Update()
    {
        if (Input.GetKey(KeyCode.D)) 
        {
            transform.position += Vector3.right * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A)) 
        {
            transform.position += Vector3.right * -moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.W)) 
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S)) 
        {
            transform.position += Vector3.up * -moveSpeed * Time.deltaTime;
        }
    }
}
