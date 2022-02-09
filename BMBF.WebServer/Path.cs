using System.Collections.Generic;
using System.Linq;

namespace BMBF.WebServer
{
    internal class Path
    {
        private readonly Segment[] _segments;

        public Path(string path) : this(SplitSegments(path).Select(ToSegment)) { }
        private Path(IEnumerable<Segment> segments)
        {
            _segments = segments.ToArray();
        }

        public bool Matches(string path, out IDictionary<string, string> extracted)
        {
            extracted = new Dictionary<string, string>();
            string[] otherSegments = SplitSegments(path).ToArray();

            if (_segments.Length != otherSegments.Length)
            {
                return false;
            }

            foreach (var (segment, otherSegment) in _segments.Zip(otherSegments))
            {
                if (!segment.Matches(otherSegment, ref extracted))
                {
                    return false;
                }
            }

            return true;
        }

        public Path Join(Path other)
        {
            return new Path(_segments.Concat(other._segments));
        }

        private static IEnumerable<string> SplitSegments(string path) => path.Split('/').Where((s) => s != string.Empty);
        private static Segment ToSegment(string s) => s.StartsWith('{') && s.EndsWith('}')
            ? new ExtractorSegment(s[1..^1])
            : new VerbatimSegment(s);
    }

    internal abstract class Segment
    {
        public abstract bool Matches(string segment, ref IDictionary<string, string> extracted);
    }

    internal class VerbatimSegment : Segment
    {
        private readonly string _value;

        public VerbatimSegment(string value)
        {
            _value = value;
        }

        public override bool Matches(string segment, ref IDictionary<string, string> extracted) => segment == _value;
    }

    internal class ExtractorSegment : Segment
    {
        private readonly string _name;

        public ExtractorSegment(string name)
        {
            _name = name;
        }

        public override bool Matches(string segment, ref IDictionary<string, string> extracted)
        {
            extracted.Add(_name, segment);
            return true;
        }
    }
}
