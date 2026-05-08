namespace CanaryAgent.Utils
{
    internal class TimeProvider
    {
        public virtual DateTime Now()
        {
            return DateTime.Now;
        }
        public bool IsNewDay(DateTime lastRun)
        {
            return Now().Date > lastRun.Date;
        }
    }
}
