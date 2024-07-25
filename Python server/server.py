# server.py
import asyncio
import websockets
import struct

async def echo(websocket, path):
    async for message in websocket:
        if isinstance(message, bytes):
            # print(f"Received binary: {message}")
            await websocket.send(message)
        else:
            print(f"Received non-binary message: {message}")
            await websocket.send(message)

async def main():
    async with websockets.serve(echo, "0.0.0.0", 3300):
        await asyncio.Future()  # run forever

if __name__ == "__main__":
    print('Going!')
    asyncio.run(main())
