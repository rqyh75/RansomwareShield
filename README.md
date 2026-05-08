# 🛡️ RansomwareShield

<div align="center">

<img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-blue?style=for-the-badge" />
<img src="https://img.shields.io/badge/language-C%20%7C%20C%23%20%7C%20JavaScript-green?style=for-the-badge" />
<img src="https://img.shields.io/badge/status-Active-success?style=for-the-badge" />
<img src="https://img.shields.io/badge/security-Ransomware%20Detection-red?style=for-the-badge" />

### 🚨 Windows-Based Early Ransomware Detection & Response System

Using **Canary Files**, **Behavioral Analysis**, **MiniFilter Drivers**, and **ETW Monitoring**

</div>

---

# ✨ Overview

**RansomwareShield** is an advanced Windows-based ransomware detection and response system designed to detect suspicious ransomware behavior *before massive encryption damage happens*.

The project combines:

* 🪤 **Dynamic Canary Files**
* ⚡ **Behavioral Analysis**
* 🧠 **ETW Process Monitoring**
* 🛡️ **Kernel-Level Minifilter Driver**
* 📊 **Real-Time Monitoring Dashboard**
* 🔥 **Automatic Response Actions**

Unlike traditional antivirus systems that depend heavily on signatures, **RansomwareShield focuses on behavior**, making it more effective against evolving ransomware threats.

---

# 🎯 Key Features

## 🪤 Smart Canary Files

* Generates realistic fake documents
* Mimics human-created files
* Detects unauthorized access instantly
* Dynamic file creation, renaming, and modification

## ⚡ Behavioral Detection

* Monitors:

  * Mass file writes
  * Rapid renaming
  * Suspicious process execution
  * Shadow copy deletion attempts
  * Defender tampering

## 🧠 ETW Process Monitoring

* Uses **Event Tracing for Windows (ETW)**
* Detects malicious PowerShell commands
* Tracks suspicious process chains

## 🛡️ Kernel-Level Minifilter Driver

* Intercepts:

  * File writes
  * File renames
  * File creation requests
* Blocks suspicious ransomware activity

## 🚨 Automated Response

* Terminates malicious processes
* Generates structured alerts
* Sends logs to dashboard
* Preserves forensic evidence

## 📊 Web-Based Dashboard

* Real-time monitoring
* Severity visualization
* Historical reports
* Authentication system
* MongoDB event storage

---

# 🏗️ System Architecture

```text
+-----------------------------------------------------------+
|                    Web Dashboard                          |
|          React + SpringBoot + MongoDB                    |
+------------------------▲----------------------------------+
                         |
                    REST API
                         |
+-----------------------------------------------------------+
|                    Response Agent                         |
|      Correlation • Severity • Auto Response              |
+-----------▲----------------▲----------------▲-------------+
            |                |                |
        Named Pipe      FltSendMessage      ETW
            |                |                |
+-----------+----+   +-------+------+   +-----+------------+
| Canary Agent  |   | Minifilter   |   | ETW Process      |
|               |   | Driver       |   | Monitor          |
+----------------+  +--------------+   +------------------+
```

---

# 🧩 Core Components

| Component             | Description                                |
| --------------------- | ------------------------------------------ |
| 🪤 Canary Agent       | Creates and monitors realistic decoy files |
| 🛡️ Minifilter Driver | Kernel-level ransomware activity detection |
| 🧠 ETW Monitor        | Tracks suspicious process behavior         |
| ⚡ Response Agent      | Correlates alerts and executes actions     |
| 📊 Dashboard          | Visualizes alerts and system activity      |

---

# 🔥 Detection Techniques

## ✔ Canary File Detection

Detects:

* File modification
* File rename
* File deletion
* Unauthorized access

## ✔ Behavioral Analysis

Detects:

* High-frequency writes
* Mass renaming
* Ransom note creation
* Suspicious command-line patterns

## ✔ Known Ransomware Indicators

Monitors:

* Malicious PowerShell commands
* Ransomware extensions
* Defender bypass attempts
* Shadow copy deletion

---

# ⚙️ Technologies Used

## 💻 Backend & System Components

* C#
* C
* Windows Driver Kit (WDK)
* ETW (Event Tracing for Windows)
* Named Pipes
* JSON

## 🌐 Dashboard

* React
* JavaScript
* Tailwind CSS
* SpringBoot
* MongoDB

## 🛠 Development Tools

* Visual Studio 2022
* VMware / VirtualBox
* DebugView

---

# 📂 Project Structure

```bash
RansomwareShield/
│
├── CanaryAgent/
├── MiniFilterDriver/
├── ETWMonitor/
├── ResponseAgent/
├── DashboardFrontend/
├── DashboardBackend/
├── Dataset/
├── Rules/
├── Documentation/
└── README.md
```

---

# 🚀 Getting Started

## 📋 Prerequisites

Before running the project, install:

* Windows 10/11
* Visual Studio 2022
* Windows Driver Kit (WDK)
* Windows SDK
* .NET SDK
* Java JDK
* Apache Maven
* Node.js + npm
* MongoDB

---

# 🛠️ Installation

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

## 3️⃣ Build Dashboard Frontend

```bash
cd DashboardFrontend
npm install
npm run dev
```

---

## 4️⃣ Build Canary Agent & Response Agent

Open solution in **Visual Studio 2022** and build:

```bash
Build → Build Solution
```

---

## 5️⃣ Build Minifilter Driver

Using:

* WDK
* Visual Studio Driver Project

Then load the driver in **Test Mode**.

---

# 📊 Dashboard Features

* 📈 Real-time alerts
* 🚨 Severity indicators
* 📜 Historical reports
* 🖥️ Monitoring status
* 🔍 Alert filtering
* 👤 User authentication

---

# 🧪 Testing Environment

The system was tested inside isolated virtual machines using:

* VMware
* VirtualBox

Tested against ransomware families including:

* Akira
* LockBit
* Makop
* Medusa
* Ghost
* RansomHub
* Interlock

---

# 📚 Appendix A — User Manuals

## 👤 User Guide

### ▶ Starting the System

1. Launch the Response Agent
2. Start Canary Agent
3. Load Minifilter Driver
4. Start ETW Monitoring
5. Open Dashboard in browser

---

### 🛡 Monitoring Alerts

The dashboard displays:

* Severity level
* Event type
* Hostname
* Timestamp
* Response action

Severity Levels:

* 🟢 Low
* 🟡 Medium
* 🟠 High
* 🔴 Critical

---

### ⚡ Automatic Response

When ransomware behavior is detected:

* Process may be terminated automatically
* Alert sent to dashboard
* Event logged locally

---

### 📂 Viewing Reports

Navigate to:

```text
Dashboard → Reports
```

Available filters:

* Date range
* Severity
* Source
* Hostname

---

# 📖 Research & Academic Context

This project was developed as a **Final Year Project** at:

🎓 **Sultan Qaboos University**
College of Science — Department of Computer Science

Project Title:

> *Windows-Based Early Ransomware Detection and Response System using Canary Files and Behavioural Analysis*



---

# 👨‍💻 Authors

* Arwa Humaid Al Hajri
* Ruqaiyah Hamed Al Hashmi
* Liya Ahmed Al Azri
* Aseel Ghusn Al Harthi

Supervisor:

* Dr. Shadha Al Amri

---

# 📜 License

This project is developed for educational and research purposes.

---

# ⭐ Future Improvements

* AI-based anomaly detection
* Network-level ransomware detection
* Linux/macOS support
* Cloud dashboard deployment
* Threat intelligence integration

---

# 💡 Inspiration

> “Detect early. Respond instantly. Protect proactively.”

---

<div align="center">

## ⭐ If you like this project, give it a star ⭐

🛡️ Stay Safe • Stay Secure • Stop Ransomware Early

</div>
