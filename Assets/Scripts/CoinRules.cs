using UnityEngine;

// Deterministic layout: IDs remain stable in both processes and across reconnects.
public static class CoinRules
{
    public const int Count = 12;
    public const float Reach = 1.8f;
    public static Vector3 Position(int id) => new Vector3(-4 + (id % 4) * 2.5f, 0.65f, -2 + (id / 4) * 2.5f);
}
