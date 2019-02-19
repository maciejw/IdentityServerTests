using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using IdentityServer4.Configuration;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using System.Net.Http;
using IdentityServer4.AccessTokenValidation;

namespace WebApi1
{
  internal static class TestCert
  {
    public static X509Certificate2 Load()
    {
      var cert = GetCertPath();
      return new X509Certificate2(cert, "idsrv3test");
    }

    public static string GetCertPath()
    {
      return Path.Combine(System.AppContext.BaseDirectory, "idsvrtest.pfx");
    }

    public static SigningCredentials LoadSigningCredentials()
    {
      var cert = Load();
      return new SigningCredentials(new X509SecurityKey(cert), "RS256");
    }
  }
  public static class Config
  {
    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
      return new IdentityResource[]
      {
        new IdentityResources.OpenId()
      };
    }

    public static IEnumerable<ApiResource> GetApis()
    {
      return new[]
      {
        new ApiResource("api1", "My API")
      };
    }

    public static IEnumerable<Client> GetClients()
    {
      return new[]
      {
        new Client
        {
          ClientId = "client",
          AllowedGrantTypes = GrantTypes.ClientCredentials,
          AccessTokenLifetime = 60,
          ClientSecrets =
          {
              new Secret("secret".Sha256())
          },
          AllowedScopes = { "api1" }
        }
      };
    }
  }



  public class Startup
  {
    private readonly IOptionsMonitor<StartupOptions> options;
    private readonly ILogger<Startup> logger;

    public Startup(IOptionsMonitor<StartupOptions> options, ILogger<Startup> logger)
    {
      this.options = options;
      this.logger = logger;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      IdentityModelEventSource.ShowPII = true;

      services
        .AddMvc()
        .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

      services
        .AddAuthentication(options =>
        {
          options.DefaultAuthenticateScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
          options.DefaultChallengeScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
          options.DefaultScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
        })
        .AddIdentityServerAuthentication(options =>
        {
          options.Authority = this.options.CurrentValue.Authority.Address;
          options.ApiName = "api1";
        });

      services
        .AddIdentityServer()
        .AddInMemoryApiResources(Config.GetApis())
        .AddInMemoryClients(Config.GetClients())
        .AddInMemoryIdentityResources(Config.GetIdentityResources())
        .AddSigningCredential(TestCert.LoadSigningCredentials());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      logger.LogInformation(new EventId(12345, "Startup"), "Starting application ${1}", new { app = "WebApi1" });

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseIdentityServer();

      app.UseHttpsRedirection();
      app.UseMvc();
    }
  }

  public class StartupOptions
  {
    public AuthorityConfiguration Authority { get; set; }
  }


  public class AuthorityConfiguration
  {
    public string Address { get; set; }
  }
}
