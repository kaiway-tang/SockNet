mergeInto(LibraryManager.library, {
    Hello: function () {
        window.alert("Hello, world!");
      },
    
      HelloString: function (str) {
        window.alert(UTF8ToString(str));
      },
    
    Bounce: function() {
        SendMessage('JSH', 'JSM', 'asdf');
    },
    
    AllInOne: function () {
        // const socket = new WebSocket('ws://192.168.1.212:3300');

        // socket.addEventListener('open', (event) => {
        //     console.log('Connected to the server');
        //     const message = 'Hello, Server!';
        //     socket.send(message);
        //     console.log(`Sent: ${message}`);
        // });

        // socket.addEventListener('message', (event) => {
        //     console.log(`Message from server: ${event.data}`);
            
        //     SendMessage('JSH', 'JSM', event.data);
        // });

        // socket.addEventListener('close', (event) => {
        //     console.log('Disconnected from the server');
        // });

        // socket.addEventListener('error', (event) => {
        //     console.error('WebSocket error:', event);
        // });  
    },

    CallUnityFunction: function(arg) {
        // Call the C# function in Unity
        if (unityInstance != null) {
            unityInstance.SendMessage('JSH', 'JSM', arg);
        }
    },

    Connect: function (url) {
        window.webSocket = new WebSocket(UTF8ToString(url));
        window.webSocket.binaryType = 'arraybuffer';

        window.webSocket.onopen = function (event) {
            console.log('WebSocket connection opened');
        };

        window.webSocket.onmessage = function (event) {
            // console.log('Message received from server: ' + event.data);

            if (event.data instanceof ArrayBuffer) {
                // Send the value to another function
                SendMessage('NM', 'JSM', arrayBufferToBase64(event.data));
            } else {
                console.error('Received message is not an ArrayBuffer');
            }
        };

        function arrayBufferToBase64(buffer) {
            return btoa(String.fromCharCode.apply(null, new Uint8Array(buffer)));
        }

        window.webSocket.onerror = function (event) {
            console.log('WebSocket error: ' + event.data);
        };

        window.webSocket.onclose = function (event) {
            console.log('WebSocket connection closed');
        };
    },

    Send: function (bufferPtr, length) {
        if (window.webSocket) {
            var buffer = new Uint8Array(Module.HEAPU8.buffer, bufferPtr, length);
            //console.log("send buffer: ", buffer);
            
            // Send as a binary buffer
            //var array_buffer = new ArrayBuffer(length);
            //var view = new DataView(buffer);
            //view.setBigInt64(0, longValue, true);  // true for little-endian
            window.webSocket.send(buffer);
        }
    },

    SendOld: function (highBits, lowBits) {
        if (window.webSocket) {
            var longValue = (BigInt(highBits) << BigInt(32)) | BigInt(lowBits >>> 0);

            console.log("high low: " + highBits + lowBits);
            
            // Send as a binary buffer
            var buffer = new ArrayBuffer(12);
            var view = new DataView(buffer);
            view.setBigInt64(0, longValue, true);  // true for little-endian
            window.webSocket.send(buffer);
        }
    },

    CloseConnection: function () {
        if (window.webSocket) {
            window.webSocket.close();
        }
    }
});
