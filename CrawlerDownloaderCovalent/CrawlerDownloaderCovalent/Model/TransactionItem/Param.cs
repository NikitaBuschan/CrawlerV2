using System;
using System.Collections.Generic;
using System.Text;

namespace CrawlerDownloaderCovalent.Model.TransactionItem
{
    public class Param
    {
        public string name { get; set; }
        public string type { get; set; }
        public bool indexed { get; set; }
        public bool decoded { get; set; }
        public string value { get; set; }
    }
}
