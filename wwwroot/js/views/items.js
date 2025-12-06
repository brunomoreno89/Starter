// wwwroot/js/views/items.js  (v5-modal-util + v4-pager-safe)
import { listItems, createItem, updateItem, deleteItem, getItem, errorToString, auth } from '../api.js';
import { getPageSize, paginate, renderPager } from '../ui/pager.js';
import { formatDateToBR } from '../utils/date.js';
import { openModal, openConfirm } from '../ui/modal.js';

function icon(name){
  if (name==='edit')  return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>`;
  if (name==='trash') return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6M14 11v6"/><path d="M9 6V4a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2"/></svg>`;
  if (name==='plus')  return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 5v14M5 12h14"/></svg>`;
  return '';
}

// fallback caso algo impeça o util de desenhar
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

export async function ItemsView(container){
  // marcador de versão (cheque no console: window.__items_view_version)
  window.__items_view_version = 'v5-modal-util';

  const card = document.createElement('div');
  card.className='card';
  card.innerHTML = `
    <div class="view-header">
      <h2 class="view-title">Items</h2>
      ${auth.hasPerm('Items.Create') ? `<button class="icon-btn" id="btnNew" title="New item">${icon('plus')}</button>` : ''}
    </div>
    <div id="msg" class="alert hidden"></div>
    <div class="table-wrap compact-table">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Description</th>
            <th>Active</th>
            <th>Created At</th>
            <th>Created By</th>
            <th>Updated At</th>
            <th>Updated By</th>
            <th style="width:110px; text-align:right;"></th></tr>
        </thead>
        <tbody id="tbody"></tbody>
      </table>
    </div>
    <div id="itemsPager"></div>`;
  container.innerHTML = '';
  container.appendChild(card);

  // garante host do pager
  let pagerHost = card.querySelector('#itemsPager');
  if (!pagerHost){ pagerHost = document.createElement('div'); pagerHost.id = 'itemsPager'; card.appendChild(pagerHost); }

  function showMsg(text,isError=false){
    const div=card.querySelector('#msg');
    div.className='alert '+(isError?'error':'success');
    div.textContent=text;
    setTimeout(()=>{div.className='alert hidden'},2000);
  }

  // pinta skeleton do pager
  renderPagerSafe(pagerHost, { total: 0, page: 1, pages: 1 }, null);

  let page = 1;

  async function refresh(){
    const body = card.querySelector('#tbody'); body.innerHTML='';
    let data=[];
    try{
      data = await listItems();
    }catch(e){
      showMsg('List error: '+(e.friendly||errorToString(e)),true);
      renderPagerSafe(pagerHost, { total: 0, page: 1, pages: 1 }, null);
      return;
    }

    const { items, total, pages } = paginate(data, page, getPageSize());

    for (const it of items){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${it.id}</td>
        <td>${it.name||''}</td>
        <td>${it.description||''}</td>
        <td>${it.active||''}</td>
        <td>${formatDateToBR(it.createdAt)}</td>
        <td>${it.createdByName ?? ''}</td>
        <td>${formatDateToBR(it.updatedAt)}</td>
        <td>${it.updatedByName ?? ''}</td>
        <td style="text-align:right;">
          ${auth.hasPerm('Items.Update')?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${auth.hasPerm('Items.Delete')?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;

      // EDIT
      tr.querySelector('[data-act="edit"]')?.addEventListener('click', async ()=>{
        let current=it; try{ current = await getItem(it.id); }catch{}
        openModal(
          'Edit item #'+it.id,
          (body)=>{
            body.innerHTML = `
              <label>Name</label>
              <input id="mName" value="${(current.name||'').replace(/"/g,'&quot;')}">
              <label>Description</label>
              <textarea id="mDesc" rows="3">${current.description||''}</textarea>
              <label for="mActive">Active</label>
              <select id="mActive">
                <option value="Yes" ${current.active === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No"  ${current.active === 'No'  ? 'selected' : ''}>No</option>
              </select>
              `;
          },
          async ()=>{
            const name = document.getElementById('mName').value.trim();
            const description = document.getElementById('mDesc').value.trim();
            const active = document.getElementById('mActive').value.trim();
            if (!name) throw new Error('Name is required');
            await updateItem(it.id, { id: it.id, name, description, active });
            showMsg('Item updated.'); await refresh();
          },
          { confirmText: 'Save changes' }
        );
      });

      // DELETE com modal de confirmação
      tr.querySelector('[data-act="del"]')?.addEventListener('click', ()=>{
        openConfirm(
          'Delete item',
          `<p>Delete item <strong>#${it.id}</strong> (${(it.name||'').replace(/</g,'&lt;')})?</p>`,
          async ()=>{
            try{
              await deleteItem(it.id);
              showMsg('Item deleted.');
              await refresh();
            }catch(e){
              showMsg('Delete error: '+(e.friendly||errorToString(e)), true);
            }
          },
          { confirmText: 'Delete', confirmClass: 'danger' }
        );
      });

      body.appendChild(tr);
    }

    renderPagerSafe(pagerHost, { total, page, pages }, (newPage)=>{ page = newPage; refresh(); });
  }

  // NEW
  card.querySelector('#btnNew')?.addEventListener('click', ()=>{
    openModal(
      'New item',
      (body)=>{
        body.innerHTML = `
          <label>Name</label><input id="nName" placeholder="Item name">
          <label>Description</label><textarea id="nDesc" rows="3" placeholder="Description (optional)"></textarea>`;
      },
      async ()=>{
        const name = document.getElementById('nName').value.trim();
        const description = document.getElementById('nDesc').value.trim();
        if (!name) throw new Error('Name is required');
        await createItem({ name, description });
        showMsg('Item created.'); await refresh();
      },
      { confirmText: 'Create item' }
    );
  });

  await refresh();
}
