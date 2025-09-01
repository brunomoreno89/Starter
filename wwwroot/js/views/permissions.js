import { listPermissions, createPermission, updatePermission, deletePermission, getPermission, errorToString, auth } from '../api.js';

function icon(name){
  if (name==='edit') return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>`;
  if (name==='trash')return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6M14 11v6"/><path d="M9 6V4a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2"/></svg>`;
  if (name==='plus') return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 5v14M5 12h14"/></svg>`;
  return '';
}
function openModal(title, contentBuilder, onSubmit){
  const backdrop = document.createElement('div'); backdrop.className='modal-backdrop';
  const modal = document.createElement('div'); modal.className='modal';
  modal.innerHTML = `<header><h3>${title}</h3><button class="close-btn" title="Close">×</button></header><div class="body"></div><footer><button class="secondary" data-action="cancel">Cancel</button><button class="primary" data-action="save">Save</button></footer>`;
  backdrop.appendChild(modal); document.body.appendChild(backdrop);
  const body = modal.querySelector('.body'); const cleanup = ()=>backdrop.remove();
  modal.querySelector('.close-btn').onclick = cleanup;
  modal.querySelector('[data-action="cancel"]').onclick = cleanup;
  modal.querySelector('[data-action="save"]').onclick = async ()=>{ try{ await onSubmit(); cleanup(); }catch(e){ alert('Save error: '+(e.friendly||errorToString(e))); } };
  contentBuilder(body); return { close: cleanup };
}

export async function PermissionsView(container){
  const card = document.createElement('div');
  card.className='card';
  card.innerHTML = `
    <div class="view-header">
      <h2 class="view-title">Permissions</h2>
      ${auth.hasPerm('Permissions.Create') ? `<button class="icon-btn" id="btnNew" title="New permission">${icon('plus')}</button>` : ''}
    </div>
    <div id="msg" class="alert hidden"></div>
    <div class="table-wrap">
      <table class="table">
        <thead><tr><th>ID</th><th>Name</th><th>Description</th><th style="width:110px; text-align:right;">Actions</th></tr></thead>
        <tbody id="tbody"></tbody>
      </table>
    </div>`;
  container.appendChild(card);

  const showMsg=(t,e=false)=>{ const div=card.querySelector('#msg'); div.className='alert '+(e?'error':'success'); div.textContent=t; setTimeout(()=>{div.className='alert hidden'},2000); };

  async function refresh(){
    const body = card.querySelector('#tbody'); body.innerHTML='';
    let data=[]; try{ data = await listPermissions(); }catch(e){ showMsg('List error: '+(e.friendly||errorToString(e)),true); return; }
    for (const p of data){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${p.id}</td>
        <td>${p.name||''}</td>
        <td>${p.description||''}</td>
        <td style="text-align:right;">
          ${auth.hasPerm('Permissions.Update')?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${auth.hasPerm('Permissions.Delete')?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;
      const btnE = tr.querySelector('[data-act="edit"]');
      const btnD = tr.querySelector('[data-act="del"]');

      if (btnE) btnE.onclick = async ()=>{
        let current=p; try{ current = await getPermission(p.id); }catch{}
        openModal('Edit permission #'+p.id,(body)=>{
          body.innerHTML = `
            <label>Name</label><input id="mName" value="${(current.name||'').replace(/"/g,'&quot;')}">
            <label>Description</label><textarea id="mDesc" rows="3">${current.description||''}</textarea>`;
        }, async ()=>{
          const name = document.getElementById('mName').value.trim();
          const description = document.getElementById('mDesc').value.trim();
          if (!name) throw new Error('Name is required');
          await updatePermission(p.id,{ id:p.id, name, description });
          showMsg('Permission updated.'); await refresh();
        });
      };

      if (btnD) btnD.onclick = async ()=>{
        if (!confirm('Delete permission '+p.id+'?')) return;
        try{ await deletePermission(p.id); showMsg('Permission deleted.'); await refresh(); }
        catch(e){ showMsg('Delete error: '+(e.friendly||errorToString(e)),true); }
      };

      body.appendChild(tr);
    }
  }

  const addBtn = card.querySelector('#btnNew');
  if (addBtn) addBtn.onclick = ()=>{
    openModal('New permission',(body)=>{
      body.innerHTML = `
        <label>Name</label><input id="nName" placeholder="e.g. Permissions.Read">
        <label>Description</label><textarea id="nDesc" rows="3" placeholder="Description (optional)"></textarea>`;
    }, async ()=>{
      const name = document.getElementById('nName').value.trim();
      const description = document.getElementById('nDesc').value.trim();
      if (!name) throw new Error('Name is required');
      await createPermission({ name, description }); showMsg('Permission created.'); await refresh();
    });
  };

  await refresh();
}
