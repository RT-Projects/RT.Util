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
        public static Color ColorBlend(Color color, Color backColor, double amount)
        {
            byte r = (byte) ((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte) ((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte) ((color.B * amount) + backColor.B * (1 - amount));
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Draws the specified Image into the destination rectangle DestRect of the Graphics object g using the specified Opacity.
        /// </summary>
        /// <param name="g">Graphics object to alpha-blend the image onto.</param>
        /// <param name="image">Image to draw.</param>
        /// <param name="destRect">Destination rectangle within the target Graphics canvas.</param>
        /// <param name="opacity">Opacity level to use when drawing the image. 0 means nothing changes.
        /// 1 means the image is drawn normally. 0.5 means a 50% blend between source and destination.</param>
        public static void DrawImageAlpha(Graphics g, Image image, Rectangle destRect, float opacity)
        {
            ColorMatrix matrix = new ColorMatrix(new float[][] {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, opacity, 0},
                new float[] {0, 0, 0, 0, 1}
            });
            ImageAttributes attr = new ImageAttributes();
            attr.SetColorMatrix(matrix,
                ColorMatrixFlag.Default,
                ColorAdjustType.Bitmap);
            g.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attr);
        }

        /// <summary>Given a two-dimensional array of booleans, generates the "outline" of the region described by the booleans set to true.
        /// If there are several disjoint regions, several separate outlines are generated.</summary>
        /// <param name="input">The input array of booleans to generate the outline from.</param>
        /// <returns>An array of paths, where each path is an array of points. The co-ordinates of the points are the indexes in the input array.</returns>
        /// <example>An input array full of booleans set to false generates an empty output array.
        /// 
        /// An input array full of booleans set to true generates a single output path which describes the complete rectangle.</example>
        public static Point[][] BoolsToPaths(Virtual2DArray<bool> input)
        {
            List<List<Point>> activeSegments = new List<List<Point>>();
            List<Point[]> completedPaths = new List<Point[]>();
            for (int y = 0; y <= input.Height; y++)
            {
                List<pathEvent> events = findEvents(activeSegments, input, y);
                for (int i = 0; i < events.Count; i += 2)
                {
                    if (events[i] is pathEventSegment && events[i + 1] is pathEventSegment)
                    {
                        int index1 = ((pathEventSegment) events[i]).SegmentIndex;
                        int index2 = ((pathEventSegment) events[i + 1]).SegmentIndex;
                        bool start = ((pathEventSegment) events[i]).StartOfSegment;
                        if (index1 == index2 && start)
                        {
                            // A segment becomes a closed path
                            activeSegments[index2].Add(new Point(events[i + 1].X, y));
                            activeSegments[index2].Add(new Point(events[i].X, y));
                            completedPaths.Add(activeSegments[index2].ToArray());
                        }
                        else if (index1 == index2)
                        {
                            // A segment becomes a closed path
                            activeSegments[index2].Add(new Point(events[i].X, y));
                            activeSegments[index2].Add(new Point(events[i + 1].X, y));
                            completedPaths.Add(activeSegments[index2].ToArray());
                        }
                        else if (start)
                        {
                            // Two segments join up
                            activeSegments[index2].Add(new Point(events[i + 1].X, y));
                            activeSegments[index2].Add(new Point(events[i].X, y));
                            activeSegments[index1].InsertRange(0, activeSegments[index2]);
                        }
                        else
                        {
                            // Two segments join up
                            activeSegments[index1].Add(new Point(events[i].X, y));
                            activeSegments[index1].Add(new Point(events[i + 1].X, y));
                            activeSegments[index1].AddRange(activeSegments[index2]);
                        }
                        activeSegments.RemoveAt(index2);
                        for (int correction = i + 2; correction < events.Count; correction++)
                        {
                            if (events[correction] is pathEventSegment &&
                                (events[correction] as pathEventSegment).SegmentIndex == index2)
                                (events[correction] as pathEventSegment).SegmentIndex = index1;
                            if (events[correction] is pathEventSegment &&
                                (events[correction] as pathEventSegment).SegmentIndex > index2)
                                (events[correction] as pathEventSegment).SegmentIndex--;
                        }
                    }
                    else if (events[i] is pathEventChange && events[i + 1] is pathEventChange)
                    {
                        // Both events are changes - create a new segment
                        activeSegments.Add(new List<Point>(new Point[] { 
                            new Point (events[input.Get(events[i].X, y) ? i : i+1].X,y),
                            new Point (events[input.Get(events[i].X, y) ? i+1 : i].X,y)
                        }));
                    }
                    else if (events[i] is pathEventSegment) // ... && Events[i+1] is RTUtilPathEventChange
                    {
                        pathEventSegment ev = events[i] as pathEventSegment;
                        if (ev.StartOfSegment)
                        {
                            activeSegments[ev.SegmentIndex].Insert(0, new Point(ev.X, y));
                            if (ev.X != events[i + 1].X)
                                activeSegments[ev.SegmentIndex].Insert(0, new Point(events[i + 1].X, y));
                        }
                        else
                        {
                            activeSegments[ev.SegmentIndex].Add(new Point(ev.X, y));
                            if (ev.X != events[i + 1].X)
                                activeSegments[ev.SegmentIndex].Add(new Point(events[i + 1].X, y));
                        }
                    }
                    else  // ... Events[i] is RTUtilPathEventChange && Events[i+1] is RTUtilPathEventSegment
                    {
                        pathEventSegment ev = events[i + 1] as pathEventSegment;
                        if (ev.StartOfSegment)
                        {
                            activeSegments[ev.SegmentIndex].Insert(0, new Point(ev.X, y));
                            if (ev.X != events[i].X)
                                activeSegments[ev.SegmentIndex].Insert(0, new Point(events[i].X, y));
                        }
                        else
                        {
                            activeSegments[ev.SegmentIndex].Add(new Point(ev.X, y));
                            if (ev.X != events[i].X)
                                activeSegments[ev.SegmentIndex].Add(new Point(events[i].X, y));
                        }
                    }
                }
            }
            return completedPaths.ToArray();
        }

        private static List<pathEvent> findEvents(List<List<Point>> activeSegments, Virtual2DArray<bool> input, int y)
        {
            List<pathEvent> results = new List<pathEvent>();

            // First add all the validity change events in the correct order
            if (y < input.Height)
            {
                for (int x = 0; x <= input.Width; x++)  // "<=" is intentional
                    if (input.Get(x, y) != input.Get(x - 1, y))
                        results.Add(new pathEventChange(x));
            }

            // Now insert the segment events in the right places
            for (int i = 0; i < activeSegments.Count; i++)
            {
                int index = 0;
                while (index < results.Count && results[index].X <= activeSegments[i][0].X)
                    index++;
                results.Insert(index, new pathEventSegment(i, true, activeSegments[i][0].X));
                index = 0;
                while (index < results.Count && results[index].X < activeSegments[i][activeSegments[i].Count - 1].X)
                    index++;
                results.Insert(index, new pathEventSegment(i, false, activeSegments[i][activeSegments[i].Count - 1].X));
            }
            return results;
        }
        private abstract class pathEvent
        {
            public int X;
        }
        private class pathEventSegment : pathEvent
        {
            public int SegmentIndex;
            public bool StartOfSegment;
            public pathEventSegment(int index, bool start, int newX)
            {
                SegmentIndex = index;
                StartOfSegment = start;
                X = newX;
            }
        }
        private class pathEventChange : pathEvent
        {
            public pathEventChange(int newX)
            {
                X = newX;
            }
        }
    }
}
