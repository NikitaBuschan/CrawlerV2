namespace CrawlerDownloaderCovalent.Model
{
    public class Pagination
    {
        public bool has_more { get; set; }
        public int page_number { get; set; }
        public int page_size { get; set; }
        public int total_count { get; set; }
    }
}
