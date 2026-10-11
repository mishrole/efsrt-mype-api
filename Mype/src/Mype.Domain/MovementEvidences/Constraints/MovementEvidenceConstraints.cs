namespace Mype.Domain.MovementEvidences.Constraints
{
    public static class MovementEvidenceConstraints
    {
        public const int StorageProviderMaxLength = 50;
        public const int StorageContainerMaxLength = 63;
        public const int StorageObjectKeyMaxLength = 500;
        public const int OriginalFileNameMaxLength = 255;
        public const int MimeTypeMaxLength = 100;
        public const int StatusMaxLength = 30;
        public const int RemovalReasonMaxLength = 40;
    }
}
