using System;
using System.Collections.Generic;
using System.Drawing;
using RT.Util.Collections;
using RT.Util.Geometry;

namespace RT.KitchenSink.Geometry
{
    /// <summary>
    /// Provides values to specify options on the <see cref="VoronoiDiagram.GenerateVoronoiDiagram(PointD[], SizeF, VoronoiDiagramFlags)"/> method.
    /// </summary>
    [Flags]
    public enum VoronoiDiagramFlags
    {
        /// <summary>Indicates that duplicate sites (points) should be removed from the input.</summary>
        RemoveDuplicates = 1,
        /// <summary>Indicates that input sites (points) that lie outside the bounds of the viewport should be ignored.</summary>
        RemoveOffboundsSites = 2
    }

    /// <summary>
    /// Static class providing methods for generating Voronoi diagrams from a set of input points.
    /// </summary>
    public static class VoronoiDiagram
    {
        /// <summary>
        /// Generates a Voronoi diagram from a set of input points.
        /// </summary>
        /// <param name="sites">Input points (sites) to generate diagram from.</param>
        /// <param name="size">Size of the viewport. The origin of the viewport is assumed to be at (0, 0).</param>
        /// <param name="flags">Set of <see cref="VoronoiDiagramFlags"/> values that specifies additional options.</param>
        /// <returns>A list of line segments describing the Voronoi diagram.</returns>
        public static Tuple<List<EdgeD>, Dictionary<PointD, PolygonD>> GenerateVoronoiDiagram(PointD[] sites, SizeF size, VoronoiDiagramFlags flags)
        {
            data d = new data(sites, size.Width, size.Height, flags);

            var ret = new Tuple<List<EdgeD>, Dictionary<PointD, PolygonD>>();
            ret.E1 = new List<EdgeD>();
            foreach (edge e in d.Edges)
                ret.E1.Add(new EdgeD(e.Start.Value, e.End.Value));
            ret.E2 = new Dictionary<PointD, PolygonD>();
            foreach (KeyValuePair<PointD, polygon> kvp in d.Polygons)
            {
                PolygonD Poly = kvp.Value.ToPolygonD();
                if (Poly != null)
                    ret.E2.Add(kvp.Key, Poly);
            }
            return ret;
        }

        /// <summary>
        /// Generates a Voronoi diagram from a set of input points.
        /// </summary>
        /// <param name="sites">Input points (sites) to generate diagram from.
        /// If two points (sites) have identical co-ordinates, an exception is raised.</param>
        /// <param name="size">Size of the viewport. The origin of the viewport is assumed to be at (0, 0).</param>
        /// <returns>A list of line segments describing the Voronoi diagram.</returns>
        public static Tuple<List<EdgeD>, Dictionary<PointD, PolygonD>> GenerateVoronoiDiagram(PointD[] sites, SizeF size)
        {
            return GenerateVoronoiDiagram(sites, size, 0);
        }

        /// <summary>
        /// Internal class to generate Voronoi diagrams using Fortune's algorithm. Contains internal data structures and methods.
        /// </summary>
        private class data
        {
            public List<arc> Arcs = new List<arc>();
            public List<siteEvent> SiteEvents = new List<siteEvent>();
            public List<circleEvent> CircleEvents = new List<circleEvent>();
            public List<edge> Edges = new List<edge>();
            public Dictionary<PointD, polygon> Polygons = new Dictionary<PointD, polygon>();

            public data(PointD[] sites, float width, float height, VoronoiDiagramFlags flags)
            {
                foreach (PointD p in sites)
                {
                    if (p.X > 0 && p.Y > 0 && p.X < width && p.Y < height)
                        SiteEvents.Add(new siteEvent(p));
                    else if ((flags & VoronoiDiagramFlags.RemoveOffboundsSites) == 0)
                        throw new Exception("The input contains a point outside the bounds or on the perimeter (coordinates " +
                            p + "). This case is not handled by this algorithm. Use the RT.Util.VoronoiDiagramFlags.REMOVE_OFFBOUNDS_SITES " +
                            "flag to automatically remove such off-bounds input points.");
                }
                SiteEvents.Sort();

                // Make sure there are no two equal points in the input
                for (int i = 1; i < SiteEvents.Count; i++)
                {
                    while (i < SiteEvents.Count && SiteEvents[i - 1].Position == SiteEvents[i].Position)
                    {
                        if ((flags & VoronoiDiagramFlags.RemoveDuplicates) == VoronoiDiagramFlags.RemoveDuplicates)
                            SiteEvents.RemoveAt(i);
                        else
                            throw new Exception("The input contains two points at the same coordinates " +
                                SiteEvents[i].Position + ". Voronoi diagrams are undefined for such a situation. " +
                                "Use the RT.Util.VoronoiDiagramFlags.REMOVE_DUPLICATES flag to automatically remove such duplicate input points.");
                    }
                }

                // Main loop
                while (SiteEvents.Count > 0 || CircleEvents.Count > 0)
                {
                    if (CircleEvents.Count > 0 && (SiteEvents.Count == 0 || CircleEvents[0].X <= SiteEvents[0].Position.X))
                    {
                        // Process a circle event
                        circleEvent evt = CircleEvents[0];
                        CircleEvents.RemoveAt(0);
                        int arcIndex = Arcs.IndexOf(evt.Arc);
                        if (arcIndex == -1) continue;

                        // The two edges left and right of the disappearing arc end here
                        if (Arcs[arcIndex - 1].Edge != null)
                            Arcs[arcIndex - 1].Edge.SetEndPoint(evt.Center);
                        if (evt.Arc.Edge != null)
                            evt.Arc.Edge.SetEndPoint(evt.Center);

                        // Remove the arc from the beachline
                        Arcs.RemoveAt(arcIndex);
                        // ArcIndex now points to the arc after the one that disappeared

                        // Start a new edge at the point where the other two edges ended
                        Arcs[arcIndex - 1].Edge = new edge(Arcs[arcIndex - 1].Site, Arcs[arcIndex].Site);
                        Arcs[arcIndex - 1].Edge.SetEndPoint(evt.Center);
                        Edges.Add(Arcs[arcIndex - 1].Edge);

                        // Recheck circle events on either side of the disappearing arc
                        if (arcIndex > 0)
                            checkCircleEvent(CircleEvents, arcIndex - 1, evt.X);
                        if (arcIndex < Arcs.Count)
                            checkCircleEvent(CircleEvents, arcIndex, evt.X);
                    }
                    else
                    {
                        // Process a site event
                        siteEvent evt = SiteEvents[0];
                        SiteEvents.RemoveAt(0);

                        if (Arcs.Count == 0)
                        {
                            Arcs.Add(new arc(evt.Position));
                            continue;
                        }

                        // Find the current arc(s) at height e.Position.y (if there are any)
                        bool arcFound = false;
                        for (int i = 0; i < Arcs.Count; i++)
                        {
                            PointD intersect;
                            if (doesIntersect(evt.Position, i, out intersect))
                            {
                                // New parabola intersects Arc - duplicate Arc
                                Arcs.Insert(i + 1, new arc(Arcs[i].Site));
                                Arcs[i + 1].Edge = Arcs[i].Edge;

                                // Add a new Arc for Event.Position in the right place
                                Arcs.Insert(i + 1, new arc(evt.Position));

                                // Add new half-edges connected to Arc's endpoints
                                Arcs[i].Edge = Arcs[i + 1].Edge = new edge(Arcs[i + 1].Site, Arcs[i + 2].Site);
                                Edges.Add(Arcs[i].Edge);

                                // Check for new circle events around the new arc:
                                checkCircleEvent(CircleEvents, i, evt.Position.X);
                                checkCircleEvent(CircleEvents, i + 2, evt.Position.X);

                                arcFound = true;
                                break;
                            }
                        }

                        if (arcFound)
                            continue;

                        // Special case: If Event.Position never intersects an arc, append it to the list.
                        // This only happens if there is more than one site event with the lowest X co-ordinate.
                        arc lastArc = Arcs[Arcs.Count - 1];
                        arc newArc = new arc(evt.Position);
                        lastArc.Edge = new edge(lastArc.Site, newArc.Site);
                        Edges.Add(lastArc.Edge);
                        lastArc.Edge.SetEndPoint(new PointD(0, (newArc.Site.Y + lastArc.Site.Y) / 2));
                        Arcs.Add(newArc);
                    }
                }

                // Advance the sweep line so no parabolas can cross the bounding box
                double var = 2 * width + height;

                // Extend each remaining edge to the new parabola intersections
                for (int i = 0; i < Arcs.Count - 1; i++)
                    if (Arcs[i].Edge != null)
                        Arcs[i].Edge.SetEndPoint(getIntersection(Arcs[i].Site, Arcs[i + 1].Site, 2 * var));

                // Clip all the edges with the bounding rectangle
                foreach (edge s in Edges)
                {
                    if (s.Start.Value.X < 0)
                        s.Start = new PointD(0, s.End.Value.X / (s.End.Value.X - s.Start.Value.X) * (s.Start.Value.Y - s.End.Value.Y) + s.End.Value.Y);
                    if (s.Start.Value.Y < 0)
                        s.Start = new PointD(s.End.Value.Y / (s.End.Value.Y - s.Start.Value.Y) * (s.Start.Value.X - s.End.Value.X) + s.End.Value.X, 0);
                    if (s.End.Value.X < 0)
                        s.End = new PointD(0, s.Start.Value.X / (s.Start.Value.X - s.End.Value.X) * (s.End.Value.Y - s.Start.Value.Y) + s.Start.Value.Y);
                    if (s.End.Value.Y < 0)
                        s.End = new PointD(s.Start.Value.Y / (s.Start.Value.Y - s.End.Value.Y) * (s.End.Value.X - s.Start.Value.X) + s.Start.Value.X, 0);

                    if (s.Start.Value.X > width)
                        s.Start = new PointD(width, (width - s.Start.Value.X) / (s.End.Value.X - s.Start.Value.X) * (s.End.Value.Y - s.Start.Value.Y) + s.Start.Value.Y);
                    if (s.Start.Value.Y > height)
                        s.Start = new PointD((height - s.Start.Value.Y) / (s.End.Value.Y - s.Start.Value.Y) * (s.End.Value.X - s.Start.Value.X) + s.Start.Value.X, height);
                    if (s.End.Value.X > width)
                        s.End = new PointD(width, (width - s.End.Value.X) / (s.Start.Value.X - s.End.Value.X) * (s.Start.Value.Y - s.End.Value.Y) + s.End.Value.Y);
                    if (s.End.Value.Y > height)
                        s.End = new PointD((height - s.End.Value.Y) / (s.Start.Value.Y - s.End.Value.Y) * (s.Start.Value.X - s.End.Value.X) + s.End.Value.X, height);
                }

                // Generate polygons from the edges
                foreach (edge e in Edges)
                {
                    if (!Polygons.ContainsKey(e.SiteA))
                        Polygons.Add(e.SiteA, new polygon(e.SiteA));
                    Polygons[e.SiteA].AddEdge(e);
                    if (!Polygons.ContainsKey(e.SiteB))
                        Polygons.Add(e.SiteB, new polygon(e.SiteB));
                    Polygons[e.SiteB].AddEdge(e);
                }
            }

            // Will a new parabola at Site intersect with the arc at ArcIndex?
            private bool doesIntersect(PointD site, int arcIndex, out PointD result)
            {
                arc arc = Arcs[arcIndex];

                result = new PointD(0, 0);
                if (arc.Site.X == site.X)
                    return false;

                if ((arcIndex == 0 || getIntersection(Arcs[arcIndex - 1].Site, arc.Site, site.X).Y <= site.Y) &&
                    (arcIndex == Arcs.Count - 1 || site.Y <= getIntersection(arc.Site, Arcs[arcIndex + 1].Site, site.X).Y))
                {
                    result.Y = site.Y;

                    // Plug it back into the parabola equation
                    result.X = (arc.Site.X * arc.Site.X + (arc.Site.Y - result.Y) * (arc.Site.Y - result.Y) - site.X * site.X)
                              / (2 * arc.Site.X - 2 * site.X);

                    return true;
                }
                return false;
            }

            // Where do two parabolas intersect?
            private PointD getIntersection(PointD siteA, PointD siteB, double scanX)
            {
                PointD result = new PointD();
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
            private void checkCircleEvent(List<circleEvent> circleEvents, int arcIndex, double scanX)
            {
                if (arcIndex == 0 || arcIndex == Arcs.Count - 1)
                    return;

                arc arc = Arcs[arcIndex];
                double maxX;
                PointD center;

                if (getCircle(Arcs[arcIndex - 1].Site, arc.Site, Arcs[arcIndex + 1].Site, out center, out maxX)/* && MaxX >= ScanX*/)
                {
                    // Add the new event in the right place using binary search
                    int low = 0;
                    int high = circleEvents.Count;
                    while (low < high)
                    {
                        int middle = (low + high) / 2;
                        circleEvent evt = circleEvents[middle];
                        if (evt.X < maxX || (evt.X == maxX && evt.Center.Y < center.Y))
                            low = middle + 1;
                        else
                            high = middle;
                    }
                    circleEvents.Insert(low, new circleEvent(maxX, center, arc));
                }
            }

            // Find the circle through points p1, p2, p3
            private bool getCircle(PointD p1, PointD p2, PointD p3, out PointD center, out double maxX)
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

        /// <summary>
        /// Internal class describing an edge in the Voronoi diagram. May be incomplete as the algorithm progresses.
        /// </summary>
        private class edge
        {
            public PointD? Start, End;
            public PointD SiteA, SiteB;
            public edge(PointD siteA, PointD siteB)
            {
                Start = null;
                End = null;
                SiteA = siteA;
                SiteB = siteB;
            }
            public void SetEndPoint(PointD end)
            {
                if (Start == null)
                    Start = end;
                else if (End == null)
                    End = end;
            }
            public override string ToString() { return (Start == null ? "?" : Start.Value.ToString()) + " ==> " + (End == null ? "?" : End.ToString()); }
        }

        /// <summary>
        /// Internal class describing a polygon in the Voronoi diagram. May be incomplete as the algorithm progresses.
        /// </summary>
        private class polygon
        {
            public bool Complete;
            public PointD Site;

            private List<PointD> _processedPoints;
            private List<edge> _unprocessedEdges;

            public polygon(PointD site)
            {
                Site = site;
                Complete = false;
                _processedPoints = new List<PointD>();
                _unprocessedEdges = new List<edge>();
            }

            public PolygonD ToPolygonD()
            {
                if (!Complete)
                    return null;
                return new PolygonD(_processedPoints);
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
        /// Internal class to describe an arc on the beachline (part of Fortune's algorithm to generate Voronoi diagrams) (used by RT.Util.VoronoiDiagram).
        /// </summary>
        private class arc
        {
            // The site the arc is associated with. There may be more than one arc for the same site in the Arcs array.
            public PointD Site;

            // The edge that is formed from the breakpoint between this Arc and the next Arc in the Arcs array.
            public edge Edge;

            public arc(PointD site)
            {
                Site = site; Edge = null;
            }

            public override string ToString()
            {
                return "Site = " + Site.ToString();
            }
        }

        /// <summary>
        /// Internal class to describe a site event (part of Fortune's algorithm to generate Voronoi diagrams) (used by RT.Util.VoronoiDiagram).
        /// </summary>
        private class siteEvent : IComparable<siteEvent>
        {
            public PointD Position;
            public siteEvent(PointD nPosition) { Position = nPosition; }
            public override string ToString()
            {
                return Position.ToString();
            }

            public int CompareTo(siteEvent other)
            {
                if (Position.X < other.Position.X)
                    return -1;
                if (Position.X > other.Position.X)
                    return 1;
                if (Position.Y < other.Position.Y)
                    return -1;
                if (Position.Y > other.Position.Y)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// Internal class to describe a circle event (part of Fortune's algorithm to generate Voronoi diagrams) (used by RT.Util.VoronoiDiagram).
        /// </summary>
        private class circleEvent : IComparable<circleEvent>
        {
            public PointD Center;
            public double X;
            public arc Arc;
            public circleEvent(double x, PointD center, arc arc)
            {
                X = x;
                Center = center;
                Arc = arc;
            }

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
}