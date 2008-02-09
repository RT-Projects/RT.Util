using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RT.Util.Voronoi
{
    /// <summary>
    /// A class describing a line segment. Used by RT.Util.VoronoiDiagram to describe the resulting Voronoi diagram.
    /// </summary>
    public class Edge
    {
        public PointF? Start, End;
        public PointF SiteA, SiteB;
        public int Pos;
        public Edge(List<Edge> Edges, Dictionary<PointF, List<Edge>> EdgesPerPoint, PointF nSiteA, PointF nSiteB)
        {
            Start = null;
            End = null;
            SiteA = nSiteA;
            SiteB = nSiteB;
            Edges.Add(this);
            if (!EdgesPerPoint.ContainsKey(nSiteA))
                EdgesPerPoint.Add(nSiteA, new List<Edge>());
            EdgesPerPoint[nSiteA].Add(this);
            if (!EdgesPerPoint.ContainsKey(nSiteB))
                EdgesPerPoint.Add(nSiteB, new List<Edge>());
            EdgesPerPoint[nSiteB].Add(this);
        }
        public void SetEndPoint(PointF nEnd)
        {
            if (Start == null)
                Start = nEnd;
            else if (End == null)
                End = nEnd;
        }
        public override string ToString() { return (Start == null ? "?" : Start.Value.ToString()) + " ==> " + (End == null ? "?" : End.ToString()); }
    }

    public class Polygon
    {
        public bool Complete;
        public List<Edge> Edges;
        private List<List<Edge>> IncompleteSegments;
        public Polygon()
        {
            Complete = false;
            Edges = new List<Edge>();
            IncompleteSegments = new List<List<Edge>>();
        }
    }
}

namespace RT.Util
{
    public enum VoronoiDiagramFlags
    {
        REMOVE_DUPLICATES = 1,
        REMOVE_OFFBOUNDS_SITES = 2
    }

    /// <summary>
    /// Static class providing methods for generating Voronoi diagrams from a set of input points.
    /// </summary>
    public class VoronoiDiagram
    {
        /// <summary>
        /// Generates a Voronoi diagram from a set of input points.
        /// </summary>
        /// <param name="Sites">Input points (sites) to generate diagram from.</param>
        /// <param name="Size">Size of the viewport. The origin of the viewport is assumed to be at (0, 0).</param>
        /// <param name="RemoveDuplicates">If true, points (sites) with identical co-ordinates are merged into one.
        /// If false, such duplicates cause an exception.</param>
        /// <returns>A list of line segments describing the Voronoi diagram.</returns>
        public static Tuple<List<Voronoi.Edge>, Dictionary<PointF, List<Voronoi.Edge>>> GenerateVoronoiDiagram(PointF[] Sites, SizeF Size, VoronoiDiagramFlags Flags)
        {
            VoronoiDiagramData d = new VoronoiDiagramData(Sites, Size.Width, Size.Height, Flags);
            return new Tuple<List<Voronoi.Edge>, Dictionary<PointF, List<Voronoi.Edge>>>(d.Edges, d.EdgesPerPoint);
        }

        /// <summary>
        /// Generates a Voronoi diagram from a set of input points.
        /// </summary>
        /// <param name="Sites">Input points (sites) to generate diagram from.
        /// If two points (sites) have identical co-ordinates, an exception is raised.</param>
        /// <param name="Size">Size of the viewport. The origin of the viewport is assumed to be at (0, 0).</param>
        /// <returns>A list of line segments describing the Voronoi diagram.</returns>
        public static List<Voronoi.Edge> GenerateVoronoiDiagram(PointF[] Sites, SizeF Size)
        {
            VoronoiDiagramData d = new VoronoiDiagramData(Sites, Size.Width, Size.Height, 0);
            return d.Edges;
        }
    }

    /// <summary>
    /// Internal class to generate Voronoi diagrams using Fortune's algorithm. Contains internal data structures and methods.
    /// </summary>
    class VoronoiDiagramData
    {
        public List<Arc> Arcs = new List<Arc>();
        public List<SiteEvent> SiteEvents = new List<SiteEvent>();
        public List<CircleEvent> CircleEvents = new List<CircleEvent>();
        public List<Voronoi.Edge> Edges = new List<Voronoi.Edge>();
        public Dictionary<PointF, List<Voronoi.Edge>> EdgesPerPoint = new Dictionary<PointF, List<Voronoi.Edge>>();

        public VoronoiDiagramData(PointF[] Sites, float Width, float Height, VoronoiDiagramFlags Flags)
        {
            foreach (PointF p in Sites)
            {
                if (p.X > 0 && p.Y > 0 && p.X < Width && p.Y < Height)
                {
                    SiteEvent SiteEvent = new SiteEvent(new PointF(p.X, p.Y));
                    SiteEvents.Add(SiteEvent);
                }
                else if ((Flags & VoronoiDiagramFlags.REMOVE_OFFBOUNDS_SITES) == 0)
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
                    if ((Flags & VoronoiDiagramFlags.REMOVE_DUPLICATES) == VoronoiDiagramFlags.REMOVE_DUPLICATES)
                        SiteEvents.RemoveAt(i);
                    else
                        throw new Exception("The input contains two points at the same coordinates " +
                            SiteEvents[i].Position + ". Voronoi diagrams are undefined for such a situation. " +
                            "Use the RT.Util.VoronoiDiagramFlags.REMOVE_DUPLICATES flag to automatically remove such duplicate input points.");
                }
            }

            // Main loop
            while (SiteEvents.Count > 0)
            {
                if (CircleEvents.Count > 0 && CircleEvents[0].X <= SiteEvents[0].Position.X)
                    ProcessCircleEvent();
                else
                    ProcessSiteEvent();
            }
            while (CircleEvents.Count > 0)
                ProcessCircleEvent();

            FinishEdges(Width, Height); // Clean up dangling edges
        }

        private void FinishEdges(float Width, float Height)
        {
            // Advance the sweep line so no parabolas can cross the bounding box
            float Var = 2 * Width + Height;

            // Extend each remaining segment to the new parabola intersections
            for (int i = 0; i < Arcs.Count - 1; i++)
                if (Arcs[i].RightSegment != null)
                    Arcs[i].RightSegment.SetEndPoint(GetIntersection(Arcs[i].Site, Arcs[i + 1].Site, 2 * Var));

            // Now clip all the edges with the bounding rectangle
            foreach (Voronoi.Edge s in Edges)
            {
                if (s.Start.Value.X < 0)
                    s.Start = new PointF(0, s.End.Value.X / (s.End.Value.X - s.Start.Value.X) * (s.Start.Value.Y - s.End.Value.Y) + s.End.Value.Y);
                if (s.Start.Value.Y < 0)
                    s.Start = new PointF(s.End.Value.Y / (s.End.Value.Y - s.Start.Value.Y) * (s.Start.Value.X - s.End.Value.X) + s.End.Value.X, 0);
                if (s.End.Value.X < 0)
                    s.End = new PointF(0, s.Start.Value.X / (s.Start.Value.X - s.End.Value.X) * (s.End.Value.Y - s.Start.Value.Y) + s.Start.Value.Y);
                if (s.End.Value.Y < 0)
                    s.End = new PointF(s.Start.Value.Y / (s.Start.Value.Y - s.End.Value.Y) * (s.End.Value.X - s.Start.Value.X) + s.Start.Value.X, 0);

                if (s.Start.Value.X > Width)
                    s.Start = new PointF(Width, (Width - s.Start.Value.X) / (s.End.Value.X - s.Start.Value.X) * (s.End.Value.Y - s.Start.Value.Y) + s.Start.Value.Y);
                if (s.Start.Value.Y > Height)
                    s.Start = new PointF((Height - s.Start.Value.Y) / (s.End.Value.Y - s.Start.Value.Y) * (s.End.Value.X - s.Start.Value.X) + s.Start.Value.X, Height);
                if (s.End.Value.X > Width)
                    s.End = new PointF(Width, (Width - s.End.Value.X) / (s.Start.Value.X - s.End.Value.X) * (s.Start.Value.Y - s.End.Value.Y) + s.End.Value.Y);
                if (s.End.Value.Y > Height)
                    s.End = new PointF((Height - s.End.Value.Y) / (s.Start.Value.Y - s.End.Value.Y) * (s.Start.Value.X - s.End.Value.X) + s.End.Value.X, Height);
            }
        }

        private void ProcessSiteEvent()
        {
            SiteEvent Event = SiteEvents[0];
            SiteEvents.RemoveAt(0);

            if (Arcs.Count == 0)
            {
                Arcs.Add(new Arc(Event.Position));
                return;
            }

            // Find the current arc(s) at height e.Position.y (if there are any)
            for (int i = 0; i < Arcs.Count; i++)
            {
                PointF Intersect;
                if (DoesIntersect(Event.Position, i, out Intersect))
                {
                    // New parabola intersects Arc - duplicate Arc
                    Arcs.Insert(i + 1, new Arc(Arcs[i].Site));
                    Arcs[i + 1].RightSegment = Arcs[i].RightSegment;

                    // Add a new Arc for Event.Position in the right place
                    Arcs.Insert(i + 1, new Arc(Event.Position));

                    // Add new half-edges connected to Arc's endpoints
                    Arcs[i].RightSegment = Arcs[i + 1].LeftSegment =
                        Arcs[i + 1].RightSegment = Arcs[i + 2].LeftSegment =
                        new Voronoi.Edge(Edges, EdgesPerPoint, Arcs[i + 1].Site, Arcs[i + 2].Site);

                    // Check for new circle events around the new arc:
                    CheckCircleEvent(CircleEvents, i, Event.Position.X);
                    CheckCircleEvent(CircleEvents, i + 2, Event.Position.X);

                    return;
                }
            }

            // Special case: If Event.Position never intersects an arc, append it to the list.
            // This only happens if there is more than one site event with the lowest X co-ordinate.
            Arc LastArc = Arcs[Arcs.Count - 1];
            Arc NewArc = new Arc(Event.Position);
            LastArc.RightSegment = NewArc.LeftSegment = new Voronoi.Edge(Edges, EdgesPerPoint, LastArc.Site, NewArc.Site);
            NewArc.LeftSegment.SetEndPoint(new PointF(0, (NewArc.Site.Y + LastArc.Site.Y) / 2));
            Arcs.Add(NewArc);
        }

        // Will a new parabola at Site intersect with the arc at ArcIndex?
        bool DoesIntersect(PointF Site, int ArcIndex, out PointF Result)
        {
            Arc Arc = Arcs[ArcIndex];

            Result = new PointF(0, 0);
            if (Arc.Site.X == Site.X)
                return false;

            if ((ArcIndex == 0 || GetIntersection(Arcs[ArcIndex - 1].Site, Arc.Site, Site.X).Y <= Site.Y) &&
                (ArcIndex == Arcs.Count - 1 || Site.Y <= GetIntersection(Arc.Site, Arcs[ArcIndex + 1].Site, Site.X).Y))
            {
                Result.Y = Site.Y;

                // Plug it back into the parabola equation
                Result.X = (Arc.Site.X * Arc.Site.X + (Arc.Site.Y - Result.Y) * (Arc.Site.Y - Result.Y) - Site.X * Site.X)
                          / (2 * Arc.Site.X - 2 * Site.X);

                return true;
            }
            return false;
        }

        // Where do two parabolas intersect?
        PointF GetIntersection(PointF SiteA, PointF SiteB, float ScanX)
        {
            PointF Result = new PointF();
            PointF p = SiteA;

            if (SiteA.X == SiteB.X)
                Result.Y = (SiteA.Y + SiteB.Y) / 2;
            else if (SiteB.X == ScanX)
                Result.Y = SiteB.Y;
            else if (SiteA.X == ScanX)
            {
                Result.Y = SiteA.Y;
                p = SiteB;
            }
            else
            {
                // Use the quadratic formula
                float z0 = 2 * (SiteA.X - ScanX);
                float z1 = 2 * (SiteB.X - ScanX);

                float a = 1 / z0 - 1 / z1;
                float b = -2 * (SiteA.Y / z0 - SiteB.Y / z1);
                float c = (SiteA.Y * SiteA.Y + SiteA.X * SiteA.X - ScanX * ScanX) / z0
                         - (SiteB.Y * SiteB.Y + SiteB.X * SiteB.X - ScanX * ScanX) / z1;

                Result.Y = (float)(-b - Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            }

            // Plug back into one of the parabola equations
            Result.X = (p.X * p.X + (p.Y - Result.Y) * (p.Y - Result.Y) - ScanX * ScanX) / (2 * p.X - 2 * ScanX);
            return Result;
        }

        private void ProcessCircleEvent()
        {
            CircleEvent Event = CircleEvents[0];
            CircleEvents.RemoveAt(0);
            int ArcIndex = Arcs.IndexOf(Event.Arc);
            if (ArcIndex == -1) return;

            // Start a new edge
            Voronoi.Edge LineSeg = new Voronoi.Edge(Edges, EdgesPerPoint, Arcs[ArcIndex - 1].Site, Arcs[ArcIndex + 1].Site);
            LineSeg.SetEndPoint(Event.Center);

            // The arcs before and after the one that disappears are now responsible for the new edge
            if (ArcIndex > 0)
                Arcs[ArcIndex - 1].RightSegment = LineSeg;
            if (ArcIndex < Arcs.Count - 1)
                Arcs[ArcIndex + 1].LeftSegment = LineSeg;

            // Remove the arc from the beachline
            Arcs.RemoveAt(ArcIndex);

            // The two edges corresponding to the disappearing arc end here
            if (Event.Arc.LeftSegment != null) Event.Arc.LeftSegment.SetEndPoint(Event.Center);
            if (Event.Arc.RightSegment != null) Event.Arc.RightSegment.SetEndPoint(Event.Center);

            // Recheck circle events on either side of the disappearing arc
            if (ArcIndex > 0)
                CheckCircleEvent(CircleEvents, ArcIndex - 1, Event.X);
            if (ArcIndex < Arcs.Count)  // remember that ArcIndex now points to the arc after the one that disappeared
                CheckCircleEvent(CircleEvents, ArcIndex, Event.X);
        }

        // Look for a new circle event for the arc at ArcIndex
        private void CheckCircleEvent(List<CircleEvent> CircleEvents, int ArcIndex, float ScanX)
        {
            if (ArcIndex == 0 || ArcIndex == Arcs.Count - 1)
                return;

            Arc Arc = Arcs[ArcIndex];
            float MaxX;
            PointF Center;

            if (GetCircle(Arcs[ArcIndex - 1].Site, Arc.Site, Arcs[ArcIndex + 1].Site, out Center, out MaxX)/* && MaxX >= ScanX*/)
            {
                // Add the new event in the right place using binary search
                int Low = 0;
                int High = CircleEvents.Count;
                while (Low < High)
                {
                    int Middle = (Low + High) / 2;
                    CircleEvent Event = CircleEvents[Middle];
                    if (Event.X < MaxX || (Event.X == MaxX && Event.Center.Y < Center.Y))
                        Low = Middle + 1;
                    else
                        High = Middle;
                }
                CircleEvents.Insert(Low, new CircleEvent(MaxX, Center, Arc));
            }
        }

        // Find the circle through points A, B, C
        private bool GetCircle(PointF A, PointF B, PointF C, out PointF Center, out float MaxX)
        {
            MaxX = 0;
            Center = new PointF(0, 0);

            // Check that BC is a "right turn" from AB
            if ((B.X - A.X) * (C.Y - A.Y) - (C.X - A.X) * (B.Y - A.Y) > 0)
                return false;

            // Algorithm from O'Rourke 2ed p. 189.
            float a = B.X - A.X, b = B.Y - A.Y,
                  c = C.X - A.X, d = C.Y - A.Y,
                  e = a * (A.X + B.X) + b * (A.Y + B.Y),
                  f = c * (A.X + C.X) + d * (A.Y + C.Y),
                  g = 2 * (a * (C.Y - B.Y) - b * (C.X - B.X));

            if (g == 0) return false;  // Points are co-linear.

            Center.X = (d * e - b * f) / g;
            Center.Y = (a * f - c * e) / g;

            // MaxX = Center.X + radius of the circle
            MaxX = Center.X + (float)Math.Sqrt(Math.Pow(A.X - Center.X, 2) + Math.Pow(A.Y - Center.Y, 2));
            return true;
        }
    }

    /// <summary>
    /// Internal class to describe an arc on the beachline (part of Fortune's algorithm to generate Voronoi diagrams) (used by RT.Util.VoronoiDiagram).
    /// </summary>
    class Arc
    {
        public PointF Site;
        public Voronoi.Edge LeftSegment, RightSegment;
        public Arc(PointF nSite) { Site = nSite; LeftSegment = null; RightSegment = null; }
        public override string ToString()
        {
            return "Site = " + Site.ToString();
        }
    }

    /// <summary>
    /// Internal class to describe a site event (part of Fortune's algorithm to generate Voronoi diagrams) (used by RT.Util.VoronoiDiagram).
    /// </summary>
    class SiteEvent : IComparable<SiteEvent>
    {
        public PointF Position;
        public SiteEvent(PointF nPosition) { Position = nPosition; }
        public override string ToString()
        {
            return Position.ToString();
        }

        public int CompareTo(SiteEvent other)
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
    class CircleEvent : IComparable<CircleEvent>
    {
        public PointF Center;
        public float X;
        public Arc Arc;
        public CircleEvent(float nX, PointF nCenter, Arc nArc) { X = nX; Center = nCenter; Arc = nArc; }
        public override string ToString()
        {
            return "(" + X + ", " + Center.Y + ") [" + Center.X + "]";
        }

        public int CompareTo(CircleEvent other)
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