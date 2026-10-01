using System.Collections.Generic;

namespace SoapUI.Models.Responses
{
    /// <summary>
    /// Structured outcome of parsing one SOAP response: lines for the log
    /// window plus an optional modal dialog payload. Keeps WinForms
    /// presentation decisions in the UI layer.
    /// </summary>
    public sealed class ResponseOutcome
    {
        public ResponseOutcome(string dialogTitle, string dialogText)
            : this(dialogTitle, dialogText, null)
        {
        }

        public ResponseOutcome(string dialogTitle, string dialogText, IList<string> logLines)
        {
            DialogTitle = dialogTitle ?? string.Empty;
            DialogText = dialogText ?? string.Empty;
            LogLines = logLines != null
                ? new List<string>(logLines)
                : new List<string>();
        }

        public string DialogTitle { get; }

        public string DialogText { get; }

        public IList<string> LogLines { get; }

        public bool HasDialog
        {
            get { return !string.IsNullOrEmpty(DialogText); }
        }

        public static ResponseOutcome Info(string title, string text)
        {
            return new ResponseOutcome(title, text);
        }

        public static ResponseOutcome Silent(IList<string> logLines)
        {
            return new ResponseOutcome(null, null, logLines);
        }
    }
}
