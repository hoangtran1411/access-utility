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
    <title>Access 97 Utility - Modern Visualizer & Diagnostic Suite</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500;600&display=swap" rel="stylesheet">
    <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
    <style>
        :root {
            --bg: #0b0f19;
            --surface: #151d30;
            --surface-elevated: #1e293b;
            --border: #2d3b55;
            --border-light: #3e4f70;
            --primary: #3b82f6;
            --primary-hover: #2563eb;
            --success: #10b981;
            --warning: #f59e0b;
            --danger: #ef4444;
            --purple: #8b5cf6;
            --cyan: #06b6d4;
            --text: #f8fafc;
            --text-secondary: #94a3b8;
            --text-muted: #64748b;
        }

        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif; }

        body {
            background-color: var(--bg);
            color: var(--text);
            min-height: 100vh;
            padding: 1.5rem 2rem;
            display: flex;
            flex-direction: column;
            gap: 1.25rem;
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
            gap: 0.85rem;
        }

        .brand-badge {
            background: linear-gradient(135deg, #3b82f6, #8b5cf6);
            padding: 0.3rem 0.65rem;
            border-radius: 6px;
            font-weight: 700;
            font-size: 0.75rem;
            letter-spacing: 0.05em;
            text-transform: uppercase;
        }

        h1 { font-size: 1.4rem; font-weight: 600; letter-spacing: -0.02em; }
        h2 { font-size: 1.15rem; font-weight: 600; color: #e2e8f0; }

        .nav-tabs {
            display: flex;
            gap: 0.5rem;
            border-bottom: 1px solid var(--border);
            padding-bottom: 0.5rem;
        }

        .tab-btn {
            background: transparent;
            color: var(--text-secondary);
            border: 1px solid transparent;
            border-radius: 6px;
            padding: 0.5rem 1rem;
            font-size: 0.9rem;
            font-weight: 500;
            cursor: pointer;
            transition: all 0.15s ease;
        }

        .tab-btn:hover {
            color: var(--text);
            background: var(--surface);
        }

        .tab-btn.active {
            background: var(--surface-elevated);
            color: var(--primary);
            border-color: var(--border-light);
            font-weight: 600;
        }

        .card {
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 10px;
            padding: 1.25rem 1.5rem;
            display: flex;
            flex-direction: column;
            gap: 1rem;
        }

        .input-group {
            display: flex;
            gap: 0.75rem;
        }

        input[type="text"], input[type="number"] {
            background: #090d16;
            border: 1px solid var(--border);
            border-radius: 6px;
            padding: 0.65rem 0.9rem;
            color: var(--text);
            font-size: 0.95rem;
        }

        input[type="text"] { flex: 1; }

        button {
            background: var(--primary);
            color: white;
            border: none;
            border-radius: 6px;
            padding: 0.65rem 1.1rem;
            font-size: 0.9rem;
            font-weight: 500;
            cursor: pointer;
            transition: background 0.15s ease;
            display: inline-flex;
            align-items: center;
            gap: 0.5rem;
        }

        button:hover { background: var(--primary-hover); }
        button.secondary { background: #25334d; }
        button.secondary:hover { background: #334466; }
        button.success { background: var(--success); }
        button.warning { background: var(--warning); color: #000; }
        button.danger { background: var(--danger); }

        .grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 1rem;
        }

        .stat-box {
            background: #0b0f19;
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 1rem;
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
        }

        .stat-label { color: var(--text-secondary); font-size: 0.8rem; font-weight: 500; text-transform: uppercase; letter-spacing: 0.05em; }
        .stat-val { font-size: 1.35rem; font-weight: 700; color: var(--text); }

        .badge {
            padding: 0.2rem 0.5rem;
            border-radius: 4px;
            font-size: 0.75rem;
            font-weight: 600;
            display: inline-block;
        }

        .badge-header { background: rgba(139, 92, 246, 0.2); color: #c4b5fd; border: 1px solid rgba(139, 92, 246, 0.4); }
        .badge-pam { background: rgba(245, 158, 11, 0.2); color: #fde68a; border: 1px solid rgba(245, 158, 11, 0.4); }
        .badge-tdef { background: rgba(6, 182, 212, 0.2); color: #a5f3fc; border: 1px solid rgba(6, 182, 212, 0.4); }
        .badge-data { background: rgba(16, 185, 129, 0.2); color: #6ee7b7; border: 1px solid rgba(16, 185, 129, 0.4); }
        .badge-index { background: rgba(59, 130, 246, 0.2); color: #93c5fd; border: 1px solid rgba(59, 130, 246, 0.4); }
        .badge-slack { background: rgba(100, 116, 139, 0.2); color: #cbd5e1; border: 1px solid rgba(100, 116, 139, 0.4); }
        .badge-corrupt { background: rgba(239, 68, 68, 0.2); color: #fca5a5; border: 1px solid rgba(239, 68, 68, 0.4); }

        table {
            width: 100%;
            border-collapse: collapse;
            font-size: 0.88rem;
        }

        th, td {
            padding: 0.65rem 0.9rem;
            text-align: left;
            border-bottom: 1px solid var(--border);
        }

        th { background: #0b0f19; color: var(--text-secondary); font-weight: 600; }

        .log-box {
            background: #070a10;
            border: 1px solid var(--border);
            border-radius: 6px;
            padding: 0.85rem;
            font-family: 'JetBrains Mono', monospace;
            font-size: 0.82rem;
            max-height: 180px;
            overflow-y: auto;
            color: #a7f3d0;
            line-height: 1.4;
        }

        /* Sector Map Grid Styles */
        .sector-legend {
            display: flex;
            flex-wrap: wrap;
            gap: 1rem;
            font-size: 0.85rem;
            color: var(--text-secondary);
        }

        .legend-item {
            display: flex;
            align-items: center;
            gap: 0.4rem;
        }

        .legend-dot {
            width: 12px;
            height: 12px;
            border-radius: 3px;
        }

        .sector-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(36px, 1fr));
            gap: 4px;
            max-height: 380px;
            overflow-y: auto;
            background: #090d16;
            padding: 0.75rem;
            border-radius: 6px;
            border: 1px solid var(--border);
        }

        .sector-tile {
            aspect-ratio: 1;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.68rem;
            font-family: 'JetBrains Mono', monospace;
            font-weight: 600;
            border-radius: 3px;
            cursor: pointer;
            transition: transform 0.1s ease, filter 0.1s ease;
            user-select: none;
        }

        .sector-tile:hover {
            transform: scale(1.15);
            filter: brightness(1.25);
            z-index: 10;
        }

        .tile-header { background: #8b5cf6; color: #fff; }
        .tile-pam { background: #f59e0b; color: #000; }
        .tile-tdef { background: #06b6d4; color: #000; }
        .tile-data { background: #10b981; color: #000; }
        .tile-index { background: #3b82f6; color: #fff; }
        .tile-slack { background: #334155; color: #94a3b8; }
        .tile-corrupt { background: #ef4444; color: #fff; }

        /* Hex Inspector Styles */
        .hex-viewer {
            background: #05070d;
            border: 1px solid var(--border);
            border-radius: 6px;
            padding: 0.75rem;
            font-family: 'JetBrains Mono', monospace;
            font-size: 0.8rem;
            line-height: 1.45;
            max-height: 480px;
            overflow-y: auto;
            white-space: pre;
            color: #cbd5e1;
        }

        .hex-line {
            display: flex;
            gap: 1rem;
        }

        .hex-offset { color: #64748b; }
        .hex-bytes { color: #38bdf8; }
        .hex-ascii { color: #a7f3d0; border-left: 1px solid #1e293b; padding-left: 0.75rem; }

        /* ERD Graph Styles */
        .erd-container {
            background: #090d16;
            border: 1px solid var(--border);
            border-radius: 6px;
            padding: 1rem;
            min-height: 300px;
            overflow: auto;
            display: flex;
            justify-content: center;
            align-items: center;
        }

        .hidden { display: none !important; }
    </style>
</head>
<body>

    <header>
        <div class="brand">
            <span class="brand-badge">Jet 3.5 Engine</span>
            <h1>Access 97 Visualizer & Maintenance Suite</h1>
        </div>
        <div style="display: flex; align-items: center; gap: 0.75rem;">
            <button onclick="triggerAutoUpdate()" class="secondary" style="padding: 0.4rem 0.8rem; font-size: 0.82rem;">Check for Updates</button>
            <span style="color: var(--text-secondary); font-size: 0.85rem;">.NET 10 Native AOT</span>
        </div>
    </header>

    <!-- File Selector Bar -->
    <div class="card">
        <div class="input-group">
            <input type="text" id="filePath" placeholder="Enter path to .mdb database (e.g. sample97.mdb or C:\Data\Main.mdb)..." value="sample97.mdb">
            <button onclick="diagnoseDb()">Diagnose & Load</button>
        </div>
    </div>

    <!-- Navigation Tabs -->
    <nav class="nav-tabs">
        <button id="tabOverviewBtn" class="tab-btn active" onclick="switchTab('overview')">Overview & Health</button>
        <button id="tabSectorBtn" class="tab-btn" onclick="switchTab('sector')">Sector Map (2KB Pages)</button>
        <button id="tabHexBtn" class="tab-btn" onclick="switchTab('hex')">Hex Page Inspector</button>
        <button id="tabErdBtn" class="tab-btn" onclick="switchTab('erd')">Schema ERD Graph</button>
        <button id="tabLogsBtn" class="tab-btn" onclick="switchTab('logs')">System Logs</button>
    </nav>

    <!-- TAB 1: Overview & Diagnostics -->
    <div id="tabOverview">
        <div id="lockCard" class="card hidden" style="background: rgba(245, 158, 11, 0.1); border-color: rgba(245, 158, 11, 0.3);">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <div>
                    <strong style="color: #fbbf24;">Lock File (.ldb) Detected</strong>
                    <div id="lockDetail" style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 0.25rem;">Checking user connections...</div>
                </div>
                <button id="cleanLockBtn" class="warning" onclick="cleanLock()">Clean Stale Lock</button>
            </div>
        </div>

        <div id="diagCard" class="card hidden">
            <h2>Database Diagnostics & Health</h2>
            <div class="grid">
                <div class="stat-box">
                    <span class="stat-label">File Size</span>
                    <span id="statSize" class="stat-val">0 MB</span>
                </div>
                <div class="stat-box">
                    <span class="stat-label">Total 2KB Pages</span>
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

            <div style="margin-top: 0.5rem; display: flex; flex-wrap: wrap; gap: 0.5rem;">
                <button class="success" onclick="compactDb()">Compact Database</button>
                <button onclick="repairDb()">Repair & Recover</button>
                <button class="secondary" onclick="exportDb('parquet')">Export Parquet</button>
                <button class="secondary" onclick="exportDb('duckdb')">Export DuckDB</button>
                <button class="secondary" onclick="exportDb('jsonl')">Export JSONL</button>
                <button class="secondary" onclick="exportDb('sqlite')">Export SQLite</button>
                <button class="secondary" onclick="exportDb('sql')">Export SQL Script</button>
                <button class="secondary" onclick="exportDb('csv')">Export CSV</button>
            </div>
        </div>

        <div id="logCard" class="card hidden">
            <h2>Execution Log</h2>
            <div id="logContent" class="log-box">Ready.</div>
        </div>

        <div id="tablesCard" class="card hidden">
            <h2>Discovered Tables</h2>
            <div style="overflow-x: auto;">
                <table>
                    <thead>
                        <tr>
                            <th>Table Name</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody id="tablesBody"></tbody>
                </table>
            </div>
        </div>
    </div>

    <!-- TAB 2: Sector Map -->
    <div id="tabSector" class="card hidden">
        <div style="display: flex; justify-content: space-between; align-items: center;">
            <h2>Interactive 2048-Byte Sector Page Distribution</h2>
            <button class="secondary" onclick="loadSectorMap()">Refresh Map</button>
        </div>

        <div class="sector-legend">
            <div class="legend-item"><span class="legend-dot" style="background:#8b5cf6;"></span> Header (0x00)</div>
            <div class="legend-item"><span class="legend-dot" style="background:#f59e0b;"></span> PAM (0x01)</div>
            <div class="legend-item"><span class="legend-dot" style="background:#06b6d4;"></span> TDEF (0x02)</div>
            <div class="legend-item"><span class="legend-dot" style="background:#10b981;"></span> Data (0x01)</div>
            <div class="legend-item"><span class="legend-dot" style="background:#3b82f6;"></span> Index (0x03/04)</div>
            <div class="legend-item"><span class="legend-dot" style="background:#334155;"></span> Slack/Free (0x00)</div>
            <div class="legend-item"><span class="legend-dot" style="background:#ef4444;"></span> Corrupt</div>
        </div>

        <div id="sectorGrid" class="sector-grid">
            <div style="grid-column: 1/-1; text-align: center; color: var(--text-secondary); padding: 2rem;">
                Load or diagnose a database to visualize sectors.
            </div>
        </div>

        <div id="sectorDetailBox" class="stat-box hidden">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <div>
                    <span id="detailPageNum" class="stat-label">Page Details</span>
                    <div id="detailDesc" style="font-size: 1rem; font-weight: 600; margin-top: 0.25rem;">-</div>
                </div>
                <button id="inspectInHexBtn" class="secondary" style="font-size: 0.85rem;" onclick="jumpToHex(currentSelectedPage)">Inspect Page in Hex Viewer →</button>
            </div>
        </div>
    </div>

    <!-- TAB 3: Hex Inspector -->
    <div id="tabHex" class="card hidden">
        <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.75rem;">
            <h2>Low-Level 2048-Byte Hex & ASCII Page Inspector</h2>
            <div style="display: flex; gap: 0.5rem; align-items: center;">
                <button class="secondary" onclick="stepPage(-1)">← Prev Page</button>
                <input type="number" id="hexPageNum" value="0" min="0" style="width: 80px; text-align: center;">
                <button class="secondary" onclick="stepPage(1)">Next Page →</button>
                <button onclick="loadHexPage()">Inspect</button>
            </div>
        </div>

        <div id="hexMetaBox" style="display: flex; gap: 1rem; align-items: center; font-size: 0.88rem; color: var(--text-secondary);">
            <span>Page <strong id="hexMetaNum">0</strong></span>
            <span id="hexMetaBadge" class="badge badge-header">Header</span>
            <span id="hexMetaDesc">Jet 3.5 Signature</span>
        </div>

        <div id="hexContent" class="hex-viewer">Select and load a page to view raw hexadecimal and ASCII bytes.</div>
    </div>

    <!-- TAB 4: ERD Graph -->
    <div id="tabErd" class="card hidden">
        <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.75rem;">
            <h2>Entity-Relationship Diagram (ERD)</h2>
            <div style="display: flex; gap: 0.5rem;">
                <button class="secondary" onclick="copyMermaidCode()">Copy Mermaid Code</button>
                <button onclick="loadErdGraph()">Refresh Diagram</button>
            </div>
        </div>

        <div id="erdContainer" class="erd-container">
            <div id="erdDiagramTarget" style="width: 100%; text-align: center; color: var(--text-secondary);">
                Click Refresh Diagram to generate the Mermaid ERD graph.
            </div>
        </div>

        <div id="erdSummaryCard" class="stat-box hidden">
            <div class="stat-label">ERD Analysis</div>
            <div id="erdSummaryText" style="font-size: 0.9rem; color: var(--text); margin-top: 0.25rem;"></div>
        </div>
    </div>

    <!-- TAB 5: System Logs -->
    <div id="tabLogs" class="card hidden">
        <div style="display: flex; justify-content: space-between; align-items: center;">
            <h2>System Logs & Operational Audits</h2>
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
        let currentSelectedPage = 0;
        let cachedMermaidCode = '';

        if (window.mermaid) {
            mermaid.initialize({ startOnLoad: false, theme: 'dark', securityLevel: 'loose' });
        }

        function switchTab(tabId) {
            const tabs = ['overview', 'sector', 'hex', 'erd', 'logs'];
            tabs.forEach(t => {
                const el = document.getElementById('tab' + t.charAt(0).toUpperCase() + t.slice(1));
                const btn = document.getElementById('tab' + t.charAt(0).toUpperCase() + t.slice(1) + 'Btn');
                if (el) el.classList.add('hidden');
                if (btn) btn.classList.remove('active');
            });

            const activeEl = document.getElementById('tab' + tabId.charAt(0).toUpperCase() + tabId.slice(1));
            const activeBtn = document.getElementById('tab' + tabId.charAt(0).toUpperCase() + tabId.slice(1) + 'Btn');
            if (activeEl) activeEl.classList.remove('hidden');
            if (activeBtn) activeBtn.classList.add('active');

            if (tabId === 'sector') loadSectorMap();
            if (tabId === 'hex') loadHexPage();
            if (tabId === 'erd') loadErdGraph();
            if (tabId === 'logs') fetchSystemLogs();
        }

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
                tr.innerHTML = `<td><strong>${summary}</strong></td><td><span class="badge badge-data">Active</span></td>`;
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
            if (box) {
                box.innerText = `[${new Date().toLocaleTimeString()}] ${msg}\n` + box.innerText;
            }
        }

        async function loadSectorMap() {
            const path = document.getElementById('filePath').value;
            if (!path) return;

            const grid = document.getElementById('sectorGrid');
            grid.innerHTML = '<div style="grid-column: 1/-1; text-align: center; color: var(--text-secondary); padding: 2rem;">Analyzing sectors...</div>';

            try {
                const res = await fetch('/api/pages?path=' + encodeURIComponent(path));
                const report = await res.json();

                grid.innerHTML = '';
                if (!report.pages || report.pages.length === 0) {
                    grid.innerHTML = '<div style="grid-column: 1/-1; text-align: center; color: var(--text-secondary); padding: 2rem;">No pages found or invalid database file.</div>';
                    return;
                }

                report.pages.forEach(p => {
                    const tile = document.createElement('div');
                    tile.className = `sector-tile tile-${p.pageType.toLowerCase()}`;
                    tile.innerText = p.pageIndex;
                    tile.title = `Page #${p.pageIndex} [${p.pageType}]: ${p.description}`;
                    tile.onclick = () => selectSectorPage(p);
                    grid.appendChild(tile);
                });
            } catch (e) {
                grid.innerHTML = `<div style="grid-column: 1/-1; text-align: center; color: var(--danger); padding: 2rem;">Failed to load sector map: ${e}</div>`;
            }
        }

        function selectSectorPage(page) {
            currentSelectedPage = page.pageIndex;
            const box = document.getElementById('sectorDetailBox');
            box.classList.remove('hidden');

            document.getElementById('detailPageNum').innerText = `Page #${page.pageIndex} [${page.pageType.toUpperCase()}]`;
            document.getElementById('detailDesc').innerText = page.description + (page.freeSpaceBytes ? ` (${page.freeSpaceBytes} bytes free)` : '');
        }

        function jumpToHex(pageIndex) {
            document.getElementById('hexPageNum').value = pageIndex;
            switchTab('hex');
        }

        function stepPage(delta) {
            const input = document.getElementById('hexPageNum');
            let val = parseInt(input.value || '0') + delta;
            if (val < 0) val = 0;
            input.value = val;
            loadHexPage();
        }

        async function loadHexPage() {
            const path = document.getElementById('filePath').value;
            const pageIndex = parseInt(document.getElementById('hexPageNum').value || '0');
            if (!path) return;

            const hexBox = document.getElementById('hexContent');
            hexBox.innerText = 'Loading page bytes...';

            try {
                const res = await fetch(`/api/pages/${pageIndex}/hex?path=${encodeURIComponent(path)}`);
                const data = await res.json();

                document.getElementById('hexMetaNum').innerText = data.pageIndex;
                document.getElementById('hexMetaDesc').innerText = data.description || '';
                
                const badge = document.getElementById('hexMetaBadge');
                badge.className = `badge badge-${(data.pageType || 'slack').toLowerCase()}`;
                badge.innerText = data.pageType || 'Unknown';

                if (data.hexLines && data.hexLines.length > 0) {
                    hexBox.innerHTML = data.hexLines.map(l => 
                        `<div class="hex-line"><span class="hex-offset">${l.offset}</span> <span class="hex-bytes">${l.hexPart1}  ${l.hexPart2}</span> <span class="hex-ascii">${escapeHtml(l.ascii)}</span></div>`
                    ).join('');
                } else {
                    hexBox.innerText = 'Page content empty or unreadable.';
                }
            } catch (e) {
                hexBox.innerText = 'Failed to load hex view: ' + e;
            }
        }

        async function loadErdGraph() {
            const path = document.getElementById('filePath').value;
            if (!path) return;

            const target = document.getElementById('erdDiagramTarget');
            target.innerHTML = 'Generating Mermaid ERD schema graph...';

            try {
                const res = await fetch('/api/erd?path=' + encodeURIComponent(path));
                const data = await res.json();

                cachedMermaidCode = data.mermaidCode || '';
                document.getElementById('erdSummaryCard').classList.remove('hidden');
                document.getElementById('erdSummaryText').innerText = 
                    `Discovered ${data.tableCount} tables and ${data.relationshipCount} foreign key relationships.`;

                if (window.mermaid && data.mermaidCode) {
                    const id = 'mermaid_' + Math.random().toString(36).substr(2, 9);
                    const { svg } = await mermaid.render(id, data.mermaidCode);
                    target.innerHTML = svg;
                } else {
                    target.innerHTML = `<pre style="text-align: left; font-family: monospace; font-size: 0.85rem; color: #a7f3d0;">${escapeHtml(data.mermaidCode)}</pre>`;
                }
            } catch (e) {
                target.innerHTML = `<div style="color: var(--danger);">Failed to render ERD diagram: ${e}</div><pre style="text-align: left; font-family: monospace; font-size: 0.82rem; color: #a7f3d0; margin-top: 1rem;">${escapeHtml(cachedMermaidCode)}</pre>`;
            }
        }

        function copyMermaidCode() {
            if (!cachedMermaidCode) return alert('No ERD code generated yet. Please load a database first.');
            navigator.clipboard.writeText(cachedMermaidCode).then(() => {
                alert('Mermaid ERD code copied to clipboard! 📋');
            });
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
                    
                    let levelBadge = `<span class="badge badge-data">INFO</span>`;
                    if (entry.level.toLowerCase() === 'error' || entry.level.toLowerCase() === 'fatal') {
                        levelBadge = `<span class="badge badge-corrupt">ERROR</span>`;
                    } else if (entry.level.toLowerCase() === 'warning') {
                        levelBadge = `<span class="badge badge-pam">WARN</span>`;
                    }
                    
                    tr.innerHTML = `
                        <td style="color: var(--text-secondary); font-size: 0.82rem;">${new Date(entry.timestamp).toLocaleString()}</td>
                        <td>${levelBadge}</td>
                        <td style="font-family: monospace; font-size: 0.82rem;">
                            ${escapeHtml(entry.message)}
                            ${entry.exception ? '<br><span style="color: var(--danger);">' + escapeHtml(entry.exception) + '</span>' : ''}
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

        function escapeHtml(text) {
            if (!text) return '';
            return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
        }
    </script>
</body>
</html>
""";
    }
}
