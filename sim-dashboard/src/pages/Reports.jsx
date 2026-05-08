import { useEffect, useMemo, useState, useRef, useCallback } from "react"
import Panel from "../components/Panel"

// ─── Constants ───────────────────────────────────────────────────────────────

const SEV_COLORS = {
  critical: "#E24B4A",
  high:     "#EF9F27",
  medium:   "#BA7517",
  low:      "#888780",
}

const SEV_BG = {
  critical: "rgba(226,75,74,0.12)",
  high:     "rgba(239,159,39,0.12)",
  medium:   "rgba(186,117,23,0.12)",
  low:      "rgba(136,135,128,0.1)",
}

const SEV_BORDER = {
  critical: "rgba(226,75,74,0.35)",
  high:     "rgba(239,159,39,0.35)",
  medium:   "rgba(186,117,23,0.35)",
  low:      "rgba(136,135,128,0.3)",
}

const SEV_ORDER  = ["critical", "high", "medium", "low"]
const SOURCE_LABELS = ["response-agent", "canary", "etw", "minifilter"]
const SOURCE_COLORS = ["#378ADD", "#534AB7", "#0F6E56", "#D85A30"]

// ─── Helpers ─────────────────────────────────────────────────────────────────

function relTime(ts) {
  if (!ts) return "—"
  const d = new Date(ts)
  if (isNaN(d)) return ts
  const diff = Date.now() - d
  const s = Math.floor(diff / 1000)
  const m = Math.floor(diff / 60000)
  const h = Math.floor(diff / 3600000)
  const day = Math.floor(diff / 86400000)
  if (s < 60)  return `${s}s ago`
  if (m < 60)  return `${m}m ago`
  if (h < 24)  return `${h}h ago`
  return `${day}d ago`
}

function fullDate(ts) {
  if (!ts) return "—"
  const d = new Date(ts)
  if (isNaN(d)) return ts
  return d.toLocaleString([], { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" })
}

// ─── Sub-components ───────────────────────────────────────────────────────────

function SeverityBadge({ severity }) {
  const s = String(severity || "").toLowerCase()
  return (
    <span
      className="inline-flex items-center gap-1 rounded px-2 py-0.5 text-[11px] font-medium capitalize border"
      style={{ background: SEV_BG[s], borderColor: SEV_BORDER[s], color: SEV_COLORS[s] }}
    >
      <span className="h-1.5 w-1.5 rounded-full" style={{ background: SEV_COLORS[s] }} />
      {s || "unknown"}
    </span>
  )
}

function MetricCard({ label, value, sub, iconBg, icon }) {
  return (
    <div className="relative overflow-hidden rounded-xl bg-slate-800/40 p-4">
      <div className="text-xs text-slate-400 mb-1">{label}</div>
      <div className="text-3xl font-semibold text-slate-100 tabular-nums">{value}</div>
      <div className="text-xs text-slate-500 mt-1">{sub}</div>
      {icon && (
        <div
          className="absolute right-3 top-1/2 -translate-y-1/2 h-8 w-8 rounded-lg flex items-center justify-center"
          style={{ background: iconBg }}
        >
          {icon}
        </div>
      )}
    </div>
  )
}

function PctBar({ label, value, max, color }) {
  const pct = max > 0 ? (value / max) * 100 : 0
  return (
    <div className="flex items-center gap-2 mb-2">
      <div className="w-16 flex-shrink-0 text-xs capitalize text-slate-400">{label}</div>
      <div className="flex-1 h-1.5 rounded-full bg-slate-800 overflow-hidden">
        <div
          className="h-full rounded-full transition-all duration-700"
          style={{ width: `${pct}%`, background: color }}
        />
      </div>
      <div className="w-6 text-right text-xs text-slate-500 tabular-nums">{value}</div>
    </div>
  )
}

function MetricRow({ label, value, color }) {
  return (
    <div className="flex items-center justify-between px-3 py-2 bg-slate-800/40 rounded-lg mb-1.5">
      <span className="text-sm text-slate-400">{label}</span>
      <span
        className="text-xs font-medium px-2 py-0.5 rounded border"
        style={color ? { color, borderColor: `${color}30`, background: `${color}10` } : {}}
      >
        {value}
      </span>
    </div>
  )
}

function SectionHeader({ title, sub, right }) {
  return (
    <div className="flex items-end justify-between border-b border-slate-800/60 pb-3 mb-4">
      <div>
        <h2 className="text-sm font-semibold text-slate-100">{title}</h2>
        {sub && <p className="text-xs text-slate-500 mt-0.5">{sub}</p>}
      </div>
      {right && <div>{right}</div>}
    </div>
  )
}

function Skeleton({ className = "" }) {
  return <div className={`animate-pulse rounded-lg bg-slate-800/60 ${className}`} />
}

// ─── Donut Chart (pure SVG) ───────────────────────────────────────────────────

function DonutChart({ segments = [], size = 120 }) {
  const total = segments.reduce((s, seg) => s + seg.value, 0) || 1
  const r = 40, cx = 50, cy = 50
  const circ = 2 * Math.PI * r
  let offset = 0

  const arcs = segments.map((seg) => {
    const dash = (seg.value / total) * circ
    const arc  = { ...seg, dash, offset }
    offset += dash
    return arc
  })

  return (
    <div className="relative flex items-center justify-center flex-shrink-0" style={{ width: size, height: size }}>
      <svg viewBox="0 0 100 100" width={size} height={size} className="rotate-[-90deg]">
        <circle cx={cx} cy={cy} r={r} fill="none" stroke="rgba(30,41,59,0.6)" strokeWidth="12" />
        {arcs.map((arc, i) => (
          <circle
            key={i}
            cx={cx} cy={cy} r={r}
            fill="none"
            stroke={arc.color}
            strokeWidth="12"
            strokeDasharray={`${arc.dash} ${circ - arc.dash}`}
            strokeDashoffset={-arc.offset}
            className="transition-all duration-700"
          />
        ))}
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center text-center pointer-events-none">
        <div className="text-xl font-semibold text-slate-100 tabular-nums">{total}</div>
        <div className="text-[10px] text-slate-500">total</div>
      </div>
    </div>
  )
}

// ─── Sparkline ────────────────────────────────────────────────────────────────

function Sparkline({ values = [], color = "#378ADD" }) {
  if (values.length < 2) return null
  const max = Math.max(...values, 1)
  const W = 300, H = 60
  const pts = values.map((v, i) => [
    (i / (values.length - 1)) * W,
    H - (v / max) * H * 0.85,
  ])
  const lineD = "M" + pts.map(p => p.join(",")).join("L")
  const areaD = lineD + `L${W},${H} L0,${H}Z`

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="w-full" style={{ height: 60 }}>
      <defs>
        <linearGradient id="spark-grad" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity="0.3" />
          <stop offset="100%" stopColor={color} stopOpacity="0" />
        </linearGradient>
      </defs>
      <path d={areaD} fill="url(#spark-grad)" />
      <path d={lineD} fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

// ─── Bar Chart (pure SVG, interactive tooltips) ───────────────────────────────

function BarChart({ values = [], labels = [], color = "#378ADD", height = 160 }) {
  const [hovered, setHovered] = useState(null)
  const max      = Math.max(...values, 1)
  const n        = values.length || 1
  const barW     = 100 / (n * 2 - 1)
  const gap      = barW

  return (
    <div className="mt-2 w-full select-none relative">
      <svg
        viewBox={`0 0 100 ${height + 28}`}
        className="w-full overflow-visible"
        style={{ height: height + 28 }}
        onMouseLeave={() => setHovered(null)}
      >
        {[0, 0.25, 0.5, 0.75, 1].map((frac, i) => (
          <line
            key={i}
            x1="0" y1={height - frac * height}
            x2="100" y2={height - frac * height}
            stroke="rgba(51,65,85,0.4)"
            strokeWidth="0.4"
            strokeDasharray="2,2"
          />
        ))}

        {values.map((v, i) => {
          const x       = i * (barW + gap)
          const barH    = (v / max) * height
          const y       = height - barH
          const isHov   = hovered === i

          return (
            <g
              key={i}
              onMouseEnter={() => setHovered(i)}
              style={{ cursor: "default" }}
            >
              <defs>
                <linearGradient id={`bg-${i}`} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={color} stopOpacity={isHov ? 1 : 0.85} />
                  <stop offset="100%" stopColor={color} stopOpacity={isHov ? 0.6 : 0.3} />
                </linearGradient>
              </defs>
              <rect x={x} y={0} width={barW} height={height} fill="rgba(30,41,59,0.3)" rx="1.5" />
              <rect
                x={x} y={y} width={barW} height={barH}
                fill={`url(#bg-${i})`}
                rx="1.5"
                className="transition-all duration-300"
              />
              {isHov && (
                <rect x={x - 0.5} y={y - 0.5} width={barW + 1} height={barH + 1} fill="none" stroke={color} strokeWidth="0.6" rx="1.5" />
              )}
              <text x={x + barW / 2} y={y - 2} textAnchor="middle" fontSize="3" fill="rgba(148,163,184,0.8)">{v}</text>
              <text x={x + barW / 2} y={height + 8} textAnchor="middle" fontSize="3.2" fill="rgba(100,116,139,0.9)">{labels[i] || ""}</text>
            </g>
          )
        })}
      </svg>
    </div>
  )
}

// ─── Timeline Entry ───────────────────────────────────────────────────────────

function TimelineEntry({ alert, isLast, onSelect, selected }) {
  return (
    <div className="relative flex gap-2.5">
      {!isLast && (
        <div
          className="absolute left-[6px] top-5 w-px h-full"
          style={{ background: "rgba(51,65,85,0.5)" }}
        />
      )}
      <div
        className="relative mt-1 h-3 w-3 flex-shrink-0 rounded-full border-2"
        style={{ borderColor: SEV_COLORS[alert.severity], background: SEV_BG[alert.severity] }}
      />
      <div
        className={`mb-3 flex-1 overflow-hidden rounded-xl border px-3 py-2.5 transition-all cursor-pointer
          ${selected ? "border-sky-500/50 bg-sky-500/5" : "border-slate-800/70 bg-slate-900/30 hover:bg-slate-900/50"}`}
        onClick={() => onSelect(alert)}
      >
        <div className="flex flex-wrap items-start justify-between gap-1.5">
          <div className="flex flex-wrap items-center gap-2">
            <SeverityBadge severity={alert.severity} />
            <span className="text-xs text-slate-500">{alert.source}</span>
          </div>
          <span className="text-xs text-slate-600">{relTime(alert.time)}</span>
        </div>
        <p className="mt-1.5 text-sm font-medium leading-snug text-slate-100">{alert.title}</p>
        <div className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-xs text-slate-500">
          <span>🖥 {alert.hostname}</span>
          {alert.responseTaken && <span>⚡ {alert.responseTaken}</span>}
          <span>🕐 {fullDate(alert.time)}</span>
        </div>
      </div>
    </div>
  )
}

// ─── Alert Detail Panel ───────────────────────────────────────────────────────

function AlertDetail({ alert, onClose }) {
  if (!alert) return (
    <div className="flex h-40 items-center justify-center text-sm text-slate-600">
      Select an alert to view details
    </div>
  )

  return (
    <div className="space-y-3">
      <div className="flex items-start justify-between gap-2">
        <SeverityBadge severity={alert.severity} />
        <button
          onClick={onClose}
          className="text-xs text-slate-500 hover:text-slate-300 transition-colors"
        >
          ✕ close
        </button>
      </div>
      <p className="text-sm font-medium text-slate-100 leading-snug">{alert.title}</p>
      {[
        ["Source",   alert.source],
        ["Host",     alert.hostname],
        ["Response", alert.responseTaken || "—"],
        ["Time",     fullDate(alert.time)],
        ["ID",       alert.id],
      ].map(([k, v]) => (
        <div key={k} className="flex items-center justify-between px-3 py-2 bg-slate-800/40 rounded-lg">
          <span className="text-xs text-slate-400">{k}</span>
          <span className="text-xs font-medium text-slate-200">{v}</span>
        </div>
      ))}
    </div>
  )
}

// ─── Main Component ───────────────────────────────────────────────────────────

const RANGE_OPTIONS = [
  { label: "7 days",  value: 7  },
  { label: "14 days", value: 14 },
  { label: "30 days", value: 30 },
]

const DEFAULT_FILTERS = {
  from:     "",
  to:       "",
  severity: "all",
  hostname: "",
  source:   "all",
}

export default function Reports() {
  const [data,         setData]         = useState(null)
  const [loading,      setLoading]      = useState(true)
  const [error,        setError]        = useState("")
  const [lastUpdated,  setLastUpdated]  = useState(null)
  const [exportMsg,    setExportMsg]    = useState("")
  const [filters,      setFilters]      = useState(DEFAULT_FILTERS)
  const [range,        setRange]        = useState(7)
  const [timelineFilter, setTimelineFilter] = useState("all")
  const [selectedAlert,  setSelectedAlert]  = useState(null)

  // ── Fetch ──────────────────────────────────────────────────────────────────

  const loadReports = useCallback(async (customFilters = filters) => {
    try {
      setLoading(true)
      setError("")
      const params = new URLSearchParams()
      if (customFilters.from)                   params.append("from",     customFilters.from)
      if (customFilters.to)                     params.append("to",       customFilters.to)
      if (customFilters.severity !== "all")     params.append("severity", customFilters.severity)
      if (customFilters.hostname.trim())        params.append("hostname", customFilters.hostname.trim())
      if (customFilters.source !== "all")       params.append("source",   customFilters.source)
      const q   = params.toString()
      const res = await fetch(`/api/reports${q ? `?${q}` : ""}`)
      if (!res.ok) throw new Error(`API error: ${res.status}`)
      setData(await res.json())
      setLastUpdated(new Date())
    } catch (e) {
      setError(e.message || "Failed to load reports")
    } finally {
      setLoading(false)
    }
  }, [filters])

  useEffect(() => { loadReports() }, [])

  // ── Derived data ───────────────────────────────────────────────────────────

  const report = useMemo(() => ({
    totalAlerts:       data?.totalAlerts       ?? 0,
    criticalAlerts:    data?.criticalAlerts    ?? 0,
    highRiskAlerts:    data?.highRiskAlerts    ?? 0,
    affectedHosts:     data?.affectedHosts     ?? 0,
    topRiskHost:       data?.topRiskHost       ?? "—",
    topRule:           data?.topRule           ?? "—",
    topSource:         data?.topSource         ?? "—",
    responseSummary:   data?.responseSummary   ?? {},
    sourceBreakdown:   data?.sourceBreakdown   ?? {},
    severityBreakdown: data?.severityBreakdown ?? {},
    timelineLabels:    data?.timelineLabels    ?? [],
    timelineCounts:    data?.timelineCounts    ?? [],
    recentAlerts:      data?.recentAlerts      ?? [],
  }), [data])

  const sevValues  = SEV_ORDER.map(k => report.severityBreakdown[k] ?? 0)
  const sevTotal   = sevValues.reduce((a, b) => a + b, 0) || 1

  const srcValues  = SOURCE_LABELS.map(k => report.sourceBreakdown[k] ?? 0)
  const srcTotal   = srcValues.reduce((a, b) => a + b, 0) || 1

  const donutSegs  = SEV_ORDER.map((k, i) => ({ label: k, value: sevValues[i], color: SEV_COLORS[k] }))

  const filteredAlerts = timelineFilter === "all"
    ? report.recentAlerts
    : report.recentAlerts.filter(a => a.severity === timelineFilter)

  const trendPeak  = Math.max(...report.timelineCounts, 0)
  const trendAvg   = report.timelineCounts.length
    ? Math.round(report.timelineCounts.reduce((a, b) => a + b, 0) / report.timelineCounts.length)
    : 0
  const trendTotal = report.timelineCounts.reduce((a, b) => a + b, 0)

  // ── Actions ────────────────────────────────────────────────────────────────

  function handleExport() {
    const blob = new Blob(
      [JSON.stringify({ generated: new Date().toISOString(), filters, ...report }, null, 2)],
      { type: "application/json" }
    )
    const a = document.createElement("a")
    a.href = URL.createObjectURL(blob)
    a.download = `security-report-${new Date().toISOString().slice(0, 10)}.json`
    a.click()
    URL.revokeObjectURL(a.href)
    setExportMsg("Exported ✓")
    setTimeout(() => setExportMsg(""), 2500)
  }

  function clearFilters() {
    setFilters(DEFAULT_FILTERS)
    loadReports(DEFAULT_FILTERS)
  }

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="mx-auto max-w-7xl space-y-8 pb-16">

      {/* Page Header */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight text-slate-100">Security Analysis Report</h1>
            {!loading && (
              <span className="flex items-center gap-1 rounded-full border border-sky-500/30 bg-sky-500/10 px-2 py-0.5 text-[11px] font-medium text-sky-400">
                Historical
              </span>
            )}
          </div>
          <p className="mt-1 text-sm text-slate-500">
            Historical analytical summary generated from MongoDB security events
            {lastUpdated && (
              <span className="ml-2 text-slate-600">· generated {relTime(lastUpdated)}</span>
            )}
          </p>
        </div>

        <div className="flex items-center gap-2">
          {exportMsg && (
            <span className="text-xs text-emerald-400 bg-emerald-500/10 border border-emerald-500/20 px-2 py-1 rounded-lg">
              {exportMsg}
            </span>
          )}
          <button
            onClick={handleExport}
            className="flex items-center gap-2 rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-all hover:border-slate-600 hover:bg-slate-800 active:scale-95"
          >
            ⬇ Export JSON
          </button>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div className="flex items-center gap-3 rounded-xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-300">
          <span>⚠</span>
          <span>{error}</span>
          <button
            onClick={() => loadReports()}
            className="ml-auto text-xs underline hover:no-underline"
          >
            Retry
          </button>
        </div>
      )}

      {/* Filters */}
      <Panel title="Report Filters" right="MongoDB historical query">
        <div className="grid grid-cols-1 gap-4 md:grid-cols-5">
          {[
            { label: "From",     id: "from",     type: "date" },
            { label: "To",       id: "to",       type: "date" },
          ].map(({ label, id, type }) => (
            <div key={id}>
              <label className="mb-1 block text-xs text-slate-500">{label}</label>
              <input
                type={type}
                value={filters[id]}
                onChange={e => setFilters(f => ({ ...f, [id]: e.target.value }))}
                className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-200 outline-none focus:border-sky-500"
              />
            </div>
          ))}

          <div>
            <label className="mb-1 block text-xs text-slate-500">Severity</label>
            <select
              value={filters.severity}
              onChange={e => setFilters(f => ({ ...f, severity: e.target.value }))}
              className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-200 outline-none focus:border-sky-500"
            >
              <option value="all">All</option>
              {SEV_ORDER.map(s => (
                <option key={s} value={s}>{s.charAt(0).toUpperCase() + s.slice(1)}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-xs text-slate-500">Source</label>
            <select
              value={filters.source}
              onChange={e => setFilters(f => ({ ...f, source: e.target.value }))}
              className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-200 outline-none focus:border-sky-500"
            >
              <option value="all">All</option>
              {SOURCE_LABELS.map(s => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-xs text-slate-500">Hostname</label>
            <input
              type="text"
              placeholder="Laptop-1"
              value={filters.hostname}
              onChange={e => setFilters(f => ({ ...f, hostname: e.target.value }))}
              className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-200 outline-none focus:border-sky-500"
            />
          </div>
        </div>

        <div className="mt-4 flex flex-wrap gap-3">
          <button
            onClick={() => loadReports()}
            className="rounded-xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-sky-400 active:scale-95"
          >
            Generate Report
          </button>
          <button
            onClick={clearFilters}
            className="rounded-xl border border-slate-700 bg-slate-900 px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-slate-800"
          >
            Clear Filters
          </button>
        </div>
      </Panel>

      {/* KPI Cards */}
      <section>
        <SectionHeader title="Overview Metrics" sub="Aggregate counts from selected historical records" />
        <div className="grid grid-cols-2 gap-4 xl:grid-cols-4">
          {loading ? (
            Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-28" />)
          ) : (
            <>
              <MetricCard label="Total Alerts"      value={report.totalAlerts}    sub="Matching report records"     iconBg="rgba(226,75,74,0.12)"   icon={<span>🚨</span>} />
              <MetricCard label="Critical Alerts"   value={report.criticalAlerts} sub="Highest severity tier"       iconBg="rgba(226,75,74,0.12)"   icon={<span>🔴</span>} />
              <MetricCard label="High Risk Alerts"  value={report.highRiskAlerts} sub="High + critical combined"    iconBg="rgba(239,159,39,0.12)"  icon={<span>⚡</span>} />
              <MetricCard label="Affected Hosts"    value={report.affectedHosts}  sub="Unique impacted endpoints"   iconBg="rgba(55,138,221,0.12)"  icon={<span>🖥</span>} />
            </>
          )}
        </div>
      </section>

      {/* Alert Trend */}
      <section>
        <SectionHeader
          title="Alert Volume Trend"
          sub="Historical alert volume based on the selected filters"
          right={
            <div className="flex gap-1">
              {RANGE_OPTIONS.map(opt => (
                <button
                  key={opt.value}
                  onClick={() => setRange(opt.value)}
                  className={`px-3 py-1 text-xs rounded-lg border transition-colors ${
                    range === opt.value
                      ? "bg-slate-700 border-slate-600 text-slate-100"
                      : "border-slate-800 text-slate-500 hover:border-slate-700 hover:text-slate-300"
                  }`}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          }
        />

        <Panel
          title="Daily Alert Count"
          right={
            <div className="flex items-center gap-1.5 text-xs text-sky-400">
              <span className="h-2 w-2 rounded-sm bg-sky-400/70" />
              Historical
            </div>
          }
        >
          {loading ? (
            <Skeleton className="mt-3 h-44" />
          ) : (
            <div className="px-2">
              <BarChart
                values={report.timelineCounts}
                labels={report.timelineLabels}
                color="#378ADD"
                height={160}
              />
              <div className="mt-3 flex justify-between text-xs text-slate-600">
                <span>Peak: {trendPeak} alerts</span>
                <span>Avg: {trendAvg} alerts/day</span>
                <span>Total: {trendTotal} alerts</span>
              </div>
            </div>
          )}
        </Panel>
      </section>

      {/* Distribution Analysis */}
      <section>
        <SectionHeader title="Distribution Analysis" sub="Breakdown of alerts by severity and detection source" />

        <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
          <Panel title="Severity Breakdown" right="Filtered data">
            {loading ? (
              <Skeleton className="h-48" />
            ) : (
              <div className="flex items-center gap-6">
                <DonutChart segments={donutSegs} size={130} />
                <div className="flex-1">
                  {SEV_ORDER.map((sev, i) => (
                    <div key={sev}>
                      <PctBar
                        label={sev}
                        value={sevValues[i]}
                        max={sevTotal}
                        color={SEV_COLORS[sev]}
                      />
                      <div className="mb-1 pl-20 text-[10px] text-slate-600">
                        {((sevValues[i] / sevTotal) * 100).toFixed(1)}% of total
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </Panel>

          <Panel title="Detection Source Breakdown" right="By origin">
            {loading ? (
              <Skeleton className="h-48" />
            ) : (
              <div className="flex flex-col gap-4">
                <BarChart
                  values={srcValues}
                  labels={SOURCE_LABELS}
                  color="#534AB7"
                  height={110}
                />
                <div className="space-y-2 border-t border-slate-800/60 pt-3">
                  {SOURCE_LABELS.map((src, i) => (
                    <div key={src} className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-2 capitalize text-slate-300">
                        <span className="h-2 w-2 rounded-sm" style={{ background: SOURCE_COLORS[i] }} />
                        {src}
                      </div>
                      <span className="text-slate-500">
                        {srcValues[i]} · {((srcValues[i] / srcTotal) * 100).toFixed(1)}%
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </Panel>
        </div>
      </section>

      {/* Intelligence Summary */}
      <section>
        <SectionHeader title="Threat Intelligence Summary" sub="Top identifiers and automated response statistics" />

        <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
          <Panel title="Key Identifiers">
            {loading ? (
              <div className="space-y-2">{[1,2,3].map(i => <Skeleton key={i} className="h-10" />)}</div>
            ) : (
              <div className="space-y-1.5">
                <MetricRow label="Top Risk Host"         value={report.topRiskHost}   color="#E24B4A" />
                <MetricRow label="Top Triggered Rule"    value={report.topRule}        color="#EF9F27" />
                <MetricRow label="Dominant Source"       value={report.topSource}      color="#534AB7" />
                <MetricRow
                  label="Alert Concentration"
                  value={`${((report.criticalAlerts / (report.totalAlerts || 1)) * 100).toFixed(1)}% critical`}
                  color="#BA7517"
                />
                <MetricRow
                  label="Host Coverage"
                  value={`${report.affectedHosts} endpoint${report.affectedHosts !== 1 ? "s" : ""} impacted`}
                />
              </div>
            )}
          </Panel>

          <Panel title="Response Actions Taken">
            {loading ? (
              <div className="space-y-2">{[1,2,3].map(i => <Skeleton key={i} className="h-10" />)}</div>
            ) : Object.entries(report.responseSummary).length === 0 ? (
              <div className="flex h-32 items-center justify-center text-sm text-slate-600">
                No response actions recorded
              </div>
            ) : (
              <div className="space-y-1.5">
                {Object.entries(report.responseSummary).map(([key, value]) => (
                  <MetricRow
                    key={key}
                    label={key.replaceAll("_", " ")}
                    value={String(value)}
                    color="#378ADD"
                  />
                ))}
                <div className="mt-3 rounded-xl border border-emerald-500/20 bg-emerald-500/5 px-3 py-2 text-xs text-emerald-400">
                  ✓ {Object.keys(report.responseSummary).length} action type
                  {Object.keys(report.responseSummary).length !== 1 ? "s" : ""} found
                </div>
              </div>
            )}
          </Panel>
        </div>
      </section>

      {/* Alert Timeline */}
      <section>
        <SectionHeader
          title="Recent Alert Timeline"
          sub="Most recent matching alert entries from the historical database"
          right={
            <select
              value={timelineFilter}
              onChange={e => { setTimelineFilter(e.target.value); setSelectedAlert(null) }}
              className="rounded-lg border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-300 outline-none focus:border-sky-500"
            >
              <option value="all">All severities</option>
              {SEV_ORDER.map(s => (
                <option key={s} value={s}>{s.charAt(0).toUpperCase() + s.slice(1)} only</option>
              ))}
            </select>
          }
        />

        <div className="grid grid-cols-1 gap-4 xl:grid-cols-3">
          {/* Feed */}
          <div className="xl:col-span-2">
            <Panel title="Alert Feed" right={`Latest ${filteredAlerts.length}`}>
              {loading ? (
                <div className="space-y-3">
                  {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-24" />)}
                </div>
              ) : filteredAlerts.length === 0 ? (
                <div className="flex h-40 items-center justify-center text-sm text-slate-600">
                  No alerts match the selected filters
                </div>
              ) : (
                <div className="mt-2">
                  {filteredAlerts.map((alert, index) => (
                    <TimelineEntry
                      key={alert.id || index}
                      alert={alert}
                      isLast={index === filteredAlerts.length - 1}
                      selected={selectedAlert?.id === alert.id}
                      onSelect={a => setSelectedAlert(prev => prev?.id === a.id ? null : a)}
                    />
                  ))}
                </div>
              )}
            </Panel>
          </div>

          {/* Sidebar */}
          <div className="space-y-4">
            {/* Alert Detail */}
            <Panel title={selectedAlert ? "Alert Detail" : "Severity Snapshot"}>
              {loading ? (
                <Skeleton className="h-32" />
              ) : selectedAlert ? (
                <AlertDetail alert={selectedAlert} onClose={() => setSelectedAlert(null)} />
              ) : (
                <div className="space-y-3">
                  {SEV_ORDER.map((sev, i) => (
                    <div key={sev} className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="h-2.5 w-2.5 rounded-full" style={{ background: SEV_COLORS[sev] }} />
                        <span className="text-sm capitalize text-slate-300">{sev}</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <div className="h-1.5 w-16 overflow-hidden rounded-full bg-slate-800">
                          <div
                            className="h-full rounded-full transition-all duration-700"
                            style={{ width: `${(sevValues[i] / sevTotal) * 100}%`, background: SEV_COLORS[sev] }}
                          />
                        </div>
                        <span className="w-6 text-right text-sm tabular-nums text-slate-500">{sevValues[i]}</span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </Panel>

            {/* Sparkline */}
            <Panel title="Report Sparkline">
              {loading ? (
                <Skeleton className="h-16" />
              ) : (
                <div>
                  <Sparkline values={report.timelineCounts} color="#378ADD" />
                  <div className="mt-2 flex justify-between text-xs text-slate-600">
                    <span>{report.timelineLabels[0] || "–"}</span>
                    <span>{report.timelineLabels[report.timelineLabels.length - 1] || "–"}</span>
                  </div>
                  <div className="mt-3 grid grid-cols-2 gap-2">
                    <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-2 text-center">
                      <div className="text-lg font-semibold text-slate-100">{trendTotal}</div>
                      <div className="text-[10px] text-slate-500">total</div>
                    </div>
                    <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-2 text-center">
                      <div className="text-lg font-semibold text-slate-100">{trendAvg}</div>
                      <div className="text-[10px] text-slate-500">avg/day</div>
                    </div>
                  </div>
                </div>
              )}
            </Panel>

            {/* Source Activity */}
            <Panel title="Source Activity">
              {loading ? (
                <Skeleton className="h-24" />
              ) : (
                <div className="space-y-2.5">
                  {SOURCE_LABELS.map((src, i) => (
                    <div key={src} className="flex items-center gap-2.5">
                      <span className="h-2 w-2 flex-shrink-0 rounded-full" style={{ background: SOURCE_COLORS[i] }} />
                      <span className="flex-1 text-xs capitalize text-slate-400">{src}</span>
                      <div className="h-1 w-16 overflow-hidden rounded-full bg-slate-800">
                        <div
                          className="h-full rounded-full"
                          style={{ width: `${(srcValues[i] / srcTotal) * 100}%`, background: SOURCE_COLORS[i] }}
                        />
                      </div>
                      <span className="w-8 text-right text-xs tabular-nums text-slate-500">{srcValues[i]}</span>
                    </div>
                  ))}
                </div>
              )}
            </Panel>
          </div>
        </div>
      </section>

      {/* Footer */}
      <div className="flex items-center justify-between border-t border-slate-800/60 pt-4 text-xs text-slate-700">
        <span>SIM Security Dashboard · Historical Reports</span>
        {lastUpdated && <span>Last generated: {lastUpdated.toLocaleTimeString()}</span>}
      </div>

    </div>
  )
}
