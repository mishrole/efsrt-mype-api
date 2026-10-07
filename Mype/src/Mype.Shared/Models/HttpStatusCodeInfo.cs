using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mype.Shared.Models
{
    [ExcludeFromCodeCoverage]
    public class HttpStatusCodeInfo
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public Dictionary<string, string[]> Errors { get; set; } = [];

        public HttpStatusCodeInfo() { }

        public HttpStatusCodeInfo(int statusCode, string message = null, string detail = null)
        {
            StatusCode = statusCode;
            Message = message;
            Detail = detail;
        }
    }
}
