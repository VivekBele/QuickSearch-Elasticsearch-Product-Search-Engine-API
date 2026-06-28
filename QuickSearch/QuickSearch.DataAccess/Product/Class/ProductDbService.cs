using Microsoft.EntityFrameworkCore;
using QuickSearch.Data.Entities;
using QuickSearch.Data.Repositories;
using QuickSearch.Model;

namespace QuickSearch.DataAccess
{
    public class ProductDbService : IProductDbService
    {
        private readonly IRepository<Product> _productRepository;

        public ProductDbService(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<PagedResponse<ProductResponse>> SearchDatabaseProductsAsync(ProductSearchRequest request)
        {
            var query = _productRepository.Query();

            // 1. Text Search Filter (on Term)
            if (!string.IsNullOrWhiteSpace(request.Term))
            {
                string termLower = request.Term.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(termLower) || 
                                         (p.Brand != null && p.Brand.ToLower().Contains(termLower)) ||
                                         (p.Category != null && p.Category.ToLower().Contains(termLower)));
            }

            // 2. Category Filter
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                query = query.Where(p => p.Category == request.Category);
            }

            // 3. Brand Filter
            if (!string.IsNullOrWhiteSpace(request.Brand))
            {
                query = query.Where(p => p.Brand == request.Brand);
            }

            // 4. Count total items before pagination
            int totalItems = await query.CountAsync();

            // 5. Apply Sorting
            if (!string.IsNullOrWhiteSpace(request.SortField))
            {
                bool isAscending = string.IsNullOrWhiteSpace(request.SortOrder) || 
                                   request.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

                switch (request.SortField.ToLower())
                {
                    case "price":
                        query = isAscending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price);
                        break;
                    case "rating":
                        query = isAscending ? query.OrderBy(p => p.Rating) : query.OrderByDescending(p => p.Rating);
                        break;
                    case "name":
                        query = isAscending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name);
                        break;
                    default:
                        // Default sorting by Id or CreatedAt
                        query = query.OrderBy(p => p.Id);
                        break;
                }
            }
            else
            {
                // Default sorting
                query = query.OrderBy(p => p.Id);
            }

            // 6. Pagination (Skip & Take)
            int skip = (request.PageNumber - 1) * request.PageSize;
            var items = await query.Skip(skip)
                                   .Take(request.PageSize)
                                   .Select(p => new ProductResponse
                                   {
                                       Id = p.Id,
                                       Name = p.Name,
                                       Price = (double)p.Price,
                                       Brand = p.Brand ?? string.Empty,
                                       Category = p.Category ?? string.Empty,
                                       Rating = p.Rating.HasValue ? (double)p.Rating.Value : 0.0,
                                       Created_at = p.CreatedAt ?? DateTime.MinValue,
                                       ImageURL = string.Empty // Database fallback doc has no Image URL
                                   })
                                   .ToListAsync();

            return new PagedResponse<ProductResponse>
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Total = totalItems,
                Items = items
            };
        }

        public async Task<ProductResponse?> GetProductFromDbAsync(int productId)
        {
            var p = await _productRepository.GetByIdAsync(productId);
            if (p == null) return null;

            return new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Price = (double)p.Price,
                Brand = p.Brand ?? string.Empty,
                Category = p.Category ?? string.Empty,
                Rating = p.Rating.HasValue ? (double)p.Rating.Value : 0.0,
                Created_at = p.CreatedAt ?? DateTime.MinValue,
                ImageURL = string.Empty
            };
        }

        public async Task<bool> UpdateProductInDbAsync(ProductResponse productDto)
        {
            var p = await _productRepository.GetByIdAsync(productDto.Id);
            if (p == null) return false;

            p.Name = productDto.Name;
            p.Price = (decimal)productDto.Price;
            p.Brand = productDto.Brand;
            p.Category = productDto.Category;
            p.Rating = (decimal)productDto.Rating;
            
            _productRepository.Update(p);
            await _productRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductFromDbAsync(int productId)
        {
            var p = await _productRepository.GetByIdAsync(productId);
            if (p == null) return false;

            _productRepository.Delete(p);
            await _productRepository.SaveChangesAsync();
            return true;
        }
    }
}
