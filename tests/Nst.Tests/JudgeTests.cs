using System.Net;
using Nst.Tests.Judging;

namespace Nst.Tests;

public class JudgeTests
{
    [Fact]
    public async Task A_failed_reply_is_not_copied_into_the_error()
    {
        const string sentinel = "SENTINEL-KEY-0123456789";
        var handler = new FixedReply(HttpStatusCode.Unauthorized, $"{{\"error\":\"bad key {sentinel}\"}}");
        var judge = new JevJudge(sentinel, handler);

        var error = await Assert.ThrowsAsync<HttpRequestException>(
            () => judge.Ask("text", new Dictionary<string, string> { ["q"] = "Is it?" }));

        Assert.Contains("401", error.Message);
        Assert.DoesNotContain(sentinel, error.ToString());
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(null, "  \n")]
    [InlineData("", null)]
    [InlineData(" ", "\t")]
    public void A_blank_key_counts_as_no_key(string? variable, string? fileText) =>
        Assert.Null(Judges.KeyFrom(variable, fileText));

    [Fact]
    public void The_variable_wins_over_the_file() =>
        Assert.Equal("from-variable", Judges.KeyFrom(" from-variable ", "from-file"));

    [Fact]
    public void The_file_is_used_when_the_variable_is_blank() =>
        Assert.Equal("from-file", Judges.KeyFrom(" ", "from-file\n"));

    sealed class FixedReply(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
