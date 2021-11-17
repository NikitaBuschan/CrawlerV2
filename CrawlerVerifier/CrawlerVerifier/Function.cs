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
        public Contract Contract = null;
        public DateTime Start;
        public async Task<string> FunctionHandler(VerifiObject data, ILambdaContext context)
        {
            Contract = data.Contract;
            Lambda._context = context;
            Lambda.Log($"Get data from loader by contract: ID {Contract.Id}, address {Contract.Address}");

            Start = DateTime.UtcNow;

            // Work with RPC
            if (Contract.LastBlockRPC < Contract.CreationBlock)
            {
                Contract.LastBlockRPC = Contract.CreationBlock;
            }

            await Download(
                "RPC",
                new DownloaderObject()
                {
                    From = Contract.LastBlockRPC,
                    To = Contract.LastBlockRPC + data.RPCCount,
                    Connections = data.Connections,
                    Contract = Contract
                },
                data.RPCCount);

            // update contract
            Contract = await GetContract(Contract);

            // Work with Covalent
            if (Contract.LastBlockWS < Contract.CreationBlock)
            {
                Contract.LastBlockWS = Contract.CreationBlock;
            }

            await Download(
                "Covalent",
                new DownloaderObject()
                {
                    From = Contract.LastBlockWS,
                    To = Contract.LastBlockWS + data.CovalentCount,
                    Connections = data.Connections,
                    Contract = Contract
                },
                data.CovalentCount);

            Lambda.Log($"Work time: {DateTime.UtcNow - Start}");

            return $"Work time: {DateTime.UtcNow - Start}";
        }

        public async Task Download(string name, DownloaderObject downloaderObject, int count)
        {
            Lambda.Log($"Run {name} downloader");

            var data = await RunDownloader(name, downloaderObject);

            if (data == null)
                return;

            Lambda.Log($"{name} logs count: {data.Count}");

            if (data.Count == 0)
            {
                Int32 from = 0;
                Int32 to = 0;

                if (name == "RPC")
                {
                    from = Contract.LastBlockRPC;
                    Contract.LastBlockRPC += count;
                    to = Contract.LastBlockRPC;
                }
                else
                {
                    from = Contract.LastBlockWS;
                    Contract.LastBlockWS += count;
                    to = Contract.LastBlockWS;
                }

                var contractId = await UpdateInDb("contract", Contract);

                Lambda.Log($"Update contract ID {contractId} Address {Contract.Address} last block {name} from {from} -> {to}");
            }
            else
            {
                for (int i = 0; i < data.Count; i++)
                    Lambda.Log($"log block num:  {data[i].BlockNumber}");
                Lambda.Log($"Run SaveLogs for {name} with {data.Count} logs");

                await SaveLogs(
                    data, 
                    Contract, 
                    name == "RPC" ? 0 : 1);
            }
        }

        public async Task<string> SaveLogs(List<Log> logs, Contract cont, int type)
        {
            List<Log> saveList = new List<Log>();

            foreach (var log in logs)
            {
                if (saveList.FirstOrDefault(x => x.Hash == log.Hash && Convert.ToInt32(x.LogIndex) == Convert.ToInt32(log.LogIndex)) == null)
                {
                    saveList.Add(new Log()
                    {
                        Type = type,
                        Contract = null,
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
                if (DateTime.UtcNow - Start > TimeSpan.FromMinutes(4.5))
                    return $"Work time: {DateTime.UtcNow - Start}";

                var contract = await GetContract(cont);

                if (contract == null)
                    return null;

                item.Contract = contract;

                await RunLogSaver(item);
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

        public async Task<Contract> GetContract(Contract contract)
        {
            var data = new DbObject()
            {
                Name = "contract",
                Data = JsonSerializer.Serialize(contract)
            };

            var result = await Lambda.Run("DBReader", JsonSerializer.Serialize(data));

            if (result == null)
                return null;

            return JsonSerializer.Deserialize<Contract>(result);
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
