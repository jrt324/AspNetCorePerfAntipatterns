using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ChattyDataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        const string GetProductsQuery = @"select product.ProductID from SalesLT.Product as product 
                               join SalesLT.ProductCategory as category 
	                             on product.ProductCategoryID = category.ProductCategoryID
                               where category.Name = 'Mountain Bikes'";

        const string GetThumbnailSlow = @"select ThumbNailPhoto from SalesLT.Product where ProductID = {0}";
        const string GetThumbnailFast = @"select ThumbNailPhoto from SalesLT.Product where ProductID in ({0})";

        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // GET api/data/slow
        [HttpGet("slow")]
        public async Task<ActionResult<IEnumerable<string>>> GetSlow()
        {
            var hashes = new List<string>();

            using (var md5 = MD5.Create())
            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                var productIDs = new List<int>();
                await connection.OpenAsync();

                using (var command = new SqlCommand(GetProductsQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                foreach (var id in productIDs)
                {
                    var commandText = string.Format(GetThumbnailSlow, id);
                
                    using (var command = new SqlCommand(commandText, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var buffer = new byte[100 * 1000];
                            var bytesRead = reader.GetBytes(0, 0, buffer, 0, buffer.Length);
                            hashes.Add(Convert.ToBase64String(md5.ComputeHash(buffer, 0, (int)bytesRead)));
                        }
                    }
                }
            }

            return Ok(hashes);
        }

        // GET api/data/fast
        [HttpGet("fast")]
        public async Task<ActionResult<IEnumerable<string>>> GetFast()
        {
            var hashes = new List<string>();

            using (var md5 = MD5.Create())
            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                var productIDs = new List<int>();
                await connection.OpenAsync();

                using (var command = new SqlCommand(GetProductsQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                var commandText = string.Format(GetThumbnailFast, string.Join(',', productIDs));
                using (var command = new SqlCommand(commandText, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var buffer = new byte[100 * 1000];
                        var bytesRead = reader.GetBytes(0, 0, buffer, 0, buffer.Length);
                        hashes.Add(Convert.ToBase64String(md5.ComputeHash(buffer, 0, (int)bytesRead)));
                    }
                }
            }

            return Ok(hashes);
        }
    }
}
