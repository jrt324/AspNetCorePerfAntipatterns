using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LOHAllocations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        // ~93KB image
        const string ImageSource = "https://blogs.microsoft.com/uploads/2012/08/8867.Microsoft_5F00_Logo_2D00_for_2D00_screen.jpg";
        private readonly IHttpClientFactory _httpClientFactory;

        public ImageController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        // GET api/image/slow
        [HttpGet("slow")]
        public async Task<ActionResult<string>> GetImageSlowAsync()
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                var response = await client.GetAsync(ImageSource);

                // !!! BUG !!!
                // Allocating large byte[] and string objects on a hot code path
                // will lead to frequent gen 2 GCs and poor performance. These objects
                // should be pooled or cached.
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                return Ok($"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}");
            }
        }

        // GET api/image/slow
        [HttpGet("fast")]
        public async Task<ActionResult<string>> GetImageFastAsync()
        {
            // Ideally the large object would be cached to avoid both the GC pressure 
            // and the http call. Assuming that isn't an option, though, ArrayPools
            // can reduce GC pressure.
            using (var client = _httpClientFactory.CreateClient())
            {
                using (var response = await client.GetAsync(ImageSource))
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    var imageBytes = ArrayPool<byte>.Shared.Rent((int)responseStream.Length);

                    try
                    {
                        using (var ms = new MemoryStream(imageBytes))
                        {
                            await responseStream.CopyToAsync(ms);
                        }

                        // This will still allocate a large string. Without changing the API to
                        // return bytes directly or implementing our own stream-based base64 encoding
                        // (both of which might be worth considering in a real, production app),
                        // this is difficult to avoid.
                        // This sample will still demonstrate the difference between allocating both 
                        // the byte[] and the string and just the string[], though.
                        return Ok($"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes, 0, (int)responseStream.Length)}");
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(imageBytes);
                    }
                }
            }
        }
    }
}
