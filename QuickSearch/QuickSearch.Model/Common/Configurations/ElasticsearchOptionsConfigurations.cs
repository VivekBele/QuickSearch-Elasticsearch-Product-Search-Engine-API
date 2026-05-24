using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSearch.Model
{
    public class ElasticsearchOptionsConfigurations
    {
        public Dictionary<string, string> Indices { get; set; }

        // When true, call the Elasticsearch Count API to get exact totals for queries.
        // When false, prefer the Search response total and fall back to Count only if missing.
        public bool UseExactCount { get; set; } = true;

        // Maximum page size to prevent large requests. If 0, no limit is enforced.
        public int MaxPageSize { get; set; } = 0;

        public string GetIndex(string key)
        {
            if (Indices.TryGetValue(key, out var indexName))
                return indexName;

            throw new KeyNotFoundException($"Index '{key}' not found in configuration.");
        }
    }
}