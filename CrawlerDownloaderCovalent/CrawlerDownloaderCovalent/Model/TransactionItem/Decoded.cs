using System;
using System.Collections.Generic;
using System.Text;

namespace CrawlerDownloaderCovalent.Model.TransactionItem
{
    public class Decoded
    {
        public string name { get; set; }
        public string signature { get; set; }
        public List<Param> @params { get; set; }
    }
}
