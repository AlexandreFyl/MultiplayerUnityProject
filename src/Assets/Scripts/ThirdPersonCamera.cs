using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;        // La cible à suivre (le joueur local)
    [SerializeField] private float distance = 7.0f;   // Distance derrière le joueur
    [SerializeField] private float height = 3.0f;     // Hauteur au-dessus du joueur
    [SerializeField] private float smoothSpeed = 10f; // Vitesse de suivi

    private Vector3 desiredPosition;
    private Quaternion desiredRotation;

    // Pour initialiser la cible si elle n'est pas définie dans l'inspecteur
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Calculer la position désirée de la caméra
        desiredPosition = target.position - (target.forward * distance) + (Vector3.up * height);

        // Rotation désirée
        desiredRotation = Quaternion.LookRotation(target.position - desiredPosition + Vector3.up * 1.0f);

        // Déplacer la caméra avec smoothing
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, smoothSpeed * Time.deltaTime);
    }
}