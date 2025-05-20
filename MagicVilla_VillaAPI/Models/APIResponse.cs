using System.Net;

namespace MagicVilla_VillaAPI.Models
{
    public class APIResponse // the standaerd API response for all endpoints
    {
        public APIResponse()
        {
            ErrorMessages = new List<string>();
        }

        public HttpStatusCode StatusCode { get; set; } // the status code of the response
        public bool IsSuccess { get; set; } = true; // the success status of the response is true by default
        public List<string> ErrorMessages { get; set; } // the message of the response
        public object Result { get; set; } // the result of the response (can be any type)
    }
}