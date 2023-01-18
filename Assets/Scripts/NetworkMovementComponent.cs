using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

public class NetworkMovementComponent : NetworkBehaviour
{
    // Referencing the player network script
    [SerializeField] private PlayerNetwork _pn;
    [SerializeField] private float _speed;

    // Code to help debug reconciliation
    // [SerializeField] private MeshFilter _meshFilter;
    // [SerializeField] private Color _color;

    //Tick rate for the server
    private int _tick = 0;
    private float _tickRate = 1f / 60f;
    private float _tickDeltaTime = 0f;

    //Store the sent input
    private const int BUFFER_SIZE = 1024;

    //Creating a new array of sent inputs, with as many elements as the buffer size
    //The buffer size is how many stored ticks of information the array will keep stored
    public InputState[] _inputStates = new InputState[BUFFER_SIZE];

    // An array of transforms with the same buffer size
    public TransformState[] _transformStates = new TransformState[BUFFER_SIZE];

    // So the server can send this information -
    // This will be the latest transform that has been established on the server
    public NetworkVariable<TransformState> ServerTransformState = new NetworkVariable<TransformState>();
    public TransformState _previousTransformState;

    private void OnEnable() 
    {
        // Listening to the variable change, and if it does then call a function
        ServerTransformState.OnValueChanged += OnServerStateChanged;
    }

    //In the future, this is where server reconciliation will be established
    private void OnServerStateChanged(TransformState previousValue, TransformState serverState)
    {
        // Set the previous transform state to be the previous value
        //_previousTransformState = previousValue;

        // if you are not the local player, ignore this code
        if(!IsLocalPlayer) return;

        // The first update should be null, so set the previous state to server state
        if(_previousTransformState == null) 
        {
            _previousTransformState = serverState;
        }

        TransformState calculatedState = _transformStates.First(localState => localState.Tick == serverState.Tick);
        // If the predicted state is not the same as the server state -
        if(calculatedState.Position != serverState.Position) 
        {
            Debug.Log("Correcting Client Position");
            // Teleport the player to the server position
            TeleportPlayer(serverState);

            // Replay the inputs that happen after
            // This grabs all server states from the ticks that happened after
            IEnumerable<InputState> inputs = _inputStates.Where(input => input.Tick > serverState.Tick);
            inputs = from input in inputs orderby input.Tick select input;

            // For each input state within those inputs -
            foreach (var inputState in inputs)
            {
                // Move the player with those missed inputs
                MovePlayer(inputState.movementInput);

                TransformState newTransformState = new TransformState()
                {
                    Tick = inputState.Tick,
                    Position = transform.position,
                    HasStartedMoving = true
                };

                for (int i = 0; i < _transformStates.Length; i++)
                {
                    if(_transformStates[i].Tick == inputState.Tick) 
                    {
                        _transformStates[i] = newTransformState;
                        break;
                    }
                }
            }
        }
    }

    private void TeleportPlayer(TransformState state)
    {   
        // Disable the player network script so that the player cant continue to move
        _pn.enabled = false;
        // Set position to the server state position
        transform.position = state.Position;
        // Enable script again
        _pn.enabled = true;

        // Reset value that was stored in local array for the transform
        for (int i = 0; i < _transformStates.Length; i++)
        {
            if(_transformStates[i].Tick == state.Tick) 
            {
                _transformStates[i] = state;
                break;
            }
        }
    }

    // Enable the PlayerNetwork script controller to call a method for actually moving
    public void ProcessLocalPlayerMovement(Vector3 movementInput)
    {
        // Used to base speed off of server tick rate, which essentially normalizes values across all clients
        _tickDeltaTime += Time.deltaTime;

        // IF we are within the tick rate
        if (_tickDeltaTime > _tickRate)
        {
            // Shows where we are in the buffer size currently
            int bufferIndex = _tick % BUFFER_SIZE;

            // If we are not the server
            if (!IsServer)
            {
                // New server RPC which takes the current tick and the input
                MovePlayer(movementInput);
                MovePlayerServerRpc(_tick, movementInput);

            } else 
            {
                MovePlayer(movementInput);

                TransformState state = new TransformState()
                {
                    Tick = _tick,
                    Position = transform.position,
                    HasStartedMoving = true
                };

                _previousTransformState = ServerTransformState.Value;
                ServerTransformState.Value = state;
            }

            InputState inputState = new InputState()
            {
                Tick = _tick,
                movementInput = movementInput,
            };

            TransformState transformState = new TransformState()
            {
                Tick = _tick,
                Position = transform.position,
                HasStartedMoving = true
            };

            _inputStates[bufferIndex] = inputState;
            _transformStates[bufferIndex] = transformState;

            _tickDeltaTime -= _tickRate;
            _tick++;


        }
    }

    public void ProcessSimulatedPlayerMovement()
    {
        _tickDeltaTime += Time.deltaTime;
        if (_tickDeltaTime > _tickRate)
        {
            if (ServerTransformState.Value.HasStartedMoving)
            {
                transform.position = ServerTransformState.Value.Position;
            }

            _tickDeltaTime -= _tickRate;
            _tick++;
        }
    }

    private void MovePlayer(Vector3 movementInput)
    {
        transform.position += movementInput * _speed * _tickRate;
    }

    // Server RPC that processes player movement
    [ServerRpc]
    private void MovePlayerServerRpc(int tick, Vector3 movementInput)
    {   
        // This will move the player
        MovePlayer(movementInput);
        // This is also where we would call functions that do other player actions, such as rotating the player

        // Set the state of the position that we arrived to, at this specific tick
        TransformState state = new TransformState()
        {
            Tick = tick,
            Position = transform.position,
            HasStartedMoving = true
        };

        //Store that state as our previous state
        _previousTransformState = ServerTransformState.Value;
        ServerTransformState.Value = state;
    }

    // Code to debug reconciliation
    // private void OnDrawGizmos() 
    // {
    //     if (ServerTransformState.Value != null)
    //     {
    //         Gizmos.color = _color;
    //         Gizmos.DrawMesh(_meshFilter.mesh, ServerTransformState.Value.Position);
    //     }
    // }

}
