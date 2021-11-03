using System;
using System.Collections.Generic;
using System.Text;

namespace CrawlerVerifier.Model
{
    public class LogObject
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public string BlockHash { get; set; }
        public string BlockNumber { get; set; }
        public string Data { get; set; }
        public string LogIndex { get; set; }
        public bool Removed { get; set; }
        public string Topics { get; set; }
        public string TransactionHash { get; set; }
        public string TransactionIndex { get; set; }
    }
}
