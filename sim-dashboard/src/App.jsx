import { useState } from "react"
import { Navigate, Route, Routes, useLocation } from "react-router-dom"

import TopBar from "./components/TopBar"
import SideNav from "./components/SideNav"

import Dashboard from "./pages/Dashboard"
import Alerts from "./pages/Alerts"
import Reports from "./pages/Reports"
import Settings from "./pages/Settings"
import MonitoringPaused from "./pages/MonitoringPaused"
import Auth from "./pages/Auth"

export default function App() {
  const [monitoringEnabled, setMonitoringEnabled] = useState(true)
  const location = useLocation()

  const isAuthPage =
    location.pathname === "/login" || location.pathname === "/signup"

  const isAuthenticated = localStorage.getItem("isAuthenticated") === "true"

  if (isAuthPage) {
    return (
      <Routes>
        <Route path="/login" element={<Auth />} />
        <Route path="/signup" element={<Auth />} />
      </Routes>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return (
    <div className="min-h-screen bg-slate-950 text-white light:bg-slate-100 light:text-slate-900">
      <TopBar
        monitoringEnabled={monitoringEnabled}
        setMonitoringEnabled={setMonitoringEnabled}
      />

      <div className="flex">
        <SideNav />

        <main className="flex-1 p-6">
          {monitoringEnabled ? (
            <Routes>
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="/dashboard" element={<Dashboard />} />
              <Route path="/alerts" element={<Alerts />} />
              <Route path="/reports" element={<Reports />} />
              <Route path="/settings" element={<Settings />} />
            </Routes>
          ) : (
            <MonitoringPaused />
          )}
        </main>
      </div>
    </div>
  )
}