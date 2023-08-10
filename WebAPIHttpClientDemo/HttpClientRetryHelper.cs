using Polly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebAPIHttpClientDemo
{
    public class HttpClientRetryHelper
    {
        public static IAsyncPolicy<HttpResponseMessage> CreateRetryFallbackPolicy(
            int maxRetryCount,
            TimeSpan retryDelay,
            HttpStatusCode[] retryableStatusCodes,
            HttpStatusCode[] fallbackStatusCodes)
        {
            var retryPolicy = Policy.Handle<Exception>()
                .OrResult<HttpResponseMessage>(r => retryableStatusCodes.Contains(r.StatusCode))
                .WaitAndRetryAsync(maxRetryCount, retryAttempt => retryDelay);

            var fallbackPolicy = Policy.Handle<Exception>()
                .OrResult<HttpResponseMessage>(r => fallbackStatusCodes.Contains(r.StatusCode))
                .FallbackAsync(
                    fallbackAction: async (result, context) =>
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent("Fallback response content")
                        };
                    },
                    onFallbackAsync: (exception, context) =>
                    {
                        // Access the original status code from the context
                        HttpStatusCode originalStatusCode = context.ContainsKey("originalStatusCode")
                            ? (HttpStatusCode)context["originalStatusCode"]
                            : HttpStatusCode.InternalServerError;

                        // Log the exception or perform any necessary actions
                        Console.WriteLine("Fallback exception: " + exception.ToString());
                        return Task.CompletedTask;
                    }
                );

            var retryFallbackPolicy = fallbackPolicy.WrapAsync(retryPolicy);

            return retryFallbackPolicy;
        }
    }

    //public class HttpClientRetryHelper
    //{
    //    public static IAsyncPolicy<HttpResponseMessage> CreateRetryFallbackPolicy(
    //        int maxRetryCount,
    //        TimeSpan retryDelay,
    //        HttpStatusCode[] retryableStatusCodes)
    //    {
    //        var retryPolicy = Policy.Handle<Exception>()
    //            .OrResult<HttpResponseMessage>(r => retryableStatusCodes.Contains(r.StatusCode))
    //            .WaitAndRetryAsync(maxRetryCount, retryAttempt => retryDelay);

    //        var fallbackPolicy = Policy.Handle<Exception>()
    //            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    //            .FallbackAsync(
    //                fallbackAction: async (result, context) =>
    //                {
    //                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
    //                    {
    //                        Content = new StringContent("Fallback response content")
    //                    };
    //                },
    //                onFallbackAsync: (exception, context) =>
    //                {
    //                    // Log the exception or perform any necessary actions
    //                    Console.WriteLine("Fallback exception: " + exception.ToString());
    //                    return Task.CompletedTask;
    //                }
    //            );

    //        var retryFallbackPolicy = fallbackPolicy.WrapAsync(retryPolicy);

    //        return retryFallbackPolicy;
    //    }
    //}
}