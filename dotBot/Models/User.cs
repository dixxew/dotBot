using System.ComponentModel.DataAnnotations;

namespace dotBot.Models
{
    public class User
    {
        public int  Id { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public bool IsAdmin { get; set; }
        public GameStat GameStat { get; set; }
        public int Marry { get; set; }
        public int MarryageRequest { get; set; }

    }
    public class GameStat
    {
        public int Id { get; set; }
        public int lvl { get; set; }
        public int exp { get; set; }
        public int expToUp { get; set; }
        public int hp { get; set; }
        public int maxHp { get; set; }
        public int power { get; set; }
        public int defence { get; set; }
        public int lvlPoints { get; set; }
        public bool isHealing { get; set; }
        public int money { get; set; }

    }
    

    
}
    
