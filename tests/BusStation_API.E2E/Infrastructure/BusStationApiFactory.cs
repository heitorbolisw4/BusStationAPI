using BusStation_API.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BusStation_API.E2E.Infrastructure
{
    /// <summary>
    /// Sobe a API inteira em memória (pipeline real: auth, rotas, EF Core) apontando
    /// para um banco Postgres DEDICADO aos testes, recriado do zero a cada execução
    /// a partir das migrations — o banco de desenvolvimento nunca é tocado.
    ///
    /// Connection string: variável BUSSTATION_E2E_CONNECTION (pensado para o CI) ou,
    /// se ausente, a DefaultConnection dos user-secrets da API com o nome do banco
    /// trocado para "{nome}_e2e".
    /// </summary>
    public class BusStationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public string ConnectionString { get; }

        public BusStationApiFactory()
        {
            ConnectionString = ResolveConnectionString();

            // Program.cs lê a connection string direto de builder.Configuration durante
            // o bootstrap; variável de ambiente é a forma garantida de ela já estar lá.
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        }

        public async Task InitializeAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

        private static string ResolveConnectionString()
        {
            var fromEnv = Environment.GetEnvironmentVariable("BUSSTATION_E2E_CONNECTION");
            if(!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv;

            var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
            var baseConnection = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Sem connection string para os testes E2E: defina BUSSTATION_E2E_CONNECTION " +
                    "ou configure ConnectionStrings:DefaultConnection nos user-secrets da API.");

            var builder = new NpgsqlConnectionStringBuilder(baseConnection);
            builder.Database = $"{builder.Database}_e2e";
            return builder.ConnectionString;
        }
    }

    // Todas as classes de teste compartilham UMA instância da API e do banco, e rodam
    // em sequência (mesma collection). Isolamento entre testes vem de dados únicos
    // (nomes/datas gerados por teste), não de limpar o banco entre eles.
    [CollectionDefinition(Name)]
    public class E2ECollection : ICollectionFixture<BusStationApiFactory>
    {
        public const string Name = "e2e";
    }
}
