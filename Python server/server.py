import asyncio
import websockets
from websockets import WebSocketServerProtocol

async def echo(websocket: WebSocketServerProtocol, path):
    async for message in websocket:
        print(f"Received message: {message}")
        await websocket.send(message)

async def main():
    server = await websockets.serve(
        echo,
        "0.0.0.0",
        3300,
        process_request=add_cors_headers
    )
    await server.wait_closed()

def add_cors_headers(path, request_headers):
    return {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
        'Access-Control-Allow-Headers': 'Content-Type',
    }

if __name__ == "__main__":
    print('Server starting...')
    asyncio.run(main())