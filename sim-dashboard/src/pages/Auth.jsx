import { useEffect, useState } from "react"
import { useLocation, useNavigate } from "react-router-dom"
import logo from "../assets/logo.png"

export default function Auth() {
  const location = useLocation()
  const navigate = useNavigate()

  const [mode, setMode] = useState(
    location.pathname === "/signup" ? "signup" : "login"
  )

  const [form, setForm] = useState({
    fullName: "",
    username: "",
    password: "",
    role: "Security Analyst",
  })

  const [loading, setLoading] = useState(false)
  const [error, setError] = useState("")
  const [success, setSuccess] = useState("")

  useEffect(() => {
    setMode(location.pathname === "/signup" ? "signup" : "login")
    setError("")
    setSuccess("")
  }, [location.pathname])

  function updateField(field, value) {
    setForm((prev) => ({
      ...prev,
      [field]: value,
    }))
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setLoading(true)
    setError("")
    setSuccess("")

    try {
      const endpoint =
        mode === "login"
          ? "http://localhost:5000/api/auth/login"
          : "http://localhost:5000/api/auth/signup"

      const res = await fetch(endpoint, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          username: form.username,
          password: form.password,
          role: form.role,
          fullName: form.fullName,
        }),
      })

      const data = await res.json()

      if (!res.ok || !data.success) {
        throw new Error(data.message || "Authentication failed")
      }

      if (mode === "signup") {
        setSuccess("Account created successfully. You can now log in.")

        setMode("login")
        navigate("/login", { replace: true })

        setForm((prev) => ({
          ...prev,
          password: "",
        }))

        return
      }

      localStorage.setItem("isAuthenticated", "true")
      localStorage.setItem("username", data.username || form.username)
      localStorage.setItem("role", data.role || "USER")

      navigate("/dashboard", { replace: true })
    } catch (err) {
      localStorage.removeItem("isAuthenticated")
      localStorage.removeItem("username")
      localStorage.removeItem("role")
      setError(err.message || "Authentication failed")
    } finally {
      setLoading(false)
    }
  }

  function switchMode(nextMode) {
    setMode(nextMode)
    setError("")
    setSuccess("")
    navigate(nextMode === "signup" ? "/signup" : "/login", { replace: true })
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-950 px-4">
      <div className="absolute inset-0 opacity-30 bg-[radial-gradient(circle_at_1px_1px,#334155_1px,transparent_0)] [background-size:18px_18px]" />

      <div className="relative w-full max-w-lg overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/80 shadow-2xl">
        <div className="border-b border-slate-800 px-8 py-7">
          <div className="flex items-center justify-center gap-4">
            <img
              src={logo}
              alt="AEGIS Logo"
              className="h-11 w-11 shrink-0 rounded-xl object-contain"
            />
            <h1 className="text-xl font-bold text-slate-100">
              Ransomware Detection Dashboard
            </h1>
          </div>
        </div>

        <div className="px-8 pt-5">
          <div className="mx-auto grid w-full max-w-xs grid-cols-2 rounded-lg border border-slate-800 bg-slate-950/40 p-1">
            <button
              type="button"
              onClick={() => switchMode("login")}
              className={`rounded-md py-2 text-sm font-semibold ${
                mode === "login"
                  ? "bg-slate-800 text-white shadow"
                  : "text-slate-500 hover:text-slate-300"
              }`}
            >
              Log In
            </button>

            <button
              type="button"
              onClick={() => switchMode("signup")}
              className={`rounded-md py-2 text-sm font-semibold ${
                mode === "signup"
                  ? "bg-slate-800 text-white shadow"
                  : "text-slate-500 hover:text-slate-300"
              }`}
            >
              Sign Up
            </button>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 px-12 py-8">
          {error && (
            <div className="rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
              {error}
            </div>
          )}

          {success && (
            <div className="rounded-lg border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-300">
              {success}
            </div>
          )}

          {mode === "signup" ? (
            <div>
              <label className="text-sm font-semibold text-slate-200">
                Full Name
              </label>
              <input
                required
                value={form.fullName}
                onChange={(e) => updateField("fullName", e.target.value)}
                placeholder="Full name"
                className="mt-2 w-full rounded-lg border border-slate-800 bg-slate-950/40 px-4 py-3 text-sm text-white outline-none placeholder:text-slate-600 focus:border-sky-500/60"
              />
            </div>
          ) : null}

          <div>
            <label className="text-sm font-semibold text-slate-200">
              Username
            </label>
            <input
              required
              value={form.username}
              onChange={(e) => updateField("username", e.target.value)}
              placeholder="Username"
              className="mt-2 w-full rounded-lg border border-slate-800 bg-slate-950/40 px-4 py-3 text-sm text-white outline-none placeholder:text-slate-600 focus:border-sky-500/60"
            />
          </div>

          {mode === "signup" ? (
            <div>
              <label className="text-sm font-semibold text-slate-200">
                Role
              </label>
              <select
                value={form.role}
                onChange={(e) => updateField("role", e.target.value)}
                className="mt-2 w-full rounded-lg border border-slate-800 bg-slate-950/40 px-4 py-3 text-sm text-white outline-none focus:border-sky-500/60"
              >
                <option>Security Analyst</option>
                <option>System Administrator</option>
                <option>Incident Responder</option>
                <option>Manager</option>
              </select>
            </div>
          ) : null}

          <div>
            <label className="text-sm font-semibold text-slate-200">
              Password
            </label>
            <input
              required
              type="password"
              value={form.password}
              onChange={(e) => updateField("password", e.target.value)}
              placeholder="••••••••"
              className="mt-2 w-full rounded-lg border border-slate-800 bg-slate-950/40 px-4 py-3 text-sm text-white outline-none placeholder:text-slate-600 focus:border-sky-500/60"
            />
          </div>

          {mode === "login" ? (
            <div className="flex items-center justify-between text-sm">
              <label className="flex items-center gap-2 text-slate-400">
                <input type="checkbox" defaultChecked className="accent-sky-500" />
                Remember me
              </label>

              <button
                type="button"
                className="text-slate-400 hover:text-sky-300"
              >
                Forgot password?
              </button>
            </div>
          ) : null}

          <button
            disabled={loading}
            className="mt-3 w-full rounded-lg bg-gradient-to-r from-sky-400 to-blue-600 px-4 py-3 text-sm font-bold text-white shadow-lg shadow-blue-950/40 hover:from-sky-300 hover:to-blue-500 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {loading
              ? mode === "login"
                ? "Logging in..."
                : "Creating account..."
              : mode === "login"
                ? "Log In"
                : "Sign Up"}
          </button>

          <p className="pt-2 text-center text-sm text-slate-400">
            {mode === "login"
              ? "Don't have an account?"
              : "Already have an account?"}{" "}
            <button
              type="button"
              onClick={() => switchMode(mode === "login" ? "signup" : "login")}
              className="font-semibold text-sky-300 hover:text-sky-200"
            >
              {mode === "login" ? "Sign up" : "Log in"}
            </button>
          </p>
        </form>
      </div>
    </div>
  )
}