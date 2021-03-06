using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CrawlerLoader.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CrawlerLoader
{
    public class Function
    {
        public async Task<string> FunctionHandler(ILambdaContext context)
        {
            Lambda._context = context;

            DateTime start = DateTime.UtcNow;

            List<Contract> contracts = await GetAllContracts();
            Lambda.Log($"Get {contracts.Count} contracts");


            var etherscanLastBlock = Convert.ToInt32(await GetLastBlock("etherscan last block"));
            Lambda.Log($"Get etherscan last block {etherscanLastBlock}");

            var etherscanConnections = new List<string>()
            {
                "https://mainnet.infura.io/v3/9594144e3adc45a195b4bf18a6d599da",
                "https://eth.getblock.io/mainnet/?api_key=ad36687c-3d48-448b-bd14-4c30d0fb9298",
                "https://eth.getblock.io/mainnet/?api_key=b2eac0c6-248f-4ea1-bca6-53400b80a0e7"
            };

            var binanceLastBlock = Convert.ToInt32(await GetLastBlock("binance last block"));
            Lambda.Log($"Get binance last block {binanceLastBlock}");

            var binanceConnections = new List<string>()
            {
                "https://bsc.getblock.io/mainnet/?api_key=955d2dfc-2270-4b80-ac95-94f28266fca7",
                "https://bsc.getblock.io/mainnet/?api_key=4a25e394-cd4b-4464-aa3d-a7a461b28a19",
                "https://bsc.getblock.io/mainnet/?api_key=b2eac0c6-248f-4ea1-bca6-53400b80a0e7",
                "https://bsc.getblock.io/mainnet/?api_key=e210ac89-08e5-4eea-8439-a71da611a1ab",
                "https://bsc.getblock.io/mainnet/?api_key=926c5fd7-ea2b-4eb9-a163-995cb73c2417",
                "https://bsc.getblock.io/mainnet/?api_key=ead64319-8fe9-4ea9-a2ce-3da91b8f76b6"
            };


            await RunVerifier(
                 etherscanLastBlock,
                 etherscanConnections,
                 contracts.Where(x => x.ChainId == 1).ToList());

            await RunVerifier(
                binanceLastBlock,
                binanceConnections,
                contracts.Where(x => x.ChainId == 56).ToList());

            return $"Work time: {DateTime.UtcNow - start}";
        }

        public async Task RunVerifier(Int32 lastBlock, List<string> connections, List<Contract> contracts)
        {
            int blocks = 10;

            foreach (var contract in contracts)
            {
                Lambda.Log($"Work with contract: ID {contract.Id}\nAddress {contract.Address}\nLast block RPC: {contract.LastBlockRPC}\nLast block Covalent {contract.LastBlockWS}");
                if (contract.LastBlockRPC + blocks < lastBlock || contract.LastBlockWS + blocks < lastBlock)
                {
                    Lambda.Log($"Get rpc count for ID {contract.Id}\nAddress {contract.Address}\nRPC LB: {contract.LastBlockRPC}\nLB: {lastBlock}");
                    var rpcCount = GetBlocksCount(contract.LastBlockRPC, lastBlock);

                    Lambda.Log($"Get rpc count for ID {contract.Id}\nAddress {contract.Address}\nCovalent LB: {contract.LastBlockWS}\nLB: {lastBlock}");
                    var covalentCount = GetBlocksCount(contract.LastBlockWS, lastBlock);

                    var verifier = new VerifiObject()
                    {
                        LastBlock = lastBlock,
                        RPCCount = rpcCount,
                        CovalentCount = covalentCount,
                        Connections = connections,
                        Contract = contract
                    };

                    var verifierString = System.Text.Json.JsonSerializer.Serialize(verifier);

                    Lambda.Log($"Run verifier for contract: id {contract.Id}, address {contract.Address}");

                    Lambda.Run("CrawlerVerifier", verifierString).ConfigureAwait(false).GetAwaiter();

                    await Task.Delay(TimeSpan.FromSeconds(8));
                }
            }
        }

        public Int32 GetBlocksCount(Int32 from, Int32 lastBlock)
        {
            var blocksLimit = 1000;
            var step = 10;

            if (from + step >= lastBlock)
            {
                Lambda.Log($"Blocks count: < 10. Return 0.");
                return 0;
            }

            for (int i = 0; i < blocksLimit; i += step)
            {
                if (from + i + step > lastBlock)
                {
                    Lambda.Log($"Blocks count: {i}");
                    return i;
                }
            }

            Lambda.Log($"Blocks count: {blocksLimit}");
            return blocksLimit;
        }

        public async Task<List<Contract>> GetAllContracts()
        {
            var dbObject = new DbObject()
            {
                Name = "allContracts",
                Data = ""
            };

            Lambda.Log("Get all contracts from DB");
            var result = await Lambda.Run("DBReader", System.Text.Json.JsonSerializer.Serialize(dbObject));

            return System.Text.Json.JsonSerializer.Deserialize<List<Contract>>(result);
        }

        public async Task<string> GetLastBlock(string data)
        {
            var dbObject = new DbObject()
            {
                Name = "dictionary",
                Data = data
            };

            Lambda.Log($"Get {data} from DB");
            var result = await Lambda.Run("DBReader", System.Text.Json.JsonSerializer.Serialize(dbObject));

            return System.Text.Json.JsonSerializer.Deserialize<DictionaryObject>(result).Value;
        }
    }
}
