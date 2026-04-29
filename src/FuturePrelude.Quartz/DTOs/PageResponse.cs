namespace FuturePrelude.Quartz;

/// <summary> 分页响应 </summary>
public class PageResponse<T>
{
    /// <summary> 数据列表 </summary>
    public List<T> Items { get; set; }

    /// <summary> 总记录数 </summary>
    public int TotalCount { get; set; }

    /// <summary> 页码 </summary>
    public int PageIndex { get; set; }

    /// <summary> 每页条数 </summary>
    public int PageSize { get; set; }

    /// <summary> 总页数 </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}