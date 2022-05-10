using DotNext.Net.Cluster.Consensus.Raft.Commands;

namespace RaftPoc.Commands
{
    [Command<SnapshotCommand>(SnapshotCommand.CommandId)]
    [Command<ConfigureIdCommand>(ConfigureIdCommand.CommandId)]
    internal class CustomCommandInterpreter : CommandInterpreter
    {
        public long Id { get; private set; }

        [CommandHandler]
        public ValueTask ConfigureTableStorage(ConfigureIdCommand command, CancellationToken token)
        {
            this.Id = command.Id;
            return token.IsCancellationRequested ? new ValueTask(Task.FromCanceled(token)) : default;
        }

        [CommandHandler(IsSnapshotHandler = true)]
        public ValueTask ApplySnapshot(SnapshotCommand command, CancellationToken token)
        {
            this.Id = command.Id;
            return token.IsCancellationRequested ? new ValueTask(Task.FromCanceled(token)) : default;
        }
    }
}
