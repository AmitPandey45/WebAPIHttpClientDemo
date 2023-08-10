using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebAPIHttpClientDemo.Controllers
{
    public class HomeController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            return Ok("Hello World!!");
        }
    }
}
