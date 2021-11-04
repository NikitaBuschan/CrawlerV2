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
            List<Log> logs = CreateLogsList(rpcData, covalentData);

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
        
        public List<Log> CreateLogsList(List<Log> rpc, List<Log> covalent)
        {
            List<Log> saveList = new List<Log>();

            foreach (var log in rpc)
            {
                if (saveList.FirstOrDefault(x => x.Hash == log.Hash && x.LogIndex == log.LogIndex) == null)
                {
                    saveList.Add(log);
                }
            }

            foreach (var log in covalent)
            {
                if (saveList.FirstOrDefault(x => x.Hash == log.Hash && x.LogIndex == log.LogIndex) == null)
                {
                    saveList.Add(log);
                }
            }

            return saveList;
        }

        public async Task<List<Log>> RunDownloader(string name, DownloaderObject downloader) =>
            JsonSerializer.Deserialize<List<Log>>(await Lambda.Run("CrawlerDownloader" + name, JsonSerializer.Serialize(downloader)));
    }
}
