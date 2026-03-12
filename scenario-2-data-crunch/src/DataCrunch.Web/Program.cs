using DataCrunch.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure agent HTTP client — base address set by Aspire service discovery or config
var agentMode = builder.Configuration["AgentMode"] ?? "Local";
var agentUrl = builder.Configuration[$"AgentEndpoints:{agentMode}"]
    ?? "https+http://datacrunchagent";

builder.Services.AddHttpClient<AgentService>(client =>
{
    client.BaseAddress = new Uri(agentUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});

var app = builder.Build();
app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<DataCrunch.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
