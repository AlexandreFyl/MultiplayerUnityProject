using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private Animator animator;

    private WebSocketManager webSocketManager;
    private bool isPerformingAction = false;

    public void Initialize(WebSocketManager manager)
    {
        webSocketManager = manager;
        Debug.Log($"PlayerController initialisé pour {gameObject.name}");
    }

    void Update()
    {

        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            Debug.Log($"Entrées détectées: H={Input.GetAxis("Horizontal")}, V={Input.GetAxis("Vertical")}");
        }

        if (gameObject.name.StartsWith("RemotePlayer_") && webSocketManager == null)
            return;

        // Déplacement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Rotation
        transform.Rotate(0, horizontal * rotateSpeed * Time.deltaTime, 0);

        // Avancer/reculer
        Vector3 movement = transform.forward * vertical * moveSpeed * Time.deltaTime;
        transform.position += movement;

        // Gestion des actions spéciales
        if (webSocketManager != null)
        {
            HandleSpecialActions();
        }
    }

    private void HandleSpecialActions()
    {
        if (isPerformingAction)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformAction("jump");
            webSocketManager.SendAction("jump");
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            PerformAction("tilt");
            webSocketManager.SendAction("tilt");
        }
    }

    public void PerformAction(string action)
    {
        switch (action)
        {
            case "jump":
                StartCoroutine(Jump());
                break;
            case "tilt":
                StartCoroutine(Tilt());
                break;
        }
    }

    private IEnumerator Jump()
    {
        isPerformingAction = true;

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
        else
        {
            // Animation simplifiée
            Vector3 originalPosition = transform.position;
            float jumpHeight = 1.0f;
            float jumpDuration = 0.5f;

            for (float t = 0; t < jumpDuration; t += Time.deltaTime)
            {
                float normalizedTime = t / jumpDuration;
                // Courbe parabolique pour le saut
                float height = Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;
                transform.position = new Vector3(originalPosition.x, originalPosition.y + height, originalPosition.z);
                yield return null;
            }

            transform.position = originalPosition;
        }

        yield return new WaitForSeconds(0.5f);
        isPerformingAction = false;
    }

    private IEnumerator Tilt()
    {
        isPerformingAction = true;

        // Animation de tilt
        Vector3 originalRotation = transform.eulerAngles;
        float tiltAngle = 30f;  // Angle d'inclinaison
        float tiltDuration = 0.2f;  // Durée de l'inclinaison
        float returnDuration = 0.15f;  // Durée du retour

        // Répéter 4 fois (2 fois à gauche, 2 fois à droite)
        for (int i = 0; i < 4; i++)
        {
            // Déterminer la direction (positif pour gauche, négatif pour droite)
            float direction = (i % 2 == 0) ? 1 : -1;

            // Inclinaison dans la direction choisie
            for (float t = 0; t < tiltDuration; t += Time.deltaTime)
            {
                float normalizedTime = t / tiltDuration;
                // Courbe d'accélération pour l'inclinaison
                float angle = Mathf.SmoothStep(0, tiltAngle * direction, normalizedTime);
                transform.eulerAngles = new Vector3(originalRotation.x, originalRotation.y, angle);
                yield return null;
            }

            // Retour à la position normale
            for (float t = 0; t < returnDuration; t += Time.deltaTime)
            {
                float normalizedTime = t / returnDuration;
                // Courbe de décélération pour le retour
                float angle = Mathf.SmoothStep(tiltAngle * direction, 0, normalizedTime);
                transform.eulerAngles = new Vector3(originalRotation.x, originalRotation.y, angle);
                yield return null;
            }
        }

        // S'assurer que le joueur revient à sa rotation d'origine
        transform.eulerAngles = originalRotation;

        yield return new WaitForSeconds(0.1f);
        isPerformingAction = false;
    }
}