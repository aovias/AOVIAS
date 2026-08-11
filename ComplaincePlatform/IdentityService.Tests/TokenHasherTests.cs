using IdentityService.Infrastructure.Security;
using Xunit;

namespace IdentityService.Tests;

public class TokenHasherTests
{
    [Fact]
    public void Hash_IsDeterministic()
    {
        var hash1 = TokenHasher.Hash("test-token");
        var hash2 = TokenHasher.Hash("test-token");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Hash_DifferentTokens_ProduceDifferentHashes()
    {
        var hash1 = TokenHasher.Hash("token-a");
        var hash2 = TokenHasher.Hash("token-b");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateOpaqueToken_ProducesUniqueValues()
    {
        var t1 = TokenHasher.GenerateOpaqueToken();
        var t2 = TokenHasher.GenerateOpaqueToken();
        Assert.NotEqual(t1, t2);
    }
}
