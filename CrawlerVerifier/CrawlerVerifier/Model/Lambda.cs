using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace CrawlerVerifier.Model
{
    public class Lambda
    {
        public static ILambdaContext _context;

        public static void Log(string str)
        {
            _context.Logger.LogLine(str);
        }

        public static async Task<string> Run(string name, string data)
        {
            var request = CreateRequest(name, data);

            var client = Lambda.GetAmazonClient();
            var response = await client.InvokeAsync(request);

            if (response.Payload == null)
            {
                Log($"Lambda {name} return null");
                return null;
            }

            var sr = new StreamReader(response.Payload);
            JsonReader reader = new JsonTextReader(sr);

            var serilizer = new JsonSerializer();
            var op = serilizer.Deserialize(reader);

            if (op == null)
            {
                Log($"Lambda {name} return null");
                return null;
            }

            return op.ToString();
        }

        public static AmazonLambdaClient GetAmazonClient() =>
            new AmazonLambdaClient("AKIAR6FFL6C3VIURC6VF", "zGY8V292GP4nRpUUAPR65XFxBczwdSor1eGXhJgX", RegionEndpoint.USEast1);

        // Create request object
        public static InvokeRequest CreateRequest(string functionName, string request) =>
            new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = request
            };
    }
}
