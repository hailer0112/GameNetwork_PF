using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class CoinPlayer : NetworkBehaviour
{
    public readonly NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
    public string LastReply { get; private set; } = "No request";
    private CharacterController controller;
    private InputAction move;
    private Vector2 serverInput;
    private float inputAt, sendAt, vertical;
    private int lastRequest = -1;
    [System.NonSerialized] public Vector2? TestInput;

    private void Awake() { controller = GetComponent<CharacterController>(); controller.enabled = false; }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            transform.position = new Vector3(OwnerClientId == NetworkManager.ServerClientId ? -2 : 2, 0, -4);
            Position.Value = transform.position;
            controller.enabled = true;
        }
        else transform.position = Position.Value;
        var renderer = GetComponentInChildren<Renderer>();
        renderer.material.color = IsOwner ? new Color(.1f, .8f, .9f) : new Color(.9f, .35f, .6f);
        if (!IsOwner) return;
        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.Enable();
    }
    private void Update()
    {
        if (!IsSpawned) return;
        if (IsOwner && Time.unscaledTime >= sendAt)
        {
            sendAt = Time.unscaledTime + .05f;
            bool editing = UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() != null;
            MoveServerRpc(TestInput ?? (editing ? Vector2.zero : move.ReadValue<Vector2>()));
        }
        if (IsOwner && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) RequestNearest();
        if (IsServer)
        {
            Vector2 input = Time.unscaledTime - inputAt < .3f ? serverInput : Vector2.zero;
            if (controller.isGrounded && vertical < 0) vertical = -2;
            vertical += -20 * Time.deltaTime;
            controller.Move(new Vector3(input.x * 4, vertical, input.y * 4) * Time.deltaTime);
            Position.Value = transform.position;
        }
        else transform.position = Vector3.Lerp(transform.position, Position.Value, 1 - Mathf.Exp(-20 * Time.deltaTime));
    }
    [ServerRpc]
    private void MoveServerRpc(Vector2 input)
    {
        if (float.IsNaN(input.x) || float.IsNaN(input.y) || float.IsInfinity(input.x) || float.IsInfinity(input.y)) return;
        serverInput = Vector2.ClampMagnitude(input, 1);
        inputAt = Time.unscaledTime;
    }
    public void Request(int id)
    {
        if (!IsSpawned || !IsOwner) return;
        lastRequest = id; CollectServerRpc(id);
    }
    public void Repeat() { Request(lastRequest); }
    public void RequestNearest()
    {
        var state = FindAnyObjectByType<CoinSession>();
        int closest = -1; float distance = float.PositiveInfinity;
        for (int i = 0; i < CoinRules.Count; i++)
        {
            if (state.IsCollected(i)) continue;
            float d = Vector3.Distance(transform.position, CoinRules.Position(i));
            if (d < distance) { distance = d; closest = i; }
        }
        Request(closest);
    }
    [ServerRpc]
    private void CollectServerRpc(int id)
    {
        var session = FindAnyObjectByType<CoinSession>();
        string result = session.TryCollect(this, id);
        ReplyClientRpc(result, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });
    }
    [ClientRpc]
    private void ReplyClientRpc(string result, ClientRpcParams target = default) { LastReply = result; Debug.Log("REPLY " + result); }
    public override void OnNetworkDespawn()
    {
        move?.Dispose(); move = null; serverInput = Vector2.zero; controller.enabled = false;
    }
}
