using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RT.Util
{
    /// <summary>
    /// PathManager builds a list of paths via calls to IncludePath and
    /// ExcludePath, which include/exclude a path with all subdirectories.
    /// Redundant entries are automatically removed.
    /// </summary>
    public class PathManager
    {
        /// <summary>
        /// The structure used to store path information
        /// </summary>
        public struct PathInfo
        {
            public string Path;
            public bool Include;
        }

        /// <summary>
        /// The list of all paths
        /// </summary>
        public List<PathInfo> Paths;

        /// <summary>
        /// Returns the index of the specified path or -1 if not found
        /// </summary>
        private int FindPath(string path)
        {
            for (int i=0; i<Paths.Count; i++)
                if (Paths[i].Path.ToUpper() == path.ToUpper())
                    return i;
            return -1;
        }

        /// <summary>
        /// Adds an include/exclude path.
        /// </summary>
        /// <param name="path">Path to be added</param>
        /// <param name="include">True if it's an include path</param>
        private void AddPath(string path, bool include)
        {
            PathInfo pi = new PathInfo();
            pi.Path = path;
            pi.Include = include;
            Paths.Add(pi);
        }

        /// <summary>
        /// Deletes all entries which are subpaths of the specified path.
        /// </summary>
        private void DeleteSubpathEntries(string path)
        {
            for (int i=Paths.Count-1; i>=0; i--)
                if (Ut.IsSubpath(path, Paths[i].Path))
                    Paths.RemoveAt(i);
        }

        /// <summary>
        /// Resets the paths to a state where all paths are excluded.
        /// </summary>
        public void Reset()
        {
            Paths = new List<PathInfo>();
        }

        /// <summary>
        /// Checks whether a path is included or excluded.
        /// </summary>
        /// <param name="path">The path to be checked</param>
        /// <param name="included">If false, the function becomes IsPathExcluded</param>
        /// <returns>Whether the path is included/excluded</returns>
        public bool IsPathIncluded(string path, bool included)
        {
            int mindist = int.MaxValue;
            int mindistn = -1;
            for (int i=0; i<Paths.Count; i++)
            {
                int d;
                try
                {
                    d = Ut.PathLevelDistance(Paths[i].Path, path);
                }
                catch (Exception)
                {
                    continue;
                }
                if (d<0)
                    continue;
                if (d<mindist)
                {
                    mindist = d;
                    mindistn = i;
                }
            }
            if (mindistn==-1)
                return !included;
            else
                return !(Paths[mindistn].Include ^ included);
        }

        /// <summary>
        /// Includes a path by doing the following:
        /// 1. Check if the path is currently included
        /// 2. Ignore if it is
        /// 3. Delete any entries which are subpaths of this path
        /// 4. Delete the entry for this path, if it exists
        /// 5. Add an entry for this path
        /// </summary>
        /// <param name="path"></param>
        public void IncludePath(string path)
        {
            int i = FindPath(path);
            if (i!=-1)
                Paths.RemoveAt(i);
            DeleteSubpathEntries(path);
            if (IsPathIncluded(path, true))
                return;
            AddPath(path, true);
        }

        /// <summary>
        /// Excludes a path by doing the following:
        /// 1. Check if the path is currently excluded
        /// 2. Ignore if it is, exclude otherwise
        /// 3. Delete any entries which are subpaths of this path
        /// </summary>
        /// <param name="path"></param>
        public void ExcludePath(string path)
        {
            int i = FindPath(path);
            if (i!=-1)
                Paths.RemoveAt(i);
            DeleteSubpathEntries(path);
            if (IsPathIncluded(path, false))
                return;
            AddPath(path, false);
        }

        /// <summary>
        /// TODO: Not quite sure what this does. It's not referenced anywhere.
        /// It appears to be able to find the first path in the list of all paths.
        /// </summary>
        public void SortPaths(string path)
        {
            int from = 0;

            // Find smallest one
            int minn = -1;
            int mindepth = int.MaxValue;
            for (int i=from; i<Paths.Count; i++)
            {
                // Compare path depth
                int depth = Ut.CountStrings(Paths[i].Path, Path.DirectorySeparatorChar+"");
                if (depth < mindepth)
                {
                    minn = i;
                    mindepth = depth;
                }
                else
                {
                    if (minn==-1)
                    {
                        minn = i;
                        mindepth = depth;
                    }
                    else
                    {
                        // Compare actual strings
                        if (string.Compare(Paths[i].Path, Paths[minn].Path, true) < 0)
                        {
                            minn = i;
                            mindepth = depth;
                        }
                    }
                }
            }
        }

    }
}
