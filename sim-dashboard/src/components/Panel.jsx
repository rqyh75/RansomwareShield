export default function Panel({ title, right, children, className = "" }) {
  return (
    <section
      className={
        "rounded-2xl border border-slate-800/80 bg-slate-900/35 " +
        "shadow-[0_0_0_1px_rgba(30,41,59,0.2),0_20px_60px_rgba(0,0,0,0.35)] " +
        "backdrop-blur " +
        className
      }
    >
      {(title || right) && (
        <div className="flex items-center justify-between border-b border-slate-800/70 px-4 py-3">
          <h3 className="text-sm font-semibold text-slate-100">{title}</h3>
          <div className="text-xs text-slate-400">{right}</div>
        </div>
      )}
      <div className="p-4">{children}</div>
    </section>
  )
}
