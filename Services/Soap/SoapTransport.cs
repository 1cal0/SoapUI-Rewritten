using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using SoapUI.Common;
using SoapUI.Models;

namespace SoapUI.Services.Soap
{
    /// <summary>
    /// Transport failure carrying the SOAP action plus any response body
    /// the server returned. Thrown by <see cref="SoapTransport"/> instead
    /// of showing UI, so callers decide how to present errors.
    /// </summary>
    public sealed class SoapTransportException : Exception
    {
        public SoapTransportException(string action, string message)
            : base(message)
        {
            Action = action;
        }

        public SoapTransportException(string action, string message, Exception inner)
            : base(message, inner)
        {
            Action = action;
        }

        public string Action { get; }

        public string ResponseBody { get; set; }

        public bool HasHttpResponse { get; set; }

        public int? StatusCode { get; set; }

        public string FaultCode { get; set; }

        public string FaultString { get; set; }
    }

    /// <summary>
    /// Minimal HTTP transport for SOAP payloads. One place owns headers,
    /// URL selection (RCC vs RBXGS) and error mapping.
    /// </summary>
    public sealed class SoapTransport
    {
        private readonly int _timeoutMs;

        public SoapTransport()
            : this(30000)
        {
        }

        public SoapTransport(int timeoutMs)
        {
            _timeoutMs = timeoutMs > 0 ? timeoutMs : 30000;
        }

        /// <summary>
        /// POSTs an already-built body fragment wrapped in the correct
        /// envelope and returns the raw XML response.
        /// </summary>
        public string Send(ServiceEndpoint endpoint, string action, string bodyFragment)
        {
            Guard.AgainstNull(endpoint, nameof(endpoint));
            Guard.AgainstNullOrWhiteSpace(action, nameof(action));

            string envelope = endpoint.IsRbxgs
                ? SoapEnvelopeBuilder.BuildRbxgsEnvelope(bodyFragment ?? string.Empty)
                : SoapEnvelopeBuilder.BuildRccEnvelope(endpoint.BaseUrl, bodyFragment ?? string.Empty);

            string url = endpoint.ToUrl();

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "text/xml; charset=utf-8";
                request.Accept = "text/xml";
                request.Timeout = _timeoutMs;
                request.Headers.Add("SOAPAction", action);
                // Legacy servers expect these; harmless on modern stacks.
                request.Headers.Add("Cache-Control", "no-cache");
                request.Headers.Add("Pragma", "no-cache");

                byte[] bytes = Encoding.UTF8.GetBytes(envelope);
                request.ContentLength = bytes.Length;

                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bytes, 0, bytes.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                var httpResponse = ex.Response as HttpWebResponse;
                string responseBody = TryReadErrorBody(ex);
                string faultCode;
                string faultString;
                ParseSoapFault(responseBody, out faultCode, out faultString);

                var message = "SOAP request '" + action + "' to " + url + " failed: " + ex.Message;
                var transportException = new SoapTransportException(action, message, ex)
                {
                    ResponseBody = responseBody,
                    HasHttpResponse = ex.Response != null,
                    StatusCode = httpResponse != null ? (int?)httpResponse.StatusCode : null,
                    FaultCode = faultCode,
                    FaultString = faultString
                };
                throw transportException;
            }
        }

        private static void ParseSoapFault(string responseBody, out string faultCode, out string faultString)
        {
            faultCode = null;
            faultString = null;

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return;
            }

            try
            {
                var fault = XDocument.Parse(responseBody)
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Fault");

                if (fault == null)
                {
                    return;
                }

                var code = fault.Elements().FirstOrDefault(x => x.Name.LocalName == "faultcode");
                var message = fault.Elements().FirstOrDefault(x => x.Name.LocalName == "faultstring");
                faultCode = code != null ? code.Value : null;
                faultString = message != null ? message.Value : null;
            }
            catch
            {
                faultCode = null;
                faultString = null;
            }
        }

        private static string TryReadErrorBody(WebException ex)
        {
            try
            {
                if (ex.Response == null)
                {
                    return null;
                }

                using (var stream = ex.Response.GetResponseStream())
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
