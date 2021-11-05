namespace CrawlerDownloaderCovalent.Model
{
    public class Pagination
    {
        public object has_more { get; set; }
        public int page_number { get; set; }
        public int page_size { get; set; }
        public object total_count { get; set; }
    }
}
