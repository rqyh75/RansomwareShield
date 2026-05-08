const SETTINGS_KEY = "sim_dashboard_settings"
const ACCOUNT_KEY  = "sim_dashboard_account"

// ─── Defaults ─────────────────────────────────────────────────────────────────

export const defaultSettings = {
  // Appearance
  theme:                   "dark",   // "dark" | "midnight" | "slate" | "light"
  accentColor:             "sky",
  // Dashboard behaviour
  dashboardRefreshSeconds: 3,
  compactMode:             false,
  animations:              true,
  // Notifications
  notifyOnCritical:        true,
  notifyOnHigh:            false,
  notifySound:             false,
  // Display
  showTimestamp:           true,
  dateFormat:              "relative", // "relative" | "absolute"
  alertsPerPage:           25,
}

export const defaultAccount = {
  displayName: "Admin",
  email:       "admin@aegis.local",
  role:        "Security Analyst",
  // password is never stored — only validated against a hash placeholder
}

// ─── Settings helpers ──────────────────────────────────────────────────────────

export function getSettings() {
  try {
    const saved = localStorage.getItem(SETTINGS_KEY)
    return saved ? { ...defaultSettings, ...JSON.parse(saved) } : { ...defaultSettings }
  } catch {
    return { ...defaultSettings }
  }
}

export function saveSettings(settings) {
  const merged = { ...defaultSettings, ...settings }
  localStorage.setItem(SETTINGS_KEY, JSON.stringify(merged))
  applySettings(merged)
  window.dispatchEvent(new CustomEvent("settingsChanged", { detail: merged }))
  return merged
}

export function resetSettings() {
  localStorage.removeItem(SETTINGS_KEY)
  applySettings(defaultSettings)
  window.dispatchEvent(new CustomEvent("settingsChanged", { detail: defaultSettings }))
  return { ...defaultSettings }
}

// ─── Account helpers ───────────────────────────────────────────────────────────

export function getAccount() {
  try {
    const saved = localStorage.getItem(ACCOUNT_KEY)
    return saved ? { ...defaultAccount, ...JSON.parse(saved) } : { ...defaultAccount }
  } catch {
    return { ...defaultAccount }
  }
}

export function saveAccount(account) {
  const merged = { ...defaultAccount, ...account }
  localStorage.setItem(ACCOUNT_KEY, JSON.stringify(merged))
  window.dispatchEvent(new CustomEvent("accountChanged", { detail: merged }))
  return merged
}

/**
 * Simple password check — stores a hashed version using btoa for demo.
 * In production, replace with a real API call.
 */
const PASSWORD_KEY = "sim_dashboard_pw"

export function getStoredPasswordHash() {
  return localStorage.getItem(PASSWORD_KEY) || btoa("admin123")
}

export function verifyPassword(plain) {
  return btoa(plain) === getStoredPasswordHash()
}

export function changePassword(currentPlain, newPlain) {
  if (!verifyPassword(currentPlain)) {
    return { ok: false, error: "Current password is incorrect." }
  }
  if (newPlain.length < 6) {
    return { ok: false, error: "New password must be at least 6 characters." }
  }
  localStorage.setItem(PASSWORD_KEY, btoa(newPlain))
  return { ok: true }
}

// ─── Apply settings to DOM ─────────────────────────────────────────────────────

export function applySettings(settings = getSettings()) {
  const root = document.documentElement

  // Theme
  const isLight = settings.theme === "light"
  root.classList.toggle("light", isLight)
  root.dataset.theme      = settings.theme
  root.dataset.accent     = settings.accentColor
  root.dataset.compact    = settings.compactMode   ? "true" : "false"
  root.dataset.animations = settings.animations    ? "true" : "false"

  // Accent colour CSS variable
  const accentMap = {
    sky:     "#38bdf8",
    blue:    "#3b82f6",
    purple:  "#a855f7",
    emerald: "#10b981",
    red:     "#ef4444",
    orange:  "#f97316",
  }
  root.style.setProperty("--accent", accentMap[settings.accentColor] || accentMap.sky)
}
