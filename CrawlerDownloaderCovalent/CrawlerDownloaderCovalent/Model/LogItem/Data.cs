using System.Collections.Generic;

namespace CrawlerDownloaderCovalent.Model
{
    public class Data
    {
        public string updated_at { get; set; }
        public List<Item> items { get; set; }
        public Pagination pagination { get; set; }
    }
}
