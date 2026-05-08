namespace CanaryAgent.Core
{
    public static class LifecycleRules
    {
        // Maximum number of canary files the agent maintains at any one time.
        // Keeping the cap at 40 balances coverage across all persona types while
        // keeping disk and memory usage predictable.
        public static bool CanCreateNewFile(int existingFileCount)
        {
            return existingFileCount < 40;
        }

        // NOTE: File modification and rename scheduling is handled by time-based
        // NextModificationTime / NextRenameTime fields on CanaryFileState.
        // The previous CanModifyFile(daysSinceLastModification) and
        // CanRenameFile(ageInDays) methods were never called and have been removed.
    }
}
