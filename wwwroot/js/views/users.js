// wwwroot/js/views/users.js
import { createUser, listUsers, getUser, updateUser, deleteUser, errorToString, auth } from '../api.js';

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

export async function UsersView(container){
  const card = document.createElement('div');
  card.className='card';
  card.innerHTML = `
    <div class="view-header">
      <h2 class="view-title">Users</h2>
      ${auth.hasPerm('Users.Create') ? `<button class="icon-btn" id="btnNew" title="New user">${icon('plus')}</button>` : ''}
    </div>
    <div id="msg" class="alert hidden"></div>
    <div class="table-wrap">
      <table class="table">
        <thead><tr><th>ID</th><th>Username</th><th>Email</th><th>Role</th><th style="width:110px; text-align:right;">Actions</th></tr></thead>
        <tbody id="tbody"></tbody>
      </table>
    </div>`;
  container.appendChild(card);

  function showMsg(text,isError=false){ const div=card.querySelector('#msg'); div.className='alert '+(isError?'error':'success'); div.textContent=text; setTimeout(()=>{div.className='alert hidden'},2500); }

  async function refresh(){
    const body = card.querySelector('#tbody'); body.innerHTML='';
    let data=[]; try{ data = await listUsers(); }catch(e){ showMsg('List error: '+(e.friendly||errorToString(e)),true); return; }
    for (const u of data){
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${u.id}</td>
        <td>${u.username||''}</td>
        <td>${u.email||''}</td>
        <td>${u.role||''}</td>
        <td style="text-align:right;">
          ${auth.hasPerm('Users.Update')?`<button class="icon-btn" title="Edit" data-act="edit">${icon('edit')}</button>`:''}
          ${auth.hasPerm('Users.Delete')?`<button class="icon-btn" title="Delete" data-act="del">${icon('trash')}</button>`:''}
        </td>`;
      const btnE = tr.querySelector('[data-act="edit"]');
      const btnD = tr.querySelector('[data-act="del"]');

      if (btnE) btnE.onclick = async ()=>{
        let current=u; try{ current = await getUser(u.id); }catch{}
        openModal('Edit user #'+u.id,(body)=>{
          body.innerHTML = `
            <label>Username</label><input id="eUser" value="${(current.username||'').replace(/"/g,'&quot;')}">
            <label>Email</label><input id="eEmail" type="email" value="${(current.email||'').replace(/"/g,'&quot;')}">
            <label>Role</label>
            <select id="eRole">
              <option ${current.role==='User'?'selected':''}>User</option>
              <option ${current.role==='Admin'?'selected':''}>Admin</option>
            </select>
            <label>New password (optional)</label><input id="ePass" type="password" placeholder="Leave blank to keep current">`;
        }, async ()=>{
          const username = document.getElementById('eUser').value.trim();
          const email = document.getElementById('eEmail').value.trim();
          const role  = document.getElementById('eRole').value;
          const password = document.getElementById('ePass').value;
          if (!username || !email) throw new Error('Username and Email are required');
          const dto = { id:u.id, username, email, role }; if (password) dto.password = password;
          await updateUser(u.id, dto); showMsg('User updated.'); await refresh();
        });
      };

      if (btnD) btnD.onclick = async ()=>{
        if (!confirm(`Delete user ${u.username} (#${u.id})?`)) return;
        try{ await deleteUser(u.id); showMsg('User deleted.'); await refresh(); }
        catch(e){ showMsg('Delete error: '+(e.friendly||errorToString(e)),true); }
      };

      body.appendChild(tr);
    }
  }

  const addBtn = card.querySelector('#btnNew');
  if (addBtn) addBtn.onclick = ()=>{
    openModal('New user',(body)=>{
      body.innerHTML = `
        <label>Username</label><input id="nUser" placeholder="Username">
        <label>Email</label><input id="nEmail" type="email" placeholder="email@example.com">
        <label>Role</label>
        <select id="nRole"><option>User</option><option>Admin</option></select>
        <label>Password</label><input id="nPass" type="password" placeholder="Initial password">`;
    }, async ()=>{
      const username = document.getElementById('nUser').value.trim();
      const email    = document.getElementById('nEmail').value.trim();
      const role     = document.getElementById('nRole').value;
      const password = document.getElementById('nPass').value;
      if (!username || !email || !password) throw new Error('All fields are required');
      await createUser({ username, email, role, password }); showMsg('User created.'); await refresh();
    });
  };

  await refresh();
}
