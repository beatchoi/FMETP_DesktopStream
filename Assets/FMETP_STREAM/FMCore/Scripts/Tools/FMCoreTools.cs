using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class FMCoreTools
{
    /// <summary>
    /// the simple way to get yield return value from coroutine
    /// yield return FMCoreTools.RunCOR<byte[]>(YourCOR, (output) => YourVariableForOutput = output);
    /// </summary>
    public static IEnumerator RunCOR<T>(IEnumerator target, Action<T> output)
    {
        object result = null;
        while (target.MoveNext())
        {
            result = target.Current;
            yield return result;
        }
        output((T)result);
    }

    /// <summary>
    /// the simple way to get yield return value from coroutine
    /// example script:
    /// yield return FMCoreTools.RunCOR<byte[]>(FMCoreTools.FMZippedByteCOR(dataByte), (output) => dataByte = output);
    /// </summary>
    public static IEnumerator FMZippedByteCOR(byte[] _inputByte)
    {
        byte[] _unzippedByte = new byte[0];
        if (Loom.numThreads < Loom.maxThreads - 1)
        {
            //reserve at least one thread for other purpose
            //need to clone a buffer for multi-threading
            byte[] _bufferByte = new byte[_inputByte.Length];
            Buffer.BlockCopy(_inputByte, 0, _bufferByte, 0, _inputByte.Length);

            bool AsyncEncoding = true;
            Loom.RunAsync(() =>
            {
                try { _unzippedByte = _bufferByte.FMZipBytes(); } catch { }
                AsyncEncoding = false;
            });
            while (AsyncEncoding) yield return null;
        }
        else { try { _unzippedByte = _inputByte.FMZipBytes(); } catch { } }
        yield return _unzippedByte;
    }

    /// <summary>
    /// the simple way to get yield return value from coroutine
    /// example script:
    /// yield return FMCoreTools.RunCOR<byte[]>(FMCoreTools.FMUnzippedByteCOR(inputByteData), (output) => inputByteData = output);
    /// </summary>
    public static IEnumerator FMUnzippedByteCOR(byte[] _inputByte)
    {
        byte[] _unzippedByte = new byte[0];
        //reserve at least one thread for other purpose
        if (Loom.numThreads < Loom.maxThreads - 1)
        {
            //reserve at least one thread for other purpose
            //need to clone a buffer for multi-threading
            byte[] _bufferByte = new byte[_inputByte.Length];
            Buffer.BlockCopy(_inputByte, 0, _bufferByte, 0, _inputByte.Length);

            bool AsyncEncoding = true;
            Loom.RunAsync(() =>
            {
                try { _unzippedByte = _bufferByte.FMUnzipBytes(); } catch { }
                AsyncEncoding = false;
            });
            while (AsyncEncoding) yield return null;
        }
        else { try { _unzippedByte = _inputByte.FMUnzipBytes(); } catch { } }
        yield return _unzippedByte;
    }

    /// <summary>
    /// convert byte[] to float[]
    /// </summary>
    public static float[] ToFloatArray(byte[] byteArray)
    {
        int len = byteArray.Length / 2;
        float[] floatArray = new float[len];
        for (int i = 0; i < byteArray.Length; i += 2)
        {
            floatArray[i / 2] = ((float)BitConverter.ToInt16(byteArray, i)) / 32767f;
        }
        return floatArray;
    }

    /// <summary>
    /// convert float to Int16 space
    /// </summary>
    public static Int16 FloatToInt16(float inputFloat)
    {
        inputFloat *= 32767;
        if (inputFloat < -32768) inputFloat = -32768;
        if (inputFloat > 32767) inputFloat = 32767;
        return Convert.ToInt16(inputFloat);
    }

    /// <summary>
    /// compare two int values, return true if they are similar referring to the sizeThreshold
    /// </summary>
    public static bool CheckSimilarSize(int inputByteLength1, int inputByteLength2, int sizeThreshold)
    {
        float diff = Mathf.Abs(inputByteLength1 - inputByteLength2);
        return diff < sizeThreshold;
    }
}