// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using CSVTranslationLookup.Configuration;
using CSVTranslationLookup.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace CSVTranslationLookup.FIleListeners
{
    /// <summary>
    /// Monitors JSON files in the editor and automatically reloads the configuration when the configuration file is saved.
    /// </summary>
    /// <remarks>
    /// This listener is registered for all JSON content types and attaches to documents that match
    /// the configuration filename. When a configuration file is saved, it triggers automatic reprocessing
    /// of the CSV translation lookup service. Event handlers are properly cleaned up when the text view is closed.
    /// </remarks>
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("json")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class ConfigurationFileListener : IVsTextViewCreationListener
    {
        /// <summary>
        /// Gets or sets the service used to convert between Visual Studio text views and WPF text views.
        /// </summary>
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        /// <summary>
        /// Gets or sets the service used to retrieve document information from text buffers.
        /// </summary>
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        /// <summary>
        /// The text document being monitored, if it matches the configuration filename.
        /// </summary>
        private ITextDocument _document;

        /// <summary>
        /// Called when a new text view is created in Visual Studio.
        /// </summary>
        /// <param name="textViewAdapter">The Visual Studio text view adapter.</param>
        /// <remarks>
        /// If the opened file is the configuration file (csvconfig.json), this method attaches
        /// a file save listener to trigger automatic configuration reloading. The text view
        /// closed event is also hooked to ensure proper cleanup of event handlers.
        /// </remarks>
        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out _document))
            {
                string filename = Path.GetFileName(_document.FilePath);

                // Only attach listener if this is the configuration file
                if (filename.Equals(Config.ConfigurationFilename, StringComparison.OrdinalIgnoreCase))
                {
                    _document.FileActionOccurred += DocumentSavedAsync;
                }
            }

            // Always hook closed event to ensure cleanup
            textView.Closed += TextviewClosed;
        }

        /// <summary>
        /// Handles text view closure by cleaning up event handlers.
        /// </summary>
        /// <param name="sender">The text view that was closed.</param>
        /// <param name="e">Event arguments.</param>
        /// <remarks>
        /// Unsubscribes from both the text view closed event and the document file action event
        /// to prevent memory leaks and ensure proper resource cleanup.
        /// </remarks>
        private void TextviewClosed(object sender, EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;

            if (view != null)
            {
                view.Closed -= TextviewClosed;
            }

            if (_document != null)
            {
                _document.FileActionOccurred -= DocumentSavedAsync;
            }
        }

        /// <summary>
        /// Handles document save events by reprocessing the configuration file.
        /// </summary>
        /// <param name="sender">The text document that triggered the event.</param>
        /// <param name="e">File action event arguments containing the file path and action type.</param>
        /// <remarks>
        /// Only processes saves to disk (not other file actions). When the configuration file is saved,
        /// it triggers the CSV translation lookup service to reload the configuration and reprocess all CSV files.
        /// Errors during processing are handled gracefully and reported to the user without showing a dialog.
        /// </remarks>
        private async void DocumentSavedAsync(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                try
                {
                    await CSVTranslationLookupPackage.Package?.LookupService?.ProcessConfigAsync(e.FilePath);
                }
                catch (Exception ex)
                {
                    await ErrorHandler.HandleAsync(
                        context: "processing saved configuration file",
                        exception: ex,
                        suggestion: "Check that csvconfig.json has valid JSON syntax and correct values.",
                        showDialog: false
                    );
                }
            }
        }
    }
}
