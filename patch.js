const origLog = console.log;
const origErr = console.error;
const logContainer = document.createElement("div");
logContainer.style.cssText = "position:fixed;top:0;left:0;bottom:0;width:100%;pointer-events:none;background:rgba(0,0,0,0.8);color:lime;font-family:monospace;padding:20px;z-index:9999;overflow-y:auto;white-space:pre-wrap;font-size:14px;";
window.addEventListener("DOMContentLoaded", () => document.body.appendChild(logContainer));
function addLog(msg, isErr) {
    const p = document.createElement("div");
    p.style.color = isErr ? "red" : "lime";
    p.textContent = msg;
    if (logContainer) logContainer.appendChild(p);
}
console.log = function(...args) { addLog(args.join(" "), false); origLog.apply(console, args); };
console.error = function(...args) { addLog(args.join(" "), true); origErr.apply(console, args); };
window.addEventListener("error", (e) => addLog(e.error?.stack || e.message, true));
window.addEventListener("unhandledrejection", (e) => addLog(e.reason?.stack || e.reason, true));
