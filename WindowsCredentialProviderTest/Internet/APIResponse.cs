using Newtonsoft.Json;

namespace WindowsCredentialProviderTest.Internet
{
    public class APIResponse
    {
        [JsonProperty("status")]
        internal string status { get; set; }

        [JsonProperty("msg")]
        internal string msg
        {
            get; set;
        }
        public static APIResponse ErrorResponse(string msg)
        {
            APIResponse response = new APIResponse();
            response.status = "E7000";
            response.msg = msg;
            return response;
        }
    }
    public class QRCodeAPIResponse : APIResponse
    {
        [JsonProperty("qrcode")]
        internal string qrcode { get; set; }
    }
    public class CheckQRResponse : APIResponse
    {
        [JsonProperty("idgateid")]
        internal string idgateid { get; set; }
    }

    public class CreateTxnAPIResponse : APIResponse
    {
        [JsonProperty("txind")]
        internal string txnid { get; set; }
    }

}
