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
        public InternetConnect()
        {
            ping = new Ping();
            client = new HttpClient();
            // magic code to fix TLS/SSL issue
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
        }
        public async Task<bool> CheckInternetConnection()
        {
            // checks internet connection by pining google DNS server 8.8.8.8
            string hostToPing = "8.8.8.8";

            PingReply reply = await ping.SendPingAsync(hostToPing);

            return (reply.Status == IPStatus.Success);
        }


        public async Task<string> FetchAPIGetTest()
        {
            string apiUrl = "https://jsonplaceholder.typicode.com/todos/1";
            string jsonResult = await client.GetStringAsync(apiUrl);
            try
            {
                Todo todo = JsonConvert.DeserializeObject<Todo>(jsonResult);
                return todo.Title + " " + todo.Completed;
            } catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public async Task<string> FetchAPIPostTest()
        {
            try
            {
                string apiUrl = "https://jsonplaceholder.typicode.com/posts"; // Example API endpoint
                string requestBody = "{\"title\": \"foo\", \"body\": \"bar\", \"userId\": 1}"; // Example JSON request body

                string result = await FetchAPIPost(apiUrl, requestBody);

                return result;
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request-related exceptions
                return $"HTTP request failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                return $"An unexpected error occurred: {ex.Message}";
            }

        }


        private async Task<string> FetchAPIGet(string apiUrl)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResult = await response.Content.ReadAsStringAsync();
                    return jsonResult;
                }
                else
                {
                    throw new HttpRequestException($"Failed to fetch data. Status code: {response.StatusCode}");
                }
            }
            catch (Exception)
            {
                // Log the exception or perform additional handling if needed
                throw; // Re-throw the exception for the caller to handle
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


    }


}
