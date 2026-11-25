using System.Net;
using Nst.Tests.Judging;

namespace Nst.Tests;

public class JudgeTests
{
    [Fact]
    public async Task A_failed_reply_is_not_copied_into_the_error()
    {
        const string sentinel = "SENTINEL-KEY-0123456789";
        var handler = new FixedReply(HttpStatusCode.Unauthorized, new StringContent($"{{\"error\":\"bad key {sentinel}\"}}"));
        var judge = new JevJudge(sentinel, handler);

        var error = await Assert.ThrowsAsync<HttpRequestException>(
            () => judge.Ask("text", new Dictionary<string, string> { ["q"] = "Is it?" }));

        Assert.Contains("401", error.Message);
        Assert.DoesNotContain(sentinel, error.ToString());
    }

    [Fact]
    public async Task A_failed_reply_is_not_read()
    {
        var body = new UnreadableBody();
        var judge = new JevJudge("key", new FixedReply(HttpStatusCode.InternalServerError, body));

        var error = await Assert.ThrowsAsync<HttpRequestException>(
            () => judge.Ask("text", new Dictionary<string, string> { ["q"] = "Is it?" }));

        Assert.Contains("500", error.Message);
        Assert.False(body.WasRead);
    }

    [Fact]
    public async Task A_reply_body_that_stalls_runs_into_the_deadline()
    {
        var judge = new JevJudge("key", new FixedReply(HttpStatusCode.OK, new StalledBody()), TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => judge.Ask("text", new Dictionary<string, string> { ["q"] = "Is it?" })).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(null, "  \n")]
    [InlineData("", null)]
    [InlineData(" ", "\t")]
    public void A_blank_key_counts_as_no_key(string? variable, string? fileText) =>
        Assert.Null(Judges.KeyFrom(variable, () => fileText));

    [Fact]
    public void The_variable_wins_over_the_file() =>
        Assert.Equal("from-variable", Judges.KeyFrom(" from-variable ", () => "from-file"));

    [Fact]
    public void The_file_is_used_when_the_variable_is_blank() =>
        Assert.Equal("from-file", Judges.KeyFrom(" ", () => "from-file\n"));

    // A key in the variable is enough: the file is not opened, so a file that cannot be read does no harm.
    [Fact]
    public void The_file_is_not_read_when_the_variable_holds_a_key() =>
        Assert.Equal("from-variable", Judges.KeyFrom("from-variable", () => throw new IOException("The key file is locked.")));

    sealed class FixedReply(HttpStatusCode status, HttpContent body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = body });
    }

    // A body that records whether anything read it, and fails if something does.
    sealed class UnreadableBody : HttpContent
    {
        public bool WasRead { get; private set; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            WasRead = true;
            throw new IOException("The body broke in transit.");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    // A body that never arrives.
    sealed class StalledBody : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            Task.Delay(Timeout.Infinite, cancellationToken);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
