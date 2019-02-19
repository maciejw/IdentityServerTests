using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace WebApi1.Tests
{
  public class UnitTest1
  {
    private readonly Xunit.Abstractions.ITestOutputHelper output;

    public UnitTest1(Xunit.Abstractions.ITestOutputHelper output)
    {
      this.output = output;
    }
    [Fact]
    public async Task use_authentication()
    {
      HttpClient client = GetClient();

      var disco = await client.GetDiscoveryDocumentAsync();

      var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
      {
        Address = disco.TokenEndpoint,
        ClientId = "client",
        ClientSecret = "secret",
        Scope = "api1"
      });

      Assert.NotEmpty(tokenResponse.AccessToken);

      client.SetBearerToken(tokenResponse.AccessToken);

      var response = await client.GetAsync("/api/values");

      Assert.True(response.IsSuccessStatusCode);

      var content = await response.Content.ReadAsStringAsync();
      Assert.Equal("[\"value1\",\"value2\"]", content);

    }
    [Fact]
    public async Task use_anonymous()
    {
      HttpClient client = GetClient();

      var response = await client.GetAsync("/api/values/5");

      Assert.True(response.IsSuccessStatusCode);

      var content = await response.Content.ReadAsStringAsync();
      Assert.Equal("value 5", content);
    }

    [Fact]
    public async Task use_unauthorized()
    {
      HttpClient client = GetClient();

      var response = await client.GetAsync("/api/values");

      Assert.False(response.IsSuccessStatusCode);

      Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpClient GetClient()
    {
      HttpMessageHandler testHandler = null;

      var testApp = new WebApplicationFactory<Startup>().WithWebHostBuilder(configuration =>
      {
        configuration.ConfigureServices(services =>
        {
          services.PostConfigureAll<IdentityServerAuthenticationOptions>(options =>
          {
            options.JwtBackChannelHandler = testHandler;
            options.Authority = "https://localhost";
          });

        });
      });

      var client = testApp.CreateClient();
      client.BaseAddress = new Uri("https://localhost/");

      testHandler = testApp.Server.CreateHandler();
      return client;
    }
  }

  public static class HttpContentExtensions
  {
    public static async Task<T> DeserializeJsonFromStream<T>(this HttpContent content)
    {
      Stream stream = await content.ReadAsStreamAsync();
      if (stream == null || stream.CanRead == false)
        return default(T);

      using (var streamReader = new StreamReader(stream))
      using (var textReader = new JsonTextReader(streamReader))
      {
        var serializer = new JsonSerializer();
        return serializer.Deserialize<T>(textReader);
      }
    }
  }
}
