using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace WindowsCredentialProviderTest.Internet
{
    public sealed class InternetConnect
    {
        readonly Ping ping;
        public InternetConnect()
        {
            ping = new Ping();
        }
        public async Task<bool> CheckInternetConnection()
        {
            // checks internet connection by pining google DNS server 8.8.8.8
            string hostToPing = "8.8.8.8";

            PingReply reply = await ping.SendPingAsync(hostToPing);

            return (reply.Status == IPStatus.Success);
        }

        public async Task<string> FetchAPI()
        {

            HttpClient client = new HttpClient();
            string apiUrl = "https://jsonplaceholder.typicode.com/todos/1";
            
            try
            {
                // magic code to fix TLS/SSL issue
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
               
                    string jsonResult = await response.Content.ReadAsStringAsync();
                    
                    Todo todo = JsonConvert.DeserializeObject<Todo>(jsonResult);

                    if (todo != null)
                    {
                        return todo.Title + " " + todo.Completed;
                    }
                    else
                    {
                        return jsonResult;
                    }
                }
                return "";
            } catch (Exception ex)
            {
                return ex.ToString();
            }

        }
    }


}
