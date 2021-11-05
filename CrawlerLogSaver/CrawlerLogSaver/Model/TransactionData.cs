using System.Numerics;

namespace CrawlerLogSaver.Model
{
    public class TransactionData
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Hash { get; set; }
        public BigInteger Value { get; set; }
    }
}
