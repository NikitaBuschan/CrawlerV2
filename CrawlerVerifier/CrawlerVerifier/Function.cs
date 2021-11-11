using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CrawlerVerifier.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CrawlerVerifier
{
    public class Function
    {
        public async Task<string> FunctionHandler(VerifiObject data, ILambdaContext context)
        {
            Lambda._context = context;
            Lambda.Log($"Get data from loader by contract: ID {data.Contract.Id}, address {data.Contract.Address}");

            DateTime start = DateTime.UtcNow;

            if (data.Contract.LastBlockRPC < data.Contract.CreationBlock)
            {
                data.Contract.LastBlockRPC = data.Contract.CreationBlock;
            }

            if (data.Contract.LastBlockWS < data.Contract.CreationBlock)
            {
                data.Contract.LastBlockWS = data.Contract.CreationBlock;
            }


            Lambda.Log("Run rpc downloader");
            var rpcData = await RunDownloader("RPC", new DownloaderObject()
            {
                From = data.Contract.LastBlockRPC,
                To = data.Contract.LastBlockRPC + data.RPCCount,
                Connections = data.Connections,
                ContractAddress = data.Contract.Address,
                ChainId = data.Contract.ChainId
            });

            if (rpcData != null)
            {
                Lambda.Log($"RPC logs count: {rpcData.Count}");
                if (rpcData.Count == 0)
                {
                    // update block

                    var from = data.Contract.LastBlockWS;
                    data.Contract.LastBlockRPC += data.RPCCount;

                    var contractId = await UpdateInDb("contract", data.Contract);

                    Lambda.Log($"Update contract {contractId} last block RPC from: {from} to: {data.Contract.LastBlockRPC}");
                }
            }
            else
            {
                rpcData = new List<Log>();
                Lambda.Log($"rpc downloader return null, contract: ID {data.Contract.Id}, address {data.Contract.Address}");
            }

            Lambda.Log($"Run save rpc logs, {rpcData.Count} count");
            SaveLogs(rpcData, data.Contract, 0, start);


            Lambda.Log("Run covalent downloader");
            var covalentData = await RunDownloader("Covalent", new DownloaderObject()
            {
                From = data.Contract.LastBlockWS,
                To = data.Contract.LastBlockWS + data.CovalentCount,
                Connections = data.Connections,
                ContractAddress = data.Contract.Address,
                ChainId = data.Contract.ChainId
            });

            if (covalentData != null)
            {
                Lambda.Log($"Covalent logs count: {covalentData.Count}");
                if (covalentData.Count == 0)
                {
                    // update block

                    var from = data.Contract.LastBlockWS;
                    data.Contract.LastBlockWS += data.CovalentCount;

                    var contractId = await UpdateInDb("contract", data.Contract);

                    Lambda.Log($"Update contract {contractId} last block Covalent from: {from} to: {data.Contract.LastBlockWS}");
                }
            }
            else
            {
                covalentData = new List<Log>();
                Lambda.Log($"covalent downloader return null, contract: ID {data.Contract.Id}, address {data.Contract.Address}");
            }


            Lambda.Log($"Run save covalent logs, {covalentData.Count} count");
            SaveLogs(covalentData, data.Contract, 1, start);

            return $"Work time: {DateTime.UtcNow - start}";
        }

        public string SaveLogs(List<Log> logs, Contract contract, int type, DateTime start)
        {
            List<Log> saveList = new List<Log>();

            foreach (var log in logs)
            {
                if (saveList.FirstOrDefault(x => x.Hash == log.Hash && x.LogIndex == log.LogIndex) == null)
                {
                    saveList.Add(new Log()
                    {
                        Type = type,
                        Contract = contract,
                        LogIndex = log.LogIndex,
                        Data = log.Data,
                        Topics = log.Topics,
                        TransactionIndex = log.TransactionIndex,
                        Removed = log.Removed,
                        BlockNumber = log.BlockNumber,
                        BlockHash = log.BlockHash,
                        Hash = log.Hash,
                        From = log.From,
                        To = log.To,
                        Value = log.Value
                    });
                }
            }

            foreach (var item in saveList)
            {
                if (DateTime.UtcNow - start > TimeSpan.FromMinutes(4.5))
                    return $"Work time: {DateTime.UtcNow - start}";

                RunLogSaver(item).ConfigureAwait(false).GetAwaiter();
            }

            return "All good";
        }

        public async Task<string> RunLogSaver(Log log) =>
            await Lambda.Run("CrawlerLogSaver", JsonSerializer.Serialize(log));

        public async Task<string> UpdateInDb(string name, Contract contract)
        {
            var data = new DbObject()
            {
                Name = name,
                Data = JsonSerializer.Serialize(contract)
            };

            Lambda.Log($"Update {name} in DB");

            return await Lambda.Run("LambdaDbUpdater", JsonSerializer.Serialize(data));
        }

        public async Task<List<Log>> RunDownloader(string name, DownloaderObject downloader)
        {
               var result = await Lambda.Run("CrawlerDownloader" + name, JsonSerializer.Serialize(downloader));

            if (result == null)
                return null;

            return JsonSerializer.Deserialize<List<Log>>(result);
        }
    }
}
