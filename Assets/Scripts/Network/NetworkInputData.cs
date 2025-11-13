using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;
    public bool jumpPressed;
}
