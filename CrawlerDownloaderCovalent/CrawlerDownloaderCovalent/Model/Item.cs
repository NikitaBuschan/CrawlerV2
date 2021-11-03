using System;
using System.Collections.Generic;

namespace CrawlerDownloaderCovalent.Model
{
    public class Item
    {
        public DateTime block_signed_at { get; set; }
        public int block_height { get; set; }
        public int tx_offset { get; set; }
        public int log_offset { get; set; }
        public string tx_hash { get; set; }
        public object _raw_log_topics_bytes { get; set; }
        public List<string> raw_log_topics { get; set; }
        public int sender_contract_decimals { get; set; }
        public string sender_name { get; set; }
        public string sender_contract_ticker_symbol { get; set; }
        public string sender_address { get; set; }
        public object sender_address_label { get; set; }
        public string sender_logo_url { get; set; }
        public string raw_log_data { get; set; }
        public Decoded decoded { get; set; }
    }
}
