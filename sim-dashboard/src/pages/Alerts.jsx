import { useEffect, useMemo, useState } from "react"
import Panel from "../components/Panel"

function SeverityBadge({ severity }) {
  const s = String(severity || "").toLowerCase()

  const cls =
    s === "critical"
      ? "border-red-500/40 bg-red-500/10 text-red-300"
      : s === "high"
      ? "border-orange-500/40 bg-orange-500/10 text-orange-300"
      : s === "medium"
      ? "border-yellow-500/40 bg-yellow-500/10 text-yellow-300"
      : "border-slate-700 bg-slate-800/40 text-slate-300"

  return (
    <span className={`inline-flex rounded-full border px-2 py-0.5 text-xs capitalize ${cls}`}>
      {s}
    </span>
  )
}

function SourceBadge({ source }) {
  const s = String(source || "").toLowerCase()

  const cls =
    s === "canary"
      ? "border-cyan-500/35 bg-cyan-500/10 text-cyan-300"
      : s === "etw"
      ? "border-violet-500/35 bg-violet-500/10 text-violet-300"
      : s === "minifilter"
      ? "border-emerald-500/35 bg-emerald-500/10 text-emerald-300"
      : "border-slate-700 bg-slate-800/40 text-slate-300"

  return (
    <span className={`inline-flex rounded-full border px-2 py-0.5 text-xs uppercase ${cls}`}>
      {s}
    </span>
  )
}

function formatRelativeTimestamp(ts) {
  if (!ts) return "-"

  const date = new Date(ts)
  if (Number.isNaN(date.getTime())) return ts

  const now = new Date()
  const diffMs = now - date

  const seconds = Math.floor(diffMs / 1000)
  const minutes = Math.floor(diffMs / (1000 * 60))
  const hours = Math.floor(diffMs / (1000 * 60 * 60))
  const days = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (seconds < 60) return `${seconds} sec ago`
  if (minutes < 60) return `${minutes} min ago`
  if (hours < 24) return `${hours} hour${hours > 1 ? "s" : ""} ago`
  return `${days} day${days > 1 ? "s" : ""} ago`
}

function formatOriginalTimestamp(ts) {
  if (!ts) return "-"
  return ts
}

function DetailsModal({ item, onClose }) {
  if (!item) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="w-full max-w-3xl rounded-2xl border border-slate-800 bg-slate-950 shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-800 px-5 py-4">
          <div>
            <h2 className="text-lg font-semibold text-slate-100">Alert Details</h2>
            <p className="mt-1 text-sm text-slate-400">{item.rule_name}</p>
          </div>

          <button
            onClick={onClose}
            className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-slate-300 hover:bg-slate-900"
          >
            Close
          </button>
        </div>

        <div className="grid gap-6 p-5 md:grid-cols-2">
          <div className="space-y-3">
            <div>
              <div className="text-xs uppercase tracking-wide text-slate-500">Timestamp</div>
              <div className="mt-1 text-sm text-slate-200">{formatOriginalTimestamp(item.timestamp)}</div>
            </div>

            <div>
              <div className="text-xs uppercase tracking-wide text-slate-500">Severity</div>
              <div className="mt-1">
                <SeverityBadge severity={item.severity} />
              </div>
            </div>

            <div>
              <div className="text-xs uppercase tracking-wide text-slate-500">Alert Type</div>
              <div className="mt-1 text-sm text-slate-200">{item.rule_name}</div>
            </div>

            <div>
              <div className="text-xs uppercase tracking-wide text-slate-500">Host Name</div>
              <div className="mt-1 text-sm text-slate-200">{item.hostname}</div>
            </div>

            <div>
              <div className="text-xs uppercase tracking-wide text-slate-500">Source</div>
              <div className="mt-1">
                <SourceBadge source={item.source} />
              </div>
            </div>

            <div>
              <div className="text-xs uppercase tracking-wide text-slate-500">Response Taken</div>
              <div className="mt-1 text-sm text-slate-200">{item.response_taken || "-"}</div>
            </div>
          </div>

          <div>
            <div className="mb-3 text-xs uppercase tracking-wide text-slate-500">Technical Details</div>

            <div className="rounded-xl border border-slate-800 bg-slate-900/40 p-4">
              {item.data && Object.keys(item.data).length > 0 ? (
                <div className="space-y-3">
                  {Object.entries(item.data).map(([key, value]) => (
                    <div key={key}>
                      <div className="text-xs text-slate-500">
                        {key.replaceAll("_", " ")}
                      </div>
                      <div className="mt-1 break-all text-sm text-slate-200">
                        {String(value)}
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-sm text-slate-500">No additional details</div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default function Alerts() {
  const [items, setItems] = useState([])
  const [error, setError] = useState("")
  const [loading, setLoading] = useState(true)
  const [selectedAlert, setSelectedAlert] = useState(null)

  const [q, setQ] = useState("")
  const [sev, setSev] = useState("ALL")
  const [src, setSrc] = useState("ALL")

  useEffect(() => {
  let mounted = true

  async function loadAlerts() {
    try {
      setError("")
      setLoading(true)

      const res = await fetch("/api/alerts")
      if (!res.ok) throw new Error(`API error: ${res.status}`)

      const json = await res.json()
      if (mounted) {
        setItems(json.items || [])
      }
    } catch (e) {
      if (mounted) {
        setError(e.message || "failed to load alerts")
      }
    } finally {
      if (mounted) {
        setLoading(false)
      }
    }
  }

  loadAlerts()
  const interval = setInterval(loadAlerts, 3000)

  return () => {
    mounted = false
    clearInterval(interval)
  }
}, [])

  const filtered = useMemo(() => {
    const query = q.trim().toLowerCase()

    return items.filter((a) => {
      const text =
        `${a.timestamp} ${a.severity} ${a.rule_name} ${a.hostname} ${a.source} ${a.response_taken}`
          .toLowerCase()

      const okQ = query ? text.includes(query) : true
      const okSev = sev === "ALL" ? true : String(a.severity || "").toLowerCase() === sev.toLowerCase()
      const okSrc = src === "ALL" ? true : String(a.source || "").toLowerCase() === src.toLowerCase()

      return okQ && okSev && okSrc
    })
  }, [items, q, sev, src])

  const counts = useMemo(() => {
    return {
      total: items.length,
      highOrCritical: items.filter((x) => ["high", "critical"].includes(String(x.severity || "").toLowerCase())).length,
      etw: items.filter((x) => String(x.source || "").toLowerCase() === "etw").length,
    }
  }, [items])

  return (
    <div className="mx-auto max-w-7xl">
      <div className="mb-4 flex items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold text-slate-100">Alerts</h1>
          <p className="mt-1 text-sm text-slate-400">
            view correlated alerts from Canary, ETW, and Minifilter sources
          </p>
        </div>

        <div className="grid grid-cols-3 gap-3">
          <div className="rounded-2xl border border-slate-800/80 bg-slate-900/35 p-3">
            <div className="text-xs text-slate-400">total</div>
            <div className="mt-1 text-2xl font-semibold text-slate-100">{counts.total}</div>
          </div>
          <div className="rounded-2xl border border-red-500/25 bg-red-500/5 p-3">
            <div className="text-xs text-slate-400">high/critical</div>
            <div className="mt-1 text-2xl font-semibold text-red-200">{counts.highOrCritical}</div>
          </div>
          <div className="rounded-2xl border border-violet-500/25 bg-violet-500/5 p-3">
            <div className="text-xs text-slate-400">etw alerts</div>
            <div className="mt-1 text-2xl font-semibold text-violet-200">{counts.etw}</div>
          </div>
        </div>
      </div>

      <Panel title="alerts list" right={loading ? "loading..." : `${filtered.length} shown`}>
        <div className="mb-4 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="search by alert type, host, source, response..."
            className="w-full rounded-xl border border-slate-800 bg-slate-950/40 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500 focus:outline-none"
          />

          <div className="flex gap-3">
            <select
              value={sev}
              onChange={(e) => setSev(e.target.value)}
              className="rounded-xl border border-slate-800 bg-slate-950/40 px-3 py-2 text-sm text-slate-100 focus:outline-none"
            >
              <option value="ALL">severity: all</option>
              <option value="critical">critical</option>
              <option value="high">high</option>
              <option value="medium">medium</option>
              <option value="low">low</option>
            </select>

            <select
              value={src}
              onChange={(e) => setSrc(e.target.value)}
              className="rounded-xl border border-slate-800 bg-slate-950/40 px-3 py-2 text-sm text-slate-100 focus:outline-none"
            >
              <option value="ALL">source: all</option>
              <option value="canary">canary</option>
              <option value="etw">etw</option>
              <option value="minifilter">minifilter</option>
            </select>
          </div>
        </div>

        {error ? (
          <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-200">
            {error}
          </div>
        ) : null}

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="text-left text-slate-400">
              <tr className="border-b border-slate-800">
                <th className="py-2 pr-3">Timestamp</th>
                <th className="py-2 pr-3">Severity</th>
                <th className="py-2 pr-3">Alert Type</th>
                <th className="py-2 pr-3">Host Name</th>
                <th className="py-2 pr-3">Source</th>
                <th className="py-2">Details</th>
              </tr>
            </thead>

            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-slate-500">
                    loading alerts...
                  </td>
                </tr>
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-slate-500">
                    no alerts available
                  </td>
                </tr>
              ) : (
                filtered.map((a, index) => (
                  <tr key={a.id || `${a.timestamp}-${index}`} className="border-b border-slate-800/60 last:border-b-0">
                    <td className="py-2 pr-3 text-slate-300">{formatRelativeTimestamp(a.timestamp)}</td>
                    <td className="py-2 pr-3">
                      <SeverityBadge severity={a.severity} />
                    </td>
                    <td className="py-2 pr-3 text-slate-100">{a.rule_name}</td>
                    <td className="py-2 pr-3 text-slate-300">{a.hostname}</td>
                    <td className="py-2 pr-3">
                      <SourceBadge source={a.source} />
                    </td>
                    <td className="py-2">
                      <button
                        onClick={() => setSelectedAlert(a)}
                        className="rounded-lg border border-slate-700 bg-slate-900/50 px-3 py-1.5 text-xs text-slate-200 hover:bg-slate-900"
                      >
                        View Details
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Panel>

      <DetailsModal item={selectedAlert} onClose={() => setSelectedAlert(null)} />
    </div>
  )
}