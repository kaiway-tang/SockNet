mergeInto(LibraryManager.library, {
    Hello: function () {
        window.alert("Hello, world!");
      },
    
      HelloString: function (str) {
        window.alert(UTF8ToString(str));
      },
    
    AllInOne: function () {
        const socket = new WebSocket('ws://192.168.1.212:3300');

        socket.addEventListener('open', (event) => {
            console.log('Connected to the server');
            const message = 'Hello, Server!';
            socket.send(message);
            console.log(`Sent: ${message}`);
        });

        socket.addEventListener('message', (event) => {
            console.log(`Message from server: ${event.data}`);
        });

        socket.addEventListener('close', (event) => {
            console.log('Disconnected from the server');
        });

        socket.addEventListener('error', (event) => {
            console.error('WebSocket error:', event);
        });  
    },

    Connect: function (url) {
        window.webSocket = new WebSocket(Pointer_stringify(url));

        window.webSocket.onopen = function (event) {
            console.log('WebSocket connection opened');
        };

        window.webSocket.onmessage = function (event) {
            console.log('Message received from server: ' + event.data);
            // Optionally, you can send the message to Unity
            // SendMessageToUnity(event.data);
        };

        window.webSocket.onerror = function (event) {
            console.log('WebSocket error: ' + event.data);
        };

        window.webSocket.onclose = function (event) {
            console.log('WebSocket connection closed');
        };
    },

    SendMessage: function (message) {
        if (window.webSocket) {
            window.webSocket.send(Pointer_stringify(message));
        }
    },

    CloseConnection: function () {
        if (window.webSocket) {
            window.webSocket.close();
        }
    }
});
