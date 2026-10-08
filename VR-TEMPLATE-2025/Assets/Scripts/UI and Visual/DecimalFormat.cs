using System.Globalization;

/// <summary>Formata números com uma casa decimal e vírgula como separador (ex: 12,5), independente da cultura do dispositivo.</summary>
public static class DecimalFormat
{
    private static readonly NumberFormatInfo CommaFormat = new NumberFormatInfo
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "."
    };

    public static string OneDecimal(float value) => value.ToString("F1", CommaFormat);
    public static string Percent(float value) => OneDecimal(value) + "%";
}
