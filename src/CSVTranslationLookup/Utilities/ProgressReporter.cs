// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace CSVTranslationLookup.Utilities
{
    /// <summary>
    /// Provides progress reporting for long-running operations.
    /// </summary>
    /// <remarks>
    /// Tracks operation progress and reports it to both the output window logger and the
    /// Visual Studio status bar. Automatically completes the operation when disposed if not
    /// already complete. Use within a using statement for automatic cleanup.
    /// </remarks>
    internal class ProgressReporter : IDisposable
    {
        /// <summary>
        /// The name of the operation being tracked.
        /// </summary>
        private readonly string _operationName;

        /// <summary>
        /// The total number of items to process.
        /// </summary>
        private readonly int _totalItems;

        /// <summary>
        /// Measures the elapsed time of the operation.
        /// </summary>
        private readonly Stopwatch _stopWatch;

        /// <summary>
        /// Indicates whether to log individual progress updates to the output window.
        /// </summary>
        private readonly bool _logProgress;

        /// <summary>
        /// The number of items that have been processed so far.
        /// </summary>
        private int _completedItems;

        /// <summary>
        /// Tracks whether this instance has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Gets the current progress percentage (0 - 100).
        /// </summary>
        public int ProgressPercentage
        {
            get
            {
                if (_totalItems > 0)
                {
                    return (_completedItems * 100) / _totalItems;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the number of completed items.
        /// </summary>
        public int CompletedItems
        {
            get
            {
                return _completedItems;
            }
        }

        /// <summary>
        /// Gets the total number of items.
        /// </summary>
        public int TotalItems
        {
            get
            {
                return _totalItems;
            }
        }

        /// <summary>
        /// Gets the elapsed time since the operation started.
        /// </summary>
        public TimeSpan ElapsedTime
        {
            get
            {
                return _stopWatch.Elapsed;
            }
        }

        /// <summary>
        /// Gets whether the operation is complete.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return _completedItems >= _totalItems;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressReporter"/> class.
        /// </summary>
        /// <param name="operationName">The name of the operation being performed.</param>
        /// <param name="totalItems">The total number of items to process.</param>
        /// <param name="logProgress">Whether to log individual progress updates to the output window. Default is <see langword="false"/>.</param>
        /// <remarks>
        /// The stopwatch starts immediately and initial progress is reported to the status bar.
        /// </remarks>
        public ProgressReporter(string operationName, int totalItems, bool logProgress = false)
        {
            _operationName = operationName;
            _totalItems = totalItems;
            _logProgress = logProgress;
            _completedItems = 0;
            _stopWatch = Stopwatch.StartNew();

            // Start Progress in status par
            UpdateProgress();
        }

        /// <summary>
        /// Reports progress for a single item.
        /// </summary>
        /// <param name="itemName">Optional name of the item being processed. If provided and logging is enabled, the item name is logged.</param>
        /// <remarks>
        /// Increments the completed item count and updates both the logger and status bar.
        /// If <paramref name="itemName"/> is provided and logging is enabled, a detailed progress
        /// entry is written to the output window.
        /// </remarks>
        public void ReportProgress(string itemName = null)
        {
            _completedItems++;

            if (_logProgress && !string.IsNullOrEmpty(itemName))
            {
                Logger.Log($"[{_completedItems}/{_totalItems}] Processing: {itemName}");
            }

            UpdateProgress();
        }

        /// <summary>
        /// Reports progress for multiple items at once.
        /// </summary>
        /// <param name="count">The number of items completed.</param>
        /// <remarks>
        /// Use this overload when processing items in batches to avoid excessive status bar updates.
        /// </remarks>
        public void ReportProgress(int count)
        {
            _completedItems += count;
            UpdateProgress();
        }

        /// <summary>
        /// Reports completion of the operation.
        /// </summary>
        /// <remarks>
        /// Stops the stopwatch, logs a completion message with elapsed time, updates the status bar,
        /// and clears the logger progress indicator. This method is called automatically by
        /// <see cref="Dispose"/> if not already complete.
        /// </remarks>
        public void Complete()
        {
            _stopWatch.Stop();

            string message = $"{_operationName} complete: {_completedItems} items in {FormatElapsedTime()}";

            Logger.Log(message);
            CSVTranslationLookupPackage.StatusTextAsync(message);
            Logger.LogProgress(false);
        }

        /// <summary>
        /// Reports cancellation of the operation.
        /// </summary>
        /// <remarks>
        /// Stops the stopwatch, logs a cancellation message showing partial progress,
        /// updates the status bar, and clears the logger progress indicator.
        /// </remarks>
        public void Cancel()
        {
            _stopWatch.Stop();

            string message = $"{_operationName} cancelled after processing {_completedItems}/{_totalItems} items";

            Logger.Log(message);
            CSVTranslationLookupPackage.StatusTextAsync(message);
            Logger.LogProgress(false);
        }

        /// <summary>
        /// Updates the progress display in both the logger and status bar.
        /// </summary>
        /// <remarks>
        /// When <see cref="TotalItems"/> is greater than 0, displays progress as a fraction and percentage.
        /// When <see cref="TotalItems"/> is 0, displays only the completed item count.
        /// </remarks>
        private void UpdateProgress()
        {
            if (_totalItems > 0)
            {
                int percentage = (_completedItems * 100) / _totalItems;
                string label = $"{_operationName} ({_completedItems}/{_totalItems})";

                Logger.LogProgress(true, label, _completedItems, _totalItems);
                CSVTranslationLookupPackage.StatusTextAsync($"{label} - {percentage}%");
            }
            else
            {
                string label = $"{_operationName} ({_completedItems} items)";
                Logger.LogProgress(true, label, _completedItems, 100);
                CSVTranslationLookupPackage.StatusTextAsync(label);
            }
        }

        /// <summary>
        /// Formats the elapsed time in a human-readable format.
        /// </summary>
        /// <returns>
        /// Formatted time string: milliseconds for &lt;1s, seconds with one decimal for &lt;1m,
        /// or minutes and seconds for longer durations.
        /// </returns>
        private string FormatElapsedTime()
        {
            var elapsed = _stopWatch.Elapsed;

            if (elapsed.TotalSeconds < 1)
            {
                return $"{elapsed.TotalMilliseconds:F0}ms";
            }
            else if (elapsed.TotalMinutes < 1)
            {
                return $"{elapsed.TotalSeconds:F1}s";
            }
            else
            {
                return $"{elapsed.Minutes}m {elapsed.Seconds}s";
            }
        }

        /// <summary>
        /// Releases resources and completes the operation if not already complete.
        /// </summary>
        /// <remarks>
        /// If the stopwatch is still running when disposed, <see cref="Complete"/> is called automatically.
        /// This ensures progress indicators are cleared even if the operation wasn't explicitly completed.
        /// </remarks>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_stopWatch.IsRunning)
                {
                    Complete();
                }
                _isDisposed = true;
            }
        }


    }
}
