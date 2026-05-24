using System.Collections.Generic;

namespace QuickSearch.Model
{
    public class PagedResponse<T>
    {
        public IEnumerable<T> Items { get; set; }
        public long Total { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
