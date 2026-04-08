namespace FleetBharat.TMSService.Application.DTOs
{
    public class CommonApiSingleResponse<T>
    {
        public bool success { get; set; }
        public int statusCode { get; set; }
        public string? message { get; set; }
        public T? data { get; set; }
    }
}
