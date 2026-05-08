using CanaryAgent.DataGen;
using CanaryAgent.Storage;

namespace CanaryAgent.Actors
{
    public class FileActor
    {
        private static readonly Random random = new Random();
        private readonly IContentGenerator content;
        private readonly List<FilePersona> availablePersonas;

        public FileActor(IContentGenerator? contentGenerator = null)
        {
            content = contentGenerator ?? new SyntheticContentGenerator();
            availablePersonas = PersonaRegistry.GetAllPersonas();
        }

        // Single authoritative definition – Agent.cs references this via FileActor.TopicFolderName.
        public static string TopicFolderName(PersonaType type) => type switch
        {
            PersonaType.Finance_Accounting   => "Finance",
            PersonaType.HR_Payroll           => "HR",
            PersonaType.IT_Credentials       => "IT",
            PersonaType.Operations_Inventory => "Operations",
            PersonaType.Sales_CRM            => "Sales",
            PersonaType.Legal_Contracts      => "Legal",
            PersonaType.Executive_Reports    => "Executive",
            PersonaType.System_Logs          => "Logs",
            _                                => "Work"
        };

        /// Append realistic content to existing files based on their persona.
        public void AppendRealisticContent(CanaryFileState file, DateTime now)
        {
            if (!File.Exists(file.FullPath)) return;

            FilePersona? persona = null;
            for (int i = 0; i < availablePersonas.Count; i++)
            {
                if (availablePersonas[i].PersonaId == file.PersonaId)
                {
                    persona = availablePersonas[i];
                    break;
                }
            }

            if (persona == null || !persona.SupportsAppending) return;

            string existingContent = File.ReadAllText(file.FullPath);
            string newContent = content.AppendToContent(existingContent, persona.Type);
            File.WriteAllText(file.FullPath, newContent);

            var info = new FileInfo(file.FullPath);
            file.LastModifiedOn       = now;
            file.LastObservedSize     = info.Length;
            file.LastObservedWriteTime = info.LastWriteTime;
            file.LastObservedHash     = Detection.CanaryWatcher.ComputeFileHash(file.FullPath);
        }

        /// Copy file with version increment, returning the new CanaryFileState.
        public CanaryFileState? CopyFileWithVersion(CanaryFileState sourceFile, DateTime now,
            List<CanaryFileState> allFiles)
        {
            if (!File.Exists(sourceFile.FullPath)) return null;

            string? dir = Path.GetDirectoryName(sourceFile.FullPath);
            if (string.IsNullOrEmpty(dir)) return null;

            string baseName = GetBaseFileName(sourceFile.FullPath);
            string ext      = Path.GetExtension(sourceFile.FullPath);

            int highestVersion = 1;
            for (int i = 0; i < allFiles.Count; i++)
            {
                string fileBase = GetBaseFileName(allFiles[i].FullPath);
                if (string.Equals(fileBase, baseName, StringComparison.OrdinalIgnoreCase))
                {
                    int version = GetVersionNumber(allFiles[i].FullPath);
                    if (version > highestVersion)
                        highestVersion = version;
                }
            }

            int nextVersion = highestVersion + 1;
            string newPath  = Path.Combine(dir, $"{baseName}_v{nextVersion}{ext}");

            if (File.Exists(newPath)) return null;

            File.Copy(sourceFile.FullPath, newPath);
            var info = new FileInfo(newPath);

            return new CanaryFileState
            {
                PersonaId              = sourceFile.PersonaId,
                LogicalName            = sourceFile.LogicalName,
                FullPath               = newPath,
                FileType               = sourceFile.FileType,
                CreatedOn              = now,
                LastModifiedOn         = now,
                LastObservedSize       = info.Length,
                LastObservedWriteTime  = info.LastWriteTime,
                LastObservedHash       = Detection.CanaryWatcher.ComputeFileHash(newPath)
            };
        }

        public CanaryFileState? CreateFileFromPersonaType(
            string targetDir, DateTime now,
            List<CanaryFileState> existingFiles, PersonaType type)
        {
            var candidates = availablePersonas.Where(p => p.Type == type).ToList();
            if (candidates.Count == 0) return null;

            var persona = candidates[random.Next(candidates.Count)];

            Directory.CreateDirectory(targetDir);

            string fileName    = PersonaRegistry.ApplyFileNamePattern(persona.FileNamePattern!);
            string fullPath    = Path.Combine(targetDir, fileName);
            string fileContent = persona.GenerateContent!(content);
            File.WriteAllText(fullPath, fileContent);

            var info = new FileInfo(fullPath);
            return new CanaryFileState
            {
                PersonaId              = persona.PersonaId!,
                LogicalName            = persona.LogicalName!,
                FullPath               = fullPath,
                FileType               = persona.FileExtension!.TrimStart('.'),
                CreatedOn              = now,
                LastModifiedOn         = now,
                LastObservedSize       = info.Length,
                LastObservedWriteTime  = info.LastWriteTime,
                LastObservedHash       = Detection.CanaryWatcher.ComputeFileHash(fullPath)
            };
        }

        private static string GetBaseFileName(string fullPath)
        {
            string name   = Path.GetFileNameWithoutExtension(fullPath);
            int vIndex    = name.LastIndexOf("_v", StringComparison.Ordinal);
            return vIndex >= 0 ? name[..vIndex] : name;
        }

        private static int GetVersionNumber(string fullPath)
        {
            string name  = Path.GetFileNameWithoutExtension(fullPath);
            int vIndex   = name.LastIndexOf("_v", StringComparison.Ordinal);
            if (vIndex >= 0 && int.TryParse(name[(vIndex + 2)..], out int ver))
                return ver;
            return 1;
        }
    }
}
