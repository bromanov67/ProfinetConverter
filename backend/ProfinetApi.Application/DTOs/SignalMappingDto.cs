public class SignalMappingDto
{
    public string Name { get; set; }           // Имя сигнала
    public string DataType { get; set; }       // Тип (Bool, Float32, Int16 и т.д.)

    // Поля от PROFINET
    public int ByteOffset { get; set; }        // Смещение байта (0..63)
    public int BitOffset { get; set; }         // Смещение бита (0..7)

    // Поле от IEC 104
    public int IOA { get; set; }               // Адрес Information Object Address
}