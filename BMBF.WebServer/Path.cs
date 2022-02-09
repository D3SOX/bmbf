using System.Collections.Generic;
using System.Linq;

namespace BMBF.WebServer
{
    internal class Path
    {
        private readonly Segment[] segments;

        public Path(string path) : this(SplitSegments(path).Select(ToSegment)) { }
        private Path(IEnumerable<Segment> segments)
        {
            this.segments = segments.ToArray();
        }

        public bool Matches(string path, out IDictionary<string, string> extracted)
        {
            extracted = new Dictionary<string, string>();
            var otherSegments = SplitSegments(path).ToArray();

            if (segments.Length != otherSegments.Length) return false;

            foreach (var (segment, otherSegment) in segments.Zip(otherSegments))
            {
                if (!segment.Matches(otherSegment, ref extracted)) return false;
            }

            return true;
        }

        public Path Join(Path other)
        {
            return new Path(Enumerable.Concat(segments, other.segments));
        }

        private static IEnumerable<string> SplitSegments(string path) => path.Split('/').Where((s) => s != string.Empty);
        private static Segment ToSegment(string s) => s.StartsWith('{') && s.EndsWith('}')
            ? new ExtractorSegment(s[1..-1])
            : new VerbatimSegment(s);
    }

    internal abstract class Segment
    {
        public abstract bool Matches(string segment, ref IDictionary<string, string> extracted);
    }

    internal class VerbatimSegment : Segment
    {
        private readonly string value;

        public VerbatimSegment(string value)
        {
            this.value = value;
        }

        public override bool Matches(string segment, ref IDictionary<string, string> _extracted) => segment == value;
    }

    internal class ExtractorSegment : Segment
    {
        private readonly string name;

        public ExtractorSegment(string name)
        {
            this.name = name;
        }

        public override bool Matches(string segment, ref IDictionary<string, string> extracted)
        {
            extracted.Add(name, segment);
            return true;
        }
    }
}
