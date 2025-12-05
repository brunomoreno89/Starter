// wwwroot/js/views/static-data.js
import {
  // Holidays
  listHolidays, createHoliday, getHoliday, updateHoliday, deleteHoliday,
  // Regions
  listRegions, createRegion, getRegion, updateRegion, deleteRegion,
  // Branches
  listBranches, createBranch, getBranch, updateBranch, deleteBranch,
  
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
        <h2 class="view-title">Static Data</h2>
      </div>

      <div class="tabs" id="ac-tabs"></div>
      <div id="ac-msg" class="alert hidden" style="margin-top:8px;"></div>

      <div id="pane-regions"     class="ac-pane hidden"></div>
      <div id="pane-branches"    class="ac-pane hidden"></div>
      <div id="pane-holidays"    class="ac-pane hidden"></div>
    </div>
  `;

  const TABS = [
    { key:'holidays', label:'Holidays', perm:'Holidays.Read', render: renderHolidays },
    { key:'regions', label:'Regions', perm:'Branches.Read', render: renderRegions },
    { key:'branches', label:'Branches', perm:'Regions.Read', render: renderBranches },
    
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
      holidays:'pane-holidays'
      , regions:'pane-regions'
      , branches:'pane-branches'
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

/*
// ---------- HOLIDAYS (com Active + filtros por Branch e Ano) ---------- 

async function renderHolidays(pane){
  const allowCreate = can('Holidays.Create');
  const allowUpdate = can('Holidays.Update');
  const allowDelete = can('Holidays.Delete');

  const currentYear = new Date().getFullYear();

  pane.innerHTML = `
    <div class="view-header" style="
    display:flex;
    justify-content:space-between;
    align-items:center;
    margin-bottom:12px;
">
    <h3 style="margin:0;">Holidays</h3>
    ${allowCreate ? `<button class="icon-btn" id="rNew" title="New holiday">${icon('plus')}</button>` : ''}
</div>

<!-- TOOLBAR DE FILTROS (igual Logs) -->
<div class="toolbar" style="display:flex; gap:16px; flex-wrap:wrap; align-items:flex-end; margin-bottom:16px;">

    <div class="field">
        <label for="fBranch">Branch</label>
        <select id="fBranch">
            <option value="">All branches</option>
        </select>
    </div>

    <div class="field">
        <label for="fYear">Year</label>
        <input id="fYear" type="number" min="1900" max="2100" style="width:110px;">
    </div>

    <div class="field" style="display:flex; gap:8px; align-items:flex-end;">
        <button type="button" id="fApply" class="btn-primary">Apply</button>
        <button type="button" id="fClear" class="btn-secondary">Clear</button>
    </div>

</div>

    

    

    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Branch</th>
            <th>Holiday Date</th>
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

  let page = 1;
  let roles = [];       // aqui são os holidays retornados da API
  let branches = [];

  // elementos dos filtros
  const branchFilterEl = pane.querySelector('#fBranch');
  const yearFilterEl   = pane.querySelector('#fYear');
  const clearFilterEl  = pane.querySelector('#fClear');

  // ano corrente como padrão
  if (yearFilterEl) {
    yearFilterEl.value = currentYear;
  }

  // carrega branches para dropdowns (filtro + modais)
  try {
    branches = await listBranches();
  } catch (e) {
    showMsgFromPane(pane, 'Error loading branches: ' + errorToString(e), true);
    branches = [];
  }

  // preenche opções do filtro de Branch
  if (branchFilterEl) {
    branchFilterEl.innerHTML = `
      <option value="">All branches</option>
      ${branches.map(b => `<option value="${b.id}">${b.description}</option>`).join('')}
    `;
  }

  const getBranchLabel = (branchId, branchDescription) => {
    // preferência: nome vindo da SP (branchDescription); se não tiver, busca na lista de branches
    if (branchDescription) return branchDescription;
    const b = branches.find(x => x.id === branchId);
    return b ? b.description : (branchId || '');
  };

  async function reload(){ 
    try { 
      roles = await listHolidays(); 
    } catch(e){ 
      showMsgFromPane(pane, errorToString(e), true); 
      roles = []; 
    } 
    fill(); 
  }

  function applyFilters(source){
    let filtered = [...source];

    const branchFilter = branchFilterEl?.value || '';
    const yearFilter   = yearFilterEl?.value?.trim();

    if (branchFilter) {
      filtered = filtered.filter(h => String(h.branchId) === String(branchFilter));
    }

    if (yearFilter) {
      filtered = filtered.filter(h => {
        if (!h.holidayDate) return false;
        const y = (h.holidayDate + '').substring(0, 4); // assume formato YYYY-MM-DD
        return String(y) === String(yearFilter);
      });
    }

    return filtered;
  }

  function fill(){
    const tb = pane.querySelector('#rBody'); 
    tb.innerHTML='';

    const filtered = applyFilters(roles);
    const { items, total, pages } = paginate(filtered, page, getPageSize());

    for (const r of items){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${r.id}</td>
        <td>${getBranchLabel(r.branchId, r.branchDescription)}</td>
        <td>${formatDateToBR(r.holidayDate)}</td>
        <td>${r.description||''}</td>
        <td>${r.active||''}</td>
        <td>${formatDateToBR(r.createdAt)}</td>
        <td>${r.createdByName ?? ''}</td>
        <td>${formatDateToBR(r.updatedAt)}</td>
        <td>${r.updatedByName ?? ''}</td>
        <td style="text-align:right;">
          ${allowUpdate?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${allowDelete?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;

      // EDIT
      tr.querySelector('[data-act="edit"]')?.addEventListener('click', ()=>{
        openModal(
          'Edit Holiday #'+r.id,
          (body)=>{
            body.innerHTML = `
              <label>Branch</label>
              <select id="mBBranchId">
                ${branches.map(b => `
                  <option value="${b.id}" ${b.id === r.branchId ? 'selected' : ''}>${b.description}</option>
                `).join('')}
              </select>

              <label>Holiday Date</label>
              <input type="date" id="mBHolidayDate" value="${(r.holidayDate ?? '').substring(0,10)}">

              <label>Description</label>
              <textarea id="mBDescription" rows="3">${r.description||''}</textarea>

              <label for="mBActive">Active</label>
              <select id="mBActive">
                <option value="Yes" ${r.active === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No"  ${r.active === 'No'  ? 'selected' : ''}>No</option>
              </select>`;
          },
          async ()=>{
            const branchid    = document.getElementById('mBBranchId').value.trim();
            const holidaydate = document.getElementById('mBHolidayDate').value.trim();
            const description = document.getElementById('mBDescription').value.trim();
            const active      = document.getElementById('mBActive').value.trim();

            if (!branchid)    throw new Error('Branch is required');
            if (!holidaydate) throw new Error('Holiday Date is required');
            if (!description) throw new Error('Description is required');

            await updateHoliday(r.id, { id:r.id, branchid, holidaydate, description, active });
            showMsgFromPane(pane,'Holiday updated.'); 
            await reload();
          },
          { confirmText: 'Save changes' }
        );
      });

      // DELETE
      tr.querySelector('[data-act="del"]')?.addEventListener('click', ()=>{
        openConfirm(
          'Delete Holiday',
          `<p>Delete Holiday <strong>${formatDateToBR(r.holidayDate)} - ${r.description} </strong>?</p>`,
          async ()=>{
            await deleteHoliday(r.id);
            showMsgFromPane(pane,'Holiday deleted.'); 
            await reload();
          },
          { confirmText: 'Delete', confirmClass: 'danger' }
        );
      });

      tb.appendChild(tr);
    }

    renderPager(
      pane.querySelector('#rPager'),
      { total, page, pages },
      (p)=>{ page=p; fill(); }
    );
  }

  // eventos dos filtros
  branchFilterEl?.addEventListener('change', () => { page = 1; fill(); });
  yearFilterEl?.addEventListener('change',   () => { page = 1; fill(); });
  clearFilterEl?.addEventListener('click', () => {
    if (branchFilterEl) branchFilterEl.value = '';
    if (yearFilterEl)   yearFilterEl.value   = currentYear;
    page = 1;
    fill();
  });

  // BOTÃO NEW
  const addBtn = pane.querySelector('#rNew');
  if (addBtn) addBtn.onclick = ()=>{
    openModal(
      'New Holiday',
      (body)=>{
        body.innerHTML = `
          <label>Branch</label>
          <select id="mRBranchId">
            <option value="">Select a branch...</option>
            ${branches.map(b => `<option value="${b.id}">${b.description}</option>`).join('')}
          </select>

          <label>Holiday Date</label>
          <input type="date" id="mRHolidayDate">

          <label>Description</label>
          <textarea id="mRDescription" rows="3" placeholder="Description (optional)"></textarea>`;
      },
      async ()=>{
        const branchid    = document.getElementById('mRBranchId').value.trim();
        const holidaydate = document.getElementById('mRHolidayDate').value.trim();
        const description = document.getElementById('mRDescription').value.trim();
        
        if (!branchid)    throw new Error('Branch is required');
        if (!holidaydate) throw new Error('Holiday Date is required');
        if (!description) throw new Error('Description is required');

        await createHoliday({ branchid, holidaydate, description });
        showMsgFromPane(pane,'Holiday created.'); 
        await reload();
      },
      { confirmText: 'Holiday created' }
    );
  };

  await reload();
}
*/

// ---------- HOLIDAYS (com Active + filtros por Branch e Ano) ---------- 

async function renderHolidays(pane){
  const allowCreate = can('Holidays.Create');
  const allowUpdate = can('Holidays.Update');
  const allowDelete = can('Holidays.Delete');

  const currentYear = new Date().getFullYear();

  pane.innerHTML = `
    <!-- HEADER -->
    <div class="view-header" style="
      display:flex;
      justify-content:space-between;
      align-items:center;
      margin-top:16px;
      margin-bottom:8px;
    ">
      <h3 style="margin:0;">Holidays</h3>
      ${allowCreate ? `<button class="icon-btn" id="rNew" title="New holiday">${icon('plus')}</button>` : ''}
    </div>

    <!-- TOOLBAR DE FILTROS (estilo Logs) -->
    <div class="toolbar compact" style="
      display:flex;
      gap:12px;
      flex-wrap:wrap;
      align-items:flex-end;
      margin-bottom:16px;
    ">
      <div class="field">
        <label for="fBranch">Branch</label>
        <select id="fBranch">
          <option value="">All branches</option>
        </select>
      </div>

      <div class="field">
        <label for="fYear">Year</label>
        <input id="fYear" type="number" min="1900" max="2100" style="width:110px;">
      </div>

      <div class="field" style="display:flex; gap:8px; align-items:flex-end;">
        <button type="button" id="fApply" class="primary">Apply</button>
        <button type="button" id="fClear" class="secondary">Clear</button>
      </div>
    </div>

    <div class="table-wrap compact-table">
      <table class="table">

        <thead>
          <tr>
            <th>ID</th>
            <th>Branch</th>
            <th>Holiday Date</th>
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
    <div id="rPager" style="margin-top:12px; text-align:right;"></div>
  `;

  let page = 1;
  let roles = [];       // holidays retornados da API
  let branches = [];

  // elementos dos filtros
  const branchFilterEl = pane.querySelector('#fBranch');
  const yearFilterEl   = pane.querySelector('#fYear');
  const clearFilterEl  = pane.querySelector('#fClear');
  const applyFilterEl  = pane.querySelector('#fApply');

  // ano corrente como padrão
  if (yearFilterEl) {
    yearFilterEl.value = currentYear;
  }

  // carrega branches para dropdowns (filtro + modais)
  try {
    branches = await listBranches();
  } catch (e) {
    showMsgFromPane(pane, 'Error loading branches: ' + errorToString(e), true);
    branches = [];
  }

  // preenche opções do filtro de Branch
  if (branchFilterEl) {
    branchFilterEl.innerHTML = `
      <option value="">All branches</option>
      ${branches.map(b => `<option value="${b.id}">${b.description}</option>`).join('')}
    `;
  }

  const getBranchLabel = (branchId, branchDescription) => {
    // preferência: nome vindo da SP (branchDescription); se não tiver, busca na lista de branches
    if (branchDescription) return branchDescription;
    const b = branches.find(x => x.id === branchId);
    return b ? b.description : (branchId || '');
  };

  async function reload(){ 
    try { 
      roles = await listHolidays(); 
    } catch(e){ 
      showMsgFromPane(pane, errorToString(e), true); 
      roles = []; 
    } 
    fill(); 
  }

  function applyFilters(source){
    let filtered = [...source];

    const branchFilter = branchFilterEl?.value || '';
    const yearFilter   = yearFilterEl?.value?.trim();

    if (branchFilter) {
      filtered = filtered.filter(h => String(h.branchId) === String(branchFilter));
    }

    if (yearFilter) {
      filtered = filtered.filter(h => {
        if (!h.holidayDate) return false;
        const y = (h.holidayDate + '').substring(0, 4); // assume YYYY-MM-DD
        return String(y) === String(yearFilter);
      });
    }

    return filtered;
  }

  function fill(){
    const tb = pane.querySelector('#rBody'); 
    tb.innerHTML='';

    const filtered = applyFilters(roles);
    const { items, total, pages } = paginate(filtered, page, getPageSize());

    for (const r of items){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${r.id}</td>
        <td>${getBranchLabel(r.branchId, r.branchDescription)}</td>
        <td>${formatDateToBR(r.holidayDate)}</td>
        <td>${r.description||''}</td>
        <td>${r.active||''}</td>
        <td>${formatDateToBR(r.createdAt)}</td>
        <td>${r.createdByName ?? ''}</td>
        <td>${formatDateToBR(r.updatedAt)}</td>
        <td>${r.updatedByName ?? ''}</td>
        <td style="text-align:right;">
          ${allowUpdate?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${allowDelete?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;

      // EDIT
      tr.querySelector('[data-act="edit"]')?.addEventListener('click', ()=>{
        openModal(
          'Edit Holiday #'+r.id,
          (body)=>{
            body.innerHTML = `
              <label>Branch</label>
              <select id="mBBranchId">
                ${branches.map(b => `
                  <option value="${b.id}" ${b.id === r.branchId ? 'selected' : ''}>${b.description}</option>
                `).join('')}
              </select>

              <label>Holiday Date</label>
              <input type="date" id="mBHolidayDate" value="${(r.holidayDate ?? '').substring(0,10)}">

              <label>Description</label>
              <textarea id="mBDescription" rows="3">${r.description||''}</textarea>

              <label for="mBActive">Active</label>
              <select id="mBActive">
                <option value="Yes" ${r.active === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No"  ${r.active === 'No'  ? 'selected' : ''}>No</option>
              </select>`;
          },
          async ()=>{
            const branchid    = document.getElementById('mBBranchId').value.trim();
            const holidaydate = document.getElementById('mBHolidayDate').value.trim();
            const description = document.getElementById('mBDescription').value.trim();
            const active      = document.getElementById('mBActive').value.trim();

            if (!branchid)    throw new Error('Branch is required');
            if (!holidaydate) throw new Error('Holiday Date is required');
            if (!description) throw new Error('Description is required');

            await updateHoliday(r.id, { id:r.id, branchid, holidaydate, description, active });
            showMsgFromPane(pane,'Holiday updated.'); 
            await reload();
          },
          { confirmText: 'Save changes' }
        );
      });

      // DELETE
      tr.querySelector('[data-act="del"]')?.addEventListener('click', ()=>{
        openConfirm(
          'Delete Holiday',
          `<p>Delete Holiday <strong>${formatDateToBR(r.holidayDate)} - ${r.description} </strong>?</p>`,
          async ()=>{
            await deleteHoliday(r.id);
            showMsgFromPane(pane,'Holiday deleted.'); 
            await reload();
          },
          { confirmText: 'Delete', confirmClass: 'danger' }
        );
      });

      tb.appendChild(tr);
    }

    renderPager(
      pane.querySelector('#rPager'),
      { total, page, pages },
      (p)=>{ page=p; fill(); }
    );
  }

  // eventos dos filtros
  applyFilterEl?.addEventListener('click', () => {
    page = 1;
    fill();
  });

  clearFilterEl?.addEventListener('click', () => {
    if (branchFilterEl) branchFilterEl.value = '';
    if (yearFilterEl)   yearFilterEl.value   = currentYear;
    page = 1;
    fill();
  });

  // BOTÃO NEW
  const addBtn = pane.querySelector('#rNew');
  if (addBtn) addBtn.onclick = ()=>{
    openModal(
      'New Holiday',
      (body)=>{
        body.innerHTML = `
          <label>Branch</label>
          <select id="mRBranchId">
            <option value="">Select a branch...</option>
            ${branches.map(b => `<option value="${b.id}">${b.description}</option>`).join('')}
          </select>

          <label>Holiday Date</label>
          <input type="date" id="mRHolidayDate">

          <label>Description</label>
          <textarea id="mRDescription" rows="3" placeholder="Description (optional)"></textarea>`;
      },
      async ()=>{
        const branchid    = document.getElementById('mRBranchId').value.trim();
        const holidaydate = document.getElementById('mRHolidayDate').value.trim();
        const description = document.getElementById('mRDescription').value.trim();
        
        if (!branchid)    throw new Error('Branch is required');
        if (!holidaydate) throw new Error('Holiday Date is required');
        if (!description) throw new Error('Description is required');

        await createHoliday({ branchid, holidaydate, description });
        showMsgFromPane(pane,'Holiday created.'); 
        await reload();
      },
      { confirmText: 'Holiday created' }
    );
  };

  await reload();
}


// ---------- REGIONS (com Active) ---------- 
async function renderRegions(pane){
  const allowCreate = can('Regions.Create');
  const allowUpdate = can('Regions.Update');
  const allowDelete = can('Regions.Delete');

  pane.innerHTML = `
    <div class="view-header">
      <h3>Regions</h3>
      ${allowCreate?`<button class="icon-btn" id="rNew" title="New Region">${icon('plus')}</button>`:''}
    </div>
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th>
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
  async function reload(){ try{ roles = await listRegions(); }catch(e){ showMsgFromPane(pane, errorToString(e), true); roles=[]; } fill(); }
  function fill(){
    const tb = pane.querySelector('#rBody'); tb.innerHTML='';
    const { items, total, pages } = paginate(roles, page, getPageSize());
    for (const r of items){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${r.id}</td>
        <td>${r.description||''}</td>
        <td>${r.active||''}</td>
        <td>${formatDateToBR(r.createdAt)}</td>
        <td>${r.createdByName ?? ''}</td>
        <td>${formatDateToBR(r.updatedAt)}</td>
        <td>${r.updatedByName ?? ''}</td>
        <td style="text-align:right;">
          ${allowUpdate?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${allowDelete?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;
      tr.querySelector('[data-act="edit"]')?.addEventListener('click', ()=>{
        openModal(
          'Edit Region #'+r.id,
          (body)=>{
            body.innerHTML = `
              
              <label>Description</label>
              <textarea id="mBDescription" rows="3">${r.description||''}</textarea>
              <label for="mBActive">Active</label>
              <select id="mBActive">
                <option value="Yes" ${r.active === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No"  ${r.active === 'No'  ? 'selected' : ''}>No</option>
              </select>`;
          },
          async ()=>{
            
            const description = document.getElementById('mBDescription').value.trim();
            const active = document.getElementById('mBActive').value.trim();
            if (!description) throw new Error('Branch Code is required');
            await updateRegion(r.id, { id:r.id, description, active });
            showMsgFromPane(pane,'Region updated.'); await reload();
          },
          { confirmText: 'Save changes' }
        );
      });
      tr.querySelector('[data-act="del"]')?.addEventListener('click', ()=>{
        openConfirm(
          'Delete Region',
          `<p>Delete Region <strong>${r.description}</strong>?</p>`,
          async ()=>{
            await deleteRegion(r.id);
            showMsgFromPane(pane,'Region deleted.'); await reload();
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
      'New Region',
      (body)=>{
        body.innerHTML = `
          
          <label>Description</label><textarea id="mRDescription" rows="3" placeholder="Description (optional)"></textarea>`;
      },
      async ()=>{
        
        const description = document.getElementById('mRDescription').value.trim();
        if (!description) throw new Error('Region Description is required');
        await createRegion({ description });
        showMsgFromPane(pane,'Region created.'); await reload();
      },
      { confirmText: 'Region Branch' }
    );
  };

  await reload();
}




// ---------- BEANCHES (com paginação) ----------
async function renderBranches(pane){
  const allowCreate = can('Branches.Create');
  const allowUpdate = can('Branches.Update');
  const allowDelete = can('Branches.Delete');

  pane.innerHTML = `
    <div class="view-header">
      <h3>Branches</h3>
      ${allowCreate?`<button class="icon-btn" id="rNew" title="New Branch">${icon('plus')}</button>`:''}
    </div>
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Region</th>
            <th>Branch Code</th>
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
  async function reload(){ try{ roles = await listBranches(); }catch(e){ showMsgFromPane(pane, errorToString(e), true); roles=[]; } fill(); }
  function fill(){
    const tb = pane.querySelector('#rBody'); tb.innerHTML='';
    const { items, total, pages } = paginate(roles, page, getPageSize());
    for (const r of items){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${r.id}</td>
        <td>${r.regionId||''}</td>
        <td>${r.branchCode||''}</td>
        <td>${r.description||''}</td>
        <td>${r.active||''}</td>
        <td>${formatDateToBR(r.createdAt)}</td>
        <td>${r.createdByName ?? ''}</td>
        <td>${formatDateToBR(r.updatedAt)}</td>
        <td>${r.updatedByName ?? ''}</td>
        <td style="text-align:right;">
          ${allowUpdate?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${allowDelete?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;
      tr.querySelector('[data-act="edit"]')?.addEventListener('click', ()=>{
        openModal(
          'Edit Branch #'+r.id,
          (body)=>{
            body.innerHTML = `
              <label>Region</label>
              <input id="mBRegionId" value="${(r.regionId||'')}">    
              <label>Branch Code</label>
              <input id="mBBranchCode" value="${(r.branchCode||'')}">  
              <label>Description</label>
              <textarea id="mBDescription" rows="3">${r.description||''}</textarea>
              <label for="mBActive">Active</label>
              <select id="mBActive">
                <option value="Yes" ${r.active === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No"  ${r.active === 'No'  ? 'selected' : ''}>No</option>
              </select>`;
          },
          async ()=>{
            const regionid = document.getElementById('mBRegionId').value.trim();
            const branchcode = document.getElementById('mBBranchCode').value.trim();
            const description = document.getElementById('mBDescription').value.trim();
            const active = document.getElementById('mBActive').value.trim();
            if (!branchcode) throw new Error('Branch Code is required');
            await updateBranch(r.id, { id:r.id, regionid, branchcode, description, active });
            showMsgFromPane(pane,'Branch updated.'); await reload();
          },
          { confirmText: 'Save changes' }
        );
      });
      tr.querySelector('[data-act="del"]')?.addEventListener('click', ()=>{
        openConfirm(
          'Delete Branch',
          `<p>Delete Branch <strong>${r.branchcode}</strong>?</p>`,
          async ()=>{
            await deleteBranch(r.id);
            showMsgFromPane(pane,'Branch deleted.'); await reload();
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
      'New Branch',
      (body)=>{
        body.innerHTML = `
          <label>Region</label><input id="mNRegionId" placeholder="Region">  
          <label>Branch Code</label><input id="mNBranchCode" placeholder="Branch Code">
          <label>Description</label><textarea id="mRDescription" rows="3" placeholder="Description (optional)"></textarea>`;
      },
      async ()=>{
        const regionid = document.getElementById('mNRegionId').value.trim();
        const branchcode = document.getElementById('mNBranchCode').value.trim();
        const description = document.getElementById('mRDescription').value.trim();
        if (!branchcode) throw new Error('Branch Code is required');
        await createBranch({ regionid, branchcode, description });
        showMsgFromPane(pane,'Branch created.'); await reload();
      },
      { confirmText: 'Create Branch' }
    );
  };

  await reload();
}



