using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Text;

namespace WindowsCredentialProviderTest.Internet
{
    public sealed class InternetConnect
    {
        readonly Ping ping;
        readonly HttpClient client;
        readonly string serverName;
        readonly string channel;
        public InternetConnect()
        {
            ping = new Ping();
            client = new HttpClient();
            serverName = "https://si.toppanidgate.com/iDenKeyFidoKlGW";
            channel = "WindowsCP";
            // magic code to fix TLS/SSL issue
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
        }
        public async Task<bool> CheckInternetConnection()
        {
            try
            {
                // checks internet connection by pining google DNS server 8.8.8.8
                string hostToPing = "8.8.8.8";

                PingReply reply = await ping.SendPingAsync(hostToPing);

                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }

        }

        public async Task<QRCodeAPIResponse> FetchQRCode(string userSid)
        {
            try
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                string apiUrl = "https://si.toppanidgate.com/iDenKeyFidoKlGW/GetRegQR";
                string requestBody = $"{{\"txdatetime\": \"{formattedDateTime}\", " +
                    $"\"id\": \"{userSid}\", " +
                    $"\"channel\": \"{channel}\"}}";
                string result = await FetchAPIPost(apiUrl, requestBody);

                QRCodeAPIResponse qRCodeAPIResponse = JsonConvert.DeserializeObject<QRCodeAPIResponse>(result);
                if (qRCodeAPIResponse == null || qRCodeAPIResponse.status == null 
                    || qRCodeAPIResponse.msg == null || qRCodeAPIResponse.qrcode== null)
                {
                    throw new Exception("is Null");
                }

                return qRCodeAPIResponse;
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request-related exceptions
                QRCodeAPIResponse errorResponse = (QRCodeAPIResponse)APIResponse.Convert(typeof(QRCodeAPIResponse),
                    APIResponse.ErrorResponse($"FetchQRCode HTTP request failed: {ex.Message}"));
                return errorResponse;
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                QRCodeAPIResponse errorResponse = (QRCodeAPIResponse)APIResponse.Convert(typeof(QRCodeAPIResponse),
                    APIResponse.ErrorResponse($"FetchQRCode An unexpected error occurred: {ex.Message}"));
                return errorResponse;
            }
        }

        public async Task<CheckQRResponse> FetchCheckQR(string userSid)
        {
            try
            {
                string apiUrl = serverName + "/CheckRegStatus";
                string requestBody = $"{{ \"id\": \"{userSid}\", " +
                    $"\"channel\": \"{channel}\"}}";
                string result = await FetchAPIPost(apiUrl, requestBody);

                CheckQRResponse checkQRResponse = JsonConvert.DeserializeObject<CheckQRResponse>(result);
                if (checkQRResponse == null || checkQRResponse.status == null 
                    || checkQRResponse.msg == null || checkQRResponse.idgateid == null)
                {
                    throw new Exception("is Null");
                }
                return checkQRResponse;
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request-related exceptions
                CheckQRResponse errorResponse = (CheckQRResponse)APIResponse.Convert(typeof(CheckQRResponse),
                    APIResponse.ErrorResponse($"FetchCheckQR HTTP request failed: {ex.Message}"));
                return errorResponse;
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                CheckQRResponse errorResponse = (CheckQRResponse)APIResponse.Convert(typeof(CheckQRResponse),
                    APIResponse.ErrorResponse($"FetchCheckQR An unexpected error occurred: {ex.Message}"));
                return errorResponse;
            }
        }

        public async Task<CreateTxnAPIResponse> FetchCreateTxn(string userSid, Action<string> setStatusText)
        {
            try
            {
                string apiUrl = serverName + "/CreateTxn";
                string requestBody = $"{{ \"id\": \"{userSid}\", " +
                    $"\"channel\": \"{channel}\"}}";
                string result = await FetchAPIPost(apiUrl, requestBody, setStatusText);

                //CreateTxnAPIResponse createTxnAPIResponse = JsonConvert.DeserializeObject<CreateTxnAPIResponse>(result);
                //if (createTxnAPIResponse == null || createTxnAPIResponse.status == null 
                //    || createTxnAPIResponse.msg == null || createTxnAPIResponse.txnid == null)
                //{
                //    throw new Exception("is Null");
                //}
                CreateTxnAPIResponse createTxnAPIResponse = new CreateTxnAPIResponse();
                createTxnAPIResponse.status = "0000";
                createTxnAPIResponse.msg = "Hello World";
                return createTxnAPIResponse;
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request-related exceptions
                CreateTxnAPIResponse errorResponse = (CreateTxnAPIResponse)APIResponse.Convert(typeof(CreateTxnAPIResponse),
                    APIResponse.ErrorResponse($"FetchCreateTxn HTTP request failed: {ex.Message}"));
                return errorResponse;
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                CreateTxnAPIResponse errorResponse = (CreateTxnAPIResponse)APIResponse.Convert(typeof(CreateTxnAPIResponse),
                    APIResponse.ErrorResponse($"FetchCreateTxn An unexpected error occurred: {ex.Message}"));
                return errorResponse;
            }
        }

        public async Task<APIResponse> FetchCheckTxn(string userSid, string txnid)
        {
            try
            {
                string apiUrl = serverName + "/CheckTxnStatus";
                string requestBody = $"{{ \"id\": \"{userSid}\", " +
                    $"\"txnid\": \"{txnid}\", " +
                    $"\"channel\": \"{channel}\"}}";
                string result = await FetchAPIPost(apiUrl, requestBody);

                APIResponse aPIResponse = JsonConvert.DeserializeObject<APIResponse>(result);
                if (aPIResponse == null || aPIResponse.status == null || aPIResponse.msg == null)
                {
                    throw new Exception("is Null");
                }
                return aPIResponse;
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request-related exceptions
                APIResponse errorResponse = APIResponse.ErrorResponse($"FetchCheckTxn HTTP request failed: {ex.Message}");
                return errorResponse;
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                APIResponse errorResponse = APIResponse.ErrorResponse($"FetchCheckTxn An unexpected error occurred: {ex.Message}");
                return errorResponse;
            }
        }

        private async Task<string> FetchAPIPost(string apiUrl, string requestBody)
        {
            try
            {
                // Content to be sent in the request
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // Sending POST request
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResult = await response.Content.ReadAsStringAsync();
                    return jsonResult;
                }
                else
                {
                    throw new HttpRequestException($"Failed to post data. Status code: {response.StatusCode}");
                }
            }
            catch (Exception)
            {
                // Log the exception or perform additional handling if needed
                throw; // Re-throw the exception for the caller to handle
            }
        }

        private async Task<string> FetchAPIPost(string apiUrl, string requestBody, Action<string> setStatusText)
        {
            try
            {
                // Content to be sent in the request
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // Sending POST request
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResult = await response.Content.ReadAsStringAsync();
                    return jsonResult;
                }
                else
                {
                    throw new HttpRequestException($"Failed to post data. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log the exception or perform additional handling if needed
                throw ex; // Re-throw the exception for the caller to handle
            }
        }
    }
}
