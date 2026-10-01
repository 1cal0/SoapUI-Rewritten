using SoapUI.Common;
using SoapUI.Models;

namespace SoapUI.Services.Soap
{
    /// <summary>
    /// Typed client for the RCC (RCCServiceSoap) endpoint set.
    /// Builds body fragments and delegates HTTP to <see cref="SoapTransport"/>.
    /// </summary>
    public sealed class RccServiceClient
    {
        private readonly ServiceEndpoint _endpoint;
        private readonly SoapTransport _transport;

        public RccServiceClient(ServiceEndpoint endpoint, SoapTransport transport)
        {
            Guard.AgainstNull(endpoint, nameof(endpoint));
            Guard.AgainstNull(transport, nameof(transport));

            _endpoint = endpoint;
            _transport = transport;
        }

        public ServiceEndpoint Endpoint
        {
            get { return _endpoint; }
        }

        public string HelloWorld()
        {
            return _transport.Send(_endpoint, "HelloWorld", "\t\t<ns1:HelloWorld>\r\n\t\t</ns1:HelloWorld>");
        }

        public string GetVersion()
        {
            return _transport.Send(_endpoint, "GetVersion", "\t\t<ns1:GetVersion>\r\n\t\t</ns1:GetVersion>");
        }

        public string GetStatus()
        {
            return _transport.Send(_endpoint, "GetStatus", "\t\t<ns1:GetStatus>\r\n\t\t</ns1:GetStatus>");
        }

        public string OpenJobEx(Job job, Script script)
        {
            Guard.AgainstNull(job, nameof(job));
            Guard.AgainstNull(script, nameof(script));

            string body = "\t\t<ns1:OpenJobEx>\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildJobFragment(job) + "\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildScriptFragment(script, false)
                + "\r\n\t\t</ns1:OpenJobEx>";
            return _transport.Send(_endpoint, "OpenJobEx", body);
        }

        public string OpenJob(Job job, Script script)
        {
            Guard.AgainstNull(job, nameof(job));
            Guard.AgainstNull(script, nameof(script));

            string body = "\t\t<ns1:OpenJob>\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildJobFragment(job) + "\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildScriptFragment(script, false)
                + "\r\n\t\t</ns1:OpenJob>";
            return _transport.Send(_endpoint, "OpenJob", body);
        }

        public string BatchJob(Job job, Script script)
        {
            Guard.AgainstNull(job, nameof(job));
            Guard.AgainstNull(script, nameof(script));

            string body = "\t\t<ns1:BatchJob>\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildJobFragment(job) + "\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildScriptFragment(script, false)
                + "\r\n\t\t</ns1:BatchJob>";
            return _transport.Send(_endpoint, "BatchJob", body);
        }

        public string BatchJobEx(Job job, Script script)
        {
            Guard.AgainstNull(job, nameof(job));
            Guard.AgainstNull(script, nameof(script));

            string body = "\t\t<ns1:BatchJobEx>\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildJobFragment(job) + "\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildScriptFragment(script, false)
                + "\r\n\t\t</ns1:BatchJobEx>";
            return _transport.Send(_endpoint, "BatchJobEx", body);
        }

        public string RenewLease(string jobId, double expirationInSeconds)
        {
            Guard.AgainstNullOrWhiteSpace(jobId, nameof(jobId));

            string body = "\t\t<ns1:RenewLease>\r\n"
                + "\t\t\t<ns1:jobID>" + XmlHelper.Escape(jobId) + "</ns1:jobID>\r\n"
                + "\t\t\t<ns1:expirationInSeconds>" + expirationInSeconds + "</ns1:expirationInSeconds>\r\n"
                + "\t\t</ns1:RenewLease>";
            return _transport.Send(_endpoint, "RenewLease", body);
        }

        public string ExecuteEx(string jobId, Script script)
        {
            Guard.AgainstNullOrWhiteSpace(jobId, nameof(jobId));
            Guard.AgainstNull(script, nameof(script));

            string body = "\t\t<ns1:ExecuteEx>\r\n"
                + "\t\t\t<ns1:jobID>" + XmlHelper.Escape(jobId) + "</ns1:jobID>\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildScriptFragment(script, false)
                + "\r\n\t\t</ns1:ExecuteEx>";
            return _transport.Send(_endpoint, "ExecuteEx", body);
        }

        public string Execute(string jobId, Script script)
        {
            Guard.AgainstNullOrWhiteSpace(jobId, nameof(jobId));
            Guard.AgainstNull(script, nameof(script));

            string body = "\t\t<ns1:Execute>\r\n"
                + "\t\t\t<ns1:jobID>" + XmlHelper.Escape(jobId) + "</ns1:jobID>\r\n\t\t\t"
                + SoapEnvelopeBuilder.BuildScriptFragment(script, false)
                + "\r\n\t\t</ns1:Execute>";
            return _transport.Send(_endpoint, "Execute", body);
        }

        public string CloseJob(string jobId)
        {
            Guard.AgainstNullOrWhiteSpace(jobId, nameof(jobId));

            return _transport.Send(_endpoint, "CloseJob",
                "\t\t<ns1:CloseJob>\r\n\t\t\t<ns1:jobID>" + XmlHelper.Escape(jobId) + "</ns1:jobID>\r\n\t\t</ns1:CloseJob>");
        }

        public string DiagEx(int type, string jobId)
        {
            return _transport.Send(_endpoint, "DiagEx",
                "\t\t<ns1:DiagEx>\r\n\t\t\t<ns1:type>" + type + "</ns1:type>\r\n"
                + "\t\t\t<ns1:jobID>" + XmlHelper.Escape(jobId ?? string.Empty) + "</ns1:jobID>\r\n\t\t</ns1:DiagEx>");
        }

        public string Diag(int type, string jobId)
        {
            return _transport.Send(_endpoint, "Diag",
                "\t\t<ns1:Diag>\r\n\t\t\t<ns1:type>" + type + "</ns1:type>\r\n"
                + "\t\t\t<ns1:jobID>" + XmlHelper.Escape(jobId ?? string.Empty) + "</ns1:jobID>\r\n\t\t</ns1:Diag>");
        }

        public string GetAllJobsEx()
        {
            return _transport.Send(_endpoint, "GetAllJobsEx", "\t\t<ns1:GetAllJobsEx>\r\n\t\t</ns1:GetAllJobsEx>");
        }

        public string GetAllJobs()
        {
            return _transport.Send(_endpoint, "GetAllJobs", "\t\t<ns1:GetAllJobs>\r\n\t\t</ns1:GetAllJobs>");
        }

        public string CloseExpiredJobs()
        {
            return _transport.Send(_endpoint, "CloseExpiredJobs", "\t\t<ns1:CloseExpiredJobs>\r\n\t\t</ns1:CloseExpiredJobs>");
        }

        public string CloseAllJobs()
        {
            return _transport.Send(_endpoint, "CloseAllJobs", "\t\t<ns1:CloseAllJobs>\r\n\t\t</ns1:CloseAllJobs>");
        }
    }
}
