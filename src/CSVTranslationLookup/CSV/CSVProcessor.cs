// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CSVTranslationLookup.Services;

namespace CSVTranslationLookup.CSV
{
    internal class CSVProcessor
    {
        public event EventHandler<CSVProcessedEventArgs> CSVProcessed;

        public void Process(string filePath)
        {
            char delimiter = CSVTranslationLookupService.Config.Delimiter;
            char quote = CSVTranslationLookupService.Config.Quote;

            try
            {
                FileInfo file = new FileInfo(filePath);
                if (!file.Exists)
                {
                    return;
                }

                Dictionary<string, CSVItem> items = new Dictionary<string, CSVItem>();
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        int currentColumn = 0;
                        int currentLine = 0;
                        StringBuilder sb = new StringBuilder();

                        bool waitingForQuote = false;
                        int c;
                        string key = string.Empty;
                        string value = string.Empty;

                        while ((c = reader.Read()) != -1)
                        {
                            char character = (char)c;

                            if (!waitingForQuote)
                            {
                                if (character == quote)
                                {
                                    waitingForQuote = true;
                                    continue;
                                }

                                if (character == delimiter)
                                {
                                    if (reader.Peek() != -1)
                                    {
                                        if (currentColumn == 0)
                                        {
                                            key = SanatizeString(sb.ToString());
                                            sb.Clear();
                                            currentColumn++;
                                        }
                                        else if (currentColumn == 1)
                                        {
                                            value = SanatizeString(sb.ToString());
                                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                            {
                                                CSVItem item = new CSVItem(key, value, currentLine, filePath);
                                                items.Add(key, item);
                                            }
                                            currentColumn = 0;
                                            currentLine++;
                                            sb.Clear();
                                        }
                                    }
                                    continue;
                                }

                                if (character == '\n')
                                {
                                    if (reader.Peek() != -1)
                                    {
                                        currentColumn = 0;
                                        value = SanatizeString(sb.ToString());
                                        sb.Clear();
                                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                        {
                                            CSVItem item = new CSVItem(key, value, currentLine, filePath);
                                            items.Add(key, item);
                                        }
                                        currentLine++;

                                    }
                                }
                            }
                            else
                            {
                                if (character == quote)
                                {
                                    if (reader.Peek() == quote)
                                    {
                                        sb.Append('"');
                                        _ = reader.Read(); //   Discard the next quote
                                        continue;
                                    }
                                    else
                                    {
                                        waitingForQuote = false;
                                        continue;
                                    }
                                }
                            }

                            sb.Append(character);
                        }
                    }
                }

                OnCSVProcessed(filePath, items);

            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        private string SanatizeString(string value)
        {
            return value.TrimEnd(new char[] { '\n', '\r' }).Trim();
        }

        private void OnCSVProcessed(string filePath, Dictionary<string, CSVItem> items)
        {
            if (CSVProcessed is not null)
            {
                CSVProcessed(this, new CSVProcessedEventArgs(filePath, items));
            }
        }
    }
}
