using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RT.Util;

namespace RT.Util.Paths
{
    /// <summary>
    /// PathManager builds a list of paths via calls to <see cref="AddIncludePath"/> and
    /// <see cref="AddExcludePath"/>, which include/exclude a path with all subdirectories.
    /// Redundant entries are automatically removed.
    /// </summary>
    [Serializable]
    public sealed class PathManager : ICloneable
    {
        /// <summary>
        /// Default constructor for PathManager - invokes <see cref="Reset"/>.
        /// </summary>
        public PathManager()
        {
            Reset();
        }

        /// <summary>
        /// The structure used to store path information
        /// </summary>
        [Serializable]
        public sealed class PathInfo
        {
            /// <summary>
            /// An absolute path to a directory.
            /// </summary>
            public string Path;

            /// <summary>
            /// Whether everything under the specified path is included or excluded from the set of files.
            /// </summary>
            public bool Include;
        }

        /// <summary>
        /// Contains the list of all included and excluded paths.
        /// </summary>
        public List<PathInfo> Paths;

        /// <summary>
        /// Creates a new instance of this class, copying all the included/excluded paths to it.
        /// </summary>
        public object Clone()
        {
            PathManager pm = new PathManager();
            pm.Paths = new List<PathInfo>(Paths.Count);
            pm.Paths.AddRange(Paths);
            return pm;
        }

        /// <summary>
        /// Returns the index of the specified path or -1 if not found
        /// </summary>
        private int FindPathEntry(string path)
        {
            for (int i = 0; i < Paths.Count; i++)
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
            if (i != -1)
                Paths.RemoveAt(i);
            // Delete any entries which are subpaths of this path
            DeleteSubpathEntries(path);
            // Check if the path is currently included/excluded; if so - done
            if (!IsPathIncluded(path) ^ include)
                return;
            // Add an entry for this path
            PathInfo pi = new PathInfo();
            pi.Path = PathUtil.NormPath(path);
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
            for (int i = Paths.Count - 1; i >= 0; i--)
                if (PathUtil.IsSubpathOf(Paths[i].Path, path))
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
            for (int i = 0; i < Paths.Count; i++)
            {
                int d = PathLevelDistance(Paths[i].Path, path);

                if (d == int.MaxValue || d < 0)
                    continue;

                if (d < mindist)
                {
                    mindist = d;
                    mindistn = i;
                }
            }
            if (mindistn == -1)
                return false;
            else
                return Paths[mindistn].Include;
        }

        /// <summary>
        /// Returns true only if the specified path is included as well as all
        /// subfiles and subpaths, recursively.
        /// </summary>
        public bool IsPathIncludedWithAllSubpaths(string path)
        {
            // This is true if and only if the nearest path above is Include
            // and there are no paths (either Incl. or Excl.) below this path
            int mindist = int.MaxValue;
            int mindistn = -1;
            for (int i = 0; i < Paths.Count; i++)
            {
                int d = PathLevelDistance(Paths[i].Path, path);

                if (d == int.MaxValue)
                    continue;
                else if (d < 0)
                    return false;

                if (d < mindist)
                {
                    mindist = d;
                    mindistn = i;
                }
            }
            if (mindistn == -1)
                return false;
            else
                return Paths[mindistn].Include;
        }

        /// <summary>
        /// Returns true only if none of the immediate children of the specified
        /// path are to be excluded from the scan.
        /// 
        /// Note: this doesn't check whether the specified path itself is included,
        /// nor whether the excluded directories actually exist. The files are currently
        /// always listed so this only checks to see whether there are any immediate
        /// exclude paths, that's it.
        /// </summary>
        public bool AllImmediateChildrenIncluded(string path)
        {
            for (int i = 0; i < Paths.Count; i++)
                if (PathLevelDistance(path, Paths[i].Path) == 1 && !Paths[i].Include)
                    return false;
            return true;
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

        #region Enumeration & failed files list/event

        /// <summary>
        /// If assigned, this delegate will be called whenever a directory cannot be
        /// enumerated, e.g. due to being unreadable etc. This function must return
        /// "false" in order to terminate scanning or "true" to continue.
        /// </summary>
        public Func<string, string, bool> ReportFail = null;

        /// <summary>
        /// Paths which could not be read while enumerating PathManager. This is automatically
        /// cleared for each enumeration.
        /// </summary>
        public List<string> FailedFiles;

        private bool DoReportFail(string DirName, string Message)
        {
            FailedFiles.Add(DirName);
            if (ReportFail == null)
                return true;
            else
                return ReportFail(DirName, Message);
        }

        /// <summary>Enumerates all files and directories according to the paths that were added using <see cref="AddIncludePath"/>
        /// and <see cref="AddExcludePath"/>. If any paths cannot be enumerated, they are added to the <see cref="FailedFiles"/> list,
        /// which is cleared before enumeration begins.</summary>
        public IEnumerable<FileSystemInfo> GetEntries()
        {
            return get(true, true);
        }

        /// <summary>Enumerates all files (not folders) according to the paths that were added using <see cref="AddIncludePath"/>
        /// and <see cref="AddExcludePath"/>. If any paths cannot be enumerated, they are added to the <see cref="FailedFiles"/> list,
        /// which is cleared before enumeration begins.</summary>
        public IEnumerable<FileInfo> GetFiles()
        {
            return get(false, true).Cast<FileInfo>();
        }

        /// <summary>Enumerates all directories (not files) according to the paths that were added using <see cref="AddIncludePath"/>
        /// and <see cref="AddExcludePath"/>. If any paths cannot be enumerated, they are added to the <see cref="FailedFiles"/> list,
        /// which is cleared before enumeration begins.</summary>
        public IEnumerable<DirectoryInfo> GetDirectories()
        {
            return get(false, true).Cast<DirectoryInfo>();
        }

        private IEnumerable<FileSystemInfo> get(bool includeDirs, bool includeFiles)
        {
            FailedFiles = new List<string>();
            var toScan = new Stack<DirectoryInfo>();
            var toExclude = new List<string>();

            var list = new List<string>(); // so that we queue items in proper order
            for (int i = 0; i < Paths.Count; i++)
            {
                if (Paths[i].Include)
                    list.Add(Paths[i].Path);
                else
                    toExclude.Add(Paths[i].Path.ToLowerInvariant());
            }
            for (int i = list.Count - 1; i >= 0; i--)
                toScan.Push(new DirectoryInfo(list[i]));

            // Scan all paths
            while (toScan.Count > 0)
            {
                DirectoryInfo curDir = toScan.Pop();
                FileInfo[] files = null;
                DirectoryInfo[] dirs;
                try
                {
                    if (includeFiles)
                        files = curDir.GetFiles();
                    dirs = curDir.GetDirectories();
                }
                catch (Exception e)
                {
                    if (DoReportFail(curDir.FullName, e.Message))
                        continue;
                    else
                        yield break;
                }

                // Files
                if (includeFiles)
                    foreach (FileInfo fi in files)
                        yield return fi;

                // Directories
                foreach (DirectoryInfo di in dirs)
                {
                    if (toExclude.Contains(PathUtil.NormPath(di.FullName).ToLowerInvariant()))
                    {
                        // Remove this item to save searching time later?
                        toExclude.Remove(PathUtil.NormPath(di.FullName).ToLowerInvariant());
                        continue;
                    }
                    toScan.Push(di);
                    if (includeDirs)
                        yield return di;
                }
            }
        }

        /// <summary>
        /// Determines the number of sublevels <paramref name="path"/> is away from <paramref name="ref_path"/>.
        /// Positive numbers indicate that <paramref name="path"/> is deeper than <paramref name="ref_path"/>;
        /// negative that it's above <paramref name="ref_path"/>.</summary>
        /// <param name="ref_path">Reference path</param>
        /// <param name="path">Path to be compared</param>
        /// <returns>The number of sublevels, or int.MaxValue if neither path is a subpath of the other.</returns>
        public static int PathLevelDistance(string ref_path, string path)
        {
            string p1 = PathUtil.NormPath(ref_path.ToUpper());
            string p2 = PathUtil.NormPath(path.ToUpper());

            if (p1 == p2)
                return 0;

            if (p1.Length < p2.Length)
            {
                if (p2.Substring(0, p1.Length) != p1)
                    return int.MaxValue;
                p1 = p2.Substring(p1.Length);
                return p1.Count(c => c == Path.DirectorySeparatorChar);
            }
            else
            {
                if (p1.Substring(0, p2.Length) != p2)
                    return int.MaxValue;
                p2 = p1.Substring(p2.Length);
                return -p2.Count(c => c == Path.DirectorySeparatorChar);
            }
        }

        #endregion
    }
}