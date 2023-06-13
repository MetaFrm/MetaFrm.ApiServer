using MetaFrm;
using MetaFrm.ApiServer.Auth;
using MetaFrm.Maui.Devices;

try
{
    IConfigurationBuilder builder1 = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

    IConfigurationRoot configuration = builder1.Build();

    Factory.Init(GetValue(configuration, "MetaFrm.Factory.BaseAddress") ?? "", GetValue(configuration, "MetaFrm.Factory.AccessKey") ?? "", DevicePlatform.Server);
}
catch (Exception e)
{
    Console.WriteLine(e);
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition =
           System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Authorize.LoadToken();//저장된 토큰 불러오기

app.Run();


static string? GetValue(IConfigurationRoot configuration, string Path)
{
    IConfigurationSection? configurationSection;
    string[] vs;

    configurationSection = null;
    vs = Path.Split('.');

    foreach (string v in vs)
        if (configurationSection == null)
            configurationSection = configuration.GetSection(v);
        else
            configurationSection = configurationSection.GetSection(v);

    if (configurationSection == null)
        return "";
    else
        return configurationSection.Value;
}