using System;
using System.Collections.Generic;
using System.Text;

namespace CrawlerDownloaderCovalent.Model.TransactionItem
{
    public class Root
    {
        public Data data { get; set; }
        public bool error { get; set; }
        public object error_message { get; set; }
        public object error_code { get; set; }
    }
}
