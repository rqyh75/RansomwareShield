import { useEffect, useMemo, useState } from "react"
import Panel from "../components/Panel"

//converts a timestamp into a user-friendly time label
function formatRelativeTime(ts) {
  if (!ts) return "-"
  const d = new Date(ts)
  if (Number.isNaN(d.getTime())) return ts

  const diff = Date.now() - d.getTime()
  const sec = Math.floor(diff / 1000)
  const min = Math.floor(diff / (1000 * 60))
  const hr = Math.floor(diff / (1000 * 60 * 60))

  if (sec < 60) return `${sec} sec ago`
  if (min < 60) return `${min} min ago`
  if (hr < 24) return `${hr} hr${hr > 1 ? "s" : ""} ago`
  return d.toLocaleString()
}

//date into a simple clock label
function formatClockLabel(date) {
  return date.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  })
}

//displays the alert severity as a colored badge
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
    <span className={`inline-flex rounded-md border px-2 py-0.5 text-[11px] capitalize ${cls}`}>
      {s}
    </span>
  )
}

//reusable card for showing dashboard numbers
function SummaryCard({ title, value, subtitle, icon, tone = "default" }) {
  const toneClass =
    tone === "red"
      ? "border-red-500/35 bg-[linear-gradient(180deg,rgba(127,29,29,0.18),rgba(15,23,42,0.35))]"
      : tone === "orange"
      ? "border-orange-500/35 bg-[linear-gradient(180deg,rgba(124,45,18,0.18),rgba(15,23,42,0.35))]"
      : tone === "blue"
      ? "border-sky-500/35 bg-[linear-gradient(180deg,rgba(30,64,175,0.18),rgba(15,23,42,0.35))]"
      : tone === "green"
      ? "border-emerald-500/35 bg-[linear-gradient(180deg,rgba(6,78,59,0.18),rgba(15,23,42,0.35))]"
      : "border-slate-800 bg-slate-900/40"

  return (
    <div className={`rounded-2xl border p-4 ${toneClass}`}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="text-4xl font-semibold leading-none text-slate-100">{value}</div>
          <div className="mt-3 text-base font-medium text-slate-100">{title}</div>
          <div className="mt-1 text-sm text-slate-400">{subtitle}</div>
        </div>
        {icon ? <div className="pt-1 text-2xl text-slate-300">{icon}</div> : null}
      </div>
    </div>
  )
}

//draws a custom SVG line chart for alert activity
function LineChartRecent({ buckets }) {
  const width = 560
  const height = 210
  const paddingLeft = 46
  const paddingRight = 22
  const paddingTop = 20
  const paddingBottom = 38

  const maxValue = Math.max(...buckets.map((b) => b.count), 1)
  const yMax = Math.max(5, Math.ceil(maxValue / 5) * 5)
  const yTicks = [
    0,
    Math.round(yMax / 4),
    Math.round(yMax / 2),
    Math.round((yMax * 3) / 4),
    yMax,
  ]

  const xForIndex = (index) =>
    paddingLeft +
    (index * (width - paddingLeft - paddingRight)) / Math.max(buckets.length - 1, 1)

  const yForValue = (value) =>
    height -
    paddingBottom -
    (value / yMax) * (height - paddingTop - paddingBottom)

  const points = buckets
    .map((bucket, index) => `${xForIndex(index)},${yForValue(bucket.count)}`)
    .join(" ")

  return (
    <div className="rounded-xl bg-[#111935]/70 p-4">
      <svg viewBox={`0 0 ${width} ${height}`} className="h-56 w-full">
        <text
          x="14"
          y="112"
          transform="rotate(-90 14 112)"
          className="fill-slate-500 text-[11px]"
        >
          Alerts
        </text>

        {yTicks.map((tick) => {
          const y = yForValue(tick)

          return (
            <g key={tick}>
              <line
                x1={paddingLeft}
                x2={width - paddingRight}
                y1={y}
                y2={y}
                stroke="rgba(148,163,184,0.14)"
                strokeWidth="1"
              />
              <text
                x={paddingLeft - 12}
                y={y + 4}
                textAnchor="end"
                className="fill-slate-500 text-[11px]"
              >
                {tick}
              </text>
            </g>
          )
        })}

        <line
          x1={paddingLeft}
          x2={width - paddingRight}
          y1={height - paddingBottom}
          y2={height - paddingBottom}
          stroke="rgba(148,163,184,0.22)"
          strokeWidth="1"
        />

        <polyline
          fill="none"
          stroke="rgba(147,197,253,0.95)"
          strokeWidth="3"
          points={points}
        />

        {buckets.map((bucket, index) => (
          <circle
            key={index}
            cx={xForIndex(index)}
            cy={yForValue(bucket.count)}
            r="4"
            fill="white"
            stroke="rgba(147,197,253,0.95)"
            strokeWidth="2"
          />
        ))}

        {buckets.map((bucket, index) => (
          <text
            key={index}
            x={xForIndex(index)}
            y={height - 10}
            textAnchor="middle"
            className="fill-slate-500 text-[11px]"
          >
            {bucket.label}
          </text>
        ))}
      </svg>
    </div>
  )
}

//creates a vertical bar chart
function VerticalBars({ items }) {
  const max = Math.max(...items.map((i) => i.value), 1)

  return (
    <div>
      <div className="flex h-44 items-end gap-4 rounded-xl bg-[#111935]/70 p-4">
        {items.map((item) => (
          <div key={item.label} className="flex flex-1 flex-col items-center gap-3">
            <div className="text-xs text-slate-400">{item.value}</div>
            <div className="flex h-28 items-end">
              <div
                className={`w-12 rounded-t-md ${item.color}`}
                style={{
                  height: `${Math.max((item.value / max) * 100, item.value > 0 ? 10 : 0)}%`,
                }}
              />
            </div>
          </div>
        ))}
      </div>

      <div className="mt-3 grid grid-cols-3 gap-4 text-center text-xs text-slate-500">
        {items.map((item) => (
          <div key={item.label}>{item.label}</div>
        ))}
      </div>
    </div>
  )
}

//creates horizontal progress bars
function ActionBars({ items }) {
  const max = Math.max(...items.map((i) => i.value), 1)

  return (
    <div className="space-y-3">
      {items.length === 0 ? (
        <div className="py-8 text-center text-sm text-slate-500">No response actions</div>
      ) : (
        items.map((item) => (
          <div key={item.label}>
            <div className="mb-1 flex items-center justify-between text-sm">
              <span className="text-slate-300">{item.label}</span>
              <span className="text-slate-400">{item.value}</span>
            </div>
            <div className="h-3 overflow-hidden rounded-full bg-slate-800/70">
              <div
                className="h-full rounded-full bg-sky-400/80"
                style={{ width: `${(item.value / max) * 100}%` }}
              />
            </div>
          </div>
        ))
      )}
    </div>
  )
}

//creates a circular ring chart using SVG
function EndpointRing({ count }) {
  const radius = 56
  const stroke = 10
  const normalizedRadius = radius - stroke / 2
  const circumference = normalizedRadius * 2 * Math.PI
  const progress = 0.82
  const strokeDashoffset = circumference - progress * circumference

  return (
    <div className="flex flex-col items-center justify-center py-2">
      <svg height={140} width={140} className="-rotate-90">
        <circle
          stroke="rgba(148,163,184,0.18)"
          fill="transparent"
          strokeWidth={stroke}
          r={normalizedRadius}
          cx={70}
          cy={70}
        />
        <circle
          stroke="rgba(125,211,252,0.95)"
          fill="transparent"
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={`${circumference} ${circumference}`}
          style={{ strokeDashoffset }}
          r={normalizedRadius}
          cx={70}
          cy={70}
        />
      </svg>

      <div className="-mt-24 text-center">
        <div className="text-4xl font-semibold text-slate-100">{count}</div>
        <div className="text-sm text-slate-400">Online</div>
      </div>
    </div>
  )
}

export default function Dashboard() {
  const [alerts, setAlerts] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState("")

  useEffect(() => {
    let mounted = true

    async function load() {
      try {
        setError("")
        const res = await fetch("/api/alerts")
        if (!res.ok) throw new Error(`API error: ${res.status}`)
        const json = await res.json()

        if (mounted) {
          setAlerts(json.items || [])
          setLoading(false)
        }
      } catch (e) {
        if (mounted) {
          setError(e.message || "failed to load dashboard data")
          setLoading(false)
        }
      }
    }

    load()
    const savedSettings = JSON.parse(
      localStorage.getItem("sim_dashboard_settings") || "{}"
    )

    const refreshSeconds = savedSettings.dashboardRefreshSeconds || 3

    const interval = setInterval(load, refreshSeconds * 1000)

    return () => {
      mounted = false
      clearInterval(interval)
    }
  }, [])

  const alerts24h = useMemo(() => {
    const now = Date.now()
    return alerts.filter((alert) => {
      const ts = new Date(alert.timestamp).getTime()
      if (Number.isNaN(ts)) return false
      return now - ts <= 24 * 60 * 60 * 1000
    })
  }, [alerts])

  const criticalAlerts = alerts.filter(
    (a) => String(a.severity || "").toLowerCase() === "critical"
  ).length

  const uniqueHosts24h = Array.from(
    new Set(alerts24h.map((a) => a.hostname).filter(Boolean))
  )

  const topRiskHost = useMemo(() => {
    const score = {}
    alerts24h.forEach((a) => {
      const host = a.hostname || "Unknown"
      const sev = String(a.severity || "").toLowerCase()
      const weight =
        sev === "critical" ? 4 : sev === "high" ? 3 : sev === "medium" ? 2 : 1
      score[host] = (score[host] || 0) + weight
    })

    const sorted = Object.entries(score).sort((a, b) => b[1] - a[1])
    return sorted[0]?.[0] || "-"
  }, [alerts24h])

  const topRule = useMemo(() => {
    const count = {}
    alerts24h.forEach((a) => {
      const rule = a.rule_name || "Unknown rule"
      count[rule] = (count[rule] || 0) + 1
    })
    const sorted = Object.entries(count).sort((a, b) => b[1] - a[1])
    return sorted[0]?.[0] || "-"
  }, [alerts24h])

  const activityBuckets = useMemo(() => {
    const now = new Date()

    const roundedNow = new Date(now)
    const minutes = roundedNow.getMinutes()
    roundedNow.setMinutes(minutes < 30 ? 0 : 30, 0, 0)

    const buckets = []

    for (let i = 4; i >= 0; i--) {
      const end = new Date(roundedNow.getTime() - i * 30 * 60 * 1000)
      const start = new Date(end.getTime() - 30 * 60 * 1000)

      buckets.push({
        label: formatClockLabel(end),
        start,
        end,
        count: 0,
      })
    }

    alerts.forEach((alert) => {
      const ts = new Date(alert.timestamp)
      if (Number.isNaN(ts.getTime())) return

      buckets.forEach((bucket) => {
        if (ts >= bucket.start && ts < bucket.end) {
          bucket.count += 1
        }
      })
    })

    return buckets
  }, [alerts])

  const sourceItems = useMemo(() => {
    const canary = alerts24h.filter((a) => a.source === "canary").length
    const etw = alerts24h.filter((a) => a.source === "etw").length
    const minifilter = alerts24h.filter((a) => a.source === "minifilter").length

    return [
      { label: "Canary", value: canary, color: "bg-cyan-400/80" },
      { label: "ETW", value: etw, color: "bg-violet-400/80" },
      { label: "Minifilter", value: minifilter, color: "bg-emerald-400/80" },
    ]
  }, [alerts24h])

  const responseItems = useMemo(() => {
    const counts = {}
    alerts24h.forEach((a) => {
      const key = (a.response_taken || "unknown").replaceAll("_", " ")
      counts[key] = (counts[key] || 0) + 1
    })

    return Object.entries(counts)
      .map(([label, value]) => ({ label, value }))
      .sort((a, b) => b.value - a.value)
  }, [alerts24h])

  const recentAlerts = useMemo(() => alerts.slice(0, 5), [alerts])
  const systemActivity = useMemo(() => alerts.slice(0, 5), [alerts])

  const hostActivity = useMemo(() => {
    const latestByHost = {}

    alerts24h.forEach((a) => {
      const host = a.hostname || "Unknown"
      const existing = latestByHost[host]
      if (!existing) {
        latestByHost[host] = a
      } else {
        const currentTime = new Date(a.timestamp).getTime()
        const existingTime = new Date(existing.timestamp).getTime()
        if (currentTime > existingTime) {
          latestByHost[host] = a
        }
      }
    })

    return Object.values(latestByHost).slice(0, 4)
  }, [alerts24h])

  return (
    <div className="mx-auto max-w-7xl">
      <div className="mb-5">
        <h1 className="text-xl font-semibold text-slate-100">Dashboard</h1>
        <p className="mt-1 text-sm text-slate-400">
          real-time overview of ransomware alerts and system analysis
        </p>
      </div>

      {error ? (
        <div className="mb-4 rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-200">
          {error}
        </div>
      ) : null}

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-12">
        <div className="space-y-4 xl:col-span-6">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <SummaryCard
              title="Critical Alerts"
              value={loading ? "..." : criticalAlerts}
              subtitle="highest severity alerts"
              tone="red"
            />

            <SummaryCard
              title="Total Alerts"
              value={loading ? "..." : alerts.length}
              subtitle="all received alerts"
              tone="orange"
            />

            <SummaryCard
              title="Affected Hosts"
              value={loading ? "..." : uniqueHosts24h.length}
              subtitle="seen in the last 24h"
              tone="blue"
            />
          </div>

          <Panel title="Alert Activity" right="Last 2 Hours">
            <LineChartRecent buckets={activityBuckets} />
          </Panel>

          <Panel title="Source Activity" right="Last 24 Hours">
            <VerticalBars items={sourceItems} />
          </Panel>

          <Panel title="Response Actions Taken" right="Last 24 Hours">
            <ActionBars items={responseItems} />
          </Panel>
        </div>

        <div className="space-y-4 xl:col-span-3">
          <Panel title="Recent Alerts" right="Latest 5">
            <div className="space-y-3">
              {recentAlerts.length === 0 ? (
                <div className="py-8 text-center text-sm text-slate-500">No alerts yet</div>
              ) : (
                recentAlerts.map((alert, index) => (
                  <div
                    key={alert.id || index}
                    className="rounded-xl border border-slate-800 bg-slate-900/30 p-3"
                  >
                    <div className="mb-2 flex items-center justify-between gap-2">
                      <SeverityBadge severity={alert.severity} />
                      <span className="text-xs text-slate-500">
                        {formatRelativeTime(alert.timestamp)}
                      </span>
                    </div>

                    <div className="text-sm font-medium leading-snug text-slate-100">
                      {alert.rule_name}
                    </div>

                    <div className="mt-1 text-xs text-slate-500">
                      {alert.hostname} · {alert.source}
                    </div>

                    <div className="mt-2 text-xs text-slate-400">
                      process: {alert.data?.process_name || "-"}
                    </div>

                    <div className="mt-1 text-xs text-slate-400">
                      parent: {alert.data?.parent_process_name || "-"}
                    </div>
                  </div>
                ))
              )}
            </div>
          </Panel>

          <Panel title="System Activity Log" right="">
            <div className="space-y-3">
              {systemActivity.length === 0 ? (
                <div className="py-8 text-center text-sm text-slate-500">No activity</div>
              ) : (
                systemActivity.map((item, index) => (
                  <div
                    key={item.id || index}
                    className="rounded-xl border border-slate-800 bg-slate-900/30 p-3"
                  >
                    <div className="text-xs text-slate-500">
                      {formatRelativeTime(item.timestamp)}
                    </div>
                    <div className="mt-1 text-sm text-slate-200">
                      {item.data?.process_name || "-"}
                    </div>
                    <div className="mt-1 text-xs text-slate-400">
                      {item.rule_name}
                    </div>
                  </div>
                ))
              )}
            </div>
          </Panel>
        </div>

        <div className="space-y-4 xl:col-span-3">
          <Panel title="Online Endpoints" right="">
            <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
              <EndpointRing count={uniqueHosts24h.length} />
            </div>
          </Panel>

          <Panel title="Top Risk Host" right="">
            <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
              <div className="break-words text-3xl font-semibold leading-tight text-slate-100">
                {topRiskHost}
              </div>
              <div className="mt-3 text-sm text-slate-400">
                highest weighted alert severity in the last 24 hours
              </div>
            </div>
          </Panel>

          <Panel title="Top Triggered Rule" right="">
            <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
              <div className="break-words text-xl font-semibold leading-snug text-slate-100">
                {topRule}
              </div>
              <div className="mt-3 text-sm text-slate-400">
                derived from recent alert frequency
              </div>
            </div>
          </Panel>

          <Panel title="Host Activity" right="">
            <div className="space-y-3">
              {hostActivity.length === 0 ? (
                <div className="py-8 text-center text-sm text-slate-500">No hosts found</div>
              ) : (
                hostActivity.map((hostItem, index) => (
                  <div
                    key={index}
                    className="flex items-center justify-between rounded-xl border border-slate-800 bg-slate-900/30 px-3 py-2"
                  >
                    <div className="min-w-0">
                      <div className="truncate text-sm text-slate-200">
                        {hostItem.hostname}
                      </div>
                      <div className="mt-1 text-xs text-slate-500">
                        {hostItem.source} · {hostItem.rule_name}
                      </div>
                    </div>
                    <SeverityBadge severity={hostItem.severity} />
                  </div>
                ))
              )}
            </div>
          </Panel>
        </div>
      </div>
    </div>
  )
}