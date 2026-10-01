using SoapUI.Common;

namespace SoapUI.Models
{
    /// <summary>
    /// An RCC job descriptor (id, lease length, category, core count).
    /// </summary>
    public sealed class Job
    {
        public Job(string id, double expirationInSeconds = 600, int category = 0, double cores = 1)
        {
            Guard.AgainstNullOrWhiteSpace(id, nameof(id));
            Guard.AgainstNegative(expirationInSeconds, nameof(expirationInSeconds));
            Guard.AgainstNegative(cores, nameof(cores));

            Id = id.Trim();
            ExpirationInSeconds = expirationInSeconds;
            Category = category;
            Cores = cores;
        }

        public string Id { get; }

        public double ExpirationInSeconds { get; }

        public int Category { get; }

        public double Cores { get; }

        public override string ToString()
        {
            return "Job(Id=" + Id + ", Expires=" + ExpirationInSeconds + "s, Category=" + Category + ", Cores=" + Cores + ")";
        }
    }
}
