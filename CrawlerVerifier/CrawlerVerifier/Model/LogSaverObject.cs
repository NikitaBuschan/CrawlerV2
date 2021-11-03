using Nethereum.RPC.Eth.DTOs;
using System;
using System.Collections.Generic;

namespace CrawlerVerifier.Model
{
    public class LogSaverObject
    {
        public List<string> Connections { get; set; }
        public Int32 From { get; set; }
        public Contract Contract { get; set; }
        public FilterLog Log { get; set; }
    }
}
