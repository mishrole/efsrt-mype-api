namespace Mype.Shared.Constants
{
    public static class Env
    {
        public const string ConnectionStringKey = "DB_CONNECTION";
        public const string ServiceNameStringKey = "SERVICE_NAME";
        public const string TimeZoneStringKey = "TIMEZONE";
        public const string OriginsConfigurationStringKey = "ORIGINS_CONFIGURATION";
        public const string JwtSecretKeyStringKey = "JWT_SECRET_KEY";
        public const string JwtIssuerStringKey = "JWT_ISSUER";
        public const string JwtAudienceStringKey = "JWT_AUDIENCE";
        public const string RetryCountStringKey = "RETRY_COUNT";
        public const string SleepDurationProviderStringKey = "SLEEP_DURATION_PROVIDER";
        public const string CorsPolicyNameStringKey = "CORS_POLICY_NAME";
        public const string JwtExpirationMinutesStringKey = "JWT_EXPIRATION_MINUTES";
        public const string EvidenceStorageProviderStringKey = "EVIDENCE_STORAGE_PROVIDER";
        public const string EvidenceStorageServiceUriStringKey = "EVIDENCE_STORAGE_SERVICE_URI";
        public const string EvidenceStorageContainerStringKey = "EVIDENCE_STORAGE_CONTAINER";
        public const string EvidenceMaxSizeBytesStringKey = "EVIDENCE_MAX_SIZE_BYTES";
        public const string EvidenceReadUriMinutesStringKey = "EVIDENCE_READ_URI_MINUTES";
        public const string EvidencePurgeRetentionDaysStringKey = "EVIDENCE_PURGE_RETENTION_DAYS";
    }
}
