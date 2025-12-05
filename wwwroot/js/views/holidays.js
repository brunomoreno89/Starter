// wwwroot/js/views/Holidays.js
import { createHoliday, listHolidays, getHoliday, updateHoliday, deleteHoliday, errorToString, auth } from '../api.js';

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

export async function HolidaysView(container){
  const card = document.createElement('div');
  card.className='card';
  card.innerHTML = `
    <div class="view-header">
      <h2 class="view-title">Holidays</h2>
      ${auth.hasPerm('Holidays.Create') ? `<button class="icon-btn" id="btnNew" title="New Holiday">${icon('plus')}</button>` : ''}
    </div>
    <div id="msg" class="alert hidden"></div>
    <div class="table-wrap">
      <table class="table">
        <thead>
            <tr>
                <th>ID</th>
                <th>Holiday Date</th>
                <th>Branch</th>
                <th>Description</th>
                <th>Active</th>
                <th>Created At</th>
                <th>Created By</th>
                <th>Updated At</th>
                <th>Updated By</th>
                
                <th style="width:110px; text-align:right;">Actions</th>
            </tr>
        </thead>
        <tbody id="tbody"></tbody>
      </table>
    </div>`;
  container.appendChild(card);

  function showMsg(text,isError=false){ const div=card.querySelector('#msg'); div.className='alert '+(isError?'error':'success'); div.textContent=text; setTimeout(()=>{div.className='alert hidden'},2500); }

  async function refresh(){
    const body = card.querySelector('#tbody'); body.innerHTML='';
    let data=[]; try{ data = await listHolidays(); }catch(e){ showMsg('List error: '+(e.friendly||errorToString(e)),true); return; }
    for (const h of data){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${h.id}</td>
        <td>${formatDateToBR(h.holidayDate)}</td>
        <td>${h.branchId||''}</td>
        <td>${h.description||''}</td>
        <td>${it.active||''}</td>
        <td>${formatDateToBR(it.createdAt)}</td>
        <td>${it.createdByName ?? ''}</td>
        <td>${formatDateToBR(it.updatedAt)}</td>
        <td>${it.updatedByName ?? ''}</td>
        <td style="text-align:right;">
          ${auth.hasPerm('Holidays.Update')?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${auth.hasPerm('Holidays.Delete')?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;
      const btnE = tr.querySelector('[data-act="edit"]');
      const btnD = tr.querySelector('[data-act="del"]');

      if (btnE) btnE.onclick = async ()=>{
        let current=h; try{ current = await getHoliday(h.id); }catch{}
        openModal('Edit Holiday #'+h.id,(body)=>{
          body.innerHTML = `
            <label>Holiday Date</label><input id="eHolidayDate" type="date" value="${(current.holidayDate||'').replace(/"/g,'&quot;')}">
            <label>Branch</label><input id="eBranchId" type="text" value="${(current.branchId||'').replace(/"/g,'&quot;')}">
            <label>Description</label><input id="eDescription" type="email" value="${(current.description||'').replace(/"/g,'&quot;')}">
            <label for="mActive">Active</label>
              <select id="eActive">
                <option value="Yes" ${current.active === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No"  ${current.active === 'No'  ? 'selected' : ''}>No</option>
              </select>`;
        }, async ()=>{
          const HolidayDate = document.getElementById('eHolidayDate').value.trim();
            const BranchId = document.getElementById('eBranchId').value.trim();
          const Description = document.getElementById('eDescription').value.trim();
          const active = document.getElementById('mActive').value.trim();
          
          const dto = { id:h.id, HolidayDate, BranchId, Description, active }
          await updateHoliday(h.id, dto); showMsg('Holiday updated.'); await refresh();
        });
      };

      if (btnD) btnD.onclick = async ()=>{
        if (!confirm(`Delete Holiday ${u.Holidayname} (#${u.id})?`)) return;
        try{ await deleteHoliday(u.id); showMsg('Holiday deleted.'); await refresh(); }
        catch(e){ showMsg('Delete error: '+(e.friendly||errorToString(e)),true); }
      };

      body.appendChild(tr);
    }
  }

  const addBtn = card.querySelector('#btnNew');
  if (addBtn) addBtn.onclick = ()=>{
    openModal('New Holiday',(body)=>{
      body.innerHTML = `
        <label>Holiday Date</label><input id="nHolidayDate" type="date">
        <label>Branch</label><input id="nBranchId" type="text">
            <label>Description</label><input id="nDescription" type="email">
            `;
    }, async ()=>{
      const HolidayDate = document.getElementById('nHoliday').value.trim();
        const BranchId = document.getElementById('nBranchId').value.trim();
      const Description    = document.getElementById('nDescription').value.trim();
      
      if (!HolidayDate) throw new Error('All fields are required');
      await createHoliday({ HolidayDate, BranchId, Description }); showMsg('Holiday created.'); await refresh();
    });
  };

  await refresh();
}
