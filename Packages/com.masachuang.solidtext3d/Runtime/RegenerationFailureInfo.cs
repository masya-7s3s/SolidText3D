namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// deferred regeneration が失敗したときの通知情報。
    /// </summary>
    public readonly struct RegenerationFailureInfo
    {
        /// <summary>
        /// deferred regeneration が失敗したときの通知情報を初期化する。
        /// </summary>
        /// <param name="requestVersion">失敗した request の version。</param>
        /// <param name="requestedText">失敗した request のテキスト。</param>
        /// <param name="message">簡潔な失敗理由。</param>
        public RegenerationFailureInfo(long requestVersion, string requestedText, string message)
        {
            RequestVersion = requestVersion;
            RequestedText = requestedText ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// 失敗した request の単調増加 version。
        /// </summary>
        public long RequestVersion { get; }

        /// <summary>
        /// 失敗時点の要求テキスト。
        /// </summary>
        public string RequestedText { get; }

        /// <summary>
        /// 呼び出し側へ返す簡潔な失敗理由。
        /// </summary>
        public string Message { get; }
    }
}