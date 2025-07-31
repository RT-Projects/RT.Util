using RT.Internal;

namespace RT.Geometry;

/// <summary>
///     Provides values to specify options on the <see cref="VoronoiDiagram.GenerateVoronoiDiagram(PointD[], double, double,
///     VoronoiDiagramFlags)"/> method.</summary>
[Flags]
public enum VoronoiDiagramFlags
{
    /// <summary>Indicates that duplicate sites (points) should be removed from the input.</summary>
    RemoveDuplicates = 1 << 0,
    /// <summary>Indicates that input sites (points) that lie outside the bounds of the viewport should be ignored.</summary>
    RemoveOffboundsSites = 1 << 1,
    /// <summary>
    ///     If not specified, only polygons contained entirely within the bounds are included. Otherwise, the theoretically
    ///     “infinite” polygons are included as polygons that are clipped to the bounding region.</summary>
    IncludeEdgePolygons = 1 << 2,
    /// <summary>
    ///     When specified, only site indices will be populated; edges and polygons will not. This is about 10% faster on
    ///     large inputs.</summary>
    OnlySites = 1 << 3,
}

/// <summary>Represents a Voronoi diagram.</summary>
public sealed class VoronoiDiagram
{
    /// <summary>Edges of the diagram, along with the sites that generated each edge.</summary>
    public List<(EdgeD edge, int siteA, int siteB)> Edges;
    /// <summary>
    ///     Polygons corresponding to each of the input points. The order of polygons in this array matches the order of the
    ///     input sites exactly. Depending on <see cref="VoronoiDiagramFlags"/>, this array contains nulls for sites that were
    ///     filtered out, as well as sites that correspond to un-closed semi-polygons around the outside of the diagram.</summary>
    public PolygonD[] Polygons;

    /// <summary>
    ///     Generates a Voronoi diagram from a set of input points.</summary>
    /// <param name="sites">
    ///     Input points (sites) to generate diagram from.</param>
    /// <param name="width">
    ///     Width of the viewport. The origin of the viewport is assumed to be at (0, 0).</param>
    /// <param name="height">
    ///     Height of the viewport. The origin of the viewport is assumed to be at (0, 0).</param>
    /// <param name="flags">
    ///     Set of <see cref="VoronoiDiagramFlags"/> values that specifies additional options.</param>
    /// <returns>
    ///     A list of line segments describing the Voronoi diagram.</returns>
    public static VoronoiDiagram GenerateVoronoiDiagram(PointD[] sites, double width, double height, VoronoiDiagramFlags flags = 0)
    {
        var data = new data(sites, width, height, flags);
        return new VoronoiDiagram
        {
            Edges = data.Edges.Select(edge => ((flags & VoronoiDiagramFlags.OnlySites) != 0 ? default : new EdgeD(edge.Start.Value, edge.End.Value), edge.SiteA.Index, edge.SiteB.Index)).ToList(),
            Polygons = (flags & VoronoiDiagramFlags.OnlySites) != 0 ? null : data.Polygons
                .Select(p => p.ToPolygonD((flags & VoronoiDiagramFlags.IncludeEdgePolygons) != 0, width, height))
                .ToArray()
        };
    }

    /// <summary>
    ///     Generates a Delaunay triangulation of the input points (sites).</summary>
    /// <param name="sites">
    ///     Input points (sites) to triangulate. Must not contain duplicates.</param>
    /// <returns>
    ///     A list of edges in the triangulation, as pairs of indices into <paramref name="sites"/>. The indices within each
    ///     pair, as well as the pairs themselves, are ordered arbitrarily.</returns>
    internal static IEnumerable<(int SiteA, int SiteB)> Triangulate(PointD[] sites)
    {
        var data = new data(sites, 0, 0, VoronoiDiagramFlags.OnlySites);
        return data.Edges.Select(e => (e.SiteA.Index, e.SiteB.Index));
    }

    /// <summary>
    ///     Internal class to generate Voronoi diagrams using Fortune’s algorithm. Contains internal data structures and
    ///     methods.</summary>
    private sealed class data
    {
        public List<arc> Arcs = [];
        private Queue<siteEvent> _siteEvents;
        private List<circleEvent> _circleEvents = [];
        public List<edge> Edges = [];
        public polygon[] Polygons;

        public data(PointD[] sites, double width, double height, VoronoiDiagramFlags flags)
        {
            var events = new List<siteEvent>(sites.Length);
            Polygons = new polygon[sites.Length];
            for (var siteIx = 0; siteIx < sites.Length; siteIx++)
            {
                var p = sites[siteIx];
                if ((flags & VoronoiDiagramFlags.OnlySites) != 0 || (p.X > 0 && p.Y > 0 && p.X < width && p.Y < height))
                    events.Add(new siteEvent(new site(siteIx, p)));
                else if ((flags & VoronoiDiagramFlags.RemoveOffboundsSites) == 0)
                    throw new Exception("The input contains a point outside the bounds or on the perimeter (coordinates " +
                        p + $"). This case is not handled by this algorithm. Use the {typeof(VoronoiDiagramFlags).FullName}.{nameof(VoronoiDiagramFlags.RemoveOffboundsSites)} " +
                        $"flag to automatically remove such off-bounds input points, or {nameof(VoronoiDiagramFlags.OnlySites)} to skip polygon and edge generation.");
            }
            events.Sort();

            // Make sure there are no two equal points in the input
            for (int i = 1; i < events.Count; i++)
            {
                while (i < events.Count && events[i - 1].Site.Position == events[i].Site.Position)
                {
                    if ((flags & VoronoiDiagramFlags.RemoveDuplicates) == VoronoiDiagramFlags.RemoveDuplicates)
                        events.RemoveAt(i);
                    else
                        throw new Exception("The input contains two points at the same coordinates " +
                            events[i].Site.Position + ". Voronoi diagrams are undefined for such a situation. " +
                            $"Use the {typeof(VoronoiDiagramFlags).FullName}.{nameof(VoronoiDiagramFlags.RemoveDuplicates)} flag to automatically remove such duplicate input points.");
                }
            }

            _siteEvents = new Queue<siteEvent>(events);

            // Main loop
            while (_siteEvents.Count > 0 || _circleEvents.Count > 0)
            {
                if (_circleEvents.Count > 0 && (_siteEvents.Count == 0 || _circleEvents[0].X <= _siteEvents.Peek().Site.Position.X))
                {
                    // Process a circle event
                    circleEvent evt = _circleEvents[0];
                    _circleEvents.RemoveAt(0);
                    int arcIndex = Arcs.IndexOf(evt.Arc);
                    if (arcIndex == -1) continue;

                    // The two edges left and right of the disappearing arc end here
                    if ((flags & VoronoiDiagramFlags.OnlySites) == 0)
                    {
                        Arcs[arcIndex - 1].Edge?.SetEndPoint(evt.Center);
                        evt.Arc.Edge?.SetEndPoint(evt.Center);
                    }

                    // Remove the arc from the beachline
                    Arcs.RemoveAt(arcIndex);
                    // ArcIndex now points to the arc after the one that disappeared

                    // Start a new edge at the point where the other two edges ended
                    Arcs[arcIndex - 1].Edge = new edge(Arcs[arcIndex - 1].Site, Arcs[arcIndex].Site);
                    if ((flags & VoronoiDiagramFlags.OnlySites) == 0)
                        Arcs[arcIndex - 1].Edge.SetEndPoint(evt.Center);
                    Edges.Add(Arcs[arcIndex - 1].Edge);

                    // Recheck circle events on either side of the disappearing arc
                    if (arcIndex > 0)
                        checkCircleEvent(arcIndex - 1);
                    if (arcIndex < Arcs.Count)
                        checkCircleEvent(arcIndex);
                }
                else
                {
                    // Process a site event
                    var evt = _siteEvents.Dequeue();

                    if (Arcs.Count == 0)
                    {
                        Arcs.Add(new arc(evt.Site));
                        continue;
                    }

                    // Find the current arc(s) at height e.Position.y (if there are any)
                    bool arcFound = false;
                    for (int i = 0; i < Arcs.Count; i++)
                    {
                        if (doesIntersect(evt.Site.Position, i, out var intersect))
                        {
                            // New parabola intersects Arc - duplicate Arc
                            Arcs.Insert(i + 1, new arc(Arcs[i].Site));
                            Arcs[i + 1].Edge = Arcs[i].Edge;

                            // Add a new Arc for Event.Position in the right place
                            Arcs.Insert(i + 1, new arc(evt.Site));

                            // Add new half-edges connected to Arc's endpoints
                            Arcs[i].Edge = Arcs[i + 1].Edge = new edge(Arcs[i + 1].Site, Arcs[i + 2].Site);
                            Edges.Add(Arcs[i].Edge);

                            // Check for new circle events around the new arc:
                            checkCircleEvent(i);
                            checkCircleEvent(i + 2);

                            arcFound = true;
                            break;
                        }
                    }

                    if (arcFound)
                        continue;

                    // Special case: If Event.Position never intersects an arc, append it to the list.
                    // This only happens if there is more than one site event with the lowest X co-ordinate.
                    arc lastArc = Arcs[Arcs.Count - 1];
                    arc newArc = new(evt.Site);
                    lastArc.Edge = new edge(lastArc.Site, newArc.Site);
                    Edges.Add(lastArc.Edge);
                    if ((flags & VoronoiDiagramFlags.OnlySites) == 0)
                        lastArc.Edge.SetEndPoint(new PointD(0, (newArc.Site.Position.Y + lastArc.Site.Position.Y) / 2));
                    Arcs.Add(newArc);
                }
            }

            if ((flags & VoronoiDiagramFlags.OnlySites) != 0)
                return;

            // Advance the sweep line so no parabolas can cross the bounding box
            double var = 2 * width + height;

            // Extend each remaining edge to the new parabola intersections
            for (int i = 0; i < Arcs.Count - 1; i++)
                Arcs[i].Edge?.SetEndPoint(getIntersection(Arcs[i].Site.Position, Arcs[i + 1].Site.Position, 2 * var));

            // Clip all the edges with the bounding rectangle and remove edges that are entirely outside
            var newEdges = new List<edge>();
            var boundingEdges = new[] { new EdgeD(0, 0, width, 0), new EdgeD(width, 0, width, height), new EdgeD(width, height, 0, height), new EdgeD(0, height, 0, 0) };
            foreach (edge e in Edges)
            {
                if ((e.Start.Value.X < 0 || e.Start.Value.X > width || e.Start.Value.Y < 0 || e.Start.Value.Y > height) &&
                    (e.End.Value.X < 0 || e.End.Value.X > width || e.End.Value.Y < 0 || e.End.Value.Y > height) &&
                    !boundingEdges.Any(be => be.IntersectsWith(new EdgeD(e.Start.Value, e.End.Value))))
                    continue;

                if (e.Start.Value.X < 0)
                    e.Start = new PointD(0, e.End.Value.X / (e.End.Value.X - e.Start.Value.X) * (e.Start.Value.Y - e.End.Value.Y) + e.End.Value.Y);
                if (e.Start.Value.Y < 0)
                    e.Start = new PointD(e.End.Value.Y / (e.End.Value.Y - e.Start.Value.Y) * (e.Start.Value.X - e.End.Value.X) + e.End.Value.X, 0);
                if (e.End.Value.X < 0)
                    e.End = new PointD(0, e.Start.Value.X / (e.Start.Value.X - e.End.Value.X) * (e.End.Value.Y - e.Start.Value.Y) + e.Start.Value.Y);
                if (e.End.Value.Y < 0)
                    e.End = new PointD(e.Start.Value.Y / (e.Start.Value.Y - e.End.Value.Y) * (e.End.Value.X - e.Start.Value.X) + e.Start.Value.X, 0);

                if (e.Start.Value.X > width)
                    e.Start = new PointD(width, (width - e.Start.Value.X) / (e.End.Value.X - e.Start.Value.X) * (e.End.Value.Y - e.Start.Value.Y) + e.Start.Value.Y);
                if (e.Start.Value.Y > height)
                    e.Start = new PointD((height - e.Start.Value.Y) / (e.End.Value.Y - e.Start.Value.Y) * (e.End.Value.X - e.Start.Value.X) + e.Start.Value.X, height);
                if (e.End.Value.X > width)
                    e.End = new PointD(width, (width - e.End.Value.X) / (e.Start.Value.X - e.End.Value.X) * (e.Start.Value.Y - e.End.Value.Y) + e.End.Value.Y);
                if (e.End.Value.Y > height)
                    e.End = new PointD((height - e.End.Value.Y) / (e.Start.Value.Y - e.End.Value.Y) * (e.Start.Value.X - e.End.Value.X) + e.End.Value.X, height);
                newEdges.Add(e);
            }
            Edges = newEdges;

            // Generate polygons from the edges
            foreach (edge e in Edges)
            {
                if (Polygons[e.SiteA.Index] == null)
                    Polygons[e.SiteA.Index] = new polygon(e.SiteA);
                Polygons[e.SiteA.Index].AddEdge(e);
                if (Polygons[e.SiteB.Index] == null)
                    Polygons[e.SiteB.Index] = new polygon(e.SiteB);
                Polygons[e.SiteB.Index].AddEdge(e);
            }
            if (sites.Length == 1) // this is the only scenario in which there exists a "polygon" with no edges
                Polygons[0] = new polygon(new site(0, sites[0]));
        }

        // Will a new parabola at p intersect with the arc at ArcIndex?
        private bool doesIntersect(PointD p, int arcIndex, out PointD result)
        {
            arc arc = Arcs[arcIndex];

            result = new PointD(0, 0);
            if (arc.Site.Position.X == p.X)
                return false;

            if ((arcIndex == 0 || getIntersection(Arcs[arcIndex - 1].Site.Position, arc.Site.Position, p.X).Y <= p.Y) &&
                (arcIndex == Arcs.Count - 1 || p.Y <= getIntersection(arc.Site.Position, Arcs[arcIndex + 1].Site.Position, p.X).Y))
            {
                result.Y = p.Y;

                // Plug it back into the parabola equation
                result.X = (arc.Site.Position.X * arc.Site.Position.X + (arc.Site.Position.Y - result.Y) * (arc.Site.Position.Y - result.Y) - p.X * p.X)
                          / (2 * arc.Site.Position.X - 2 * p.X);

                return true;
            }
            return false;
        }

        // Where do two parabolas intersect?
        private static PointD getIntersection(PointD siteA, PointD siteB, double scanX)
        {
            PointD result = new();
            PointD p = siteA;

            if (siteA.X == siteB.X)
                result.Y = (siteA.Y + siteB.Y) / 2;
            else if (siteB.X == scanX)
                result.Y = siteB.Y;
            else if (siteA.X == scanX)
            {
                result.Y = siteA.Y;
                p = siteB;
            }
            else
            {
                // Use the quadratic formula
                double z0 = 2 * (siteA.X - scanX);
                double z1 = 2 * (siteB.X - scanX);

                double a = 1 / z0 - 1 / z1;
                double b = -2 * (siteA.Y / z0 - siteB.Y / z1);
                double c = (siteA.Y * siteA.Y + siteA.X * siteA.X - scanX * scanX) / z0
                         - (siteB.Y * siteB.Y + siteB.X * siteB.X - scanX * scanX) / z1;

                result.Y = (-b - Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            }

            // Plug back into one of the parabola equations
            result.X = (p.X * p.X + (p.Y - result.Y) * (p.Y - result.Y) - scanX * scanX) / (2 * p.X - 2 * scanX);
            return result;
        }

        // Look for a new circle event for the arc at ArcIndex
        private void checkCircleEvent(int arcIndex)
        {
            if (arcIndex == 0 || arcIndex == Arcs.Count - 1)
                return;

            if (getCircle(Arcs[arcIndex - 1].Site.Position, Arcs[arcIndex].Site.Position, Arcs[arcIndex + 1].Site.Position, out var center, out var maxX))
            {
                // Add the new event in the right place using binary search
                int low = 0;
                int high = _circleEvents.Count;
                while (low < high)
                {
                    int middle = (low + high) / 2;
                    circleEvent evt = _circleEvents[middle];
                    if (evt.X < maxX || (evt.X == maxX && evt.Center.Y < center.Y))
                        low = middle + 1;
                    else
                        high = middle;
                }
                _circleEvents.Insert(low, new circleEvent(maxX, center, Arcs[arcIndex]));
            }
        }

        // Find the circle through points p1, p2, p3
        private static bool getCircle(PointD p1, PointD p2, PointD p3, out PointD center, out double maxX)
        {
            maxX = 0;
            center = new PointD(0, 0);

            // Check that BC is a "right turn" from AB
            if ((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y) > 0)
                return false;

            // Algorithm from O'Rourke 2ed p. 189.
            double a = p2.X - p1.X, b = p2.Y - p1.Y,
                   c = p3.X - p1.X, d = p3.Y - p1.Y,
                   e = a * (p1.X + p2.X) + b * (p1.Y + p2.Y),
                   f = c * (p1.X + p3.X) + d * (p1.Y + p3.Y),
                   g = 2 * (a * (p3.Y - p2.Y) - b * (p3.X - p2.X));

            if (g == 0) return false;  // Points are co-linear.

            center.X = (d * e - b * f) / g;
            center.Y = (a * f - c * e) / g;

            // MaxX = Center.X + radius of the circle
            maxX = center.X + Math.Sqrt(Math.Pow(p1.X - center.X, 2) + Math.Pow(p1.Y - center.Y, 2));
            return true;
        }
    }

    private struct site(int index, PointD position) : IEquatable<site>
    {
        public int Index { get; private set; } = index;
        public PointD Position { get; private set; } = position;
        public readonly bool Equals(site other) => other.Index == Index;
        public override readonly bool Equals(object obj) => obj is site other && Equals(other);
        public override readonly int GetHashCode() => Index;
    }

    /// <summary>Internal class describing an edge in the Voronoi diagram. May be incomplete as the algorithm progresses.</summary>
    private sealed class edge(site siteA, site siteB)
    {
        public PointD? Start = null, End = null;
        public site SiteA = siteA;
        public site SiteB = siteB;

        public void SetEndPoint(PointD end)
        {
            if (Start == null)
                Start = end;
            else if (End == null)
                End = end;
        }
        public override string ToString() { return (Start == null ? "?" : Start.Value.ToString()) + " ==> " + (End == null ? "?" : End.ToString()); }
    }

    /// <summary>Internal class describing a polygon in the Voronoi diagram. May be incomplete as the algorithm progresses.</summary>
    private sealed class polygon(site site)
    {
        public bool Complete = false;
        public site Site = site;
        private readonly List<PointD> _processedPoints = [];
        private List<edge> _unprocessedEdges = [];

        private static int rectEdge(PointD p, double width, double height) =>
            p.Y == 0 ? 0 : p.X == width ? 1 : p.Y == height ? 2 : p.X == 0 ? 3 : throw new Exception("Point is not on the edge.");

        public PolygonD ToPolygonD(bool autocomplete, double width, double height)
        {
            if (Complete)
                return new PolygonD(_processedPoints);

            if (!autocomplete)
                return null;

            // A polygon with zero processed points is only possible if the input was a single site. Special-case this.
            if (_processedPoints.Count == 0)
                return new PolygonD(new PointD(0, 0), new PointD(width, 0), new PointD(width, height), new PointD(0, height));

            var firstPointEdge = rectEdge(_processedPoints[0], width, height);
            var lastPoint = _processedPoints[_processedPoints.Count - 1];
            var lastPointEdge = rectEdge(lastPoint, width, height);
            var origNumVertices = _processedPoints.Count;
            var origUnprocessed = _unprocessedEdges.ToList();

            // Clockwise
            while (true)
            {
                if (lastPointEdge == firstPointEdge)
                    break;

                var upe = _unprocessedEdges.Count == 0 ? null : _unprocessedEdges
                    .SelectTwo(e => new { Edge = e, Start = true, Point = e.Start.Value }, e => new { Edge = e, Start = false, Point = e.End.Value })
                    .FirstOrDefault(inf =>
                        lastPointEdge == 0 ? (inf.Point.Y == 0 && inf.Point.X > lastPoint.X) :
                        lastPointEdge == 1 ? (inf.Point.X == width && inf.Point.Y > lastPoint.Y) :
                        lastPointEdge == 2 ? (inf.Point.Y == height && inf.Point.X < lastPoint.X) : (inf.Point.X == 0 && inf.Point.Y < lastPoint.Y));

                if (upe != null)
                {
                    _unprocessedEdges.Remove(upe.Edge);
                    _processedPoints.Add(upe.Start ? upe.Edge.Start.Value : upe.Edge.End.Value);
                    AddEdge(upe.Edge);
                    if (_unprocessedEdges.Count > 0)
                        throw new Exception("Assertion failed: There should be no unprocessed edges left.");
                    lastPointEdge = rectEdge(_processedPoints[_processedPoints.Count - 1], width, height);
                }
                else
                {
                    lastPoint =
                        lastPointEdge == 0 ? new PointD(width, 0) :
                        lastPointEdge == 1 ? new PointD(width, height) :
                        lastPointEdge == 2 ? new PointD(0, height) : new PointD(0, 0);
                    _processedPoints.Add(lastPoint);
                    lastPointEdge = (lastPointEdge + 1) % 4;
                }
            }
            if (_unprocessedEdges.Count == 0)
            {
                var polygon = new PolygonD(_processedPoints);
                if (polygon.ContainsPoint(Site.Position))
                    return polygon;
            }
            _processedPoints.RemoveRange(origNumVertices, _processedPoints.Count - origNumVertices);
            _unprocessedEdges = origUnprocessed;

            // Counter-clockwise
            lastPoint = _processedPoints[_processedPoints.Count - 1];
            lastPointEdge = rectEdge(lastPoint, width, height);
            while (true)
            {
                if (lastPointEdge == firstPointEdge)
                    break;

                var upe = _unprocessedEdges.Count == 0 ? null : _unprocessedEdges
                    .SelectTwo(e => new { Edge = e, Start = true, Point = e.Start.Value }, e => new { Edge = e, Start = false, Point = e.End.Value })
                    .FirstOrDefault(inf =>
                        lastPointEdge == 0 ? (inf.Point.Y == 0 && inf.Point.X < lastPoint.X) :
                        lastPointEdge == 1 ? (inf.Point.X == width && inf.Point.Y < lastPoint.Y) :
                        lastPointEdge == 2 ? (inf.Point.Y == height && inf.Point.X > lastPoint.X) : (inf.Point.X == 0 && inf.Point.Y > lastPoint.Y));

                if (upe != null)
                {
                    _unprocessedEdges.Remove(upe.Edge);
                    _processedPoints.Add(upe.Start ? upe.Edge.Start.Value : upe.Edge.End.Value);
                    AddEdge(upe.Edge);
                    if (_unprocessedEdges.Count > 0)
                        throw new Exception("Assertion failed: There should be no unprocessed edges left.");
                    lastPointEdge = rectEdge(_processedPoints[_processedPoints.Count - 1], width, height);
                }
                else
                {
                    lastPoint =
                        lastPointEdge == 0 ? new PointD(0, 0) :
                        lastPointEdge == 1 ? new PointD(width, 0) :
                        lastPointEdge == 2 ? new PointD(width, height) : new PointD(0, height);
                    _processedPoints.Add(lastPoint);
                    lastPointEdge = (lastPointEdge + 3) % 4;
                }
            }
            return new PolygonD(Enumerable.Reverse(_processedPoints));
        }

        public void AddEdge(edge edge)
        {
            if (edge.Start == null || edge.End == null)
                throw new Exception("Assertion failed: Polygon.AddEdge() called with incomplete edge.");

            // Ignore zero-length edges
            if (edge.Start.Value == edge.End.Value)
                return;

            if (Complete)
                throw new Exception("Assertion failed: Polygon.AddEdge() called when polygon already complete.");

            if (_processedPoints.Count == 0)
            {
                _processedPoints.Add(edge.Start.Value);
                _processedPoints.Add(edge.End.Value);
                return;
            }

            if (!edgeAttach(edge))
            {
                _unprocessedEdges.Add(edge);
                return;
            }

            bool found = true;
            while (found)
            {
                found = false;
                foreach (edge e in _unprocessedEdges)
                {
                    if (edgeAttach(e))
                    {
                        _unprocessedEdges.Remove(e);
                        found = true;
                        break;
                    }
                }
            }

            if (_unprocessedEdges.Count == 0 && _processedPoints[0] == _processedPoints[_processedPoints.Count - 1])
            {
                _processedPoints.RemoveAt(_processedPoints.Count - 1);
                Complete = true;
            }
        }

        private bool edgeAttach(edge Edge)
        {
            if (Edge.Start.Value == _processedPoints[0])
                _processedPoints.Insert(0, Edge.End.Value);
            else if (Edge.End.Value == _processedPoints[0])
                _processedPoints.Insert(0, Edge.Start.Value);
            else if (Edge.Start.Value == _processedPoints[_processedPoints.Count - 1])
                _processedPoints.Add(Edge.End.Value);
            else if (Edge.End.Value == _processedPoints[_processedPoints.Count - 1])
                _processedPoints.Add(Edge.Start.Value);
            else
                return false;

            if (_processedPoints.Count == 3)
            {
                // When we have three points, we can test whether they make a left-turn or a right-turn.
                PointD a = _processedPoints[0], b = _processedPoints[1], c = _processedPoints[2];
                if ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y) < 0)
                {
                    // If they make a left-turn, we want to swap them because
                    // otherwise we end up with a counter-clockwise polygon.
                    _processedPoints[0] = c;
                    _processedPoints[2] = a;
                }
            }

            return true;
        }
    }

    /// <summary>
    ///     Internal class to describe an arc on the beachline (part of Fortune's algorithm to generate Voronoi diagrams)
    ///     (used by RT.Util.VoronoiDiagram).</summary>
    private sealed class arc(site site)
    {
        // The site the arc is associated with. There may be more than one arc for the same site in the Arcs array.
        public site Site = site;

        // The edge that is formed from the breakpoint between this Arc and the next Arc in the Arcs array.
        public edge Edge = null;

        public override string ToString() => "Site = " + Site.ToString();
    }

    /// <summary>
    ///     Internal class to describe a site event (part of Fortune's algorithm to generate Voronoi diagrams) (used by
    ///     RT.Util.VoronoiDiagram).</summary>
    private sealed class siteEvent(site site) : IComparable<siteEvent>
    {
        public site Site = site;

        public override string ToString() => Site.Position.ToString();

        public int CompareTo(siteEvent other)
        {
            if (Site.Position.X < other.Site.Position.X)
                return -1;
            if (Site.Position.X > other.Site.Position.X)
                return 1;
            if (Site.Position.Y < other.Site.Position.Y)
                return -1;
            if (Site.Position.Y > other.Site.Position.Y)
                return 1;
            return 0;
        }
    }

    /// <summary>
    ///     Internal class to describe a circle event (part of Fortune's algorithm to generate Voronoi diagrams) (used by
    ///     RT.Util.VoronoiDiagram).</summary>
    private sealed class circleEvent(double x, PointD center, arc arc) : IComparable<circleEvent>
    {
        public PointD Center = center;
        public double X = x;
        public arc Arc = arc;

        public override string ToString()
        {
            return "(" + X + ", " + Center.Y + ") [" + Center.X + "]";
        }

        public int CompareTo(circleEvent other)
        {
            if (X < other.X)
                return -1;
            if (X > other.X)
                return 1;
            if (Center.Y < other.Center.Y)
                return -1;
            if (Center.Y > other.Center.Y)
                return 1;
            return 0;
        }
    }
}
