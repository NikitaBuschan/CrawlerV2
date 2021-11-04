using System.Collections.Generic;

namespace CrawlerDownloaderRPC.Model
{
    public class Log
    {
        public string LogIndex { get; set; }
        public string Data { get; set; }
        public List<string> Topics { get; set; }
        public string TransactionIndex { get; set; }
        public bool Removed { get; set; }
        public string BlockNumber { get; set; }
        public string BlockHash { get; set; }
        public string Hash { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Value { get; set; }
    }
}
