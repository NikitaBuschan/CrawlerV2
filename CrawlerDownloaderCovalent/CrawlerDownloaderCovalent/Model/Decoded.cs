using System.Collections.Generic;

namespace CrawlerDownloaderCovalent.Model
{
    public class Decoded
    {
        public string name { get; set; }
        public string signature { get; set; }
        public List<Param> @params { get; set; }
    }
}
