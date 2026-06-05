using Belumi.Infrastructure.Services;
using Xunit;

namespace Belumi.Tests;

public sealed class FirebaseAdminAppFactoryTests
{
    [Fact]
    public void EscapeJsonStringNewlines_WithNormalJson_DoesNotChangeFormat()
    {
        var input = @"{
  ""type"": ""service_account"",
  ""project_id"": ""belumi-123""
}";
        var expected = @"{
  ""type"": ""service_account"",
  ""project_id"": ""belumi-123""
}";

        var result = FirebaseAdminAppFactory.EscapeJsonStringNewlines(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EscapeJsonStringNewlines_WithRawNewlinesInValue_EscapesThem()
    {
        var input = @"{
  ""private_key"": ""-----BEGIN PRIVATE KEY-----
line1
line2
-----END PRIVATE KEY-----""
}";
        var expected = @"{
  ""private_key"": ""-----BEGIN PRIVATE KEY-----\nline1\nline2\n-----END PRIVATE KEY-----""
}";

        var result = FirebaseAdminAppFactory.EscapeJsonStringNewlines(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EscapeJsonStringNewlines_WithRawCRLFInValue_EscapesThem()
    {
        var input = "{ \"key\": \"value\r\nnext\" }";
        var expected = "{ \"key\": \"value\\nnext\" }";

        var result = FirebaseAdminAppFactory.EscapeJsonStringNewlines(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EscapeJsonStringNewlines_WithEscapedQuotes_CorrectlyTracksStringState()
    {
        // \" key \" has escaped quotes inside, so the string doesn't end early
        var input = @"{
  ""message"": ""Hello \""world\""
with newlines""
}";
        var expected = @"{
  ""message"": ""Hello \""world\""\nwith newlines""
}";

        var result = FirebaseAdminAppFactory.EscapeJsonStringNewlines(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EscapeJsonStringNewlines_WithAlreadyEscapedNewlines_DoesNotDoubleEscape()
    {
        var input = @"{
  ""private_key"": ""-----BEGIN PRIVATE KEY-----\nline1\nline2\n-----END PRIVATE KEY-----""
}";
        var expected = @"{
  ""private_key"": ""-----BEGIN PRIVATE KEY-----\nline1\nline2\n-----END PRIVATE KEY-----""
}";

        var result = FirebaseAdminAppFactory.EscapeJsonStringNewlines(input);
        Assert.Equal(expected, result);
    }
}
