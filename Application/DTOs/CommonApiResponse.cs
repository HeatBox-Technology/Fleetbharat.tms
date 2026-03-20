using System.Collections.Generic;

namespace FleetBharat.TMSService.Application.DTOs
{
    public class CommonApiResponse<T>
    {
        public bool success { get; set; }
        public int statusCode { get; set; }
        public string? message { get; set; }
        public List<T>? data { get; set; }
    }
}
