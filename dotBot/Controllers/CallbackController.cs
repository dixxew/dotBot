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
    List<String> answerCheckList_hi = new List<String> {".hi", ".qq"};
    

    public CallbackController(IConfiguration configuration, ApplicationContext context)
    {
        _configuration = configuration;
        db = context;
    }

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
                //VKAnswer(updates.Object.message.peer_id, "bot");
                break;
        }
        
        // Возвращаем "ok" серверу Callback API        
        return Ok("ok");
    }
    public void CheckMessage(ApplicationContext db, Message message)
    {
        //проверяем есть ли юзер в бд
        if (db.Users.Find(message.from_id) == null)
        {
            CreateCharacter(db, message.from_id);
        }

        //провеяем есть ли отвеченное сообщение
        if (message.reply_message != null)
        {
            //проверяем есть ли в бд юзер отвечаемого сообщения
            if (db.Users.Find(message.reply_message.from_id) == null)
            {
                CreateCharacter(db, message.reply_message.from_id);
            }
        }

        switch (message.text.ToLower())
        {
            case ".":
                VKAnswer(message.peer_id, "-bot");
                break;   
                
            case "статы":
            case "стата":
            case "профиль":
            case "персонаж":
            case "stats":
                showStats(message);
                break;

            case "ударить":
            case "убить":
            case "уебать":
            case "врезать":
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
            
            
        }
        if (message.text.ToLower().Contains(".up"))
        {
            useLvlPoints(message);
        }
        if (message.text.ToLower().Contains(".a"))
        {
            admin(message);
        }
        lvlUp(message.from_id);
        db.GameStats.Find(message.from_id).exp++;
        db.SaveChanges();
    }

    public async void loot(Message message)
    {

    }

    private async void showHp(Message message)
    {
        var db = getNewDbContext();
        string result = "";
        GameStat _gs = db.GameStats.Find(message.from_id);

        
        _gs = db.GameStats.Find(message.from_id);
        result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + "\n";
        result += "HP: " + _gs.hp.ToString();

        VKAnswer(message.peer_id, result);
    }
    
    public async void showHelp(Message message)
    {
        string result = "Статы - Характеристики вашего персонажа" + "\n" + "---" + "\n";
        result += "Хп - Очки здоровья" + "\n" + "---" + "\n";
        result += "Ударить - Перешлите сообщение с этой командой чтобы ударить пользователя" + "\n" + "---" + "\n";
        result += "Повысить - параметры с - Сила или з - Защита (обязательные), число (необязательный)" + "\n" + "Например: Повысить з 5";
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

        result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " HP восстановлено" + "\n";

        db.SaveChangesAsync();
        VKAnswer(message.peer_id, result);
    }

    private async void showStats(Message message)
    {
        var db = getNewDbContext();
        string result = "";
        GameStat _gs = db.GameStats.Find(message.from_id) ;
        User user = db.Users.Find(message.from_id);
        _gs = db.GameStats.Find(message.from_id);
        result += user.Name + "\n";
        result += "Уровень: " + _gs.lvl + "\n";
        result += "Сила: " + _gs.power + "\n";
        result += "Защита: " + _gs.defence + "\n";
        result += "Очки: " + _gs.lvlPoints + "\n" + "---" + "\n";
        result += "До следующего уровня " + (_gs.expToUp - _gs.exp) + " опыта" +"\n";

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
            VKAnswer(message.peer_id, "Не бей себя, глупИшка");
            return;
        }
        

        if (_gs2.hp > 0)
        {
            if (_gs2.defence < _gs.power)
            {
                _gs2.hp -= _gs.power - _gs2.defence;
                result += "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " ушатал(а) " + "[id" + message.reply_message.from_id + "|" + db.Users.Find(message.reply_message.from_id).Name + "]" + "\n";
                result += "Нанесено " + (_gs.power-_gs2.defence) + " урона" + "\n";


                if (_gs2.hp <= 0)
                {
                    _gs2.hp = 0;                    
                    result += "[id" + message.reply_message.from_id + "|" + db.Users.Find(message.reply_message.from_id).Name + "]" + " мертв(а)";
                    double exp1Temp = _gs.expToUp;
                    double exp2Temp = _gs2.expToUp;
                    int loot;
                    switch (_gs2.lvl - _gs.lvl)
                    {
                        case < 0:
                            break;
                        case < 5:
                            loot = Convert.ToInt32((exp2Temp - exp1Temp) / 3);
                            _gs.exp += loot;
                            result += "Поднято " + loot + " экспы";
                            break;
                        case < 20:
                            loot = Convert.ToInt32((exp2Temp - exp1Temp) / 4);
                            _gs.exp += loot;
                            result += "Поднято " + loot + " экспы";
                            break;
                        case < 50:
                            loot = Convert.ToInt32((exp2Temp - exp1Temp) / 6);
                            _gs.exp += loot;
                            result += "Поднято " + loot + " экспы";
                            break;
                        case < 100:
                            loot = Convert.ToInt32((exp2Temp - exp1Temp) / 10);
                            _gs.exp += loot;
                            result += "Поднято " + loot + " экспы";
                            break;
                    }
                }
                else
                {
                    result += "Здоровье:" + _gs2.hp + "\n";
                }
            } else
            {
                result += "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " ты слаб(а)";
            }
        }
        else
        {
            _gs2.hp = 0;
            result += "[id" + message.reply_message.from_id + "|" + db.Users.Find(message.reply_message.from_id).Name + "]" + " уничтожен(а)";

        }
        if (!_gs2.isHealing)
        {
            healing(message.reply_message.from_id);
        }
        db.SaveChangesAsync();
        VKAnswer(message.peer_id, result);

    }

    public async void lvlUp(int id)
    {
        var db = getNewDbContext();
        GameStat _gs = db.GameStats.Find(id);
        if (_gs.exp >= _gs.expToUp)
        {
            _gs.lvl++;
            _gs.lvlPoints++;
            _gs.maxHp = 8 * _gs.lvl;
            _gs.hp = _gs.maxHp;
            _gs.expToUp =  _gs.expToUp + 10 * _gs.lvl ;
            db.SaveChanges();
        }
    }

    public async void useLvlPoints(Message message)
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
        


        switch (message.text[4])
        {
            case 'с':
                if (_gs.lvlPoints> 0)
                {
                    if (messageWantToUp == 0)
                    {
                        _gs.power++;
                        _gs.lvlPoints--;
                        result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " Сила увеличена на 1";
                    } else
                    {
                        if (messageWantToUp <= _gs.lvlPoints)
                        {
                            _gs.power += messageWantToUp;
                            _gs.lvlPoints -= messageWantToUp;
                            result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " Сила увеличена на " + messageWantToUp.ToString();
                        }
                        else
                        {
                            result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " не хватает очков";
                        }
                    }
                } else
                {
                    result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " не хватает очков";
                }
                break;
            case 'з':
                if (_gs.lvlPoints > 0)
                {
                    if (messageWantToUp == 0)
                    {
                        _gs.defence++;
                        _gs.lvlPoints--;
                        result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " Защита увеличена на 1";
                    }
                    else
                    {
                        if (messageWantToUp <= _gs.lvlPoints)
                        {
                            _gs.defence += messageWantToUp;
                            _gs.lvlPoints -= messageWantToUp;
                            result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " Защита увеличена на " + messageWantToUp.ToString();
                        }
                        else
                        {
                            result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " не хватает очков";
                        }
                    }
                }
                else
                {
                    result = "[id" + message.from_id + "|" + db.Users.Find(message.from_id).Name + "]" + " не хватает очков";
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

    //отхил запускается сразу после удара
    public async void healing(int Vk_id)
    {
        var db = getNewDbContext();

        GameStat _gs = db.GameStats.Find(Vk_id);
        _gs.isHealing = true;
        await Task.Delay(60000);

        _gs.hp = _gs.maxHp;
        _gs.isHealing = false;
        db.SaveChanges();
    }

    public async void admin(Message message)
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
            db.SaveChanges();
        }
    }

    public void CreateCharacter(ApplicationContext db, int Vk_id)
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
        user.Name = p.FirstName + " " + p.LastName;

        user.Nickname = user.Name;        
        user.IsAdmin = false;
        user.GameStat = _gs;

        db.Users.Add(user);
        db.GameStats.Add(_gs);
        db.SaveChanges();
    }

    public async void VKAnswer(int peerId, string message)
    {
        VkApi api = new VkApi();
        if (!api.IsAuthorized)
        {
            api.Authorize(new ApiAuthParams
            {
                AccessToken = _configuration["Config:AccessToken"]
            });
        }
        Random rnd = new Random(); 
        api.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams
        {
            RandomId = rnd.Next(0, 999999900), // уникальный
            PeerId = peerId,
            Message = message
        }); ;
    }

    public ApplicationContext getNewDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
        optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));
        var db = new ApplicationContext(optionsBuilder.Options);
        return db;
    }
}

