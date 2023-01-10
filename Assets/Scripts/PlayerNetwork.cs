using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private NetworkVariable<float> moveSpeed = new NetworkVariable<float>(10f);
    [SerializeField] private Vector3 clientTransformVector = new Vector3();
    [SerializeField] private Vector3 hostTransformVector = new Vector3();
    [SerializeField] private NetworkVariable<float> serverTime = new NetworkVariable<float>(0f);

    private void Update()
    {
        if (IsServer) 
        { 
            serverTime.Value = Time.deltaTime;
        }
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
            hostTransformVector = moveDirection;

        } else if (IsClient && IsLocalPlayer)
        {
            float moveX = 0f;
            float moveY = 0f;

            if(Input.GetKey(KeyCode.W)) moveY = +1f;
            if(Input.GetKey(KeyCode.S)) moveY = -1f;
            if(Input.GetKey(KeyCode.A)) moveX = -1f;
            if(Input.GetKey(KeyCode.D)) moveX = +1f;

            Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;
            clientTransformVector = moveDirection;
        }

    }

    void FixedUpdate()
    {
        if (IsServer && IsLocalPlayer)
        {
            HostMovement(hostTransformVector);
        }
        if (IsClient && IsLocalPlayer) 
        {
            ClientMovementServerRpc(clientTransformVector);
        }
    }

    public void HostMovement(Vector3 moveDirection)
    { 
        transform.position += moveDirection * moveSpeed.Value * serverTime.Value;
    }

    [ServerRpc]
    public void ClientMovementServerRpc(Vector3 moveDirection)
    {
        transform.position += moveDirection * moveSpeed.Value * serverTime.Value;
    }
}
