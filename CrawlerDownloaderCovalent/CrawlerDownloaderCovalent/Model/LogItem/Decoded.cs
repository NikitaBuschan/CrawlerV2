using System;
using System.Collections.Generic;
using System.Text;

namespace CrawlerDownloaderCovalent.Model.LogItem
{
    public class Decoded
    {
        public string name { get; set; }
        public string signature { get; set; }
        public List<Param> @params { get; set; }
    }
}
