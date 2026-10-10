namespace Mype.Tests.Integration
{
    [CollectionDefinition(Name)]
    public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
    {
        public const string Name = "PostgreSQL integration";
    }
}
