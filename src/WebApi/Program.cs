using Vonage_SSW_Workshop.Application;
using Vonage_SSW_Workshop.Infrastructure;
using Vonage_SSW_Workshop.WebApi;
using Vonage_SSW_Workshop.WebApi.Endpoints;
using Vonage_SSW_Workshop.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddCustomProblemDetails();

builder.Services.AddWebApi();
builder.Services.AddApplication();
builder.AddInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapOpenApi();
app.MapCustomScalarApiReference();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapCallEndpoints();

app.MapDefaultEndpoints();
app.UseExceptionHandler();

app.Run();