import { NavLink } from "react-router-dom"

const items = [
  { label: "Dashboard", to: "/dashboard" },
  { label: "Alerts", to: "/alerts" },
  { label: "Reports", to: "/reports" },
  { label: "Settings", to: "/settings" },
]

export default function SideNav() {
  return (
    <aside className="sticky top-[73px] flex h-[calc(100vh-73px)] w-64 shrink-0 flex-col border-r border-slate-800 bg-slate-950 px-5 py-5 light:border-slate-300 light:bg-slate-100">
      <div>
        <div className="mb-4 text-xs uppercase tracking-[0.18em] text-slate-500">
          Navigation
        </div>

        <nav className="space-y-2">
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `block rounded-xl px-4 py-3 text-sm font-medium transition ${
                  isActive
                    ? "border border-slate-700 bg-slate-900 text-white light:border-slate-300 light:bg-white light:text-slate-900"
                    : "text-slate-300 hover:bg-slate-900/50 hover:text-white light:text-slate-700 light:hover:bg-white"
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </div>

      <div className="mt-auto">
        <div className="flex items-center justify-between rounded-2xl border border-slate-800 bg-slate-900/40 p-3 light:border-slate-300 light:bg-white">
          <div className="flex min-w-0 items-center gap-3">
            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full border border-sky-400/40 bg-slate-950 text-slate-300 light:bg-slate-100">
              👤
            </div>

            <div className="min-w-0">
              <div className="truncate text-sm font-semibold text-slate-100 light:text-slate-900">
                Admin
              </div>
              <div className="truncate text-xs text-slate-500">
                Security Analyst
              </div>
            </div>
          </div>

          <button
            onClick={() => {
              localStorage.removeItem("isAuthenticated")
              window.location.href = "/login"
            }}
            className="ml-3 text-slate-500 transition hover:text-red-300"
            title="Logout"
          >
            ⏻
          </button>
        </div>
      </div>
    </aside>
  )
}