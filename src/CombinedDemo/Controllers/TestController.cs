using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CombinedDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        const string GetMountainBikesQuery = @"select product.ProductID from SalesLT.Product where ProductCategoryID = 5";

        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpGet("Test1")]
        public ActionResult<IEnumerable<string>> GetInStockThumbnailHashes1()
        {
            // List of hashes to return
            var hashes = new List<string>();

            using (var md5 = MD5.Create())
            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                var productIDs = new List<int>();

                // Open the SQL connection
                connection.Open();

                // Iterate through all product IDs and find those that are in stock
                using (var command = new SqlCommand(GetMountainBikesQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        if (ProductIsInStock(id).Result)
                        {
                            productIDs.Add(id);
                        }
                    }
                }

                // Iterate through in-stock products, retrieve images and store hashes
                foreach (var id in productIDs)
                {
                    var commandText = $"select ThumbNailPhoto from SalesLT.Product where ProductID = {id}";

                    using (var command = new SqlCommand(commandText, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Some product images could be large
                            var thumbnail = new byte[100 * 1000];
                            var bytesRead = reader.GetBytes(0, 0, thumbnail, 0, thumbnail.Length);
                            hashes.Add(Convert.ToBase64String(md5.ComputeHash(thumbnail, 0, (int)bytesRead)));
                        }
                    }
                }
            }

            return Ok(hashes);
        }

        [HttpGet("Test2")]
        public ActionResult<IEnumerable<string>> GetInStockThumbnailHashes2()
        {
            throw new NotImplementedException();
        }

        [HttpGet("Test3")]
        public ActionResult<IEnumerable<string>> GetInStockThumbnailHashes3()
        {
            throw new NotImplementedException();
        }

        [HttpGet("Test4")]
        public ActionResult<IEnumerable<string>> GetInStockThumbnailHashes4()
        {
            throw new NotImplementedException();
        }

        private async Task<bool> ProductIsInStock(int productId)
        {
            // This method mimcs a call to another service to check stock
            await Task.Delay(50);

            // For test purposes, just say product IDs in the 700s or 900s are in stock and others aren't
            var productSeries = productId / 100;
            return productSeries == 7 || productSeries == 9;
        }
    }
}
