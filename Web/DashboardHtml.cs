namespace AccessUtility.Web
{
    public static class DashboardHtml
    {
        public const string HtmlContent = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Access 97 Utility - Compact & Repair Dashboard</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg: #0f172a;
            --surface: #1e293b;
            --border: #334155;
            --primary: #3b82f6;
            --primary-hover: #2563eb;
            --success: #10b981;
            --warning: #f59e0b;
            --danger: #ef4444;
            --text: #f8fafc;
            --text-secondary: #94a3b8;
        }

        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Inter', sans-serif; }

        body {
            background-color: var(--bg);
            color: var(--text);
            min-height: 100vh;
            padding: 2rem;
            display: flex;
            flex-direction: column;
            gap: 1.5rem;
        }

        header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding-bottom: 1rem;
            border-bottom: 1px solid var(--border);
        }

        .brand {
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }

        .brand-badge {
            background: linear-gradient(135deg, #3b82f6, #8b5cf6);
            padding: 0.25rem 0.6rem;
            border-radius: 6px;
            font-weight: 700;
            font-size: 0.8rem;
            text-transform: uppercase;
        }

        h1 { font-size: 1.5rem; font-weight: 600; }

        .card {
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 1.5rem;
            display: flex;
            flex-direction: column;
            gap: 1rem;
        }

        .input-group {
            display: flex;
            gap: 0.75rem;
        }

        input[type="text"] {
            flex: 1;
            background: #0f172a;
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 0.75rem 1rem;
            color: var(--text);
            font-size: 0.95rem;
        }

        button {
            background: var(--primary);
            color: white;
            border: none;
            border-radius: 8px;
            padding: 0.75rem 1.25rem;
            font-size: 0.95rem;
            font-weight: 500;
            cursor: pointer;
            transition: all 0.2s ease;
            display: inline-flex;
            align-items: center;
            gap: 0.5rem;
        }

        button:hover { background: var(--primary-hover); }
        button.secondary { background: #334155; }
        button.secondary:hover { background: #475569; }
        button.success { background: var(--success); }
        button.warning { background: var(--warning); color: #000; }

        .grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 1.5rem;
        }

        .stat-box {
            background: #0f172a;
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 1rem;
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
        }

        .stat-label { color: var(--text-secondary); font-size: 0.85rem; font-weight: 500; }
        .stat-val { font-size: 1.4rem; font-weight: 700; color: var(--text); }

        .lock-banner {
            background: rgba(245, 158, 11, 0.1);
            border: 1px solid rgba(245, 158, 11, 0.3);
            border-radius: 8px;
            padding: 1rem;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            font-size: 0.9rem;
        }

        th, td {
            padding: 0.75rem 1rem;
            text-align: left;
            border-bottom: 1px solid var(--border);
        }

        th { background: #0f172a; color: var(--text-secondary); font-weight: 600; }

        .badge {
            padding: 0.2rem 0.5rem;
            border-radius: 4px;
            font-size: 0.75rem;
            font-weight: 600;
        }

        .badge-success { background: rgba(16, 185, 129, 0.2); color: #34d399; }
        .badge-warning { background: rgba(245, 158, 11, 0.2); color: #fbbf24; }
        .badge-danger { background: rgba(239, 68, 68, 0.2); color: #f87171; }

        .actions { display: flex; gap: 0.75rem; flex-wrap: wrap; }
        .log-box {
            background: #090d16;
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 1rem;
            font-family: monospace;
            font-size: 0.85rem;
            max-height: 200px;
            overflow-y: auto;
            color: #a7f3d0;
        }

        .hidden { display: none !important; }
    </style>
</head>
<body>

    <header>
        <div class="brand">
            <span class="brand-badge">Native AOT</span>
            <h1>Access 97 Compact & Repair Utility</h1>
        </div>
        <div style="display: flex; align-items: center; gap: 1rem;">
            <button onclick="triggerAutoUpdate()" class="secondary" style="padding: 0.4rem 0.8rem; font-size: 0.85rem;">Check for Updates</button>
            <span style="color: var(--text-secondary); font-size: 0.9rem;">.NET 10 | Jet 3.5 Engine</span>
        </div>
    </header>

    <!-- File Selector Card -->
    <div class="card">
        <h2>Select Access 97 Database (.mdb)</h2>
        <div class="input-group">
            <input type="text" id="filePath" placeholder="Enter path to .mdb file (e.g. C:\Database\Northwind.mdb)...">
            <button onclick="diagnoseDb()">Diagnose & Inspect</button>
        </div>
    </div>

    <!-- Lock Status Card -->
    <div id="lockCard" class="lock-banner hidden">
        <div>
            <strong id="lockTitle" style="color: #fbbf24;">Lock File (.ldb) Detected</strong>
            <div id="lockDetail" style="font-size: 0.85rem; margin-top: 0.25rem;">Checking user connections...</div>
        </div>
        <button id="cleanLockBtn" class="warning" onclick="cleanLock()">Clean Stale Lock</button>
    </div>

    <!-- Health & Diagnostics Grid -->
    <div id="diagCard" class="card hidden">
        <h2>Database Diagnostics & Health</h2>
        <div class="grid">
            <div class="stat-box">
                <span class="stat-label">File Size</span>
                <span id="statSize" class="stat-val">0 MB</span>
            </div>
            <div class="stat-box">
                <span class="stat-label">Total Pages</span>
                <span id="statPages" class="stat-val">0</span>
            </div>
            <div class="stat-box">
                <span class="stat-label">Fragmentation Space</span>
                <span id="statFrag" class="stat-val">0%</span>
            </div>
            <div class="stat-box">
                <span class="stat-label">Corrupted Pages</span>
                <span id="statCorrupt" class="stat-val">0</span>
            </div>
        </div>

        <div class="actions" style="margin-top: 1rem;">
            <button class="success" onclick="compactDb()">Compact Database</button>
            <button onclick="repairDb()">Repair & Recover</button>
            <button class="secondary" onclick="exportDb('sqlite')">Export to SQLite</button>
            <button class="secondary" onclick="exportDb('sql')">Export SQL Script</button>
        </div>
    </div>

    <!-- Operation Output Log -->
    <div id="logCard" class="card hidden">
        <h2>Execution Log</h2>
        <div id="logContent" class="log-box">Ready.</div>
    </div>

    <!-- Table Explorer -->
    <div id="tablesCard" class="card hidden">
        <h2>Database Tables</h2>
        <div style="overflow-x: auto;">
            <table>
                <thead>
                    <tr>
                        <th>Table Name</th>
                        <th>Columns</th>
                        <th>Rows</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody id="tablesBody">
                </tbody>
            </table>
        </div>
    </div>

    <!-- System Logs Viewer -->
    <div class="card">
        <div style="display: flex; justify-content: space-between; align-items: center;">
            <h2>Daemon System Logs</h2>
            <button onclick="fetchSystemLogs()" class="secondary">Refresh Logs</button>
        </div>
        <div style="overflow-x: auto;">
            <table>
                <thead>
                    <tr>
                        <th style="width: 20%">Timestamp</th>
                        <th style="width: 10%">Level</th>
                        <th>Message</th>
                    </tr>
                </thead>
                <tbody id="systemLogsBody">
                    <tr><td colspan="3" style="text-align: center; color: var(--text-secondary);">Click Refresh to load system logs...</td></tr>
                </tbody>
            </table>
        </div>
    </div>

    <script>
        async function diagnoseDb() {
            const path = document.getElementById('filePath').value;
            if (!path) return alert('Please enter a .mdb file path');

            log('Scanning database page structure...');
            const res = await fetch('/api/diagnose?path=' + encodeURIComponent(path));
            const data = await res.json();

            document.getElementById('diagCard').classList.remove('hidden');
            document.getElementById('logCard').classList.remove('hidden');

            document.getElementById('statSize').innerText = (data.fileSizeBytes / (1024*1024)).toFixed(2) + ' MB';
            document.getElementById('statPages').innerText = data.totalPages;
            document.getElementById('statFrag').innerText = data.fragmentationPercentage + '%';
            document.getElementById('statCorrupt').innerText = data.corruptPagesCount;

            log(data.statusSummary);

            if (data.lockInfo && data.lockInfo.exists) {
                const lCard = document.getElementById('lockCard');
                lCard.classList.remove('hidden');
                document.getElementById('lockDetail').innerText = 
                    data.lockInfo.isOrphanLock ? 'Orphan .ldb file left from past crash (Safe to clean).' : 
                    'Active connection detected (' + data.lockInfo.connectedUsers.length + ' users).';
            } else {
                document.getElementById('lockCard').classList.add('hidden');
            }

            // Render tables
            const tbody = document.getElementById('tablesBody');
            tbody.innerHTML = '';
            (data.tableSummaries || []).forEach(summary => {
                const tr = document.createElement('tr');
                tr.innerHTML = `<td><strong>${summary}</strong></td><td>-</td><td>-</td><td><span class="badge badge-success">Recovered</span></td>`;
                tbody.appendChild(tr);
            });
            document.getElementById('tablesCard').classList.remove('hidden');
        }

        async function compactDb() {
            const path = document.getElementById('filePath').value;
            log('Starting compact operation...');
            const res = await fetch('/api/compact?path=' + encodeURIComponent(path));
            const data = await res.json();
            log(data.message);
            if (data.success) diagnoseDb();
        }

        async function repairDb() {
            const path = document.getElementById('filePath').value;
            log('Starting deep repair and recovery scan...');
            const res = await fetch('/api/repair?path=' + encodeURIComponent(path));
            const data = await res.json();
            log(data.message + '\n' + (data.recoveryLog || []).join('\n'));
            if (data.success) diagnoseDb();
        }

        async function cleanLock() {
            const path = document.getElementById('filePath').value;
            const res = await fetch('/api/clean-lock?path=' + encodeURIComponent(path));
            const data = await res.json();
            log(data.message);
            diagnoseDb();
        }

        async function exportDb(fmt) {
            const path = document.getElementById('filePath').value;
            log('Exporting database as ' + fmt.toUpperCase() + '...');
            const res = await fetch(`/api/export?path=${encodeURIComponent(path)}&format=${fmt}`);
            const data = await res.json();
            log('Export complete! File saved at: ' + data.outputPath);
        }

        function log(msg) {
            const box = document.getElementById('logContent');
            box.innerText = `[${new Date().toLocaleTimeString()}] ${msg}\n` + box.innerText;
        }

        async function fetchSystemLogs() {
            try {
                const res = await fetch('/api/logs?limit=30');
                const logs = await res.json();
                
                const tbody = document.getElementById('systemLogsBody');
                tbody.innerHTML = '';
                
                if (!logs || logs.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="3" style="text-align: center; color: var(--text-secondary);">No logs found.</td></tr>';
                    return;
                }
                
                logs.forEach(entry => {
                    const tr = document.createElement('tr');
                    
                    let levelBadge = `<span class="badge badge-success">INFO</span>`;
                    if (entry.level.toLowerCase() === 'error' || entry.level.toLowerCase() === 'fatal') {
                        levelBadge = `<span class="badge badge-danger">ERROR</span>`;
                    } else if (entry.level.toLowerCase() === 'warning') {
                        levelBadge = `<span class="badge badge-warning">WARN</span>`;
                    }
                    
                    tr.innerHTML = `
                        <td style="color: var(--text-secondary); font-size: 0.85rem;">${new Date(entry.timestamp).toLocaleString()}</td>
                        <td>${levelBadge}</td>
                        <td style="font-family: monospace; font-size: 0.85rem;">
                            ${entry.message}
                            ${entry.exception ? '<br><span style="color: var(--danger);">' + entry.exception + '</span>' : ''}
                        </td>
                    `;
                    tbody.appendChild(tr);
                });
            } catch (e) {
                console.error("Failed to load logs", e);
            }
        }

        async function triggerAutoUpdate() {
            if (!confirm('Check GitHub for a newer version of AccessUtility?')) return;
            document.getElementById('logCard').classList.remove('hidden');
            log('Checking for new version release on GitHub...');
            try {
                const res = await fetch('/api/update', { method: 'POST' });
                const data = await res.json();
                const msg = data.message || data.Message || 'Update check completed.';
                log(msg);
                alert(msg);
            } catch (e) {
                log('Auto update check failed: ' + e);
            }
        }
    </script>
</body>
</html>
""";
    }
}
