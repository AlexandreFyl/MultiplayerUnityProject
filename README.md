# Environnement multijoueur avec Unity et WebSockets

Un mini-jeu multijoueur en 3D développé dans le cadre du cours de Réalité Augmentée. Cette application permet à plusieurs joueurs de se connecter à un environnement partagé et d'interagir en temps réel.

## Structure du projet

-   `/build` : Version compilée du jeu (exécutable Windows)
-   `/server` : Serveur WebSocket Python
-   `/src` : Code source du projet Unity

## Prérequis

### Pour exécuter le jeu

-   Windows 10/11 (pour la version compilée)
-   OU Unity 2022.3 (pour exécuter depuis l'éditeur)

### Pour le serveur

-   Python 3.8 ou supérieur
-   Bibliothèque websockets (`pip install websockets`)

## Installation et démarrage

### Serveur

1. Ouvrez un terminal dans le dossier `/server`
2. Exécutez `python server.py`
3. Le serveur démarrera sur `ws://localhost:8765`

### Client

-   **Option 1 (version compilée)** : Double-cliquez sur le fichier exécutable dans `/build`
-   **Option 2 (depuis Unity)** : Ouvrez le projet dans Unity et appuyez sur Play

## Fonctionnalités

-   Connexion automatique au serveur WebSocket
-   Déplacement avec ZQSD/flèches directionnelles
-   Actions spéciales :
    -   Espace : Sauter
    -   E : Danser avec les autres joueurs
-   Synchronisation en temps réel des positions et actions
-   Interface utilisateur affichant les contrôles, le nombre de joueurs et l'état de la connexion
-   Couleurs différentes pour chaque joueur

## Contrôles

-   **ZQSD/Flèches** : Déplacement
-   **Espace** : Sauter
-   **E** : Danser

## Architecture technique

Le projet utilise :

-   **Unity** pour le client (avec NativeWebSocket et Newtonsoft.Json)
-   **Python** avec la bibliothèque websockets pour le serveur
-   Communication en temps réel via WebSockets
-   Serialization JSON pour les échanges de données

Le serveur implémente les fonctions suivantes :

-   Gestion des connexions : Établissement et maintien des connexions avec les clients
-   Attribution d'identifiants : Chaque client reçoit un UUID unique lors de sa connexion
-   Suivi des joueurs : Maintien d'un dictionnaire des joueurs connectés et de leurs données
-   Diffusion des messages : Transmission des mises à jour de position et d'actions à tous les clients
-   Le serveur attribue également une couleur aléatoire à chaque joueur pour faciliter l'identification visuelle.

Le client unity lui intègre plusieurs scripts, à savoir :

-   **WebSocketManager** : Ce singleton gère toute la communication avec le serveur WebSocket et coordonne la création/suppression des joueurs. Il :

    -   Établit la connexion avec le serveur
    -   Envoie régulièrement des mises à jour de position
    -   Traite les messages reçus du serveur
    -   Crée et gère les objets joueurs dans la scène

*   **PlayerController** : Ce composant gère les contrôles du joueur et l'exécution des actions spéciales :

    -   Déplacement (translation avant/arrière, rotation)
    -   Actions spéciales (saut, danse)
    -   Animations associées à ces actions

-   **ThirdPersonCamera** : Une caméra qui suit le joueur à la troisième personne, offrant une vue agréable sur le personnage et l'environnement.

-   **GameUI** : Gère l'interface utilisateur qui affiche :
    -   Le nombre de joueurs connectés
    -   Le statut de la connexion
    -   Les instructions de contrôle

## Difficultés rencontrées et solutions

**Gestion des joueurs multiples**

Problème : Accumulation des instances de joueurs à chaque reconnexion.

Solution : Mise en place d'un système de nettoyage qui supprime les joueurs existants au démarrage et utilisation de tags pour identifier les différents types de joueurs.

**Initialisation du contrôleur**

Problème : Le joueur ne pouvait pas se déplacer lorsque le serveur était démarré.

Solution : Modification de l'ordre d'initialisation et création du joueur avant même la connexion au serveur. Ajout d'une condition dans le contrôleur pour permettre au joueur de bouger même sans référence au WebSocketManager.

## Perspectives d'amélioration :

-   Ajout d'un système de chat entre joueurs
-   Implémentation d'éléments de gameplay (collecte d'objets, score)
-   Amélioration des modèles 3D et des animations
-   Optimisation de la synchronisation réseau pour réduire la latence
-   Gestion des déconnexions inattendues et reconnexion automatique
