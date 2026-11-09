namespace Flow.Diagrams.Tests.Memory;

/// <summary>
/// Forces collections until a weakly referenced object is gone. Create the object in a separate
/// <c>[MethodImpl(MethodImplOptions.NoInlining)]</c> method that returns only the <see cref="WeakReference"/>, so no
/// local of the test (which Debug builds keep alive to the end of the method) roots it.
/// </summary>
internal static class Gc
{
    public static bool IsCollected(WeakReference reference)
    {
        for (var attempt = 0; attempt < 5 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        return !reference.IsAlive;
    }

    /// <summary>The names of the references whose objects survive forced collections.</summary>
    public static List<string> Uncollected(IEnumerable<(string Name, WeakReference Reference)> references) =>
        references.Where(r => !IsCollected(r.Reference)).Select(r => r.Name).ToList();
}
