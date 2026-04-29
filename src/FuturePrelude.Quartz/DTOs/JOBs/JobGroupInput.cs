namespace FuturePrelude.Quartz;

/// <summary> JobGroup 分组基础入参 </summary>
public class JobGroupBaseInput
{
    /// <summary> 分组主键，-1：表示新增 </summary>
    public long Id { get; set; }
}

/// <summary> JobGroup 分组入参 </summary>
public class JobGroupInput : JobGroupBaseInput
{
    /// <summary> 分组名称（唯一） </summary>
    public string Name { get; set; }

    /// <summary> 状态（0-禁用，1-启用） </summary>
    public int Status { get; set; }

    /// <summary> 分组描述 </summary>
    public string Description { get; set; }

    /// <summary> 创建人 </summary>
    public string CreateBy { get; set; }

    /// <summary> 更新人 </summary>
    public string UpdateBy { get; set; }
}
