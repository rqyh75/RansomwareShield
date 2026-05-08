import { useEffect, useState } from "react"
import Panel from "../components/Panel"
import {
  changePassword,
  defaultSettings,
  getAccount,
  getSettings,
  resetSettings,
  saveAccount,
  saveSettings,
} from "../utils/appSettings"

// ─── Helpers ──────────────────────────────────────────────────────────────────

const ACCENT_OPTIONS = [
  { value: "sky",     label: "Sky",     color: "#38bdf8" },
  { value: "blue",    label: "Blue",    color: "#3b82f6" },
  { value: "purple",  label: "Purple",  color: "#a855f7" },
  { value: "emerald", label: "Emerald", color: "#10b981" },
  { value: "red",     label: "Red",     color: "#ef4444" },
  { value: "orange",  label: "Orange",  color: "#f97316" },
]

const THEME_OPTIONS = [
  {
    value: "dark",
    label: "Dark",
    sub: "Default dark dashboard",
    preview: "#020617",
  },
  {
    value: "midnight",
    label: "Midnight",
    sub: "Deep black background",
    preview: "#000814",
  },
  {
    value: "slate",
    label: "Slate",
    sub: "Softer slate surface",
    preview: "#0f172a",
  },
  {
    value: "light",
    label: "Light",
    sub: "Bright mode",
    preview: "#f1f5f9",
  },
]

const REFRESH_OPTIONS = [
  { value: 3,  label: "Every 3 seconds  (fastest)" },
  { value: 5,  label: "Every 5 seconds" },
  { value: 10, label: "Every 10 seconds" },
  { value: 30, label: "Every 30 seconds" },
  { value: 60, label: "Every 1 minute" },
]

const ALERTS_PER_PAGE_OPTIONS = [10, 25, 50, 100]

// ─── Tiny sub-components ──────────────────────────────────────────────────────

function Toggle({ checked, onChange, id }) {
  return (
    <button
      type="button"
      id={id}
      role="switch"
      aria-checked={checked}
      onClick={() => onChange(!checked)}
      className={`relative h-7 w-12 flex-shrink-0 rounded-full transition-colors duration-200 focus:outline-none ${
        checked ? "bg-sky-500" : "bg-slate-700"
      }`}
    >
      <span
        className={`absolute top-1 h-5 w-5 rounded-full bg-white shadow-sm transition-all duration-200 ${
          checked ? "left-6" : "left-1"
        }`}
      />
    </button>
  )
}

function ToggleRow({ label, sub, checked, onChange }) {
  return (
    <div className="flex items-center justify-between rounded-xl border border-slate-800 bg-slate-900/30 px-4 py-3.5 light:border-slate-200 light:bg-slate-50">
      <div>
        <div className="text-sm font-medium text-slate-200 light:text-slate-800">{label}</div>
        {sub && <p className="mt-0.5 text-xs text-slate-500">{sub}</p>}
      </div>
      <Toggle checked={checked} onChange={onChange} />
    </div>
  )
}

function SectionTitle({ children, sub }) {
  return (
    <div className="mb-4 border-b border-slate-800/60 pb-3 light:border-slate-200">
      <h2 className="text-base font-semibold text-slate-100 light:text-slate-900">{children}</h2>
      {sub && <p className="mt-0.5 text-xs text-slate-500">{sub}</p>}
    </div>
  )
}

function FieldLabel({ htmlFor, children }) {
  return (
    <label
      htmlFor={htmlFor}
      className="mb-1.5 block text-sm font-medium text-slate-300 light:text-slate-700"
    >
      {children}
    </label>
  )
}

function Input({ id, type = "text", value, onChange, placeholder, className = "" }) {
  return (
    <input
      id={id}
      type={type}
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      className={`w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-200 outline-none transition focus:border-sky-500 focus:ring-1 focus:ring-sky-500/40 light:border-slate-300 light:bg-white light:text-slate-900 ${className}`}
    />
  )
}

function Select({ id, value, onChange, children, className = "" }) {
  return (
    <select
      id={id}
      value={value}
      onChange={onChange}
      className={`rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-200 outline-none transition focus:border-sky-500 light:border-slate-300 light:bg-white light:text-slate-900 ${className}`}
    >
      {children}
    </select>
  )
}

function Toast({ message, type = "success" }) {
  if (!message) return null
  const styles = {
    success: "border-emerald-500/30 bg-emerald-500/10 text-emerald-300",
    error:   "border-red-500/30   bg-red-500/10   text-red-300",
    info:    "border-sky-500/30   bg-sky-500/10   text-sky-300",
  }
  return (
    <div
      className={`flex items-center gap-2 rounded-xl border px-4 py-2 text-sm transition-all ${styles[type]}`}
    >
      <span>{type === "success" ? "✓" : type === "error" ? "✕" : "ℹ"}</span>
      <span>{message}</span>
    </div>
  )
}

// ─── Tab nav ──────────────────────────────────────────────────────────────────

const TABS = [
  { id: "account",       label: "Account",       icon: "👤" },
  { id: "appearance",    label: "Appearance",     icon: "🎨" },
  { id: "dashboard",     label: "Dashboard",      icon: "⚡" },
  { id: "notifications", label: "Notifications",  icon: "🔔" },
  { id: "security",      label: "Security",       icon: "🔒" },
  { id: "danger",        label: "Danger Zone",    icon: "⚠" },
]

// ─── Main Component ───────────────────────────────────────────────────────────

export default function Settings() {
  const [activeTab, setActiveTab] = useState("account")
  const [settings,  setSettings]  = useState(defaultSettings)
  const [account,   setAccount]   = useState({ displayName: "", email: "", role: "" })

  // Toast state
  const [toast, setToast] = useState({ message: "", type: "success" })

  // Password form
  const [pwForm, setPwForm] = useState({ current: "", next: "", confirm: "" })
  const [pwVisible, setPwVisible] = useState({ current: false, next: false, confirm: false })

  // Account editing
  const [accountDirty, setAccountDirty] = useState(false)

  // ── Load from localStorage on mount ────────────────────────────────────────

  useEffect(() => {
    setSettings(getSettings())
    setAccount(getAccount())
  }, [])

  // ── Helpers ─────────────────────────────────────────────────────────────────

  function showToast(message, type = "success") {
    setToast({ message, type })
    setTimeout(() => setToast({ message: "", type: "success" }), 2800)
  }

  function updateSetting(key, value) {
    setSettings(prev => {
      const updated = { ...prev, [key]: value }
      saveSettings(updated)
      return updated
    })
    showToast("Setting saved", "success")
  }

  // ── Account section ──────────────────────────────────────────────────────────

  function handleAccountChange(key, value) {
    setAccount(prev => ({ ...prev, [key]: value }))
    setAccountDirty(true)
  }

  function handleSaveAccount() {
    saveAccount(account)
    setAccountDirty(false)
    showToast("Account updated successfully", "success")
  }

  // ── Password change ──────────────────────────────────────────────────────────

  function handlePasswordChange(e) {
    e.preventDefault()
    if (pwForm.next !== pwForm.confirm) {
      showToast("New passwords do not match.", "error")
      return
    }
    const result = changePassword(pwForm.current, pwForm.next)
    if (result.ok) {
      setPwForm({ current: "", next: "", confirm: "" })
      showToast("Password changed successfully!", "success")
    } else {
      showToast(result.error, "error")
    }
  }

  // ── Render ───────────────────────────────────────────────────────────────────

  return (
    <div className="mx-auto max-w-5xl pb-16">

      {/* Page Header */}
      <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-100 light:text-slate-900">
            Settings
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            Manage your account, appearance, and dashboard preferences.
          </p>
        </div>
        <Toast message={toast.message} type={toast.type} />
      </div>

      <div className="flex gap-6">

        {/* Sidebar tabs */}
        <aside className="w-48 flex-shrink-0">
          <nav className="space-y-1 sticky top-[89px]">
            {TABS.map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex w-full items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium text-left transition-all ${
                  activeTab === tab.id
                    ? "bg-sky-500/15 text-sky-400 border border-sky-500/25"
                    : "text-slate-400 hover:bg-slate-900/50 hover:text-slate-200 border border-transparent light:hover:bg-slate-100 light:text-slate-600"
                }`}
              >
                <span className="text-base leading-none">{tab.icon}</span>
                {tab.label}
              </button>
            ))}
          </nav>
        </aside>

        {/* Content */}
        <div className="min-w-0 flex-1 space-y-6">

          {/* ── ACCOUNT ─────────────────────────────────────────────────────── */}
          {activeTab === "account" && (
            <>
              <Panel title="Profile" right="Stored locally">
                <SectionTitle sub="Your display name and role shown across the dashboard">
                  Account details
                </SectionTitle>

                <div className="mb-6 flex items-center gap-4">
                  <div className="flex h-16 w-16 flex-shrink-0 items-center justify-center rounded-full border-2 border-sky-500/30 bg-slate-800 text-2xl">
                    👤
                  </div>
                  <div>
                    <div className="text-base font-semibold text-slate-100 light:text-slate-900">
                      {account.displayName || "Admin"}
                    </div>
                    <div className="text-sm text-slate-400">{account.role}</div>
                    <div className="text-xs text-slate-500">{account.email}</div>
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <FieldLabel htmlFor="displayName">Display Name</FieldLabel>
                    <Input
                      id="displayName"
                      value={account.displayName}
                      onChange={e => handleAccountChange("displayName", e.target.value)}
                      placeholder="Your name"
                    />
                  </div>
                  <div>
                    <FieldLabel htmlFor="role">Role</FieldLabel>
                    <Input
                      id="role"
                      value={account.role}
                      onChange={e => handleAccountChange("role", e.target.value)}
                      placeholder="e.g. Security Analyst"
                    />
                  </div>
                  <div className="sm:col-span-2">
                    <FieldLabel htmlFor="email">Email</FieldLabel>
                    <Input
                      id="email"
                      type="email"
                      value={account.email}
                      onChange={e => handleAccountChange("email", e.target.value)}
                      placeholder="admin@aegis.local"
                    />
                  </div>
                </div>

                {accountDirty && (
                  <div className="mt-4 flex gap-3">
                    <button
                      onClick={handleSaveAccount}
                      className="rounded-xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-sky-400 active:scale-95"
                    >
                      Save Changes
                    </button>
                    <button
                      onClick={() => { setAccount(getAccount()); setAccountDirty(false) }}
                      className="rounded-xl border border-slate-700 bg-slate-900 px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-slate-800"
                    >
                      Cancel
                    </button>
                  </div>
                )}
              </Panel>

              <Panel title="Session" right="">
                <SectionTitle sub="Current authentication details">
                  Active session
                </SectionTitle>
                <div className="space-y-2">
                  {[
                    ["Logged in as",  account.displayName || "Admin"],
                    ["Session type",  "Browser (localStorage)"],
                    ["Auth method",   "Password"],
                  ].map(([k, v]) => (
                    <div
                      key={k}
                      className="flex items-center justify-between rounded-xl border border-slate-800 bg-slate-900/30 px-4 py-3 light:border-slate-200 light:bg-slate-50"
                    >
                      <span className="text-sm text-slate-400">{k}</span>
                      <span className="text-sm font-medium text-slate-200 light:text-slate-800">{v}</span>
                    </div>
                  ))}
                </div>

                <div className="mt-4">
                  <button
                    onClick={() => {
                      localStorage.removeItem("isAuthenticated")
                      window.location.href = "/login"
                    }}
                    className="rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-2 text-sm font-semibold text-red-300 transition hover:bg-red-500/20"
                  >
                    Sign Out
                  </button>
                </div>
              </Panel>
            </>
          )}

          {/* ── APPEARANCE ──────────────────────────────────────────────────── */}
          {activeTab === "appearance" && (
            <>
              <Panel title="Appearance" right="Applied instantly">
                <SectionTitle sub="Choose how the dashboard looks">
                  Theme
                </SectionTitle>

                <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                  {THEME_OPTIONS.map(t => (
                    <button
                      key={t.value}
                      onClick={() => updateSetting("theme", t.value)}
                      className={`relative overflow-hidden rounded-xl border px-3 py-3 text-left text-sm transition ${
                        settings.theme === t.value
                          ? "border-sky-500 ring-1 ring-sky-500/40"
                          : "border-slate-800 hover:border-slate-600"
                      }`}
                    >
                      <div
                        className="mb-2 h-10 w-full rounded-lg border border-white/10"
                        style={{ background: t.preview }}
                      />
                      <div className={`font-semibold ${settings.theme === t.value ? "text-sky-300" : "text-slate-300"}`}>
                        {t.label}
                      </div>
                      <div className="mt-0.5 text-xs text-slate-500">{t.sub}</div>
                      {settings.theme === t.value && (
                        <span className="absolute right-2 top-2 flex h-4 w-4 items-center justify-center rounded-full bg-sky-500 text-[10px] text-white">✓</span>
                      )}
                    </button>
                  ))}
                </div>

                <div className="mt-6">
                  <SectionTitle sub="Highlight colour used for active elements and buttons">
                    Accent colour
                  </SectionTitle>
                  <div className="grid grid-cols-3 gap-3 sm:grid-cols-6">
                    {ACCENT_OPTIONS.map(a => (
                      <button
                        key={a.value}
                        onClick={() => updateSetting("accentColor", a.value)}
                        className={`flex flex-col items-center gap-2 rounded-xl border py-3 text-xs font-medium transition ${
                          settings.accentColor === a.value
                            ? "border-sky-500 bg-sky-500/10 text-slate-100"
                            : "border-slate-800 bg-slate-900/40 text-slate-400 hover:border-slate-600"
                        }`}
                      >
                        <span
                          className="h-6 w-6 rounded-full shadow-sm"
                          style={{ backgroundColor: a.color }}
                        />
                        {a.label}
                      </button>
                    ))}
                  </div>
                  <p className="mt-2 text-xs text-slate-500">
                    The accent colour updates the sidebar active state, buttons, and focus rings.
                  </p>
                </div>

                <div className="mt-6 space-y-3">
                  <SectionTitle sub="Visual behaviour options">Display options</SectionTitle>
                  <ToggleRow
                    label="Animations"
                    sub="Enable page transitions and loading effects"
                    checked={settings.animations}
                    onChange={v => updateSetting("animations", v)}
                  />
                  <ToggleRow
                    label="Compact Mode"
                    sub="Reduce card and panel padding throughout the UI"
                    checked={settings.compactMode}
                    onChange={v => updateSetting("compactMode", v)}
                  />
                </div>
              </Panel>
            </>
          )}

          {/* ── DASHBOARD ───────────────────────────────────────────────────── */}
          {activeTab === "dashboard" && (
            <Panel title="Dashboard Behaviour" right="Affects live data">
              <SectionTitle sub="How frequently the dashboard polls for new alerts">
                Live refresh interval
              </SectionTitle>

              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3 lg:grid-cols-5">
                {REFRESH_OPTIONS.map(opt => (
                  <button
                    key={opt.value}
                    onClick={() => updateSetting("dashboardRefreshSeconds", opt.value)}
                    className={`rounded-xl border px-3 py-3 text-center text-sm transition ${
                      settings.dashboardRefreshSeconds === opt.value
                        ? "border-sky-500 bg-sky-500/10 text-sky-300 ring-1 ring-sky-500/30"
                        : "border-slate-800 bg-slate-900/30 text-slate-400 hover:border-slate-600 hover:text-slate-200"
                    }`}
                  >
                    <div className="text-lg font-bold tabular-nums">
                      {opt.value < 60 ? `${opt.value}s` : "1m"}
                    </div>
                    <div className="mt-1 text-[11px] text-slate-500">
                      {opt.value < 60 ? "seconds" : "minute"}
                    </div>
                  </button>
                ))}
              </div>

              <p className="mt-3 text-xs text-slate-500">
                Current: every <strong className="text-slate-300">{settings.dashboardRefreshSeconds} second{settings.dashboardRefreshSeconds !== 1 ? "s" : ""}</strong>.
                Faster refresh uses more bandwidth.
              </p>

              <div className="mt-6">
                <SectionTitle sub="Controls how alerts are listed and timestamped">
                  Alert display
                </SectionTitle>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <FieldLabel htmlFor="alertsPerPage">Alerts per page</FieldLabel>
                    <Select
                      id="alertsPerPage"
                      value={settings.alertsPerPage}
                      onChange={e => updateSetting("alertsPerPage", Number(e.target.value))}
                      className="w-full"
                    >
                      {ALERTS_PER_PAGE_OPTIONS.map(n => (
                        <option key={n} value={n}>{n} alerts</option>
                      ))}
                    </Select>
                  </div>

                  <div>
                    <FieldLabel htmlFor="dateFormat">Timestamp format</FieldLabel>
                    <Select
                      id="dateFormat"
                      value={settings.dateFormat}
                      onChange={e => updateSetting("dateFormat", e.target.value)}
                      className="w-full"
                    >
                      <option value="relative">Relative (2m ago)</option>
                      <option value="absolute">Absolute (Apr 30, 14:22)</option>
                    </Select>
                  </div>
                </div>
              </div>

              <div className="mt-6">
                <SectionTitle sub="Extra display toggles">Options</SectionTitle>
                <div className="space-y-3">
                  <ToggleRow
                    label="Show timestamps"
                    sub="Display time labels on alert feed entries"
                    checked={settings.showTimestamp}
                    onChange={v => updateSetting("showTimestamp", v)}
                  />
                </div>
              </div>
            </Panel>
          )}

          {/* ── NOTIFICATIONS ───────────────────────────────────────────────── */}
          {activeTab === "notifications" && (
            <Panel title="Notifications" right="Browser alerts">
              <SectionTitle sub="Choose which events trigger a browser notification">
                Alert notifications
              </SectionTitle>

              <div className="space-y-3">
                <ToggleRow
                  label="Critical alerts"
                  sub="Get notified immediately when a critical-severity alert fires"
                  checked={settings.notifyOnCritical}
                  onChange={v => updateSetting("notifyOnCritical", v)}
                />
                <ToggleRow
                  label="High-severity alerts"
                  sub="Notify on high-severity detections"
                  checked={settings.notifyOnHigh}
                  onChange={v => updateSetting("notifyOnHigh", v)}
                />
                <ToggleRow
                  label="Notification sound"
                  sub="Play an audio chime alongside browser notifications"
                  checked={settings.notifySound}
                  onChange={v => updateSetting("notifySound", v)}
                />
              </div>

              <div className="mt-4 rounded-xl border border-slate-800/60 bg-slate-900/20 px-4 py-3 text-xs text-slate-500 light:border-slate-200 light:bg-slate-50">
                Browser notifications require permission. If you denied it previously, reset it in your browser's site settings for this page.
              </div>

              <div className="mt-4">
                <button
                  onClick={() => {
                    if (!("Notification" in window)) {
                      showToast("Your browser doesn't support notifications.", "error")
                      return
                    }
                    Notification.requestPermission().then(perm => {
                      if (perm === "granted") {
                        new Notification("AEGIS", { body: "Notifications are working correctly." })
                        showToast("Test notification sent!", "success")
                      } else {
                        showToast("Permission denied — allow notifications in your browser settings.", "error")
                      }
                    })
                  }}
                  className="rounded-xl border border-slate-700 bg-slate-900 px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-slate-800"
                >
                  Send Test Notification
                </button>
              </div>
            </Panel>
          )}

          {/* ── SECURITY / PASSWORD ──────────────────────────────────────────── */}
          {activeTab === "security" && (
            <Panel title="Security" right="Password & access">
              <SectionTitle sub="Update your login credentials">
                Change password
              </SectionTitle>

              <form onSubmit={handlePasswordChange} className="space-y-4 sm:max-w-sm">
                {[
                  { id: "current", label: "Current password",  key: "current" },
                  { id: "next",    label: "New password",      key: "next"    },
                  { id: "confirm", label: "Confirm new password", key: "confirm" },
                ].map(({ id, label, key }) => (
                  <div key={id}>
                    <FieldLabel htmlFor={id}>{label}</FieldLabel>
                    <div className="relative">
                      <Input
                        id={id}
                        type={pwVisible[key] ? "text" : "password"}
                        value={pwForm[key]}
                        onChange={e => setPwForm(f => ({ ...f, [key]: e.target.value }))}
                        placeholder="••••••••"
                      />
                      <button
                        type="button"
                        onClick={() => setPwVisible(v => ({ ...v, [key]: !v[key] }))}
                        className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-slate-500 hover:text-slate-300"
                      >
                        {pwVisible[key] ? "Hide" : "Show"}
                      </button>
                    </div>
                  </div>
                ))}

                <div className="rounded-xl border border-slate-800/60 bg-slate-900/20 px-3 py-2.5 text-xs text-slate-500">
                  Password must be at least 6 characters. Default is <code className="text-slate-400">admin123</code>.
                </div>

                <button
                  type="submit"
                  className="rounded-xl bg-sky-500 px-5 py-2 text-sm font-semibold text-white transition hover:bg-sky-400 active:scale-95"
                >
                  Update Password
                </button>
              </form>

              <div className="mt-8">
                <SectionTitle sub="Actions that affect access to this dashboard">
                  Access control
                </SectionTitle>
                <div className="space-y-3">
                  <div className="flex items-center justify-between rounded-xl border border-slate-800 bg-slate-900/30 px-4 py-3.5 light:border-slate-200 light:bg-slate-50">
                    <div>
                      <div className="text-sm font-medium text-slate-200 light:text-slate-800">Sign out of this device</div>
                      <p className="mt-0.5 text-xs text-slate-500">Clears authentication and returns to login</p>
                    </div>
                    <button
                      onClick={() => {
                        localStorage.removeItem("isAuthenticated")
                        window.location.href = "/login"
                      }}
                      className="flex-shrink-0 rounded-xl border border-red-500/30 bg-red-500/10 px-3 py-1.5 text-xs font-semibold text-red-300 transition hover:bg-red-500/20"
                    >
                      Sign Out
                    </button>
                  </div>
                </div>
              </div>
            </Panel>
          )}

          {/* ── DANGER ZONE ─────────────────────────────────────────────────── */}
          {activeTab === "danger" && (
            <Panel title="Danger Zone" right="Irreversible actions">
              <div className="space-y-4">
                <div className="rounded-xl border border-red-500/20 bg-red-500/5 p-4">
                  <div className="mb-3 flex items-start gap-2">
                    <span className="mt-0.5 text-red-400">⚠</span>
                    <p className="text-xs text-red-300/80">
                      These actions cannot be undone. Proceed with caution.
                    </p>
                  </div>

                  <div className="space-y-4">
                    {/* Reset settings */}
                    <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3.5">
                      <div>
                        <div className="text-sm font-semibold text-slate-200">Reset all settings</div>
                        <p className="mt-0.5 text-xs text-slate-500">
                          Restores all dashboard preferences to factory defaults
                        </p>
                      </div>
                      <button
                        onClick={() => {
                          const reset = resetSettings()
                          setSettings(reset)
                          showToast("Settings reset to defaults", "info")
                        }}
                        className="flex-shrink-0 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-2 text-sm font-semibold text-red-300 transition hover:bg-red-500/20"
                      >
                        Reset Settings
                      </button>
                    </div>

                    {/* Clear all data */}
                    <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3.5">
                      <div>
                        <div className="text-sm font-semibold text-slate-200">Clear all stored data</div>
                        <p className="mt-0.5 text-xs text-slate-500">
                          Removes all localStorage entries including account info, settings, and session
                        </p>
                      </div>
                      <button
                        onClick={() => {
                          if (window.confirm("Are you sure? This clears all data and signs you out.")) {
                            localStorage.clear()
                            window.location.href = "/login"
                          }
                        }}
                        className="flex-shrink-0 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-2 text-sm font-semibold text-red-300 transition hover:bg-red-500/20"
                      >
                        Clear Everything
                      </button>
                    </div>
                  </div>
                </div>

                {/* Debug view */}
                <div>
                  <div className="mb-2 text-xs font-medium text-slate-500 uppercase tracking-widest">
                    Current saved settings (debug)
                  </div>
                  <pre className="overflow-auto rounded-xl border border-slate-800 bg-slate-950 p-4 text-xs text-slate-400 light:border-slate-200 light:bg-slate-50 light:text-slate-600">
                    {JSON.stringify(settings, null, 2)}
                  </pre>
                </div>
              </div>
            </Panel>
          )}

        </div>
      </div>
    </div>
  )
}
