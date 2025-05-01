using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI connectedPlayersText;
    [SerializeField] private TextMeshProUGUI controlsInfoText;
    [SerializeField] private TextMeshProUGUI connectionStatusText;

    [Header("Colors")]
    [SerializeField] private Color connectedColor = new Color(0.1f, 0.8f, 0.1f); // Vert
    [SerializeField] private Color disconnectedColor = new Color(0.8f, 0.1f, 0.1f); // Rouge

    private WebSocketManager webSocketManager;

    void Start()
    {
        webSocketManager = WebSocketManager.Instance;

        // Mettre à jour le texte des contrôles
        controlsInfoText.text =
            "ZQSD/Flèches: Déplacement\n" +
            "Espace: Sauter\n" +
            "E: Danser";

        // Indiquer que la connexion est en cours
        UpdateConnectionStatus(false, "En attente de connexion...");

        // Configurer la taille du panel
        SetupPanelSize();
    }

    void Update()
    {
        // Vérifier si le WebSocketManager est initialisé
        if (webSocketManager != null)
        {
            // Mettre à jour le compteur de joueurs connectés
            int playersCount = webSocketManager.GetConnectedPlayersCount();
            connectedPlayersText.text = $"Joueurs connectés: {playersCount}";

            // Mettre à jour le statut de connexion
            bool isConnected = webSocketManager.IsConnected();
            UpdateConnectionStatus(isConnected, isConnected ? "Connecté" : "Déconnecté");
        }
    }

    private void UpdateConnectionStatus(bool connected, string statusMessage)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.color = connected ? connectedColor : disconnectedColor;
            connectionStatusText.text = statusMessage;
        }
    }

    private void SetupPanelSize()
    {
        // Obtenir une référence au RectTransform du Panel
        RectTransform panelRect = transform.parent.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // Définir la hauteur à 80 pixels
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80f);

            // S'assurer que le Panel s'étire horizontalement
            panelRect.anchorMin = new Vector2(0, panelRect.anchorMin.y);
            panelRect.anchorMax = new Vector2(1, panelRect.anchorMax.y);
            panelRect.offsetMin = new Vector2(0, panelRect.offsetMin.y);
            panelRect.offsetMax = new Vector2(0, panelRect.offsetMax.y);
        }
    }

    public void ShowEventMessage(string message, bool isError = false)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.color = isError ? disconnectedColor : connectedColor;
            connectionStatusText.text = message;

            // Rétablir le statut normal après 3 secondes
            StartCoroutine(ResetStatusAfterDelay(3f));
        }
    }

    private IEnumerator ResetStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Revenir au statut de connexion normal
        if (webSocketManager != null)
        {
            bool isConnected = webSocketManager.IsConnected();
            UpdateConnectionStatus(isConnected, isConnected ? "Connecté" : "Déconnecté");
        }
    }
}