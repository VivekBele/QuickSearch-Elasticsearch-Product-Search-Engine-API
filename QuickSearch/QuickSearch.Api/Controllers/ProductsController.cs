using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickSearch.LoggerUtility;
using QuickSearch.Model;
using QuickSearch.HelperUtilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuickSearch.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        #region Fields
        private readonly IProductServices _productServices;
        private readonly ILogger _logger;
        #endregion

        #region Constructors
        public ProductsController(IProductServices productServices, ILogger logger)
        {
            _productServices = productServices;
            _logger = logger;
        }
        #endregion

        #region Endpoints
        [HttpGet("all")]
        [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProducts(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] bool? isFromElastic = true)
        {
            try
            {
                string? term = HttpContext.Request.Query["term"];
                string? category = HttpContext.Request.Query["category"];
                string? brand = HttpContext.Request.Query["brand"];
                string? sortField = HttpContext.Request.Query["sortField"];
                string? sortOrder = HttpContext.Request.Query["sortOrder"];

                var request = new ProductSearchRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    IsFromElastic = isFromElastic ?? true,
                    Term = term,
                    Category = category,
                    Brand = brand,
                    SortField = sortField,
                    SortOrder = sortOrder
                };
                var paged = await _productServices.SearchProduct(request);
                if (paged == null || paged.Items == null || !paged.Items.Any())
                    return NoContent();

                return Ok(paged);
            }
            catch (Exception ex)
            {
                ApiErrorResponse errorResponse = new ApiErrorResponse
                {
                    Status = 500,
                    Error = "Internal Server Error",
                    Message = $"An unexpected error occurred in GetAllProducts endpoint: {ex.Message}"
                };
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"An unexpected error occurred in GetAllProducts endpoint: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductsController"
                });
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        [HttpGet("{productId}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProduct(long productId, bool? isFromElastic = true)
        {
            try
            {
                var product = await _productServices.GetProduct(productId, isFromElastic ?? true);
                if (product == null || product.Id <= 0)
                    return NoContent();

                return Ok(product);
            }
            catch (Exception ex)
            {
                ApiErrorResponse errorResponse = new ApiErrorResponse
                {
                    Status = 500,
                    Error = "Internal Server Error",
                    Message = $"An unexpected error occurred in GetProduct endpoint: {ex.Message}"
                };
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"An unexpected error occurred in GetProduct endpoint: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductsController"
                });
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        [HttpPost("search")]
        [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Search([FromBody] ProductSearchRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request?.Term))
                {
                    var badRequest = new ApiErrorResponse
                    {
                        Status = 400,
                        Error = "Bad Request",
                        Message = QuickSearchResource.BadRequest
                    };
                    return StatusCode(StatusCodes.Status400BadRequest, badRequest);
                }
                var paged = await _productServices.SearchProduct(request);
                if (paged == null || paged.Items == null || !paged.Items.Any())
                    return NoContent();

                return Ok(paged);
            }
            catch (Exception ex)
            {
                ApiErrorResponse errorResponse = new ApiErrorResponse
                {
                    Status = 500,
                    Error = "Internal Server Error",
                    Message = $"An unexpected error occurred in Search endpoint: {ex.Message}"
                };
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"An unexpected error occurred in Search endpoint: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductsController"
                });
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        [HttpPut("{productId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProduct(long productId, [FromBody] ProductResponse product)
        {
            try
            {
                if (product == null || product.Id != productId)
                {
                    var badRequest = new ApiErrorResponse
                    {
                        Status = 400,
                        Error = "Bad Request",
                        Message = "Product ID mismatch or invalid product data."
                    };
                    return BadRequest(badRequest);
                }

                var existing = await _productServices.GetProduct(productId, false);
                if (existing == null)
                {
                    var notFound = new ApiErrorResponse
                    {
                        Status = 404,
                        Error = "Not Found",
                        Message = $"Product with ID {productId} does not exist in the database."
                    };
                    return NotFound(notFound);
                }

                var success = await _productServices.UpdateProduct(product);
                if (!success)
                {
                    var error = new ApiErrorResponse
                    {
                        Status = 500,
                        Error = "Internal Server Error",
                        Message = "Failed to update product in database."
                    };
                    return StatusCode(StatusCodes.Status500InternalServerError, error);
                }

                return Ok(new { Message = "Product updated successfully." });
            }
            catch (Exception ex)
            {
                ApiErrorResponse errorResponse = new ApiErrorResponse
                {
                    Status = 500,
                    Error = "Internal Server Error",
                    Message = $"An unexpected error occurred in UpdateProduct endpoint: {ex.Message}"
                };
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"An unexpected error occurred in UpdateProduct endpoint: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductsController"
                });
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        [HttpDelete("{productId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProduct(long productId)
        {
            try
            {
                var existing = await _productServices.GetProduct(productId, false);
                if (existing == null)
                {
                    var notFound = new ApiErrorResponse
                    {
                        Status = 404,
                        Error = "Not Found",
                        Message = $"Product with ID {productId} does not exist in the database."
                    };
                    return NotFound(notFound);
                }

                var success = await _productServices.DeleteProduct(productId);
                if (!success)
                {
                    var error = new ApiErrorResponse
                    {
                        Status = 500,
                        Error = "Internal Server Error",
                        Message = "Failed to delete product from database."
                    };
                    return StatusCode(StatusCodes.Status500InternalServerError, error);
                }

                return Ok(new { Message = "Product deleted successfully." });
            }
            catch (Exception ex)
            {
                ApiErrorResponse errorResponse = new ApiErrorResponse
                {
                    Status = 500,
                    Error = "Internal Server Error",
                    Message = $"An unexpected error occurred in DeleteProduct endpoint: {ex.Message}"
                };
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"An unexpected error occurred in DeleteProduct endpoint: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "ProductsController"
                });
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }
        #endregion
    }
}
