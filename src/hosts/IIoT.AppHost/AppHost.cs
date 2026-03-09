var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.IIoT_HttpApi>("iiot-httpapi");

builder.Build().Run();
