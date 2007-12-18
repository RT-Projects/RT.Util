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
    [Serializable]
    public class PathManager : ICloneable
    {
        /// <summary>
        /// Default constructor for PathManager - invokes Reset().
        /// </summary>
        public PathManager()
        {
            Reset();
        }

        /// <summary>
        /// The structure used to store path information
        /// </summary>
        [Serializable]
        public class PathInfo
        {
            public string Path;
            public bool Include;
        }

        /// <summary>
        /// The list of all paths
        /// </summary>
        public List<PathInfo> Paths;

        public object Clone()
        {
            PathManager PM = new PathManager();
            PM.Paths = new List<PathInfo>(Paths.Count);
            PM.Paths.AddRange(Paths);
            return PM;
        }

        /// <summary>
        /// Returns the index of the specified path or -1 if not found
        /// </summary>
        private int FindPathEntry(string path)
        {
            for (int i=0; i<Paths.Count; i++)
                if (Paths[i].Path.ToUpper() == path.ToUpper())
                    return i;
            return -1;
        }

        /// <summary>
        /// Adds an include/exclude entry 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="include"></param>
        private void AddPathEntry(string path, bool include)
        {
            // Delete the entry for this path, if it exists
            int i = FindPathEntry(path);
            if (i!=-1)
                Paths.RemoveAt(i);
            // Delete any entries which are subpaths of this path
            DeleteSubpathEntries(path);
            // Check if the path is currently included/excluded; if so - done
            if (!IsPathIncluded(path) ^ include)
                return;
            // Add an entry for this path
            PathInfo pi = new PathInfo();
            pi.Path = Ut.NrmPath(path);
            pi.Include = include;
            // Find where to insert it
            // Paths can be naturally sorted lexicographically, so it's as simple as that
            int fp = -1;
            for (i = 0; i < Paths.Count; i++)
                if (string.Compare(pi.Path, Paths[i].Path, true) < 0)
                {
                    fp = i;
                    break;
                }
            if (fp == -1)
                Paths.Add(pi);
            else
                Paths.Insert(fp, pi);
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
        /// Includes the specified path to the paths tree. This operation
        /// removes any entries which refer to subpaths of the specified path.
        /// </summary>
        public void AddIncludePath(string path)
        {
            AddPathEntry(path, true);
        }

        /// <summary>
        /// Excludes the specified path from the paths tree. This operation
        /// removes any entries which refer to subpaths of the specified path.
        /// </summary>
        public void AddExcludePath(string path)
        {
            AddPathEntry(path, false);
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
        /// <returns>Whether the path is included/excluded</returns>
        public bool IsPathIncluded(string path)
        {
            int mindist = int.MaxValue;
            int mindistn = -1;
            for (int i=0; i<Paths.Count; i++)
            {
                int d = Ut.PathLevelDistance(Paths[i].Path, path);
                
                if (d == int.MaxValue || d < 0)
                    continue;

                if (d<mindist)
                {
                    mindist = d;
                    mindistn = i;
                }
            }
            if (mindistn==-1)
                return false;
            else
                return Paths[mindistn].Include;
        }

        /// <summary>
        /// Returns the number of paths marked as "include". Mainly intended to verify
        /// that there is at least one path included.
        /// </summary>
        public int IncludedPathCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Paths.Count; i++)
                    if (Paths[i].Include)
                        n++;
                return n;
            }
        }

        #region Enumeration & failed files list

        /// <summary>
        /// Paths which could not be read while enumerating PathManager. This is automatically
        /// cleared for each enumeration.
        /// </summary>
        public List<string> FailedFiles;

        /// <summary>
        /// Enumerates all files and folders visible to the PathManager, according to which
        /// paths were added using AddIncludePath and AddExcludePath. If any paths cannot be
        /// enumerated, they are added to FailedFiles list, which is cleared before
        /// enumeration begins.
        /// </summary>
        /// <param name="includeDirs">If true, directories will be enumerated as DirectoryInfo's.</param>
        /// <param name="includeFiles">If true, files will be enumerated as FileInfo's. Otherwise files
        /// won't be listed, which is considerably faster (but also considerably less useful...)</param>
        public IEnumerable<FileSystemInfo> GetFiles(bool includeDirs, bool includeFiles)
        {
            FailedFiles = new List<string>();
            Stack<DirectoryInfo> ToScan = new Stack<DirectoryInfo>();
            List<string> ToExclude = new List<string>();

            List<string> l = new List<string>(); // so that we queue items in proper order
            for (int i = 0; i < Paths.Count; i++)
            {
                if (Paths[i].Include)
                    l.Add(Paths[i].Path);
                else
                    ToExclude.Add(Paths[i].Path.ToLowerInvariant());
            }
            for (int i = l.Count-1; i >= 0; i--)
                ToScan.Push(new DirectoryInfo(l[i]));

            // Scan all paths
            while (ToScan.Count > 0)
            {
                DirectoryInfo curDI = ToScan.Pop();
                FileInfo[] files = null;
                DirectoryInfo[] dirs;
                try
                {
                    if (includeFiles)
                        files = curDI.GetFiles();
                    dirs = curDI.GetDirectories();
                }
                catch
                {
                    FailedFiles.Add(curDI.FullName);
                    continue;
                }

                // Files
                if (includeFiles)
                    foreach (FileInfo fi in files)
                        yield return fi;

                // Directories
                foreach (DirectoryInfo di in dirs)
                {
                    if (ToExclude.Contains(Ut.NrmPath(di.FullName).ToLowerInvariant()))
                    {
                        // Remove this item to save searching time later?
                        ToExclude.Remove(Ut.NrmPath(di.FullName).ToLowerInvariant());
                        continue;
                    }
                    ToScan.Push(di);
                    if (includeDirs)
                        yield return di;
                }
            }
        }

        #endregion

    }
}
