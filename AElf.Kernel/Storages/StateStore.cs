using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;
using NLog;

namespace AElf.Kernel.Storages
{
    public class StateStore : IStateStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private readonly ILogger _logger;

        public StateStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
            _logger = LogManager.GetLogger(nameof(DataStore));
        }

        private static string GetKey(StatePath path)
        {
            return $"{GlobalConfig.StatePrefix}{path.GetHash().DumpHex()}";
        }

        public async Task SetAsync(StatePath path, byte[] value)
        {
            try
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var key = GetKey(path);
                _logger.Info("[##StatePath-M1]: Key-[{0}], Length-[{1}], Value-[{2}]", key, value.Length, StateValue.Create(value));
                await _keyValueDatabase.SetAsync(key, value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<byte[]> GetAsync(StatePath path)
        {
            try
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                var key = GetKey(path);
                var res = await _keyValueDatabase.GetAsync(key);
//                return res ?? new byte[0];
                return res;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<bool> PipelineSetDataAsync(Dictionary<StatePath, byte[]> pipelineSet)
        {
            try
            {
                var dict = pipelineSet.ToDictionary(kv => GetKey(kv.Key), kv => kv.Value);
                foreach (var key in dict.Keys)
                {
                    _logger.Info("[##StatePath-M2]: Key-[{0}], Length-[{1}], Value-[{2}]", key, dict[key].Length, StateValue.Create(dict[key]));
                }

                return await _keyValueDatabase.PipelineSetAsync(dict);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}