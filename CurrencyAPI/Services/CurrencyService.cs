using CurrencyAPI.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NLog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CurrencyAPI.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly Settings _settings;
        private readonly IDatabase _redis;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public CurrencyService(IOptions<Settings> settings)
        {
            _settings = settings.Value;
            var redisDatabase = ConnectionMultiplexer.Connect(_settings.RedisConnectionString);
            _redis = redisDatabase.GetDatabase();
        }

        /// <summary>
        /// Возвращает все доступные тикеры валют
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAllTickers()
        {
            List<string> tickers = new();
            try
            {
                string dailyRatesJson = await GetDailyRatesJson();

                var jObject = JObject.Parse(dailyRatesJson).Last.Children().ToList().Children().ToList();
                tickers.AddRange(jObject.SelectMany(item => item.Children<JObject>()
                .SelectMany(content => content.Properties()
                .Where(prop => prop.Name == "CharCode")
                .Select(prop => prop.Value.ToString()))));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return null;
            }

            return tickers;
        }

        /// <summary>
        /// Возвращает текущий курс валюты по тикеру
        /// </summary>
        /// <param name="ticker">Тикер валюты</param>
        /// <returns></returns>
        public async Task<double> GetRateByTicker(string ticker)
        {
            double rate = 0;
            try
            {
                string dailyRatesJson = await GetDailyRatesJson();

                var jObject = JObject.Parse(dailyRatesJson).Last.Children().ToList().Children().ToList();
                foreach (var prop in jObject.SelectMany(item => item.Children<JObject>()
                .SelectMany(content => content.Properties()
                .Where(prop => prop.Name == "CharCode" && prop.Value.ToString() == ticker.ToUpper())
                   )))
                {
                    rate = (double)prop.Next.Next.Next;
                    return rate;
                }

                return rate;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Получает ежедневный список валют и курсов из Redis или по HTTP, если кэш еще пуст
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetDailyRatesJson()
        {
            var dailyRatesJson = await GetRedisDataAsync($"{DateTime.Now.ToShortDateString()}");
            if (string.IsNullOrEmpty(dailyRatesJson))
            {
                HttpClientHandler hch = new HttpClientHandler();
                hch.Proxy = null;
                hch.UseProxy = false;
                using (HttpClient client = new HttpClient(hch))
                {
                    var response = await client.GetAsync(_settings.Url);
                    if (response != null)
                    {
                        dailyRatesJson = await response.Content.ReadAsStringAsync();
                        await SetRedisDataAsync(DateTime.Now.ToShortDateString(), dailyRatesJson);
                    }
                }
            }

            return dailyRatesJson;
        }

        /// <summary>
        /// Получает данные из Redis
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        private async Task<string> GetRedisDataAsync(string redisKey)
        {
            try
            {
                var json = await _redis.StringGetAsync(redisKey);
                return json;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Receiving data from Redis is unsuccessful. " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Записывает данные в Redis
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        private async Task SetRedisDataAsync(string redisKey, string json)
        {
            try
            {
                var lifeTime = _settings.RedisCashLifeTime;
                await _redis.StringSetAsync(redisKey, json, lifeTime);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Writing data to Redis is unsuccessful. " + e.Message);
            }
        }
    }
}
