using System;
using System.Collections.Generic;
using System.Text;

namespace CrawlerDownloaderCovalent.Model.TransactionItem
{
    public class Data
    {
        public string updated_at { get; set; }
        public List<Item> items { get; set; }
        public object pagination { get; set; }
    }
}
