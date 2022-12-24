namespace TestWork4.Enums.Translations;

public static class FlatStatusTranslation
{
    public static string GetRussianName(FlatStatus flatStatus) => flatStatus switch
    {
        FlatStatus.Sold => "Продано",
        FlatStatus.Reserved => "Бронь",
        FlatStatus.Sale => "В продаже",
        FlatStatus.Unknown => "",
        _ => throw new ArgumentOutOfRangeException(nameof(flatStatus), flatStatus, null)
    };
}