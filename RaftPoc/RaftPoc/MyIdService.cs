
using DotNext.Net.Cluster.Consensus.Raft;

using Grpc.Core;

using Raft.Poc.V1;

using RaftPoc.Commands;

namespace RaftPoc
{
    public class MyIdService : Raft.Poc.V1.IdService.IdServiceBase
    {
        private IDataEngine DataEngine { get; }

        public MyIdService(IDataEngine dataEngine)
        {
            this.DataEngine = dataEngine;
            this.CommandInterpreter = new CustomCommandInterpreter();
        }

        private CustomCommandInterpreter CommandInterpreter { get; }

        /// <inheritdoc />
        public override async Task<GetIdReply> GetId(GetIdRequest request, ServerCallContext context)
        {
            var cluster = context.GetHttpContext().RequestServices.GetRequiredService<IRaftCluster>();
            await cluster.Readiness;

            await cluster.ApplyReadBarrierAsync(context.CancellationToken);
            var id = await this.DataEngine.GetIdAsync(context.CancellationToken).ConfigureAwait(false);
            return new GetIdReply() { Id = id };
        }

        /// <inheritdoc />
        public override async Task<ConfigureIdReply> ConfigureId(ConfigureIdRequest request, ServerCallContext context)
        {
            try
            {
                var success = await this.DataEngine.ConfigureIdAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
                if (success)
                {
                    var cluster = context.GetHttpContext().RequestServices.GetRequiredService<IRaftCluster>();
                    var entry = this.CommandInterpreter.CreateLogEntry(new ConfigureIdCommand(request.Id), cluster.Term);

                    if (cluster.Members.Count > 1 && !cluster.LeadershipToken.IsCancellationRequested)
                    {
                        var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cluster.LeadershipToken);
                        var replicate = await cluster.ReplicateAsync(entry, linkedSource.Token).ConfigureAwait(false);
                        if (!replicate)
                        {
                            throw new InvalidOperationException("Failed to replicate table configuration across the orchestration cluster.");
                        }
                    }
                }

                return new ConfigureIdReply() { Success = success };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Could not configure table storage.", e));
            }

        }
    }
}