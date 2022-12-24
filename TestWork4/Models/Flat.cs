using TestWork4.Enums;
using TestWork4.Enums.Translations;

namespace TestWork4.Models;

public class Flat
{
    public string Id { get; set; }
    public string Number { get; set; }
    public string Building { get; set; }
    public int Floor { get; set; }
    public decimal Area { get; set; }
    public int Rooms { get; set; }
    public int? Price { get; set; }
    public FlatStatus Status { get; set; }

    public decimal? SPrice { get => Price is null ? null : Math.Round((decimal)(Price / Area), 2); }
    
    public DateTime Created = DateTime.Now;
    
    public override string ToString() =>
        string.Join('|', Id, Number, Building, Floor, Area.ToString().Replace(',', '.'), Rooms, Price, SPrice,
            FlatStatusTranslation.GetRussianName(Status), Created.ToString("yyyy-MM-dd"));
}