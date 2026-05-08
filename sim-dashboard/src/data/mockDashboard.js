//i only added this file for testing but later i will remove it
export const mockDashboard = {
  status: "SAFE",
  monitoring: true,
  alertsToday: 3,
  canaryTriggered: 1,
  suspiciousEvents: 2,
  recentAlerts: [
    { time: "12:02", file: "canary1.docx", action: "MODIFIED", severity: "HIGH" },
    { time: "12:05", file: "budget.xlsx", action: "RENAMED", severity: "MEDIUM" },
    { time: "12:06", file: "hr-policy.pdf", action: "ACCESSED", severity: "LOW" },
  ],
}
