using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Nop.Core.Caching;
using Nop.Services.Media.RoxyFileman;
using System.Security.Cryptography;

namespace Nop.Web.Controllers;

/// <summary>
/// Controller for serving public static files with caching strategy
/// </summary>

public partial class PublicStaticFilesController : BasePublicController
{
    #region Fields

    protected readonly IRoxyFilemanService _roxyFilemanService;
    protected readonly IStaticCacheManager _staticCacheManager;

    #endregion

    #region Ctor

    public PublicStaticFilesController(IRoxyFilemanService roxyFilemanService, IStaticCacheManager staticCacheManager)
    {
        _roxyFilemanService = roxyFilemanService;
        _staticCacheManager = staticCacheManager;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Generate ETag for content
    /// </summary>
    /// <param name="content">Content to generate ETag for</param>
    /// <returns>ETag string</returns>
    protected virtual string GenerateETag(byte[] content)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(content);
        return $"\"{Convert.ToBase64String(hash)}\"";
    }

    /// <summary>
    /// Check if client cache is valid
    /// </summary>
    /// <param name="etag">ETag to check</param>
    /// <param name="lastModified">Last modified date to check</param>
    /// <returns>True if client cache is valid</returns>
    protected virtual bool IsClientCacheValid(string etag, DateTimeOffset lastModified)
    {
        // Check ETag if enabled
        if (!string.IsNullOrEmpty(etag))
        {
            var ifNoneMatch = Request.Headers.IfNoneMatch.FirstOrDefault();
            if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == etag)
                return true;
        }

        // Check Last-Modified if enabled
        if (Request.Headers.TryGetValue("If-Modified-Since", out var ifModifiedSince))
        {
            if (DateTimeOffset.TryParse(ifModifiedSince, out var clientLastModified))
            {
                return lastModified <= clientLastModified;
            }
        }

        return false;
    }

    /// <summary>
    /// Set HTTP cache headers
    /// </summary>
    /// <param name="etag">ETag value</param>
    /// <param name="lastModified">Last modified date</param>
    protected virtual void SetCacheHeaders(string etag, DateTimeOffset lastModified)
    {
        var browserCacheSeconds = 14400; // 4 hours default

        // Set cache-control headers based on settings
        Response.Headers.CacheControl = $"public, max-age={browserCacheSeconds}";

        // Set ETag if enabled
        if (!string.IsNullOrEmpty(etag))
        {
            Response.Headers.ETag = etag;
        }

        // Set Last-Modified if enabled
        Response.Headers.LastModified = lastModified.ToString("R");

        // Set expiration date
        Response.Headers.Expires = DateTimeOffset.UtcNow.AddHours(4).ToString("R");

        // Add vary header for proper caching
        Response.Headers.Vary = "Accept-Encoding";

        // Add compression headers if enabled
        Response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    #endregion

    #region Methods

    /// <summary>
    /// Serve image from database by virtual path with caching strategy
    /// </summary>
    /// <param name="path">Virtual path to the image</param>
    /// <returns>Image file</returns>
    [HttpGet]
    [Route("images/uploaded/{*path}")]
    public virtual async Task<IActionResult> ServeImage(string path)
    {
        try
        {
            // Create cache key for this image
            var cacheKeyString = $"roxy_image_{path.Replace("/", "_").Replace("\\", "_")}";
            var cacheKey = new CacheKey(cacheKeyString);

            // Try to get cached image data - use non-nullable type to match what we store
            var cachedData = await _staticCacheManager.GetAsync<(byte[] content, string contentType, DateTimeOffset lastModified)>(cacheKey, 
                defaultValue: default((byte[] content, string contentType, DateTimeOffset lastModified)));

            // Check if we have valid cached data (content array is not null/empty)
            if (cachedData.content != null && cachedData.content.Length > 0)
            {
                var (content, contentType, cachedLastModified) = cachedData;

                // Generate ETag for cached content
                var etag = GenerateETag(content);

                // Check if client cache is valid
                if (IsClientCacheValid(etag, cachedLastModified))
                {
                    return new StatusCodeResult(304); // Not Modified
                }

                // Set cache headers
                SetCacheHeaders(etag, cachedLastModified);

                return File(content, contentType);
            }

            // Get image from service
            var (stream, name, lastModified) = _roxyFilemanService.GetFileStreamWithInfo(path);

            if (stream == null || stream == Stream.Null)
                return NotFound();

            if (!new FileExtensionContentTypeProvider().TryGetContentType(path, out var imageContentType))
                imageContentType = "application/octet-stream";

            // Read stream content to byte array for caching
            byte[] imageContent;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageContent = memoryStream.ToArray();
            }

            // Cache the image data based on settings
            var cacheTimeMinutes = 4 * 60; // 4 hours in minutes
            var cacheKeyWithTime = new CacheKey(cacheKeyString) { CacheTime = cacheTimeMinutes };
            
            // Store the tuple directly (non-nullable)
            await _staticCacheManager.SetAsync(cacheKeyWithTime, (imageContent, imageContentType, lastModified));

            // Generate ETag
            var imageEtag = GenerateETag(imageContent);

            // Check if client cache is valid
            if (IsClientCacheValid(imageEtag, lastModified))
            {
                return new StatusCodeResult(304); // Not Modified
            }

            // Set cache headers
            SetCacheHeaders(imageEtag, lastModified);

            return File(imageContent, imageContentType);
        }
        catch
        {
            return NotFound();
        }
    }

    #endregion
}