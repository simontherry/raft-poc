using DotNext.IO;
using DotNext.Runtime.Serialization;

namespace RaftPoc.Commands
{
    public struct ConfigureIdCommand : ISerializable<ConfigureIdCommand>
    {
        public long Id { get; }

        internal const int CommandId = 0;

        public ConfigureIdCommand(long id)
        {
            this.Id = id;
        }

        /// <inheritdoc />
        public long? Length => sizeof(long);
        
        public static async ValueTask<ConfigureIdCommand> ReadFromAsync<TReader>(TReader reader, CancellationToken token)
            where TReader : notnull, IAsyncBinaryReader
        {
            var id = await reader.ReadInt64Async(true, token);
            return new ConfigureIdCommand(id);
        }

        /// <inheritdoc />
        public ValueTask WriteToAsync<TWriter>(TWriter writer, CancellationToken token)
            where TWriter : IAsyncBinaryWriter
        {
            return writer.WriteInt64Async(this.Id, true, token);
        }
    }
}