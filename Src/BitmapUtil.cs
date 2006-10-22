using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace RT.Util
{
    public class BitmapUtil
    {
        public static void DrawImageAlpha(Graphics g, Image Image, Rectangle DestRect, float Opacity)
        {
            ColorMatrix ColorMatrix = new ColorMatrix(new float[][] {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, Opacity, 0},
                new float[] {0, 0, 0, 0, 1}
            });
            ImageAttributes ImageAttributes = new ImageAttributes();
            ImageAttributes.SetColorMatrix(ColorMatrix,
                ColorMatrixFlag.Default,
                ColorAdjustType.Bitmap);
            g.DrawImage(Image, DestRect, 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, ImageAttributes);
        }

        public static Point[][] BoolsToPaths(Virtual2DArray<bool> Input)
        {
            List<List<Point>> ActiveSegments = new List<List<Point>>();
            List<Point[]> CompletedPaths = new List<Point[]>();
            for (int y = 0; y < Input.Height-1; y++)
            {
                List<RTUtilPathEvent> Events = FindEvents(ActiveSegments, Input, y);
                for (int i = 0; i < Events.Count; i += 2)
                {
                    if (Events[i] is RTUtilPathEventSegment && Events[i+1] is RTUtilPathEventSegment)
                    {
                        int Index1 = ((RTUtilPathEventSegment) Events[i]).SegmentIndex;
                        int Index2 = ((RTUtilPathEventSegment) Events[i+1]).SegmentIndex;
                        bool Start = ((RTUtilPathEventSegment) Events[i]).StartOfSegment;
                        if (Index1 == Index2 && Start)
                        {
                            // A segment becomes a closed path
                            ActiveSegments[Index2].Add(new Point(Events[i+1].X, y));
                            ActiveSegments[Index2].Add(new Point(Events[i].X, y));
                            CompletedPaths.Add(ActiveSegments[Index2].ToArray());
                        }
                        else if (Index1 == Index2)
                        {
                            // A segment becomes a closed path
                            ActiveSegments[Index2].Add(new Point(Events[i].X, y));
                            ActiveSegments[Index2].Add(new Point(Events[i+1].X, y));
                            CompletedPaths.Add(ActiveSegments[Index2].ToArray());
                        }
                        else if (Start)
                        {
                            // Two segments join up
                            ActiveSegments[Index2].Add(new Point(Events[i+1].X, y));
                            ActiveSegments[Index2].Add(new Point(Events[i].X, y));
                            ActiveSegments[Index1].InsertRange(0, ActiveSegments[Index2]);
                        }
                        else
                        {
                            // Two segments join up
                            ActiveSegments[Index1].Add(new Point(Events[i].X, y));
                            ActiveSegments[Index1].Add(new Point(Events[i+1].X, y));
                            ActiveSegments[Index1].AddRange(ActiveSegments[Index2]);
                        }
                        ActiveSegments.RemoveAt(Index2);
                        for (int Correction = i+2; Correction < Events.Count; Correction++)
                        {
                            if (Events[Correction] is RTUtilPathEventSegment &&
                                (Events[Correction] as RTUtilPathEventSegment).SegmentIndex == Index2)
                                (Events[Correction] as RTUtilPathEventSegment).SegmentIndex = Index1;
                            if (Events[Correction] is RTUtilPathEventSegment &&
                                (Events[Correction] as RTUtilPathEventSegment).SegmentIndex > Index2)
                                (Events[Correction] as RTUtilPathEventSegment).SegmentIndex--;
                        }
                    }
                    else if (Events[i] is RTUtilPathEventChange && Events[i+1] is RTUtilPathEventChange)
                    {
                        // Both events are changes - create a new segment
                        ActiveSegments.Add(new List<Point>(new Point[] { 
                            new Point (Events[Input.Get(Events[i].X, y) ? i : i+1].X,y),
                            new Point (Events[Input.Get(Events[i].X, y) ? i+1 : i].X,y)
                        }));
                    }
                    else if (Events[i] is RTUtilPathEventSegment) // ... && Events[i+1] is RTUtilPathEventChange
                    {
                        RTUtilPathEventSegment Ev = Events[i] as RTUtilPathEventSegment;
                        if (Ev.StartOfSegment)
                        {
                            ActiveSegments[Ev.SegmentIndex].Insert(0, new Point(Ev.X, y));
                            if (Ev.X != Events[i+1].X)
                                ActiveSegments[Ev.SegmentIndex].Insert(0, new Point(Events[i+1].X, y));
                        }
                        else
                        {
                            ActiveSegments[Ev.SegmentIndex].Add(new Point(Ev.X, y));
                            if (Ev.X != Events[i+1].X)
                                ActiveSegments[Ev.SegmentIndex].Add(new Point(Events[i+1].X, y));
                        }
                    }
                    else  // ... Events[i] is RTUtilPathEventChange && Events[i+1] is RTUtilPathEventSegment
                    {
                        RTUtilPathEventSegment Ev = Events[i+1] as RTUtilPathEventSegment;
                        if (Ev.StartOfSegment)
                        {
                            ActiveSegments[Ev.SegmentIndex].Insert(0, new Point(Ev.X, y));
                            if (Ev.X != Events[i].X)
                                ActiveSegments[Ev.SegmentIndex].Insert(0, new Point(Events[i].X, y));
                        }
                        else
                        {
                            ActiveSegments[Ev.SegmentIndex].Add(new Point(Ev.X, y));
                            if (Ev.X != Events[i].X)
                                ActiveSegments[Ev.SegmentIndex].Add(new Point(Events[i].X, y));
                        }
                    }
                }
            }
            return CompletedPaths.ToArray();
        }

        private static List<RTUtilPathEvent> FindEvents(List<List<Point>> ActiveSegments,
            Virtual2DArray<bool> Input, int y)
        {
            List<RTUtilPathEvent> Results = new List<RTUtilPathEvent>();

            // First add all the validity change events in the correct order
            for (int x = 1; x < Input.Width; x++)
                if (Input.Get(x, y) != Input.Get(x - 1, y))
                    Results.Add(new RTUtilPathEventChange(x));

            // Now insert the segment events in the right places
            for (int i = 0; i < ActiveSegments.Count; i++)
            {
                int Index = 0;
                while (Index < Results.Count && Results[Index].X <= ActiveSegments[i][0].X)
                    Index++;
                Results.Insert(Index, new RTUtilPathEventSegment(i, true, ActiveSegments[i][0].X));
                Index = 0;
                while (Index < Results.Count && Results[Index].X < ActiveSegments[i][ActiveSegments[i].Count-1].X)
                    Index++;
                Results.Insert(Index, new RTUtilPathEventSegment(i, false, ActiveSegments[i][ActiveSegments[i].Count-1].X));
            }
            return Results;
        }
        private abstract class RTUtilPathEvent
        {
            public int X;
        }
        private class RTUtilPathEventSegment : RTUtilPathEvent
        {
            public int SegmentIndex;
            public bool StartOfSegment;
            public RTUtilPathEventSegment(int Index, bool Start, int NewX)
            {
                SegmentIndex = Index;
                StartOfSegment = Start;
                X = NewX;
            }
        }
        private class RTUtilPathEventChange : RTUtilPathEvent
        {
            public RTUtilPathEventChange(int NewX)
            {
                X = NewX;
            }
        }
    }
}
