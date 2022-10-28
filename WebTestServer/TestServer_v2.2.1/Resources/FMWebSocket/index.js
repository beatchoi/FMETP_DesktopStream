//new solution
//upgrade port for http use...
//ref: https://programmer.group/in-nodejs-http-protocol-and-ws-protocol-reuse-the-same-port.html
var express = require("express");
const WS_MODULE = require("ws");
const http = require("http");

const app = express();
app.use(express.static(__dirname + '/public'));
const port = 3000;

app.get("/hello", (req, res) => {
  res.send("hello world");
});

const server = http.createServer(app);
ws = new WS_MODULE.Server({server});

server.listen(port, () => {
  console.log("Server turned on, port number:" + port);
});
//new solution

var serverID = 'undefined';
var serverWS = null;
const clients = new Map();

function uuidv4()
{
  // return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
  //   var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
  //   return v.toString(16);
  // });

  function s4() { return Math.floor((1 + Math.random()) * 0x10000).toString(16).substring(1); }
  return s4() + s4() + '-' + s4();
}

function ByteToInt32(_byte, _offset)
{
    return (_byte[_offset] & 255) + ((_byte[_offset + 1] & 255) << 8) + ((_byte[_offset + 2] & 255) << 16) + ((_byte[_offset + 3] & 255) << 24);
}
function ByteToInt16(_byte, _offset)
{
    return (_byte[_offset] & 255) + ((_byte[_offset + 1] & 255) << 8);
}

ws.on('connection', function connection(ws) {
      const wsid = uuidv4();
      const networkType = 'undefined';
      const metadata = { ws, networkType, wsid };

      if(!clients.has(wsid))
      {
        ws.id = wsid;

        clients.set(wsid, metadata);
        console.log("connection count: " + clients.size + " : " + wsid);

        ws.send("OnReceivedWSIDEvent(" + wsid +")");
      }


  function heartbeat()
  {
    if (!ws) return;
    if (ws.readyState !== 1) return;
    if (serverID !== 'undefined')
    {
      ws.send("heartbeat");
    }
    else
    {
      ws.send("WaitingUnityServer");
    }
    setTimeout(heartbeat, 500);
  }

  //onOpen
  heartbeat();

  ws.on('close', function close() {
    //on user disconnected
    if(wsid === serverID)
    {
      //on server disconnected

      serverID = 'undefined';
      serverWS = null;
      console.log("Disconnected [Server]: " + wsid);

      for (let i = clients.size - 1; i >= 0; i--)
      {
        var clientWSID = [...clients][i][0];
        if(clientWSID !== serverID)
        {
          [...clients][i][1].ws.send("OnLostServerEvent(" + wsid + ")");
          [...clients][i][1].ws.close();
          clients.delete([...clients][i][0]);
        }
      }
    }
    else
    {
      //on client disconnected
      console.log("Disconnected [Client]: " + wsid);
      if(serverWS !== null) serverWS.send("OnClientDisconnectedEvent(" + wsid + ")");
    }

    clients.delete(wsid);
    console.log("connection count: " + clients.size);
  });

  ws.on('message', function incoming(message) {
    //var decodeString = new String(message);
    //console.log(decodeString);

    //check registration
    if(message.length === 4)
    {
      if(message[0] === 0 && message[1] === 0 && message[2] === 9 && message[3] === 3)
      {
        serverID = wsid;
        serverWS = ws;
        console.log("regServer: " + wsid + "[Server] " + serverID);
        clients.get(wsid).networkType = 'server';

        for (let i = 0; i < clients.size; i++) {
          var clientWSID = [...clients][i][0];
          if(clientWSID !== wsid)
          {
            [...clients][i][1].ws.send("OnFoundServerEvent(" + wsid + ")");
          }
          if(clientWSID !== serverID) serverWS.send("OnClientConnectedEvent(" + clientWSID + ")");
        }
      }
      else if(message[0] === 0 && message[1] === 0 && message[2] === 9 && message[3] === 4)
      {
        console.log("regClient: " + wsid + "[Server] " + serverID);
        clients.get(wsid).networkType = 'client';

        if(serverWS !== null)
        {
          //tell server about the new connected client
          serverWS.send("OnClientConnectedEvent(" + wsid + ")");

          //tell client about the existing server
          ws.send("OnFoundServerEvent(" + serverID + ")");
        }
      }
    }

    // if(message.length > 4 && message[0] === 0)
    if(message.length > 4)
    {
      if (serverID !== 'undefined')
      {
          switch(message[1])
          {
              //emit type: all;
              case 0:
                for (let i = 0; i < clients.size; i++) {
                  var clientWSID = [...clients][i][0];
                  if(clientWSID !== serverID)
                  {
                    [...clients][i][1].ws.send(message);
                  }
                }
                //stream serverWS as the last one
                serverWS.send(message);
                break;
              //emit type: server;
              case 1:
                serverWS.send(message);
                break;
              //emit type: others;
              case 2:
                for (let i = 0; i < clients.size; i++) {
                  var clientWSID = [...clients][i][0];
                  if(clientWSID !== wsid)
                  {
                    [...clients][i][1].ws.send(message);
                  }
                }
                break;
              case 3:
                  //send to target
                  var _wsidByteLength = ByteToInt16(message, 4);
                  //_wsidByteLength
                  var _wsidByte = message.slice(6, 6 + _wsidByteLength);
                  var _wsid = String.fromCharCode(..._wsidByte);
                  try{ clients.get(_wsid).ws.send(message); console.log("work! " + _wsid) } catch{}
                  break;
          }
      }
      else
      {
          console.log('cannot find any active server');
      }
    }
  });
});
