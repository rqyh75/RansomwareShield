namespace CanaryAgent.Storage
{
    public class CanaryFileState
    {
        public string PersonaId { get; set; } = "";
        public string LogicalName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string FileType { get; set; } = "";
        public DateTime CreatedOn { get; set; }
        public DateTime LastModifiedOn { get; set; }
        public long LastObservedSize { get; set; }
        public DateTime LastObservedWriteTime { get; set; }
        public string? LastObservedHash { get; set; }
        public DateTime NextModificationTime { get; set; }
        public DateTime NextRenameTime { get; set; }

    }
}
