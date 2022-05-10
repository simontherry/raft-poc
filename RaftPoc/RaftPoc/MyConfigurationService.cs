using DotNext.Net.Cluster;
using DotNext.Net.Cluster.Consensus.Raft;
using DotNext.Net.Cluster.Consensus.Raft.Http;
using DotNext.Net.Http;

using Grpc.Core;

using Raft.Poc.V1;

namespace RaftPoc
{
    public class MyConfigurationService : ConfigurationService.ConfigurationServiceBase
    {
        /// <inheritdoc />
        public override async Task<AddNodeReply> AddNode(AddNodeRequest request, ServerCallContext context)
        {
            var endPoint = new HttpEndPoint(request.Node.Hostname, request.Node.Port, request.Node.UseSsl);
            var uri = $"{endPoint.CreateUriBuilder().Uri}ping";

            try
            {
                // execute ping-pong request
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(uri, context.CancellationToken);
                response.EnsureSuccessStatusCode();
                var reply = await response.Content.ReadAsStringAsync(context.CancellationToken);
                if (reply != "pong")
                {
                    throw new RpcException(new Status(StatusCode.Internal, $"Could not contact {uri}"));
                }
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Could not contact {uri}", e));
            }

            try
            {
                var raftCluster = context.GetHttpContext().RequestServices.GetRequiredService<IRaftHttpCluster>();
                var success = await raftCluster.AddMemberAsync(ClusterMemberId.FromEndPoint(endPoint), endPoint, context.CancellationToken);
                return new AddNodeReply { Success = success };
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Failed to add node {uri}: {e.Message}", e));
            }
        }

        /// <inheritdoc />
        public override async Task<RemoveNodeReply> RemoveNode(RemoveNodeRequest request, ServerCallContext context)
        {
            var endPoint = new HttpEndPoint(request.Node.Hostname, request.Node.Port, request.Node.UseSsl);
            var uri = $"{endPoint.CreateUriBuilder().Uri}";
            var cluster = context.GetHttpContext().RequestServices.GetRequiredService<IRaftCluster>();
            if (!cluster.Members.Any(x => Equals(x.EndPoint, endPoint)))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"{uri} is not part of the cluster"));
            }

            try
            {
                var raftCluster = context.GetHttpContext().RequestServices.GetRequiredService<IRaftHttpCluster>();
                var success = await raftCluster.RemoveMemberAsync(ClusterMemberId.FromEndPoint(endPoint), context.CancellationToken);
                return new RemoveNodeReply { Success = success };
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Failed to remove node {uri}: {e.Message}", e));
            }
        }
    }
}