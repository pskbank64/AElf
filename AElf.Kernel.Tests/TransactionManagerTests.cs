using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using AElf.Kernel.Persistence;
using AElf.Miner.TxMemPool;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class TransactionManagerTests
    {
        private ITransactionStore _store;

        public TransactionManagerTests(ITransactionStore store)
        {
            _store = store;
        }

        [Fact]
        public async Task TestInsert()
        {
            await _store.AddTransactionAsync(new Transaction
            {
                From = Address.FromRawBytes(Hash.Generate().ToByteArray()),
                To = Address.FromRawBytes(Hash.Generate().ToByteArray())
            });
        }

        [Fact]
        public async Task GetTest()
        {
            var t = BuildTransaction();
            var key = await _store.AddTransactionAsync(t);
            var td = await _store.GetTransaction(key);
            Assert.Equal(t, td);
        }
        
        public static Transaction BuildTransaction(Address adrTo = null, ulong nonce = 0, ECKeyPair keyPair = null)
        {
            keyPair = keyPair ?? new KeyPairGenerator().Generate();

            var tx = new Transaction();
            tx.From = keyPair.GetAddress();
            tx.To = adrTo ?? Address.FromRawBytes(Hash.Generate().ToByteArray());
            tx.IncrementId = nonce;
            tx.Sig = new Signature
            {
                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded())
            };
            tx.Fee = TxPoolConfig.Default.FeeThreshold + 1;
            tx.MethodName = "hello world";
            tx.Params = ByteString.CopyFrom(new Parameters
            {
                Params = { new Param
                {
                    IntVal = 1
                }}
            }.ToByteArray());

            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.DumpByteArray());
            
            // Update the signature
            tx.Sig.R = ByteString.CopyFrom(signature.R);
            tx.Sig.S = ByteString.CopyFrom(signature.S);
            return tx;
        }
    }
}