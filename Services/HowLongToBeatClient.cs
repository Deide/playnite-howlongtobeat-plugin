﻿using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using HowLongToBeat.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatClient
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private string UrlBase { get; set; }
        private string UrlSearch { get; set; }
        private string UrlGame { get; set; }


        public HowLongToBeatClient()
        {
            UrlBase = "https://howlongtobeat.com/";
            UrlSearch = UrlBase + "search_results.php";
            UrlGame = UrlBase + "game.php?id=";
        }

        public List<HltbData> Search(string Name)
        {
            string data = GameSearch(Name).GetAwaiter().GetResult();
            return SearchParser(data);
        }

        /// <summary>
        /// Download search data.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private async Task<string> GameSearch(string Name)
        { 
            try
            {
                HttpClient client = new HttpClient();

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("queryString", Name),
                    new KeyValuePair<string, string>("t", "games"),
                    new KeyValuePair<string, string>("sorthead", "popular"),
                    new KeyValuePair<string, string>("sortd", "Normal Order"),
                    new KeyValuePair<string, string>("plat", ""),
                    new KeyValuePair<string, string>("length_type", "main"),
                    new KeyValuePair<string, string>("length_min", ""),
                    new KeyValuePair<string, string>("length_max", ""),
                    new KeyValuePair<string, string>("detail", "0")
                });
                
                var response = client.PostAsync(UrlSearch, content).Result;
                string responseBody = response.Content.ReadAsStringAsync().Result;

                return responseBody;
            }
            catch (HttpRequestException ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                logger.Error(ex, $"HowLongToBeat [{FileName} {LineNumber}] ");

                return "";
            }
        }

        /// <summary>
        /// Convert Time string from hltb to long seconds.
        /// </summary>
        /// <param name="Time"></param>
        /// <returns></returns>
        private long ConvertStringToLong(string Time)
        {
            if (Time.IndexOf("Hours") > -1)
            {
                Time = Time.Replace("Hours", "");
                Time = Time.Replace("&#189;", ".5");
                Time = Time.Replace("½", ".5");
                Time = Time.Trim();

                return (long)(Convert.ToDouble(Time, new NumberFormatInfo { NumberGroupSeparator = "." }) * 3600);
            }

            if (Time.IndexOf("Mins") > -1)
            {
                Time = Time.Replace("Mins", "");
                Time = Time.Replace("&#189;", ".5");
                Time = Time.Replace("½", ".5");
                Time = Time.Trim();

                return (long)(Convert.ToDouble(Time, new NumberFormatInfo { NumberGroupSeparator = "." }) * 60);
            }

            return 0;
        }

        /// <summary>
        /// Parse html search result.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private List<HltbData> SearchParser(string data)
        {
            List<HltbData> ReturnData = new List<HltbData>();

            if (data != "")
            {
                try {
                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(data);

                    string Name = "";
                    int Id = 0;
                    string UrlImg = "";
                    string Url = "";

                    long MainStory = 0;
                    long MainExtra = 0;
                    long Completionist = 0;
                    long Solo = 0;
                    long CoOp = 0;
                    long Vs = 0;

                    bool IsMainStory = true;
                    bool IsMainExtra = true;
                    bool IsCompletionist = true;

                    foreach (var SearchElement in htmlDocument.QuerySelectorAll("li.back_darkish"))
                    {
                        var ElementA = SearchElement.QuerySelector(".search_list_image a");
                        var ElementImg = SearchElement.QuerySelector(".search_list_image a img");
                        Name = ElementA.GetAttribute("title");
                        Id = int.Parse(ElementA.GetAttribute("href").Replace("game?id=", ""));
                        UrlImg = ElementImg.GetAttribute("src");
                        Url = UrlBase + ElementA.GetAttribute("href");

                        var ElementDetails = SearchElement.QuerySelector(".search_list_details_block");
                        var Details = ElementDetails.QuerySelectorAll(".search_list_tidbit");
                        if (Details.Length == 0)
                        {
                            Details = ElementDetails.QuerySelectorAll("div");
                        }
                        int iElement = 0;

                        foreach (var El in Details)
                        {
                            switch (iElement)
                            {
                                case 0:
                                    IsMainStory = (El.InnerHtml == "Main Story");
                                    break;
                                case 1:
                                    if (El.InnerHtml != "")
                                    {
                                        if (IsMainStory)
                                        {
                                            MainStory = ConvertStringToLong(El.InnerHtml);
                                        }
                                        else
                                        {
                                            Solo = ConvertStringToLong(El.InnerHtml);
                                        }
                                    }
                                    break;

                                case 2:
                                    IsMainExtra = (El.InnerHtml == "Main + Extra");
                                    break;
                                case 3:
                                    if (El.InnerHtml != "")
                                    {
                                        if (IsMainExtra)
                                        {
                                            MainExtra = ConvertStringToLong(El.InnerHtml);
                                        }
                                        else
                                        {
                                            CoOp = ConvertStringToLong(El.InnerHtml);
                                        }
                                    }
                                    break;

                                case 4:
                                    IsCompletionist = (El.InnerHtml == "Completionist");
                                    break;
                                case 5:
                                    if (El.InnerHtml != "")
                                    {
                                        if (IsCompletionist)
                                        {
                                            Completionist = ConvertStringToLong(El.InnerHtml);
                                        }
                                        else
                                        {
                                            Vs = ConvertStringToLong(El.InnerHtml);
                                        }
                                    }
                                    break;
                            }
                            iElement += 1;
                        }

                        //logger.Debug($"Name: {Name} - MainStory: {MainStory} - MainExtra: {MainExtra} - Completionist: {Completionist} - Solo: {Solo} - CoOp: {CoOp} - Vs: {Vs}");
                        ReturnData.Add(new HltbData
                        {
                            Name = Name,
                            Id = Id,
                            UrlImg = UrlImg,
                            Url = Url,
                            MainStory = MainStory,
                            MainExtra = MainExtra,
                            Completionist = Completionist,
                            Solo = Solo,
                            CoOp = CoOp,
                            Vs = Vs
                        });
                    }
                }
                catch (Exception ex)
                {
                    var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                    string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                    logger.Error(ex, $"HowLongToBeat [{FileName} {LineNumber}] ");
                }
            }

            return ReturnData;
        }
    }
}
