using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RT.Util
{
    public class VoronoiDiagram
    {
        public static List<LineSegment> GenerateVoronoiDiagram(PointF[] Sites, SizeF Size)
        {
            VoronoiDiagramData d = new VoronoiDiagramData(Sites, Size.Width, Size.Height);
            return d.AllSegments;
        }
    }

    public class LineSegment
    {
        public PointF Start, End;
        public bool Done;
        public LineSegment(List<LineSegment> AllSegments, PointF nStart) { Start = nStart; End = new PointF(0, 0); Done = false; AllSegments.Add(this); }
        public void Finish(PointF nEnd) { if (Done) return; End = nEnd; Done = true; }
        public override string ToString() { return Start.ToString() + " ==> " + End.ToString(); }
    }

    class VoronoiDiagramData
    {
        public Arc ArcRoot = null;
        public List<SiteEvent> SiteEvents = new List<SiteEvent>();
        public List<CircleEvent> CircleEvents = new List<CircleEvent>();
        public List<LineSegment> AllSegments = new List<LineSegment>();

        public VoronoiDiagramData(PointF[] Sites, float Width, float Height)
        {
            foreach (PointF p in Sites)
            {
                // Create a site event
                SiteEvent SiteEvent = new SiteEvent(new PointF(p.X, p.Y));
                // Add the event in the right place
                int Index = 0;
                while (Index < SiteEvents.Count && SiteEvents[Index].Position.X < p.X)
                    Index++;
                SiteEvents.Insert(Index, SiteEvent);
            }

            // Main loop
            while (SiteEvents.Count > 0)
            {
                if (CircleEvents.Count > 0 && CircleEvents[0].X <= SiteEvents[0].Position.X)
                    ProcessCircleEvent(CircleEvents);
                else
                    ProcessSiteEvent(SiteEvents, CircleEvents);
            }
            while (CircleEvents.Count > 0)
                ProcessCircleEvent(CircleEvents);

            FinishEdges(Width, Height); // Clean up dangling edges.
        }

        private void FinishEdges(float Width, float Height)
        {
            // Advance the sweep line so no parabolas can cross the bounding box.
            float Var = 2 * Width + Height;

            // Extend each remaining segment to the new parabola intersections.
            for (Arc Arc = ArcRoot; Arc.Next != null; Arc = Arc.Next)
                if (Arc.RightSegment != null)
                    Arc.RightSegment.Finish(GetIntersection(Arc.Site, Arc.Next.Site, 2 * Var));
        }

        private void ProcessSiteEvent(List<SiteEvent> SiteEvents, List<CircleEvent> CircleEvents)
        {
            SiteEvent Event = SiteEvents[0];
            SiteEvents.RemoveAt(0);

            if (ArcRoot == null)
            {
                ArcRoot = new Arc(Event.Position, null, null);
                return;
            }

            // Find the current arc(s) at height e.Position.y (if there are any).
            for (Arc Arc = ArcRoot; Arc != null; Arc = Arc.Next)
            {
                PointF Intersect;
                if (DoesIntersect(Event.Position, Arc, out Intersect))
                {
                    PointF Dummy;
                    // New parabola intersects Arc.  If necessary, duplicate Arc.
                    if (Arc.Next != null && !DoesIntersect(Event.Position, Arc.Next, out Dummy))
                    {
                        Arc.Next.Prev = new Arc(Arc.Site, Arc, Arc.Next);
                        Arc.Next = Arc.Next.Prev;
                    }
                    else Arc.Next = new Arc(Arc.Site, Arc, null);
                    Arc.Next.RightSegment = Arc.RightSegment;

                    // Add e.Position between Arc and Arc.Next.
                    Arc.Next.Prev = new Arc(Event.Position, Arc, Arc.Next);
                    Arc.Next = Arc.Next.Prev;

                    Arc = Arc.Next; // Now Arc points to the new arc.

                    // Add new half-edges connected to Arc's endpoints.
                    Arc.Prev.RightSegment = Arc.LeftSegment = new LineSegment(AllSegments, Intersect);
                    Arc.Next.LeftSegment = Arc.RightSegment = new LineSegment(AllSegments, Intersect);

                    // Check for new circle events around the new arc:
                    CheckCircleEvent(CircleEvents, Arc, Event.Position.X);
                    CheckCircleEvent(CircleEvents, Arc.Prev, Event.Position.X);
                    CheckCircleEvent(CircleEvents, Arc.Next, Event.Position.X);

                    return;
                }
            }

            // Special case: If Event.Position never intersects an arc, append it to the list.
            Arc LastArc = ArcRoot;
            while (LastArc.Next != null) LastArc = LastArc.Next;  // Find the last node.
            LastArc.Next = new Arc(Event.Position, LastArc, null);
            // Insert segment between Event.Position and LastArc
            LastArc.RightSegment = LastArc.Next.LeftSegment = new LineSegment(AllSegments, new PointF(0, (LastArc.Next.Site.Y + LastArc.Site.Y) / 2));
        }

        // Will a new parabola at Site intersect with Arc?
        bool DoesIntersect(PointF Site, Arc Arc, out PointF Result)
        {
            Result = new PointF(0, 0);
            if (Arc.Site.X == Site.X)
                return false;

            if ((Arc.Prev == null || GetIntersection(Arc.Prev.Site, Arc.Site, Site.X).Y <= Site.Y) &&
                (Arc.Next == null || Site.Y <= GetIntersection(Arc.Site, Arc.Next.Site, Site.X).Y))
            {
                Result.Y = Site.Y;

                // Plug it back into the parabola equation.
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
                // Use the quadratic formula.
                float z0 = 2 * (SiteA.X - ScanX);
                float z1 = 2 * (SiteB.X - ScanX);

                float a = 1 / z0 - 1 / z1;
                float b = -2 * (SiteA.Y / z0 - SiteB.Y / z1);
                float c = (SiteA.Y * SiteA.Y + SiteA.X * SiteA.X - ScanX * ScanX) / z0
                         - (SiteB.Y * SiteB.Y + SiteB.X * SiteB.X - ScanX * ScanX) / z1;

                Result.Y = (float)(-b - Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            }

            // Plug back into one of the parabola equations.
            Result.X = (p.X * p.X + (p.Y - Result.Y) * (p.Y - Result.Y) - ScanX * ScanX) / (2 * p.X - 2 * ScanX);
            return Result;
        }

        private void ProcessCircleEvent(List<CircleEvent> CircleEvents)
        {
            CircleEvent Event = CircleEvents[0];
            CircleEvents.RemoveAt(0);
            if (!Event.Valid) return;

            // Start a new edge.
            LineSegment LineSeg = new LineSegment(AllSegments, Event.Center);

            // Remove the associated arc from the front.
            Arc Arc = Event.Arc;
            if (Arc.Prev != null)
            {
                Arc.Prev.Next = Arc.Next;
                Arc.Prev.RightSegment = LineSeg;
            }
            if (Arc.Next != null)
            {
                Arc.Next.Prev = Arc.Prev;
                Arc.Next.LeftSegment = LineSeg;
            }

            // Finish the edges before and after Arc.
            if (Arc.LeftSegment != null) Arc.LeftSegment.Finish(Event.Center);
            if (Arc.RightSegment != null) Arc.RightSegment.Finish(Event.Center);

            // Recheck circle events on either side of Arc:
            if (Arc.Prev != null) CheckCircleEvent(CircleEvents, Arc.Prev, Event.X);
            if (Arc.Next != null) CheckCircleEvent(CircleEvents, Arc.Next, Event.X);
        }


        // Look for a new circle event for Arc.
        private void CheckCircleEvent(List<CircleEvent> CircleEvents, Arc Arc, double ScanX)
        {
            // Invalidate any old event.
            if (Arc.Event != null && Arc.Event.X != ScanX)
                Arc.Event.Valid = false;
            Arc.Event = null;

            if (Arc.Prev == null || Arc.Next == null)
                return;

            double MaxX;
            PointF Center;

            if (GetCircle(Arc.Prev.Site, Arc.Site, Arc.Next.Site, out MaxX, out Center) && MaxX > ScanX)
            {
                // Create new event.
                Arc.Event = new CircleEvent(MaxX, Center, Arc);

                // Add the event in the right place
                int Index = 0;
                while (Index < CircleEvents.Count && CircleEvents[Index].X < Arc.Event.X)
                    Index++;
                CircleEvents.Insert(Index, Arc.Event);
            }
        }

        // Find the rightmost point on the circle through a, b, c.
        private bool GetCircle(PointF A, PointF B, PointF C, out double MaxX, out PointF Center)
        {
            MaxX = 0;
            Center = new PointF(0, 0);

            // Check that BC is a "right turn" from AB.
            if ((B.X - A.X) * (C.Y - A.Y) - (C.X - A.X) * (B.Y - A.Y) > 0)
                return false;

            // Algorithm from O'Rourke 2ed p. 189.
            float a = B.X - A.X, b = B.Y - A.Y,
                  c = C.X - A.X, d = C.Y - A.Y,
                  e = a * (A.X + B.X) + b * (A.Y + B.Y),
                  f = c * (A.X + C.X) + d * (A.Y + C.Y),
                  g = 2 * (a * (C.Y - B.Y) - b * (C.X - B.X));

            if (g == 0) return false;  // Points are co-linear.

            // Point o is the center of the circle.
            Center.X = (d * e - b * f) / g;
            Center.Y = (a * f - c * e) / g;

            // o.X plus radius equals max X coordinate.
            MaxX = Center.X + Math.Sqrt(Math.Pow(A.X - Center.X, 2) + Math.Pow(A.Y - Center.Y, 2));
            return true;
        }
    }

    class Arc
    {
        public PointF Site;
        public Arc Prev;
        public Arc Next;
        public CircleEvent Event;
        public LineSegment LeftSegment, RightSegment;
        public Arc(PointF nSite, Arc nPrev, Arc nNext) { Site = nSite; Prev = nPrev; Next = nNext; Event = null; LeftSegment = null; RightSegment = null; }
    }

    class SiteEvent
    {
        public PointF Position;
        public SiteEvent(PointF nPosition) { Position = nPosition; }
    }

    class CircleEvent
    {
        public PointF Center;
        public double X;
        public Arc Arc;
        public bool Valid;
        public CircleEvent(double nX, PointF nCenter, Arc nArc) { X = nX; Center = nCenter; Arc = nArc; Valid = true; }
    }
}