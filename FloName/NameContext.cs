using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Test_FloName")]
namespace FloName
{
    internal class NameContext: INameContext
    {
        private readonly ConcurrentDictionary<string, int> _sequences = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _unique = new();


        // Default constructor — fully fresh context
        public NameContext()
        {
            _sequences = new ConcurrentDictionary<string, int>();
        }

        // Batch constructor — shares sequence state from parent, fresh uniqueness
        public NameContext(NameContext parent)
        {
            _sequences = parent._sequences; // shared reference, SEQ increments across batch
        }

        public int GetSequence(string key, int start) //start only used on first call.
        {
            return _sequences.AddOrUpdate(
                key,
                start,
                (_, old) => old + 1);
        }

        public bool RegisterUnique(string key, string value)
        {
            var set = _unique.GetOrAdd(key, _ => []);

            lock (set)
            {
                if (set.Contains(value))
                    return false;

                set.Add(value);
                return true;
            }
        }
    }
}
