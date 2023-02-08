using System;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VkNet.Model;

namespace dotBot.Models
{
    [Serializable]
    public class Updates
    {
        /// <summary>
        /// Тип события
        /// </summary>
        [JsonProperty("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Тип события
        /// </summary>
        [JsonProperty("event_id")]
        public string? EventId { get; set; }

        /// <summary>
        /// Тип события
        /// </summary>
        [JsonProperty("v")]
        public string? apiVer { get; set; }

        /// <summary>
        /// Объект, инициировавший событие
        /// Структура объекта зависит от типа уведомления
        /// </summary>
        [JsonProperty("object")]
        public Object? Object { get; set; }

        /// <summary>
        /// ID сообщества, в котором произошло событие
        /// </summary>
        [JsonProperty("group_id")]
        public long? GroupId { get; set; }

        [JsonProperty("secret")]
        public string? Secret { get; set; }
    }


    

    public class Message
    {
        public int date { get; set; }
        public int from_id { get; set; }
        public int id { get; set; }
        public int @out { get; set; }
        public List<object> attachments { get; set; }
        public int conversation_message_id { get; set; }
        public List<object> fwd_messages { get; set; }
        public bool important { get; set; }
        public bool is_hidden { get; set; }
        public int peer_id { get; set; }
        public int random_id { get; set; }
        public ReplyMessage? reply_message { get; set; }
        public string text { get; set; }
    }

    public class Object
    {
        public Message message { get; set; }
        public JsonObject? client_info { get; set; }
    }

    public class ReplyMessage
    {
        public int date { get; set; }
        public int from_id { get; set; }
        public string text { get; set; }
        public List<object> attachments { get; set; }
        public int conversation_message_id { get; set; }
        public int id { get; set; }
        public int peer_id { get; set; }
    }
}
