#include "ProvisioningPortalView.h"

namespace {
    bool shouldAutoRefresh(ProvisioningPortalState state) {
        return state == ProvisioningPortalState::WaitingForWifi ||
            state == ProvisioningPortalState::RequestingCode ||
            state == ProvisioningPortalState::WaitingForApproval;
    }
}

std::string renderProvisioningStatusHtml(const ProvisioningPortalViewModel& model) {
    if (model.state == ProvisioningPortalState::Idle || model.state == ProvisioningPortalState::ConfigRequired) {
        return std::string();
    }

    if (model.state == ProvisioningPortalState::Provisioned) {
        return "<section class='ok'><strong>Provisioning complete.</strong><p>Device credentials were received and saved.</p></section>";
    }

    if (model.state == ProvisioningPortalState::Error) {
        std::string text = model.message.empty() ? "Provisioning failed." : model.message;
        return "<section class='err'><strong>Error</strong><p>" + text + "</p></section>";
    }

    std::string info = model.message.empty() ? "Provisioning in progress..." : model.message;
    std::string codeBlock;
    if (!model.provisionCode.empty()) {
        codeBlock = "<p><strong>Code: </strong><span style='font-size:20px'>" + model.provisionCode + "</span></p>";
    }

    return "<section class='info'><strong>Provisioning</strong><p>" + info + "</p>" + codeBlock + "</section>";
}

std::string renderProvisioningPortalPage(const ProvisioningPortalViewModel& model) {
    const uint16_t defaultServerPort = 1883;
    uint16_t serverPortToDisplay = model.server.port > 0 ? model.server.port : defaultServerPort;
    bool autoRefreshStatus = shouldAutoRefresh(model.state);

    std::string html = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>";

    html +=
        "<title>Device Setup</title><style>body{font-family:Arial,sans-serif;max-width:720px;margin:40px auto;padding:0 16px;color:#1f2937}"
        "section{padding:14px 16px;border-radius:10px;margin:14px 0}"
        ".info{background:#eff6ff;border:1px solid #bfdbfe}"
        ".ok{background:#ecfdf5;border:1px solid #a7f3d0}"
        ".err{background:#fef2f2;border:1px solid #fecaca}"
        "h1{font-size:28px;margin-bottom:8px}p{color:#6b7280}form{display:grid;gap:12px;margin-top:24px}label{display:grid;gap:6px;font-weight:600}"
        "input,select{padding:12px 14px;border:1px solid #d1d5db;border-radius:10px;font-size:16px}"
        ".ssid-row{display:flex;gap:8px}"
        ".ssid-row select{flex:1}"
        ".hidden{display:none}"
        "button{padding:12px 16px;border:0;border-radius:10px;background:#111827;color:#fff;font-weight:700;font-size:16px}"
        ".scan-btn{background:#374151;white-space:nowrap}</style></head><body>";
    html += "<h1>Device Setup</h1><p>Enter Wi-Fi and server settings. AP remains active until provisioning is complete.</p>";
    html += "<div id='provision-status'>" + renderProvisioningStatusHtml(model) + "</div>";
    html += "<form method='post' action='/save' id='setup-form'>";
    html += "<label>Wi-Fi Network<div class='ssid-row'><select id='ssid-select' name='ssidSelect'><option value=''>Scanning networks...</option></select><button type='button' class='scan-btn' id='scan-btn'>Scan</button></div></label>";
    html += "<label id='ssid-manual-wrap' class='hidden'>Wi-Fi SSID<input id='ssid-manual' name='ssid' value='" + model.wifi.ssid + "' placeholder='Enter SSID'></label>";
    html += "<label>Wi-Fi Password<input name='password' value='" + model.wifi.password + "'></label>";
    html += "<label>Server Address<input name='server' value='" + model.server.address + "'></label>";
    html += "<label>Server Port<input name='port' type='number' min='1' max='65535' value='" + std::to_string(serverPortToDisplay) + "'></label>";
    html += "<button type='submit'>Save and Connect</button></form>";

    html +=
        "<script>(function(){"
        "var statusEl=document.getElementById('provision-status');"
        "var wifiSelect=document.getElementById('ssid-select');"
        "var ssidManualWrap=document.getElementById('ssid-manual-wrap');"
        "var ssidManual=document.getElementById('ssid-manual');"
        "var scanBtn=document.getElementById('scan-btn');"
        "var setupForm=document.getElementById('setup-form');"
        "var preferredSsid=ssidManual.value||'';"
        "var autoRefreshStatus=" + std::string(autoRefreshStatus ? "true" : "false") + ";"
        "if(!wifiSelect||!ssidManualWrap||!ssidManual||!scanBtn||!setupForm){return;}"
        "function isOtherSelected(){return wifiSelect.value==='__other__';}"
        "function syncManualVisibility(){"
        "if(isOtherSelected()){ssidManualWrap.classList.remove('hidden');return;}"
        "ssidManualWrap.classList.add('hidden');"
        "ssidManual.value='';"
        "}"
        "function refreshStatus(){"
        "if(!autoRefreshStatus||!statusEl){return;}"
        "fetch('/status',{cache:'no-store'})"
        ".then(function(r){if(!r.ok){throw new Error('status');}return r.text();})"
        ".then(function(statusHtml){statusEl.innerHTML=statusHtml;})"
        ".catch(function(){});"
        "}"
        "function selectedKey(){"
        "var opt=wifiSelect.options[wifiSelect.selectedIndex];"
        "if(!opt){return '';}"
        "return (opt.value||'')+'|'+(opt.getAttribute('data-bssid')||'');"
        "}"
        "function sortBySignal(items){"
        "return items.sort(function(a,b){return (b.rssi||-9999)-(a.rssi||-9999);});"
        "}"
        "function renderNetworks(items){"
        "var current=selectedKey();"
        "while(wifiSelect.firstChild){wifiSelect.removeChild(wifiSelect.firstChild);}"
        "var placeholder=document.createElement('option');"
        "placeholder.value='';"
        "placeholder.textContent='-- Select network --';"
        "wifiSelect.appendChild(placeholder);"
        "items=sortBySignal(items||[]);"
        "var ssidCounts={};"
        "for(var k=0;k<items.length;k++){"
        "var candidateSsid=((items[k]||{}).ssid||'');"
        "ssidCounts[candidateSsid]=(ssidCounts[candidateSsid]||0)+1;"
        "}"
        "for(var i=0;i<items.length;i++){"
        "var n=items[i]||{};"
        "var opt=document.createElement('option');"
        "var hidden=!n.ssid;"
        "var ssid=n.ssid||'';"
        "var bssid=n.bssid||'';"
        "var channel=(n.channel===undefined||n.channel===null)?'?':n.channel;"
        "var label=hidden?'(Hidden network)':ssid;"
        "if(!hidden&&(ssidCounts[ssid]||0)>1){label+=' (ch '+channel+')';}"
        "opt.value=ssid;"
        "opt.setAttribute('data-bssid',bssid);"
        "opt.textContent=label;"
        "wifiSelect.appendChild(opt);"
        "}"
        "var hasPreferred=false;"
        "if(preferredSsid){"
        "for(var s=0;s<wifiSelect.options.length;s++){"
        "if(wifiSelect.options[s].value===preferredSsid){hasPreferred=true;break;}"
        "}"
        "if(!hasPreferred){"
        "var saved=document.createElement('option');"
        "saved.value=preferredSsid;"
        "saved.textContent=preferredSsid;"
        "saved.setAttribute('data-bssid','');"
        "wifiSelect.appendChild(saved);"
        "}"
        "}"
        "var other=document.createElement('option');"
        "other.value='__other__';"
        "other.textContent='Other';"
        "wifiSelect.appendChild(other);"
        "if(items.length===0){"
        "var empty=document.createElement('option');"
        "empty.value='';"
        "empty.textContent='No Wi-Fi networks found';"
        "wifiSelect.insertBefore(empty,other);"
        "}"
        "for(var j=0;j<wifiSelect.options.length;j++){"
        "var candidate=wifiSelect.options[j];"
        "var candidateKey=(candidate.value||'')+'|'+(candidate.getAttribute('data-bssid')||'');"
        "if(candidateKey===current){wifiSelect.selectedIndex=j;break;}"
        "}"
        "if(wifiSelect.value===''&&preferredSsid){"
        "for(var p=0;p<wifiSelect.options.length;p++){"
        "if(wifiSelect.options[p].value===preferredSsid){wifiSelect.selectedIndex=p;break;}"
        "}"
        "}"
        "syncManualVisibility();"
        "}"
        "function refreshNetworks(){"
        "fetch('/wifi-networks',{cache:'no-store'})"
        ".then(function(r){if(!r.ok){throw new Error('wifi');}return r.json();})"
        ".then(function(payload){renderNetworks(payload.results||[]);})"
        ".catch(function(){});"
        "}"
        "scanBtn.addEventListener('click',function(){"
        "scanBtn.disabled=true;"
        "fetch('/wifi-scan',{method:'POST',cache:'no-store'})"
        ".then(function(){setTimeout(refreshNetworks,1500);})"
        ".catch(function(){})"
        ".finally(function(){setTimeout(function(){scanBtn.disabled=false;},1200);});"
        "});"
        "wifiSelect.addEventListener('change',function(){"
        "syncManualVisibility();"
        "});"
        "setupForm.addEventListener('submit',function(event){"
        "if(isOtherSelected()){"
        "if(ssidManual.value.trim().length===0){"
        "alert('Please enter Wi-Fi SSID.');"
        "event.preventDefault();"
        "return;"
        "}"
        "return;"
        "}"
        "ssidManual.value='';"
        "});"
        "refreshNetworks();"
        "if(autoRefreshStatus){refreshStatus();setInterval(refreshStatus,3000);}"
        "})();</script>";

    html += "</body></html>";

    return html;
}
