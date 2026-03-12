var builder = DistributedApplication.CreateBuilder(args);

// The hosted agent — runs on port 8088
var agent = builder.AddProject<Projects.DataCrunchAgent>("datacrunchagent");

// The Blazor frontend — gets a reference to the agent for service discovery
builder.AddProject<Projects.DataCrunch_Web>("datacrunchweb")
    .WithReference(agent)
    .WithExternalHttpEndpoints();

builder.Build().Run();
