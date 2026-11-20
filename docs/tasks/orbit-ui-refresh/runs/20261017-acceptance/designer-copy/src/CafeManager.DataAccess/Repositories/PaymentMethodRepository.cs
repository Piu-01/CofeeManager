using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Catalogs;

namespace CafeManager.DataAccess.Repositories;

public sealed class PaymentMethodRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor = new(factory);
    public Task<List<NameLookup>> SupportedAsync(AccountIdentity actor, PaymentPurpose purpose, CancellationToken token) =>
        executor.ReadAsync("sp_PaymentMethodListSupported", [.. CatalogRepository.Actor(actor),
            AuthRepository.Parameter("@Purpose", SqlDbType.VarChar, purpose.ToString(), 20)], r => new NameLookup(r.GetString(0).TrimEnd(), r.GetString(1)), token);
    public async Task<CatalogWriteResult> SetSupportAsync(AccountIdentity actor, string code, bool supported, byte[] revision, CancellationToken token) =>
        (await executor.ReadAsync("sp_PaymentMethodSetSupport", [.. CatalogRepository.Actor(actor),
            AuthRepository.Parameter("@Code", SqlDbType.Char, code, 5), AuthRepository.Parameter("@Supported", SqlDbType.Bit, supported),
            AuthRepository.Parameter("@ExpectedRevision", SqlDbType.Binary, revision, 8)], CatalogRepository.Result, token, true)).Single();
}
