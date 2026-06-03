namespace MemoryMonitoring.Core.Policies;

/// <summary>
/// 提供固定容量的轻量环形缓冲区。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class RingHistoryBuffer<T>
{
    private readonly Queue<T> _queue;
    private readonly int _capacity;

    public RingHistoryBuffer(int capacity)
    {
        _capacity = capacity;
        _queue = new Queue<T>(capacity);
    }

    public void Add(T item)
    {
        if (_queue.Count == _capacity)
        {
            _queue.Dequeue();
        }

        _queue.Enqueue(item);
    }

    public IReadOnlyList<T> GetSnapshot() => _queue.ToArray();
}
