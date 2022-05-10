using DotNext.Net.Cluster.Consensus.Raft;

using RaftPoc.Commands;

namespace RaftPoc
{
    public interface IDataEngine
    {
        Task<bool> ConfigureIdAsync(long id, CancellationToken token);

        Task<long> GetIdAsync(CancellationToken token);
    }

    internal class PersistentEngineState : MemoryBasedStateMachine, IDataEngine
    {
        public const string LogLocation = "logLocation";

        private readonly DataEngine inner;

        private long id;

        public PersistentEngineState(IConfiguration configuration)
            : this(configuration[LogLocation], 50)
        {
        }

        /// <inheritdoc />
        public PersistentEngineState(string path, int recordsPerPartition, Options? configuration = null)
            : base(path, recordsPerPartition, configuration)
        {
            this.inner = new DataEngine();
        }

        /// <inheritdoc />
        public Task<bool> ConfigureIdAsync(long id, CancellationToken token)
        {
            return this.inner.ConfigureIdAsync(id, token);
        }

        /// <inheritdoc />
        public Task<long> GetIdAsync(CancellationToken token)
        {
            return this.inner.GetIdAsync(token);
        }

        /// <inheritdoc />
        protected override async ValueTask ApplyAsync(LogEntry entry)
        {
            if (entry.Length == 0L)
            {
                return;
            }

            var interpreter = new CustomCommandInterpreter();
            var commandId = await interpreter.InterpretAsync(entry);
            switch (commandId)
            {
                case ConfigureIdCommand.CommandId:
                    {
                        this.id = interpreter.Id;
                        await this.inner.ConfigureIdAsync(interpreter.Id, CancellationToken.None);
                        break;
                    }

                case SnapshotCommand.CommandId:
                    {
                        this.id = interpreter.Id;
                        break;
                    }

                default:
                    {
                        Console.WriteLine("Unknown command");
                        break;
                    }
            }
        }

        /// <inheritdoc />
        protected override SnapshotBuilder CreateSnapshotBuilder(in SnapshotBuilderContext context)
        {
            return new IdStateSnapshotBuilder(context);
        }

        private sealed class IdStateSnapshotBuilder : IncrementalSnapshotBuilder
        {
            private readonly CustomCommandInterpreter commandInterpreter;

            private long value;

            public IdStateSnapshotBuilder(in SnapshotBuilderContext context)
                : base(context)
            {
                this.commandInterpreter = new CustomCommandInterpreter();
            }

            public override ValueTask WriteToAsync<TWriter>(TWriter writer, CancellationToken token)
            {
                return writer.WriteInt64Async(this.value, true, token);
            }

            protected override async ValueTask ApplyAsync(LogEntry entry)
            {
                await this.commandInterpreter.InterpretAsync(entry);
                this.value = this.commandInterpreter.Id;
            }
        }
    }
}