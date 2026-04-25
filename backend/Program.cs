using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SongAppApi.Authorization;
using SongAppApi.Helpers;
using SongAppApi.Services;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Http.Features;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// add services to DI container
{
    var services = builder.Services;
    var env = builder.Environment;

    services.AddDbContext<DataContext>();
    services.AddCors();
    services.AddControllers().AddJsonOptions(x =>
    {
        // serialize enums as strings in api responses (e.g. Role)
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile));
    services.AddSwaggerGen();

    // configure strongly typed settings object
    services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("FileUpload"));

    // file upload config
    var uploadConfig = builder.Configuration.GetSection("FileUpload").Get<FileUploadSettings>()
    ?? throw new InvalidOperationException("FileUpload section missing from configuration.");

    var maxCategoryBytes = new[]
    {
        uploadConfig.Image.MaxSizeBytes,
        uploadConfig.Audio.MaxSizeBytes,
        uploadConfig.Video.MaxSizeBytes
    }.Max();

    var transportLimit = maxCategoryBytes + (5L * 1024 * 1024); // +5 MB headroom

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = transportLimit;
        options.ValueLengthLimit = int.MaxValue;
        options.MultipartHeadersLengthLimit = int.MaxValue;
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = transportLimit;
    });


    // configure DI for application services
    services.AddScoped<IJwtUtils, JwtUtils>();
    services.AddScoped<IAccountService, AccountService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IFileService, FileService>();
    services.AddScoped<ISongService, SongService>();
    services.AddScoped<IPlaylistService, PlaylistService>();
}

var app = builder.Build();

// migrate any database changes on startup (includes initial db creation)
using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dataContext.Database.Migrate();
}

// configure HTTP request pipeline
{
    // generated swagger json and swagger ui middleware
    app.UseSwagger();
    app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET Sign-up and Verification API"));

    // global cors policy
    app.UseCors(x => x
        .SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

    // global error handler
    app.UseMiddleware<ErrorHandlerMiddleware>();

    // custom jwt auth middleware
    app.UseMiddleware<JwtMiddleware>();

    app.MapControllers();
}

app.Run("http://localhost:4000");