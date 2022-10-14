var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/config/appsettings.json", true)
    .AddEnvironmentVariables();

builder.Services.AddPromotionCore();

builder.Services
    .AddPromotionClientServices((options, _) =>
        options.Url = builder.Configuration.GetValue<Uri>("Promotion:PromotionServiceBaseUrl"))
    .AddPlayerServiceClient((options, _) =>
    options.BaseUrl = builder.Configuration.GetValue<Uri>("Promotion:PlayerServiceBaseUrl"))
    .AddVipLevelServiceClient(httpClientBuilder => httpClientBuilder
        .ConfigureHttpClient((sp, http) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            http.BaseAddress = config.GetValue<Uri>("Promotion:VipLevelServiceBaseUrl");
        }));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
