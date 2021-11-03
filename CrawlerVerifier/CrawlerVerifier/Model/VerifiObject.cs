using System;
using System.Collections.Generic;

namespace CrawlerVerifier.Model
{
    public class VerifiObject
    {
        public Int32 LastBlock { get; set; }
        public Int32 RPCCount { get; set; }
        public Int32 CovalentCount { get; set; }
        public List<string> Connections { get; set; }
        public Contract Contract { get; set; }
    }
}
