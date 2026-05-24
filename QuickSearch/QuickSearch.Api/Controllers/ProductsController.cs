using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickSearch.LoggerUtility;
using QuickSearch.Model;
using QuickSearch.HelperUtilities;

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
        public async Task<IActionResult> GetAllProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, bool? isFromElastic = true)
        {
            try
            {
                var paged = await _productServices.GetAllProducts(pageNumber, pageSize, isFromElastic ?? true);
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
                var product = await _productServices.GetProduct(productId);
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

        #endregion
    }

}

