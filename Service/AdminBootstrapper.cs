using BusStation_API.Data;
using BusStation_API.Entities;
using BusStation_API.Interface;
using Microsoft.EntityFrameworkCore;

namespace BusStation_API.Service
{
    /// <summary>
    /// Cria o PRIMEIRO admin a partir de BOOTSTRAP_ADMIN_EMAIL / BOOTSTRAP_ADMIN_PASSWORD.
    /// Como POST /admin/create exige AdminPolicy, sem isto ninguém conseguiria criar o
    /// primeiro. Não faz nada se as duas variáveis não estiverem definidas ou se já
    /// existir qualquer admin, então pode rodar a cada boot sem efeito colateral.
    /// A senha fica só na variável de ambiente, nunca em migration nem no repositório.
    /// </summary>
    public class AdminBootstrapper
    {
        public const string EmailKey = "BOOTSTRAP_ADMIN_EMAIL";
        public const string PasswordKey = "BOOTSTRAP_ADMIN_PASSWORD";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AdminBootstrapper> _logger;

        public AdminBootstrapper(IServiceScopeFactory scopeFactory, ILogger<AdminBootstrapper> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <returns>true se criou o admin; false se não havia o que fazer.</returns>
        public async Task<bool> SeedFirstAdminAsync(string? email, string? password, CancellationToken ct = default)
        {
            if(string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return false;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if(await db.Admins.AnyAsync(ct))
                return false;

            var hash = scope.ServiceProvider.GetRequiredService<IAuthService>().GenerateHash(password);
            db.Admins.Add(new Admin { Name = "Bootstrap Admin", Email = email, Password = hash });
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Primeiro admin criado a partir de {EmailKey}.", EmailKey);
            return true;
        }
    }

    /// <summary>
    /// Roda o <see cref="AdminBootstrapper"/> no startup. Falha de banco (ainda sem
    /// migration, fora do ar) vira log de erro e NÃO derruba a API: as migrations rodam
    /// num passo separado (efbundle, CHORE-023), e o seed é tentado de novo no próximo boot.
    /// </summary>
    public class AdminBootstrapHostedService : IHostedService
    {
        private readonly AdminBootstrapper _bootstrapper;
        private readonly IConfiguration _config;
        private readonly ILogger<AdminBootstrapHostedService> _logger;

        public AdminBootstrapHostedService(AdminBootstrapper bootstrapper, IConfiguration config, ILogger<AdminBootstrapHostedService> logger)
        {
            _bootstrapper = bootstrapper;
            _config = config;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var email = _config[AdminBootstrapper.EmailKey];
            var password = _config[AdminBootstrapper.PasswordKey];
            if(string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            try
            {
                await _bootstrapper.SeedFirstAdminAsync(email, password, cancellationToken);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,
                    "Não foi possível criar o primeiro admin ({EmailKey}). As migrations já foram aplicadas? " +
                    "A API segue no ar e tenta de novo no próximo boot.", AdminBootstrapper.EmailKey);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
