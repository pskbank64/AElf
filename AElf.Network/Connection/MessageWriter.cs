using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Common;
using K4os.Compression.LZ4;
using NLog;

[assembly: InternalsVisibleTo("AElf.Network.Tests")]
namespace AElf.Network.Connection
{

    public class WriteJob
    {
        public Message Message { get; set; }
        public Action<Message> SuccessCallback { get; set; }
    }
    /// <summary>
    /// This class performs writes to the underlying tcp stream.
    /// </summary>
    public class MessageWriter : IMessageWriter
    {
        private const int DefaultMaxOutboundPacketSize = 20148;

        private readonly ILogger _logger;
        private readonly NetworkStream _stream;

        private BlockingCollection<WriteJob> _outboundMessages;

        internal bool IsDisposed { get; private set; }

        /// <summary>
        /// This configuration property determines the maximum size an
        /// outgoing messages payload. If the payloads size is larger
        /// than this value, this message will be send in multiple sub
        /// packets.
        /// </summary>
        public int MaxOutboundPacketSize { get; set; } = DefaultMaxOutboundPacketSize;

        public MessageWriter(NetworkStream stream)
        {
            _outboundMessages = new BlockingCollection<WriteJob>();
            _stream = stream;

            _logger = LogManager.GetLogger(nameof(MessageWriter));
        }

        /// <summary>
        /// Starts the dequing of outgoing messages.
        /// </summary>
        public void Start()
        {
            Task.Run(() => DequeueOutgoingLoop()).ConfigureAwait(false);
        }

        public void EnqueueMessage(Message p, Action<Message> successCallback = null)
        {
            if (IsDisposed || _outboundMessages == null || _outboundMessages.IsAddingCompleted)
                return;

            try
            {
                _outboundMessages.Add(new WriteJob { Message = p, SuccessCallback = successCallback});
            }
            catch (Exception e)
            {
                _logger.Trace(e, "Exception while enqueue for outgoing message.");
            }
        }

        /// <summary>
        /// The main loop that sends queud up messages from the message queue.
        /// </summary>
        internal void DequeueOutgoingLoop()
        {
            while (!IsDisposed && _outboundMessages != null)
            {
                WriteJob job;

                try
                {
                    job = _outboundMessages.Take();
                }
                catch (Exception)
                {
                    Dispose(); // if already disposed will do nothing 
                    break;
                }

                var p = job.Message;

                if (p == null)
                {
                    _logger?.Warn("Cannot write a null message.");
                    continue;
                }

                try
                {
                    if (p.Payload.Length > MaxOutboundPacketSize)
                    {
                        //压缩测试1
                        Task.Run(() =>
                        {
                            var data = p.Payload;
                            Random rd = new Random();
                            switch (rd.Next(1,4))
                            {
                                case 1:
                                    //Test1-Compress
                                    var compressData1 = Compress(data);
                                    Decompress(compressData1);
                                    break;
                                case 2:
                                    //Test2-Encode
                                    var compressData2 = Encode(data);
                                    Decode(compressData2, data.Length);
                                    break;
                                case 3:
                                    //Test3-ComplexCompress
                                    var compressData3 = ComplexCompress(data, out var length1);
                                    ComplexDecompress(compressData3, length1);
                                    break;
                                case 4:
                                    //Test4-ComplexEncode
                                    var compressData4 = ComplexEncode(data, out var length2);
                                    ComplexDecode(compressData4, length2);
                                    break;
                            }
                        });

                        var partials = PayloadToPartials(p.Type, p.Payload, MaxOutboundPacketSize);

                        _logger?.Trace($"Message split into {partials.Count} packets.");

                        foreach (var msg in partials)
                        {
                            SendPartialPacket(msg);
                        }
                    }
                    else
                    {
                        // Send without splitting
                        SendPacketFromMessage(p);
                    }

                    job.SuccessCallback?.Invoke(p);
                }
                catch (Exception e) when (e is IOException || e is ObjectDisposedException)
                {
                    _logger?.Trace("Exception with the underlying socket or stream closed.");
                    Dispose();
                }
                catch (Exception e)
                {
                    _logger?.Trace(e, "Exception while dequeing message.");
                }
            }

            _logger?.Trace("Finished writting messages.");
        }

        internal List<PartialPacket> PayloadToPartials(int msgType, byte[] arrayToSplit, int chunckSize)
        {
            List<PartialPacket> splitted = new List<PartialPacket>();

            int sourceArrayLength = arrayToSplit.Length; 
            int wholePacketCount = sourceArrayLength / chunckSize;
            int lastPacketSize = sourceArrayLength % chunckSize;

            if (wholePacketCount == 0 && lastPacketSize <= 0)
                return null;

            for (int i = 0; i < wholePacketCount; i++)
            {
                byte[] slice = new byte[chunckSize];
                Array.Copy(arrayToSplit, i*chunckSize, slice, 0, MaxOutboundPacketSize);

                var partial = new PartialPacket {
                    Type = msgType, Position = i, TotalDataSize = sourceArrayLength, Data = slice
                };
                
                splitted.Add(partial);
            }
            
            if (lastPacketSize != 0)
            {
                byte[] slice = new byte[lastPacketSize];
                Array.Copy(arrayToSplit, wholePacketCount*chunckSize, slice, 0, lastPacketSize);
                
                var partial = new PartialPacket {
                    Type = msgType, Position = wholePacketCount, TotalDataSize = sourceArrayLength, Data = slice
                };
                
                // Set last packet flag to this packet
                partial.IsEnd = true;
                
                splitted.Add(partial);
            }
            else
            {
                splitted.Last().IsEnd = true;
            }

            return splitted;
        }

        internal void SendPacketFromMessage(Message p)
        {
            byte[] type = {(byte) p.Type};
            byte[] hasId = {p.HasId ? (byte) 1 : (byte) 0};
            byte[] isbuffered = {0};
            byte[] length = BitConverter.GetBytes(p.Length);
            byte[] arrData = p.Payload;

            byte[] b;

            if (p.HasId)
            {
                b = ByteArrayHelpers.Combine(type, hasId, p.Id, isbuffered, length, arrData);
            }
            else
            {
                b = ByteArrayHelpers.Combine(type, hasId, isbuffered, length, arrData);
            }

            _stream.Write(b, 0, b.Length);
        }

        internal void SendPartialPacket(PartialPacket p)
        {
            byte[] type = {(byte) p.Type};
            byte[] hasId = {p.HasId ? (byte) 1 : (byte) 0};
            byte[] isbuffered = {1};
            byte[] length = BitConverter.GetBytes(p.Data.Length);

            byte[] posBytes = BitConverter.GetBytes(p.Position);
            byte[] isEndBytes = p.IsEnd ? new byte[] {1} : new byte[] {0};
            byte[] totalLengthBytes = BitConverter.GetBytes(p.TotalDataSize);

            byte[] arrData = p.Data;

            byte[] b;
            if (p.HasId)
            {
                b = ByteArrayHelpers.Combine(type, hasId, p.Id, isbuffered, length, posBytes, isEndBytes, totalLengthBytes, arrData);
            }
            else
            {
                b = ByteArrayHelpers.Combine(type, hasId, isbuffered, length, posBytes, isEndBytes, totalLengthBytes, arrData);
            }

            _stream.Write(b, 0, b.Length);
        }

        internal byte[] Compress(byte[] data)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                MemoryStream ms = new MemoryStream();
                GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true);
                zip.Write(data, 0, data.Length);
                zip.Close();
                byte[] buffer = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(buffer, 0, buffer.Length);
                ms.Close();

                stopwatch.Stop();
                _logger.Info($"Compress: Before: {data.Length}, After: {buffer.Length}, Time: {stopwatch.ElapsedMilliseconds}ms");

                return buffer;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        internal byte[] Decompress(byte[] data)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                MemoryStream ms = new MemoryStream(data);
                GZipStream zip = new GZipStream(ms, CompressionMode.Decompress, true);
                MemoryStream msreader = new MemoryStream();
                byte[] buffer = new byte[0x1000];
                while (true)
                {
                    int reader = zip.Read(buffer, 0, buffer.Length);
                    if (reader <= 0)
                    {
                        break;
                    }
                    msreader.Write(buffer, 0, reader);
                }
                zip.Close();
                ms.Close();
                msreader.Position = 0;
                buffer = msreader.ToArray();
                msreader.Close();

                stopwatch.Stop();
                _logger.Info($"Decompress: Before: {data.Length}, After: {buffer.Length}, Time: {stopwatch.ElapsedMilliseconds}ms");

                return buffer;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        internal byte[] Encode(byte[] data)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var encoded = LZ4CodecHelper.Encode(data, 0, data.Length, LZ4Level.L00_FAST);

            stopwatch.Stop();
            _logger.Info($"Encode: Before: {data.Length}, After: {encoded.Length}, Time: {stopwatch.ElapsedMilliseconds}ms");

            return encoded;
        }

        internal byte[] Decode(byte[] data, int targetLength)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var decoded = LZ4CodecHelper.Decode(data, 0, data.Length, targetLength);

            stopwatch.Stop();
            _logger.Info($"Decode: Before: {data.Length}, After: {decoded.Length}, Time: {stopwatch.ElapsedMilliseconds}ms");

            return decoded;
        }

        internal byte[] ComplexCompress(byte[] data, out int targetLength)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var encoded = LZ4CodecHelper.Encode(data, 0, data.Length, LZ4Level.L00_FAST);
            targetLength = encoded.Length;

            MemoryStream ms = new MemoryStream();
            GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true);
            zip.Write(encoded, 0, targetLength);
            zip.Close();
            byte[] buffer = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(buffer, 0, buffer.Length);
            ms.Close();

            stopwatch.Stop();
            _logger.Info($"ComplexCompress: Before: {data.Length}, After: {buffer.Length}, Time: {stopwatch.ElapsedMilliseconds}ms");

            return buffer;
        }

        internal byte[] ComplexDecompress(byte[] data, int targetLength)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                MemoryStream ms = new MemoryStream(data);
                GZipStream zip = new GZipStream(ms, CompressionMode.Decompress, true);
                MemoryStream msreader = new MemoryStream();
                byte[] buffer = new byte[0x1000];
                while (true)
                {
                    int reader = zip.Read(buffer, 0, buffer.Length);
                    if (reader <= 0)
                    {
                        break;
                    }
                    msreader.Write(buffer, 0, reader);
                }
                zip.Close();
                ms.Close();
                msreader.Position = 0;
                buffer = msreader.ToArray();
                msreader.Close();

                var decoded = LZ4CodecHelper.Decode(buffer, 0, buffer.Length, targetLength);

                stopwatch.Stop();
                _logger.Info($"ComplexDecompress: Before: {data.Length}, After: {decoded.Length}, Time: {stopwatch.ElapsedMilliseconds}ms");

                return decoded;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        internal byte[] ComplexEncode(byte[] data, out int targetLength)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            MemoryStream ms = new MemoryStream();
            GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true);
            zip.Write(data, 0, data.Length);
            zip.Close();
            byte[] buffer = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(buffer, 0, buffer.Length);
            ms.Close();
            targetLength = buffer.Length;

            var encoded = LZ4CodecHelper.Encode(buffer, 0, buffer.Length, LZ4Level.L00_FAST);

            stopwatch.Stop();
            _logger.Info($"ComplexEncode: Before: {data.Length}, After: {encoded.Length}, Time: {stopwatch.ElapsedMilliseconds}ms");

            return buffer;
        }

        internal byte[] ComplexDecode(byte[] data, int targetLength)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var decoded = LZ4CodecHelper.Decode(data, 0, data.Length, targetLength);

                MemoryStream ms = new MemoryStream(decoded);
                GZipStream zip = new GZipStream(ms, CompressionMode.Decompress, true);
                MemoryStream msreader = new MemoryStream();
                byte[] buffer = new byte[0x1000];
                while (true)
                {
                    int reader = zip.Read(buffer, 0, buffer.Length);
                    if (reader <= 0)
                    {
                        break;
                    }
                    msreader.Write(buffer, 0, reader);
                }
                zip.Close();
                ms.Close();
                msreader.Position = 0;
                buffer = msreader.ToArray();
                msreader.Close();

                stopwatch.Stop();
                _logger.Info($"ComplexDecode data: Before: {data.Length}, After: {buffer.Length}, Time: {stopwatch.ElapsedMilliseconds}ms");

                return decoded;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        #region Closing and disposing

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            // Note that This will cause an IOException in the read loop.
            _stream?.Close();

            _outboundMessages?.CompleteAdding();
            _outboundMessages?.Dispose();
            _outboundMessages = null;

            IsDisposed = true;
        }

        #endregion
    }

    public class LZ4CodecHelper
    {
        public static byte[] Encode(
            byte[] source, int sourceIndex, int sourceLength, LZ4Level level)
        {
            var bufferLength = LZ4Codec.MaximumOutputSize(sourceLength);
            var buffer = new byte[bufferLength];
            var targetLength = LZ4Codec.Encode(
                source, sourceIndex, sourceLength, buffer, 0, bufferLength, level);
            if (targetLength == bufferLength)
                return buffer;

            var target = new byte[targetLength];
            Buffer.BlockCopy(buffer, 0, target, 0, targetLength);
            return target;
        }

        public static byte[] Decode(
            byte[] source, int sourceIndex, int sourceLength, int targetLength)
        {
            var result = new byte[targetLength];
            var decodedLength = LZ4Codec.Decode(
                source, sourceIndex, sourceLength, result, 0, targetLength);
            if (decodedLength != targetLength)
                throw new ArgumentException(
                    $"Decoded length does not match expected value: {decodedLength}/{targetLength}");

            return result;
        }
    }
}