namespace Api.Models.Common
{
    /// <summary>
    /// HTTP response model of the API, wrapping multiple items in one response.
    /// </summary>
    /// <typeparam name="T">the type of objects returned</typeparam>
    public class ItemCollectionResponse<T>
    {
        public ICollection<T> Items { get; set; } = [];
        public int TotalCount { get { return Items.Count; } }
    }
}
