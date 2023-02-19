using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dotBot.Models;
using dotBot.Controllers;
using Microsoft.Extensions.Configuration;
using VkNet.Model;
using VkNet;
using VkNet.Enums;
using Newtonsoft.Json;
using dotBot.Models;
using User = dotBot.Models.User;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Message = dotBot.Models.Message;
using Azure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Globalization;
using System.Data;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class CallbackController : ControllerBase
{
	/// <summary>
	/// Конфигурация приложения
	/// </summary>
	private readonly IConfiguration _configuration;

	VkApi api = new VkApi();
	private readonly ApplicationContext db;
	List<String> answerCheckList_hi = new List<String> { ".hi", ".qq" };


	public CallbackController(IConfiguration configuration, ApplicationContext context)
	{
		_configuration = configuration;
		db = context;
	}


	//сюда обращается Vk Callback
	[HttpPost]
	public IActionResult Callback([FromBody] Updates updates)
	{

		// Проверяем, что находится в поле "type" 
		switch (updates.Type)
		{
			// Если это уведомление для подтверждения адреса
			case "confirmation":
				// Отправляем строку для подтверждения 
				return Ok(_configuration["Config:Confirmation"]);
			default:
				CheckMessage(db, updates.Object.message);
				break;
		}

		// Возвращаем "ok" серверу Callback API        
		return Ok("ok");
		if (!api.IsAuthorized)
		{
			api.Authorize(new ApiAuthParams
			{
				AccessToken = _configuration["Config:AccessToken"]
			});
		}
	}

	//GET запрос вк
	/*private void vkGET()
	{
		System.Net.WebRequest reqPOST = System.Net.WebRequest.Create(@"http://site.ru/send.php");
		reqPOST.Method = "POST"; // Устанавливаем метод передачи данных в POST
		reqPOST.Timeout = 120000; // Устанавливаем таймаут соединения
		reqPOST.ContentType = "application/x-www-form-urlencoded"; // указываем тип контента
																   // передаем список пар параметров / значений для запрашиваемого скрипта методом POST
																   // здесь используется кодировка cp1251 для кодирования кирилицы и спец. символов в значениях параметров
																   // Если скрипт должен принимать данные в utf-8, то нужно выбрать Encodinf.UTF8
		byte[] sentData = Encoding.GetEncoding(1251).GetBytes("message=" + System.Web.HttpUtility.UrlEncode("отправляемые данные", Encoding.GetEncoding(1251)));
		reqPOST.ContentLength = sentData.Length;
		System.IO.Stream sendStream = reqPOST.GetRequestStream();
		sendStream.Write(sentData, 0, sentData.Length);
		sendStream.Close();
		//System.Net.WebResponse result = reqPOST.GetResponse();
	}*/

	//основной метод работающий с запросом от сервера
	public void CheckMessage(ApplicationContext db, Message message)
	{
		/*
         * Объект message содержит основные поля:
         * .peer_id - идентификатор беседы в личке бота
         * .from_id - индентификатор пользователя отправившего сообщение
         * .text - текст сообщения
         * 
         * В случае наличия пересылаемого сообщения(иначе объект содержит NULL)
         * .reply - объект пересылаемого сообщения
         * .reply.from_id - индентификатор пользователя пересылаемого сообщение
         * .reply.text - текст переслыаемого сообщения
         * 
         */

		//проверяем есть ли юзер в бд
		if (db.Users.Find(message.from_id) == null)
		{
			//если нет создаем для него запись
			CreateCharacter(db, message.from_id);
		}

		//провеяем есть ли пересылаемое сообщение
		if (message.reply_message != null)
		{
			//проверяем есть ли в бд юзер пересылаемого сообщения
			if (db.Users.Find(message.reply_message.from_id) == null)
			{
				CreateCharacter(db, message.reply_message.from_id);
			}
		}

		//создание таблицы для конфы(каждая беседа имееют свою свою таблицу хранющую данные о пользователях):
		//Id - используется уникальный идентефикатор вконтакте
		//количество сообщений
		//статуса админа(пользователь, модератор, админ, создатель)
		//количество предупреждений
		//забанен ли пользователь в этой беседе
		if (message.peer_id > 2000000000)
		{
			string sqlCommand = "IF OBJECT_ID(N'dbo.peer" + message.peer_id + "', N'U') IS NULL CREATE TABLE peer" + message.peer_id + " (Id INT PRIMARY KEY NOT NULL, Name NVARCHAR(MAX) NOT NULL, MessageCount INT NOT NULL, WarningCount INT NOT NULL, AdmRole INT NOT NULL , IsInBan INT NOT NULL )";
			sendSqlCommand(sqlCommand);

			string checkUser = "SELECT ISNULL(Id,0) FROM peer" + message.peer_id + " WHERE Id =" + message.from_id;

			if (!sendSqlCommand(checkUser).HasRows)
			{
				sqlCommand = "INSERT INTO peer" + message.peer_id + " VALUES ( " + message.from_id + ", '" + db.Users.Find(message.from_id).Nickname + "', 0, 0, 0, 0)";
				sendSqlCommand(sqlCommand);
			}
			//создание пользователя в случае отсутствия записи


			//кол-во сообщений +1
			sqlCommand = "UPDATE peer" + message.peer_id + " SET MessageCount=MessageCount+1 WHERE Id=" + message.from_id;
			sendSqlCommand(sqlCommand);

			/*создание записи о беседе в общей таблице
			string checkPeer = "SELECT ISNULL(Id,0) FROM peers WHERE Id =" + message.peer_id;

			if (!sendSqlCommand(checkPeer).HasRows)
			{
				sqlCommand = "INSERT INTO peers VALUES ( " + message.peer_id + ", 'NoName', 0, 0, 0, 0)";
				sendSqlCommand(sqlCommand);
			}
			*/
		}
		

		//Проверка содержания текста сообщения
		switch (message.text.ToLower())
		{
			case ".":
				VKAnswer(message.peer_id, "-bot");
				break;

			//развлекателные функции
			case "статы":
			case "стата":
			case "профиль":
			case "персонаж":
			case "stats":
				showStats(message);
				break;

			case "ударить":
			case "убить":
			case "k":
				kick(message);
				break;

			case "хп":
			case "hp":
			case "здоровье":
				showHp(message);
				break;

			case ".heal":
				heal(message);
				break;

			case "помощь":
			case "help":
				showHelp(message);
				break;

			case "брак":
			case "свадьба":
				marriageRequest(message);
				break;

			case "развод":
			case "развестись":
				divorce(message);
				break;

			case "брак да":
			case "брак нет":
				marriage(message);
				break;

			case "браки":
			case "пары":
				marryes(message);
				break;


			//администрирование
			case "кик":
				admKick(message);
				break;

			case "бан":
				admBan(message);
				break;

			case "назначить админом":
			case "назначить модером":
				admGiveRole(message);
				break;



		}


		//увеличение характеристик персонажа
		if (char.ToLower(message.text[0]) == 'н' && char.ToLower(message.text[1]) == 'и' && char.ToLower(message.text[2]) == 'к')
		{
			changeNickaname(message);
		}
		//увеличение характеристик персонажа
		if (message.text.ToLower().Contains("Повысить"))
		{
			useLvlPoints(message);
		}

		//админская(Таблица dbo.Users поле isAdmin) функция начисления очков статов
		if (message.text.ToLower().Contains(".a"))
		{
			admin(message);
		}

		//функция повышения уровня персонажа
		lvlUp(message.from_id);

		//начисление опыта персонажу
		db.GameStats.Find(message.from_id).exp++;
		db.SaveChanges();
	}


	//
	//Общее
	//
	private async void changeNickaname(Message message)
	{
		var db = getNewDbContext();
		var user = db.Users.Find(message.from_id);
		string newNick = message.text.Remove(0, 3);
		user.Nickname = newNick;
		db.SaveChanges();
		string result = "Ник изменен на " + newNick;
		VKAnswer(message.peer_id, result);
	}
	private async void showHelp(Message message)
	{
		string result = "Статы - Характеристики вашего персонажа" + "\n" + "---" + "\n";
		result += "Хп - Очки здоровья" + "\n" + "---" + "\n";
		result += "Ударить - Перешлите сообщение с этой командой чтобы ударить пользователя" + "\n" + "---" + "\n";
		result += "Повысить - параметры с - Сила или з - Защита (обязательные), число (необязательный)" + "\n" + "Например: Повысить з 5";
		VKAnswer(message.peer_id, result);
	}
	private void CreateCharacter(ApplicationContext db, int Vk_id)
	{
		if (!api.IsAuthorized)
		{
			api.Authorize(new ApiAuthParams
			{
				AccessToken = _configuration["Config:AccessToken"]
			});
		}
		GameStat _gs = new GameStat();
		User user = new User();

		_gs.Id = Vk_id;
		_gs.lvl = 1;
		_gs.exp = 0;
		_gs.expToUp = 10;
		_gs.hp = 10;
		_gs.maxHp = 10;
		_gs.power = 1;
		_gs.defence = 0;
		_gs.lvlPoints = 0;
		_gs.isHealing = false;

		user.Id = Vk_id;

		var p = api.Users.Get(new long[] { Vk_id }).FirstOrDefault();
		user.Nickname = p.FirstName + " " + p.LastName;

		user.Nickname = user.Nickname;
		user.IsAdmin = false;
		user.GameStat = _gs;

		db.Users.Add(user);
		db.GameStats.Add(_gs);
		db.SaveChanges();
	}

	//ответ вконтакту
	private async void VKAnswer(int peerId, string message)
	{
		//проверка авторизации в вк
		if (!api.IsAuthorized)
		{
			api.Authorize(new ApiAuthParams
			{
				//access токен для авторизации хранится в файле appsettings.json
				AccessToken = _configuration["Config:AccessToken"]
			});
		}

		Random rnd = new Random();
		api.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams
		{
			RandomId = rnd.Next(0, 999999900), // уникальный id для каждого отправляемого сообщения
			PeerId = peerId, //идентефикатор чата
			Message = message
		}); ;
	}

	//функция генерации контекста БД для ассинхронных методов
	private ApplicationContext getNewDbContext()
	{
		var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
		optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));
		var db = new ApplicationContext(optionsBuilder.Options);
		return db;
	}

	//функция для общения с бд sql запросами
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




	//
	//Развлекательные
	//
	private void marriageRequest(Message message)
	{
		var db = getNewDbContext();
		var _requester = db.Users.Find(message.from_id);
		var _target = db.Users.Find(message.reply_message.from_id);
		string result;
		if (_requester.Marry == 0)
		{
			if (_target.Marry == 0)
			{
				result = "[id" + _requester.Id.ToString() + "|" + _requester.Nickname + "]" + " предложил(а) " + "[id" + _target.Id.ToString() + "|" + _target.Nickname + "]" + " брак" + "\n" + "Для ответа: Брак да / нет";
				_target.MarryageRequest = _requester.Id;
			}
			else
			{
				result = "[id" + _target.Id.ToString() + "|" + _target.Nickname + "]" + " состоит в браке c " + "[id" + _target.Id.ToString() + "|" + _target.Nickname + "]";

			}
		}
		else
		{
			result = "[id" + _requester.Id.ToString() + "|" + _requester.Nickname + "]" + " состоит в браке c " + "[id" + _target.Id.ToString() + "|" + _target.Nickname + "]";
		}
		db.SaveChanges();
		VKAnswer(message.peer_id, result);
	}
	private void marriage(Message message)
	{
		var db = getNewDbContext();
		var _user1 = db.Users.Find(message.from_id);
		var _user2 = db.Users.Find(_user1.MarryageRequest);
		string result = "";
		switch (message.text.ToLower())
		{
			case "брак да":
				_user1.Marry = _user2.Id;
				_user2.Marry = _user1.Id;
				result = "[id" + _user1.Id.ToString() + "|" + _user1.Nickname + "]" + " и " + "[id" + _user2.Id.ToString() + "|" + _user2.Nickname + "]" + " теперь состоят в браке";
				_user1.MarryageRequest = 0;
				break;
			case "брак нет":
				result = "[id" + _user2.Id.ToString() + "|" + _user2.Nickname + "]" + " не хочет брак(((((";
				_user1.MarryageRequest = 0;
				break;

		}
		db.SaveChanges();
		VKAnswer(message.peer_id, result);
	}
	private void divorce(Message message)
	{

		var db = getNewDbContext();
		var _gs1 = db.Users.Find(message.from_id);
		var _gs2 = db.Users.Find(_gs1.Marry);
		string result;
		if (_gs1.Marry != 0)
		{
			_gs1.Marry = 0;
			_gs2.Marry = 0;
			result = "[id" + _gs1.Id.ToString() + "|" + _gs1.Nickname + "]" + " и " + "[id" + _gs2.Id.ToString() + "|" + _gs2.Nickname + "]" + " больше не в браке. Поздравляю!!!";
		}
		else
		{
			result = "Для этого неплохо было бы иметь парнера";
		}

		db.SaveChanges();
		VKAnswer(message.peer_id, result);
	}

	private async void showHp(Message message)
	{
		var db = getNewDbContext();
		string result = "";
		GameStat _gs = db.GameStats.Find(message.from_id);


		_gs = db.GameStats.Find(message.from_id);
		result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + "\n";
		result += "HP: " + _gs.hp.ToString();

		VKAnswer(message.peer_id, result);
	}
	private async void heal(Message message)
	{
		var db = getNewDbContext();
		string result = "";
		GameStat _gs = db.GameStats.Find(message.from_id);
		GameStat _gs2;

		if (message.reply_message != null)
		{
			_gs2 = db.GameStats.Find(message.reply_message.from_id);
			_gs2.hp = _gs2.maxHp;
		}
		else
		{
			_gs.hp = _gs.maxHp;
		}

		result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " HP восстановлено" + "\n";

		db.SaveChangesAsync();
		VKAnswer(message.peer_id, result);
	}
	private async void showStats(Message message)
	{
		var db = getNewDbContext();
		string result = "";
		GameStat _gs = db.GameStats.Find(message.from_id);
		User user = db.Users.Find(message.from_id);
		_gs = db.GameStats.Find(message.from_id);
		result += "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + "\n" + "---" + "\n";
		result += "GOLD: " + _gs.money + "\n";
		result += "Уровень: " + _gs.lvl + "\n";
		result += "Сила: " + _gs.power + "\n";
		result += "Защита: " + _gs.defence + "\n";
		result += "Очки: " + _gs.lvlPoints + "\n" + "---" + "\n";
		result += "До следующего уровня " + (_gs.expToUp - _gs.exp) + " опыта" + "\n";

		VKAnswer(message.peer_id, result);
	}
	private async void kick(Message message)
	{
		var db = getNewDbContext();
		string result = "";
		GameStat _gs = db.GameStats.Find(message.from_id);
		GameStat _gs2 = db.GameStats.Find(message.reply_message.from_id);

		if (_gs.Id == _gs2.Id)
		{
			VKAnswer(message.peer_id, "Не бей себя, глупышка");
			return;
		}

		//пользователь жив?
		if (_gs2.hp > 0)
		{
			//хватает ли сил ударить?
			if (_gs2.defence < _gs.power)
			{
				_gs2.hp -= _gs.power - _gs2.defence;
				result += "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " ударил(а) " + "[id" + message.reply_message.from_id + "|" + db.Users.Find(message.reply_message.from_id).Nickname + "]" + "\n";
				result += "Нанесено " + (_gs.power - _gs2.defence) + " урона" + "\n";

				//убил?
				if (_gs2.hp <= 0)
				{
					_gs2.hp = 0;
					result += "[id" + message.reply_message.from_id + "|" + db.Users.Find(message.reply_message.from_id).Nickname + "]" + " мертв(а)" + "\n";
					double exp1Temp = _gs.expToUp;
					double exp2Temp = _gs2.expToUp;
					//начисление опыта взависимости от разницы уровней
					int loot;
					switch (_gs2.lvl - _gs.lvl)
					{
						case < 0:
							break;
						case < 5:
							loot = Convert.ToInt32((exp2Temp - exp1Temp) / 3);
							_gs.exp += loot;
							result += "Поднято " + loot + " экспы" + "\n";
							break;
						case < 20:
							loot = Convert.ToInt32((exp2Temp - exp1Temp) / 4);
							_gs.exp += loot;
							result += "Поднято " + loot + " экспы" + "\n";
							break;
						case < 50:
							loot = Convert.ToInt32((exp2Temp - exp1Temp) / 6);
							_gs.exp += loot;
							result += "Поднято " + loot + " экспы" + "\n";
							break;
						case < 100:
							loot = Convert.ToInt32((exp2Temp - exp1Temp) / 10);
							_gs.exp += loot;
							result += "Поднято " + loot + " экспы" + "\n";
							break;
					}
					//начисление золота
					result += "Заработано " + giveMoney(_gs, _gs2) + " голды";
				}
				else
				{
					result += "Здоровье:" + _gs2.hp + "\n";
				}
			}
			else
			{
				result += "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " ты слаб(а)";
			}
		}
		else
		{
			_gs2.hp = 0;
			result += "[id" + message.reply_message.from_id + "|" + db.Users.Find(message.reply_message.from_id).Nickname + "]" + " уничтожен(а)";

		}
		if (!_gs2.isHealing)
		{
			healing(message.reply_message.from_id);
		}
		db.SaveChangesAsync();
		VKAnswer(message.peer_id, result);

	}
	private async void lvlUp(int id)
	{
		var db = getNewDbContext();
		GameStat _gs = db.GameStats.Find(id);
		if (_gs.exp >= _gs.expToUp)
		{
			_gs.lvl++;
			_gs.lvlPoints++;
			_gs.maxHp = 8 * _gs.lvl;
			_gs.hp = _gs.maxHp;
			_gs.expToUp = _gs.expToUp + 10 * _gs.lvl;
			db.SaveChanges();
		}
	}
	private string giveMoney(GameStat _gs, GameStat _gs2)
	{
		Random rnd = new Random();
		int soooo;
		//начисление денег за убийство в зависимости от уровня пользователя(target)
		switch (_gs2.lvl)
		{
			case < 10:
				soooo = rnd.Next(1, 5);
				_gs.money += soooo;
				return soooo.ToString();
				break;
			case < 20:
				soooo = rnd.Next(50, 100);
				_gs.money += soooo;
				return soooo.ToString();
				break;
			case < 40:
				soooo = rnd.Next(1000, 5000);
				_gs.money += soooo;
				return soooo.ToString();
				break;
			case < 60:
				soooo = rnd.Next(20000, 100000);
				_gs.money += soooo;
				return soooo.ToString();
				break;
			case < 80:
				soooo = rnd.Next(500000, 2000000);
				_gs.money += soooo;
				return soooo.ToString();
				break;
			default:
				soooo = rnd.Next(10000000, 50000000);
				_gs.money += soooo;
				return soooo.ToString();
		}

	}
	private async void useLvlPoints(Message message)
	{
		var db = getNewDbContext();
		var _gs = db.GameStats.Find(message.from_id);
		string result = "";

		char[] numberInString = message.text.Where(x => Char.IsDigit(x)).ToArray();
		string s = new string(numberInString);
		int messageWantToUp = 0;
		if (s != "")
		{
			messageWantToUp = Convert.ToInt32(s);
		}

		//что хочет повысить пользователь

		switch (message.text[4])
		{
			case 'с':
				if (_gs.lvlPoints > 0)
				{
					if (messageWantToUp == 0)
					{
						_gs.power++;
						_gs.lvlPoints--;
						result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " Сила увеличена на 1";
					}
					else
					{
						if (messageWantToUp <= _gs.lvlPoints)
						{
							_gs.power += messageWantToUp;
							_gs.lvlPoints -= messageWantToUp;
							result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " Сила увеличена на " + messageWantToUp.ToString();
						}
						else
						{
							result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " не хватает очков";
						}
					}
				}
				else
				{
					result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " не хватает очков";
				}
				break;
			case 'з':
				if (_gs.lvlPoints > 0)
				{
					if (messageWantToUp == 0)
					{
						_gs.defence++;
						_gs.lvlPoints--;
						result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " Защита увеличена на 1";
					}
					else
					{
						if (messageWantToUp <= _gs.lvlPoints)
						{
							_gs.defence += messageWantToUp;
							_gs.lvlPoints -= messageWantToUp;
							result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " Защита увеличена на " + messageWantToUp.ToString();
						}
						else
						{
							result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " не хватает очков";
						}
					}
				}
				else
				{
					result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Nickname + "]" + " не хватает очков";
				}
				break;
		}
		db.SaveChanges();
		VKAnswer(message.peer_id, result);
		/*switch (message.text.ToLower())
        {
            case ".up d":
                db.GameStats.Find(message.from_id).defence++;
                break;
            case ".up p":
                db.GameStats.Find(message.from_id).defence++;
                break;
        }*/
	}
	private async void healing(int Vk_id)
	{
		//отхил запускается после первого удара
		//через минуту здоровье полностью восстановиться
		var db = getNewDbContext();

		GameStat _gs = db.GameStats.Find(Vk_id);
		_gs.isHealing = true;
		await Task.Delay(60000);

		_gs.hp = _gs.maxHp;
		_gs.isHealing = false;
		db.SaveChanges();
	}
	private async void admin(Message message)
	{
		if (db.Users.Find(message.from_id).IsAdmin)
		{
			var db = getNewDbContext();
			var _gs = db.GameStats.Find(message.from_id);

			char[] numberInString = message.text.Where(x => Char.IsDigit(x)).ToArray();
			string s = new string(numberInString);
			int messageWantToUp = 1;

			if (s != "")
			{
				messageWantToUp = Convert.ToInt32(s);
			}
			_gs.lvlPoints += messageWantToUp;
			SqlCommand msc;
			db.SaveChanges();

		}
	}

	//вывод браков 
	private async void marryes(Message message)
	{
		//получение пользователей конфы
		string sqlCommand = "SELECT Id FROM peer" + message.peer_id;
		SqlDataReader usersFromPeer = sendSqlCommand(sqlCommand);
		List<int> peerUsers = new List<int>();
		while (usersFromPeer.Read())
		{
			peerUsers.Add(usersFromPeer.GetInt32(0));
		}
		int[] arrPeerUsers = peerUsers.ToArray();

		//получение браков для пользователей конфы
		sqlCommand = "SELECT Id, Marry FROM Users WHERE Marry>0 AND Id IN (peerUsers)";
		sqlCommand = sqlCommand.Replace(
		"peerUsers",
		string.Join(",", arrPeerUsers.Select(pu => pu.ToString())));
		SqlDataReader marriesFromUDB = sendSqlCommand(sqlCommand);
		DataTable dtU = new DataTable();
		dtU.Load(marriesFromUDB);
		var db = getNewDbContext();


		string result = "Браки беседы:\n";
		int i = 0;
		while (i < dtU.Rows.Count)
		{
			DataRow row = dtU.Rows[i];

			result += "[id" + row[0] + "|" + db.Users.Find(row[0]).Nickname + "]" + " + " + "[id" + row[1] + "|" + db.Users.Find(row[1]).Nickname + "]" + "\n";
			i += 2;
		}
		VKAnswer(message.peer_id, result);
	}




	//
	//Управление admXxx
	//
	private void admKick(Message message)
	{
		var db = getNewDbContext();
		string sqlCommand = "SELECT AdmRole FROM peer" + message.peer_id + " WHERE Id=" + message.from_id;
		int senderRole = Convert.ToInt32(sendSqlCommand(sqlCommand).Read());
		string result;
		string requester = db.Users.Find(message.from_id).Nickname;
		string target = db.Users.Find(message.reply_message.from_id).Nickname;
		if (senderRole >= 1)
		{
			sendSqlCommand(sqlCommand);
			result = target + " был исключен";
		}
		else
		{
			result = requester + " у вас недостаточно прав";
		}
		if (!api.IsAuthorized)
		{
			api.Authorize(new ApiAuthParams
			{
				AccessToken = _configuration["Config:AccessToken"]
			});
		}
		var removeChatUser = api.Messages.RemoveChatUser(chatId: Convert.ToUInt32(message.peer_id - 2000000000), userId: message.reply_message.from_id);
		VKAnswer(message.peer_id, result);
	}
	private void admBan(Message message)
	{
		var db = getNewDbContext();
		string sqlCommand = "SELECT AdmRole FROM peer" + message.peer_id + " WHERE Id=" + message.from_id;
		int senderRole = Convert.ToInt32(sendSqlCommand(sqlCommand).GetValue(1));
		string result;
		string requester = db.Users.Find(message.from_id).Nickname;
		string target = db.Users.Find(message.reply_message.from_id).Nickname;
		if (senderRole >= 2)
		{
			sendSqlCommand(sqlCommand);
			sqlCommand = "UPDATE peer" + message.peer_id + " SET IsInBab=1 WHERE Id=" + message.reply_message.from_id;
			sendSqlCommand(sqlCommand);

			result = target + " был забанен";
		}
		else
		{
			result = requester + " у вас недостаточно прав";
		}
		if (!api.IsAuthorized)
		{
			api.Authorize(new ApiAuthParams
			{
				AccessToken = _configuration["Config:AccessToken"]
			});
		}
		var removeChatUser = api.Messages.RemoveChatUser(chatId: Convert.ToUInt32(message.peer_id), userId: message.reply_message.from_id);
		VKAnswer(message.peer_id, result);
	}
	private void admGiveRole(Message message)
	{
		switch (message.text)
		{
			case "назначить модером":
				string sqlCommand = "UPDATE peer" + message.peer_id + " SET AdmRole=1 WHERE Id=" + message.from_id;
				sendSqlCommand(sqlCommand);
				break;
			case "назначить админом":
				sqlCommand = "UPDATE peer" + message.peer_id + " SET AdmRole=2 WHERE Id=" + message.from_id;
				sendSqlCommand(sqlCommand);
				break;
		}


	}
}

