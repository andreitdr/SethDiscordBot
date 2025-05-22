namespace PollMaker.Internal;

internal sealed record PollState(string Question, string[] Options)
{
    public List<HashSet<ulong>> Votes { get; } =
        Enumerable.Range(0, Options.Length).Select(_ => new HashSet<ulong>()).ToList();

    public bool IsOpen { get; private set; } = true;

    public void Close() => IsOpen = false;

    /// <summary>
    /// Toggle the member’s vote.  
    /// Clicking the **same button** again removes their vote;  
    /// clicking a **different** button moves the vote.
    /// </summary>
    public void ToggleVote(int optionIdx, ulong userId)
    {
        if (!IsOpen) return;
            
        if (Votes[optionIdx].Contains(userId))
        {
            Votes[optionIdx].Remove(userId);
            return;
        }
            
        foreach (var set in Votes)
            set.Remove(userId);

        Votes[optionIdx].Add(userId);
    }
}