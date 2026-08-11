using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>
/// Simulates the operator answering (or cancelling) an "Administrator Privileges Required" prompt —
/// no UI, no dispatcher, just a queue of pre-scripted answers so a test can drive a multi-attempt
/// conversation (e.g. "wrong password once, then correct") deterministically.
/// </summary>
public sealed class FakeBootstrapElevationHandler : IBootstrapElevationHandler
{
    private readonly Queue<SudoPasswordPromptResult> answers;

    public FakeBootstrapElevationHandler(params SudoPasswordPromptResult[] answers)
    {
        this.answers = new Queue<SudoPasswordPromptResult>(answers);
    }

    public int RequestCount { get; private set; }

    public List<bool> RetryFlagsSeen { get; } = [];

    public Task<SudoPasswordPromptResult> RequestSudoPasswordAsync(bool isRetryAfterRejection, CancellationToken cancellationToken)
    {
        RequestCount++;
        RetryFlagsSeen.Add(isRetryAfterRejection);
        var answer = answers.Count > 0
            ? answers.Dequeue()
            : new SudoPasswordPromptResult { Password = null }; // no more scripted answers — treat as cancel, never hang a test
        return Task.FromResult(answer);
    }
}
