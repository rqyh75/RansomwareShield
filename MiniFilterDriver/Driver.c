
#include <fltKernel.h> // Main header for File System MiniFilter drivers (Filter Manager API)
#include <ntstrsafe.h> // Safe string handling functions for kernel mode
#include <ntstatus.h>  // for STATUS_* codes

// ================= GLOBALS =================
// 
// Global variable to store filter handle returned by FltRegisterFilter
PFLT_FILTER gFilterHandle = NULL;
// Time window: 10 seconds in 100-nanosecond intervals (LARGE_INTEGER / KeQuerySystemTime units)
#define WRITE_WARN_THRESHOLD   10    // Log warning 3 write/sec sus
#define WRITE_BLOCK_THRESHOLD  50   // Block the process 20 writes/sec block
#define TIME_WINDOW_SECONDS 5
#define TIME_WINDOW_100NS   ((LONGLONG)TIME_WINDOW_SECONDS * 10000000LL)
// Track multiple processes simultaneously
#define MAX_TRACKED_PROCESSES 32
// Extensions to block (from rule R3001, R4001)
#define MAX_RENAME_TRACKERS 32
#define RENAME_THRESHOLD 30
#define RENAME_WINDOW_SECONDS 5
#define RENAME_WINDOW_100NS ((LONGLONG)RENAME_WINDOW_SECONDS * 10000000LL)// Extensions to block (from rule R3001, R4001)
// Name of the communication port used by user-mode (must match FilterConnectCommunicationPort)
#define MINIFILTER_PORT_NAME L"\\MiniRansomPort"

// ================= MINIFILTER → USER COMMUNICATION =================
// gServerPort: represents the server-side port created by the minifilter
// gClientPort: represents the connection to the user-mode response agent
// These are used with FltCreateCommunicationPort and FltSendMessage
//for the sending data globals
PFLT_PORT gServerPort = NULL;
PFLT_PORT gClientPort = NULL;
#define MF_MAX_PROCESS_NAME 16
#define MF_MAX_ACTION_NAME    64
#define MF_MAX_RESPONSE_NAME  16
#define MF_MAX_PATH           520

//structure tracks how many times a process writes to files.
typedef struct _PROCESS_TRACKER {
    HANDLE Pid;  //Process ID performing writes
    ULONG  WriteCount;  // # OF write operations detected
    LONGLONG  WindowStart;   // Timestamp of first write in current window (100ns units)
    BOOLEAN   Active;
} PROCESS_TRACKER;

typedef struct _MINIFILTER_NOTIFICATION {
    ULONG ProcessId;
    WCHAR ProcessName[MF_MAX_PROCESS_NAME];
    WCHAR Action[MF_MAX_ACTION_NAME];
    WCHAR Response[MF_MAX_RESPONSE_NAME];
    WCHAR TargetPath[MF_MAX_PATH];
} MINIFILTER_NOTIFICATION, * PMINIFILTER_NOTIFICATION;

NTKERNELAPI
PCHAR
NTAPI
PsGetProcessImageFileName(
    PEPROCESS Process
);


// Structure for mass rename tracking (per process)
typedef struct _RENAME_TRACKER {
    HANDLE Pid;
    ULONG  RenameCount;
    LONGLONG WindowStart;
    BOOLEAN Active;
} RENAME_TRACKER;

//global variable stored in kernel memory.

PROCESS_TRACKER gTrackers[MAX_TRACKED_PROCESSES] = { 0 }; //Stores = Current suspicious process + How many writes it performed

// Global arrays for rename tracking and spinlock
RENAME_TRACKER gRenameTrackers[MAX_RENAME_TRACKERS] = { 0 };
KSPIN_LOCK gRenameLock;

// Spinlock to protect gTrackers from concurrent access across CPUs
KSPIN_LOCK gTrackerLock;

// ================= WHITELIST =================
// System processes that should never be blocked
static const WCHAR* gWhitelistedProcesses[] = {
    // === Core Windows System ===
     L"System",
     L"Registry",        // Kernel registry process
     L"smss.exe",        // Session Manager
     L"csrss.exe",       // Client Server Runtime
     L"wininit.exe",     // Windows Initialization
     L"winlogon.exe",    // Windows Logon
     L"services.exe",    // Service Control Manager
     L"lsass.exe",       // Local Security Authority
     L"svchost.exe",     // Service Host

     // === Windows Security ===
     L"MsMpEng.exe",     // Windows Defender
     L"SecurityHealth",  // Windows Security
     L"SgrmBroker.exe",  // System Guard

     // === Windows Update ===
     L"MoUsoCoreWorke",  // Update Orchestrator (15 char limit cuts here)
     L"WaaSMedic.exe",   // Windows Update Medic
     L"UsoClient.exe",   // Update Session Orchestrator
     L"wuauclt.exe",     // Windows Update Client
     L"TiWorker.exe",    // Windows Modules Installer Worker
     L"TrustedInstall",  // TrustedInstaller.exe (truncated)

     // === Windows Search & Indexing ===
     L"SearchIndexer.",  // Windows Search Indexer (truncated)
     L"SearchProtocol",  // Search Protocol Host

     // === .NET & Runtime ===
     L"mscorsvw.exe",    // .NET Runtime Optimizer
     L"ngen.exe",        // .NET Native Image Generator

     // === Error Reporting ===
     L"WerFault.exe",    // Windows Error Reporting
     L"WerFaultSecur",   // WerFaultSecure.exe

     // === Cloud & Sync ===
     L"OneDrive.exe",    // OneDrive Sync

     // === Browsers (write heavily for cache/logs) ===
     L"msedge.exe",      // Microsoft Edge
     L"msedgewebview2",  // Edge WebView2
     L"chrome.exe",      // Google Chrome
     L"firefox.exe",     // Firefox
     L"explorer.exe",    // Firefox

     // === Visual Studio / Build Tools ===
     L"devenv.exe",      // Visual Studio IDE
     L"cl.exe",          // MSVC Compiler
     L"link.exe",        // MSVC Linker
     L"MSBuild.exe",     // MSBuild
     L"vcpkgsrv.exe",    // Vcpkg Service
     L"msbuild.exe",     // MSBuild (lowercase)

     // === System Maintenance ===
     L"provtool.exe",    // Provisioning Tool
     L"sppsvc.exe",      // Software Protection
     L"SppExtComObj.",   // KMS Activation

     // === Web Dashboard ===
     L"dllhost.exe",    // Provisioning Tool
     L"mongod.exe",      // Software Protection
     L"MongoDBCompass",   // KMS Activation

    NULL              // sentinel

};

// ================= RANSOMWARE INDICATORS =================
// Known ransomware extensions (R3001)
static const WCHAR* gRansomwareExtensions[] = {
    L".abcd", L".lockbit", L".lockbitv2", L".mkp",
    L".interlock", L".1nt3rlock", L".medusa",
    NULL
};

// Known ransom note filenames (R3002)
static const WCHAR* gRansomNoteNames[] = {
    L"akira_readme.txt",
    L"How To Restore Your Files.txt",
    L"!README!.txt",
    L"!!!READ_ME_MEDUSA!!!.txt",
    L"!!!OPEN_ME!!!.txt",
    NULL
};

// Ransomware detection helpers
BOOLEAN IsRansomwareExtension(PUNICODE_STRING FileName);
BOOLEAN IsRansomNote(PUNICODE_STRING FileName);
int FindRenameTrackerSlot(HANDLE pid);
int GetFreeRenameSlot(void);

BOOLEAN IsSuspiciousPath(PUNICODE_STRING FullName);
BOOLEAN ShouldBlockFile(PUNICODE_STRING FullName);


BOOLEAN IsWhitelistedProcess()
{
    PEPROCESS process = PsGetCurrentProcess();

    // PsGetProcessImageFileName returns a plain CHAR*, max 15 chars
    PCHAR name = (PCHAR)PsGetProcessImageFileName(process);

    if (!name) return FALSE;

    for (int i = 0; gWhitelistedProcesses[i] != NULL; i++) {
        // Simple ASCII compare (image name is always ASCII here)
        UNICODE_STRING uni;
        WCHAR wideName[16] = { 0 };
        for (int j = 0; j < 15 && name[j]; j++)
            wideName[j] = (WCHAR)name[j];

        RtlInitUnicodeString(&uni, gWhitelistedProcesses[i]);
        UNICODE_STRING check;
        RtlInitUnicodeString(&check, wideName);

        if (RtlEqualUnicodeString(&uni, &check, TRUE))  // TRUE = case-insensitive
            return TRUE;
    }
    return FALSE;
}
// ================= TRACKER HELPERS =================

// Find existing tracker slot for PID, or return -1
int FindTrackerSlot(HANDLE pid)
{
    for (int i = 0; i < MAX_TRACKED_PROCESSES; i++) {
        if (gTrackers[i].Active && gTrackers[i].Pid == pid)
            return i;
    }
    return -1;
}

// Find a free slot, evicting the oldest if full
int GetFreeSlot()
{
    for (int i = 0; i < MAX_TRACKED_PROCESSES; i++) {
        if (!gTrackers[i].Active)
            return i;
    }
    // All slots full → evict slot 0 (simple strategy)
    return 0;
}

// ================= FUNCTION DECLARATIONS =================
//
// unload routine
//FLT_FILTER_UNLOAD_FLAGS tells why the driver is being unloaded.
NTSTATUS SimpleMiniFilterUnload(FLT_FILTER_UNLOAD_FLAGS Flags);

// Pre-operation callback prototype (runs BEFORE file operation happens)
FLT_PREOP_CALLBACK_STATUS
//function for intercept file create/open operations.
PreCreateCallback(
    PFLT_CALLBACK_DATA Data,   // Info about the I/O request
    PCFLT_RELATED_OBJECTS FltObjects, // Related objects (file, instance, volume)
    PVOID* CompletionContext   // PVOID *CompletionContext in PreOperation to store a pointer to the data you want to pass to the PostOperation
);
//function for intercept file write operations
FLT_PREOP_CALLBACK_STATUS
PreWriteCallback(
    PFLT_CALLBACK_DATA Data,
    PCFLT_RELATED_OBJECTS FltObjects,
    PVOID* CompletionContext
);

FLT_PREOP_CALLBACK_STATUS 
PreRenameCallback(
    PFLT_CALLBACK_DATA Data, 
    PCFLT_RELATED_OBJECTS FltObjects, 
    PVOID* CompletionContext
);
//declarition for custom functions 
BOOLEAN  //return true if suspicious
IsSuspiciousPath(PUNICODE_STRING FullName);
BOOLEAN
ShouldBlockFile(PUNICODE_STRING FullName);
//for convert fun
VOID GetCurrentProcessNameWide(_Out_writes_(MF_MAX_PROCESS_NAME) WCHAR* outName);

NTSTATUS
MiniConnectNotify(
    PFLT_PORT ClientPort,
    PVOID ServerPortCookie,
    PVOID ConnectionContext,
    ULONG SizeOfContext,
    PVOID* ConnectionPortCookie
);

VOID
MiniDisconnectNotify(
    PVOID ConnectionCookie
);

VOID
SendMinifilterNotification(
    HANDLE Pid,
    PCWSTR ProcessName,
    PCWSTR Action,
    PCWSTR Response,
    PUNICODE_STRING TargetPath
);

// ================= CALLBACK TABLE =================
//
// Operation callback table tells fltMgr which I/O operation i want to intercept
const FLT_OPERATION_REGISTRATION Callbacks[] = {
    { IRP_MJ_CREATE,              // Intercept file create/open
      0,                          // Flags: 0 = no filtering rules → intercept everything
      PreCreateCallback,          // Pre-operation callback
      NULL },                     // Post-operation callback

    { IRP_MJ_WRITE, //write operations 
      0,
      PreWriteCallback,
      NULL },

    { IRP_MJ_SET_INFORMATION,
    0, 
    PreRenameCallback, 
    NULL },

    { IRP_MJ_OPERATION_END }      // Must terminate with this && marks end of table
};


// ================= FILTER REGISTRATION =================
//
// Filter registration with filter manger
const FLT_REGISTRATION FilterRegistration = {
    sizeof(FLT_REGISTRATION), // Structure size
    FLT_REGISTRATION_VERSION, //version
    0,                        // flags (none)
    NULL,               // Contexts
    Callbacks,          // Operation callbacks table
    SimpleMiniFilterUnload // unload routine
};


// ================= UNLOAD ROUTINE =================
// 
// Unload callback 
//it runs when fltmc ,  system shutdown or driver removel
NTSTATUS
SimpleMiniFilterUnload(
    FLT_FILTER_UNLOAD_FLAGS Flags
)
{   //Prevents compiler warning if we don’t use Flags.
    UNREFERENCED_PARAMETER(Flags);
    if (gServerPort) {
        FltCloseCommunicationPort(gServerPort);
        gServerPort = NULL;
    }

    if (gClientPort) {
        FltCloseClientPort(gFilterHandle, &gClientPort);
        gClientPort = NULL;
    }


    if (gFilterHandle) {
        FltUnregisterFilter(gFilterHandle); //remove filter from system
        gFilterHandle = NULL;               // reset global handle
    }
    //debug msg
    DbgPrintEx(DPFLTR_IHVDRIVER_ID,
        DPFLTR_INFO_LEVEL,
        "[SMF] SimpleMiniFilter unloaded\n");

    //unload successful
    return STATUS_SUCCESS;
}


// ================= PRE-CREATE CALLBACK =================
// 
// PreCreate callback: triggered on every file open/create
FLT_PREOP_CALLBACK_STATUS
PreCreateCallback(
    PFLT_CALLBACK_DATA Data,
    PCFLT_RELATED_OBJECTS FltObjects,
    PVOID* CompletionContext
)
{
    UNREFERENCED_PARAMETER(CompletionContext);

    //Structure that will store file name info.
    PFLT_FILE_NAME_INFORMATION nameInfo = NULL;

    //If no file object → do nothing.
    //it prevent crashes 
    if (!FltObjects->FileObject)
        return FLT_PREOP_SUCCESS_NO_CALLBACK;

    //get full file path from request
    if (NT_SUCCESS(FltGetFileNameInformation(
        Data, //pointer to store everything abt the operation
        FLT_FILE_NAME_NORMALIZED | //full clean path
        FLT_FILE_NAME_QUERY_ALWAYS_ALLOW_CACHE_LOOKUP, // use cache for performence
        &nameInfo))) { //give the addres of the pointer nameInfo to store the full path

        //splits file into compontes extentio , dir ...
        FltParseFileNameInformation(nameInfo);

        //get process id 
        HANDLE pid = PsGetCurrentProcessId();

        WCHAR processNameW[MF_MAX_PROCESS_NAME];
        GetCurrentProcessNameWide(processNameW);

            // Detection logic
        if (IsSuspiciousPath(&nameInfo->Name)) {

            PCHAR processName = PsGetProcessImageFileName(PsGetCurrentProcess());
            DbgPrintEx(DPFLTR_IHVDRIVER_ID,
                DPFLTR_ERROR_LEVEL,
                "[EDR ALERT] Suspicious file access detected! PID: %llu (%s) File: %wZ\n",
                (ULONGLONG)pid, processName ? processName : "unknown", &nameInfo->Name);
            
            SendMinifilterNotification(
                pid,
                processNameW,
                L"create_exe_in_temp_or_suspicious_open",
                L"warn",
                &nameInfo->Name
            );
        
        }

        // Blocking logic
        if (ShouldBlockFile(&nameInfo->Name)) {
            //If file is secret.txt → block it.
            PCHAR processName = PsGetProcessImageFileName(PsGetCurrentProcess());
            DbgPrintEx(DPFLTR_IHVDRIVER_ID,
                DPFLTR_ERROR_LEVEL,
                "[EDR BLOCK] Blocking access to secret.txt\nPID: %llu (%s) File: %wZ\n",
                (ULONGLONG)pid, processName ? processName : "unknown", &nameInfo->Name);

            SendMinifilterNotification(
                pid,
                processNameW,
                L"blocked_file_access",
                L"block",
                &nameInfo->Name
            );

            //deny access
            Data->IoStatus.Status = STATUS_ACCESS_DENIED;
            Data->IoStatus.Information = 0;

            //release memory allocated by filter 
            FltReleaseFileNameInformation(nameInfo);

            //stop operation and file not opended 
            return FLT_PREOP_COMPLETE;
        }

        // Ransom note detection (R3002) – block creation of known ransom note files
        if (IsRansomNote(&nameInfo->FinalComponent)) {
            PCHAR processName = PsGetProcessImageFileName(PsGetCurrentProcess());
            DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
                "[RANSOMWARE DETECTED] PID %llu (%s) creating ransom note %wZ\n",
                (ULONGLONG)pid, processName ? processName : "unknown", &nameInfo->Name);
            
            SendMinifilterNotification(
                pid,
                processNameW,
                L"ransom_note_create",
                L"block",
                &nameInfo->Name
            );
            
            Data->IoStatus.Status = STATUS_ACCESS_DENIED;
            Data->IoStatus.Information = 0;
            FltReleaseFileNameInformation(nameInfo);
            return FLT_PREOP_COMPLETE;
        }

        FltReleaseFileNameInformation(nameInfo);
    }

    //Allow file operation to continue normally.
    return FLT_PREOP_SUCCESS_NO_CALLBACK;
}


// ================= PRE-WRITE CALLBACK =================
FLT_PREOP_CALLBACK_STATUS
PreWriteCallback(
    PFLT_CALLBACK_DATA Data,
    PCFLT_RELATED_OBJECTS FltObjects,
    PVOID* CompletionContext
)
{

  /*  DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, "[DEBUG] Write callback entered\n");*/
    UNREFERENCED_PARAMETER(CompletionContext);

    if (!FltObjects->FileObject)
        return FLT_PREOP_SUCCESS_NO_CALLBACK;

    // Skip whitelisted system processes immediately
   if (IsWhitelistedProcess())
        return FLT_PREOP_SUCCESS_NO_CALLBACK;


    HANDLE pid = PsGetCurrentProcessId();
    LARGE_INTEGER now;
    KeQuerySystemTime(&now);

    KIRQL oldIrql;
    KeAcquireSpinLock(&gTrackerLock, &oldIrql);
    int slot = FindTrackerSlot(pid);

    if (slot == -1) {
        // First time seeing this PID → allocate a slot
        slot = GetFreeSlot();
        gTrackers[slot].Pid = pid;
        gTrackers[slot].WriteCount = 0;
        gTrackers[slot].WindowStart = now.QuadPart;
        gTrackers[slot].Active = TRUE;
    }

    // Check if the time window has expired → reset counter
    if ((now.QuadPart - gTrackers[slot].WindowStart) > TIME_WINDOW_100NS) {

        gTrackers[slot].WriteCount = 0;
        gTrackers[slot].WindowStart = now.QuadPart;   // start a fresh window
    }

    gTrackers[slot].WriteCount++;
    ULONG currentCount = gTrackers[slot].WriteCount;

    KeReleaseSpinLock(&gTrackerLock, oldIrql);


    // Get process name ONCE here — used in both branches below
    PCHAR processName = PsGetProcessImageFileName(PsGetCurrentProcess());
    if (!processName) processName = "unknown";

    WCHAR processNameW[MF_MAX_PROCESS_NAME];
    GetCurrentProcessNameWide(processNameW);

    PFLT_FILE_NAME_INFORMATION nameInfo = NULL;
    UNICODE_STRING emptyPath;
    RtlInitUnicodeString(&emptyPath, L"");

    if (NT_SUCCESS(FltGetFileNameInformation(
        Data,
        FLT_FILE_NAME_NORMALIZED | FLT_FILE_NAME_QUERY_ALWAYS_ALLOW_CACHE_LOOKUP,
        &nameInfo))) {
        FltParseFileNameInformation(nameInfo);
    }

    // ---- Decision ----
    // ---- Tier 1: Warn ----
    if (currentCount == WRITE_WARN_THRESHOLD) {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID,
            DPFLTR_WARNING_LEVEL,
            "[WARN] PID %llu (%s) hit %lu writes in %d sec - watching\n",
            (ULONGLONG)pid, processName, currentCount, TIME_WINDOW_SECONDS);
        
        SendMinifilterNotification(
            pid,
            processNameW,
            L"too_many_writes",
            L"warn",
            nameInfo ? &nameInfo->Name : &emptyPath
        );

        if (nameInfo) {
            FltReleaseFileNameInformation(nameInfo);
        }

        // Allow — just logging, not blocking yet
        return FLT_PREOP_SUCCESS_NO_CALLBACK;
    }

    // ---- Tier 2: Block ----
    if (currentCount > WRITE_BLOCK_THRESHOLD) {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID,
            DPFLTR_ERROR_LEVEL,
            "[RANSOMWARE DETECTED] PID %llu (%s) wrote %lu times in %d sec. Blocking.\n",
            (ULONGLONG)pid, processName, currentCount, TIME_WINDOW_SECONDS);

        // Reset counter so we keep blocking each write going forward
        // but don't spam the log on every single IRP after the first block
        KeAcquireSpinLock(&gTrackerLock, &oldIrql);
        gTrackers[slot].WriteCount = WRITE_BLOCK_THRESHOLD + 1; // stay above threshold
        gTrackers[slot].WindowStart = now.QuadPart;
        KeReleaseSpinLock(&gTrackerLock, oldIrql);

        SendMinifilterNotification(
            pid,
            processNameW,
            L"too_many_writes",
            L"block",
            nameInfo ? &nameInfo->Name : &emptyPath
        );

        if (nameInfo) {
            FltReleaseFileNameInformation(nameInfo);
        }

        Data->IoStatus.Status = STATUS_ACCESS_DENIED;
        Data->IoStatus.Information = 0;
        return FLT_PREOP_COMPLETE;
    }

    if (nameInfo) {
        FltReleaseFileNameInformation(nameInfo);
    }

    return FLT_PREOP_SUCCESS_NO_CALLBACK;
}



// ================= PRE-RENAME CALLBACK =================
// Intercepts file rename operations (IRP_MJ_SET_INFORMATION)
FLT_PREOP_CALLBACK_STATUS
PreRenameCallback(
    PFLT_CALLBACK_DATA Data,
    PCFLT_RELATED_OBJECTS FltObjects,
    PVOID* CompletionContext
)
{
    UNREFERENCED_PARAMETER(CompletionContext);
    UNREFERENCED_PARAMETER(FltObjects);

    // Only care about rename operations
    if (Data->Iopb->Parameters.SetFileInformation.FileInformationClass != FileRenameInformation)
        return FLT_PREOP_SUCCESS_NO_CALLBACK;

    PFILE_RENAME_INFORMATION renameInfo = (PFILE_RENAME_INFORMATION)Data->Iopb->Parameters.SetFileInformation.InfoBuffer;
    if (!renameInfo || !renameInfo->FileNameLength)
        return FLT_PREOP_SUCCESS_NO_CALLBACK;

    // Extract the new file name (relative path)
    UNICODE_STRING newFileName;
    newFileName.Buffer = renameInfo->FileName;
    newFileName.Length = (USHORT)renameInfo->FileNameLength;
    newFileName.MaximumLength = newFileName.Length;

    // Check if the new name has a ransomware extension
    if (IsRansomwareExtension(&newFileName)) {
        HANDLE pid = PsGetCurrentProcessId();
        PCHAR processName = PsGetProcessImageFileName(PsGetCurrentProcess());
        if (!processName) processName = "unknown";

        WCHAR processNameW[MF_MAX_PROCESS_NAME];
        GetCurrentProcessNameWide(processNameW);

        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[RANSOMWARE DETECTED] PID %llu (%s) renaming to malicious extension %wZ\n",
            (ULONGLONG)pid, processName, &newFileName);

        SendMinifilterNotification(
            pid,
            processNameW,
            L"rename_to_ransomware_extension",
            L"block",
            &newFileName
        );

        // Block the rename operation
        Data->IoStatus.Status = STATUS_ACCESS_DENIED;
        Data->IoStatus.Information = 0;
        return FLT_PREOP_COMPLETE;
    }

    // TODO: Optional mass rename detection (R4001) – track rename counts per process
    // using gRenameTrackers and the helpers below.

    return FLT_PREOP_SUCCESS_NO_CALLBACK;
}

//=================Sending function======================


// ================= USER-MODE CONNECTION CALLBACK =================
// Called when the response agent connects using FilterConnectCommunicationPort
// Stores the client port so we can send messages using FltSendMessage
NTSTATUS
MiniConnectNotify(
    PFLT_PORT ClientPort,
    PVOID ServerPortCookie,
    PVOID ConnectionContext,
    ULONG SizeOfContext,
    PVOID* ConnectionPortCookie
)
{
    UNREFERENCED_PARAMETER(ServerPortCookie);
    UNREFERENCED_PARAMETER(ConnectionContext);
    UNREFERENCED_PARAMETER(SizeOfContext);
    UNREFERENCED_PARAMETER(ConnectionPortCookie);

    gClientPort = ClientPort;

    DbgPrintEx(DPFLTR_IHVDRIVER_ID,
        DPFLTR_INFO_LEVEL,
        "[SMF] User-mode client connected to communication port\n");

    return STATUS_SUCCESS;
}


// ================= USER-MODE DISCONNECT CALLBACK =================
// Called when user-mode disconnects
// Cleans up the client port reference safely

VOID
MiniDisconnectNotify(
    PVOID ConnectionCookie
)
{
    UNREFERENCED_PARAMETER(ConnectionCookie);

    if (gClientPort) {
        FltCloseClientPort(gFilterHandle, &gClientPort);
        gClientPort = NULL;
    }

    DbgPrintEx(DPFLTR_IHVDRIVER_ID,
        DPFLTR_INFO_LEVEL,
        "[SMF] User-mode client disconnected from communication port\n");
}

VOID
SendMinifilterNotification(
    HANDLE Pid,
    PCWSTR ProcessName,
    PCWSTR Action,
    PCWSTR Response,
    PUNICODE_STRING TargetPath
)
{
    if (gClientPort == NULL)
        return;

    MINIFILTER_NOTIFICATION msg;
    LARGE_INTEGER timeout;

    RtlZeroMemory(&msg, sizeof(msg));

    msg.ProcessId = HandleToULong(Pid);

    if (ProcessName) {
        RtlStringCchCopyW(msg.ProcessName, MF_MAX_PROCESS_NAME, ProcessName);
    }
    else {
        RtlStringCchCopyW(msg.ProcessName, MF_MAX_PROCESS_NAME, L"unknown");
    }

    if (Action) {
        RtlStringCchCopyW(msg.Action, MF_MAX_ACTION_NAME, Action);
    }

    if (Response) {
        RtlStringCchCopyW(msg.Response, MF_MAX_RESPONSE_NAME, Response);
    }

    if (TargetPath && TargetPath->Buffer) {
        SIZE_T copyChars = min((SIZE_T)(TargetPath->Length / sizeof(WCHAR)), (SIZE_T)(MF_MAX_PATH - 1));
        RtlCopyMemory(msg.TargetPath, TargetPath->Buffer, copyChars * sizeof(WCHAR));
        msg.TargetPath[copyChars] = L'\0';
    }

    timeout.QuadPart = -10 * 1000 * 1000; // 1 second

    //View in DebugView.exe
    DbgPrintEx(DPFLTR_IHVDRIVER_ID,
        DPFLTR_INFO_LEVEL,
        "[SMF] Sending event: PID=%llu Action=%ws Response=%ws Target=%ws\n",
        (ULONGLONG)Pid,
        Action ? Action : L"(null)",
        Response ? Response : L"(null)",
        (TargetPath && TargetPath->Buffer) ? TargetPath->Buffer : L"(null)");

    FltSendMessage(
        gFilterHandle,
        &gClientPort,
        &msg,
        sizeof(msg),
        NULL,
        NULL,
        &timeout
    );
}

// ================= DRIVER ENTRY =================
// 
// Driver entry like main()
NTSTATUS  //32-bit value used in Windows kernel to indicate success or failure.
DriverEntry(
    PDRIVER_OBJECT DriverObject, //A pointer to a structure created by the Windows kernel that represents your driver.
    PUNICODE_STRING RegistryPath
 
)
{
    UNREFERENCED_PARAMETER(RegistryPath);

    KeInitializeSpinLock(&gTrackerLock);
    KeInitializeSpinLock(&gRenameLock);

    NTSTATUS status;
    OBJECT_ATTRIBUTES oa;
    UNICODE_STRING uniPortName;
    NTSTATUS portStatus;
    PSECURITY_DESCRIPTOR sd = NULL;

    //Registers filter with system.
    status = FltRegisterFilter(DriverObject, &FilterRegistration, &gFilterHandle);
    if (!NT_SUCCESS(status)) {
        return status;
    }
   
    // ================= CREATE COMMUNICATION PORT =================
    // This sets up a kernel → user communication channel
    // User-mode will connect using FilterConnectCommunicationPort
    RtlInitUnicodeString(&uniPortName, MINIFILTER_PORT_NAME);
    // Create the communication port and register connect/disconnect callbacks
    portStatus = FltBuildDefaultSecurityDescriptor(&sd, FLT_PORT_ALL_ACCESS);
    if (!NT_SUCCESS(portStatus)) {
        FltUnregisterFilter(gFilterHandle);
        return portStatus;
    }

    InitializeObjectAttributes(
        &oa,
        &uniPortName,
        OBJ_KERNEL_HANDLE | OBJ_CASE_INSENSITIVE,
        NULL,
        sd
    );
    portStatus = FltCreateCommunicationPort(
        gFilterHandle,
        &gServerPort,
        &oa,
        NULL,
        MiniConnectNotify,
        MiniDisconnectNotify,
        NULL,
        1
    );

    DbgPrintEx(DPFLTR_IHVDRIVER_ID,
        DPFLTR_INFO_LEVEL,
        "[SMF] FltCreateCommunicationPort status = 0x%08X\n",
        portStatus);

    //Starts intercepting file operations.
    // Start filtering AFTER communication port is ready
    // Ensures messages can be sent immediately when events occur
  //Starts intercepting file operations.
    // Start filtering AFTER communication port is ready
    // Ensures messages can be sent immediately when events occur


    FltFreeSecurityDescriptor(sd);

    if (!NT_SUCCESS(portStatus)) {
        FltUnregisterFilter(gFilterHandle);
        return portStatus;
    }

    status = FltStartFiltering(gFilterHandle);
    if (!NT_SUCCESS(status)) {
        FltCloseCommunicationPort(gServerPort);
        gServerPort = NULL;
        FltUnregisterFilter(gFilterHandle);
        return status;
    }

    DbgPrintEx(DPFLTR_IHVDRIVER_ID,
        DPFLTR_INFO_LEVEL,
        "[SMF] FltStartFiltering status = 0x%08X\n",
        status);

    DbgPrintEx(DPFLTR_IHVDRIVER_ID,
        DPFLTR_INFO_LEVEL,
        "[SMF] SimpleMiniFilter loaded successfully! 15tn RQ ;)\n");
 
    return STATUS_SUCCESS;
}

//=========== HELPER FUNCTION FOR DETECTION AND BLOCKING
#define MAX_PATH_LEN 512



BOOLEAN  //return true if suspicious
IsSuspiciousPath(PUNICODE_STRING FullName)
{   //detect executable in Temp folder
    if (wcsstr(FullName->Buffer, L"\\AppData\\Local\\Temp\\") &&
        wcsstr(FullName->Buffer, L".exe")) {
        return TRUE;
    }
    //detect access to SAM database
    if (wcsstr(FullName->Buffer, L"\\System32\\config\\SAM")) {
        return TRUE;
    }

    //safe
    return FALSE;
}

BOOLEAN
ShouldBlockFile(PUNICODE_STRING FullName)
{   //If file contains secret.txt in path → block.
    if (wcsstr(FullName->Buffer, L"secret.txt")) {
        return TRUE;
    }
    return FALSE;
}
// ================= RANSOMWARE HELPER FUNCTIONS =================
// Checks if a file name ends with a known ransomware extension
BOOLEAN IsRansomwareExtension(PUNICODE_STRING FileName)
{
    if (!FileName || !FileName->Buffer) return FALSE;

    for (int i = 0; gRansomwareExtensions[i] != NULL; i++) {
        UNICODE_STRING ext;
        RtlInitUnicodeString(&ext, gRansomwareExtensions[i]);
        if (FileName->Length >= ext.Length) {
            // Point to the end of the filename (where extension would be)
            UNICODE_STRING fileExt;
            fileExt.Buffer = FileName->Buffer + (FileName->Length / sizeof(WCHAR) - ext.Length / sizeof(WCHAR));
            fileExt.Length = ext.Length;
            fileExt.MaximumLength = ext.Length;
            if (RtlEqualUnicodeString(&fileExt, &ext, TRUE))
                return TRUE;
        }
    }
    return FALSE;
}

// Checks if the file name matches a known ransom note
BOOLEAN IsRansomNote(PUNICODE_STRING FileName)
{
    if (!FileName || !FileName->Buffer) return FALSE;

    for (int i = 0; gRansomNoteNames[i] != NULL; i++) {
        UNICODE_STRING note;
        RtlInitUnicodeString(&note, gRansomNoteNames[i]);
        if (RtlEqualUnicodeString(FileName, &note, TRUE))
            return TRUE;
    }
    return FALSE;
}

// Find existing rename tracker slot for a PID
int FindRenameTrackerSlot(HANDLE pid)
{
    for (int i = 0; i < MAX_RENAME_TRACKERS; i++) {
        if (gRenameTrackers[i].Active && gRenameTrackers[i].Pid == pid)
            return i;
    }
    return -1;
}

// Get a free rename tracker slot (evict oldest if full)
int GetFreeRenameSlot()
{
    for (int i = 0; i < MAX_RENAME_TRACKERS; i++) {
        if (!gRenameTrackers[i].Active)
            return i;
    }
    return 0;   // all full – evict slot 0
}

//=================Convert to unicode for ANSI/truncated data========================
// ================= PROCESS NAME CONVERSION =================
// Converts ANSI process name (PsGetProcessImageFileName) to WCHAR
// Required because communication messages use Unicode strings
VOID GetCurrentProcessNameWide(_Out_writes_(MF_MAX_PROCESS_NAME) WCHAR* outName)
{
    RtlZeroMemory(outName, sizeof(WCHAR) * MF_MAX_PROCESS_NAME);

    PCHAR ansiName = PsGetProcessImageFileName(PsGetCurrentProcess());
    if (!ansiName) {
        RtlStringCchCopyW(outName, MF_MAX_PROCESS_NAME, L"unknown");
        return;
    }

    for (int i = 0; i < MF_MAX_PROCESS_NAME - 1 && ansiName[i] != '\0'; i++) {
        outName[i] = (WCHAR)ansiName[i];
    }
}
