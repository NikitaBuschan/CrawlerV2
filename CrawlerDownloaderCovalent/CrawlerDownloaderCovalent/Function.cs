using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CrawlerDownloaderCovalent.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CrawlerDownloaderCovalent
{
    public class Function
    {
        public async Task<List<Log>> FunctionHandler(DownloaderObject data, ILambdaContext context)
        {
            Lambda._context = context;
            Lambda.Log($"Get data from verifier: {data}");

            var logs = GetLogs(
                data.ChainId,
                data.ContractAddress,
                data.From,
                data.To,
                data.To - data.From);

            if (logs == null)
            {
                Lambda.Log($"Get logs return null");
                return null;
            }

            var list = await CreateLogList(logs, data.ChainId);

            List<Log> result = new List<Log>();

            var logsCount = 50;

            if (list.Count > logsCount)
            {
                result.AddRange(list.GetRange(0, logsCount));
            }
            else
            {
                result.AddRange(list);
            }


            Lambda.Log($"Return {result.Count} logs");
            return result;
        }

        public async Task<List<Log>> CreateLogList(List<Item> list, int chainId)
        {
            List<Log> result = new List<Log>();

            foreach (var log in list)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                var transactionLogs = GetTransactionByHash(log.tx_hash, chainId);

                if (transactionLogs == null)
                {
                    Lambda.Log($"Return {result.Count} logs");
                    return result;
                }

                foreach (var transaction in transactionLogs)
                {
                    foreach (var e in transaction.log_events)
                    {
                        if (result.FirstOrDefault(x => x.LogIndex == e.log_offset.ToString() && x.Hash == transaction.tx_hash) == null)
                        {
                            result.Add(new Log()
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
            }

            return result;
        }

        public List<Model.TransactionItem.Item> GetTransactionByHash(string hash, int chainId)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    var json = wc.DownloadString($"https://api.covalenthq.com/v1/{chainId}/transaction_v2/{hash}/?&key=ckey_dd18afc253ad477cb3190daa779");

                    var res = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                    Model.TransactionItem.Root a = JsonSerializer.Deserialize<Model.TransactionItem.Root>(res.ToString());

                    return a.data.items;
                }
            }
            catch (Exception ex)
            {
                Lambda.Log(ex.Message);
                return null;
            }
        }

        public List<Item> GetLogs(int chainId, string address, Int32 from, Int32 to, Int32 count)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    var json = wc.DownloadString($"https://api.covalenthq.com/v1/{chainId}/events/address/{address}/?starting-block={from}&ending-block={to}&page-size={count}&limit={count}&key=ckey_32589216c5ac4a279361594588e");

                    var res = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                    Root a = JsonSerializer.Deserialize<Root>(res.ToString());

                    return a.data.items;
                }
            }
            catch (Exception ex)
            {
                Lambda.Log(ex.Message);
                return null;
            }

        }
    }
}
