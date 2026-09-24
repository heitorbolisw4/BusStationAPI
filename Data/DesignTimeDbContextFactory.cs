using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusStation_API.Data
{
    /// <summary>
    /// Usado só pelas ferramentas do EF (dotnet ef migrations add / database update /
    /// migrations bundle). Sem isto o EF sobe o Program.cs inteiro para achar o
    /// DbContext, e o fail-fast de startup (chaves JWT, CORS) derrubaria o efbundle,
    /// que só precisa de uma connection string.
    ///
    /// Connection string, em ordem de precedência:
    /// 1. argumento --connection do efbundle;
    /// 2. MIGRATIONS_CONNECTION: a connection string DIRETA do Neon (sem -pooler). É o que
    ///    o Pre-Deploy Command do Railway usa (`./efbundle`, sem argumentos), porque
    ///    migration não deve passar pelo pooler;
    /// 3. ConnectionStrings__DefaultConnection do ambiente ou dos user-secrets.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Sem connection string aqui, o EF ainda aceita a que vier por --connection;
            // o placeholder só serve para montar as options (gerar migration não conecta).
            var connectionString = config["MIGRATIONS_CONNECTION"]
                ?? config.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=busstation";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new AppDbContext(options);
        }
    }
}
