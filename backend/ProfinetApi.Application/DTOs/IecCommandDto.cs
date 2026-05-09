public class IecCommandDto
{
    public string Id { get; set; } = "";
    public string? SourceId { get; set; }
    public bool Checked { get; set; }
    public string Node { get; set; } = "Root";
    public string ReceiverName { get; set; } = "";
    public int Asdu { get; set; }
    public int Ioa { get; set; }
    public string FrameType { get; set; } = "";
    public string CsDataType { get; set; } = "";
}
