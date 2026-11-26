using Npgsql;

namespace BalonmanoBunol.Infrastructure.Persistence;

public static class NeonConnectionStringValidator
{
    public static void EnsureDirectConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var host = new NpgsqlConnectionStringBuilder(connectionString).Host ?? string.Empty;
        var usesNeonPooler = false;
        foreach (var hostname in host.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var firstLabel = hostname.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (firstLabel is not null && firstLabel.EndsWith("-pooler", StringComparison.OrdinalIgnoreCase))
            {
                usesNeonPooler = true;
                break;
            }
        }
        if (usesNeonPooler)
            throw new InvalidOperationException("La inicialización requiere la conexión directa de Neon (host sin el sufijo '-pooler') porque utiliza un bloqueo PostgreSQL de sesión. Cambia la cadena agrupada por la conexión directa de Neon antes de iniciar la aplicación.");
    }
}
