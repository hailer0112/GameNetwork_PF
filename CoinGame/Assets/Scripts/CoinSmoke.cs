using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Opt-in integration scenario; ordinary launches never run scripted input.
public class CoinSmoke : MonoBehaviour
{
    private IEnumerator Start()
    {
        var args = Environment.GetCommandLineArgs();
        int preview = Array.IndexOf(args, "--capture-preview");
        if (preview >= 0 && preview + 1 < args.Length)
        {
            yield return null;
            CoinMenu.Instance.Connect(true);
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(args[preview + 1]);
            yield return new WaitForSeconds(2);
            Application.Quit();
            yield break;
        }
        bool host = Array.IndexOf(args, "--smoke-host") >= 0;
        bool client = Array.IndexOf(args, "--smoke-client") >= 0;
        if (!host && !client) yield break;
        yield return null;
        CoinMenu.Instance.Connect(host);
        float deadline = Time.realtimeSinceStartup + 20;
        while ((!NetworkManager.Singleton.IsConnectedClient || CoinMenu.Instance.Local() == null) && Time.realtimeSinceStartup < deadline) yield return null;
        if (!NetworkManager.Singleton.IsConnectedClient) { Debug.LogError("SMOKE_FAIL connection"); Application.Quit(2); yield break; }
        var player = CoinMenu.Instance.Local();
        var state = FindAnyObjectByType<CoinSession>();
        yield return new WaitForSeconds(1);
        Debug.Log($"SMOKE_CONNECTED owner={player.OwnerClientId} score={state.Score.Value} mask={state.CollectedMask.Value}");
        if (client)
        {
            player.Request(-1); yield return new WaitForSeconds(.4f);
            if (player.LastReply != "REJECT invalid ID") Debug.LogError("SMOKE_FAIL invalid ID");
            player.Request(11); yield return new WaitForSeconds(.4f);
            if (player.LastReply != "REJECT out of range") Debug.LogError("SMOKE_FAIL range");
            var start = player.transform.position;
            player.TestInput = Vector2.up;
            deadline = Time.realtimeSinceStartup + 8;
            while (player.Position.Value.z < -2.2f && Time.realtimeSinceStartup < deadline) yield return null;
            player.TestInput = Vector2.zero;
            yield return new WaitForSeconds(.5f);
            Debug.Log($"SMOKE_MOVEMENT from={start} to={player.transform.position} authoritative={player.Position.Value}");
            if (player.Position.Value.z < start.z + 1) Debug.LogError("SMOKE_FAIL movement");
            player.RequestNearest(); yield return new WaitForSeconds(.5f);
            if (player.LastReply != "ACCEPT") Debug.LogError("SMOKE_FAIL collect " + player.LastReply);
            int score = state.Score.Value;
            player.Repeat(); yield return new WaitForSeconds(.5f);
            if (player.LastReply != "REJECT already collected" || state.Score.Value != score) Debug.LogError("SMOKE_FAIL duplicate");
            Debug.Log($"SMOKE_CLIENT_STATE score={state.Score.Value} mask={state.CollectedMask.Value} rejected={state.Rejected.Value}");
            Debug.Log("SMOKE_CLIENT_DONE");
            CoinMenu.Instance.Disconnect(); yield return new WaitForSeconds(1);
            CoinMenu.Instance.Connect(false);
            deadline = Time.realtimeSinceStartup + 15;
            while (!NetworkManager.Singleton.IsConnectedClient && Time.realtimeSinceStartup < deadline) yield return null;
            yield return new WaitForSeconds(1);
            if (!NetworkManager.Singleton.IsConnectedClient || state.Score.Value != score) Debug.LogError("SMOKE_FAIL reconnect state");
            Debug.Log($"SMOKE_RECONNECTED score={state.Score.Value} mask={state.CollectedMask.Value}");
            Application.Quit();
        }
        else
        {
            float end = Time.realtimeSinceStartup + 25;
            while (state.Score.Value == 0 && Time.realtimeSinceStartup < end) yield return null;
            player.Request(2); // A second participant requests the same ID after first award.
            yield return new WaitForSeconds(.5f);
            if (player.LastReply != "REJECT already collected") Debug.LogError("SMOKE_FAIL second participant duplicate");
            Debug.Log("SMOKE_SECOND_PARTICIPANT_DUPLICATE " + player.LastReply);
            while (Time.realtimeSinceStartup < end) yield return null;
            Debug.Log($"SMOKE_HOST_STATE score={state.Score.Value} mask={state.CollectedMask.Value} accepted={state.Accepted.Value} rejected={state.Rejected.Value} players={NetworkManager.Singleton.ConnectedClientsIds.Count}");
            if (state.Score.Value != 1 || state.Accepted.Value != 1 || state.Rejected.Value < 3 || NetworkManager.Singleton.ConnectedClientsIds.Count != 1) Debug.LogError("SMOKE_FAIL host final state");
            Debug.Log("SMOKE_HOST_DONE"); Application.Quit();
        }
    }
}
