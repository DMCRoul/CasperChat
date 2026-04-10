using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", () => "CasperChat server is running");

app.MapGet("/version", () =>
{
    return Results.Ok(new
    {
        LatestVersion = "1.0.1",
        MinimumSupportedVersion = "1.0.0",
        DownloadUrl = "https://github.com/DMCRoul/CasperChat/releases",
        ReleaseNotes = "Исправления и улучшения клиента."
    });
});

app.MapPost("/upload", async (HttpRequest request) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Invalid form");

    var form = await request.ReadFormAsync();

    var file = form.Files.FirstOrDefault();

    if (file == null || file.Length == 0)
        return Results.BadRequest("File not found");

    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    if (!Directory.Exists(uploadsFolder))
        Directory.CreateDirectory(uploadsFolder);

    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
    var filePath = Path.Combine(uploadsFolder, fileName);

    using (var stream = File.Create(filePath))
    {
        await file.CopyToAsync(stream);
    }

    var fileUrl = $"/uploads/{fileName}";

    return Results.Ok(new
    {
        FileName = file.FileName,
        FileUrl = fileUrl
    });
});

app.MapGet("/", () => "Casper Chat Server Running");
app.Run();