using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace WindowsCredentialProviderTest.Internet
{
    public sealed class InternetConnect
    {
        readonly Ping ping;
        readonly HttpClient client;
        public InternetConnect()
        {
            ping = new Ping();
            client = new HttpClient();
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
            return "???";
            string apiUrl = "https://jsonplaceholder.typicode.com/todos/1";

            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();
                Todo todo = JsonSerializer.Deserialize<Todo>(jsonResult);
                return "HELLO";
                return todo.Title + " " + todo.Completed;
            }
            return "";
            return null;
        }
    }


}
