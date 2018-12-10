using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;

namespace AElf.Kernel.Persistence
{
    public class FunctionMetadataDao : IFunctionMetadataDao
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "FunctionMetadata";

        public FunctionMetadataDao(IKeyValueDatabase database)
        {
            _database = database;
        }
    }
}