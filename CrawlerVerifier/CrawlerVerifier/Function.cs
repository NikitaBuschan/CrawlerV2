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
            Lambda.Log($"Get data from loader by contract: {data.Contract.Id}");

            DateTime start = DateTime.UtcNow;

            // run downloader covalent
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
                    data.Contract.LastBlockRPC += data.RPCCount;
                    var contractId = await UpdateInDb("contract", data.Contract);

                    Lambda.Log($"Update contract {contractId} last block RPC to: {data.Contract.LastBlockRPC}");
                }
            }
            else
            {
                rpcData = new List<Log>();
                Lambda.Log($"rpc downloader return null, contract {data.Contract.Id}");
            }

            // run downloader covalent
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
                    data.Contract.LastBlockWS += data.CovalentCount;
                    var contractId = await UpdateInDb("contract", data.Contract);

                    Lambda.Log($"Update contract {contractId} last block Covalent to: {data.Contract.LastBlockWS}");
                }
            }
            else
            {
                covalentData = new List<Log>();
                Lambda.Log($"covalent downloader return null, contract {data.Contract.Id}");
            }

            // CHECKING DATA
            List<Log> logs = CreateLogsList(rpcData, covalentData, data.Contract);

            // RUN LOG SAVER
            logs = logs.OrderBy(x => x.BlockNumber).ToList();


            foreach (var log in logs)
            {
                if (DateTime.UtcNow - start > TimeSpan.FromMinutes(4.5))
                    return $"Work time: {DateTime.UtcNow - start}";

                RunLogSaver(log).ConfigureAwait(false).GetAwaiter();
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }

            return $"Work time: {DateTime.UtcNow - start}";
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

        public List<Log> CreateLogsList(List<Log> rpc, List<Log> covalent, Contract contract)
        {
            List<Log> saveList = new List<Log>();

            foreach (var log in rpc)
            {
                if (saveList.FirstOrDefault(x => x.Hash == log.Hash && x.LogIndex == log.LogIndex) == null)
                {
                    saveList.Add(new Log()
                    {
                        Type = 0,
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

            foreach (var log in covalent)
            {
                if (saveList.FirstOrDefault(x => x.Hash == log.Hash && x.LogIndex == log.LogIndex) == null)
                {
                    saveList.Add(new Log()
                    {
                        Type = 1,
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

            return saveList;
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
