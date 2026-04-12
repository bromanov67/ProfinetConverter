public class CommandDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Node { get; set; } = "Root";
    public int RegAddress { get; set; }
    public string DataType { get; set; } = "Bool";
    public string CsDataType { get; set; } = "BOOLEAN";
    public string Retranslation { get; set; } = "-";
    public bool Checked { get; set; }
}