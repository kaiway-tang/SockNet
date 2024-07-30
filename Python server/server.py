import asyncio
import websockets
import struct
from collections import deque
import time

# Set to store all connected clients
connected_clients = set()

host_connected = False
id_deque = deque()
id_end = 64

id_message = bytearray([0, 0, 1, 0, 0, 0, 0])
time_message = bytearray([0, 0, 3, 0, 0])

sync_package = []

def GetID():
    global id_end
    if len(id_deque) > 0:
        return id_deque.pop()
    id_end += 1
    return (id_end - 1)

def ReturnID(id):
    id_deque.append(id)

def GetTime():
    return struct.pack('>H', round(time.time() * 1000) % 60000)

async def HandleServerRequest(message, websocket):
    global id_end
    # client initial connection
    if message[2] == 0:
        print("new kid")
        await websocket.send(bytearray([0, 0, 0, 0 if host_connected else 1]))
        for buffer in sync_package:
            await websocket.send(buffer)
    
    elif message[2] == 1:
        print("host update")
        id_end = struct.unpack('>H', message[3:5])[0]
    
    elif message[2] == 2:
        new_id = GetID()
        print("assigned ID: ", new_id)

        id_message[3:5] = message[5:7]
        id_message[5:7] = struct.pack('>H', new_id)
        await websocket.send(id_message)

        prefab_ID = struct.unpack('>H', message[3:5])[0]
        prefab_ID += 1024
        id_message[3:5] = struct.pack('>H', prefab_ID)

        for client in connected_clients:
            if client != websocket:
                await client.send(id_message)

        sync_package.append(id_message)
    
    elif message[2] == 3:
        time_message[3:5] = GetTime()
        await websocket.send(time_message)
    
    elif message[2] == 4:
        new_id = GetID()        

        id_message[3:5] = message[5:7]
        id_message[5:7] = struct.pack('>H', new_id)
        # await websocket.send(id_message)

        prefab_ID = struct.unpack('>H', message[3:5])[0]
        prefab_ID += 1024
        id_message[3:5] = struct.pack('>H', prefab_ID)

        print("assigned ID: ", new_id, " for new prefab ID: ", prefab_ID)

        for client in connected_clients:
            await client.send(id_message)

        sync_package.append(id_message)


async def handle_client(websocket):
    # Add the new client to our set
    connected_clients.add(websocket)
    try:
        async for message in websocket:           

            # special server request
            if message[1] == 0 and message[0] == 0:
                await HandleServerRequest(message, websocket)

            # Broadcast the message to all clients except the sender
            else:
                for client in connected_clients:
                    if client != websocket:
                        await client.send(message)
            
            if isinstance(message, bytes):
                pass                
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