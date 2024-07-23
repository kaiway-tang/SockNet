# server.py
import asyncio
import websockets

async def echo(websocket, path):
    async for message in websocket:
        print(f"Received message: {message}")
        await websocket.send(message)

async def main():
    async with websockets.serve(echo, "192.168.1.212", 3300):
        await asyncio.Future()  # run forever

if __name__ == "__main__":
    print('Going!')
    asyncio.run(main())
