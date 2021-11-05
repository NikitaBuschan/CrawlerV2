namespace CrawlerLogSaver.Model
{
    public class Log
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string BlockHash { get; set; }
        public string BlockNumber { get; set; }
        public bool Removed { get; set; }
        public string TransactionIndex { get; set; }
    }
}
