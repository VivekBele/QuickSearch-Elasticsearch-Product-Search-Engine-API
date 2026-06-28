using QuickSearch.Model;

namespace QuickSearch.DataAccess
{
    public interface IProductDbService
    {
        Task<PagedResponse<ProductResponse>> SearchDatabaseProductsAsync(ProductSearchRequest request);
        Task<ProductResponse?> GetProductFromDbAsync(int productId);
        Task<bool> UpdateProductInDbAsync(ProductResponse productDto);
        Task<bool> DeleteProductFromDbAsync(int productId);
    }
}
