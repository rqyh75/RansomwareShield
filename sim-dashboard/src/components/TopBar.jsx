import { useEffect, useMemo, useState } from "react"
import logo from "../assets/logo.png"

function HeaderStat({ label, value, valueClass = "text-slate-100" }) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-2.5 light:border-slate-300 light:bg-white">
      <div className="text-[10px] font-medium uppercase tracking-[0.14em] text-slate-400 light:text-slate-500">
        {label}
      </div>
      <div className={`mt-1.5 text-[1.35rem] font-semibold leading-none ${valueClass}`}>
        {value}
      </div>
    </div>
  )
}

function getAlertTime(alert) {
  const value =
    alert.timestamp ||
    alert.time ||
    alert.createdAt ||
    alert.eventTime

  if (!value) return null

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return null
  }

  return date
}

function getSystemStatus(alerts) {
  if (!alerts || alerts.length === 0) {
  return {
    text: "STABLE",
    className: "text-emerald-400",
    health: 100,
  }
}

  const now = new Date()

  const validAlertTimes = alerts
    .map(getAlertTime)
    .filter(Boolean)
    .sort((a, b) => b - a)

  const latestAlertTime = validAlertTimes[0]

  /*
    Reset rule:
    If the system was previously not stable, but 3 hours passed
    without receiving any new alert, the status becomes STABLE again.
  */
  if (latestAlertTime) {
    const threeHoursMs = 3 * 60 * 60 * 1000
    const timeSinceLatestAlert = now - latestAlertTime

    if (timeSinceLatestAlert >= threeHoursMs) {
      return {
        text: "STABLE",
        className: "text-emerald-400",
        health: 100,
      }
    }
  }

  const alertsLast60Seconds = alerts.filter((alert) => {
    const time = getAlertTime(alert)
    return time && now - time <= 60 * 1000
  }).length

  const hasCritical = alerts.some(
    (alert) => String(alert.severity || "").toLowerCase() === "critical"
  )

  const hasHigh = alerts.some(
    (alert) => String(alert.severity || "").toLowerCase() === "high"
  )

  const hasMedium = alerts.some(
    (alert) => String(alert.severity || "").toLowerCase() === "medium"
  )

  const hasLow = alerts.some(
    (alert) => String(alert.severity || "").toLowerCase() === "low"
  )

  if (alertsLast60Seconds >= 15 || hasCritical || hasHigh) {
    return {
      text: "UNDER ATTACK",
      className: "text-red-300 light:text-red-600",
      health: 10,
    }
  }

  if (alertsLast60Seconds >= 10 || hasMedium) {
    return {
      text: "HIGH RISK",
      className: "text-orange-300 light:text-orange-600",
      health: 35,
    }
  }

  if (alertsLast60Seconds >= 5 || hasLow) {
    return {
      text: "WARNING",
      className: "text-yellow-300 light:text-yellow-600",
      health: 65,
    }
  }

  return {
    text: "STABLE",
    className: "text-emerald-400",
    health: 100,
  }
}

export default function TopBar({ monitoringEnabled, setMonitoringEnabled }) {
  const [theme, setTheme] = useState("dark")
  const [currentTime, setCurrentTime] = useState("")
  const [alerts, setAlerts] = useState([])

  useEffect(() => {
    const savedTheme = localStorage.getItem("theme") || "dark"
    setTheme(savedTheme)

    if (savedTheme === "light") {
      document.documentElement.classList.add("light")
    } else {
      document.documentElement.classList.remove("light")
    }
  }, [])

  useEffect(() => {
    function updateTime() {
      const now = new Date()

      setCurrentTime(
        now.toLocaleTimeString([], {
          hour: "numeric",
          minute: "2-digit",
          second: "2-digit",
        })
      )
    }

    updateTime()

    const interval = setInterval(updateTime, 1000)

    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    let mounted = true

    async function loadAlerts() {
      try {
        const res = await fetch("/api/alerts")

        if (!res.ok) {
          throw new Error(`API error: ${res.status}`)
        }

        const json = await res.json()

        if (mounted) {
          setAlerts(json.items || [])
        }
      } catch {
          if (mounted) {
            setAlerts([])
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

  function toggleTheme() {
    const newTheme = theme === "dark" ? "light" : "dark"

    setTheme(newTheme)
    localStorage.setItem("theme", newTheme)

    if (newTheme === "light") {
      document.documentElement.classList.add("light")
    } else {
      document.documentElement.classList.remove("light")
    }
  }

  const systemStatus = useMemo(() => {
    return getSystemStatus(alerts)
  }, [alerts])

  return (
    <header className="border-b border-slate-800 bg-slate-950 px-6 py-3 light:border-slate-300 light:bg-slate-100">
      <div className="flex items-center justify-between gap-6">
        <div className="flex min-w-0 items-center gap-4">
          <img
            src={logo}
            alt="Cat Logo"
            className="h-11 w-11 shrink-0 rounded-xl object-contain"
          />

          <div className="min-w-0">
            <h1 className="mt-0.5 text-2xl font-bold tracking-tight text-white light:text-slate-900">
              RansomwareShield
            </h1>

            <p className="truncate text-xs font-normal text-slate-400 light:text-slate-600">
              Early Ransomware Detection and Response System
            </p>
          </div>
        </div>

        <div className="flex items-center justify-end gap-3">
          <HeaderStat
            label="System Status"
            value={systemStatus.text}
            valueClass={`text-[1.15rem] font-semibold leading-none ${systemStatus.className}`}
          />

          <HeaderStat
            label="System Health"
            value={`${systemStatus.health}%`}
            valueClass="text-[1.35rem] font-semibold leading-none text-white light:text-slate-900"
          />

          <button
            onClick={() => setMonitoringEnabled((prev) => !prev)}
            className={`rounded-xl border px-4 py-2.5 text-sm font-medium transition ${
              monitoringEnabled
                ? "border-emerald-500/40 bg-emerald-500/10 text-emerald-300 hover:bg-emerald-500/15"
                : "border-red-500/40 bg-red-500/10 text-red-300 hover:bg-red-500/15"
            }`}
          >
            Monitoring: {monitoringEnabled ? "ON" : "OFF"}
          </button>

          <button
            onClick={toggleTheme}
            className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-2.5 text-sm font-medium text-slate-200 transition hover:bg-slate-800 light:border-slate-300 light:bg-white light:text-slate-800 light:hover:bg-slate-200"
          >
            {theme === "dark" ? "Light Mode" : "Dark Mode"}
          </button>

          <div className="min-w-[95px] text-right text-[1.3rem] font-medium leading-none tracking-tight text-slate-200 light:text-slate-700">
            {currentTime}
          </div>
        </div>
      </div>
    </header>
  )
}