using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using personsapi.Attributes;
using personsapi.Interfaces;

// namespace personsapi.Filters
// {
//     public class CacheActionFilter : IAsyncActionFilter
//     {
//         private readonly ICacheService _cacheService;
//         private readonly ILogger<CacheActionFilter> _logger;

//         public CacheActionFilter(ICacheService cacheService, ILogger<CacheActionFilter> logger)
//         {
//             _cacheService = cacheService;
//             _logger = logger;
//         }

//         public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//         {
//             var cacheAttribute = context.ActionDescriptor.EndpointMetadata
//                 .OfType<CacheResultAttribute>()
//                 .FirstOrDefault();

//             var invalidateCacheAttribute = context.ActionDescriptor.EndpointMetadata
//                 .OfType<InvalidateCacheAttribute>()
//                 .FirstOrDefault();

//             if (invalidateCacheAttribute != null)
//             {
//                 var result = await next();

//                 if (context.HttpContext.Response.StatusCode >= 200 && context.HttpContext.Response.StatusCode < 300)
//                 {
//                     await _cacheService.InvalidateAsync(invalidateCacheAttribute.CacheKeys);
//                     _logger.LogInformation("Cache invalidated for keys: {Keys}", string.Join(", ", invalidateCacheAttribute.CacheKeys));
//                 }
//                 return;
//             }

//             if (cacheAttribute == null)
//             {
//                 await next();
//                 return;
//             }

//             var cacheKey = BuildCacheKey(cacheAttribute.CacheKey, context.ActionArguments);
//             var expiration = cacheAttribute.Duration > 0 ? TimeSpan.FromSeconds(cacheAttribute.Duration) : null;

//             var response = await _cacheService.GetOrSetAsync<IActionResult>(
//                 cacheKey,
//                 async () =>
//                 {
//                     var executedContext = await next();
//                     return executedContext.Result;
//                 },
//                 expiration);

//             context.Result = response;
//         }

//         private string BuildCacheKey(string baseKey, IDictionary<string, object> parameters)
//         {
//             var paramValues = parameters.OrderBy(x => x.Key)
//                 .Select(x => $"{x.Key}:{x.Value}")
//                 .ToList();

//             return $"{baseKey}:{string.Join(":", paramValues)}";
//         }
//     }

// }