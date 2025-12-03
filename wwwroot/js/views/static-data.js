// wwwroot/js/views/access.js
import {
  // Users
  listUsers, createUser, getUser, updateUser, deleteUser,
  // Roles
  listRoles, createRole, updateRole, deleteRole,
  // Permissions
  listPermissions, createPermission, updatePermission, deletePermission,
  // Role x Permission
  listRolePermissions, assignRolePermissions,
  // User x Role
  listUserRoles, assignUserRoles,
  // util
  errorToString, auth
} from '../api.js';
import { getPageSize, paginate, renderPager } from '../ui/pager.js';
import { formatDateToBR } from '../utils/date.js';
import { openModal, openConfirm } from '../ui/modal.js';

const can = (perm) => auth.hasPerm(perm) || auth.hasRole('Admin');

function icon(name){
  if (name==='edit')  return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>`;
  if (name==='trash') return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6M14 11v6"/><path d="M9 6V4a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2"/></svg>`;
  if (name==='plus')  return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 5v14M5 12h14"/></svg>`;
  return '';
}

function showMsgFromPane(pane, text, isError=false){
  const root = pane.closest('.card') || document;
  const el = root.querySelector('#ac-msg'); if(!el) return;
  el.className = 'alert ' + (isError ? 'error' : 'success');
  el.textContent = text;
  setTimeout(()=>{ el.className='alert hidden'; }, 2000);
}

export async function StaticDataView(container){
  container.innerHTML = `
    <div class="card">
      <div class="view-header">
        <h2 class="view-title">Access Control</h2>
      </div>

      <div class="tabs" id="ac-tabs"></div>
      <div id="ac-msg" class="alert hidden" style="margin-top:8px;"></div>

      <div id="pane-regions"     class="ac-pane hidden"></div>
      <div id="pane-branches"    class="ac-pane hidden"></div>
      <div id="pane-holidays"    class="ac-pane hidden"></div>
    </div>
  `;

  const TABS = [
    { key:'regions',        label:'Regions',              perm:'Regions.Read',             render: renderUsers },
    { key:'branches',        label:'Branches',              perm:'Branches.Read',             render: renderRoles },
    { key:'holidays',  label:'Holidays',              perm:'Holidays.Read',       render: renderPermissions },
    
  ];

  const tabsHost = container.querySelector('#ac-tabs');
  const visibleTabs = TABS.filter(t => can(t.perm));
  if (visibleTabs.length === 0){
    tabsHost.innerHTML = `<div class="alert error">You don't have permission to access this area.</div>`;
    return;
  }
  tabsHost.innerHTML = '';
  for (const t of visibleTabs){
    const b = document.createElement('button');
    b.type = 'button';
    b.className = 'tab';
    b.dataset.key = t.key;
    b.textContent = t.label;
    b.addEventListener('click', ()=> activateTab(t.key));
    tabsHost.appendChild(b);
  }

  await activateTab(visibleTabs[0].key);

  async function activateTab(key){
    tabsHost.querySelectorAll('.tab').forEach(btn=>{
      btn.classList.toggle('active', btn.dataset.key === key);
    });
    container.querySelectorAll('.ac-pane').forEach(p => p.classList.add('hidden'));
    const paneId = {
      users:'pane-users', roles:'pane-roles', permissions:'pane-permissions',
      roleperms:'pane-roleperms', userroles:'pane-userroles'
    }[key];
    const pane = container.querySelector('#'+paneId);
    pane.classList.remove('hidden');

    const tab = TABS.find(t => t.key === key);
    if (!tab) return;
    try{
      await tab.render(pane, container);
    }catch(e){
      showMsgFromPane(pane, errorToString(e), true);
      pane.innerHTML = `<div class="alert error">${errorToString(e)}</div>`;
    }
  }
}

/* ---------- REGIONS (com Active) ---------- */
async function renderUsers(pane){
  const allowCreate = can('Regions.Create');
  const allowUpdate = can('Regions.Update');
  const allowDelete = can('Regions.Delete');

  pane.innerHTML = `
    <div class="view-header">
      <h3>Users</h3>
      ${allowCreate?`<button class="icon-btn" id="uNew" title="New user">${icon('plus')}</button>`:''}
    </div>
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Description</th>
            <th style="width:110px; text-align:right;"></th>
          </tr>
        </thead>
        <tbody id="uBody"></tbody>
      </table>
    </div>
    <div id="uPager"></div>
  `;

  let page = 1;
  let data = [];
  async function reload(){
    try{ data = await listUsers(); }catch(e){ showMsgFromPane(pane, errorToString(e), true); data = []; }
    fill();
  }
  function fill(){
    const tb = pane.querySelector('#uBody'); tb.innerHTML='';
    const { items, total, pages } = paginate(data, page, getPageSize());
    for (const r of items){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${r.id}</td>
        <td>${r.description||''}</td>
    
        <td style="text-align:right;">
          ${allowUpdate?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${allowDelete?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;
      const btnE = tr.querySelector('[data-act="edit"]');
      const btnD = tr.querySelector('[data-act="del"]');

      if (btnE) btnE.onclick = async ()=>{
        let current = u; try{ current = await getUser(u.id); }catch{}
        openModal(
          'Edit user #'+u.id,
          (body)=>{
            body.innerHTML = `
              <label>Username</label>
              <input id="mUUserName" value="${(current.username||'').replace(/"/g,'&quot;')}">
              <label>Name</label>
              <input id="mUName" type="name" value="${(current.name||'').replace(/"/g,'&quot;')}">
              <label>Email</label>
              <input id="mUEmail" type="email" value="${(current.email||'').replace(/"/g,'&quot;')}">
              <label for="mUActive">Active</label>
              <select id="mUActive">
                <option value="Yes" ${current.active === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No"  ${current.active === 'No'  ? 'selected' : ''}>No</option>
              </select>`;
          },
          async ()=>{
            const username = document.getElementById('mUUserName').value.trim();
            const name     = document.getElementById('mUName').value.trim();
            const email    = document.getElementById('mUEmail').value.trim();
            const active   = document.getElementById('mUActive').value.trim();
            if (!username) throw new Error('Username is required');
            await updateUser(u.id, { id: u.id, username, name, email, active });
            showMsgFromPane(pane,'User updated.'); await reload();
          },
          { confirmText: 'Save changes' }
        );
      };

      if (btnD) btnD.onclick = ()=>{
        openConfirm(
          'Confirm deletion',
          `<p>Are you sure you want to delete user <strong>${u.username||('User#'+u.id)}</strong>?</p>
           <p style="font-size:smaller;color:#666;">This will deactivate the user (soft delete).</p>`,
          async () => {
            await deleteUser(u.id);
            showMsgFromPane(pane,'User deleted.');
            await reload();
          },
          { confirmText: 'Delete', confirmClass: 'danger' }
        );
      };

      tb.appendChild(tr);
    }
    renderPager(pane.querySelector('#uPager'), { total, page, pages }, (p)=>{ page=p; fill(); });
  }

  const addBtn = pane.querySelector('#uNew');
  if (addBtn) addBtn.onclick = ()=>{
    openModal(
      'New user',
      (body)=>{
        body.innerHTML = `
          <label>Username</label><input id="mUUserName" placeholder="username">
          <label>Complete Name</label><input id="mUName" placeholder="name">
          <label>Email</label><input id="nUEmail" type="email" placeholder="email@example.com">
          <label>Password</label><input id="nUPwd" type="password" placeholder="initial password">
          <label for="nUActive">Active</label>
            <select id="nUActive">
              <option value="Yes">Yes</option>
              <option value="No">No</option>
            </select>`;
      },
      async ()=>{
        const username = document.getElementById('mUUserName').value.trim();
        const name     = document.getElementById('mUName').value.trim();
        const email    = document.getElementById('nUEmail').value.trim();
        const password = document.getElementById('nUPwd').value.trim();
        const active   = document.getElementById('nUActive').value.trim();

        if (!username) throw new Error('Username is required');
        if (!password) throw new Error('Password is required');

        await createUser({ username, name, email, password, active });
        showMsgFromPane(pane,'User created.'); await reload();
      },
      { confirmText: 'Create user' }
    );
  };

  await reload();
}

/* ---------- ROLES (com paginação) ---------- */
async function renderRoles(pane){
  const allowCreate = can('Roles.Create');
  const allowUpdate = can('Roles.Update');
  const allowDelete = can('Roles.Delete');

  pane.innerHTML = `
    <div class="view-header">
      <h3>Roles</h3>
      ${allowCreate?`<button class="icon-btn" id="rNew" title="New role">${icon('plus')}</button>`:''}
    </div>
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Description</th>
            <th>Active</th>
            <th>Creation Dt</th>
            <th>Created By</th>
            <th>Update Dt</th>
            <th>Updated By</th>
            <th style="width:110px; text-align:right;"></th>
          </tr>
          </thead>
        <tbody id="rBody"></tbody>
      </table>
    </div>
    <div id="rPager"></div>
  `;

  let page = 1, roles=[];
  async function reload(){ try{ roles = await listRoles(); }catch(e){ showMsgFromPane(pane, errorToString(e), true); roles=[]; } fill(); }
  function fill(){
    const tb = pane.querySelector('#rBody'); tb.innerHTML='';
    const { items, total, pages } = paginate(roles, page, getPageSize());
    for (const r of items){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${r.id}</td>
        <td>${r.name||''}</td>
        <td>${r.description||''}</td>
        <td>${r.active||''}</td>
        <td>${formatDateToBR(r.creationDt)}</td>
        <td>${r.createdByName ?? ''}</td>
        <td>${formatDateToBR(r.updateDt)}</td>
        <td>${r.updatedByName ?? ''}</td>
        <td style="text-align:right;">
          ${allowUpdate?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${allowDelete?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;
      tr.querySelector('[data-act="edit"]')?.addEventListener('click', ()=>{
        openModal(
          'Edit role #'+r.id,
          (body)=>{
            body.innerHTML = `
              <label>Name</label>
              <input id="mRName" value="${(r.name||'').replace(/"/g,'&quot;')}">
              <label>Description</label>
              <textarea id="mRDesc" rows="3">${r.description||''}</textarea>
              <label for="mRActive">Active</label>
              <select id="mRActive">
                <option value="Yes" ${r.active === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No"  ${r.active === 'No'  ? 'selected' : ''}>No</option>
              </select>`;
          },
          async ()=>{
            const name = document.getElementById('mRName').value.trim();
            const description = document.getElementById('mRDesc').value.trim();
            const active = document.getElementById('mRActive').value.trim();
            if (!name) throw new Error('Name is required');
            await updateRole(r.id, { id:r.id, name, description, active });
            showMsgFromPane(pane,'Role updated.'); await reload();
          },
          { confirmText: 'Save changes' }
        );
      });
      tr.querySelector('[data-act="del"]')?.addEventListener('click', ()=>{
        openConfirm(
          'Delete role',
          `<p>Delete role <strong>${r.name}</strong>?</p>`,
          async ()=>{
            await deleteRole(r.id);
            showMsgFromPane(pane,'Role deleted.'); await reload();
          },
          { confirmText: 'Delete', confirmClass: 'danger' }
        );
      });
      tb.appendChild(tr);
    }
    renderPager(pane.querySelector('#rPager'), { total, page, pages }, (p)=>{ page=p; fill(); });
  }

  const addBtn = pane.querySelector('#rNew');
  if (addBtn) addBtn.onclick = ()=>{
    openModal(
      'New role',
      (body)=>{
        body.innerHTML = `
          <label>Name</label><input id="nRName" placeholder="Role name">
          <label>Description</label><textarea id="nRDesc" rows="3" placeholder="Description (optional)"></textarea>`;
      },
      async ()=>{
        const name = document.getElementById('nRName').value.trim();
        const description = document.getElementById('nRDesc').value.trim();
        if (!name) throw new Error('Name is required');
        await createRole({ name, description });
        showMsgFromPane(pane,'Role created.'); await reload();
      },
      { confirmText: 'Create role' }
    );
  };

  await reload();
}

/* ---------- PERMISSIONS (subtabs + paginação) ---------- */
async function renderPermissions(pane){
  const allowCreate = can('Permissions.Create');
  const allowUpdate = can('Permissions.Update');
  const allowDelete = can('Permissions.Delete');

  pane.innerHTML = `
    <div class="view-header">
      <h3>Permissions</h3>
      ${allowCreate?`<button class="icon-btn" id="pNew" title="New permission">${icon('plus')}</button>`:''}
    </div>

    <div id="perm-tabs" class="tabs" style="margin-top:6px;"></div>

    <div class="table-wrap" style="margin-top:8px;">
      <table class="table">
        <thead>
          <tr>
            <th style="width:72px;">ID</th>
            <th>Permission</th>
            <th>Description</th>
            <th>Active</th>
            <th>Creation Dt</th>
            <th>Created By</th>
            <th>Update Dt</th>
            <th>Updated By</th>
            <th style="width:110px; text-align:right;"></th>
          </tr>
        </thead>
        <tbody id="pBody"></tbody>
      </table>
    </div>
    <div id="pPager"></div>
  `;

  let allPerms = [], groupMap = new Map(), groups = [], activeGroup = 'General', page = 1;

  function buildGroups(){
    groupMap = new Map();
    for (const p of allPerms){
      const name = String(p.name || '');
      const dot = name.indexOf('.');
      const g = dot > 0 ? name.slice(0, dot) : (name || 'General');
      if (!groupMap.has(g)) groupMap.set(g, []);
      groupMap.get(g).push(p);
    }
    groups = Array.from(groupMap.keys()).sort((a,b)=>a.localeCompare(b));
    if (!groups.length) groups = ['General'];
    if (!groupMap.has(activeGroup)) activeGroup = groups[0];
  }

  function paintTabs(){
    const host = pane.querySelector('#perm-tabs');
    host.innerHTML = '';
    for (const g of groups){
      const b = document.createElement('button');
      b.type='button';
      b.className = 'tab' + (g===activeGroup ? ' active' : '');
      b.textContent = g;
      b.addEventListener('click', ()=>{
        activeGroup = g; page = 1;
        paintTabs(); fillTable();
      });
      host.appendChild(b);
    }
  }

  function permsOfActive(){
    const list = (groupMap.get(activeGroup) || []).slice();
    list.sort((a,b)=> String(a.name||'').localeCompare(String(b.name||'')));
    return list;
  }

  function fillTable(){
    const tb = pane.querySelector('#pBody'); tb.innerHTML = '';
    const list = permsOfActive();
    const { items, total, pages } = paginate(list, page, getPageSize());

    for (const p of items){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${p.id}</td>
        <td>${p.name || ''}</td>
        <td>${p.description || ''}</td>
        <td>${p.active}</td>
        <td>${formatDateToBR(p.creationDt)}</td>
        <td>${p.createdByName ?? ''}</td>
        <td>${formatDateToBR(p.updateDt)}</td>
        <td>${p.updatedByName ?? ''}</td>
        <td style="text-align:right;">
          ${allowUpdate?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${allowDelete?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>
      `;

      tr.querySelector('[data-act="edit"]')?.addEventListener('click', ()=>{
        openModal(
          'Edit permission #'+p.id,
          (body)=>{
            body.innerHTML = `
              <label>Name</label>
              <input id="mPName" value="${(p.name||'').replace(/"/g,'&quot;')}">
              <label>Description</label>
              <textarea id="mPDesc" rows="3">${p.description||''}</textarea>
              <label for="mPActive">Active</label>
              <select id="mPActive">
                <option value="Yes" ${p.active === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No"  ${p.active === 'No'  ? 'selected' : ''}>No</option>
              </select>
              `;
          },
          async ()=>{
            const name = document.getElementById('mPName').value.trim();
            const description = document.getElementById('mPDesc').value.trim();
            const active = document.getElementById('mPActive').value.trim();
            if (!name) throw new Error('Name is required');
            await updatePermission(p.id, { id:p.id, name, description, active });
            await reload(); showMsgFromPane(pane,'Permission updated.');
          },
          { confirmText: 'Save changes' }
        );
      });

      tr.querySelector('[data-act="del"]')?.addEventListener('click', ()=>{
        openConfirm(
          `Delete permission`,
          `<p>Delete permission <strong>${p.name}</strong>?</p>`,
          async ()=>{
            await deletePermission(p.id);
            await reload();
            showMsgFromPane(pane,'Permission deleted.');
          },
          { confirmText: 'Delete', confirmClass: 'danger' }
        );
      });

      tb.appendChild(tr);
    }

    renderPager(pane.querySelector('#pPager'), { total, page, pages }, (p)=>{ page=p; fillTable(); });
  }

  async function reload(){
    try{ allPerms = await listPermissions(); }catch(e){ showMsgFromPane(pane, errorToString(e), true); allPerms = []; }
    buildGroups(); paintTabs(); fillTable();
  }

  const addBtn = pane.querySelector('#pNew');
  if (addBtn) addBtn.onclick = ()=>{
    const defaultName = activeGroup && activeGroup !== 'General' ? `${activeGroup}.` : '';
    openModal(
      'New permission',
      (body)=>{
        body.innerHTML = `
          <label>Name</label><input id="nPName" placeholder="e.g. Items.Read" value="${defaultName}">
          <label>Description</label><textarea id="nPDesc" rows="3" placeholder="Description (optional)"></textarea>`;
      },
      async ()=>{
        const name = document.getElementById('nPName').value.trim();
        const description = document.getElementById('nPDesc').value.trim();
        if (!name) throw new Error('Name is required');
        await createPermission({ name, description });
        await reload(); showMsgFromPane(pane,'Permission created.');
      },
      { confirmText: 'Create permission' }
    );
  };

  await reload();
}

/* ---------- ROLE × PERMISSIONS (grupos + paginação) ---------- */
async function renderRolePermissions(pane){
  if (!can('RolePermissions.Assign')){
    pane.innerHTML = `<div class="alert error">Sem permissão.</div>`;
    return;
  }

  pane.innerHTML = `
    <div class="view-header" style="gap:12px; flex-wrap:wrap; align-items:center;">
      <h3>Role × Permissions</h3>
      <label style="margin-left:auto; display:flex; align-items:center; gap:8px;">
        <span>Role</span>
        <select id="rp-role" style="min-height:32px;"></select>
      </label>
    </div>
    <div id="rp-tabs" class="tabs" style="margin-top:6px;"></div>
    <div id="rp-grid" style="margin-top:10px;"></div>
    <div style="margin-top:12px;">
      <button class="primary" id="rp-save">Save</button>
    </div>
  `;

  const roles = await listRoles();
  const allPerms = await listPermissions();

  const groupMap = new Map();
  for (const p of allPerms) {
    const name = String(p.name || '');
       const dot = name.indexOf('.');
    const group = dot > 0 ? name.slice(0, dot) : (name || 'General');
    if (!groupMap.has(group)) groupMap.set(group, []);
    groupMap.get(group).push(p);
  }
  const groups = Array.from(groupMap.keys()).sort((a,b)=>a.localeCompare(b));

  let activeRoleId = roles?.[0]?.id ?? null;
  let activeGroup  = groups[0] || 'General';
  let selected = new Set();
  let page = 1;

  const selRole = pane.querySelector('#rp-role');
  selRole.innerHTML = roles.map(r => `<option value="${r.id}" ${r.id===activeRoleId?'selected':''}>${r.name}</option>`).join('');
  selRole.onchange = async () => { activeRoleId = Number(selRole.value); await loadRolePerms(); renderGrid(); };

  const tabsHost = pane.querySelector('#rp-tabs');
  function paintGroupTabs() {
    tabsHost.innerHTML = '';
    for (const g of groups) {
      const b = document.createElement('button');
      b.type='button';
      b.className = 'tab' + (g===activeGroup ? ' active' : '');
      b.textContent = g;
      b.addEventListener('click', ()=>{
        activeGroup = g; page = 1;
        paintGroupTabs();
        renderGrid();
      });
      tabsHost.appendChild(b);
    }
  }

  async function loadRolePerms(){
    if (!activeRoleId){ selected = new Set(); return; }
    const rp = await listRolePermissions(activeRoleId);
    const ids = rp?.map?.(x => x.id ?? x.permissionId ?? x) ?? [];
    selected = new Set(ids.map(Number));
  }

  function renderGrid(){
    const wrap = pane.querySelector('#rp-grid');
    wrap.innerHTML = `
      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th style="width:64px;">Allow</th>
              <th>Permission</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody id="rp-tbody"></tbody>
        </table>
      </div>
      <div id="rpPager"></div>
    `;

    const permsOfGroup = (groupMap.get(activeGroup) || []).slice()
      .sort((a,b)=> String(a.name||'').localeCompare(String(b.name||'')));

    const { items, total, pages } = paginate(permsOfGroup, page, getPageSize());
    const tb = wrap.querySelector('#rp-tbody');

    for (const p of items){
      const tr = document.createElement('tr');
      const checked = selected.has(p.id) ? 'checked' : '';
      tr.innerHTML = `
        <td><input type="checkbox" data-id="${p.id}" ${checked}></td>
        <td>${p.name || ''}</td>
        <td>${p.description || ''}</td>
      `;
      tr.querySelector('input[type="checkbox"]').addEventListener('change', (e)=>{
        const pid = Number(e.target.getAttribute('data-id'));
        if (e.target.checked) selected.add(pid); else selected.delete(pid);
      });
      tb.appendChild(tr);
    }

    renderPager(pane.querySelector('#rpPager'), { total, page, pages }, (p)=>{ page=p; renderGrid(); });
  }

  async function save(){
    try{
      if (!activeRoleId) return;
      const dto = { roleId: activeRoleId, permissionIds: Array.from(selected) };
      await assignRolePermissions(dto);
      showMsgFromPane(pane,'Saved.');
    }catch(e){ showMsgFromPane(pane, errorToString(e), true); }
  }

  pane.querySelector('#rp-save').addEventListener('click', save);

  paintGroupTabs();
  await loadRolePerms();
  renderGrid();
}

/* ---------- USER × ROLES (alinhado + paginação) ---------- */
async function renderUserRoles(pane){
  if (!can('UserRoles.Assign')){
    pane.innerHTML = `<div class="alert error">Sem permissão.</div>`;
    return;
  }

  pane.innerHTML = `
    <div class="view-header" style="gap:12px; flex-wrap:wrap; align-items:center;">
      <h3>User × Roles</h3>
      <label style="margin-left:auto; display:flex; align-items:center; gap:8px;">
        <span>User</span>
        <select id="ur-user" style="min-height:32px;"></select>
      </label>
    </div>
    <div id="ur-grid" style="margin-top:10px;"></div>
    <div style="margin-top:12px;">
      <button class="primary" id="ur-save">Save</button>
    </div>
  `;

  const users = await listUsers();
  const roles = (await listRoles()).slice().sort((a,b)=>String(a.name||'').localeCompare(String(b.name||'')));

  let activeUserId = users?.[0]?.id ?? null;
  let selected = new Set();
  let page = 1;

  const sel = pane.querySelector('#ur-user');
  sel.innerHTML = users.map(u =>
    `<option value="${u.id}" ${u.id===activeUserId?'selected':''}>${u.username || u.email || ('User#'+u.id)}</option>`
  ).join('');
  sel.onchange = async () => { activeUserId = Number(sel.value); await loadUserRoles(); renderGrid(); };

  async function loadUserRoles(){
    if (!activeUserId){ selected = new Set(); return; }
    const ur = await listUserRoles(activeUserId);
    const ids = ur?.map?.(x => x.id ?? x.roleId ?? x) ?? [];
    selected = new Set(ids.map(Number));
  }

  function renderGrid(){
    const wrap = pane.querySelector('#ur-grid');
    wrap.innerHTML = `
      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th style="width:64px;">Allow</th>
              <th>Role</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody id="ur-tbody"></tbody>
        </table>
      </div>
      <div id="urPager"></div>
    `;

    const { items, total, pages } = paginate(roles, page, getPageSize());
    const tb = wrap.querySelector('#ur-tbody');

    for (const r of items){
      const tr = document.createElement('tr');
      const checked = selected.has(r.id) ? 'checked' : '';
      tr.innerHTML = `
        <td><input type="checkbox" data-id="${r.id}" ${checked}></td>
        <td>${r.name || ''}</td>
        <td>${r.description || ''}</td>
      `;
      tr.querySelector('input[type="checkbox"]').addEventListener('change', (e)=>{
        const rid = Number(e.target.getAttribute('data-id'));
        if (e.target.checked) selected.add(rid); else selected.delete(rid);
      });
      tb.appendChild(tr);
    }

    renderPager(pane.querySelector('#urPager'), { total, page, pages }, (p)=>{ page=p; renderGrid(); });
  }

  async function save(){
    try{
      if (!activeUserId) return;
      const dto = { userId: activeUserId, roleIds: Array.from(selected) };
      await assignUserRoles(dto);
      showMsgFromPane(pane,'Saved.');
    }catch(e){ showMsgFromPane(pane, errorToString(e), true); }
  }

  pane.querySelector('#ur-save').addEventListener('click', save);

  await loadUserRoles();
  renderGrid();
}
