export default function MonitoringPaused() {
  return (
    <div className="flex min-h-[calc(100vh-120px)] items-center justify-center">
      <div className="w-full max-w-2xl rounded-3xl border border-yellow-500/30 bg-yellow-500/10 p-10 text-center shadow-xl">
        

        <h1 className="text-3xl font-bold text-yellow-200 light:text-yellow-700">
          Monitoring is Paused
        </h1>

        <p className="mx-auto mt-4 max-w-xl text-base leading-7 text-slate-300 light:text-slate-700">
          Live dashboard updates are currently turned off. Turn monitoring back on from the top bar
          to resume viewing updates.
        </p>

      </div>
    </div>
  )
}