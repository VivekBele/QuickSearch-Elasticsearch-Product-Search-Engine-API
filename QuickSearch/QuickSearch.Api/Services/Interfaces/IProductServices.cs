using Elastic.Clients.Elasticsearch;
using QuickSearch.Model;

namespace QuickSearch.Api
{
    public interface IProductServices
    {
        Task<PagedResponse<ProductResponse>> GetAllProducts(int pageNumber, int pageSize, bool isFromElastic);
        Task<PagedResponse<ProductResponse>> GetAllIndexProducts(int pageNumber, int pageSize);
        Task<ProductResponse?> GetProduct(long productId);
        Task<PagedResponse<ProductResponse>> SearchProduct(ProductSearchRequest request);
        Task<PagedResponse<ProductResponse>> SearchIndexProducts(ProductSearchRequest request);
        Task<PagedResponse<ProductResponse>> SearchDatabaseProducts(ProductSearchRequest request);
    }
}