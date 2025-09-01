// wwwroot/js/api.js
// ---- JWT helpers ----
function decodeBase64Url(str){ str=str.replace(/-/g,'+').replace(/_/g,'/'); while (str.length%4) str+='='; try{ return atob(str); }catch{ return ''; } }
function parseJwt(token){
  try{
    const parts = token.split('.');
    if (parts.length !== 3) return {};
    const payload = decodeBase64Url(parts[1]);
    return JSON.parse(payload || '{}') || {};
  }catch{ return {}; }
}
/*
function rolesFromPayload(p){
  if (!p) return [];
  const msRole = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
  let roles = [];
  if (Array.isArray(p.role)) roles = p.role;
  else if (typeof p.role === 'string') roles = [p.role];
  else if (Array.isArray(p.roles)) roles = p.roles;
  else if (p[msRole]) roles = Array.isArray(p[msRole]) ? p[msRole] : [p[msRole]];
  return [...new Set(roles.map(r=>String(r).trim()))].filter(Boolean);
}
function permsFromPayload(p){
  if (!p) return [];
  if (Array.isArray(p.perms)) return p.perms;
  if (typeof p.perms === 'string') { try { const a = JSON.parse(p.perms); if (Array.isArray(a)) return a; } catch{} }
  return [];
}
  */


// Helpers (use em api.js)
function rolesFromPayload(p){
  if (!p) return [];
  const msRole = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
  let roles = [];

  if (Array.isArray(p.role)) roles = p.role;
  else if (typeof p.role === 'string') roles = [p.role];

  if (Array.isArray(p.roles)) roles = roles.concat(p.roles);
  else if (typeof p.roles === 'string') {
    // suporta roles como JSON string
    try { const arr = JSON.parse(p.roles); if (Array.isArray(arr)) roles = roles.concat(arr); } catch { roles.push(p.roles); }
  }

  if (p[msRole]) roles = roles.concat(Array.isArray(p[msRole]) ? p[msRole] : [p[msRole]]);

  return [...new Set(roles.map(r => String(r).trim()).filter(Boolean))];
}

function permsFromPayload(p){
  if (!p) return [];
  const keys = ['perm', 'perms', 'permission', 'permissions'];
  const out = new Set();

  for (const k of keys){
    const v = p[k];
    if (!v) continue;

    if (Array.isArray(v)) {
      v.forEach(x => x && out.add(String(x).trim()));
      continue;
    }

    if (typeof v === 'string') {
      // tenta JSON array; senão, aceita CSV
      try { const arr = JSON.parse(v); if (Array.isArray(arr)) { arr.forEach(x => x && out.add(String(x).trim())); continue; } } catch {}
      v.split(',').forEach(x => x && out.add(x.trim()));
      continue;
    }

    if (typeof v === 'object') {
      Object.values(v).forEach(x => x && out.add(String(x).trim()));
    }
  }

  return [...out];
}


function isTokenExpired(token){
  const p = parseJwt(token); if (!p || !p.exp) return false; const now = Math.floor(Date.now()/1000); return p.exp <= now;
}
// ---- Config ----
function basePath(){ try{ return new URL(document.baseURI).pathname.replace(/\/$/,'')||'' }catch{ return '' } }
export const cfg = { get baseUrl(){ return localStorage.getItem('baseUrl') || `${basePath()}/api`; }, set baseUrl(v){ localStorage.setItem('baseUrl', v); } };

// ---- Auth session ----
/*
export const auth = {
  saveToken(raw){
    const clean = (raw || '').replace(/^Bearer\s+/i, '').trim();   // <-- remove "Bearer "
    localStorage.setItem('token', clean);
  },
  get token(){
    const t = localStorage.getItem('token') || '';
    return t.replace(/^Bearer\s+/i, '').trim();                     // <-- sanitize ao ler
  },
  get claims(){ return parseJwt(this.token); },
  get exp(){ const { exp } = this.claims || {}; return typeof exp==='number' ? exp : null; },
  isExpired(){ if(!this.token) return true; if(!this.exp) return false; return Date.now() >= this.exp*1000; },

  // roles/perms robustos
  roles(){
    const c=this.claims||{};
    const std=c['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    const alt=c['role'];
    const out=[];
    if (Array.isArray(std)) out.push(...std); else if (std) out.push(std);
    if (Array.isArray(alt)) out.push(...alt); else if (alt) out.push(alt);
    const j=c['roles']; // pode vir como JSON string
    if (typeof j==='string') { try{ const arr=JSON.parse(j); if(Array.isArray(arr)) out.push(...arr); }catch{} }
    return Array.from(new Set(out));
  },
  hasRole(...names){ const rs=this.roles(); return rs.some(r => names.includes(r)); },

  perms(){
    const c=this.claims||{};
    let p = c['perm'] ?? c['perms'];
    if (!p) return [];
    if (Array.isArray(p)) return p;
    if (typeof p === 'string') {
      try { const arr = JSON.parse(p); if (Array.isArray(arr)) return arr; } catch {}
      return p.split(',').map(s=>s.trim()).filter(Boolean);
    }
    if (typeof p === 'object') { try { return Object.values(p).map(String); } catch{} }
    return [];
  },
  hasPerm(name){ try { return this.perms().includes(name); } catch { return false; } },

  clear(){ localStorage.removeItem('token'); }
};

export function isSessionValid(){ return !!auth.token && !auth.isExpired(); }
*/

// ---- Helpers genéricos para normalizar arrays/strings/CSV/JSON ----
function _collect(out, v) {
  if (!v && v !== 0) return;
  if (Array.isArray(v)) { v.forEach(x => _collect(out, x)); return; }
  if (typeof v === 'string') {
    // Tenta JSON array; senão aceita CSV; senão valor único
    try { const arr = JSON.parse(v); if (Array.isArray(arr)) { arr.forEach(x => _collect(out, x)); return; } } catch {}
    v.split(',').forEach(s => { s = s.trim(); if (s) out.add(s); });
    return;
  }
  out.add(String(v).trim());
}

function _rolesFromClaims(p){
  if (!p) return [];
  const out = new Set();
  const MS_ROLE = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
  _collect(out, p.role);
  _collect(out, p.roles);   // <- agora lê array, JSON string, CSV ou string única
  _collect(out, p[MS_ROLE]);
  return [...out];
}

function _permsFromClaims(p){
  if (!p) return [];
  const out = new Set();
  const KEYS = ['perm', 'perms', 'permission', 'permissions'];
  for (const k of KEYS) _collect(out, p[k]);  // <- aceita array, JSON string, CSV, objeto
  return [...out];
}

// ---- AUTH (drop-in) ----
export const auth = {
  saveToken(raw){
    const clean = (raw || '').replace(/^Bearer\s+/i, '').trim();
    localStorage.setItem('token', clean);
  },
  get token(){
    const t = localStorage.getItem('token') || '';
    return t.replace(/^Bearer\s+/i, '').trim();
  },
  get claims(){ return parseJwt(this.token); },
  get exp(){ const { exp } = this.claims || {}; return typeof exp === 'number' ? exp : null; },
  isExpired(){ if(!this.token) return true; if(!this.exp) return false; return Date.now() >= this.exp * 1000; },

  // ---- Roles / Perms robustos ----
  roles(){ return _rolesFromClaims(this.claims); },
  hasRole(...names){
    const set = new Set(this.roles().map(r => String(r).toLowerCase()));
    return names.some(n => set.has(String(n).toLowerCase()));
  },

  perms(){ return _permsFromClaims(this.claims); },
  hasPerm(name){
    const set = new Set(this.perms().map(p => String(p).toLowerCase()));
    return set.has(String(name).toLowerCase());
  },

  clear(){ localStorage.removeItem('token'); },

  // QoL para debug
  debug(){ console.log('claims:', this.claims); console.log('roles:', this.roles()); console.log('perms:', this.perms()); }
};

export function isSessionValid(){ return !!auth.token && !auth.isExpired(); }



// ---- Error formatting ----
export function errorToString(err){
  try{
    if (!err) return 'Unknown error';
    if (typeof err) return err.message || String(err);
  }catch{}
  try{
    if (typeof err.body === 'string') return err.body;
    if (err.body && typeof err.body === 'object'){
      const b = err.body;
      if (b.errors && typeof b.errors === 'object'){
        const parts=[]; for (const [k,vals] of Object.entries(b.errors)){ if (Array.isArray(vals)&&vals.length) parts.push(`${k}: ${vals.join(', ')}`) }
        if (parts.length) return parts.join(' | ');
      }
      if (Array.isArray(b) && b.length && (b[0].errorMessage || b[0].message || b[0].PropertyName)){
        return b.map(x=>(x.propertyName||x.PropertyName||'Error')+': '+(x.errorMessage||x.message||JSON.stringify(x))).join(' | ');
      }
      if (b.detail) return b.detail;
      if (b.title) return b.title;
      if (b.message) return b.message;
      return JSON.stringify(b);
    }
    return err.message || 'Unexpected error';
  }catch{ return 'Unexpected error'; }
}


// ---- HTTP helper ----
function join(base, path){ return base.replace(/\/+$/,'') + '/' + path.replace(/^\/+/, ''); }
async function fetchJSON(path, { method='GET', body, headers={} } = {}){
  const url = join(cfg.baseUrl, path);
  const h = { 'Accept':'application/json', 'Content-Type':'application/json', ...headers };
  if (auth.token) h['Authorization'] = 'Bearer ' + auth.token;
  const res = await fetch(url, { method, headers:h, body: body?JSON.stringify(body):undefined });
  const text = await res.text(); let data=null; try{ data = text?JSON.parse(text):null }catch{ data=text }
  if (!res.ok){ const err = new Error('HTTP '+res.status); err.status=res.status; err.body=data; try{err.friendly=errorToString(err)}catch{}; throw err; }
  return data;
}

// ---- API: Auth & Settings ----
export async function login(username,password){ return fetchJSON('/auth/login', { method:'POST', body:{ username,password } }); }
export async function changePassword(currentPassword, newPassword){ return fetchJSON('/auth/change-password', { method:'POST', body:{ currentPassword, newPassword } }); }

// ---- API: Items ----
export async function listItems(){ return fetchJSON('/items'); }
export async function getItem(id){ return fetchJSON('/items/'+id); }
export async function createItem(dto){ return fetchJSON('/items',{method:'POST', body:dto}); }
export async function updateItem(id,dto){ return fetchJSON('/items/'+id,{method:'PUT', body:dto}); }
export async function deleteItem(id){ return fetchJSON('/items/'+id,{method:'DELETE'}); }

// ---- API: Permissions ----
export async function listPermissions(){ return fetchJSON('/permissions'); }
export async function getPermission(id){ return fetchJSON('/permissions/'+id); }
export async function createPermission(dto){ return fetchJSON('/permissions',{ method:'POST', body: dto }); }
export async function updatePermission(id, dto){ return fetchJSON('/permissions/'+id,{ method:'PUT', body: dto }); }
export async function deletePermission(id){ return fetchJSON('/permissions/'+id,{ method:'DELETE' }); }

// ---- API: Roles ----
export async function listRoles(){ return fetchJSON('/roles'); }
export async function getRole(id){ return fetchJSON('/roles/'+id); }
export async function createRole(dto){ return fetchJSON('/roles',{ method:'POST', body: dto }); }
export async function updateRole(id, dto){ return fetchJSON('/roles/'+id,{ method:'PUT', body: dto }); }
export async function deleteRole(id){ return fetchJSON('/roles/'+id,{ method:'DELETE' }); }

// ---- API: Role × Permission (amarração) ----
export async function listRolePermissions(roleId){ return fetchJSON('/rolepermissions/'+roleId); }
export async function assignRolePermissions(dto){ return fetchJSON('/rolepermissions/assign',{ method:'POST', body: dto }); }

// ---- API: User × Role (amarração) ----
export async function listUserRoles(userId){ return fetchJSON('/userroles/'+userId); }
export async function assignUserRoles(dto){ return fetchJSON('/userroles/assign',{ method:'POST', body: dto }); }

// ---- API: Users ----
export async function createUser(dto){ return fetchJSON('/users', { method:'POST', body:dto }); } // { username,email,role,password }
export async function listUsers(){ return fetchJSON('/users'); }
export async function getUser(id){ return fetchJSON('/users/'+id); }
export async function updateUser(id,dto){ return fetchJSON('/users/'+id,{method:'PUT', body:dto}); }
export async function deleteUser(id){ return fetchJSON('/users/'+id,{method:'DELETE'}); }

// ---- API: Logs ----
// --- APPEND EM api.js ---

// Helper simples de querystring: datas como YYYY-MM-DD (sem toISOString)
function qs(params){
  const q = new URLSearchParams();
  for (const [k, v] of Object.entries(params || {})){
    if (v == null || v === '') continue;
    if (v instanceof Date){
      const y = v.getFullYear();
      const m = String(v.getMonth()+1).padStart(2,'0');
      const d = String(v.getDate()).padStart(2,'0');
      q.append(k, `${y}-${m}-${d}`); // envia só a data local
    } else {
      q.append(k, v);
    }
  }
  const s = q.toString();
  return s ? ('?' + s) : '';
}

// ---- API: Logs ----
export async function listLogs({ User, StartDate, EndDate } = {}){
  return fetchJSON('/logs' + qs({ User, StartDate, EndDate }));
}

// ---- API: Users (leve) ----
export async function listUsersLight(){
  const users = await listUsers(); // seu endpoint padrão, já autenticado
  return (Array.isArray(users) ? users : []).map(u => ({
    id: u.id ?? u.Id ?? u.userId ?? u.UserId,
    username: u.username ?? u.Username ?? u.login ?? u.Login ?? '',
    name: u.name ?? u.Name ?? u.fullName ?? u.FullName ?? ''
  }));
}

// pega filename do header Content-Disposition, se existir
function _filenameFromContentDisposition(h){
  const cd = h.get('Content-Disposition') || h.get('content-disposition');
  if (!cd) return null;
  // exemplos: attachment; filename="logs_20250831_235959.csv"; filename*=UTF-8''...
  const m1 = cd.match(/filename\*=(?:UTF-8'')?([^;]+)/i);
  if (m1 && m1[1]) return decodeURIComponent(m1[1].replace(/^"+|"+$/g,''));
  const m2 = cd.match(/filename="?([^"]+)"?/i);
  if (m2 && m2[1]) return m2[1];
  return null;
}

// fetch de arquivo (blob) com Authorization (segue padrão do projeto)
async function fetchFile(path, { accept = 'text/csv' } = {}){
  const url = join(cfg.baseUrl, path);
  const headers = { 'Accept': accept };
  if (auth.token) headers['Authorization'] = 'Bearer ' + auth.token;

  const res = await fetch(url, { method: 'GET', headers });
  const blob = await res.blob();

  if (!res.ok){
    const err = new Error('HTTP '+res.status);
    err.status = res.status;
    // tenta montar friendly a partir do corpo (se for json/text)
    try{
      const text = await blob.text();
      err.body = text;
      err.friendly = text || ('HTTP '+res.status);
    }catch{}
    throw err;
  }

  const filename = _filenameFromContentDisposition(res.headers);
  return { blob, filename };
}

// ---- API: Logs (substitua sua função export antiga por esta) ----
export async function exportLogsCsv({ User, StartDate, EndDate } = {}){
  // monte a QS igual à listagem (datas em YYYY-MM-DD)
  const params = new URLSearchParams();
  if (User) params.append('User', User);
  if (StartDate instanceof Date){
    const y=StartDate.getFullYear(), m=String(StartDate.getMonth()+1).padStart(2,'0'), d=String(StartDate.getDate()).padStart(2,'0');
    params.append('StartDate', `${y}-${m}-${d}`);
  }
  if (EndDate instanceof Date){
    const y=EndDate.getFullYear(), m=String(EndDate.getMonth()+1).padStart(2,'0'), d=String(EndDate.getDate()).padStart(2,'0');
    params.append('EndDate', `${y}-${m}-${d}`);
  }

  const path = '/logs/export.csv' + (params.toString() ? ('?'+params.toString()) : '');
  const { blob, filename } = await fetchFile(path, { accept: 'text/csv' });

  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename || 'logs.csv';
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}


