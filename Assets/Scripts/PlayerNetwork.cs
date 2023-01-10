using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private NetworkVariable<float> moveSpeed = new NetworkVariable<float>(3f);
    [SerializeField] private NetworkVariable<float> clientPlayerX = new NetworkVariable<float>(0f);
    [SerializeField] private NetworkVariable<float> clientPlayerY = new NetworkVariable<float>(0f);

    private void Update()
    {
        if (!IsOwner) return;

        if (IsServer && IsLocalPlayer)
        { 
            float moveX = 0f;
            float moveY = 0f;

            if(Input.GetKey(KeyCode.W)) moveY = +1f;
            if(Input.GetKey(KeyCode.S)) moveY = -1f;
            if(Input.GetKey(KeyCode.A)) moveX = -1f;
            if(Input.GetKey(KeyCode.D)) moveX = +1f;

            Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;
            transform.position += moveDirection * moveSpeed.Value * Time.deltaTime;
        } else if (IsClient && IsLocalPlayer)
        {
            float moveX = 0f;
            float moveY = 0f;

            if(Input.GetKey(KeyCode.W)) moveY = +1f;
            if(Input.GetKey(KeyCode.S)) moveY = -1f;
            if(Input.GetKey(KeyCode.A)) moveX = -1f;
            if(Input.GetKey(KeyCode.D)) moveX = +1f;

            Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;

            //Debug.Log(moveDirection);

            MovementServerRpc(moveDirection);
        }

    }

    [ServerRpc]
    public void MovementServerRpc(Vector3 moveDirection)
    {
        Debug.Log("Tried Server RPC");
        transform.position += moveDirection * moveSpeed.Value * Time.deltaTime;
    }
}
