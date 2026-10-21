using System;
using System.Net;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// UGUI is constructed here so a fresh project has no missing Inspector references.
public class CoinMenu : MonoBehaviour
{
    public static CoinMenu Instance;
    private Text summary, reply;
    private InputField address, port;
    private Button host, client;
    private string status = "Disconnected";
    private float connectingAt;
    private bool pending;
    private Font font;
    private void Start()
    {
        Instance = this;
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = new GameObject("Connection HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1100, 720);
        var panel = Rect("Panel", canvas.transform, 16, -16, 430, 310);
        panel.gameObject.AddComponent<Image>().color = new Color(.035f, .06f, .1f, .95f);
        Label(panel, "COIN LINK / NETWORK LAB", 16, -12, 400, 30, 23);
        address = Field(panel, "127.0.0.1", 16, -50, 250);
        port = Field(panel, "7777", 280, -50, 130);
        host = Button(panel, "Host", 16, -94, () => Connect(true));
        client = Button(panel, "Client", 150, -94, () => Connect(false));
        Button(panel, "Disconnect", 284, -94, Disconnect);
        summary = Label(panel, "Disconnected", 16, -142, 400, 62, 18);
        Label(panel, "WASD / arrows: move    E: collect nearest", 16, -207, 400, 26, 17);
        reply = Label(panel, "No request", 16, -239, 400, 50, 17);
        var tests = Rect("Test panel", canvas.transform, 16, -340, 430, 150);
        tests.gameObject.AddComponent<Image>().color = new Color(.035f, .06f, .1f, .95f);
        Label(tests, "SERVER VALIDATION TESTS", 16, -8, 400, 30, 19);
        Button(tests, "Collect", 16, -44, () => Local()?.RequestNearest());
        Button(tests, "Invalid ID", 150, -44, () => Local()?.Request(-1));
        Button(tests, "Repeat last", 284, -44, () => Local()?.Repeat());
        Button(tests, "Far coin #11", 16, -88, () => Local()?.Request(11));
        Label(tests, "Coin #11 must still be distant/uncollected.", 150, -92, 260, 44, 15);
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
    }
    public CoinPlayer Local() => NetworkManager.Singleton.LocalClient?.PlayerObject?.GetComponent<CoinPlayer>();
    public void Connect(bool asHost)
    {
        ConnectAsync(asHost).Forget();
    }
    private async UniTask ConnectAsync(bool asHost)
    {
        var manager = NetworkManager.Singleton;
        if (pending || manager.IsListening || manager.ShutdownInProgress) return;
        if (!IPAddress.TryParse(address.text, out _) || !ushort.TryParse(port.text, out var p) || p == 0) { status = "Invalid address / port"; return; }
        manager.GetComponent<UnityTransport>().SetConnectionData(address.text, p, "127.0.0.1");
        pending = true; connectingAt = Time.unscaledTime; status = "Connecting...";
        try
        {
            if (!(asHost ? manager.StartHost() : manager.StartClient())) { pending = false; status = "Start failed; retry"; }
            if (pending)
                await UniTask.WaitUntil(() => !pending, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { pending = false; status = "Start error: " + ex.Message; manager.Shutdown(); }
    }
    private void OnConnected(ulong id) { if (id == NetworkManager.Singleton.LocalClientId) { pending = false; status = "Connected"; } }
    private void OnDisconnected(ulong id)
    {
        var m = NetworkManager.Singleton;
        if (id == m.LocalClientId) { pending = false; status = "Disconnected / " + m.DisconnectReason; }
    }
    public void Disconnect() { pending = false; status = "Disconnected"; NetworkManager.Singleton.Shutdown(); }
    private void Update()
    {
        var m = NetworkManager.Singleton;
        if (m == null || host == null) return;
        if (pending && Time.unscaledTime - connectingAt > 10) { Disconnect(); status = "Connection timed out; retry"; }
        host.interactable = client.interactable = !pending && !m.IsListening && !m.ShutdownInProgress;
        address.interactable = port.interactable = host.interactable;
        var session = FindAnyObjectByType<CoinSession>();
        summary.text = $"{status} / {address.text}:{port.text}\n" + (m.IsHost ? $"Host / players {m.ConnectedClientsIds.Count}" : m.IsConnectedClient ? "Client" : "Offline");
        if (session != null && session.IsSpawned) summary.text += $" / Score {session.Score.Value} / Left {session.Remaining}";
        reply.text = "Server reply: " + (Local()?.LastReply ?? "No player");
    }
    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
    }
    RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); return r;
    }
    Text Label(Transform parent, string value, float x, float y, float w, float h, int size)
    {
        var text = Rect(value, parent, x, y, w, h).gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = size; text.color = Color.white; text.text = value; text.raycastTarget = false; return text;
    }
    Button Button(Transform parent, string title, float x, float y, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(title, parent, x, y, 126, 36); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.13f, .32f, .47f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        var text = Label(rect, title, 0, 0, 126, 36, 17); text.alignment = TextAnchor.MiddleCenter; return button;
    }
    InputField Field(Transform parent, string value, float x, float y, float width)
    {
        var rect = Rect("Address field", parent, x, y, width, 32); rect.gameObject.AddComponent<Image>().color = new Color(.12f, .16f, .22f);
        var field = rect.gameObject.AddComponent<InputField>(); field.textComponent = Label(rect, "", 8, 0, width - 16, 32, 17); field.textComponent.alignment = TextAnchor.MiddleLeft;
        field.text = value; field.characterLimit = 64; return field;
    }
}
