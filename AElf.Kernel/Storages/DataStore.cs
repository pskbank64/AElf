using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database;
using System.Linq;
using AElf.Kernel.Types;
using Google.Protobuf;
using Org.BouncyCastle.Asn1.X509;
using AElf.Common;
using NLog;

namespace AElf.Kernel.Storages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class DataStore : IDataStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private readonly ILogger _logger;

        public DataStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
            _logger = LogManager.GetLogger(nameof(DataStore));
        }

        public async Task InsertAsync<T>(Hash pointerHash, T obj) where T : IMessage
        {
            try
            {
                if (pointerHash == null)
                {
                    throw new Exception("Point hash cannot be null.");
                }

                if (obj == null)
                {
                    throw new Exception("Cannot insert null value.");
                }

                var key = pointerHash.GetKeyString(typeof(T).Name);
                _logger.Info("[##DataStore]: Type-[{0}], Key-[{1}], Length-[{2}], Value-[{3}]",typeof(T).Name, key, obj.ToByteArray().Length, obj);
                await _keyValueDatabase.SetAsync("Default", key, obj.ToByteArray());
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                throw;
            }
        }

        public async Task InsertBytesAsync<T>(Hash pointerHash, byte[] obj) where T : IMessage
        {
            try
            {
                if (pointerHash == null)
                {
                    throw new Exception("Point hash cannot be null.");
                }

                if (obj == null)
                {
                    throw new Exception("Cannot insert null value.");
                }

                var key = pointerHash.GetKeyString(typeof(byte[]).Name);
                await _keyValueDatabase.SetAsync("Default", key, obj);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                throw;
            }
        }

        public async Task<T> GetAsync<T>(Hash pointerHash) where T : IMessage, new()
        {
            try
            {
                if (pointerHash == null)
                {
                    throw new Exception("Pointer hash cannot be null.");
                }
                
                var typeName = typeof(T).Name;
                var key = pointerHash.GetKeyString(typeof(T).Name);
                var res = await _keyValueDatabase.GetAsync(typeName,key);
                return  res == null ? default(T): res.Deserialize<T>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public async Task<byte[]> GetBytesAsync<T>(Hash pointerHash) where T : IMessage, new()
        {
            try
            {
                if (pointerHash == null)
                {
                    throw new Exception("Pointer hash cannot be null.");
                }
                
                var key = pointerHash.GetKeyString(typeof(byte[]).Name);
                return await _keyValueDatabase.GetAsync("Default", key);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task RemoveAsync<T>(Hash pointerHash) where T : IMessage
        {
            try
            {
                if (pointerHash == null)
                {
                    throw new Exception("Pointer hash cannot be null.");
                }

                var typeName = typeof(T).Name;
                var key = pointerHash.GetKeyString(typeName);
                await _keyValueDatabase.RemoveAsync(typeName,key);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}