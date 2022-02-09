using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace BMBF.WebServer
{
    internal class HeaderDictionary : IDictionary<string, string>
    {
        private readonly IDictionary<string, string> _inner = new Dictionary<string, string>();

        public string this[string name]
        {
            get => _inner[name.ToLowerInvariant()];
            set => _inner[name.ToLowerInvariant()] = value;
        }

        public ICollection<string> Keys => _inner.Keys;
        public ICollection<string> Values => _inner.Values.SelectMany((v) => v.Split(',')).ToArray();
        public int Count => _inner.Values.SelectMany((v) => v.Split(',')).Count();
        public bool IsReadOnly => false;

        public void Add(string name, string value)
        {
            var n = name.ToLowerInvariant();
            if (_inner.ContainsKey(n))
            {
                _inner[n] += $",{value}";
            }
            else
            {
                _inner[n] = value;
            }
        }
        public void Add(KeyValuePair<string, string> header) => Add(header.Key, header.Value);

        public void Clear() => _inner.Clear();

        public bool Contains(KeyValuePair<string, string> header)
        {
            string n = header.Key.ToLowerInvariant();
            return _inner.ContainsKey(n) && _inner[n].Split(",").Contains(header.Value);
        }

        public bool ContainsKey(string name) => _inner.ContainsKey(name.ToLowerInvariant());

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => AsCollection().CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => AsCollection().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Remove(string name) => _inner.Remove(name.ToLowerInvariant());

        public bool Remove(KeyValuePair<string, string> header)
        {
            var n = header.Key.ToLowerInvariant();
            if (!_inner.ContainsKey(n))
            {
                return false;
            }

            string v = _inner[n];
            if (v == header.Value)
            {
                _inner.Remove(n);
                return true;
            }
            else
            {
                _inner[n] = string.Join(',', v.Split(',').Where((vv) => vv != header.Value));
                return _inner[n] != v;
            }
        }

        public bool TryGetValue(string name, [MaybeNullWhen(false)] out string value) => _inner.TryGetValue(name.ToLowerInvariant(), out value);

        private ICollection<KeyValuePair<string, string>> AsCollection() =>
            Keys.SelectMany(n =>
                _inner[n].Split(',').Select(v =>
                    new KeyValuePair<string, string>(n, v))).ToArray();
    }
}
