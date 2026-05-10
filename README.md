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

<svg width="860" height="800" viewBox="0 0 860 800" xmlns="http://www.w3.org/2000/svg" font-family="'Segoe UI', system-ui, -apple-system, sans-serif">
  <rect width="860" height="800" fill="#0d1117" rx="12"/>
  <text x="430" y="36" text-anchor="middle" fill="#e6edf3" font-size="15" font-weight="600" letter-spacing="0.5">Ransomware Detection &amp; Response System — Architecture</text>
  <rect x="28" y="56" width="804" height="120" rx="10" fill="#161b22" stroke="#f0883e" stroke-width="1" stroke-dasharray="6 4"/>
  <text x="44" y="76" fill="#f0883e" font-size="10" font-weight="600" letter-spacing="1">KERNEL MODE</text>
  <rect x="264" y="86" width="332" height="76" rx="8" fill="#1a1006" stroke="#f0883e" stroke-width="1.2"/>
  <text x="430" y="114" text-anchor="middle" fill="#f0883e" font-size="13" font-weight="600">Minifilter Driver  (Driver.c)</text>
  <text x="430" y="133" text-anchor="middle" fill="#b36a28" font-size="10">Kernel-level file I/O interception · IRP_MJ_CREATE / WRITE / RENAME</text>
  <text x="430" y="150" text-anchor="middle" fill="#b36a28" font-size="10">Threshold counters · ransomware ext / note detection · block I/O</text>
  <rect x="28" y="196" width="804" height="148" rx="10" fill="#161b22" stroke="#58a6ff" stroke-width="1" stroke-dasharray="6 4"/>
  <text x="44" y="216" fill="#58a6ff" font-size="10" font-weight="600" letter-spacing="1">USER MODE — SENSORS</text>
  <rect x="44" y="226" width="246" height="100" rx="8" fill="#0c1929" stroke="#58a6ff" stroke-width="1.2"/>
  <text x="167" y="252" text-anchor="middle" fill="#58a6ff" font-size="13" font-weight="600">ETW Process Monitor</text>
  <text x="167" y="271" text-anchor="middle" fill="#3b82c4" font-size="11">Process create / terminate</text>
  <text x="167" y="288" text-anchor="middle" fill="#3b82c4" font-size="11">Full command-line capture</text>
  <text x="167" y="305" text-anchor="middle" fill="#3b82c4" font-size="11">Malicious tool name blocklist</text>
  <rect x="570" y="226" width="246" height="100" rx="8" fill="#0c1929" stroke="#58a6ff" stroke-width="1.2"/>
  <text x="693" y="252" text-anchor="middle" fill="#58a6ff" font-size="13" font-weight="600">Canary Agent  (C#)</text>
  <text x="693" y="271" text-anchor="middle" fill="#3b82c4" font-size="11">Decoy file lifecycle management</text>
  <text x="693" y="288" text-anchor="middle" fill="#3b82c4" font-size="11">Hash / size / timestamp / rename checks</text>
  <text x="693" y="305" text-anchor="middle" fill="#3b82c4" font-size="11">Sends via CanaryAgentPipe (Named Pipe)</text>
  <line x1="430" y1="162" x2="430" y2="372" stroke="#f0883e" stroke-width="1.2" stroke-dasharray="4 3" marker-end="url(#arr-orange)"/>
  <rect x="310" y="192" width="240" height="18" rx="4" fill="#0d1117"/>
  <text x="430" y="205" text-anchor="middle" fill="#f0883e" font-size="10">FltSendMessage  (binary struct)</text>
  <line x1="167" y1="326" x2="265" y2="418" stroke="#58a6ff" stroke-width="1.2" stroke-dasharray="4 3" marker-end="url(#arr-blue)"/>
  <text x="168" y="368" fill="#58a6ff" font-size="10" text-anchor="middle">in-process event</text>
  <line x1="693" y1="326" x2="595" y2="418" stroke="#58a6ff" stroke-width="1.2" stroke-dasharray="4 3" marker-end="url(#arr-blue)"/>
  <text x="692" y="368" fill="#58a6ff" font-size="10" text-anchor="middle">Named Pipe (JSON)</text>
  <rect x="28" y="362" width="804" height="196" rx="10" fill="#161b22" stroke="#3fb950" stroke-width="1" stroke-dasharray="6 4"/>
  <text x="44" y="382" fill="#3fb950" font-size="10" font-weight="600" letter-spacing="1">USER MODE — RESPONSE AGENT  (C#)</text>
  <rect x="44" y="392" width="186" height="76" rx="7" fill="#0d1f16" stroke="#3fb950" stroke-width="1"/>
  <text x="137" y="416" text-anchor="middle" fill="#3fb950" font-size="12" font-weight="600">Rule Engine</text>
  <text x="137" y="434" text-anchor="middle" fill="#2e7d45" font-size="10">rules.json pattern match</text>
  <text x="137" y="450" text-anchor="middle" fill="#2e7d45" font-size="10">Regex · blocklist · thresholds</text>
  <rect x="246" y="392" width="186" height="76" rx="7" fill="#0d1f16" stroke="#3fb950" stroke-width="1"/>
  <text x="339" y="416" text-anchor="middle" fill="#3fb950" font-size="12" font-weight="600">Correlator</text>
  <text x="339" y="434" text-anchor="middle" fill="#2e7d45" font-size="10">Cross-source event linking</text>
  <text x="339" y="450" text-anchor="middle" fill="#2e7d45" font-size="10">Context enrichment</text>
  <rect x="448" y="392" width="186" height="76" rx="7" fill="#0d1f16" stroke="#3fb950" stroke-width="1"/>
  <text x="541" y="413" text-anchor="middle" fill="#3fb950" font-size="12" font-weight="600">Severity Selector</text>
  <text x="541" y="430" text-anchor="middle" fill="#2e7d45" font-size="10">Low / Medium → alert only</text>
  <text x="541" y="446" text-anchor="middle" fill="#2e7d45" font-size="10">High / Critical → kill process</text>
  <rect x="650" y="392" width="166" height="76" rx="7" fill="#0d1f16" stroke="#3fb950" stroke-width="1"/>
  <text x="733" y="416" text-anchor="middle" fill="#3fb950" font-size="12" font-weight="600">Local Alert Log</text>
  <text x="733" y="434" text-anchor="middle" fill="#2e7d45" font-size="10">Structured .txt on disk</text>
  <text x="733" y="450" text-anchor="middle" fill="#2e7d45" font-size="10">Offline fallback</text>
  <line x1="230" y1="430" x2="244" y2="430" stroke="#3fb950" stroke-width="1" marker-end="url(#arr-green)"/>
  <line x1="432" y1="430" x2="446" y2="430" stroke="#3fb950" stroke-width="1" marker-end="url(#arr-green)"/>
  <line x1="634" y1="430" x2="648" y2="430" stroke="#3fb950" stroke-width="1" marker-end="url(#arr-green)"/>
  <rect x="44" y="484" width="590" height="60" rx="7" fill="#0d1f16" stroke="#3fb950" stroke-width="0.8" stroke-dasharray="4 3"/>
  <text x="339" y="506" text-anchor="middle" fill="#3fb950" font-size="11" font-weight="600">Process Terminator  ·  KillProcess(pid)</text>
  <text x="339" y="524" text-anchor="middle" fill="#2e7d45" font-size="10">Invoked by Severity Selector on High / Critical alerts · terminates offending process tree</text>
  <line x1="541" y1="468" x2="541" y2="484" stroke="#3fb950" stroke-width="1" stroke-dasharray="3 2" marker-end="url(#arr-green)"/>
  <line x1="430" y1="558" x2="430" y2="608" stroke="#a371f7" stroke-width="1.5" marker-end="url(#arr-purple)"/>
  <rect x="290" y="568" width="280" height="18" rx="4" fill="#0d1117"/>
  <text x="430" y="581" text-anchor="middle" fill="#a371f7" font-size="10">HTTPS POST /api/events  (JSON alert)</text>
  <rect x="28" y="598" width="804" height="180" rx="10" fill="#161b22" stroke="#a371f7" stroke-width="1" stroke-dasharray="6 4"/>
  <text x="44" y="618" fill="#a371f7" font-size="10" font-weight="600" letter-spacing="1">WEB DASHBOARD</text>
  <rect x="44" y="628" width="230" height="132" rx="8" fill="#1a0f29" stroke="#a371f7" stroke-width="1.2"/>
  <text x="159" y="652" text-anchor="middle" fill="#a371f7" font-size="13" font-weight="600">Spring Boot Backend  (Java)</text>
  <text x="159" y="671" text-anchor="middle" fill="#7555b8" font-size="10">REST API · JWT auth</text>
  <text x="159" y="687" text-anchor="middle" fill="#7555b8" font-size="10">Event normalisation · live buffer</text>
  <text x="159" y="703" text-anchor="middle" fill="#7555b8" font-size="10">GET /api/dashboard  /alerts  /events</text>
  <text x="159" y="719" text-anchor="middle" fill="#7555b8" font-size="10">GET /api/reports  /detection-activity</text>
  <text x="159" y="735" text-anchor="middle" fill="#7555b8" font-size="10">POST /api/auth/login  /signup</text>
  <rect x="290" y="628" width="190" height="132" rx="8" fill="#1a0f29" stroke="#a371f7" stroke-width="1.2"/>
  <text x="385" y="652" text-anchor="middle" fill="#a371f7" font-size="13" font-weight="600">MongoDB</text>
  <text x="385" y="671" text-anchor="middle" fill="#7555b8" font-size="10">siem_dashboard DB</text>
  <text x="385" y="687" text-anchor="middle" fill="#7555b8" font-size="10">security_events collection</text>
  <text x="385" y="703" text-anchor="middle" fill="#7555b8" font-size="10">users collection</text>
  <text x="385" y="719" text-anchor="middle" fill="#7555b8" font-size="10">Persistent alert history</text>
  <text x="385" y="735" text-anchor="middle" fill="#7555b8" font-size="10">Historical reports</text>
  <rect x="496" y="628" width="320" height="132" rx="8" fill="#1a0f29" stroke="#a371f7" stroke-width="1.2"/>
  <text x="656" y="652" text-anchor="middle" fill="#a371f7" font-size="13" font-weight="600">React Frontend  (Vite + Tailwind)</text>
  <text x="656" y="671" text-anchor="middle" fill="#7555b8" font-size="10">Dashboard  ·  Alerts  ·  Detection Activity</text>
  <text x="656" y="687" text-anchor="middle" fill="#7555b8" font-size="10">Reports  ·  Settings  ·  Auth</text>
  <text x="656" y="703" text-anchor="middle" fill="#7555b8" font-size="10">Live polling — dashboard / alerts pages</text>
  <text x="656" y="719" text-anchor="middle" fill="#7555b8" font-size="10">Historical query — reports page</text>
  <text x="656" y="735" text-anchor="middle" fill="#7555b8" font-size="10">JWT stored client-side · React Router DOM</text>
  <line x1="274" y1="694" x2="288" y2="694" stroke="#a371f7" stroke-width="1" marker-end="url(#arr-purple)"/>
  <line x1="288" y1="706" x2="274" y2="706" stroke="#a371f7" stroke-width="1" marker-end="url(#arr-purple)"/>
  <line x1="480" y1="694" x2="494" y2="694" stroke="#a371f7" stroke-width="1" marker-end="url(#arr-purple)"/>
  <text x="487" y="688" text-anchor="middle" fill="#a371f7" font-size="9">GET</text>
  <defs>
    <marker id="arr-orange" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
      <path d="M2 1L8 5L2 9" fill="none" stroke="#f0883e" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
    </marker>
    <marker id="arr-blue" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
      <path d="M2 1L8 5L2 9" fill="none" stroke="#58a6ff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
    </marker>
    <marker id="arr-green" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
      <path d="M2 1L8 5L2 9" fill="none" stroke="#3fb950" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
    </marker>
    <marker id="arr-purple" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
      <path d="M2 1L8 5L2 9" fill="none" stroke="#a371f7" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
    </marker>
  </defs>
</svg>

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

# 🚀 Installation & Deployment Guide

## 📋 Requirements 
Before running the project, you should have :

* Windows 10 / Windows 11
* Visual Studio 2022
* Windows Driver Kit (WDK)
* Windows SDK
* Java JDK
* Apache Maven
* Node.js & npm
* MongoDB

---

# ⚙️ Installation & Deployment Guide

⚠️ IMPORTANT:

RansomwareShield interacts with low-level Windows kernel components and should ONLY be deployed inside isolated virtual machine environments.

Recommended:

* VMware
* VirtualBox

Do NOT test ransomware samples on a host machine.
---

# 📚 Manual Usage

### 🖥️ Step 1 — Setup Virtual Machine
* Install Windows 10/11 inside a VM
* Update Windows
* Enable VM snapshots
* Install all required software

### 🛠️ Step 2 — Install Visual Studio & WDK

Install:

* Visual Studio Community 2022
* Windows Driver Kit (WDK)
* Windows SDK
* x64/x86 C++ Spectre Libraries

Enable workloads:

* Desktop Development with C++
* Windows Driver Development
  
### 🔐 Step 3 — Enable Test Signing Mode

Open PowerShell as Administrator:

```powershell
bcdedit /set testsigning on
```
Reboot the virtual machine.

After reboot:
* Verify “Test Mode” appears on the desktop.
  
### ⚙️ Step 4 — Build the Minifilter Driver
* Open Visual Studio as Administrator
* Create:
   * Kernel Mode Driver, Empty (KMDF)
* Configure:
  * Project Name: MiniFilter
  * Platform: x64
* Add:
  * Driver.c
* Copy the Minifilter source code into Driver.c
  
**Configure Driver Dependencies**

Navigate to:

```text
Project → Properties → Linker → Input
```

Append:

```text
fltMgr.lib
```

Enable:

* Driver Signing → Test Sign

Build the solution.

Generated driver:

```text
\MiniFilter\x64\Debug\MiniFilter.sys
```

### 📦 Step 5 — Register the Driver

Open CMD as Administrator:

```cmd 
copy \x64\Debug\MiniFilter.sys C:\Windows\System32\drivers\
```

Register the service:

```cmd 
reg add "HKLM\SYSTEM\CurrentControlSet\Services\MiniFilter" /v Type /t REG_DWORD /d 2 /f
```
(Additional registry configuration may be required.)

### 🌐 Step 6 — Setup Dashboard

Install:

* Java JDK
* Apache Maven
* Node.js
* MongoDB

Add to Environment Variables:

```cmd
jdk-26.0.1\bin
apache-maven-3.9.14\bin
```

Move:

* sim-dashboard
* backend-java

to Desktop.

Install frontend dependencies:
```cmd
cd Desktop\sim-dashboard
npm install
```
Verify installation:
```cmd
node -v
npm -v
```
### 🗄️ Step 7 — Configure MongoDB
* Open MongoDB Compass
* Create a new connection:
   * siem_dashboard
### 🐤 Step 8 — Build Canary Agent
* Open CanaryAgent in Visual Studio
* Build Solution
* Run in Debug Mode
### 🚨 Step 9 — Build Response Agent
* Open ResponseAgent
* Build Solution
* Run in Debug Mode
### 🔍 Step 10 — Run ETW Monitor
* Open the ETW monitoring project
* Build the solution
* Run as Administrator
### 🧪 Step 11 — Execute Ransomware Testing
⚠️ VM ONLY
* Execute ransomware sample
* Observe:
  * Canary alerts
  * Minifilter detections
  * ETW detections
  * Process termination
  * Dashboard logs
### 📊 Step 12 — Monitor Dashboard

Open the dashboard in browser and monitor:
 * Alerts
 * Severity Levels
 * File Activity
 * Process Activity
 * Response Actions
 * Detection Logs

---

## 🐤 Canary Files - IMPORTANT

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

# 📈 Future Improvements

* 🌐 Network-based ransomware detection
* 🔐 Encryption level detection
* 📁 Suspecious API detection
* ☁️ Cloud dashboard deployment
* 🤖 Machine learning integration
* 🔔 Email/SMS alerting
* 🧩 SIEM integration
* 🐧 Linux support



---

<div align="center">

## 🛡️ Detect Early. Respond Fast. Stay Protected.

</div>
