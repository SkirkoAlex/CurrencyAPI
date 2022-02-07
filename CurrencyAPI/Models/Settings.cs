using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyAPI.Models
{
    public class Settings
    {
        public string Url { get; set; }
        public string RedisConnectionString { get; set; }
        public TimeSpan RedisCashLifeTime { get; set; }
    }
}
