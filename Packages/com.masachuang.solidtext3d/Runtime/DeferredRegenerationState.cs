namespace MasaChuang.SolidText3D
{
    internal sealed class DeferredRegenerationState
    {
        public long LastRequestedVersion { get; set; }

        public long LastAppliedVersion { get; set; }

        public TextStateRequest? InFlightRequest { get; set; }

        public TextStateRequest? PendingLatestRequest { get; set; }

        public PreparedDisplayResult ReadyResult { get; set; }

        public DisplayResultSignature LastAppliedSignature { get; set; }

        public bool HasPendingWork => InFlightRequest.HasValue || PendingLatestRequest.HasValue || ReadyResult != null;

        public void ClearReadyResult()
        {
            ReadyResult = null;
        }
    }
}