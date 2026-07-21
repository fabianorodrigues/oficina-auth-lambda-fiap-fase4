namespace Oficina.Auth.Cpf;

public interface IAuthUserRepository
{
    Task<CadastroUserRow?> FindByCpfAsync(string cpf, CancellationToken cancellationToken);
}

