var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Dragon Placement API!");
app.MapGet("/dragons", () => "Return a list of dragons");

app.Run();
