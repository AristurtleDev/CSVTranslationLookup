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
    internal class ProgressReporter : IDisposable
    {
        private readonly string _operationName;
        private readonly int _totalItems;
        private readonly Stopwatch _stopWatch;
        private readonly bool _logProgress;
        private int _completedItems;
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
        /// Gets teh number of completed items.
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
        /// Gets the elapsed time.
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
        /// <param name="logProgress">Whether to log progress to the output window.</param>
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
        /// <param name="itemName">Optionalname of the item being processed.</param>
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
        public void ReportProgress(int count)
        {
            _completedItems += count;
            UpdateProgress();
        }

        /// <summary>
        /// Reports completion of operation.
        /// </summary>
        public void Complete()
        {
            _stopWatch.Stop();

            string message = $"{_operationName} complete: {_completedItems} items in {FormatElapsedTime()}";

            Logger.Log(message);
            CSVTranslationLookupPackage.StatusTextAsync(message);
            Logger.LogProgress(false);
        }

        public void Cancel()
        {
            _stopWatch.Stop();

            string message = $"{_operationName} cancelled after processing {_completedItems}/{_totalItems} items";

            Logger.Log(message);
            CSVTranslationLookupPackage.StatusTextAsync(message);
            Logger.LogProgress(false);
        }

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

        /// <inheritdoc/>
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
