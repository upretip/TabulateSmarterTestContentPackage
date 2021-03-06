﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using TabulateSmarterTestContentPackage.Models;

namespace TabulateSmarterTestContentPackage.Utilities
{
    public static class ReportingUtility
    {
        public static int ErrorCount { get; set; }
        public static string ErrorReportPath { get; set; }
        public static string CurrentPackageName { get; set; }
        public static bool DeDuplicate { get; set; }

        static TextWriter m_ErrorReport { get; set; }
        static HashSet<ShaHash> s_ErrorsReported = new HashSet<ShaHash>();

        private static void InitErrorReport()
        {
            if (m_ErrorReport != null) return;
            m_ErrorReport = new StreamWriter(ErrorReportPath, false, Encoding.UTF8);
            m_ErrorReport.WriteLine("Folder,BankKey,ItemId,ItemType,Category,Severity,ErrorMessage,Detail");

            var application = System.Reflection.Assembly.GetExecutingAssembly();
            string detail = $"version='{application.GetName().Version}' options='{Program.Options}'";
            m_ErrorReport.WriteLine(string.Join(",", string.Empty,
                string.Empty, string.Empty, string.Empty,
                ErrorCategory.System.ToString(), ErrorSeverity.Message.ToString(),
                "Tabulator Start", Tabulator.CsvEncode(detail)));
        }

        private static void InternalReportError(string folder, string itemType, string bankKey, string itemId, ErrorCategory category, ErrorSeverity severity, string msg, string detail)
        {
            // If deduplicate, find out if this error has already been reported for this particular item.
            if (DeDuplicate)
            {
                // Create a hash of the itemId and message
                var errHash = new ShaHash(string.Concat(itemType, bankKey, itemId, msg));

                // If it's aready in the set then exit
                if (!s_ErrorsReported.Add(errHash))
                {
                    return; // Already reported an error of this type on this item
                }
            }

            if (m_ErrorReport == null)
            {
                InitErrorReport();
            }

            if (CurrentPackageName != null)
            {
                folder = string.Concat(CurrentPackageName, "/", folder);
            }

            // "Folder,ItemType,BankKey,ItemId,Category,Severity,ErrorMessage,Detail"
            m_ErrorReport.WriteLine(string.Join(",", Tabulator.CsvEncode(folder),
                Tabulator.CsvEncode(bankKey), Tabulator.CsvEncode(itemId), Tabulator.CsvEncode(itemType),
                category.ToString(), severity.ToString(), Tabulator.CsvEncode(msg), Tabulator.CsvEncode(detail)));

            ++ErrorCount;
        }

        public static void ReportError(ItemIdentifier ii, ErrorCategory category, ErrorSeverity severity, string msg, string detail = null)
        {
            string folderName;
            string itemType;
            string bankKey;
            string itemId;
            if (ii != null)
            {
                folderName = ii.FolderName;
                itemType = ii.ItemType;
                bankKey = ii.BankKey.ToString();
                itemId = ii.ItemId.ToString();
            }
            else
            {
                folderName = string.Empty;
                itemType = null;
                bankKey = null;
                itemId = null;
            }
            InternalReportError(folderName, itemType, bankKey, itemId, category, severity, msg, detail);
        }

        public static void ReportError(string folder, ErrorCategory category, ErrorSeverity severity, string msg, string detail = null)
        {
            InternalReportError(folder, null, null, null, category, severity, msg, detail);
        }

        public static void ReportError(FileFolder folder, ErrorCategory category, ErrorSeverity severity, string msg, string detail = null)
        {
            string folderName = folder.RootedName;
            if (!string.IsNullOrEmpty(folderName) && folderName[0] == '/')
            {
                folderName = folderName.Substring(1);
            }
            InternalReportError(folderName, null, null, null, category, severity, msg, detail);
        }

        public static void ReportError(ItemIdentifier ii, ErrorCategory category, ErrorSeverity severity, string msg,
            string detail, params object[] args)
        {
            ReportError(ii, category, severity, msg, string.Format(System.Globalization.CultureInfo.InvariantCulture, detail, args));
        }

        public static void ReportError(ItemIdentifier ii, ErrorSeverity severity, Exception err)
        {
            ReportError(ii, ErrorCategory.Exception, severity, err.GetType().Name, err.ToString());
        }

        public static void ReportError(string validationOption, ItemIdentifier ii, ErrorCategory category,
            ErrorSeverity severity, string msg, string detail, params object[] args)
        {
            if (Program.gValidationOptions.IsEnabled(validationOption))
            {
                ReportError(ii, category, severity, msg, detail, args);
            }
        }

        public static void ReportError(string validationOption, ItemIdentifier ii, ErrorCategory category,
            ErrorSeverity severity, string msg, string detail = null)
        {
            if (Program.gValidationOptions.IsEnabled(validationOption))
            {
                ReportError(ii, category, severity, msg, detail);
            }
        }

        public static void ReportWitError(ItemIdentifier ii, ItemIdentifier witIt, ErrorSeverity severity, string msg,
            string detail = null)
        {
            detail = string.Concat($"wordlistId='{witIt.ItemId}' ", detail);
            ReportError(ii, ErrorCategory.Wordlist, severity, msg, detail);
        }

        public static void ReportWitError(ItemIdentifier ii, ItemIdentifier witIt, ErrorSeverity severity, string msg,
            string detail, params object[] args)
        {
            ReportWitError(ii, witIt, severity, msg, string.Format(System.Globalization.CultureInfo.InvariantCulture, detail, args));
        }

        public static void CloseReport()
        {
            if (m_ErrorReport != null)
            {
                m_ErrorReport.Dispose();
                m_ErrorReport = null;
            }
            s_ErrorsReported.Clear();
        }
    }
}