using System.ComponentModel;
using Ecommerce.Api.IntegrationTests.Startup;

using Respawn;
using Respawn.Graph;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ecommerce.Contracts.Requests;
using Ecommerce.Contracts;
using Ecommerce.Contracts.Responses;

namespace Ecommerce.Api.IntegrationTests;

public class BaseIntegrationTest : IAsyncLifetime
{
    readonly LoginRequest defaultUser = new("default@gmail.com", "password.123");

    readonly LoginRequest adminUser = new("admin@gmail.com", "password.123");

    public HttpClient HttpClient = null!;

    public HttpClient AdminUserHttpClient = null!;

    public HttpClient DefaultUserHttpClient = null!;

    internal EcommerceProgram EcommerceProgram = null!;

    private IConfiguration _configuration = null!;

    private Respawner _respawner = null!;
    private Respawner _respawnerHangFire = null!;

    public async Task InitializeAsync()
    {
        EcommerceProgram = new EcommerceProgram();

        _configuration = EcommerceProgram.Services.GetRequiredService<IConfiguration>();

        HttpClient = EcommerceProgram.CreateDefaultClient();

        AdminUserHttpClient = await CreateCustomHttpClient(EcommerceProgram, HttpClient, adminUser);

        DefaultUserHttpClient = await CreateCustomHttpClient(EcommerceProgram, HttpClient, defaultUser);

        _respawner = await Respawner.CreateAsync(_configuration.GetConnectionString("TestConnection")!, new RespawnerOptions{
            TablesToIgnore = new Table[]
            {
                "AspNetRoleClaims",
                "AspNetUserClaims",
                "AspNetUserLogins",
                "AspNetUserTokens",
                "__EFMigrationsHistory"
            }
        });
        
        _respawnerHangFire = await Respawner.CreateAsync(_configuration.GetConnectionString("HangfireConnection")!);
    }

    private static async Task<HttpClient> CreateCustomHttpClient(EcommerceProgram program, HttpClient httpClient, LoginRequest user)
    {
        var httpResponse = await httpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, user);
        
        var httpResponseReadedAsString = await httpResponse.Content.ReadAsStringAsync();
        
        var authenticateResponse = JsonConvert.DeserializeObject<AuthenticateResponse>(httpResponseReadedAsString);

        var customHttpClient = program.CreateDefaultClient();
        
        customHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authenticateResponse.AccessToken);
        
        return customHttpClient;
    }

    public async Task DisposeAsync()
    {
        var task1 = _respawner.ResetAsync(_configuration.GetConnectionString("TestConnection")!);
        var task2 = _respawnerHangFire.ResetAsync(_configuration.GetConnectionString("HangfireConnection")!);

        await Task.WhenAll(task1, task2);
    }
}