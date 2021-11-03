using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CrawlerVerifier.Model;
using Nethereum.RPC.Eth.DTOs;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CrawlerVerifier
{
    public class Function
    {
        public async Task<string> FunctionHandler(VerifiObject data, ILambdaContext context)
        {
            Lambda._context = context;
            Lambda.Log($"Get data from loader: {data}");

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

            Lambda.Log($"RPC logs count: {rpcData.Count}");
            if (rpcData.Count == 0)
            {
                // update block
                data.Contract.LastBlockRPC += data.RPCCount;
                await Lambda.Run("LambdaDbUpdater", JsonSerializer.Serialize(data.Contract));
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

            Lambda.Log($"Covalent logs count: {covalentData.Count}");
            if (covalentData.Count == 0)
            {
                // update block
                data.Contract.LastBlockWS += data.CovalentCount;
                await Lambda.Run("LambdaDbUpdater", JsonSerializer.Serialize(data.Contract));
            }

            // CHECKING DATA
            List<FilterLog> logs = CreateLogsList(rpcData, covalentData);

            // RUN LOG SAVER
            logs = logs.OrderBy(x => x.BlockNumber).ToList();


            foreach (var log in logs)
            {
                if (DateTime.UtcNow - start > TimeSpan.FromMinutes(4.5))
                    return $"Work time: {DateTime.UtcNow - start}";

                //SaveLog(log, )
            }

            // https://api.covalenthq.com/v1/56/events/address/0x537509C227b4F69F3871a665Ef25E85c92d39ed8/?starting-block=9010206&ending-block=9010306&page-size=100&limit=100



            return $"Work time: {DateTime.UtcNow - start}";
        }

        //public async Task<string> SaveLog(FilterLog log)
        //{

        //}

        public List<FilterLog> CreateLogsList(List<FilterLog> rpc, List<FilterLog> covalent)
        {
            List<FilterLog> saveList = new List<FilterLog>();

            foreach (var log in rpc)
            {
                if (saveList.FirstOrDefault(x => x.TransactionHash == log.TransactionHash && x.LogIndex == log.LogIndex) == null)
                {
                    saveList.Add(log);
                }
            }

            foreach (var log in covalent)
            {
                if (saveList.FirstOrDefault(x => x.TransactionHash == log.TransactionHash && x.LogIndex == log.LogIndex) == null)
                {
                    saveList.Add(log);
                }
            }

            return saveList;
        }

        public async Task<List<FilterLog>> RunDownloader(string name, DownloaderObject downloader)
        {
            var test = JsonSerializer.Serialize(downloader);
            return JsonSerializer.Deserialize<List<FilterLog>>(await Lambda.Run("CrawlerDownloader" + name, JsonSerializer.Serialize(downloader)));
        }
    }
}
