# 🛡️ RansomwareShield

<div align="center">

<img src="https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-blue?style=for-the-badge" />
<img src="https://img.shields.io/badge/Language-C%20%7C%20C%23%20%7C%20JavaScript-success?style=for-the-badge" />
<img src="https://img.shields.io/badge/Status-Active%20Development-orange?style=for-the-badge" />
<img src="https://img.shields.io/badge/Security-Ransomware%20Detection-red?style=for-the-badge" />

### 🚨 Windows-Based Early Ransomware Detection and Response System

### using Canary Files & Behavioural Analysis

</div>

---

# 📖 Overview

**RansomwareShield** is a Windows-based cybersecurity project designed to detect and respond to ransomware attacks in their **early stages** before massive encryption damage occurs.

The system combines:

* 🎯 **Dynamic Canary Files**
* 🧠 **Behavioral Analysis**
* ⚡ **Real-Time Detection**
* 🛑 **Automated Response**
* 📊 **Web-Based Monitoring Dashboard**

Unlike traditional antivirus solutions that rely heavily on signatures, RansomwareShield focuses on **behavior-driven detection** to identify suspicious ransomware activities such as:

* Mass file encryption
* Rapid file renaming
* Suspicious process execution
* Shadow copy deletion attempts
* Unauthorized canary file access

---

# ✨ Features

## 🐤 Canary File Protection

* Generates realistic fake files
* Mimics human behavior using timestamps & metadata
* Detects:

  * Access
  * Modification
  * Rename
  * Deletion

---

## 🔍 Behavioral Detection Engine

* Detects abnormal file write activity
* Detects suspicious rename operations
* Detects ransomware note creation
* Detects malicious command lines using ETW

---

## ⚙️ Minifilter Driver

* Kernel-level monitoring
* Intercepts:

  * CREATE
  * WRITE
  * SET_INFORMATION operations
* Uses Windows Filter Manager

---

## 📡 ETW Process Monitoring

* Monitors process creation events
* Detects:

  * PowerShell abuse
  * vssadmin deletion commands
  * Suspicious scripts
  * Known malicious tools

---

## 🚨 Automated Response Agent

* Assigns severity levels
* Terminates malicious processes
* Sends alerts to dashboard
* Stores forensic logs

---

## 📊 Web Dashboard

* Live monitoring
* Alert visualization
* Historical reports
* Severity filtering
* System statistics

---

# 🏗️ System Architecture

```text
+------------------------------------------------------+
|                  Web Dashboard                       |
|      (React + SpringBoot + MongoDB)                 |
+-------------------------▲----------------------------+
                          |
                    REST API (JSON)
                          |
+------------------------------------------------------+
|                 Response Agent                       |
|  - Correlation                                       |
|  - Severity Assignment                               |
|  - Process Termination                               |
|  - Alert Logging                                     |
+---------▲-------------------▲------------------------+
          |                   |
          |                   |
   Named Pipe         FltSendMessage
          |                   |
+---------+-----+     +------+-------------------------+
| Canary Agent |     | Minifilter Driver              |
|               |     | Kernel-Level File Monitoring  |
+---------------+     +-------------------------------+

                +------------------------------------+
                | ETW Process Monitoring             |
                | Event Tracing for Windows          |
                +------------------------------------+
```

---

# 🛠️ Tech Stack

| Component          | Technology                   |
| ------------------ | ---------------------------- |
| Canary Agent       | C#                           |
| Response Agent     | C#                           |
| ETW Monitoring     | C# + ETW                     |
| Minifilter Driver  | C + WDK                      |
| Frontend Dashboard | React + Tailwind CSS         |
| Backend Dashboard  | Java SpringBoot              |
| Database           | MongoDB                      |
| IPC                | Named Pipes + FltSendMessage |
| IDE                | Visual Studio 2022           |

---

# 📂 Project Structure

```bash
RansomwareShield/
│
├── CanaryAgent/
├── MinifilterDriver/
├── ETWMonitor/
├── ResponseAgent/
├── DashboardFrontend/
├── DashboardBackend/
├── Dataset/
├── Docs/
└── README.md
```

---

# 🚀 Getting Started

## 📋 Prerequisites

Before running the project, install:

* Windows 10 / Windows 11
* Visual Studio 2022
* Windows Driver Kit (WDK)
* Windows SDK
* Java JDK
* Apache Maven
* Node.js & npm
* MongoDB

---

# ⚡ Installation

## 1️⃣ Clone Repository

```bash
git clone https://github.com/rqyh75/RansomwareShield.git
cd RansomwareShield
```

---

## 2️⃣ Build Dashboard Backend

```bash
cd DashboardBackend
mvn spring-boot:run
```

---

## 3️⃣ Run Dashboard Frontend

```bash
cd DashboardFrontend
npm install
npm run dev
```

---

## 4️⃣ Build Minifilter Driver

Open solution in Visual Studio with WDK installed.

Build in:

```text
Release x64
```

---

## 5️⃣ Run Canary Agent & Response Agent

```bash
cd CanaryAgent
dotnet run

cd ../ResponseAgent
dotnet run
```

---

# 🧪 Testing Environment

⚠️ IMPORTANT:

This project should ONLY be tested inside:

* Virtual Machines
* Isolated environments
* Sandboxed systems

Recommended:

* VMware
* VirtualBox

Never execute ransomware samples on a host machine.

---

# 📌 Detection Capabilities

| Detection Type        | Supported |
| --------------------- | --------- |
| Canary File Access    | ✅         |
| Mass File Writes      | ✅         |
| Mass Renaming         | ✅         |
| Ransom Note Creation  | ✅         |
| Suspicious PowerShell | ✅         |
| Shadow Copy Deletion  | ✅         |
| Known Malicious Tools | ✅         |

---

# 📷 Dashboard Preview

## Main Dashboard

* Live alerts
* Severity statistics
* Host activity
* Monitoring status

## Reports Page

* Historical logs
* Filtering
* Trend analysis

## Alerts Page

* Real-time incidents
* Alert details
* Severity visualization

---

# 📚 Manual Usage

The following user manual is based on **Appendix A: User Manuals** from the project report. 

---

# 👤 User Manual

## 🔐 Login to Dashboard

1. Open the dashboard in browser
2. Enter:

   * Username
   * Password
3. Click **Login**

If no account exists:

* Click **Sign Up**
* Create a new account

---

## ▶️ Start Monitoring

1. Start:

   * Canary Agent
   * Response Agent
   * ETW Monitor
   * Minifilter Driver
2. Open dashboard
3. Verify status shows:

```text
Monitoring Active
```

---

## 🐤 Canary Files

The system automatically:

* Creates fake files
* Places them in directories
* Monitors them continuously

Do NOT manually modify canary files.

---

## 🚨 Alert Monitoring

When ransomware behavior is detected:

The dashboard displays:

* Timestamp
* Severity
* Source
* Rule Name
* Response Action

Severity Levels:

* 🟢 Low
* 🟡 Medium
* 🟠 High
* 🔴 Critical

---

## 🛑 Automated Response

For High/Critical threats:

* Suspicious process is terminated automatically
* Alert is logged
* Dashboard updates instantly

---

## 📊 Reports

Navigate to:

```text
Reports Page
```

Features:

* Historical analysis
* Filtering by severity
* Event trends
* Host analysis

---

## ⚙️ Settings

Administrators can:

* Manage accounts
* Configure dashboard preferences
* View monitoring settings

---

# 🔒 Security Notes

* Designed specifically for Windows systems
* Requires administrator privileges
* Uses secure IPC mechanisms
* Logs events locally for forensic analysis

---

# 📈 Future Improvements

* 🌐 Network-based ransomware detection
* ☁️ Cloud dashboard deployment
* 🤖 Machine learning integration
* 🔔 Email/SMS alerting
* 🧩 SIEM integration
* 🐧 Linux support

---

# 👨‍💻 Authors

* Arwa Humaid Al Hajri
* Ruqaiyah Hamed Al Hashmi
* Liya Ahmed Al Azri
* Aseel Ghusn Al Harthi

---

# 🎓 Academic Information

**Sultan Qaboos University**
College of Science
Department of Computer Science

Final Year Project — Spring 2026

Supervisor:

* Dr. Shadha Al Amri

Examiner:

* Dr. Ahmad Soleimani

---

# 📜 License

This project is developed for educational and research purposes.

---

# ⭐ Support the Project

If you like this project:

🌟 Star the repository
🍴 Fork the project
🐛 Report issues
💡 Suggest improvements

---

<div align="center">

## 🛡️ Detect Early. Respond Fast. Stay Protected.

</div>
