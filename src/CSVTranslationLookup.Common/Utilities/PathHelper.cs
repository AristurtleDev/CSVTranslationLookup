// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

namespace CSVTranslationLookup.Common.Utilities
{
    /// <summary>
    /// Provides safe, cross-platform path manipulation utilities.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Normalizes path seperators to the currnet platform's directory seperator
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path, or null if input was null.</returns>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // Replace both forward and backslashes with platform-specific seperator
            return path.Replace('/', Path.DirectorySeparatorChar)
                       .Replace('\\', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Safely combines multiple path components, handling null/empty values.
        /// </summary>
        /// <param name="paths">The paths components to combine.</param>
        /// <returns>The combine path.</returns>
        /// <exception cref="ArgumentException">Thrown when no valid paths provided.</exception>
        public static string SafeCombine(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                throw new ArgumentException("At least one path component required", nameof(paths));
            }

            // Filter out null/empty paths and combine
            var validPaths = paths.Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (validPaths.Length == 0)
            {
                throw new ArgumentException("All path components were null or empty", nameof(paths));
            }

            // Normalize seperators in all components
            var normalizedPaths = validPaths.Select(NormalizePath).ToArray();

            return Path.Combine(normalizedPaths);
        }

        /// <summary>
        /// Safely gets the directory name froma path, returning null if path is invalid.
        /// </summary>
        /// <param name="path">The path to extract directory from.</param>
        /// <returns>The directory name, or null if path is null/empty or extraction fails.</returns>
        public static string SafeGetDirectoryName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return Path.GetDirectoryName(path);
            }
            catch (ArgumentException)
            {
                // Path contains invalid characters
                return null;
            }
            catch (PathTooLongException)
            {
                return null;
            }
        }

        /// <summary>
        /// Safely gets the file name from a path, returning null if path is invalid.
        /// </summary>
        /// <param name="path">The path to extract file name from.</param>
        /// <returns>The file name, or null if path is null/empty or extraction fails.</returns>
        public static string SafeGetFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return Path.GetFileName(path);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Safely gets the file extension from a path, returning null if path is invalid.
        /// </summary>
        /// <param name="path">The path to extract extension from.</param>
        /// <returns>The extension (including dot), or null if path is invalid.</returns>
        public static string SafeGetExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return Path.GetExtension(path);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a path has a specific extension (case-insensitive).
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <param name="extension">The extension to check for (with or without leading dot).</param>
        /// <returns>true if path has the specified extension; otherwise, false.</returns>
        public static bool HasExtension(string path, string extension)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(extension))
            {
                return false;
            }

            // Ensure extension has leading dot
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            string actualExtension = SafeGetExtension(path);
            return !string.IsNullOrEmpty(actualExtension) &&
                   actualExtension.Equals(extension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares two file paths for equality, accounting for case sensiivity.
        /// </summary>
        /// <param name="path1">The first path.</param>
        /// <param name="path2">The second path.</param>
        /// <returns>true if paths are equal; otherwise, false.</returns>
        public static bool ArePathsEqual(string path1, string path2)
        {
            if (path1 == null && path2 == null)
            {
                return true;
            }

            if (path1 == null || path2 == null)
            {
                return false;
            }

            // Normalize both paths
            string normalized1 = NormalizePath(path1);
            string normalized2 = NormalizePath(path2);

            StringComparison comparison = Path.DirectorySeparatorChar == '\\' ?
                                          StringComparison.OrdinalIgnoreCase :
                                          StringComparison.Ordinal;

            return string.Equals(normalized1, normalized2, comparison);
        }

        /// <summary>
        /// Attempts to get the full absolute path, returning the original path if it fails.
        /// </summary>
        /// <param name="path">The path to get the full path for.</param>
        /// <returns>The full absolute path, or the original path if conversion fails.</returns>
        public static string GetFullPathSafe(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch (ArgumentException)
            {
                return path;
            }
            catch (PathTooLongException)
            {
                return path;
            }
            catch (NotSupportedException)
            {
                return path;
            }
        }

        /// <summary>
        /// Checks if a path is rooted (absolute) safely.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>true if path is rooted; flase if not rooted or invalid.</returns>
        public static bool IsPathRooted(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                return Path.IsPathRooted(path);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Ensures a path ends with the directory seperator character.
        /// </summary>
        /// <param name="path">The path to ensure ends with separater.</param>
        /// <returns>The path with trailing seperator, or the original path if null/empty.</returns>
        public static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (path[path.Length - 1] != Path.DirectorySeparatorChar &&
                path[path.Length - 1] != Path.AltDirectorySeparatorChar)
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        /// <summary>
        /// Removes trailing directory seperator characters from a path.
        /// </summary>
        /// <param name="path">The path to remove trailing seperator from.</param>
        /// <returns>The path without trailing seperator, or the original path if null/empty.</returns>
        public static string RemoveTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Gets the working directory for an exectuable path, defaulting to exectuable's directory.
        /// </summary>
        /// <param name="executablePath">The path to the exectuable.</param>
        /// <returns>
        /// The directory containing the exectuable, or null if path is invalid.
        /// Returns null for command-only paths (no directory seperators).
        /// </returns>
        public static string GetWorkingDirectoryForExecutable(string executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
            {
                return null;
            }

            // if it's just a command name (no path), return null
            if (!executablePath.Contains(Path.DirectorySeparatorChar) &&
               !executablePath.Contains(Path.AltDirectorySeparatorChar))
            {
                return null;
            }

            return SafeGetDirectoryName(executablePath);
        }

        /// <summary>
        /// Creates a relative path from one path to another.
        /// </summary>
        /// <param name="fromPath">The source path.</param>
        /// <param name="toPath">The destination path.</param>
        /// <returns>The relative path, or the toPath if relative path cannot be created.</returns>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath) || string.IsNullOrEmpty(toPath))
            {
                return toPath;
            }

            try
            {
                Uri fromUri = new Uri(GetFullPathSafe(fromPath));
                Uri toUri = new Uri(GetFullPathSafe(toPath));

                Uri relativeUri = fromUri.MakeRelativeUri(toUri);
                string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

                return NormalizePath(relativePath);
            }
            catch
            {
                return toPath;
            }
        }
    }
}
