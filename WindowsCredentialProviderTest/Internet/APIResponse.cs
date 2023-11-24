using Newtonsoft.Json;
using System;

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

        public static APIResponse Convert(Type targetType, APIResponse response)
        {
            if (targetType == typeof(QRCodeAPIResponse))
            {
                QRCodeAPIResponse newResponse = new QRCodeAPIResponse();
                newResponse.status = response.status;
                newResponse.msg = response.msg;
                newResponse.qrcode = null;
                return newResponse;
            }
            else if (targetType == typeof(CheckQRResponse)) {
                CheckQRResponse newResponse = new CheckQRResponse();
                newResponse.status = response.status;
                newResponse.msg = response.msg;
                newResponse.idgateid = null;
                return newResponse;
            }
            else if (targetType == typeof(CreateTxnAPIResponse))
            {
                CreateTxnAPIResponse newResponse = new CreateTxnAPIResponse();
                newResponse.status = response.status;
                newResponse.msg = response.msg;
                newResponse.txnid = null;
                return newResponse;
            }
            else
            {
                return response;
            }

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
