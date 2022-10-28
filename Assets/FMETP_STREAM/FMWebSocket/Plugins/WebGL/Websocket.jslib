mergeInto(LibraryManager.library, {
  //Unity 2021_2 or before: Pointer_stringify(_src);
  //Unity 2021_2 or above: UTF8ToString(_src);
  WebSocketAddSocketIO_2021_2: function (_src)
  {
    var src = UTF8ToString(_src);
    var sc = document.createElement("script");
    sc.setAttribute("src", src);
    document.head.appendChild(sc);
    console.log(">>>>>>>>> adding~ script");
  },

  WebSocketAddGZip_2021_2: function (_src)
  {
    var src = UTF8ToString(_src);
    var sc = document.createElement("script");
    sc.setAttribute("src", src);
    document.head.appendChild(sc);
    console.log(">>>>>>>>> adding~ script(GZip)");
  },

  WebSocketAddEventListeners_2021_2: function (_gameobject)
  {
    var gameobject = UTF8ToString(_gameobject);
    window.socketEvents = {};
    window.socketEventListener = function(event, data){
        var socketData = { socketEvent: event, eventData: typeof data === 'undefined' ? '' : JSON.stringify(data) };
        try { gameInstance.SendMessage(gameobject, 'InvokeEventCallback', JSON.stringify(socketData)); } catch(e) {}
        try { unityInstance.SendMessage(gameobject, 'InvokeEventCallback', JSON.stringify(socketData)); } catch(e) {}
    };
  },

  WebSocketConnect_2021_2: function (_src, _gameobject)
  {


      var src = UTF8ToString(_src);
      var gameobject = UTF8ToString(_gameobject);

      window.socketIO = io.connect(src);
      window.socketIO.on('connect', function(){
          try { gameInstance.SendMessage(gameobject, 'OnOpen'); } catch(e) {}
          try { unityInstance.SendMessage(gameobject, 'OnOpen'); } catch(e) {}
          try { gameInstance.SendMessage(gameobject, 'SetSocketID', window.socketIO.io.engine.id); } catch(e) {}
          try { unityInstance.SendMessage(gameobject, 'SetSocketID', window.socketIO.io.engine.id); } catch(e) {}
      });

      //==========================audio==========================
      console.log("before adding Audio!!!!!! Listener");

      var label_aud = 2001;
      var dataID_aud = 0;
      var dataLength_aud = 0;
      var receivedLength_aud = 0;
      var dataByte_aud = new Uint8Array(100);
      var ReadyToGetFrame_aud = true;
      var SourceSampleRate = 44100;
      var SourceChannels = 1;
      var ABuffer = new Float32Array(0);

      var startTime = 0;
      var audioCtx = new AudioContext();

      window.socketIO.on('OnReceiveData', function (data) {
          var _byteData = new Uint8Array(data.DataByte);
          var _label = ByteToInt16(_byteData, 0);
          if (_label == label_aud)
          {
            var _dataID = ByteToInt16(_byteData, 2);
            if (_dataID != dataID_aud) receivedLength_aud = 0;

            dataID_aud = _dataID;
            dataLength_aud = ByteToInt32(_byteData, 4);
            var _offset = ByteToInt32(_byteData, 8);
            var _GZipMode = (_byteData[12] == 1) ? true : false;

            if (receivedLength_aud == 0) dataByte_aud = new Uint8Array(dataLength_aud);
            receivedLength_aud += _byteData.length - 14;
            //----------------add byte----------------
            dataByte_aud = CombineInt8Array(dataByte_aud, _byteData.slice(14, _byteData.length), _offset);
            //----------------add byte----------------
            if (ReadyToGetFrame_aud)
            {
              if (receivedLength_aud == dataLength_aud) ProcessAudioData(dataByte_aud, _GZipMode);
            }
          }
      });

      window.socketIO.on('message', function(data){
        //Receive Regular raw message, events...
        //example on socket.io server: io.emit('message', 'send sth to allllll');
        try { gameInstance.SendMessage(gameobject, 'RegOnMessage', data); } catch(e) {}
        try { unityInstance.SendMessage(gameobject, 'RegOnMessage', data); } catch(e) {}
      });

      function ProcessAudioData(_byte, _GZipMode)
      {
          ReadyToGetFrame_aud = false;

          var bytes = new Uint8Array(_byte);
          if(_GZipMode)
          {
             var gunzip = new Zlib.Gunzip (bytes);
             bytes = gunzip.decompress();
          }

          //read meta data
          SourceSampleRate = ByteToInt32(bytes, 0);
          SourceChannels = ByteToInt32(bytes, 4);

          //conver byte[] to float
          var BufferData = bytes.slice(8, bytes.length);
          var AudioInt16 = new Int16Array(BufferData.buffer);

          //=====================playback=====================
          if(AudioInt16.length > 0) StreamAudio(SourceChannels, AudioInt16.length, SourceSampleRate, AudioInt16);
          //=====================playback=====================

          ReadyToGetFrame_aud = true;
      }

      function StreamAudio(NUM_CHANNELS, NUM_SAMPLES, SAMPLE_RATE, AUDIO_CHUNKS)
      {
          var audioBuffer = audioCtx.createBuffer(NUM_CHANNELS, (NUM_SAMPLES / NUM_CHANNELS), SAMPLE_RATE);
          for (var channel = 0; channel < NUM_CHANNELS; channel++)
          {
              // This gives us the actual ArrayBuffer that contains the data
              var nowBuffering = audioBuffer.getChannelData(channel);
              for (var i = 0; i < NUM_SAMPLES; i++)
              {
                  var order = i * NUM_CHANNELS + channel;
                  var localSample = 1.0/32767.0;
                  localSample *= AUDIO_CHUNKS[order];
                  nowBuffering[i] = localSample;
              }
          }
          var source = audioCtx.createBufferSource();
          source.buffer = audioBuffer;
          source.connect(audioCtx.destination);
          source.start(startTime);
          startTime += audioBuffer.duration;
      }
      function CombineInt8Array(a, b, offset)
      {
          var c = new Int8Array(a.length);
          c.set(a);
          c.set(b, offset);
          return c;
      }
      function ByteToInt32(_byte, _offset)
      {
          return (_byte[_offset] & 255) + ((_byte[_offset + 1] & 255) << 8) + ((_byte[_offset + 2] & 255) << 16) + ((_byte[_offset + 3] & 255) << 24);
      }
      function ByteToInt16(_byte, _offset)
      {
          return (_byte[_offset] & 255) + ((_byte[_offset + 1] & 255) << 8);
      }
      //==========================audio==========================
      for(var socketEvent in window.socketEvents)
      {
          window.socketIO.on(socketEvent, window.socketEvents[socketEvent]);
      }
  },

  WebSocketClose_2021_2: function ()
  {
    if(typeof window.socketIO !== 'undefined') window.socketIO.disconnect();
  },

  WebSocketEmitEvent_2021_2: function (_e)
  {
    var e = UTF8ToString(_e);
    if(typeof window.socketIO !== 'undefined') window.socketIO.emit(e);
  },

  WebSocketEmitData_2021_2: function (_e, _data)
  {
    var e = UTF8ToString(_e);
    var data = UTF8ToString(_data);
    var obj = JSON.parse(data);
    if(typeof window.socketIO !== 'undefined') window.socketIO.emit(e, obj);
  },

  WebSocketEmitEventAction_2021_2: function (_e, _packetId, _gameobject)
  {
    var e = UTF8ToString(_e);
    var packetId = UTF8ToString(_packetId);
    var gameobject = UTF8ToString(_gameobject);

    if(typeof window.socketIO !== 'undefined')
    {
        window.socketIO.emit(e, function(data){
            var ackData = {
                packetID: packetId,
                data: typeof data === 'undefined' ? '' : JSON.stringify(data)
            };
        });

        try { gameInstance.SendMessage(gameobject, 'InvokeAck', JSON.stringify(ackData)); } catch(e) {}
        try { unityInstance.SendMessage(gameobject, 'InvokeAck', JSON.stringify(ackData)); } catch(e) {}
    }
  },

  WebSocketEmitDataAction_2021_2: function (_e, _data, _packetId, _gameobject)
  {
    var e = UTF8ToString(_e);
    var data = UTF8ToString(_data);
    var obj = JSON.parse(data);
    var packetId = UTF8ToString(_packetId);
    var gameobject = UTF8ToString(_gameobject);

    if(typeof window.socketIO !== 'undefined')
    {
        window.socketIO.emit(e, obj, function(data){
            var ackData = { packetID: packetId, data: typeof data === 'undefined' ? '' : JSON.stringify(data) };
        });

        try { gameInstance.SendMessage(gameobject, 'InvokeAck', JSON.stringify(ackData)); } catch(e) {}
        try { unityInstance.SendMessage(gameobject, 'InvokeAck', JSON.stringify(ackData)); } catch(e) {}
    }
  },

  WebSocketOn_2021_2: function (_e)
  {
    var e = UTF8ToString(_e);
    if(typeof window.socketEvents[e] === 'undefined')
    {
        window.socketEvents[e] = function(data){ window.socketEventListener(e, data); };
        if(typeof window.socketIO !== 'undefined') window.socketIO.on(e, function(data){ window.socketEventListener(e, data); });
    }
  },

  IsSocketIOConnected_2021_2: function (_gameobject)
  {
    //socketio
    if(window.socketIO === null) return;
    var gameobject = UTF8ToString(_gameobject);
    if(window.socketIO.connected === true)
    {
      try { gameInstance.SendMessage(gameobject, 'RegWebSocketConnected'); } catch(e) {}
      try { unityInstance.SendMessage(gameobject, 'RegWebSocketConnected'); } catch(e) {}
    }
    else
    {
      try { gameInstance.SendMessage(gameobject, 'RegWebSocketDisconnected'); } catch(e) {}
      try { unityInstance.SendMessage(gameobject, 'RegWebSocketDisconnected'); } catch(e) {}
    }
  },

  REG_IsWebSocketConnected_2021_2: function (_gameobject)
  {
    //without socketIO
    if(websocket === null) return;
    var gameobject = UTF8ToString(_gameobject);
    if(websocket.readyState === WebSocket.OPEN || websocket.readyState === WebSocket.CLOSING)
    {
      try { gameInstance.SendMessage(gameobject, 'RegWebSocketConnected'); } catch(e) {}
      try { unityInstance.SendMessage(gameobject, 'RegWebSocketConnected'); } catch(e) {}
    }
    else
    {
      try { gameInstance.SendMessage(gameobject, 'RegWebSocketDisconnected'); } catch(e) {}
      try { unityInstance.SendMessage(gameobject, 'RegWebSocketDisconnected'); } catch(e) {}
    }
  },

  REG_WebSocketAddEventListeners_2021_2: function (_src, _gameobject)
  {
    var src = UTF8ToString(_src);
    var gameobject = UTF8ToString(_gameobject);

    websocket = new WebSocket(src);
    websocket.onopen = function(evt) { onOpen(evt) };
    websocket.onclose = function(evt) { onClose(evt) };
    websocket.onmessage = function(evt) { onMessage(evt) };
    websocket.onerror = function(evt) { onError(evt) };

    function onOpen(evt)
    {
      if(websocket === null) return;
      if(websocket.readyState === WebSocket.OPEN)
      {
        try { gameInstance.SendMessage(gameobject, 'RegOnOpen'); } catch(e) {}
        try { unityInstance.SendMessage(gameobject, 'RegOnOpen'); } catch(e) {}
      }
    }
    function onClose(evt)
    {
      if(websocket === null) return;
      try { gameInstance.SendMessage(gameobject, 'RegOnClose'); } catch(e) {}
      try { unityInstance.SendMessage(gameobject, 'RegOnClose'); } catch(e) {}
      websocket = null;
    }
    function onMessage(evt)
    {
      if(websocket === null) return;
      try { gameInstance.SendMessage(gameobject, 'RegOnMessage', evt.data); } catch(e) {}
      try { unityInstance.SendMessage(gameobject, 'RegOnMessage', evt.data); } catch(e) {}
    }
    function onError(evt)
    {
      if(websocket === null) return;
      try { gameInstance.SendMessage(gameobject, 'RegOnError', evt.data); } catch(e) {}
      try { unityInstance.SendMessage(gameobject, 'RegOnError', evt.data); } catch(e) {}
    }
  },

  REG_Close_2021_2: function (_e, _data)
  {
    if(websocket === null) return;
    if(websocket.readyState === WebSocket.OPEN) websocket.close();
  },

  REG_Send_2021_2: function(_e)
  {
    if(websocket === null) return;
    if(websocket.readyState === WebSocket.OPEN)
    {
      var e = UTF8ToString(_e);
      websocket.send(e);
    }
  },

});
