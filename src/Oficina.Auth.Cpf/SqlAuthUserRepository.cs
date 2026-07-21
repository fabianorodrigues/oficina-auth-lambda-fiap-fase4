using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Oficina.Auth.Cpf;

public sealed class SqlAuthUserRepository(string connectionString, ILogger<SqlAuthUserRepository> logger) : IAuthUserRepository
{
    private const int CommandTimeoutSeconds = 5;

    public async Task<CadastroUserRow?> FindByCpfAsync(string cpf, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Id,
                Cpf,
                Nome,
                Perfil,
                SenhaHash,
                Ativo
            FROM Funcionarios
            WHERE Cpf = @Cpf;
            """;

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
            command.Parameters.Add(new SqlParameter("@Cpf", System.Data.SqlDbType.NVarChar, 11) { Value = cpf });

            await using var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return new CadastroUserRow(
                reader.GetGuid(0).ToString("D"),
                reader.GetString(1),
                reader.GetString(2),
                MapRole(reader.GetInt32(3)),
                reader.GetString(4),
                reader.GetBoolean(5));
        }
        catch (Exception ex) when (ex is SqlException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "DatabaseQueryFailed");
            throw;
        }
    }

    private static string MapRole(int perfil) => perfil switch
    {
        1 => "Funcionario",
        2 => "Admin",
        _ => "Funcionario"
    };
}

