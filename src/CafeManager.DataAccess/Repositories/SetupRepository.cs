using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Requests;

namespace CafeManager.DataAccess.Repositories;

public sealed class SetupRepository(ConnectionFactory factory)
{
    private readonly AuthRepository auth = new(factory);
    public async Task CheckAccessAsync(CancellationToken token = default) =>
        _ = (await auth.ReadAsync("sp_SetupCheckAccess", [], r => r.GetBoolean(0), token)).Single();
    public Task<AppSetupStatus> ReadStateAsync(CancellationToken token = default) => auth.ReadStateAsync(token);
    public Task<List<SetupCandidate>> CandidatesAsync(CancellationToken token = default) => auth.ReadAsync("sp_SetupListCandidates", [],
        r => new SetupCandidate(r.GetString(0).TrimEnd(), r.GetString(1), r.IsDBNull(3) ? null : r.GetInt32(3),
            r.IsDBNull(4) ? null : r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5).TrimEnd(), r.GetBoolean(8),
            r.IsDBNull(7) ? null : (byte[])r[7]), token);
    public async Task CompleteAsync(BootstrapRequest request, string hash, CancellationToken token)
    {
        await auth.ReadAsync("sp_SetupBootstrap", [AuthRepository.Parameter("@Mode", SqlDbType.VarChar, request.Mode, 30),
            AuthRepository.Parameter("@Username", SqlDbType.VarChar, request.Username, 50),
            AuthRepository.Parameter("@NewHash", SqlDbType.VarChar, hash, 255),
            AuthRepository.Parameter("@MaNV", SqlDbType.Char, request.EmployeeCode, 5),
            AuthRepository.Parameter("@TenNV", SqlDbType.NVarChar, request.EmployeeName, 50),
            AuthRepository.Parameter("@SDT", SqlDbType.VarChar, request.Phone, 15),
            AuthRepository.Parameter("@ExpectedStamp", SqlDbType.Binary, request.ExpectedStamp, 8)], _ => true, token, true);
    }
    public async Task ConfirmLegacyAsync(AccountCredential credential, CancellationToken token)
    {
        await auth.ReadAsync("sp_SetupConfirmLegacy", [AuthRepository.Parameter("@MaTK", SqlDbType.Int, credential.Identity.AccountId),
            AuthRepository.Parameter("@ExpectedHash", SqlDbType.VarChar, credential.Hash, 255),
            AuthRepository.Parameter("@ExpectedStamp", SqlDbType.Binary, credential.Identity.SecurityStamp, 8)], _ => true, token, true);
    }
}
