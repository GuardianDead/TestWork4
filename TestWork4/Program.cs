using TestWork4.Enums;
using TestWork4.Services;


var flatParser = new FlatParserService();
var sites = Enum.GetValues<FlatSite>();
Parallel.ForEach(sites, site =>
{
    var flats = flatParser.Parse(site);
    var stringFlats = string.Join("\r\n", flats);
    File.WriteAllText($@"{site}.txt", stringFlats);
});