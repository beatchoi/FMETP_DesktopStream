mergeInto(LibraryManager.library, {
  //Unity 2021_2 or before: Pointer_stringify(_src);
  //Unity 2021_2 or above: UTF8ToString(_src);
  FMWebSocket_AddGZip_2021_2: function (_src)
  {
    var src = UTF8ToString(_src);
    var sc = document.createElement("script");
    sc.setAttribute("src", src);
    document.head.appendChild(sc);
    console.log(">>>>>>>>> adding~ script(GZip)");
  },

  // FMWebSocket_IsWebSocketConnected_2021_2: function (_gameobject)
  // {
  //   if(websocket === null) return;
  //   var gameobject = UTF8ToString(_gameobject);
  //   if(websocket.readyState === WebSocket.OPEN || websocket.readyState === WebSocket.CLOSING)
  //   {
  //     try { gameInstance.SendMessage(gameobject, 'RegWebSocketConnected'); } catch(e) {}
  //     try { unityInstance.SendMessage(gameobject, 'RegWebSocketConnected'); } catch(e) {}
  //   }
  //   else
  //   {
  //     try { gameInstance.SendMessage(gameobject, 'RegWebSocketDisconnected'); } catch(e) {}
  //     try { unityInstance.SendMessage(gameobject, 'RegWebSocketDisconnected'); } catch(e) {}
  //   }
  // },
  FMWebSocket_IsWebSocketConnected_2021_2: function ()
  {
    var returnStr = "false";
    if(websocket !== null)
    {
      if(websocket.readyState === WebSocket.OPEN || websocket.readyState === WebSocket.CLOSING)
      {
        returnStr = "true";
      }
    }
    var bufferSize = lengthBytesUTF8(returnStr) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(returnStr, buffer, bufferSize);
    return buffer;
  },

  FMWebSocket_AddEventListeners_2021_2: function (_src, _gameobject)
  {
    var src = UTF8ToString(_src);
    var gameobject = UTF8ToString(_gameobject);

    websocket = new WebSocket(src);
    websocket.binaryType = 'arraybuffer';
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

    function onError(evt)
    {
      if(websocket === null) return;
      try { gameInstance.SendMessage(gameobject, 'RegOnError', evt.data); } catch(e) {}
      try { unityInstance.SendMessage(gameobject, 'RegOnError', evt.data); } catch(e) {}
    }

    //==========================audio variables==========================
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
    //==========================audio variables==========================

    function onMessage(evt)
    {
      if(websocket === null) return;
      if(typeof evt.data === "string")
      {
        // if(evt.data!=="heartbeat") console.log("this is string data!");

        try { gameInstance.SendMessage(gameobject, 'RegOnMessageString', evt.data); } catch(e) {}
        try { unityInstance.SendMessage(gameobject, 'RegOnMessageString', evt.data); } catch(e) {}
      }
      else if(evt.data instanceof ArrayBuffer)
      {
        var _byteRaw = new Uint8Array(evt.data);

        //==========================audio processing==========================
        var _byteData = _byteRaw.slice(4, _byteRaw.length);
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
        //==========================audio processing==========================

        // console.log("this is array buffer!");
        var bytes = new Uint8Array(_byteRaw);
        //----conver byte[] to Base64 string----
        var len = bytes.byteLength;
        var binary = '';
        for (var i = 0; i < len; i++) binary += String.fromCharCode(bytes[i]);
        var base64String = btoa(binary);
        //----conver byte[] to Base64 string----

        try { gameInstance.SendMessage(gameobject, 'RegOnMessageRawData', base64String); } catch(e) {}
        try { unityInstance.SendMessage(gameobject, 'RegOnMessageRawData', base64String); } catch(e) {}
      }
    }

    // begin Audio functions
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
    // end Audio functions
  },

  FMWebSocket_SendByte_2021_2: function(_array, _size)
  {
    const newArray = new ArrayBuffer(_size);
    const newByteArray = new Uint8Array(newArray);
    for(var i = 0; i < _size; i++) newByteArray[i] = HEAPU8[_array + i];
    websocket.send(newByteArray);
  },

  FMWebSocket_SendString_2021_2: function (_src)
  {
    var stringData = UTF8ToString(_src);
    websocket.send(stringData);
  },

  FMWebSocket_Close_2021_2: function (_e, _data)
  {
    if(websocket === null) return;
    if(websocket.readyState === WebSocket.OPEN) websocket.close();
  },
});
