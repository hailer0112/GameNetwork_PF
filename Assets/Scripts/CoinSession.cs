using Unity.Netcode;
using UnityEngine;

public class CoinSession : NetworkBehaviour
{
    public readonly NetworkVariable<int> Score = new NetworkVariable<int>();
    public readonly NetworkVariable<int> CollectedMask = new NetworkVariable<int>();
    public readonly NetworkVariable<int> Accepted = new NetworkVariable<int>();
    public readonly NetworkVariable<int> Rejected = new NetworkVariable<int>();
    private readonly GameObject[] visuals = new GameObject[CoinRules.Count];
    public string LastDecision { get; private set; } = "No requests yet";
    public int Remaining
    {
        get { int n = 0; for (int i = 0; i < CoinRules.Count; i++) if (!IsCollected(i)) n++; return n; }
    }
    public bool IsCollected(int id) => id >= 0 && id < CoinRules.Count && (CollectedMask.Value & (1 << id)) != 0;

    private void Start()
    {
        for (int i = 0; i < visuals.Length; i++)
        {
            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Coin_" + i;
            coin.transform.position = CoinRules.Position(i);
            coin.transform.rotation = Quaternion.Euler(90, 0, 0);
            coin.transform.localScale = new Vector3(.6f, .1f, .6f);
            Destroy(coin.GetComponent<Collider>());
            coin.GetComponent<Renderer>().material.color = new Color(1f, .73f, .12f);
            visuals[i] = coin;
        }
    }
    private void Update()
    {
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null) visuals[i].SetActive(IsSpawned && !IsCollected(i));
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer) { Score.Value = CollectedMask.Value = Accepted.Value = Rejected.Value = 0; }
    }
    public string TryCollect(CoinPlayer player, int id)
    {
        if (!IsServer || !IsSpawned) return "REJECT no active server";
        string reason = id < 0 || id >= CoinRules.Count ? "invalid ID" :
            IsCollected(id) ? "already collected" :
            Vector3.Distance(player.transform.position, CoinRules.Position(id)) > CoinRules.Reach ? "out of range" : null;
        if (reason != null) { Rejected.Value++; LastDecision = "REJECT " + reason; }
        else
        {
            // Both requests are processed on Unity's server main thread. Set the bit before awarding.
            CollectedMask.Value |= 1 << id;
            Score.Value++; Accepted.Value++;
            LastDecision = "ACCEPT";
        }
        Debug.Log($"COIN peer={player.OwnerClientId} id={id} result={LastDecision} score={Score.Value} mask={CollectedMask.Value}");
        return LastDecision;
    }
}
