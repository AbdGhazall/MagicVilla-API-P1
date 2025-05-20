using static MagicVilla_Utility.SD;

namespace MagicVilla_Web.Models
{
    public class APIRequest
    {
        public ApiType ApiType { get; set; } = ApiType.GET; // http type
        public string Url { get; set; } // api url
        public object Data { get; set; } = null; // data to be sent to the api

        public string Token { get; set; }
    }
}