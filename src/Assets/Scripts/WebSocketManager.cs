using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

[System.Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;

    public static Vector3Data FromVector3(Vector3 v)
    {
        return new Vector3Data { x = v.x, y = v.y, z = v.z };
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[System.Serializable]
public class PlayerData
{
    public Vector3Data position;
    public Vector3Data rotation;
    public string color;
    public string action;
}

[System.Serializable]
public class MessageData
{
    public string type;
    public string id;
    public string message;
    public Vector3Data position;
    public Vector3Data rotation;
    public string action;
    public Dictionary<string, PlayerData> players;
    public PlayerData player;
}

public class WebSocketManager : MonoBehaviour
{
    [SerializeField] private string serverUrl = "ws://localhost:8765";
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playersContainer;
    [SerializeField] private GameObject localPlayerPrefab;

    private WebSocket webSocket;
    private string clientId;
    private Dictionary<string, GameObject> remotePlayers = new Dictionary<string, GameObject>();
    private GameObject localPlayer;
    private float positionUpdateInterval = 0.05f; // 20 fois par seconde
    private float lastPositionUpdateTime;

    public static WebSocketManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        // Nettoyage des joueurs existants
        CleanupExistingPlayers();

        // Créer le joueur local immédiatement (pas après la connexion)
        CreateLocalPlayer();

        webSocket = new WebSocket(serverUrl);

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connexion établie!");
        };

        webSocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            HandleMessage(message);
        };

        webSocket.OnError += (e) =>
        {
            Debug.LogError($"Erreur: {e}");
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("Connexion fermée");
        };

        // Connexion au serveur
        try
        {
            await webSocket.Connect();
            Debug.Log("Connexion WebSocket complétée");
        }
        catch (Exception e)
        {
            Debug.LogError($"Échec de connexion: {e.Message}");
        }
    }

    private void CleanupExistingPlayers()
    {
        // Nettoyage des joueurs locaux existants
        GameObject[] existingLocalPlayers = GameObject.FindGameObjectsWithTag("LocalPlayer");
        foreach (GameObject player in existingLocalPlayers)
        {
            Destroy(player);
        }

        // Nettoyage des joueurs distants
        foreach (var player in remotePlayers.Values)
        {
            Destroy(player);
        }
        remotePlayers.Clear();
    }

    private void HandleMessage(string message)
    {
        try
        {
            MessageData data = JsonConvert.DeserializeObject<MessageData>(message);

            switch (data.type)
            {
                case "connection":
                    clientId = data.id;
                    Debug.Log($"Connexion réussie. ID client: {clientId}");
                    break;

                case "players_list":
                    ProcessPlayersList(data.players);
                    break;

                case "player_joined":
                    AddRemotePlayer(data.id, data.player);
                    break;

                case "player_left":
                    RemoveRemotePlayer(data.id);
                    break;

                case "player_position":
                    UpdateRemotePlayerPosition(data.id, data.position, data.rotation);
                    break;

                case "player_action":
                    PerformRemotePlayerAction(data.id, data.action);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur lors du traitement du message: {e.Message}");
        }
    }

    private void ProcessPlayersList(Dictionary<string, PlayerData> players)
    {
        foreach (var kvp in players)
        {
            if (kvp.Key != clientId)
            {
                AddRemotePlayer(kvp.Key, kvp.Value);
            }
        }
    }

    private void AddRemotePlayer(string playerId, PlayerData playerData)
    {
        if (!remotePlayers.ContainsKey(playerId))
        {
            GameObject newPlayer = Instantiate(playerPrefab, playerData.position.ToVector3(), Quaternion.Euler(playerData.rotation.ToVector3()), playersContainer);
            newPlayer.name = $"RemotePlayer_{playerId}";

            if (ColorUtility.TryParseHtmlString(playerData.color, out Color playerColor))
            {
                Renderer renderer = newPlayer.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = playerColor;
                }
            }

            remotePlayers.Add(playerId, newPlayer);
            Debug.Log($"Joueur distant ajouté: {playerId}");
        }
    }

    private void RemoveRemotePlayer(string playerId)
    {
        if (remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            Destroy(playerObj);
            remotePlayers.Remove(playerId);
            Debug.Log($"Joueur distant supprimé: {playerId}");
        }
    }

    private void UpdateRemotePlayerPosition(string playerId, Vector3Data position, Vector3Data rotation)
    {
        if (remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            playerObj.transform.position = position.ToVector3();
            playerObj.transform.rotation = Quaternion.Euler(rotation.ToVector3());
        }
    }

    private void PerformRemotePlayerAction(string playerId, string action)
    {
        if (remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            PlayerController playerController = playerObj.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.PerformAction(action);
            }
        }
    }

    private void CreateLocalPlayer()
    {
        // Vérifier si un joueur local existe déjà
        if (localPlayer != null)
        {
            Debug.Log("Un joueur local existe déjà");
            return;
        }

        Debug.Log("Création du joueur local");
        localPlayer = Instantiate(localPlayerPrefab, new Vector3(0, 1, 0), Quaternion.identity);
        localPlayer.name = "LocalPlayer";
        localPlayer.tag = "LocalPlayer";

        // Assigner un contrôleur au joueur local
        PlayerController playerController = localPlayer.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.Initialize(this);
        }
        else
        {
            Debug.LogError("PlayerController non trouvé sur le préfab du joueur local");
        }

        // Créer et configurer la caméra à la troisième personne
        SetupThirdPersonCamera();
    }

    private void SetupThirdPersonCamera()
    {
        // Désactiver la caméra principale si elle existe
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }

        // Créer une nouvelle caméra
        GameObject cameraObject = new GameObject("ThirdPersonCamera");
        Camera playerCamera = cameraObject.AddComponent<Camera>();
        playerCamera.tag = "MainCamera"; // Pour que Camera.main fonctionne

        // Ajouter le script de caméra à la troisième personne
        ThirdPersonCamera thirdPersonCamera = cameraObject.AddComponent<ThirdPersonCamera>();
        thirdPersonCamera.SetTarget(localPlayer.transform);

        // Positionner initialement la caméra derrière le joueur
        cameraObject.transform.position = localPlayer.transform.position - (localPlayer.transform.forward * 5) + (Vector3.up * 2);
        cameraObject.transform.LookAt(localPlayer.transform);
    }

    public void SendPositionUpdate()
    {
        if (webSocket.State == WebSocketState.Open && localPlayer != null)
        {
            var data = new MessageData
            {
                type = "position",
                position = Vector3Data.FromVector3(localPlayer.transform.position),
                rotation = Vector3Data.FromVector3(localPlayer.transform.eulerAngles)
            };

            string json = JsonConvert.SerializeObject(data);
            webSocket.SendText(json);
        }
    }

    public void SendAction(string action)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            var data = new MessageData
            {
                type = "action",
                action = action
            };

            string json = JsonConvert.SerializeObject(data);
            webSocket.SendText(json);
        }
    }

    private void OnApplicationQuit()
    {
        CleanupConnection();
    }

    private void OnDisable()
    {
        CleanupConnection();
    }

    private async void CleanupConnection()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            try
            {
                await webSocket.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"Erreur lors de la fermeture de la connexion: {e.Message}");
            }
        }
    }

    public int GetConnectedPlayersCount()
    {
        return remotePlayers.Count + 1; // Inclure le joueur local
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            webSocket.DispatchMessageQueue();
        }
#endif

        // Envoyer régulièrement la position
        if (localPlayer != null && Time.time - lastPositionUpdateTime > positionUpdateInterval)
        {
            SendPositionUpdate();
            lastPositionUpdateTime = Time.time;
        }
    }

    public bool IsConnected()
    {
        return webSocket != null && webSocket.State == WebSocketState.Open;
    }
}