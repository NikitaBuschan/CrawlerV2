using System;
using System.Collections.Generic;
using System.Text;

namespace CrawlerDownloaderRPC.Model
{
    public class DownloaderObject
    {
        public Int32 From { get; set; }
        public Int32 To { get; set; }
        public List<string> Connections { get; set; }
        public string ContractAddress { get; set; }
        public int ChainId { get; set; }
    }
}
