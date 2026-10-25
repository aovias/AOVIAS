using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IntegrationService.Application.Integration.Platform;
using Shared.Authorization;

namespace IntegrationService.Application.Integration.Security;

public interface IIntegrationAuthorizationService
{
    Task EnsureAsync(ClaimsPrincipal user, string permissionCode, Guid? organizationId, CancellationToken ct = default);
}

public sealed class IntegrationAuthorizationService(IAuthorizationEvaluator evaluator) : IIntegrationAuthorizationService
{
    public async Task EnsureAsync(ClaimsPrincipal user, string permissionCode, Guid? organizationId, CancellationToken ct = default)
    {
        var req = AuthorizationContextBuilder.FromPermissionCode(user, permissionCode);
        if (organizationId.HasValue)
            req = new AuthorizationRequest
            {
                SubjectId = req.SubjectId, PersonId = req.PersonId, EmploymentId = req.EmploymentId,
                ProfessionalProfileId = req.ProfessionalProfileId, OrganizationId = organizationId,
                HospitalId = req.HospitalId, DepartmentId = req.DepartmentId,
                Resource = req.Resource, Action = req.Action, Context = req.Context
            };
        var decision = await evaluator.EvaluateAsync(req, ct);
        if (!decision.Allowed) throw new UnauthorizedAccessException($"{permissionCode} required.");
    }
}

public sealed class CredentialProtector(IConfiguration configuration) : ICredentialProtector
{
    private byte[] Key => SHA256.HashData(Encoding.UTF8.GetBytes(configuration["Integration:CredentialEncryptionKey"] ?? "default-dev-key-32-chars-minimum!!"));

    public string Protect(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public string Unprotect(string protectedText)
    {
        var data = Convert.FromBase64String(protectedText);
        var nonce = data[..12];
        var tag = data[12..28];
        var cipher = data[28..];
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(Key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
