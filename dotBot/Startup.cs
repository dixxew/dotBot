using dotBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace dotBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
    }
}