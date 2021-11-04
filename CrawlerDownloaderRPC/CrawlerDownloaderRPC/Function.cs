using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CrawlerDownloaderRPC.Model;
using Nethereum.BlockchainProcessing;
using Nethereum.BlockchainProcessing.LogProcessing;
using Nethereum.BlockchainProcessing.Orchestrator;
using Nethereum.BlockchainProcessing.Processor;
using Nethereum.BlockchainProcessing.ProgressRepositories;
using Nethereum.RPC.Eth.Blocks;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CrawlerDownloaderRPC
{
    public class Function
    {
        public static CancellationTokenSource cancellationToken = new CancellationTokenSource();
        public static DateTime refreshDataTime = new DateTime();

        public async Task<List<Log>> FunctionHandler(DownloaderObject data, ILambdaContext context)
        {
            Lambda._context = context;
            Lambda.Log($"Get data from verifier: {data}");

            List<FilterLog> logs = new List<FilterLog>();

            foreach (var connection in data.Connections)
            {
                var web3 = new Web3(connection);

                var list = await GetLogs(web3, data.From, data.To, data.ContractAddress);

                if (list != null)
                {
                    logs.AddRange(list);
                    break;
                }
            }

            List<Log> result = new List<Log>();

            foreach (var log in logs)
            {
                Transaction receipt = new Transaction();

                foreach (var connection in data.Connections)
                {
                    var web3 = new Web3(connection);
                    
                    try
                    {
                        receipt = GetReceipt(web3, log.TransactionHash);
                        if (receipt != null)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (receipt == null)
                {
                    Lambda.Log($"Return {result.Count} logs");
                    return result;
                }

                result.Add(new Log()
                {
                    LogIndex = log.LogIndex.Value.ToString(),
                    Data = log.Data,
                    Topics = CreateTopics(log.Topics),
                    TransactionIndex = log.TransactionIndex.Value.ToString(),
                    Removed = log.Removed,
                    BlockNumber = log.BlockNumber.Value.ToString(),
                    BlockHash = log.BlockHash,
                    Hash = log.TransactionHash,
                    From = receipt.From,
                    To = receipt.To,
                    Value = receipt.Value.Value.ToString()
                });
            }

            Lambda.Log($"Return {result.Count} logs");
            return result;
        }

        public Transaction GetReceipt(Web3 web3, string hash) =>
            web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(hash).Result;

        public List<string> CreateTopics(object[] topics)
        {
            var res = new List<string>();

            foreach (var topic in topics)
            {
                res.Add(topic.ToString());
            }

            return res;
        }

        public async Task<List<FilterLog>> GetLogs(Web3 web3, Int32 from, Int32 to, string address)
        {
            var logs = new List<FilterLog>();

            var filter = new NewFilterInput() { Address = new[] { address } };
            int DefaultBlocksPerBatch = to - from;
            const int RequestRetryWeight = 0;
            const int MinimumBlockConfirmations = 12;

            var logProcessorHandler = new ProcessorHandler<FilterLog>(
                action: (log) =>
                {
                    logs.Add(log);
                },
                criteria: (log) => log.Removed == false);

            IEnumerable<ProcessorHandler<FilterLog>> logProcessorHandlers = new ProcessorHandler<FilterLog>[] { logProcessorHandler };

            IBlockchainProcessingOrchestrator orchestrator = new LogOrchestrator(
                ethApi: web3.Eth,
                logProcessors: logProcessorHandlers,
                filterInput: filter,
                defaultNumberOfBlocksPerRequest: DefaultBlocksPerBatch,
                retryWeight: RequestRetryWeight);

            IBlockProgressRepository progressRepository = new InMemoryBlockchainProgressRepository();

            IWaitStrategy waitForBlockConfirmationsStrategy = new WaitStrategy();

            ILastConfirmedBlockNumberService lastConfirmedBlockNumberService =
            new LastConfirmedBlockNumberService(
                web3.Eth.Blocks.GetBlockNumber, waitForBlockConfirmationsStrategy, MinimumBlockConfirmations);

            var processor = new BlockchainProcessor(orchestrator, progressRepository, lastConfirmedBlockNumberService);

            Lambda.Log("Creting task (get logs)\n");

            cancellationToken = new CancellationTokenSource();

            refreshDataTime = DateTime.UtcNow;

            // Check for refresh
            CheckForRefresh().ConfigureAwait(false).GetAwaiter();

            bool result = false;

            result = await Task.Run(async () =>
            {
                try
                {
                    await processor.ExecuteAsync(
                    toBlockNumber: to,
                    startAtBlockNumberIfNotProcessed: from);
                }
                catch (Exception ex)
                {
                    Lambda.Log("Failed getting logs with exception: " + ex.Message);
                    return false;
                }

                return true;
            }, cancellationToken.Token);

            if (result == false)
            {
                Lambda.Log($"Finish task with null logs");
                return null;
            }

            Lambda.Log($"Finish task (get {logs.Count} logs)");

            return logs;
        }

        public async Task CheckForRefresh()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow - refreshDataTime > TimeSpan.FromSeconds(20))
                {
                    cancellationToken.Cancel();
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}
