using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var migrationsAssembly = typeof(Program).GetTypeInfo().Assembly.GetName().Name;

builder.Services.AddIdentityServer()
    .AddConfigurationStore(options =>
    {
        options.ConfigureDbContext = b => b.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(migrationsAssembly));
    })
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = b => b.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(migrationsAssembly));

        // this enables automatic token cleanup. this is optional.
        options.EnableTokenCleanup = true;
        options.TokenCleanupInterval = 30; // interval in seconds
    }).AddDeveloperSigningCredential();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    var configurationDbContext = serviceProvider.GetRequiredService<ConfigurationDbContext>();
    
    // if there are no clients or resources, add them
    if (!configurationDbContext.Clients.Any())
    {

        var clientId = app.Configuration["DefaultClientId"];
        var clientSecret = app.Configuration["DefaultClientSecret"];

        var client = new Client()
        {
            ClientId = clientId,
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret(clientSecret.Sha256()) },
            AllowedScopes = { "video_api" }
        };

        configurationDbContext.Clients.Add(client.ToEntity());

        configurationDbContext.SaveChanges();
    }

    if (!configurationDbContext.IdentityResources.Any())
    {
        foreach (var resource in Config.GetIdentityResources())
        {
            configurationDbContext.IdentityResources.Add(resource.ToEntity());
        }
        configurationDbContext.SaveChanges();
    }

    if (!configurationDbContext.ApiScopes.Any())
    {
        foreach (var apiScope in Config.GetApiScopes())
        {
            configurationDbContext.ApiScopes.Add(apiScope.ToEntity());
        }
        configurationDbContext.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseIdentityServer();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
