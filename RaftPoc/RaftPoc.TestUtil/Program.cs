// See https://aka.ms/new-console-template for more information

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Configuration;

using Raft.Poc.V1;

async Task CreateClusterAsync(int portToConnect, int[] ports)
{
    var channel = GrpcChannel.ForAddress($"https://localhost:{portToConnect}/");
    var client = new ConfigurationService.ConfigurationServiceClient(channel);
    foreach (var other in ports)
    {
        Console.WriteLine($"Adding {other} to {portToConnect}");
        try
        {
            await client.AddNodeAsync(
                new AddNodeRequest { Node = new Node { Hostname = "localhost", Port = other, UseSsl = true }, },
                deadline: DateTime.UtcNow.AddSeconds(5));
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
        {
            Console.WriteLine("Adding member timed out.");
        }

        await Task.Delay(TimeSpan.FromSeconds(1));
        Console.WriteLine("Press to continue");
        Console.ReadLine();
    }
}

var appConfiguration = new ConfigurationBuilder().AddCommandLine(args).Build();

var createCluster = appConfiguration["createCluster"] != null;
var configureId = appConfiguration["configureId"] != null;
var getId = appConfiguration["getId"] != null;

const int Port = 5001;
if (createCluster)
{
    var ports = new[] { 5002, 5003 };
    await CreateClusterAsync(Port, ports).ConfigureAwait(false);
}

var channel = GrpcChannel.ForAddress($"https://localhost:{Port}/");
var client = new IdService.IdServiceClient(channel);

if (configureId)
{
    var id = Convert.ToInt64(appConfiguration["configureId"]);
    var reply = await client.ConfigureIdAsync(new ConfigureIdRequest() { Id = id });
    Console.WriteLine($"Configured id: {id} - {reply.Success}");
}

if (getId)
{
    var reply = await client.GetIdAsync(new GetIdRequest());
    Console.WriteLine($"Get id: {reply.Id}");
}

