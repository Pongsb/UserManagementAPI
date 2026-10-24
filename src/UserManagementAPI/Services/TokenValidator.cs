using System.Security.Cryptography;
using System.Text;

namespace UserManagementAPI.Services;

public sealed class TokenValidator
{
    private readonly byte[] _expectedHash;

    public TokenValidator(string token)
    {
        _expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    public bool IsValid(string candidate)
    {
        var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(candidate));
        return CryptographicOperations.FixedTimeEquals(_expectedHash, candidateHash);
    }
}
