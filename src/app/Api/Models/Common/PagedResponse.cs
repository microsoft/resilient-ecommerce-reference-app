namespace Api.Models.Common
{
    /// <summary>
    /// HTTP response model for paged data.
    /// </summary>
    /// <typeparam name="T">the type of objects the page holds</typeparam>
    public class PagedResponse<T>
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int Skipped { get; set; }
        public ICollection<T> PageData { get; set; } = [];
    }
}
