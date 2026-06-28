using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Options;
using QuickSearch.DataAccess;
using QuickSearch.HelperUtilities;
using QuickSearch.LoggerUtility;
using QuickSearch.Model;

namespace QuickSearch.Api
{
    public class ProductServices : IProductServices
    {
        #region Fields
        private readonly ElasticsearchClient _client;
        private readonly ElasticsearchOptionsConfigurations _options;
        private readonly ILogger _logger;
        private readonly IProductDbService _productDbService;
        #endregion

        #region Constructors
        public ProductServices(
            ElasticsearchClient client, 
            IOptions<ElasticsearchOptionsConfigurations> options, 
            ILogger logger, 
            IProductDbService productDbService)
        {
            _client = client;
            _options = options.Value;
            _logger = logger;
            _productDbService = productDbService;
        }
        #endregion

        #region Public Methods
        public async Task<PagedResponse<ProductResponse>> GetAllProducts(int pageNumber, int pageSize, bool isFromElastic)
        {
            await _logger.LogAsync(new LoggerRequestModel
            {
                Message = "GetAllProducts execution started.",
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Source = "ProductServices"
            });

            try
            {
                if (isFromElastic)
                {
                    return await GetAllIndexProducts(pageNumber, pageSize);
                }
                else
                {
                    return await SearchDatabaseProducts(new ProductSearchRequest
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        IsFromElastic = false
                    });
                }
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in GetAllProducts: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductServices"
                });
                throw;
            }
            finally
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = "GetAllProducts execution ended.",
                    Timestamp = DateTime.UtcNow,
                    Level = "Information",
                    Source = "ProductServices"
                });
            }
        }

        public async Task<PagedResponse<ProductResponse>> GetAllIndexProducts(int pageNumber, int pageSize)
        {
            await _logger.LogAsync(new LoggerRequestModel
            {
                Message = "GetAllIndexProducts execution started.",
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Source = "ProductServices"
            });

            try
            {
                string index = _options.GetIndex(QuickSearchConstants.ProductIndex);

                int pn = Math.Max(1, pageNumber);
                int ps = Math.Max(1, pageSize);
                if (_options.MaxPageSize > 0)
                    ps = Math.Min(ps, _options.MaxPageSize);
                int from = (pn - 1) * ps;

                SearchResponse<ProductResponse> allProducts = await _client.SearchAsync<ProductResponse>(s => s
                    .Indices(index)
                    .From(from)
                    .Size(ps)
                    .TrackTotalHits(true)
                    .Query(q => q.MatchAll())
                );

                if (!allProducts.IsValidResponse)
                {
                    await _logger.LogAsync(new LoggerRequestModel
                    {
                        Message = "GetAllIndexProducts: Elasticsearch returned invalid response.",
                        Timestamp = DateTime.UtcNow,
                        Level = "Warning",
                        Source = "ProductServices"
                    });
                    return new PagedResponse<ProductResponse>
                    {
                        Items = Enumerable.Empty<ProductResponse>(),
                        Total = 0,
                        PageNumber = pn,
                        PageSize = ps
                    };
                }

                long total = 0;
                // If UseExactCount is configured, call Count API for exact totals. Otherwise prefer response total.
                if (_options.UseExactCount)
                {
                    try
                    {
                        var countResp = await _client.CountAsync<ProductResponse>(c => c.Indices(index).Query(q => q.MatchAll()));
                        if (countResp.IsValidResponse)
                        {
                            total = countResp.Count;
                            await _logger.LogAsync(new LoggerRequestModel
                            {
                                Message = $"GetAllIndexProducts: Count API returned {total} for index '{index}'",
                                Timestamp = DateTime.UtcNow,
                                Level = "Information",
                                Source = "ProductServices"
                            });
                        }
                        else
                        {
                            await _logger.LogAsync(new LoggerRequestModel
                            {
                                Message = $"GetAllIndexProducts: Count API invalid for index '{index}': {countResp.DebugInformation}",
                                Timestamp = DateTime.UtcNow,
                                Level = "Warning",
                                Source = "ProductServices"
                            });
                            total = ExtractTotalFromResponse(allProducts);
                            await _logger.LogAsync(new LoggerRequestModel
                            {
                                Message = $"GetAllIndexProducts: Fallback total extracted: {total}; Search DebugInfo: {allProducts.DebugInformation}",
                                Timestamp = DateTime.UtcNow,
                                Level = "Warning",
                                Source = "ProductServices"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogAsync(new LoggerRequestModel
                        {
                            Message = $"GetAllIndexProducts: Count API failed: {ex.Message}; falling back to response total. Search DebugInfo: {allProducts.DebugInformation}",
                            Timestamp = DateTime.UtcNow,
                            Level = "Warning",
                            Source = "ProductServices"
                        });
                        total = ExtractTotalFromResponse(allProducts);
                    }
                }
                else
                {
                    total = ExtractTotalFromResponse(allProducts);
                }

                return new PagedResponse<ProductResponse>
                {
                    Items = allProducts.Documents ?? Enumerable.Empty<ProductResponse>(),
                    Total = total,
                    PageNumber = pn,
                    PageSize = ps
                };
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in GetAllIndexProducts: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductServices"
                });
                throw;
            }
            finally
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = "GetAllIndexProducts execution ended.",
                    Timestamp = DateTime.UtcNow,
                    Level = "Information",
                    Source = "ProductServices"
                });
            }
        }

        public async Task<ProductResponse?> GetProduct(long productId, bool isFromElastic = true)
        {
            await _logger.LogAsync(new LoggerRequestModel
            {
                Message = $"GetProduct execution started. isFromElastic: {isFromElastic}",
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Source = "ProductServices"
            });
            try
            {
                if (isFromElastic)
                {
                    string index = _options.GetIndex(QuickSearchConstants.ProductIndex);
                    GetResponse<ProductResponse> product = await _client.GetAsync<ProductResponse>(productId, g => g.Index(index));

                    if (!product.IsValidResponse)
                    {
                        await _logger.LogAsync(new LoggerRequestModel
                        {
                            Message = "GetProduct: Elasticsearch returned invalid response.",
                            Timestamp = DateTime.UtcNow,
                            Level = "Warning",
                            Source = "ProductServices"
                        });
                        return null;
                    }

                    return product.Source;
                }
                else
                {
                    return await _productDbService.GetProductFromDbAsync((int)productId);
                }
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in GetProduct: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductServices"
                });
                throw;
            }
            finally
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = "GetProduct execution ended.",
                    Timestamp = DateTime.UtcNow,
                    Level = "Information",
                    Source = "ProductServices"
                });
            }
        }

        public async Task<PagedResponse<ProductResponse>> SearchProduct(ProductSearchRequest request)
        {
            await _logger.LogAsync(new LoggerRequestModel
            {
                Message = "SearchProduct execution started.",
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Source = "ProductServices"
            });
            try
            {
                if (request.IsFromElastic)
                {
                    return await SearchIndexProducts(request);
                }
                else
                {
                    return await SearchDatabaseProducts(request);
                }
            }
            catch(Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in SearchProduct: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductServices"
                });
                throw;
            }
            finally
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = "SearchProduct execution ended.",
                    Timestamp = DateTime.UtcNow,
                    Level = "Information",
                    Source = "ProductServices"
                });
            }
        }

        public async Task<PagedResponse<ProductResponse>> SearchIndexProducts(ProductSearchRequest request)
        {
            try
            {
                string index = _options.GetIndex(QuickSearchConstants.ProductIndex);

                var mustQueries = new List<Query>();

                if (!string.IsNullOrEmpty(request.Term))
                {
                    // If numeric -> ID search
                    if (long.TryParse(request.Term, out var idValue))
                    {
                        mustQueries.Add(new TermQuery
                        {
                            Field = "id",
                            Value = idValue
                        });
                    }
                    else
                    {
                        // Text search
                        mustQueries.Add(new MultiMatchQuery
                        {
                            Fields = new Field[] { "name", "brand", "category" },
                            Query = request.Term,
                            Type = TextQueryType.PhrasePrefix
                        });
                    }
                }

                if (!string.IsNullOrEmpty(request.Category))
                {
                    // Use the keyword subfield for exact term matching on analyzed text fields
                    mustQueries.Add(new TermQuery
                    {
                        Field = "category",
                        Value = request.Category
                    });
                }

                if (!string.IsNullOrEmpty(request.Brand))
                {
                    mustQueries.Add(new MatchQuery
                    {
                        Field = "brand",
                        Query = request.Brand
                    });
                }

                int pageNumber = Math.Max(1, request.PageNumber);
                int pageSize = Math.Max(1, request.PageSize);

                if (_options.MaxPageSize > 0)
                    pageSize = Math.Min(pageSize, _options.MaxPageSize);

                int from = (pageNumber - 1) * pageSize;

                var response = await _client.SearchAsync<ProductResponse>(s => s
                    .Indices(index)
                    .From(from)
                    .Size(pageSize)
                    .TrackTotalHits(true) // IMPORTANT for total count
                    .Query(q => q.Bool(b => b.Must(mustQueries)))
                    .Sort(srt =>
                    {
                        if (!string.IsNullOrEmpty(request.SortField))
                        {
                            var order = request.SortOrder?.ToLower() == "desc"
                                ? Elastic.Clients.Elasticsearch.SortOrder.Desc
                                : Elastic.Clients.Elasticsearch.SortOrder.Asc;

                            srt.Field(request.SortField, order);
                        }
                    })
                );

                return new PagedResponse<ProductResponse>
                {
                    Items = response.Documents ?? new List<ProductResponse>(),
                    Total = response.Total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in SearchIndexProducts: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductServices"
                });

                throw;
            }
        }

        public async Task<PagedResponse<ProductResponse>> SearchDatabaseProducts(ProductSearchRequest request)
        {
            await _logger.LogAsync(new LoggerRequestModel
            {
                Message = "SearchDatabaseProducts execution started.",
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Source = "ProductServices"
            });

            try
            {
                return await _productDbService.SearchDatabaseProductsAsync(request);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in SearchDatabaseProducts: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductServices"
                });
                throw;
            }
        }

        public async Task<bool> UpdateProduct(ProductResponse product)
        {
            await _logger.LogAsync(new LoggerRequestModel
            {
                Message = $"UpdateProduct execution started for Product Id: {product.Id}.",
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Source = "ProductServices"
            });

            try
            {
                bool success = await _productDbService.UpdateProductInDbAsync(product);

                if (success)
                {
                    // Update Elasticsearch
                    string index = _options.GetIndex(QuickSearchConstants.ProductIndex);
                    var esResponse = await _client.IndexAsync(product, idx => idx.Index(index).Id(product.Id));
                    if (!esResponse.IsValidResponse)
                    {
                        await _logger.LogAsync(new LoggerRequestModel
                        {
                            Message = $"Product {product.Id} updated in DB but Elasticsearch update failed: {esResponse.DebugInformation}",
                            Timestamp = DateTime.UtcNow,
                            Level = "Warning",
                            Source = "ProductServices"
                        });
                    }
                    else
                    {
                        await _logger.LogAsync(new LoggerRequestModel
                        {
                            Message = $"Product {product.Id} updated successfully in DB and Elasticsearch.",
                            Timestamp = DateTime.UtcNow,
                            Level = "Information",
                            Source = "ProductServices"
                        });
                    }
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in UpdateProduct: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductServices"
                });
                throw;
            }
        }

        public async Task<bool> DeleteProduct(long productId)
        {
            await _logger.LogAsync(new LoggerRequestModel
            {
                Message = $"DeleteProduct execution started for Product Id: {productId}.",
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Source = "ProductServices"
            });

            try
            {
                bool success = await _productDbService.DeleteProductFromDbAsync((int)productId);

                if (success)
                {
                    // Delete from Elasticsearch
                    string index = _options.GetIndex(QuickSearchConstants.ProductIndex);
                    var esResponse = await _client.DeleteAsync<ProductResponse>(productId, idx => idx.Index(index));
                    if (!esResponse.IsValidResponse)
                    {
                        await _logger.LogAsync(new LoggerRequestModel
                        {
                            Message = $"Product {productId} deleted from DB but Elasticsearch deletion failed: {esResponse.DebugInformation}",
                            Timestamp = DateTime.UtcNow,
                            Level = "Warning",
                            Source = "ProductServices"
                        });
                    }
                    else
                    {
                        await _logger.LogAsync(new LoggerRequestModel
                        {
                            Message = $"Product {productId} deleted successfully from DB and Elasticsearch.",
                            Timestamp = DateTime.UtcNow,
                            Level = "Information",
                            Source = "ProductServices"
                        });
                    }
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in DeleteProduct: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductServices"
                });
                throw;
            }
        }

        private long ExtractTotalFromResponse<T>(SearchResponse<T> resp) where T : class
        {
            try
            {
                // Prefer HitsMetadata.Total when available, otherwise use resp.Total
                object totalObj = resp.HitsMetadata?.Total ?? resp.Total;
                if (totalObj == null)
                    return 0;

                // If it's already a long
                if (totalObj is long longVal)
                    return longVal;

                // If it's a numeric type, try convert
                if (totalObj is int intVal)
                    return intVal;

                // Try reflection to get a 'Value' property (TotalHits type often exposes Value)
                var t = totalObj.GetType();
                var prop = t.GetProperty("Value");
                if (prop != null)
                {
                    var val = prop.GetValue(totalObj);
                    if (val is long lv) return lv;
                    if (val is int iv) return iv;
                    if (val != null && long.TryParse(val.ToString(), out var parsed)) return parsed;
                }

                // Fallback: parse number from ToString()
                var s = totalObj.ToString();
                if (!string.IsNullOrEmpty(s) && long.TryParse(s, out var v))
                    return v;
                var m = System.Text.RegularExpressions.Regex.Match(s ?? string.Empty, "\\d+");
                if (m.Success && long.TryParse(m.Value, out var v2))
                    return v2;

                return 0;
            }
            catch
            {
                return 0;
            }
        }
        #endregion
    }
}