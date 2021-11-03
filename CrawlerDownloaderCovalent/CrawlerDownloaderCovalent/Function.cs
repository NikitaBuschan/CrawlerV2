using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using CrawlerDownloaderCovalent.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CrawlerDownloaderCovalent
{
    public class Function
    {
        public string FunctionHandler(DownloaderObject data, ILambdaContext context)
        {
            var logs = GetLogs(
                data.ChainId, 
                data.ContractAddress, 
                data.From, 
                data.To, 
                data.To - data.From);



            return input?.ToUpper();
        }

        public List<Item> GetLogs(int chainId, string address, Int32 from, Int32 to, Int32 count)
        {
            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString($"https://api.covalenthq.com/v1/{chainId}/events/address/{address}/?starting-block={from}&ending-block={to}&page-size={count}&limit={count}");

                var res = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                Root a = System.Text.Json.JsonSerializer.Deserialize<Root>(res.ToString());

                return a.data.items;
            }
        }
    }
}
