// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
    internal class ProgressReporter : IAsyncDisposable
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

        private ProgressReporter(string operationName, int totalItems, bool logProgress = false)
        {
            _operationName = operationName;
            _totalItems = totalItems;
            _logProgress = logProgress;
            _completedItems = 0;
            _stopWatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Asynchronously creates a new <see cref="ProgressReporter"/> instance.
        /// </summary>
        /// <param name="operationName">The name of the operation being performed.</param>
        /// <param name="totalItems">The total number of items to process.</param>
        /// <param name="logProgress">Whether to log individual progress updates to the output window. Default is <see langword="false"/>.</param>
        /// <remarks>
        /// The stopwatch starts immediately and initial progress is reported to the status bar.
        /// </remarks>
        public static async Task<ProgressReporter> CreateAsync(string operationName, int totalItems, bool logProgress = false)
        {
            ProgressReporter reporter = new ProgressReporter(operationName, totalItems, logProgress);
            await reporter.UpdateProgressAsync();
            return reporter;
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
        public async Task ReportProgressAsync(string itemName = null)
        {
            _completedItems++;

            if (_logProgress && !string.IsNullOrEmpty(itemName))
            {
                await Logger.LogAsync($"[{_completedItems}/{_totalItems}] Processing: {itemName}");
            }

            await UpdateProgressAsync();
        }

        /// <summary>
        /// Reports progress for multiple items at once.
        /// </summary>
        /// <param name="count">The number of items completed.</param>
        /// <remarks>
        /// Use this overload when processing items in batches to avoid excessive status bar updates.
        /// </remarks>
        public async Task ReportProgressAsync(int count)
        {
            _completedItems += count;
            await UpdateProgressAsync();
        }

        /// <summary>
        /// Reports completion of the operation.
        /// </summary>
        /// <remarks>
        /// Stops the stopwatch, logs a completion message with elapsed time, updates the status bar,
        /// and clears the logger progress indicator. This method is called automatically by
        /// <see cref="Dispose"/> if not already complete.
        /// </remarks>
        public async Task CompleteAsync()
        {
            _stopWatch.Stop();

            string message = $"{_operationName} complete: {_completedItems} items in {FormatElapsedTime()}";

            await Logger.LogAsync(message);
            await CSVTranslationLookupPackage.StatusTextAsync(message);
            await Logger.LogProgressAsync(false);
        }

        /// <summary>
        /// Reports cancellation of the operation.
        /// </summary>
        /// <remarks>
        /// Stops the stopwatch, logs a cancellation message showing partial progress,
        /// updates the status bar, and clears the logger progress indicator.
        /// </remarks>
        public async Task CancelAsync()
        {
            _stopWatch.Stop();

            string message = $"{_operationName} cancelled after processing {_completedItems}/{_totalItems} items";

            await Logger.LogAsync(message);
            await CSVTranslationLookupPackage.StatusTextAsync(message);
            await Logger.LogProgressAsync(false);
        }

        /// <summary>
        /// Updates the progress display in both the logger and status bar.
        /// </summary>
        /// <remarks>
        /// When <see cref="TotalItems"/> is greater than 0, displays progress as a fraction and percentage.
        /// When <see cref="TotalItems"/> is 0, displays only the completed item count.
        /// </remarks>
        private async Task UpdateProgressAsync()
        {
            if (_totalItems > 0)
            {
                int percentage = (_completedItems * 100) / _totalItems;
                string label = $"{_operationName} ({_completedItems}/{_totalItems})";

                await Logger.LogProgressAsync(true, label, _completedItems, _totalItems);
                await CSVTranslationLookupPackage.StatusTextAsync($"{label} - {percentage}%");
            }
            else
            {
                string label = $"{_operationName} ({_completedItems} items)";
                await Logger.LogProgressAsync(true, label, _completedItems, 100);
                await CSVTranslationLookupPackage.StatusTextAsync(label);
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
        public async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                if (_stopWatch.IsRunning)
                {
                    await CompleteAsync();
                }
                _isDisposed = true;
            }
        }


    }
}
