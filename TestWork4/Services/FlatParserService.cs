using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RestSharp;
using TestWork4.Enums;
using TestWork4.Models;

namespace TestWork4.Services;

public class FlatParserService
{
    protected readonly RestClient _client;
    protected readonly HtmlWeb _htmlParser = new();

    public FlatParserService(RestClient? client = null)
    {
        _client = client ?? new RestClient();
    }

    public IEnumerable<Flat> Parse(FlatSite site) => site switch
    {
        FlatSite.Bakeevopark => ParseBakeevopark(),
        FlatSite.BiryuzovayaZhemchuzhina => ParseBiryuzovayaZhemchuzhina(),
        FlatSite.Smssnsk => ParseSmssnsk(),
        FlatSite.DomRafinad => ParseDomRafinad(),
        _ => throw new ArgumentOutOfRangeException(nameof(site), site, null)
    };

    private IEnumerable<Flat> ParseBakeevopark()
    {
        var request = new RestRequest("http://bakeevopark.ru/ajax/handler.php");
        request.AddStringBody("params%5Bbuild_status%5D%5B%5D=unfinished&params%5Bbuild_status%5D%5B%5D=ready&par" +
                              "ams%5Bsquare-from%5D=30.36&params%5Bsquare-to%5D=113.61&params%5Bprice-from%5D=4+070" +
                              "+000&params%5Bprice-to%5D=14+179+594&params%5Bparam1%5D=false&params%5Bparam2%5D=fa" +
                              "lse&params%5Bparam3%5D=false&params%5Bparam4%5D=false&params%5Bparam5%5D=false&params" +
                              "%5Bparam6%5D=false&params%5Bparam8%5D=false&params%5Bparam9%5D=false&params%5Bparam10%" +
                              "5D=false&params%5Bparam11%5D=false&params%5Bparam12%5D=false&params%5Bparam13%5D=false&p" +
                              "arams%5Bparam14%5D=false&params%5Bbalcony%5D=false&params%5Bsort%5D=1",
            "application/x-www-form-urlencoded");
        foreach (var rawFlat in JArray.Parse(_client.Post(request).Content).AsParallel())
            yield return new Flat()
            {
                Id = rawFlat["FIELDS"]["ID"].Value<string>(),
                Area = rawFlat["PROPS"]["SQUARE"]["VALUE"].Value<decimal>(),
                Building = "Дом №" + rawFlat["PROPS"]["HOUSE"]["VALUE"].Value<string>(),
                Floor = rawFlat["PROPS"]["FLOOR"]["VALUE"].Value<int>(),
                Number = rawFlat["PROPS"]["FLAT_NUMBER"]["VALUE"].Value<string>(),
                Price = rawFlat["FIELDS"]["PROPERTY_PRICE_VALUE"].Value<int>(),
                Rooms = rawFlat["PROPS"]["ROOMS_COUNT"]["VALUE"].Value<int>() - 1,
                Status = FlatStatus.Unknown
            };
    }

    private IEnumerable<Flat> ParseBiryuzovayaZhemchuzhina()
    {
        var rawFlats = _htmlParser.Load("https://2.ac-biryuzovaya-zhemchuzhina.ru/flats/all?floor=&type=&" +
                                        "status=&minArea=&maxArea=&minPrice=&maxPrice=").DocumentNode
            .SelectNodes("//tbody/tr");
        foreach (var rawFlat in rawFlats.AsParallel())
        {
            var id = rawFlat.ChildNodes[11].FirstChild.InnerText[0].ToString();
            var rooms = rawFlat.ChildNodes[1].FirstChild.InnerText[0];
            var price = rawFlat.ChildNodes[5].ChildNodes.Count < 3 ? null : rawFlat.ChildNodes[5].ChildNodes[1].InnerText;
            var type = rawFlat.ChildNodes[7].ChildNodes[1].InnerText switch
            {
                "Продано" => FlatStatus.Sold,
                "Бронировать" => FlatStatus.Sale,
                "Забронировано" => FlatStatus.Reserved,
                _ => FlatStatus.Unknown
            };
            yield return new Flat()
            {
                Id = id,
                Area = decimal.Parse(rawFlat.ChildNodes[3].InnerText.Replace('.', ',')),
                Building = "Секция №" + rawFlat.ChildNodes[17].InnerText,
                Floor = int.Parse(rawFlat.ChildNodes[9].FirstChild.InnerText),
                Number = id,
                Price = price == null ? null : int.Parse(price[..^5].Replace(" ", "")),
                Rooms = char.IsDigit(rooms) ? rooms : 0,
                Status = type
            };
        }
    }

    private IEnumerable<Flat> ParseSmssnsk()
    {
        var request = new RestRequest("https://an.smssnsk.ru/");
        var response = _client.Get(request);
        request.Resource = "https://an.smssnsk.ru/site/search";
        request.AddOrUpdateHeader("Cookie", response.Headers.ToArray()[4].Value.ToString());
        request.AddOrUpdateHeader("X-CSRF-Token", new Regex("<meta name=\"csrf-token\" content=\"(\\w.*)\">")
            .Match(response.Content).Groups[1].Value);
        request.AddStringBody("flats_type=%D0%9A%D0%B2%D0%B0%D1%80%D1%82%D0%B8%D1%80%D0%B0&r" +
                              "esult=search&sort=&curr_page=1&type_search=result&count_result=200" +
                              "&min_cost=4600000&max_cost=12700000&min_square=31&max_square=102&g" +
                              "k=80", "application/x-www-form-urlencoded");

        foreach (var rawFlat in JArray.Parse(_client.Post(request).Content).AsParallel())
            yield return new Flat()
            {
                Id = rawFlat["id"].Value<string>(),
                Area = rawFlat["area_total"].Value<decimal>(),
                Building = "Дом №" + rawFlat["houseid"].Value<string>(),
                Floor = rawFlat["floor"]["number"].Value<int>(),
                Number = rawFlat["number"].Value<string>(),
                Price = rawFlat["price"].Value<int>(),
                Rooms = rawFlat["typeid"].Value<int>() / 2,
                Status = FlatStatus.Unknown
            };
    }

    private IEnumerable<Flat> ParseDomRafinad()
    {
        var request = new RestRequest("https://dom-rafinad.ru/ajax/get_flats_example.json?&limit=1000");
        foreach (var rawFlat in JObject.Parse(_client.Get(request).Content)["flats"].AsParallel())
            yield return new Flat()
            {
                Id = rawFlat["id"].ToString(),
                Area = decimal.Parse(rawFlat["area"].ToString()),
                Building = "Корпус №" + rawFlat["bld"] + " Секция №" + rawFlat["section"],
                Floor = int.Parse(rawFlat["floor"].ToString()),
                Number = rawFlat["num"].ToString(),
                Price = int.Parse(rawFlat["price"].ToString()),
                Rooms = int.Parse(rawFlat["rooms"].ToString()),
                Status = FlatStatus.Unknown
            };
    }
}