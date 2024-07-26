import asyncio
import websockets

# Set to store all connected clients
connected_clients = set()

async def handle_client(websocket, path):
    # Add the new client to our set
    connected_clients.add(websocket)
    try:
        async for message in websocket:
            # Broadcast the message to all clients except the sender
            for client in connected_clients:
                if client != websocket or True:
                    if isinstance(message, bytes):
                        await client.send(message)
                    else:
                        await client.send(f"Client {id(websocket)} says: {message}")
            
            # Print the message to server console
            if isinstance(message, bytes):
                print(f"{id(websocket)} sent {message}")
            else:
                print(f"Received message from client {id(websocket)}: {message}")
    finally:
        # Remove the client from our set when they disconnect
        connected_clients.remove(websocket)

async def main():
    server = await websockets.serve(handle_client, "0.0.0.0", 3300)
    print("Server started on 0.0.0.0:3300")
    await server.wait_closed()

if __name__ == "__main__":
    print("Going!")
    asyncio.run(main())