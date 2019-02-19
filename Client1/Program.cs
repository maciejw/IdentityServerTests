using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;

namespace Client1
{
  class Program
  {
    static async Task Main(string[] args)
    {
      using (var handler = new HttpClientHandler())
      {
        // handler.ClientCertificateOptions = ClientCertificateOption.Manual;

        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        var client = new HttpClient(handler);

        var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
        if (disco.IsError)
        {
          Console.WriteLine(disco.Error);
          return;
        }


        var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
          Address = disco.TokenEndpoint,
          ClientId = "client",
          ClientSecret = "secret",

          Scope = "api1"
        });

        if (tokenResponse.IsError)
        {
          Console.WriteLine(tokenResponse.Error);
          return;
        }

        Console.WriteLine(tokenResponse.Json);

        var apiClient = new HttpClient(handler);
        apiClient.SetBearerToken(tokenResponse.AccessToken);

        var response = await apiClient.GetAsync("https://localhost:5001/api/values");
        if (!response.IsSuccessStatusCode)
        {
          Console.WriteLine(response.StatusCode);
        }
        else
        {
          var content = await response.Content.ReadAsStringAsync();
          Console.WriteLine(JArray.Parse(content));
        }

      }

    }
  }
}
