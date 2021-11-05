using System;
using System.Collections.Generic;
using System.Text;

namespace CrawlerLogSaver.Model
{
    public class Contract
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public int ChainId { get; set; }
        public int ContractType { get; set; }
        public int Company { get; set; }
        public Int32 LastBlockRPC { get; set; }
        public Int32 LastBlockWS { get; set; }
        public Int32 CreationBlock { get; set; }
    }
}
