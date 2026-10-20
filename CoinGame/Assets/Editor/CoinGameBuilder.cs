using System;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CoinGameBuilder
{
    public const string ScenePath = "Assets/Scenes/CoinArena.unity";
    [MenuItem("Coin Link/Create Arena")]
    public static void CreateArena()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("Arena already exists. Refusing to overwrite.");
        Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/Prefabs");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var player = new GameObject("CoinPlayer", typeof(NetworkObject), typeof(CharacterController), typeof(CoinPlayer));
        var cc = player.GetComponent<CharacterController>(); cc.height = 2; cc.center = Vector3.up; cc.radius = .4f;
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; body.transform.SetParent(player.transform, false); body.transform.localPosition = Vector3.up;
        UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
        var prefab = PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/CoinPlayer.prefab");
        UnityEngine.Object.DestroyImmediate(player);
        var managerObject = new GameObject("NetworkManager");
        var transport = managerObject.AddComponent<UnityTransport>(); transport.SetConnectionData("127.0.0.1", 7777, "127.0.0.1");
        var manager = managerObject.AddComponent<NetworkManager>();
        manager.NetworkConfig.NetworkTransport = transport; manager.NetworkConfig.PlayerPrefab = prefab;
        manager.NetworkConfig.EnableSceneManagement = true; manager.NetworkConfig.TickRate = 30;
        var prefabs = ScriptableObject.CreateInstance<NetworkPrefabsList>(); prefabs.Add(new NetworkPrefab { Prefab = prefab });
        AssetDatabase.CreateAsset(prefabs, "Assets/Prefabs/NetworkPrefabs.asset"); manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear(); manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabs);
        new GameObject("SharedCoinState", typeof(NetworkObject), typeof(CoinSession));
        new GameObject("Menu", typeof(CoinMenu), typeof(CoinSmoke));
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Arena"; ground.transform.position = new Vector3(0, -.5f, 0); ground.transform.localScale = new Vector3(20, 1, 18);
        CreateMaterial(ground, "Ground", new Color(.16f, .23f, .29f));
        Wall(new Vector3(-10, 1, 0), new Vector3(.5f, 2, 18)); Wall(new Vector3(10, 1, 0), new Vector3(.5f, 2, 18));
        Wall(new Vector3(0, 1, 9), new Vector3(20, 2, .5f)); Wall(new Vector3(0, 1, -9), new Vector3(20, 2, .5f));
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
        camera.transform.position = new Vector3(-3, 16, -12); camera.transform.LookAt(new Vector3(-3, 0, 0));
        camera.GetComponent<Camera>().orthographic = true; camera.GetComponent<Camera>().orthographicSize = 10;
        camera.GetComponent<Camera>().backgroundColor = new Color(.04f, .07f, .1f); camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        var light = new GameObject("Sun", typeof(Light)); light.GetComponent<Light>().type = LightType.Directional; light.transform.rotation = Quaternion.Euler(55, -35, 0);
        RenderSettings.ambientLight = new Color(.6f, .6f, .65f);
        PlayerSettings.productName = "Coin Link"; PlayerSettings.runInBackground = true; PlayerSettings.defaultScreenWidth = 1100; PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        settings.FindProperty("activeInputHandler").intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }; AssetDatabase.SaveAssets();
        Debug.Log("COIN_ARENA_CREATED");
    }
    static void Wall(Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Wall"; wall.transform.position = position; wall.transform.localScale = scale;
    }
    static void CreateMaterial(GameObject target, string name, Color color)
    {
        var material = new Material(Shader.Find("Standard")); material.color = color;
        AssetDatabase.CreateAsset(material, "Assets/Prefabs/" + name + ".mat"); target.GetComponent<Renderer>().sharedMaterial = material;
    }
    public static void BuildBatch()
    {
        if (!File.Exists(ScenePath)) CreateArena();
        Directory.CreateDirectory("Builds");
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/CoinLink.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (result.summary.result != BuildResult.Succeeded) throw new Exception("Coin build failed: " + result.summary.result);
        Debug.Log("COIN_BUILD_OK");
    }
}

