using dotBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Diagnostics;


namespace dotBot.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
		private readonly IConfiguration _configuration;
		

		public HomeController(IConfiguration configuration, ILogger<HomeController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
		private SqlDataReader sendSqlCommand(string textOfCommand)
		{
			SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

			connection.Open();
			SqlCommand command = new SqlCommand();

			command.Connection = connection;
			command.CommandText = textOfCommand;

			SqlDataReader reader = command.ExecuteReader();
			return reader;
		}

	}
}