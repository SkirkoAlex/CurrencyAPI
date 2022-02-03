using CurrencyAPI.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NLog;
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
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public CurrencyService(IOptions<Settings> settings)
        {
            _settings = settings.Value;
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
                HttpClientHandler hch = new HttpClientHandler();
                hch.Proxy = null;
                hch.UseProxy = false;
                using (HttpClient client = new HttpClient(hch))
                {
                    var response = await client.GetAsync(_settings.Url);
                    if (response != null)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var jObject = JObject.Parse(jsonString).Last.Children().ToList().Children().ToList();
                        tickers.AddRange(jObject.SelectMany(item => item.Children<JObject>()
                        .SelectMany(content => content.Properties()
                        .Where(prop => prop.Name == "CharCode")
                        .Select(prop => prop.Value.ToString()))));
                    }
                }
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
                HttpClientHandler hch = new HttpClientHandler();
                hch.Proxy = null;
                hch.UseProxy = false;
                using (HttpClient client = new HttpClient(hch))
                {
                    var response = await client.GetAsync(_settings.Url);
                    if (response != null)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var jObject = JObject.Parse(jsonString).Last.Children().ToList().Children().ToList();
                        foreach (var prop in jObject.SelectMany(item => item.Children<JObject>()
                        .SelectMany(content => content.Properties()
                        .Where(prop => prop.Name == "CharCode" && prop.Value.ToString() == ticker.ToUpper())
                           )))
                        {
                            rate = (double)prop.Next.Next.Next;
                            return rate;
                        }
                    }
                }

                return rate;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return 0;
            }
        }
    }
}
