using System.Text.Json.Serialization;
using DragonPlacementApi.Endpoints;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer;
using Microsoft.EntityFrameworkCore;
using TimekeepingDataLayer.Models;
using TimekeepingDataLayer;

const string allowedOriginsPolicy = "DragonPlacementAllowedOrigins";

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins")?.Split(",") ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(
        allowedOriginsPolicy,
        policy => policy.WithOrigins(allowedOrigins).AllowCredentials().AllowAnyHeader().AllowAnyMethod()
    )
);

var connectionString = builder.Configuration.GetConnectionString("DragonPlacementDb");
builder.Services
    .AddEntityFrameworkSqlite()
    .AddDbContext<DragonPlacementContext>(options => options.UseSqlite(connectionString))
    .AddDbContext<TimekeepingContext>(options => options.UseSqlite(connectionString));
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddLogging();
builder.Services.AddScoped<IDragonPlacementUnitOfWork, DragonPlacementUnitOfWork>();
builder.Services.AddScoped<ITimekeepingUnitOfWork, TimekeepingUnitOfWork>();

var app = builder.Build();
app.UseCors(allowedOriginsPolicy);
app.UseExceptionHandler(appBuilder => appBuilder.Run(async context => await Results.Problem().ExecuteAsync(context)));

app.MapGet("/", () => "Dragon Placement API!");

app.MapGet("/dragon", DragonEndpoints.GetDragons);
app.MapGet("/dragon/{dragonId}", DragonEndpoints.GetDragonAsync);
app.MapPost("/dragon", DragonEndpoints.CreateDragonAsync);
app.MapPut("/dragon/{dragonId}", DragonEndpoints.UpdateDragonAsync);
app.MapDelete("/dragon/{dragonId}", DragonEndpoints.DeleteDragonAsync);

app.MapGet("/job", JobEndpoints.GetJobs);
app.MapGet("/job/{jobId}", JobEndpoints.GetJob);
app.MapPost("/job", JobEndpoints.CreateJobAsync);
app.MapPut("/job/{jobId}", JobEndpoints.UpdateJobAsync);
app.MapDelete("/job/{jobId}", JobEndpoints.DeleteJobAsync);
app.MapGet("/job/{jobId}/assigned-dragon", JobEndpoints.GetAssignedDragons);
app.MapPost("/job/{jobId}/assigned-dragon/{dragonId}", JobEndpoints.AssignDragonToJobAsync);
app.MapDelete("/job/{jobId}/assigned-dragon/{dragonId}", JobEndpoints.UnassignDragonFromJobAsync);

app.MapGet("/skill-tag", SkillEndpoints.GetSkillTagsAsync);

app.MapGet("/dragon/{unusedId}/assignment/{assignmentId}/payperiod", PayPeriodEndpoints.GetPayPeriods);
app.MapGet("/v2/dragon/{unusedId}/assignment/{assignmentId}/payperiodcandidate", PayPeriodEndpoints.GetValidPayPeriods);
app.MapGet("/v2/payperiod/{payPeriodId}", PayPeriodEndpoints.GetPayPeriodAsync);
app.MapPost("/v2/payperiod", PayPeriodEndpoints.CreatePayPeriodAsync);
app.MapPut("/v2/payperiod/{payPeriodId}", PayPeriodEndpoints.UpdatePayPeriodAsync);
app.MapDelete("/payperiod/{payPeriodId}", PayPeriodEndpoints.DeletePayPeriodAsync);

app.Run();
