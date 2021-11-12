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
            var contract = data.Contract;

            Lambda._context = context;
            Lambda.Log($"Get data from loader by contract: ID {contract.Id}, address {contract.Address}");

            DateTime start = DateTime.UtcNow;

            if (contract.LastBlockRPC < contract.CreationBlock)
            {
                contract.LastBlockRPC = contract.CreationBlock;
            }

            if (contract.LastBlockWS < contract.CreationBlock)
            {
                contract.LastBlockWS = contract.CreationBlock;
            }


            Lambda.Log("Run rpc downloader");
            var rpcData = await RunDownloader("RPC", new DownloaderObject()
            {
                From = contract.LastBlockRPC,
                To = contract.LastBlockRPC + data.RPCCount,
                Connections = data.Connections,
                ContractAddress = contract.Address,
                ChainId = contract.ChainId
            });

            if (rpcData != null)
            {
                Lambda.Log($"RPC logs count: {rpcData.Count}");
                if (rpcData.Count == 0)
                {
                    // update block

                    var from = contract.LastBlockRPC;
                    contract.LastBlockRPC += data.RPCCount;

                    var contractId = await UpdateInDb("contract", contract);

                    Lambda.Log($"Update contract {contractId} last block RPC from: {from} to: {contract.LastBlockRPC}");
                }
            }
            else
            {
                rpcData = new List<Log>();
                Lambda.Log($"rpc downloader return null, contract: ID {contract.Id}, address {contract.Address}");
            }

            if (rpcData.Count != 0)
            {
                Lambda.Log($"Run save rpc logs, {rpcData.Count} count");
                Lambda.Log($"Last block RPC: {contract.LastBlockRPC}");

                for (int i = 0; i < rpcData.Count; i++)
                {
                    Lambda.Log($"log block num:  {rpcData[i].BlockNumber}");
                }

                await SaveLogs(rpcData, contract, 0, start);
            }

            Lambda.Log("Run covalent downloader");
            var covalentData = await RunDownloader("Covalent", new DownloaderObject()
            {
                From = contract.LastBlockWS,
                To = contract.LastBlockWS + data.CovalentCount,
                Connections = data.Connections,
                ContractAddress = contract.Address,
                ChainId = contract.ChainId
            });

            if (covalentData != null)
            {
                Lambda.Log($"Covalent logs count: {covalentData.Count}");
                if (covalentData.Count == 0)
                {
                    // update block

                    var from = contract.LastBlockWS;
                    contract.LastBlockWS += data.CovalentCount;

                    var contractId = await UpdateInDb("contract", contract);

                    Lambda.Log($"Update contract {contractId} last block Covalent from: {from} to: {contract.LastBlockWS}");
                }
            }
            else
            {
                covalentData = new List<Log>();
                Lambda.Log($"covalent downloader return null, contract: ID {contract.Id}, address {contract.Address}");
            }

            if (covalentData.Count != 0)
            {
                Lambda.Log($"Run save covalent logs, {covalentData.Count} count");
                Lambda.Log($"Last block Covalent: {contract.LastBlockWS}");

                for (int i = 0; i < covalentData.Count; i++)
                {
                    Lambda.Log($"log block num:  {covalentData[i].BlockNumber}");
                }

                await SaveLogs(covalentData, contract, 1, start);
            }

            return $"Work time: {DateTime.UtcNow - start}";
        }

        public async Task<string> SaveLogs(List<Log> logs, Contract cont, int type, DateTime start)
        {
            var contract = await GetContract(cont);

            if (contract == null) 
                return null;

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
