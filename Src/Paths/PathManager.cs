using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RT.Util.Paths
{
    /// <summary>
    ///     Maintains a selection of paths in the filesystem and offers methods for enumerating all the included paths and/or
    ///     files.</summary>
    /// <remarks>
    ///     <para>
    ///         Initially empty. Paths may be added to the set (with all subpaths) by calling <see cref="AddIncludePath"/>, and
    ///         removed from the set using <see cref="AddExcludePath"/> (again, with subpaths). The order of these calls is
    ///         important.</para>
    ///     <para>
    ///         There is currently no support for including or excluding non-recursively, nor for including/excluding specific
    ///         files.</para></remarks>
    [Serializable]
    public sealed class PathManager
    {
        /// <summary>Constructs an empty instance.</summary>
        public PathManager()
        {
            Paths = new List<PathInfo>();
            Reset();
        }

        /// <summary>Constructs an instance containing all of the specified paths (with all subpaths).</summary>
        public PathManager(params string[] paths)
            : this()
        {
            foreach (var item in paths)
                AddIncludePath(item);
        }

        /// <summary>Constructs an instance containing all of the specified paths (with all subpaths).</summary>
        public PathManager(IEnumerable<string> paths)
            : this()
        {
            foreach (var item in paths)
                AddIncludePath(item);
        }

        /// <summary>The structure used to store path information</summary>
        [Serializable]
        public sealed class PathInfo
        {
            /// <summary>An absolute path to a directory.</summary>
            public string Path { get; set; }

            /// <summary>Whether everything under the specified path is included or excluded from the set.</summary>
            public bool Include { get; set; }
        }

        /// <summary>Contains the list of all included and excluded paths.</summary>
        public List<PathInfo> Paths { get; set; }

        /// <summary>
        ///     If set, this callback is invoked whenever a reparse point is encountered to decide whether to process it or
        ///     skip it. If null, reparse points are not recursed into.</summary>
        public Func<DirectoryInfo, bool> ShouldRecurseIntoReparsePoint { get; set; }

        /// <summary>Creates a deep clone of this class.</summary>
        public PathManager Clone()
        {
            PathManager pm = new PathManager();
            pm.Paths = new List<PathInfo>(Paths.Count);
            pm.Paths.AddRange(Paths);
            return pm;
        }

        /// <summary>Returns the index of the specified path or -1 if not found</summary>
        private int findPathEntry(string path)
        {
            for (int i = 0; i < Paths.Count; i++)
                if (Paths[i].Path.ToUpper() == path.ToUpper())
                    return i;
            return -1;
        }

        /// <summary>Adds an include/exclude entry</summary>
        private void addPathEntry(string path, bool include)
        {
            // Delete the entry for this path, if it exists
            int i = findPathEntry(path);
            if (i != -1)
                Paths.RemoveAt(i);
            // Delete any entries which are subpaths of this path
            deleteSubpathEntries(path);
            // Check if the path is currently included/excluded; if so - done
            if (!IsPathIncluded(path) ^ include)
                return;
            // Add an entry for this path
            var pi = new PathInfo();
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

        /// <summary>Deletes all entries which are subpaths of the specified path.</summary>
        private void deleteSubpathEntries(string path)
        {
            for (int i = Paths.Count - 1; i >= 0; i--)
                if (PathUtil.IsSubpathOf(Paths[i].Path, path))
                    Paths.RemoveAt(i);
        }

        /// <summary>
        ///     Makes the specified path and all subpaths included into the set of paths.</summary>
        /// <remarks>
        ///     The exact way in which this call affects <see cref="Paths"/> is not part of the contract. This method only
        ///     guarantees that this path and all subpaths will be part of the set after the call returns.</remarks>
        public void AddIncludePath(string path)
        {
            addPathEntry(path, true);
        }

        /// <summary>
        ///     Makes the specified path and all subpaths excluded from the set of paths.</summary>
        /// <remarks>
        ///     The exact way in which this call affects <see cref="Paths"/> is not part of the contract. This method only
        ///     guarantees that this path and all subpaths will not be part of the set after the call returns.</remarks>
        public void AddExcludePath(string path)
        {
            addPathEntry(path, false);
        }

        /// <summary>A synonym for <see cref="AddIncludePath"/>, only chainable (returns <c>this</c>).</summary>
        public PathManager Include(string path)
        {
            AddIncludePath(path);
            return this;
        }

        /// <summary>A synonym for <see cref="AddExcludePath"/>, only chainable (returns <c>this</c>).</summary>
        public PathManager Exclude(string path)
        {
            AddExcludePath(path);
            return this;
        }

        /// <summary>Resets the set of paths to a state where all paths are excluded.</summary>
        public void Reset()
        {
            Paths.Clear();
        }

        /// <summary>Returns true iff the specified path is part of the path set.</summary>
        public bool IsPathIncluded(string path)
        {
            int mindist = int.MaxValue;
            int mindistn = -1;
            for (int i = 0; i < Paths.Count; i++)
            {
                int d = pathLevelDistance(Paths[i].Path, path);

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

        /// <summary>Returns true iff the specified path and all the subpaths are part of the path set.</summary>
        public bool IsPathIncludedWithAllSubpaths(string path)
        {
            // This is true if and only if the nearest path above is Include
            // and there are no paths (either Incl. or Excl.) below this path
            int mindist = int.MaxValue;
            int mindistn = -1;
            for (int i = 0; i < Paths.Count; i++)
            {
                int d = pathLevelDistance(Paths[i].Path, path);

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

        #region Enumeration & failed files list/event

        /// <summary>
        ///     If assigned, this delegate will be called whenever a directory cannot be enumerated, e.g. due to being unreadable
        ///     etc. This function must return "false" in order to terminate scanning or "true" to continue.</summary>
        public Func<FileSystemInfo, Exception, bool> ReportFail = null;

        /// <summary>Paths which could not be read while enumerating PathManager. Automatically cleared before each enumeration.</summary>
        public List<FileSystemInfo> FailedFiles;

        private bool doReportFail(FileSystemInfo filedir, Exception excp)
        {
            FailedFiles.Add(filedir);
            if (ReportFail == null)
                return true;
            else
                return ReportFail(filedir, excp);
        }

        /// <summary>
        ///     Enumerates all files and directories according to the paths that were added using <see cref="AddIncludePath"/> and
        ///     <see cref="AddExcludePath"/>. If any paths cannot be enumerated, they are added to the <see cref="FailedFiles"/>
        ///     list, which is cleared before enumeration begins.</summary>
        public IEnumerable<FileSystemInfo> GetEntries()
        {
            return get(true, true);
        }

        /// <summary>
        ///     Enumerates all files (not folders) according to the paths that were added using <see cref="AddIncludePath"/> and
        ///     <see cref="AddExcludePath"/>. If any paths cannot be enumerated, they are added to the <see cref="FailedFiles"/>
        ///     list, which is cleared before enumeration begins.</summary>
        public IEnumerable<FileInfo> GetFiles()
        {
            return get(false, true).Cast<FileInfo>();
        }

        /// <summary>
        ///     Enumerates all directories (not files) according to the paths that were added using <see cref="AddIncludePath"/>
        ///     and <see cref="AddExcludePath"/>. If any paths cannot be enumerated, they are added to the <see
        ///     cref="FailedFiles"/> list, which is cleared before enumeration begins.</summary>
        public IEnumerable<DirectoryInfo> GetDirectories()
        {
            return get(true, false).Cast<DirectoryInfo>();
        }

        private IEnumerable<FileSystemInfo> get(bool includeDirs, bool includeFiles)
        {
            FailedFiles = new List<FileSystemInfo>();
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

                if (includeDirs)
                    yield return curDir;

                if (curDir.Attributes.HasFlag(FileAttributes.ReparsePoint) && !shouldRecurseIntoReparse(curDir))
                    continue;

                try
                {
                    if (includeFiles)
                        files = curDir.GetFiles();
                    dirs = curDir.GetDirectories();
                }
                catch (Exception e)
                {
                    if (doReportFail(curDir, e))
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
                    try
                    {
                        if (toExclude.Contains(PathUtil.NormPath(di.FullName).ToLowerInvariant()))
                        {
                            // Remove this item to save searching time later?
                            toExclude.Remove(PathUtil.NormPath(di.FullName).ToLowerInvariant());
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        if (doReportFail(di, e))
                            continue;
                        else
                            yield break;
                    }
                    toScan.Push(di);
                }
            }
        }

        private bool shouldRecurseIntoReparse(DirectoryInfo curDir)
        {
            if (ShouldRecurseIntoReparsePoint == null)
                return false;
            return ShouldRecurseIntoReparsePoint(curDir);
        }

        /// <summary>
        ///     Determines the number of sublevels <paramref name="path"/> is away from <paramref name="ref_path"/>. Positive
        ///     numbers indicate that <paramref name="path"/> is deeper than <paramref name="ref_path"/>; negative that it's above
        ///     <paramref name="ref_path"/>.</summary>
        /// <param name="ref_path">
        ///     Reference path</param>
        /// <param name="path">
        ///     Path to be compared</param>
        /// <returns>
        ///     The number of sublevels, or int.MaxValue if neither path is a subpath of the other.</returns>
        private static int pathLevelDistance(string ref_path, string path)
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
