import { listRoles, listPermissions, listRolePermissions, assignRolePermissions, errorToString, auth } from '../api.js';

function openModal(){} // placeholder (não precisamos aqui)
export async function RolePermissionsView(container){
  const card = document.createElement('div');
  card.className='card';
  card.innerHTML = `
    <div class="view-header"><h2 class="view-title">Role × Permissions</h2></div>
    <div id="msg" class="alert hidden"></div>
    <div class="grid-2">
      <div>
        <h3>Roles</h3>
        <div class="list" id="roles"></div>
      </div>
      <div>
        <h3 id="permTitle">Permissions</h3>
        <div class="list" id="perms"></div>
        <div class="actions-right">
          ${auth.hasPerm('RolePermissions.Update')?`<button class="primary" id="btnSave">Save</button>`:''}
        </div>
      </div>
    </div>`;
  container.appendChild(card);

  const showMsg=(t,e=false)=>{ const div=card.querySelector('#msg'); div.className='alert '+(e?'error':'success'); div.textContent=t; setTimeout(()=>{div.className='alert hidden'},2000); };

  const rolesEl = card.querySelector('#roles');
  const permsEl = card.querySelector('#perms');
  const btnSave = card.querySelector('#btnSave');

  let roles=[], perms=[], currentRoleId=null, assigned=new Set();

  function renderRoles(){
    rolesEl.innerHTML='';
    for (const r of roles){
      const div=document.createElement('div');
      div.innerHTML=`<label><input type="radio" name="role" value="${r.id}"> ${r.name}</label>`;
      rolesEl.appendChild(div);
    }
  }
  function renderPerms(){
    permsEl.innerHTML='';
    for (const p of perms){
      const checked = assigned.has(p.id) ? 'checked' : '';
      const div=document.createElement('div');
      div.innerHTML=`<label><input type="checkbox" value="${p.id}" ${checked}/> ${p.name}</label>`;
      permsEl.appendChild(div);
    }
  }

  rolesEl.onclick = async (e)=>{
    const r = e.target.closest('input[type="radio"]'); if(!r) return;
    currentRoleId = +r.value;
    card.querySelector('#permTitle').textContent = `Permissions (Role #${currentRoleId})`;
    try{
      const data = await listRolePermissions(currentRoleId);
      assigned = new Set(data.map(x=>x.id));
      renderPerms();
    }catch(err){ showMsg('Load error: '+(err.friendly||errorToString(err)), true); }
  };

  permsEl.onclick = (e)=>{
    const c = e.target.closest('input[type="checkbox"]'); if(!c) return;
    const id = +c.value;
    if (c.checked) assigned.add(id); else assigned.delete(id);
  };

  if (btnSave) btnSave.onclick = async ()=>{
    if (!currentRoleId) return alert('Select a role first');
    try{
      await assignRolePermissions({ roleId: currentRoleId, permissionIds: Array.from(assigned) });
      showMsg('Permissions saved.');
    }catch(err){ showMsg('Save error: '+(err.friendly||errorToString(err)), true); }
  };

  try{
    [roles, perms] = await Promise.all([listRoles(), listPermissions()]);
    renderRoles(); renderPerms();
    // auto-selecionar se vier roleId na querystring
    const qs=new URLSearchParams(location.search); const rid=qs.get('roleId');
    if (rid && roles.some(r=>r.id==rid)) {
      const radio = rolesEl.querySelector(`input[value="${rid}"]`);
      if (radio){ radio.checked=true; radio.dispatchEvent(new Event('click')); }
    }
  }catch(err){ showMsg('Init error: '+(err.friendly||errorToString(err)), true); }
}
