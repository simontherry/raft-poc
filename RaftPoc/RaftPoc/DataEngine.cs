namespace RaftPoc
{
    internal class DataEngine : IDataEngine
    {
        private long? value;

        /// <inheritdoc />
        public Task<bool> ConfigureIdAsync(long id, CancellationToken token)
        {
            this.value = id;
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public Task<long> GetIdAsync(CancellationToken token)
        {
            if (this.value.HasValue)
            {
                return Task.FromResult(this.value.Value);
            }

            throw new InvalidOperationException("Id has not been set yet.");
        }
    }
}