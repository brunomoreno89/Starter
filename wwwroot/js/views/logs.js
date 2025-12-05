// wwwroot/js/views/logs.js
import { listLogs, listUsersLight, exportLogsCsv, errorToString } from '../api.js';
import { getPageSize, paginate, renderPager } from '../ui/pager.js';
import { formatDateToBR } from '../utils/date.js';

function renderPagerSafe(host, meta, onChange){
  try {
    renderPager(host, meta, onChange);
  } catch {
    const { total=0, page=1, pages=1 } = meta || {};
    host.innerHTML = `
      <div class="pager" style="display:flex; align-items:center; justify-content:flex-end; gap:8px; margin-top:8px;">
        <span class="muted small">${total} item(s)</span>
        <button class="secondary" data-act="prev" ${page<=1?'disabled':''}>‹</button>
        <span class="muted small">Page ${page} / ${pages}</span>
        <button class="secondary" data-act="next" ${page>=pages?'disabled':''}>›</button>
      </div>`;
    host.querySelector('[data-act="prev"]')?.addEventListener('click', ()=> onChange?.(Math.max(1, page-1)));
    host.querySelector('[data-act="next"]')?.addEventListener('click', ()=> onChange?.(Math.min(pages, page+1)));
  }
}

function toInputDate(d){
  const y=d.getFullYear(), m=String(d.getMonth()+1).padStart(2,'0'), day=String(d.getDate()).padStart(2,'0');
  return `${y}-${m}-${day}`;
  //return `${day}-${m}-${y}`;
}
function monthBounds(now=new Date()){
  const start=new Date(now.getFullYear(), now.getMonth(), 1, 0,0,0);
  const end  =new Date(now.getFullYear(), now.getMonth()+1, 0, 23,59,59);
  return { start, end };
}
function formatDateTimeBR(v){
  if (!v) return '';
  const d = new Date(v);
  return isNaN(d) ? String(v) : d.toLocaleString('pt-BR');
}

export async function LogsView(container){
  window.__logs_view_version = 'v5-logs';

  const { start, end } = monthBounds();

  const card = document.createElement('div');
  card.className = 'card';
  card.innerHTML = `
    <div class="view-header">
      <h2 class="view-title">Logs</h2>
    </div>

    <div class="toolbar compact" style="display:flex; gap:8px; flex-wrap:wrap; align-items:end;">
      <div class="field">
        <label for="fUser">User</label>
        <select id="fUser" class="input" style="min-width:220px">
          <option value="">(Todos)</option>
        </select>
      </div>

      <div class="field">
        <label for="fStart">Start Date</label>
        <input id="fStart" class="input" type="date" value="${formatDateToBR(toInputDate(start))}">
      </div>
      <div class="field">
        <label for="fEnd">End Date</label>
        <input id="fEnd" class="input" type="date" value="${formatDateToBR(toInputDate(end))}">
      </div>

      <div class="field" style="display:flex; gap:8px;">
        <button id="btnApply" class="primary">Apply</button>
        <button id="btnClear" class="secondary">Clear</button>
        <button id="btnExportCsv" class="secondary">Export CSV</button>
      </div>
    </div>

    <div id="msg" class="alert hidden"></div>

    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th style="width:80px">Id</th>
            <th style="width:190px">Date</th>
            <th>User</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody id="tbody"></tbody>
      </table>
    </div>

    <div id="logsPager"></div>
  `;

  container.innerHTML = '';
  container.appendChild(card);

  const elUser  = card.querySelector('#fUser');
  const elStart = card.querySelector('#fStart');
  const elEnd   = card.querySelector('#fEnd');
  const pagerHost = card.querySelector('#logsPager');

  const elMsg = card.querySelector('#msg');
  function showMsg(text,isError=false){
    elMsg.className='alert '+(isError?'error':'success');
    elMsg.textContent=text;
    setTimeout(()=>{ elMsg.className='alert hidden'; }, 2500);
  }

  // carrega usuários (leve)
  try{
    const users = await listUsersLight();
    for (const u of (users||[])){
      const opt = document.createElement('option');
      opt.value = u.username || u.name || (u.id!=null? String(u.id):'');
      opt.textContent = (u.username || '') + (u.name ? ` (${u.name})` : '');
      elUser.appendChild(opt);
    }
  }catch(e){
    console.warn('listUsersLight falhou:', e);
  }

  let page = 1;

  async function refresh(){
    const body = card.querySelector('#tbody');
    body.innerHTML = '';

    // garante padrão mês corrente se limpar filtros
    if (!elStart.value || !elEnd.value){
      const mb = monthBounds();
      if (!elStart.value) elStart.value = toInputDate(mb.start);
      if (!elEnd.value)   elEnd.value   = toInputDate(mb.end);
    }

    const q = {
      User: (elUser.value || '').trim() || null,
      StartDate: elStart.value ? new Date(elStart.value + 'T00:00:00') : null,
      EndDate: elEnd.value ? new Date(elEnd.value + 'T00:00:00') : null  // back converte fim exclusivo
    };

    let data = [];
    try{
      const raw = await listLogs(q);
      data = Array.isArray(raw) ? raw : (raw?.items ?? raw?.data ?? raw?.logs ?? []);
    }catch(e){
      showMsg('List error: ' + (e.friendly || errorToString(e)), true);
      renderPagerSafe(pagerHost, { total:0, page:1, pages:1 }, null);
      return;
    }

    const { items, total, pages } = paginate(data, page, getPageSize());
    for (const it of (items ?? [])){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${it.id ?? ''}</td>
        <td>${formatDateTimeBR(it.execDate)}</td>
        <td>${(it.username || '') + (it.name ? ` (${it.name})` : '')}</td>
        <td>${it.description ?? ''}</td>
      `;
      body.appendChild(tr);
    }

    renderPagerSafe(pagerHost, { total, page, pages }, (newPage)=>{ page=newPage; refresh(); });
  }

  card.querySelector('#btnApply')?.addEventListener('click', ()=>{ page=1; refresh(); });
  card.querySelector('#btnClear')?.addEventListener('click', ()=>{
    elUser.value = '';
    const mb = monthBounds();
    elStart.value = toInputDate(mb.start);
    elEnd.value   = toInputDate(mb.end);
    page=1; refresh();
  });
  card.querySelector('#btnExportCsv')?.addEventListener('click', async ()=>{
    try{
      const q = {
        User: (elUser.value || '').trim() || null,
        StartDate: elStart.value ? new Date(elStart.value + 'T00:00:00') : null,
        EndDate: elEnd.value ? new Date(elEnd.value + 'T00:00:00') : null
      };
      await exportLogsCsv(q);
    }catch(e){
      showMsg('Export error: ' + (e.friendly || errorToString(e)), true);
    }
  });

  renderPagerSafe(pagerHost, { total:0, page:1, pages:1 }, null);
  await refresh();
}
