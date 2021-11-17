using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CrawlerDownloaderCovalent.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CrawlerDownloaderCovalent
{
    public class Function
    {
        public static CancellationTokenSource CancellationToken;
        public DateTime Start;
        public TimeSpan WorkTime;
        public List<Log> Result;

        public async Task<List<Log>> FunctionHandler(DownloaderObject data, ILambdaContext context)
        {
            Start = DateTime.UtcNow;
            CancellationToken = new CancellationTokenSource();
            WorkTime = TimeSpan.FromMinutes(Convert.ToDouble(Environment.GetEnvironmentVariable("WorkTimeInMinutes")));
            Result = new List<Log>();

            Lambda._context = context;

            Lambda.Log($"Get data from verifier contract: ID {data.Contract.Id} Address {data.Contract.Address}");

            // Check for end of working time
            CheckForRefresh().ConfigureAwait(false).GetAwaiter();

            var logs = GetLogs(
                data.Contract.ChainId,
                data.Contract.Address,
                data.From,
                data.To,
                data.To - data.From);

            if (logs == null)
                return Result;

            var list = CreateClearList(logs);
            Lambda.Log($"Create clear list with {list.Count} logs");

            await Task.Run(async () =>
            {
                CreateLogList(list, data.Contract.ChainId).ConfigureAwait(false).GetAwaiter();

                while (!CancellationToken.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(1));

            }, CancellationToken.Token);

            Lambda.Log($"Return {Result.Count} logs");
            return Result;
        }


        public async Task CheckForRefresh()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow - Start > WorkTime)
                    CancellationToken.Cancel();

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        public List<Item> GetLogs(int chainId, string address, Int32 from, Int32 to, Int32 count)
        {
            var keys = new List<string>()
            {
                "32589216c5ac4a279361594588e",
                "dd18afc253ad477cb3190daa779"
            };

            foreach (var key in keys)
            {
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        var json = DownloadData(wc, $"https://api.covalenthq.com/v1/{chainId}/events/address/{address}/?starting-block={from}&ending-block={to}&page-size={count}&limit={count}&key=ckey_{key}");

                        if (json != null)
                        {
                            var res = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                            Root a = JsonSerializer.Deserialize<Root>(res.ToString());

                            Lambda.Log($"Get {a.data.items.Count} logs");
                            return a.data.items;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Lambda.Log($"Get logs get error: {ex.Message}");
                    continue;
                }
            }

            Lambda.Log($"Get null logs");
            return null;
        }

        public List<Item> CreateClearList(List<Item> list)
        {
            var res = new List<Item>();
            var maxCount = 15;

            foreach (var item in list)
            {
                if (res.Count >= maxCount)
                {
                    return res;
                }

                if (res.FirstOrDefault(
                    x =>
                    x.tx_hash == item.tx_hash &&
                    x.log_offset == item.log_offset &&
                    x.block_height == item.block_height) == null)
                {
                    res.Add(item);
                }
            }

            return res;
        }

        public async Task CreateLogList(List<Item> list, int chainId)
        {
            var i = 1;

            foreach (var log in list)
            {
                Lambda.Log($"Get transaction logs by {i} log");

                var transactionLogs = GetTransactionByHash(log.tx_hash, chainId);
                await Task.Delay(TimeSpan.FromSeconds(2.5));

                if (transactionLogs == null)
                {
                    Lambda.Log($"Get transaction {i} return null; Return {Result.Count} logs");
                    return;
                }

                foreach (var transaction in transactionLogs)
                {
                    foreach (var e in transaction.log_events)
                    {
                        if (Result.FirstOrDefault(x => Convert.ToInt32(x.LogIndex) == e.log_offset && x.Hash == transaction.tx_hash) == null)
                        {
                            Result.Add(new Log()
                            {
                                LogIndex = e.log_offset.ToString(),
                                Data = e.raw_log_data,
                                Topics = e.raw_log_topics,
                                TransactionIndex = transaction.tx_offset.ToString(),
                                Removed = transaction.successful,
                                BlockNumber = transaction.block_height.ToString(),
                                BlockHash = "",
                                Hash = transaction.tx_hash,
                                From = transaction.from_address,
                                To = transaction.to_address,
                                Value = transaction.value
                            });
                        }
                    }
                }

                i++;
            }

            CancellationToken.Cancel();
        }


        public List<Model.TransactionItem.Item> GetTransactionByHash(string hash, int chainId)
        {
            var keys = new List<string>()
            {
                "32589216c5ac4a279361594588e",
                "dd18afc253ad477cb3190daa779"
            };

            foreach (var key in keys)
            {
                using (WebClient wc = new WebClient())
                {
                    try
                    {

                        var json = DownloadData(wc, $"https://api.covalenthq.com/v1/{chainId}/transaction_v2/{hash}/?&key=ckey_{key}");

                        var res = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                        Model.TransactionItem.Root a = JsonSerializer.Deserialize<Model.TransactionItem.Root>(res.ToString());

                        return a.data.items;

                    }
                    catch (Exception ex)
                    {
                        Lambda.Log($"Get transaction by hash get error: {ex.Message}");
                        continue;
                    }
                }
            }

            return null;
        }

        public string DownloadData(WebClient client, string str)
        {
            try
            {
                return client.DownloadString(str);
            }
            catch (TimeoutException ex)
            {
                Lambda.Log($"Download data error, {ex.Message}");
                return null;
            }
        }
    }
}
