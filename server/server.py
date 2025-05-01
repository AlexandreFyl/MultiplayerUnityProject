import asyncio
import json
import websockets
import uuid

# Stockage des joueurs connectés: {client_id: {position, color, action}}
connected_players = {}

# Dictionnaire pour stocker les WebSockets des clients
connected_websockets = {}


async def register_websocket(websocket, client_id):
    connected_websockets[client_id] = websocket


async def unregister_websocket(client_id):
    if client_id in connected_websockets:
        del connected_websockets[client_id]


async def broadcast(message, exclude=None):
    # Envoyer un message à tous les clients connectés
    if (
        connected_websockets
    ):  # Utiliser connected_websockets au lieu de connected_players
        tasks = []
        for client_id, client in connected_websockets.items():
            if exclude is None or client_id != exclude:
                tasks.append(client.send(message))

        if tasks:  # S'assurer qu'il y a des tâches à exécuter
            await asyncio.gather(*tasks)


async def broadcast_position(sender_id, position, rotation):
    # Envoyer la position mise à jour à tous les joueurs sauf l'expéditeur
    message = json.dumps(
        {
            "type": "player_position",
            "id": sender_id,
            "position": position,
            "rotation": rotation,
        }
    )
    await broadcast(message, exclude=sender_id)


async def broadcast_action(sender_id, action):
    # Envoyer l'action à tous les joueurs
    message = json.dumps({"type": "player_action", "id": sender_id, "action": action})
    await broadcast(message)


async def broadcast_player_joined(player_id):
    # Informer tous les autres joueurs qu'un nouveau joueur a rejoint
    message = json.dumps(
        {
            "type": "player_joined",
            "id": player_id,
            "player": connected_players[player_id],
        }
    )
    await broadcast(message, exclude=player_id)


async def broadcast_player_left(player_id):
    # Informer tous les joueurs qu'un joueur est parti
    message = json.dumps({"type": "player_left", "id": player_id})
    await broadcast(message)


async def server_handler(websocket):
    client_id = str(uuid.uuid4())

    try:
        # Enregistrer le websocket
        await register_websocket(websocket, client_id)

        # Envoyer l'ID au client
        await websocket.send(
            json.dumps(
                {
                    "type": "connection",
                    "id": client_id,
                    "message": "Connecté au serveur",
                }
            )
        )

        print(f"Nouveau joueur connecté: {client_id}")

        # Ajouter le joueur
        connected_players[client_id] = {
            "position": {"x": 0, "y": 0, "z": 0},
            "rotation": {"x": 0, "y": 0, "z": 0},
            "color": "#" + uuid.uuid4().hex[0:6],
            "action": "idle",
        }

        # Envoyer la liste des joueurs existants
        await websocket.send(
            json.dumps({"type": "players_list", "players": connected_players})
        )

        # Annoncer aux autres
        await broadcast_player_joined(client_id)

        # Boucle principale de réception
        async for message in websocket:
            data = json.loads(message)

            if data["type"] == "position":
                connected_players[client_id]["position"] = data["position"]
                connected_players[client_id]["rotation"] = data["rotation"]
                await broadcast_position(client_id, data["position"], data["rotation"])

            elif data["type"] == "action":
                connected_players[client_id]["action"] = data["action"]
                await broadcast_action(client_id, data["action"])

    except websockets.exceptions.ConnectionClosed:
        print(f"Connexion fermée pour {client_id}")

    finally:
        # Nettoyage à la déconnexion
        if client_id in connected_players:
            del connected_players[client_id]
        await unregister_websocket(client_id)
        await broadcast_player_left(client_id)
        print(f"Joueur déconnecté: {client_id}")


async def start_server_async():
    print("Serveur démarré sur ws://localhost:8765")
    server = await websockets.serve(server_handler, "localhost", 8765)
    await server.wait_closed()


# Point d'entrée principal
if __name__ == "__main__":
    try:
        asyncio.run(start_server_async())
    except KeyboardInterrupt:
        print("Serveur arrêté par l'utilisateur")
