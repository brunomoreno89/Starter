// wwwroot/js/views/settings.js
import { auth, errorToString, changePassword } from '../api.js';
import { renderNavbar, renderUserArea } from '../components/navbar.js';

// Helpers
function base64UrlDecode(str) {
  try {
    str = str.replace(/-/g, '+').replace(/_/g, '/');
    const pad = str.length % 4;
    if (pad) str += '='.repeat(4 - pad);
    return atob(str);
  } catch { return ''; }
}
function parseJwt(token) {
  if (!token || token.split('.').length < 2) return null;
  try {
    const payload = token.split('.')[1];
    const json = base64UrlDecode(payload);
    return JSON.parse(json);
  } catch { return null; }
}

export async function SettingsView(container) {
  const claims = parseJwt(auth.token) || {};
  const exp = claims.exp ? new Date(claims.exp * 1000) : null;

  const username =
    claims.unique_name ||
    claims.name ||
    claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || '';

  const rolesArr = (() => {
    try { return typeof auth.roles === 'function' ? auth.roles() : (Array.isArray(auth.roles) ? auth.roles : []); }
    catch { return []; }
  })();
  const rolesStr = rolesArr.join(', ');

  const card = document.createElement('div');
  card.className = 'card';
  card.innerHTML = `<h2>Settings</h2>`;
  container.innerHTML = '';
  container.appendChild(card);

  const box = document.createElement('div');
  box.innerHTML = `
    <div class="grid">
      <div>
        <h3>Profile</h3>
        <label>Username</label>
        <input value="${username}" disabled />
        <label>Role(s)</label>
        <input value="${rolesStr}" disabled />
        <label>Token expires</label>
        <input value="${exp ? exp.toLocaleString() : 'n/a'}" disabled />
      </div>

      <div>
        <h3>Appearance</h3>
        <label>Theme</label>
        <select id="themeSel">
          <option value="dark">Dark</option>
          <option value="light">Light</option>
        </select>
        <div class="row" style="margin-top:8px;">
          <button class="primary" id="saveTheme">Save theme</button>
          <button class="secondary" id="resetUI">Reset UI state</button>
        </div>
        <div class="alert hidden" id="msgTheme"></div>

        <div style="margin-top:16px;">
          <h3>List Pagination</h3>
          <label>Rows per page</label>
          <select id="rowsPerPage">
            <option>5</option><option>10</option><option>20</option><option>50</option><option>100</option>
          </select>
          <div class="row" style="margin-top:8px;">
            <button class="primary" id="saveRows">Save rows/page</button>
          </div>
          <div class="alert hidden" id="msgRows"></div>
        </div>
      </div>
    </div>

    <div class="grid" style="margin-top:16px;">
      <div>
        <h3>Security</h3>
        <label>Current password</label>
        <input id="curPwd" type="password" placeholder="Enter current password">
        <label>New password</label>
        <input id="newPwd" type="password" placeholder="Enter new password (min 6)">
        <div class="row" style="margin-top:8px;">
          <button class="primary" id="btnChangePwd">Change password</button>
        </div>
        <div class="alert hidden" id="msgPwd"></div>
      </div>
    </div>
  `;
  card.appendChild(box);

  // Theme
  const themeSel = box.querySelector('#themeSel');
  themeSel.value = localStorage.getItem('theme') || 'dark';
  const msgTheme = box.querySelector('#msgTheme');
  const showThemeMsg = (t, err=false)=>{ msgTheme.className = 'alert ' + (err?'error':'success'); msgTheme.textContent = t; setTimeout(()=>{ msgTheme.className='alert hidden'; }, 2000); };
  box.querySelector('#saveTheme').onclick = () => {
    const v = themeSel.value;
    localStorage.setItem('theme', v);
    document.body.classList.toggle('light', v === 'light');
    renderNavbar(); renderUserArea();
    showThemeMsg('Theme saved.');
  };
  box.querySelector('#resetUI').onclick = () => {
    const token = localStorage.getItem('token') || '';
    const baseUrl = localStorage.getItem('baseUrl') || '';
    localStorage.clear();
    if (token) localStorage.setItem('token', token);
    if (baseUrl) localStorage.setItem('baseUrl', baseUrl);
    localStorage.setItem('theme','dark');
    document.body.classList.toggle('light', false);
    renderNavbar(); renderUserArea();
    showThemeMsg('UI state reset.');
  };

  // Rows per page
  const rowsSel  = card.querySelector('#rowsPerPage');
  const msgRows  = card.querySelector('#msgRows');
  const current = parseInt(localStorage.getItem('pageSize') || '10',10);
  rowsSel.value = String([5,10,20,50,100].includes(current) ? current : 10);
  const showRowsMsg = (t,err=false)=>{ msgRows.className='alert '+(err?'error':'success'); msgRows.textContent=t; setTimeout(()=>{ msgRows.className='alert hidden'; },1800); };
  card.querySelector('#saveRows')?.addEventListener('click', ()=>{
    const n = parseInt(rowsSel.value,10);
    if (!Number.isFinite(n) || n<5 || n>500) return showRowsMsg('Invalid value', true);
    localStorage.setItem('pageSize', String(n));
    showRowsMsg('Rows per page saved.');
  });

  // Password change
  const curPwd = box.querySelector('#curPwd');
  const newPwd = box.querySelector('#newPwd');
  const msgPwd = box.querySelector('#msgPwd');
  const showPwdMsg = (t, err=false)=>{ msgPwd.className='alert '+(err?'error':'success'); msgPwd.textContent=t; setTimeout(()=>{ msgPwd.className='alert hidden'; }, 2500); };
  box.querySelector('#btnChangePwd').onclick = async ()=>{
    msgPwd.className='alert hidden'; msgPwd.textContent='';
    const c = curPwd.value.trim(); const n = newPwd.value.trim();
    if (!c || !n) return showPwdMsg('Please fill both fields.', true);
    if (n.length < 6) return showPwdMsg('New password must be at least 6 characters.', true);
    try{ await changePassword(c,n); curPwd.value=''; newPwd.value=''; showPwdMsg('Password changed successfully.'); }
    catch(e){ showPwdMsg(e.friendly || errorToString(e), true); }
  };
}
