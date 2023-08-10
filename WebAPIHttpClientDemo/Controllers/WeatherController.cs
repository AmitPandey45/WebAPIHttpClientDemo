using Microsoft.Web.Http;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebAPIHttpClientDemo.Controllers
{
    [ApiVersion("1.0")]
    [RoutePrefix("api/v{version:apiVersion}/weather")]
    public class WeatherController : ApiController
    {
        [HttpGet]
        [Route("Details/{city}")]
        public IHttpActionResult WeatherDetails(string city)
        {
            var httpClient = new HttpClient();

            // Configure retry settings
            int maxRetryCount = 3;
            TimeSpan retryDelay = TimeSpan.FromSeconds(5);
            HttpStatusCode[] retryableStatusCodes = { HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable, HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest };
            HttpStatusCode[] fallbackStatusCodes = { HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable, HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest };

            // Create a retry and fallback policy with Polly
            var retryFallbackPolicy = HttpClientRetryHelper.CreateRetryFallbackPolicy(maxRetryCount, retryDelay, retryableStatusCodes, fallbackStatusCodes);

            // Use the combined policy with the HttpClient
            HttpResponseMessage response = retryFallbackPolicy.ExecuteAsync(() =>
            {
                return httpClient.GetAsync($"http://api.weatherapi.com/v1/current.json?key=00140c27b738416a80393748230608&q={city}&aqi=yes");
            }).Result;

            // Process the response
            if (response.IsSuccessStatusCode)
            {
                string content = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Response content: " + content);
                return Ok(content);
            }
            else
            {
                Console.WriteLine("Request failed: " + response.StatusCode);
                return InternalServerError(new Exception("An error occurred."));
            }
        }

        [HttpGet]
        [Route("{city}")]
        public IHttpActionResult GetWeatherDetails(string city)
        {
            var httpClient = new HttpClient();
            // Create a fallback policy to handle exceptions and non-successful HTTP status codes
            var fallbackPolicy = Policy.Handle<Exception>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .FallbackAsync(
                    fallbackAction: async (result, context) =>
                    {
            // Handle fallback logic here
            Console.WriteLine("Fallback action executed");

            // You can return a response indicating the error or just a default response
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent("Fallback response content")
                        };
                    },
                    onFallbackAsync: (exception, context) =>
                    {
            // Log the exception or perform any necessary actions
            Console.WriteLine("Fallback exception: " + exception.ToString());
                        return Task.CompletedTask;
                    }
                );

            // Create a retry policy with exponential backoff
            var retryPolicy = Policy.Handle<Exception>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            // Combine the fallback and retry policies
            var combinedPolicy = fallbackPolicy.WrapAsync(retryPolicy);

            // Use the combined policy with the HttpClient
            HttpResponseMessage response = combinedPolicy.ExecuteAsync(() =>
            {
                return httpClient.GetAsync($"http://api.weatherapi.com/v1/current.json?key=00140c27b738416a80393748230608&q={city}&aqi=yes");
            }).Result;

            // Process the response
            if (response.IsSuccessStatusCode)
            {
                string content = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Response content: " + content);
                return Ok(content);
            }
            else
            {
                Console.WriteLine("Request failed: " + response.StatusCode);
                return InternalServerError(new Exception("An error occurred."));
            }
        }
    }
}
