using System;
using System.Collections.Generic;
using System.Text;

namespace CrawlerDownloaderCovalent.Model.TransactionItem
{
    public class Item
    {
        public DateTime block_signed_at { get; set; }
        public int block_height { get; set; }
        public string tx_hash { get; set; }
        public int tx_offset { get; set; }
        public bool successful { get; set; }
        public string from_address { get; set; }
        public object from_address_label { get; set; }
        public string to_address { get; set; }
        public object to_address_label { get; set; }
        public string value { get; set; }
        public double value_quote { get; set; }
        public int gas_offered { get; set; }
        public int gas_spent { get; set; }
        public long gas_price { get; set; }
        public double gas_quote { get; set; }
        public double gas_quote_rate { get; set; }
        public List<LogEvent> log_events { get; set; }
    }
}
