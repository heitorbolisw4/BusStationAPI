using System.Text;

namespace BusStation_API.Configuration
{
    /// <summary>
    /// Configuração que o deploy precisa: segredos obrigatórios (fail-fast), origens de
    /// CORS e porta. Tudo vem de IConfiguration: em produção, variável de ambiente
    /// (ex.: JwtSettings__User__SecretKey); em dev, user-secrets.
    /// </summary>
    public static class StartupConfig
    {
        public const string CorsPolicyName = "Frontend";
        public const string DevFrontendOrigin = "http://localhost:5173";

        // Chave de config -> nome da variável de ambiente correspondente (para a mensagem de erro).
        private static readonly (string Key, string EnvVar)[] RequiredSecrets =
        [
            ("ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection"),
            ("JwtSettings:User:SecretKey", "JwtSettings__User__SecretKey"),
            ("JwtSettings:Admin:SecretKey", "JwtSettings__Admin__SecretKey"),
        ];

        // HMAC-SHA256 exige chave de pelo menos 256 bits; menor que isso a geração do
        // token estoura em runtime (IDX10720), no primeiro login.
        private const int MinJwtKeyBytes = 32;

        /// <summary>
        /// Derruba o startup com mensagem clara se faltar configuração obrigatória, em vez
        /// de subir e responder 401/500 sem explicação na primeira request.
        /// </summary>
        public static void EnsureRequiredSettings(IConfiguration config, IHostEnvironment env)
        {
            var problems = new List<string>();

            foreach(var (key, envVar) in RequiredSecrets)
            {
                var value = config[key];
                if(string.IsNullOrWhiteSpace(value))
                    problems.Add($"{envVar} está vazia");
                else if(key.EndsWith(":SecretKey") && Encoding.UTF8.GetByteCount(value) < MinJwtKeyBytes)
                    problems.Add($"{envVar} precisa ter pelo menos {MinJwtKeyBytes} bytes (HMAC-SHA256)");
            }

            if(!env.IsDevelopment() && AllowedOrigins(config, env).Length == 0)
                problems.Add("Cors__AllowedOrigins está vazia (fora de Development não há origem padrão)");

            if(problems.Count > 0)
                throw new InvalidOperationException(
                    $"Configuração obrigatória ausente ou inválida (ambiente {env.EnvironmentName}):\n - " +
                    string.Join("\n - ", problems) +
                    "\nEm produção, defina como variável de ambiente; em dev, use `dotnet user-secrets set`.");
        }

        /// <summary>
        /// Origens liberadas no CORS. Aceita lista (Cors__AllowedOrigins__0, __1...) ou um
        /// valor só separado por vírgula (Cors__AllowedOrigins=https://a,https://b), que é
        /// mais prático de cadastrar no painel do provedor. Em Development, sem nada
        /// configurado, cai no Vite local.
        /// </summary>
        public static string[] AllowedOrigins(IConfiguration config, IHostEnvironment env)
        {
            var section = config.GetSection("Cors:AllowedOrigins");
            var raw = section.GetChildren().Select(c => c.Value).ToList();
            if(raw.Count == 0)
                raw.Add(section.Value);

            var origins = raw
                .SelectMany(v => (v ?? "").Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(o => o.TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if(origins.Length == 0 && env.IsDevelopment())
                return [DevFrontendOrigin];

            return origins;
        }

        /// <summary>
        /// Render (e outros PaaS) informam a porta em PORT. Sem ela, vale o padrão do
        /// ASP.NET Core (ASPNETCORE_HTTP_PORTS, 8080 na imagem aspnet). Nada fixo no código.
        /// </summary>
        public static void UsePortFromEnvironment(this ConfigureWebHostBuilder webHost, IConfiguration config)
        {
            var port = config["PORT"];
            if(string.IsNullOrWhiteSpace(port))
                return;

            if(!int.TryParse(port, out var number) || number is < 1 or > 65535)
                throw new InvalidOperationException($"PORT inválida: '{port}'.");

            webHost.UseUrls($"http://+:{number}");
        }
    }
}
