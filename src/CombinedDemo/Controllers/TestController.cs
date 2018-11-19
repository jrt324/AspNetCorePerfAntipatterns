using System;
using System.Buffers;
using System.Collections.Concurrent;
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

            // Product IDs
            var productIDs = new List<int>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                connection.Open();

                // Store all returned product IDs
                using (var command = new SqlCommand(GetMountainBikesQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                // Close the connection for now since determining which products 
                // are in stock can take a moment and we don't want to exhaust SQL connections
                connection.Close();
            }

            var inStockProductIds = new List<int>();
            foreach (var id in productIDs)
            {
                // Filter for in-stock products
                if (ProductIsInStock(id).Result)
                {
                    inStockProductIds.Add(id);
                }
            }

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                connection.Open();

                // Iterate through in-stock products, retrieve images and store hashes
                foreach (var id in inStockProductIds)
                {
                    var commandText = $"select ThumbNailPhoto from SalesLT.Product where ProductID = {id}";

                    using (var command = new SqlCommand(commandText, connection))
                    using (var reader = command.ExecuteReader())
                    using (var md5 = MD5.Create())
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

                connection.Close();
            }

            return Ok(hashes);
        }

        // Use ArrayPool<byte> instead of allocating large byte[]s
        [HttpGet("Test2")]
        public ActionResult<IEnumerable<string>> GetInStockThumbnailHashes2()
        {
            // List of hashes to return
            var hashes = new List<string>();

            // Product IDs
            var productIDs = new List<int>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                connection.Open();

                // Store all returned product IDs
                using (var command = new SqlCommand(GetMountainBikesQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                // Close the connection for now since determining which products 
                // are in stock can take a moment and we don't want to exhaust SQL connections
                connection.Close();
            }

            var inStockProductIds = new List<int>();
            foreach (var id in productIDs)
            {
                // Filter for in-stock products
                if (ProductIsInStock(id).Result)
                {
                    inStockProductIds.Add(id);
                }
            }

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                connection.Open();

                // Iterate through in-stock products, retrieve images and store hashes
                foreach (var id in inStockProductIds)
                {
                    var commandText = $"select ThumbNailPhoto from SalesLT.Product where ProductID = {id}";

                    using (var command = new SqlCommand(commandText, connection))
                    using (var reader = command.ExecuteReader())
                    using (var md5 = MD5.Create())
                    {
                        while (reader.Read())
                        {
                            // Some product images could be large
                            var thumbnail = ArrayPool<byte>.Shared.Rent(100 * 1000);
                            try
                            {
                                var bytesRead = reader.GetBytes(0, 0, thumbnail, 0, thumbnail.Length);
                                hashes.Add(Convert.ToBase64String(md5.ComputeHash(thumbnail, 0, (int)bytesRead)));
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(thumbnail);
                            }
                        }
                    }
                }

                connection.Close();
            }

            return Ok(hashes);
        }

        // Make async
        [HttpGet("Test3")]
        public async Task<ActionResult<IEnumerable<string>>> GetInStockThumbnailHashes3()
        {
            // List of hashes to return
            var hashes = new List<string>();

            // Product IDs
            var productIDs = new List<int>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                await connection.OpenAsync();

                // Store all returned product IDs
                using (var command = new SqlCommand(GetMountainBikesQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                // Close the connection for now since determining which products 
                // are in stock can take a moment and we don't want to exhaust SQL connections
                connection.Close();
            }

            var inStockProductIds = new List<int>();
            foreach (var id in productIDs)
            {
                // Filter for in-stock products
                if (await ProductIsInStock(id))
                {
                    inStockProductIds.Add(id);
                }
            }

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                await connection.OpenAsync();

                // Iterate through in-stock products, retrieve images and store hashes
                foreach (var id in inStockProductIds)
                {
                    var commandText = $"select ThumbNailPhoto from SalesLT.Product where ProductID = {id}";

                    using (var command = new SqlCommand(commandText, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    using (var md5 = MD5.Create())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Some product images could be large
                            var thumbnail = ArrayPool<byte>.Shared.Rent(100 * 1000);
                            try
                            {
                                var bytesRead = reader.GetBytes(0, 0, thumbnail, 0, thumbnail.Length);
                                hashes.Add(Convert.ToBase64String(md5.ComputeHash(thumbnail, 0, (int)bytesRead)));
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(thumbnail);
                            }
                        }
                    }
                }

                connection.Close();
            }

            return Ok(hashes);
        }

        // Consolidate SQL chatter
        [HttpGet("Test4")]
        public async Task<ActionResult<IEnumerable<string>>> GetInStockThumbnailHashes4()
        {
            // List of hashes to return
            var hashes = new List<string>();

            // Product IDs
            var productIDs = new List<int>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                await connection.OpenAsync();

                // Store all returned product IDs
                using (var command = new SqlCommand(GetMountainBikesQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                // Close the connection for now since determining which products 
                // are in stock can take a moment and we don't want to exhaust SQL connections
                connection.Close();
            }

            var inStockProductIds = new List<int>();
            foreach (var id in productIDs)
            {
                if (await ProductIsInStock(id))
                {
                    inStockProductIds.Add(id);
                }
            }

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                await connection.OpenAsync();

                var commandText = $"select ThumbNailPhoto from SalesLT.Product where ProductID in ({string.Join(',', inStockProductIds)})";

                // Retrieve thumbnails (and compute hashes) for in-stock products
                using (var command = new SqlCommand(commandText, connection))
                using (var reader = await command.ExecuteReaderAsync())
                using (var md5 = MD5.Create())
                {
                    while (await reader.ReadAsync())
                    {
                        // Some product images could be large
                        var thumbnail = ArrayPool<byte>.Shared.Rent(100 * 1000);
                        try
                        {
                            var bytesRead = reader.GetBytes(0, 0, thumbnail, 0, thumbnail.Length);
                            hashes.Add(Convert.ToBase64String(md5.ComputeHash(thumbnail, 0, (int)bytesRead)));
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(thumbnail);
                        }
                    }
                }

                connection.Close();
            }

            return Ok(hashes);
        }

        private async Task<bool> ProductIsInStock(int productId)
        {
            // This method mimcs a call to another service to check stock
            await Task.Delay(20);

            // For test purposes, just say product IDs in the 700s or 900s are in stock and others aren't
            var productSeries = productId / 100;
            return productSeries == 7 || productSeries == 9;
        }
    }
}
