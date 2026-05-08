import { useEffect, useMemo, useState } from "react"
import Panel from "../components/Panel"

function SummaryCard({ value, label, tone = "default" }) {
  const toneCls =
    tone === "blue"
      ? "border-sky-500/30 bg-sky-500/8"
      : tone === "amber"
      ? "border-amber-500/30 bg-amber-500/8"
      : tone === "red"
      ? "border-red-500/30 bg-red-500/8"
      : "border-slate-800 bg-slate-900/40"

  return (
    <div className={`rounded-2xl border px-4 py-3 ${toneCls}`}>
      <div className="text-2xl font-semibold text-slate-100">{value}</div>
      <div className="mt-1 text-xs text-slate-400">{label}</div>
    </div>
  )
}

function StatusBadge({ status }) {
  const s = String(status || "").toLowerCase()

  const cls =
    s === "blocked"
      ? "border-red-500/35 bg-red-500/12 text-red-300"
      : s === "suspicious"
      ? "border-yellow-500/35 bg-yellow-500/12 text-yellow-300"
      : "border-emerald-500/30 bg-emerald-500/10 text-emerald-300"

  return (
    <span className={`inline-flex min-w-20 justify-center rounded-md border px-2 py-1 text-xs font-medium capitalize ${cls}`}>
      {s}
    </span>
  )
}

function TypeBadge({ type }) {
  const map = {
    write: "border-sky-500/30 bg-sky-500/10 text-sky-300",
    rename: "border-violet-500/30 bg-violet-500/10 text-violet-300",
    create: "border-emerald-500/30 bg-emerald-500/10 text-emerald-300",
    delete: "border-rose-500/30 bg-rose-500/10 text-rose-300",
    set_info: "border-amber-500/30 bg-amber-500/10 text-amber-300",
  }

  return (
    <span className={`inline-flex rounded-md border px-2 py-1 text-xs font-medium uppercase ${map[type] || "border-slate-700 bg-slate-800/40 text-slate-300"}`}>
      {type}
    </span>
  )
}

function ActionButton({ action }) {
  const a = String(action || "").toLowerCase()

  const cls =
    a === "review"
      ? "border-blue-500/35 bg-blue-500/10 text-blue-300"
      : a === "inspect"
      ? "border-indigo-500/35 bg-indigo-500/10 text-indigo-300"
      : "border-slate-700 bg-slate-800/40 text-slate-300"

  return (
    <button className={`rounded-md border px-3 py-1 text-xs font-medium capitalize transition hover:opacity-90 ${cls}`}>
      {a}
    </button>
  )
}

export default function Minifilters() {
  const [search, setSearch] = useState("")
  const [rows, setRows] = useState([])
  const [summary, setSummary] = useState({
    monitoredProcesses: 0,
    filesSeen: 0,
    threatEvents: 0,
  })
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetch("/api/detection-activity")
      .then((res) => res.json())
      .then((data) => {
        setRows(data.items || [])
        setSummary(
          data.summary || {
            monitoredProcesses: 0,
            filesSeen: 0,
            threatEvents: 0,
          }
        )
      })
      .catch((err) => console.error("Failed to load minifilters:", err))
      .finally(() => setLoading(false))
  }, [])

  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows

    return rows.filter((row) =>
      `${row.time} ${row.type} ${row.status} ${row.lastAction} ${row.path} ${row.action}`
        .toLowerCase()
        .includes(q)
    )
  }, [rows, search])

  return (
    <div className="mx-auto max-w-7xl">
      <div className="mb-5 flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <h1 className="text-xl font-semibold text-slate-100">Detection Activity</h1>
          <p className="mt-1 text-sm text-slate-400">
            monitor suspicious endpoint activity from Minifilter and ETW sources
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button className="rounded-xl border border-slate-800 bg-slate-900/40 px-3 py-2 text-sm text-slate-200 transition hover:bg-slate-900">
            Export
          </button>
        </div>
      </div>

      <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-4">
        <SummaryCard value={summary.monitoredProcesses} label="Monitored Processes" tone="blue" />
        <SummaryCard value={summary.filesSeen} label="Files Seen" tone="amber" />
        <SummaryCard value={summary.threatEvents} label="Threat Events" tone="red" />

        <div className="rounded-2xl border border-slate-800 bg-slate-900/40 px-4 py-3">
          <div className="text-xs text-slate-400">Current Status</div>
          <div className="mt-2 flex items-center gap-2">
            <span className="inline-block h-2.5 w-2.5 rounded-full bg-emerald-400 shadow-[0_0_18px_rgba(74,222,128,0.7)]" />
            <span className="text-sm font-medium text-emerald-300">Active</span>
          </div>
        </div>
      </div>

      <Panel title="" right="">
        <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex w-full max-w-xl items-center rounded-xl border border-slate-800 bg-slate-950/50 px-3 py-2">
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by type, status, action or path..."
              className="w-full bg-transparent text-sm text-slate-100 placeholder:text-slate-500 focus:outline-none"
            />
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => setSearch("")}
              className="rounded-xl border border-slate-800 bg-slate-900/40 px-3 py-2 text-sm text-slate-200 transition hover:bg-slate-900"
            >
              Clear Filters
            </button>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[980px] text-sm">
            <thead className="text-left text-slate-400">
              <tr className="border-b border-slate-800">
                <th className="py-3 pr-4 font-medium">Time</th>
                <th className="py-3 pr-4 font-medium">Type</th>
                <th className="py-3 pr-4 font-medium">Status</th>
                <th className="py-3 pr-4 font-medium">Last Action</th>
                <th className="py-3 pr-4 font-medium">Path</th>
                <th className="py-3 font-medium">Action</th>
              </tr>
            </thead>

            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={6} className="py-10 text-center text-slate-500">
                    loading minifilter events...
                  </td>
                </tr>
              ) : filteredRows.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-10 text-center text-slate-500">
                    no minifilter events match your search
                  </td>
                </tr>
              ) : (
                filteredRows.map((row) => (
                  <tr key={row.id} className="border-b border-slate-800/60 last:border-b-0 hover:bg-slate-800/20">
                    <td className="py-3 pr-4 text-slate-300">{row.time}</td>
                    <td className="py-3 pr-4">
                      <TypeBadge type={row.type} />
                    </td>
                    <td className="py-3 pr-4">
                      <StatusBadge status={row.status} />
                    </td>
                    <td className="py-3 pr-4 text-slate-300">{row.lastAction}</td>
                    <td className="max-w-[420px] py-3 pr-4 text-slate-500">
                      <span className="block truncate">{row.path}</span>
                    </td>
                    <td className="py-3">
                      <ActionButton action={row.action} />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="mt-4 flex items-center justify-between border-t border-slate-800 pt-4 text-xs text-slate-500">
          <span>Showing {filteredRows.length} results</span>
          <span>Page 1 of 1</span>
        </div>
      </Panel>
    </div>
  )
}