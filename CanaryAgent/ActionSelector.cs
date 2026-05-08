using CanaryAgent.Storage;

namespace CanaryAgent.Core
{
    public class ActionSelector {
        //Missing3:Try if possible not to use AgentAction
        public AgentAction Decide(StateStore state, DateTime now)
        {

            bool createDue =
                LifecycleRules.CanCreateNewFile(state.Files.Count) &&
                (state.NextFileCreationTime == DateTime.MinValue || now >= state.NextFileCreationTime);

            bool renameDue = false;
            for (int i = 0; i < state.Files.Count; i++)
            {
                if (state.Files[i].NextRenameTime != DateTime.MinValue &&
                    now >= state.Files[i].NextRenameTime)
                {
                    renameDue = true;
                    break;
                }
            }

            bool modifyDue = false;
            for (int i = 0; i < state.Files.Count; i++)
            {
                if (state.Files[i].NextModificationTime != DateTime.MinValue &&
                    now >= state.Files[i].NextModificationTime)
                {
                    modifyDue = true;
                    break;
                }
            }

            // While the system is still building up, prioritize creation
            if (state.Files.Count < 25 && createDue)
                return AgentAction.CreateFile;

            // After enough files exist, allow rename/modify first
            if (renameDue)
                return AgentAction.RenameFile;

            if (modifyDue)
                return AgentAction.ModifyFile;

            if (createDue)
                return AgentAction.CreateFile;

            return AgentAction.None;

        }

    }
}
