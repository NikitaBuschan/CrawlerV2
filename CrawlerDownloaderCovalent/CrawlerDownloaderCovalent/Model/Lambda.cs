using Amazon.Lambda.Core;

namespace CrawlerDownloaderCovalent.Model
{
    public class Lambda
    {
        public static ILambdaContext _context;

        public static void Log(string str)
        {
            _context.Logger.LogLine(str);
        }
    }
}
