import { listUsers, listRoles, listUserRoles, assignUserRoles, errorToString, auth } from '../api.js';

export async function UserRolesView(container){
  const card = document.createElement('div');
  card.className='card';
  card.innerHTML = `
    <div class="view-header"><h2 class="view-title">User × Roles</h2></div>
    <div id="msg" class="alert hidden"></div>
    <div class="grid-2">
      <div>
        <h3>Users</h3>
        <div class="list" id="users"></div>
      </div>
      <div>
        <h3 id="rolesTitle">Roles</h3>
        <div class="list" id="roles"></div>
        <div class="actions-right">
          ${auth.hasPerm('UserRoles.Assign')?`<button class="primary" id="btnSave">Save</button>`:''}
        </div>
      </div>
    </div>`;
  container.appendChild(card);

  const showMsg=(t,e=false)=>{ const div=card.querySelector('#msg'); div.className='alert '+(e?'error':'success'); div.textContent=t; setTimeout(()=>{div.className='alert hidden'},2000); };

  const usersEl = card.querySelector('#users');
  const rolesEl = card.querySelector('#roles');
  const btnSave = card.querySelector('#btnSave');

  let users=[], roles=[], currentUserId=null, assigned=new Set();

  function renderUsers(){
    usersEl.innerHTML='';
    for (const u of users){
      const div=document.createElement('div');
      div.innerHTML=`<label><input type="radio" name="user" value="${u.id}"> ${u.username} (${u.email})</label>`;
      usersEl.appendChild(div);
    }
  }
  function renderRoles(){
    rolesEl.innerHTML='';
    for (const r of roles){
      const checked = assigned.has(r.id) ? 'checked' : '';
      const div=document.createElement('div');
      div.innerHTML=`<label><input type="checkbox" value="${r.id}" ${checked}/> ${r.name}</label>`;
      rolesEl.appendChild(div);
    }
  }

  usersEl.onclick = async (e)=>{
    const r = e.target.closest('input[type="radio"]'); if(!r) return;
    currentUserId = +r.value;
    card.querySelector('#rolesTitle').textContent = `Roles (User #${currentUserId})`;
    try{
      const data = await listUserRoles(currentUserId);
      assigned = new Set(data.map(x=>x.id));
      renderRoles();
    }catch(err){ showMsg('Load error: '+(err.friendly||errorToString(err)), true); }
  };

  rolesEl.onclick = (e)=>{
    const c = e.target.closest('input[type="checkbox"]'); if(!c) return;
    const id = +c.value;
    if (c.checked) assigned.add(id); else assigned.delete(id);
  };

  if (btnSave) btnSave.onclick = async ()=>{
    if (!currentUserId) return alert('Select a user first');
    try{
      await assignUserRoles({ userId: currentUserId, roleIds: Array.from(assigned) });
      showMsg('Roles saved.');
    }catch(err){ showMsg('Save error: '+(err.friendly||errorToString(err)), true); }
  };

  try{
    [users, roles] = await Promise.all([listUsers(), listRoles()]);
    renderUsers(); renderRoles();
    // auto-selecionar se vier userId na querystring
    const qs=new URLSearchParams(location.search); const uid=qs.get('userId');
    if (uid && users.some(u=>u.id==uid)) {
      const radio = usersEl.querySelector(`input[value="${uid}"]`);
      if (radio){ radio.checked=true; radio.dispatchEvent(new Event('click')); }
    }
  }catch(err){ showMsg('Init error: '+(err.friendly||errorToString(err)), true); }
}
