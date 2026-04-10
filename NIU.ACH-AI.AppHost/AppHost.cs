using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Load additional configuration from a secrets file
builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

var rabbitUserValue = builder.Configuration["RabbitMQ:User"] ?? "guest";
var rabbitPassValue = builder.Configuration["RabbitMQ:Password"] ?? "guest";

// Create the user parameters for RabbitMQ
var rabbitUserParam = builder.AddParameter("rabbit-user", rabbitUserValue);
var rabbitPassParam = builder.AddParameter("rabbit-pass", rabbitPassValue, secret: true);

// 1. Define the RabbitMQ Resource
// "messaging" is the name used to connect later in the application
// WithManagementPlugin() adds the web UI at localhost:15672
var rabbitMq = builder
    .AddRabbitMQ("messaging", userName: rabbitUserParam, password: rabbitPassParam)
    .WithManagementPlugin()
    .WithEndpoint("management", e => e.Port = 15672);

// 2. Register the application project
builder.AddProject<Projects.NIU_ACH_AI_FrontendConsole>("niu-ach-ai-frontendconsole")
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq);  // Ensure RabbitMQ is ready before starting the app

builder.Build().Run();
