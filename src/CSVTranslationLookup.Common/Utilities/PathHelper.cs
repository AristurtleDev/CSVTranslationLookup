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
    /// <remarks>
    /// All methods handle invalid paths gracefully by returning null, empty strings, or the original
    /// input rather than throwing exceptions. Path separators are normalized to the current platform's
    /// directory separator. Path comparisons account for platform-specific case sensitivity
    /// (case-insensitive on Windows, case-sensitive on Unix-like systems).
    /// </remarks>
    public static class PathHelper
    {
        /// <summary>
        /// Normalizes path separators to the current platform's directory separator.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>
        /// The normalized path with platform-specific separators, or <see langword="null"/> if input was <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// Replaces both forward slashes (/) and backslashes (\) with the platform-specific
        /// directory separator (<see cref="Path.DirectorySeparatorChar"/>). This ensures paths
        /// work correctly on both Windows and Unix-like systems.
        /// </remarks>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // Replace both forward and backslashes with platform-specific separator
            return path.Replace('/', Path.DirectorySeparatorChar)
                       .Replace('\\', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Safely combines multiple path components, handling null and empty values.
        /// </summary>
        /// <param name="paths">The path components to combine.</param>
        /// <returns>The combined path with normalized separators.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when no paths are provided or all path components are null or empty.
        /// </exception>
        /// <remarks>
        /// Filters out null and empty path components before combining. All valid components
        /// are normalized to use platform-specific directory separators before being combined.
        /// </remarks>
        public static string SafeCombine(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                throw new ArgumentException("At least one path component required", nameof(paths));
            }

            // Filter out null/empty paths
            var validPaths = paths.Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (validPaths.Length == 0)
            {
                throw new ArgumentException("All path components were null or empty", nameof(paths));
            }

            // Normalize separators in all components before combining
            var normalizedPaths = validPaths.Select(NormalizePath).ToArray();

            return Path.Combine(normalizedPaths);
        }

        /// <summary>
        /// Safely gets the directory name from a path, returning <see langword="null"/> if the path is invalid.
        /// </summary>
        /// <param name="path">The path to extract the directory from.</param>
        /// <returns>
        /// The directory name, or <see langword="null"/> if the path is null, empty, or extraction fails.
        /// </returns>
        /// <remarks>
        /// Catches exceptions from <see cref="Path.GetDirectoryName(string)"/> and returns null
        /// instead of propagating them. This includes <see cref="ArgumentException"/> for invalid
        /// characters and <see cref="PathTooLongException"/>.
        /// </remarks>
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
        /// Safely gets the file name from a path, returning <see langword="null"/> if the path is invalid.
        /// </summary>
        /// <param name="path">The path to extract the file name from.</param>
        /// <returns>
        /// The file name with extension, or <see langword="null"/> if the path is null, empty, or extraction fails.
        /// </returns>
        /// <remarks>
        /// Catches exceptions from <see cref="Path.GetFileName(string)"/> and returns null
        /// instead of propagating them.
        /// </remarks>
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
        /// Safely gets the file extension from a path, returning <see langword="null"/> if the path is invalid.
        /// </summary>
        /// <param name="path">The path to extract the extension from.</param>
        /// <returns>
        /// The extension including the leading dot (e.g., ".csv"), or <see langword="null"/> if the path is invalid.
        /// </returns>
        /// <remarks>
        /// Catches exceptions from <see cref="Path.GetExtension(string)"/> and returns null
        /// instead of propagating them.
        /// </remarks>
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
        /// <returns>
        /// <see langword="true"/> if the path has the specified extension; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The extension comparison is case-insensitive. The extension parameter can be provided
        /// with or without a leading dot (e.g., both ".csv" and "csv" work). Returns false if
        /// either parameter is null or empty.
        /// </remarks>
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
        /// Compares two file paths for equality, accounting for platform-specific case sensitivity.
        /// </summary>
        /// <param name="path1">The first path.</param>
        /// <param name="path2">The second path.</param>
        /// <returns>
        /// <see langword="true"/> if the paths are equal; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Normalizes both paths to use platform-specific separators before comparing.
        /// Uses case-insensitive comparison on Windows (where paths are case-insensitive) and
        /// case-sensitive comparison on Unix-like systems. Returns true if both paths are null.
        /// </remarks>
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

            // Normalize both paths to platform-specific separators
            string normalized1 = NormalizePath(path1);
            string normalized2 = NormalizePath(path2);

            // Use case-insensitive comparison on Windows, case-sensitive on Unix-like systems
            StringComparison comparison = Path.DirectorySeparatorChar == '\\' ?
                                          StringComparison.OrdinalIgnoreCase :
                                          StringComparison.Ordinal;

            return string.Equals(normalized1, normalized2, comparison);
        }

        /// <summary>
        /// Attempts to get the full absolute path, returning the original path if conversion fails.
        /// </summary>
        /// <param name="path">The path to get the full path for.</param>
        /// <returns>
        /// The full absolute path, or the original path if conversion fails or the path is invalid.
        /// </returns>
        /// <remarks>
        /// Catches exceptions from <see cref="Path.GetFullPath(string)"/> and returns the original
        /// path instead of propagating them. This includes <see cref="ArgumentException"/>,
        /// <see cref="PathTooLongException"/>, and <see cref="NotSupportedException"/>.
        /// </remarks>
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
        /// <returns>
        /// <see langword="true"/> if the path is rooted; <see langword="false"/> if not rooted or invalid.
        /// </returns>
        /// <remarks>
        /// Catches exceptions from <see cref="Path.IsPathRooted(string)"/> and returns false
        /// instead of propagating them. Returns false for null or empty paths.
        /// </remarks>
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
        /// Ensures a path ends with the directory separator character.
        /// </summary>
        /// <param name="path">The path to ensure ends with a separator.</param>
        /// <returns>
        /// The path with a trailing separator, or the original path if null or empty.
        /// </returns>
        /// <remarks>
        /// Adds <see cref="Path.DirectorySeparatorChar"/> to the end of the path if it doesn't
        /// already end with either <see cref="Path.DirectorySeparatorChar"/> or
        /// <see cref="Path.AltDirectorySeparatorChar"/>.
        /// </remarks>
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
        /// Removes trailing directory separator characters from a path.
        /// </summary>
        /// <param name="path">The path to remove trailing separators from.</param>
        /// <returns>
        /// The path without trailing separators, or the original path if null or empty.
        /// </returns>
        /// <remarks>
        /// Removes both <see cref="Path.DirectorySeparatorChar"/> and
        /// <see cref="Path.AltDirectorySeparatorChar"/> from the end of the path.
        /// </remarks
        public static string RemoveTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Gets the working directory for an executable path.
        /// </summary>
        /// <param name="executablePath">The path to the executable.</param>
        /// <returns>
        /// The directory containing the executable, or <see langword="null"/> if the path is invalid
        /// or contains only a command name without directory separators.
        /// </returns>
        /// <remarks>
        /// Returns null for command-only paths (e.g., "notepad.exe") that don't contain directory
        /// separators. For full paths (e.g., "C:\Program Files\App\app.exe"), returns the directory
        /// portion. Useful for determining the working directory when launching external applications.
        /// </remarks>
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
        /// <param name="fromPath">The source path (starting point).</param>
        /// <param name="toPath">The destination path (target).</param>
        /// <returns>
        /// The relative path from <paramref name="fromPath"/> to <paramref name="toPath"/>,
        /// or <paramref name="toPath"/> if a relative path cannot be created.
        /// </returns>
        /// <remarks>
        /// Both paths are converted to absolute paths before computing the relationship.
        /// The result is normalized to use platform-specific directory separators.
        /// Returns <paramref name="toPath"/> if either parameter is null or empty, or
        /// if the operation fails.
        /// </remarks>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath) || string.IsNullOrEmpty(toPath))
            {
                return toPath;
            }

            try
            {
                // Convert both paths to absolute paths
                Uri fromUri = new Uri(GetFullPathSafe(fromPath));
                Uri toUri = new Uri(GetFullPathSafe(toPath));

                // Compute relative path
                Uri relativeUri = fromUri.MakeRelativeUri(toUri);
                string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

                // Normalize to platform-specific separators
                return NormalizePath(relativePath);
            }
            catch
            {
                // If anything fails, return the original destination path
                return toPath;
            }
        }
    }
}
