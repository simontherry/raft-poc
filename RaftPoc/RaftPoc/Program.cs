using DotNext.Net.Cluster.Consensus.Raft;
using DotNext.Net.Cluster.Consensus.Raft.Http;

using RaftPoc;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).AddCommandLine(args).Build();

bool coldStart = config["coldStart"] != null;

var port = 5001;

if (config["urls"] != null)
{
    var uri = new Uri(config["urls"]);
    port = uri.Port;
}

var rootDir = Environment.CurrentDirectory;
var clusterConfigurationLocation = Path.Combine(rootDir, "config", $"{port}");
var logLocation = Path.Combine(rootDir, "log", $"{port}");

if (Directory.Exists(logLocation))
{
    Directory.Delete(logLocation, true); // delete old log at startup, everything gets streamed from other nodes. // TODO what if entire storage cluster was down?
}

var configuration = new Dictionary<string, string>
                        {
                            { "partitioning", "false" },
                            { "lowerElectionTimeout", "150" },
                            { "upperElectionTimeout", "300" },
                            { "requestTimeout", "00:10:00" },
                            { "publicEndPoint", $"https://localhost:{port}" },
                            { "coldStart", $"{coldStart}" },
                            { "requestJournal:memoryLimit", "5" },
                            { "requestJournal:expiration", "00:01:00" },
                            { "clusterConfigurationLocation", clusterConfigurationLocation },
                            { "logLocation", logLocation },
                        };

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddInMemoryCollection(configuration);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DataEngine>();
builder.Services.ConfigureCluster<ClusterChangedLogger>();
builder.Services.UsePersistentConfigurationStorage(configuration["clusterConfigurationLocation"]);
builder.Services.UsePersistenceEngine<IDataEngine, PersistentEngineState>();

builder.Services.AddGrpc();

builder.Services.AddLogging(
    builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Error);
            builder.AddFilter("Engine", LogLevel.Warning);
        });

builder.Services.AddOptions();

builder.JoinCluster();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

const string AddNodePath = "/raft.poc.v1.ConfigurationService/AddNode";
const string RemoveNodePath = "/raft.poc.v1.ConfigurationService/RemoveNode";
const string ConfigureIdPath = "/raft.poc.v1.IdService/ConfigureId";

app.UseHttpsRedirection();

app.UseConsensusProtocolHandler()
    .RedirectToLeader(AddNodePath)
    .RedirectToLeader(RemoveNodePath)
    .RedirectToLeader(ConfigureIdPath)
    .UseRouting()
    .UseEndpoints(builder =>
        {
            builder.MapGet("/ping", () => "pong");
            builder.MapGrpcService<MyIdService>();
            builder.MapGrpcService<MyConfigurationService>();
        });

app.Run();