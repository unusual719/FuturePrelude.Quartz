namespace FuturePrelude.Quartz;

/// <summary> Job 分组出参 </summary>
public class JobGroupOutput
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Status { get; set; }
    public string Description { get; set; }
}