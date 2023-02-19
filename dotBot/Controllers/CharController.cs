using dotBot.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dotBot.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VkNet;

namespace dotBot.Controllers
{
    public class CharController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationContext db;
        public CharController(IConfiguration configuration, ApplicationContext context)
        {
            _configuration = configuration;
            db = context;
        }
        private ApplicationContext getNewDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));
            var db = new ApplicationContext(optionsBuilder.Options);
            return db;
        }
        // GET: CharController
        public ActionResult Index(int? Id)
        {
			ViewBag.HaveData = false;
			if (db.Users.Find(Id) != null)
            {
				ViewBag.HaveData = true;
				ViewBag.userName = db.Users.Find(Id).Nickname;

				ViewBag.userLvl = db.GameStats.Find(Id).lvl;
				ViewBag.userExp = db.GameStats.Find(Id).exp;
				ViewBag.userExpToUp = db.GameStats.Find(Id).expToUp;
				ViewBag.userHp = db.GameStats.Find(Id).hp;
				ViewBag.userHpMax = db.GameStats.Find(Id).maxHp;
				ViewBag.userPower = db.GameStats.Find(Id).power;
				ViewBag.userDefence = db.GameStats.Find(Id).defence;
				ViewBag.userLvlPoints = db.GameStats.Find(Id).lvlPoints;
				ViewBag.userMoney = db.GameStats.Find(Id).money;
			}
            
			return View();
        }

        // GET: CharController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CharController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CharController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: CharController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: CharController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: CharController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: CharController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }


}
