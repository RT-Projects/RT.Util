using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using RT.Util.Collections;

namespace RT.Util.Drawing
{
    /// <summary>
    /// Contains static methods for various graphics-related operations.
    /// </summary>
    public static class GraphicsUtil
    {
        /// <summary>
        /// Blends the specified colors together. Amount specifies how much
        /// of the Color to keep, "on top of" the BackColor.
        /// </summary>
        public static Color ColorBlend(Color Color, Color BackColor, double Amount)
        {
            byte R = (byte) ((Color.R * Amount) + BackColor.R * (1 - Amount));
            byte G = (byte) ((Color.G * Amount) + BackColor.G * (1 - Amount));
            byte B = (byte) ((Color.B * Amount) + BackColor.B * (1 - Amount));
            return Color.FromArgb(R, G, B);
        }

        /// <summary>
        /// Draws the specified Image into the destination rectangle DestRect of the Graphics object g using the specified Opacity.
        /// </summary>
        /// <param name="g">Graphics object to alpha-blend the image onto.</param>
        /// <param name="Image">Image to draw.</param>
        /// <param name="DestRect">Destination rectangle within the target Graphics canvas.</param>
        /// <param name="Opacity">Opacity level to use when drawing the image. 0 means nothing changes.
        /// 1 means the image is drawn normally. 0.5 means a 50% blend between source and destination.</param>
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

        /// <summary>Given a two-dimensional array of booleans, generates the "outline" of the region described by the booleans set to true.
        /// If there are several disjoint regions, several separate outlines are generated.</summary>
        /// <param name="Input">The input array of booleans to generate the outline from.</param>
        /// <returns>An array of paths, where each path is an array of points. The co-ordinates of the points are the indexes in the input array.</returns>
        /// <example>An input array full of booleans set to false generates an empty output array.
        /// 
        /// An input array full of booleans set to true generates a single output path which describes the complete rectangle.</example>
        public static Point[][] BoolsToPaths(Virtual2DArray<bool> Input)
        {
            List<List<Point>> ActiveSegments = new List<List<Point>>();
            List<Point[]> CompletedPaths = new List<Point[]>();
            for (int y = 0; y <= Input.Height; y++)
            {
                List<RTUtilPathEvent> Events = FindEvents(ActiveSegments, Input, y);
                for (int i = 0; i < Events.Count; i += 2)
                {
                    if (Events[i] is RTUtilPathEventSegment && Events[i + 1] is RTUtilPathEventSegment)
                    {
                        int Index1 = ((RTUtilPathEventSegment) Events[i]).SegmentIndex;
                        int Index2 = ((RTUtilPathEventSegment) Events[i + 1]).SegmentIndex;
                        bool Start = ((RTUtilPathEventSegment) Events[i]).StartOfSegment;
                        if (Index1 == Index2 && Start)
                        {
                            // A segment becomes a closed path
                            ActiveSegments[Index2].Add(new Point(Events[i + 1].X, y));
                            ActiveSegments[Index2].Add(new Point(Events[i].X, y));
                            CompletedPaths.Add(ActiveSegments[Index2].ToArray());
                        }
                        else if (Index1 == Index2)
                        {
                            // A segment becomes a closed path
                            ActiveSegments[Index2].Add(new Point(Events[i].X, y));
                            ActiveSegments[Index2].Add(new Point(Events[i + 1].X, y));
                            CompletedPaths.Add(ActiveSegments[Index2].ToArray());
                        }
                        else if (Start)
                        {
                            // Two segments join up
                            ActiveSegments[Index2].Add(new Point(Events[i + 1].X, y));
                            ActiveSegments[Index2].Add(new Point(Events[i].X, y));
                            ActiveSegments[Index1].InsertRange(0, ActiveSegments[Index2]);
                        }
                        else
                        {
                            // Two segments join up
                            ActiveSegments[Index1].Add(new Point(Events[i].X, y));
                            ActiveSegments[Index1].Add(new Point(Events[i + 1].X, y));
                            ActiveSegments[Index1].AddRange(ActiveSegments[Index2]);
                        }
                        ActiveSegments.RemoveAt(Index2);
                        for (int Correction = i + 2; Correction < Events.Count; Correction++)
                        {
                            if (Events[Correction] is RTUtilPathEventSegment &&
                                (Events[Correction] as RTUtilPathEventSegment).SegmentIndex == Index2)
                                (Events[Correction] as RTUtilPathEventSegment).SegmentIndex = Index1;
                            if (Events[Correction] is RTUtilPathEventSegment &&
                                (Events[Correction] as RTUtilPathEventSegment).SegmentIndex > Index2)
                                (Events[Correction] as RTUtilPathEventSegment).SegmentIndex--;
                        }
                    }
                    else if (Events[i] is RTUtilPathEventChange && Events[i + 1] is RTUtilPathEventChange)
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
                            if (Ev.X != Events[i + 1].X)
                                ActiveSegments[Ev.SegmentIndex].Insert(0, new Point(Events[i + 1].X, y));
                        }
                        else
                        {
                            ActiveSegments[Ev.SegmentIndex].Add(new Point(Ev.X, y));
                            if (Ev.X != Events[i + 1].X)
                                ActiveSegments[Ev.SegmentIndex].Add(new Point(Events[i + 1].X, y));
                        }
                    }
                    else  // ... Events[i] is RTUtilPathEventChange && Events[i+1] is RTUtilPathEventSegment
                    {
                        RTUtilPathEventSegment Ev = Events[i + 1] as RTUtilPathEventSegment;
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
            if (y < Input.Height)
            {
                for (int x = 0; x <= Input.Width; x++)  // "<=" is intentional
                    if (Input.Get(x, y) != Input.Get(x - 1, y))
                        Results.Add(new RTUtilPathEventChange(x));
            }

            // Now insert the segment events in the right places
            for (int i = 0; i < ActiveSegments.Count; i++)
            {
                int Index = 0;
                while (Index < Results.Count && Results[Index].X <= ActiveSegments[i][0].X)
                    Index++;
                Results.Insert(Index, new RTUtilPathEventSegment(i, true, ActiveSegments[i][0].X));
                Index = 0;
                while (Index < Results.Count && Results[Index].X < ActiveSegments[i][ActiveSegments[i].Count - 1].X)
                    Index++;
                Results.Insert(Index, new RTUtilPathEventSegment(i, false, ActiveSegments[i][ActiveSegments[i].Count - 1].X));
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
