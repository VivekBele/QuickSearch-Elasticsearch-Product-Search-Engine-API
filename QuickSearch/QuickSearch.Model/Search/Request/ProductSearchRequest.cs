using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSearch.Model
{
    public class ProductSearchRequest
    {
        public string Term { get; set; }
        public string Category { get; set; }
        public string Brand { get; set; }
        public string SortField { get; set; }
        public string SortOrder { get; set; }
        public bool IsFromElastic { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}