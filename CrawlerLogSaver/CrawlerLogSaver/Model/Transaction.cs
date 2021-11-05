using System.ComponentModel.DataAnnotations;

namespace CrawlerLogSaver.Model
{
    public class Transaction
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string Hash { get; set; }
        public string Object { get; set; }
    }
}
