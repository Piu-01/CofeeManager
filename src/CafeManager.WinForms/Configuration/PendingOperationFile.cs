using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos;
namespace CafeManager.WinForms.Configuration;

// DPAPI theo Windows user; file chỉ chứa yêu cầu đang chờ đối chiếu.
public sealed class PendingOperationFile(string? scope = null) : IPendingOperationStore
{
    private readonly string prefix = scope is null ? "" : Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(scope))) + "_";
    private string PathFor(int actor,string operation)
    {
        if (actor<=0 || operation.Length==0 || operation.Any(c=>!char.IsAsciiLetterOrDigit(c))) throw new ArgumentException(L.T("Nhận dạng yêu cầu không hợp lệ."));
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CoffeeManager","Pending",$"{prefix}{actor}_{operation}.bin");
    }
    public PendingOperation? Read(int actor,string operation)
    {
        var path=PathFor(actor,operation);
        if (!File.Exists(path)) return null;
        var result=JsonSerializer.Deserialize<PendingOperation>(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser));
        if (result is null || result.Actor!=actor || result.Operation!=operation) throw new InvalidDataException(L.T("Không đọc được yêu cầu đang chờ đối chiếu."));
        return result;
    }
    public void Save(PendingOperation operation)
    {
        var path=PathFor(operation.Actor,operation.Operation); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var gate=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var previous=Read(operation.Actor,operation.Operation);
        if(previous is not null && previous.RequestId!=operation.RequestId)
            throw new IOException(L.T("Còn yêu cầu khác chưa đối chiếu trên tài khoản này; mở lại màn để kiểm tra kết quả trước."));
        var temp=path+".tmp";
        File.WriteAllBytes(temp,ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(operation),null,DataProtectionScope.CurrentUser));
        File.Move(temp,path,true);
    }
    public void Clear(int actor,string operation)
    {
        var path=PathFor(actor,operation);
        if(!Directory.Exists(Path.GetDirectoryName(path))) return;
        using var gate=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        if(File.Exists(path)) File.Delete(path);
    }
}
