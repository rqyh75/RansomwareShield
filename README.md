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

### 🛡️ RansomwareShield
a Windows-based cybersecurity project designed to detect, monitor, and respond to ransomware attacks during their early execution stages before large-scale file encryption and system damage occur.
The project combines multiple defensive layers operating in both **user mode** and **kernel mode** to provide real-time ransomware detection through behavioral analysis, file system monitoring, process activity inspection, and automated defensive response mechanisms.
Instead of relying solely on traditional signature-based antivirus detection, RansomwareShield focuses on behavior-driven analysis to identify suspicious activities commonly associated with modern ransomware attacks.
.

### Core Detection Layers :

* 🎯 **Dynamic Canary File Detection**
* ⚙️ **Kernel-level Minifilter Driver**
* 🔍 **ETW Process Event Monitoring**
* 🧠 **Behavioral & Process Analysis**
* 🛑 **Automated Threat Response**
* 📊 **Web-Based Monitoring Dashboard**

 # 📌 Detection Capabilities

| Detection Type                      | Supported |
| ----------------------------------- | --------- |
| Canary File Access                  | ✅         |
| Mass File Encryption Attempts       | ✅         |
| Rapid File Modification Activity    | ✅         |
| Mass File Renaming                  | ✅         |
| Ransom Note Creation                | ✅         |
| Abnormal Write-Frequency Detection  | ✅         |
| Suspicious PowerShell Execution     | ✅         |
| Script-Based Execution Detection    | ✅         |
| Shadow Copy Deletion Attempts       | ✅         |
| Malicious Process Execution Chains  | ✅         |
| Ransomware File Extension Detection | ✅         |
| Excessive I/O Request Detection     | ✅         |


---

# ✨ Components 

## 🐤 Canary Agent

The Canary Agent operates in user mode and is responsible for generating and monitoring decoy files that imitate realistic user documents.

The agent creates believable fake files using generated datasets and continuously watches for unauthorized access, modification, deletion, or encryption attempts. Since ransomware commonly targets user documents first, interacting with these canary files acts as an early warning indicator.

When suspicious activity is detected, the Canary Agent immediately sends alerts to the Response Agent using Named Pipes communication in JSON format.
### Canary Agent Responsibilities
* **Generate realistic decoy files**
* **Monitor file access behavior**
* **Detect unauthorized modifications**
* **Trigger early ransomware alerts**
* **Communicate alerts to the Response Agent**

---

## 🔍 Behavioral Detection Engine

* Detects abnormal file write activity
* Detects suspicious rename operations
* Detects ransomware note creation
* Detects malicious command lines using ETW

---

## ⚙️ Minifilter Driver

The Minifilter Driver operates at the Windows kernel level and monitors low-level file system operations in real time.

The driver observes file creation, deletion, renaming, and modification activities to detect ransomware-like behaviors such as mass file encryption, abnormal write frequency, suspicious extensions, and ransom note creation patterns.

The driver also supports defensive blocking capabilities by intercepting and denying malicious I/O requests when dangerous behavior thresholds are exceeded.

Communication between kernel mode and user mode is implemented using FltSendMessage.

## Minifilter Driver Responsibilities
* **Monitor file system activity**
* **Detect abnormal file operations**
* **Detect ransomware extensions and notes**
* **Track excessive write operations**
* **Block malicious I/O requests**
* **Send kernel alerts to user mode**

---

## 🔍 ETW Process Monitoring

The ETW Process Monitor uses Event Tracing for Windows (ETW) to observe process-related activities and suspicious execution behavior.

This component monitors:

* **Process creation**
* **PowerShell execution**
* **Script execution**
* **Suspicious command-line arguments**
** **Abnormal execution chains**
* **Potential ransomware behaviors**

Behavioral indicators are analyzed against a ransomware behavior dataset collected from public reports and threat intelligence references.

The ETW monitor strengthens detection by identifying malicious activity that may not yet interact directly with files.

## ETW Process Monitor Responsibilities
* **Monitor process activity**
* **Detect suspicious execution patterns**
* **Analyze command-line behavior**
* **Detect script abuse**
* **Identify ransomware indicators**
* **Generate behavioral alerts**


---

## 🚨 Response Agent

The Response Agent acts as the central coordination and defensive response component of the framework.

It receives alerts from:

* **Canary Agent**
* **Minifilter Driver**
* **ETW Process Monitor**

The Response Agent correlates incoming events, classifies threat severity, and performs automated response actions such as:

* **Process termination**
* **Alert forwarding**
* **Event logging**
* **Dashboard synchronization**

The Response Agent also standardizes communication using JSON-formatted data exchange between all system components.

## Response Agent Responsibilities

* **Collect alerts from all modules**
* **Correlate suspicious activities**
* **Execute automated response actions**
* **Terminate suspicious processes**
* **Forward logs to the dashboard**
* **Manage inter-component communication**
---

## 📊 Web Dashboard

The Web-Based Dashboard provides centralized real-time monitoring and visualization for system alerts and ransomware activities.

The dashboard displays:

* **Detected ransomware events**
* **Suspicious process information**
* **Severity levels**
* **Timestamps**
* **Response actions**
* **File activity logs**
* **Detection statistics**

The frontend was developed using React, while the backend uses Spring Boot APIs and MongoDB for event storage.

## Dashboard Responsibilities
* **Display real-time alerts**
* **Visualize system activity**
* **Store monitoring logs**
* **Provide centralized monitoring**
* **Display threat severity information**
* **Improve system usability and analysis**
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
