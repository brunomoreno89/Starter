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

const can = (perm) => auth.hasPerm(perm) || auth.hasRole("Admin");

/* -----------------------------------------------------------------
   ACCESS VIEW  (SEM MENU INTERNO – CONTROLADO PELA NAVBAR)
------------------------------------------------------------------ */

export async function AccessView(container) {
  container.innerHTML = `
    <div class="card">

      <div id="ac-msg" class="alert hidden"></div>

      <div id="pane-users" class="ac-pane hidden"></div>
      <div id="pane-roles" class="ac-pane hidden"></div>
      <div id="pane-permissions" class="ac-pane hidden"></div>
      <div id="pane-roleperms" class="ac-pane hidden"></div>
      <div id="pane-userroles" class="ac-pane hidden"></div>

    </div>
  `;

  const TABS = [
    { key: "users",       label: "Users",              perm: "Users.Read",             render: renderUsers },
    { key: "roles",       label: "Roles",              perm: "Roles.Read",             render: renderRoles },
    { key: "permissions", label: "Permissions",        perm: "Permissions.Read",       render: renderPermissions },
    { key: "roleperms",   label: "Role × Permissions", perm: "RolePermissions.Assign", render: renderRolePermissions },
    { key: "userroles",   label: "User × Roles",       perm: "UserRoles.Assign",       render: renderUserRoles }
  ];

  const visible = TABS.filter(t => can(t.perm));

  const msg = container.querySelector('#ac-msg');
  if (!visible.length) {
    msg.classList.remove('hidden');
    msg.innerHTML = `You don't have permission to access this module.`;
    return;
  }

  let activeKey = window.__acActiveTab || visible[0].key;

  async function activateTab(key) {
    if (!key) return;
    const tab = TABS.find(t => t.key === key);
    if (!tab) return;
    if (!can(tab.perm)) return;

    activeKey = key;
    window.__acActiveTab = key;

    // esconde todos os panes
    container.querySelectorAll(".ac-pane").forEach(p =>
      p.classList.add("hidden")
    );

    const paneId = {
      users:       "pane-users",
      roles:       "pane-roles",
      permissions: "pane-permissions",
      roleperms:   "pane-roleperms",
      userroles:   "pane-userroles"
    }[key];

    const pane = container.querySelector("#" + paneId);
    if (!pane) return;
    pane.classList.remove("hidden");

    try {
      await tab.render(pane, container);

      // avisa a NAVBAR qual aba está ativa
      window.dispatchEvent(new CustomEvent("ac-tab-changed", {
        detail: { key }
      }));
    } catch (err) {
      pane.innerHTML = `<div class="alert error">${errorToString(err)}</div>`;
    }
  }

  // listener vindo da NAVBAR (clicou no dropdown lá em cima)
  if (window.__acTabChangeHandler) {
    window.removeEventListener("ac-tab-change", window.__acTabChangeHandler);
  }

  window.__acTabChangeHandler = (ev) => {
    const key = ev.detail?.key;
    if (!key || key === activeKey) return;
    activateTab(key);
  };

  window.addEventListener("ac-tab-change", window.__acTabChangeHandler);

  // primeira aba
  await activateTab(activeKey);
}



/* -----------------------------------------------------------------
   RENDER USERS
------------------------------------------------------------------ */

async function renderUsers(pane) {
  const allowCreate = can("Users.Create");
  const allowUpdate = can("Users.Update");
  const allowDelete = can("Users.Delete");

  pane.innerHTML = `
    <div class="view-header">
      <h3>Users</h3>
      ${allowCreate ? `<button class="icon-btn" id="uNew">+</button>` : ""}
    </div>

    <div class="table-wrap compact-table">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th><th>Username</th><th>Name</th><th>Email</th>
            <th>Active</th><th>Creation Dt</th><th>Created By</th>
            <th>Update Dt</th><th>Updated By</th><th></th>
          </tr>
        </thead>
        <tbody id="uBody"></tbody>
      </table>
    </div>
    <div id="uPager"></div>
  `;

  let page = 1;
  let data = [];

  async function reload() {
    try { data = await listUsers(); }
    catch (e) { data = []; }
    fill();
  }

  function fill() {
    const tb = pane.querySelector("#uBody");
    tb.innerHTML = "";

    const { items, total, pages } = paginate(data, page, getPageSize());

    for (const u of items) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${u.id}</td>
        <td>${u.username}</td>
        <td>${u.name}</td>
        <td>${u.email}</td>
        <td>${u.active}</td>
        <td>${formatDateToBR(u.creationDt)}</td>
        <td>${u.createdByName ?? ""}</td>
        <td>${formatDateToBR(u.updatedDt)}</td>
        <td>${u.updatedByName ?? ""}</td>
        <td style="text-align:right;">
          ${allowUpdate ? `<button class="icon-btn" data-act="edit">✎</button>` : ""}
          ${allowDelete ? `<button class="icon-btn" data-act="del">🗑</button>` : ""}
        </td>
      `;

      // editar
      tr.querySelector('[data-act="edit"]')?.addEventListener("click", async () => {
        let current = await getUser(u.id);
        openModal(
          "Edit User #" + u.id,
          body => {
            body.innerHTML = `
              <label>Username</label><input id="mUUserName" value="${current.username}">
              <label>Name</label><input id="mUName" value="${current.name}">
              <label>Email</label><input id="mUEmail" value="${current.email}">
              <label>Active</label>
              <select id="mUActive">
                <option value="Yes" ${current.active === "Yes" ? "selected" : ""}>Yes</option>
                <option value="No"  ${current.active === "No" ? "selected" : ""}>No</option>
              </select>
            `;
          },
          async () => {
            const dto = {
              id: u.id,
              username: mUUserName.value.trim(),
              name: mUName.value.trim(),
              email: mUEmail.value.trim(),
              active: mUActive.value
            };
            await updateUser(u.id, dto);
            await reload();
          }
        );
      });

      // delete
      tr.querySelector('[data-act="del"]')?.addEventListener("click", () => {
        openConfirm(
          "Delete User",
          `<p>Are you sure you want to delete <strong>${u.username}</strong>?</p>`,
          async () => {
            await deleteUser(u.id);
            await reload();
          }
        );
      });

      tb.appendChild(tr);
    }

    renderPager(pane.querySelector("#uPager"), { total, page, pages }, p => {
      page = p;
      fill();
    });
  }

  pane.querySelector("#uNew")?.addEventListener("click", () => {
    openModal(
      "New User",
      body => {
        body.innerHTML = `
          <label>Username</label><input id="nUUser">
          <label>Name</label><input id="nUName">
          <label>Email</label><input id="nUEmail">
          <label>Password</label><input id="nUPwd" type="password">
          <label>Active</label>
          <select id="nUActive"><option>Yes</option><option>No</option></select>
        `;
      },
      async () => {
        await createUser({
          username: nUUser.value.trim(),
          name: nUName.value.trim(),
          email: nUEmail.value.trim(),
          password: nUPwd.value.trim(),
          active: nUActive.value
        });
        await reload();
      }
    );
  });

  await reload();
}

/* -----------------------------------------------------------------
   RENDER ROLES
------------------------------------------------------------------ */

async function renderRoles(pane) {
  const allowCreate = can("Roles.Create");
  const allowUpdate = can("Roles.Update");
  const allowDelete = can("Roles.Delete");

  pane.innerHTML = `
    <div class="view-header">
      <h3>Roles</h3>
      ${allowCreate ? `<button class="icon-btn" id="rNew">+</button>` : ""}
    </div>

    <div class="table-wrap compact-table">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th><th>Name</th><th>Description</th><th>Active</th>
            <th>Creation Dt</th><th>Created By</th><th>Update Dt</th><th>Updated By</th><th></th>
          </tr>
        </thead>
        <tbody id="rBody"></tbody>
      </table>
    </div>
    <div id="rPager"></div>
  `;

  let page = 1;
  let roles = [];

  async function reload() {
    roles = await listRoles();
    fill();
  }

  function fill() {
    const tb = pane.querySelector("#rBody");
    tb.innerHTML = "";

    const { items, total, pages } = paginate(roles, page, getPageSize());

    for (const r of items) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${r.id}</td>
        <td>${r.name}</td>
        <td>${r.description}</td>
        <td>${r.active}</td>
        <td>${formatDateToBR(r.creationDt)}</td>
        <td>${r.createdByName ?? ""}</td>
        <td>${formatDateToBR(r.updateDt)}</td>
        <td>${r.updatedByName ?? ""}</td>
        <td>
          ${allowUpdate ? `<button class="icon-btn" data-act="edit">✎</button>` : ""}
          ${allowDelete ? `<button class="icon-btn" data-act="del">🗑</button>` : ""}
        </td>
      `;

      // editar
      tr.querySelector('[data-act="edit"]')?.addEventListener("click", () => {
        openModal(
          "Edit Role #" + r.id,
          body => {
            body.innerHTML = `
              <label>Name</label><input id="mRName" value="${r.name}">
              <label>Description</label><textarea id="mRDesc">${r.description ?? ""}</textarea>
              <label>Active</label>
              <select id="mRActive">
                <option value="Yes" ${r.active === "Yes" ? "selected" : ""}>Yes</option>
                <option value="No"  ${r.active === "No" ? "selected" : ""}>No</option>
              </select>
            `;
          },
          async () => {
            await updateRole(r.id, {
              id: r.id,
              name: mRName.value.trim(),
              description: mRDesc.value.trim(),
              active: mRActive.value
            });
            await reload();
          }
        );
      });

      // deletar
      tr.querySelector('[data-act="del"]')?.addEventListener("click", () => {
        openConfirm(
          "Delete Role",
          `<p>Delete role <strong>${r.name}</strong>?</p>`,
          async () => {
            await deleteRole(r.id);
            await reload();
          }
        );
      });

      tb.appendChild(tr);
    }

    renderPager(pane.querySelector("#rPager"), { total, page, pages }, p => {
      page = p;
      fill();
    });
  }

  pane.querySelector("#rNew")?.addEventListener("click", () => {
    openModal(
      "New Role",
      body => {
        body.innerHTML = `
            <label>Name</label><input id="nRName">
            <label>Description</label><textarea id="nRDesc"></textarea>
        `;
      },
      async () => {
        await createRole({
          name: nRName.value.trim(),
          description: nRDesc.value.trim()
        });
        await reload();
      }
    );
  });

  await reload();
}

/* -----------------------------------------------------------------
   RENDER PERMISSIONS
------------------------------------------------------------------ */

async function renderPermissions(pane) {
  const allowCreate = can("Permissions.Create");
  const allowUpdate = can("Permissions.Update");
  const allowDelete = can("Permissions.Delete");

  pane.innerHTML = `
    <div class="view-header">
      <h3>Permissions</h3>
      ${allowCreate ? `<button class="icon-btn" id="pNew">+</button>` : ""}
    </div>
    <div id="perm-tabs" class="tabs"></div>

    <div class="table-wrap compact-table">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th><th>Permission</th><th>Description</th><th>Active</th>
            <th>CreationDt</th><th>CreatedBy</th><th>UpdateDt</th><th>UpdatedBy</th><th></th>
          </tr>
        </thead>
        <tbody id="pBody"></tbody>
      </table>
    </div>
    <div id="pPager"></div>
  `;

  let allPerms = [];
  let groupMap = new Map();
  let groups = [];
  let activeGroup = "General";
  let page = 1;

  async function reload() {
    allPerms = await listPermissions();
    buildGroups();
    paintTabs();
    fillTable();
  }

  function buildGroups() {
    groupMap = new Map();
    for (const p of allPerms) {
      const name = p.name || "";
      const idx = name.indexOf(".");
      const g = idx > 0 ? name.slice(0, idx) : "General";

      if (!groupMap.has(g)) groupMap.set(g, []);
      groupMap.get(g).push(p);
    }

    groups = [...groupMap.keys()].sort();
    if (!groupMap.has(activeGroup)) activeGroup = groups[0];
  }

  function paintTabs() {
    const host = pane.querySelector("#perm-tabs");
    host.innerHTML = "";

    for (const g of groups) {
      const btn = document.createElement("button");
      btn.className = "tab" + (g === activeGroup ? " active" : "");
      btn.textContent = g;
      btn.onclick = () => {
        activeGroup = g;
        page = 1;
        paintTabs();
        fillTable();
      };
      host.appendChild(btn);
    }
  }

  function permsOfGroup() {
    return (groupMap.get(activeGroup) || []).sort((a, b) =>
      (a.name || "").localeCompare(b.name || "")
    );
  }

  function fillTable() {
    const tb = pane.querySelector("#pBody");
    tb.innerHTML = "";

    const list = permsOfGroup();
    const { items, total, pages } = paginate(list, page, getPageSize());

    for (const p of items) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${p.id}</td>
        <td>${p.name}</td>
        <td>${p.description}</td>
        <td>${p.active}</td>
        <td>${formatDateToBR(p.creationDt)}</td>
        <td>${p.createdByName ?? ""}</td>
        <td>${formatDateToBR(p.updateDt)}</td>
        <td>${p.updatedByName ?? ""}</td>
        <td>
          ${allowUpdate ? `<button class="icon-btn" data-act="edit">✎</button>` : ""}
          ${allowDelete ? `<button class="icon-btn" data-act="del">🗑</button>` : ""}
        </td>
      `;

      tr.querySelector('[data-act="edit"]')?.addEventListener("click", () => {
        openModal(
          "Edit Permission #" + p.id,
          body => {
            body.innerHTML = `
              <label>Name</label><input id="mPName" value="${p.name}">
              <label>Description</label><textarea id="mPDesc">${p.description ?? ""}</textarea>
              <label>Active</label>
              <select id="mPActive">
                <option value="Yes" ${p.active === "Yes" ? "selected" : ""}>Yes</option>
                <option value="No"  ${p.active === "No" ? "selected" : ""}>No</option>
              </select>
            `;
          },
          async () => {
            await updatePermission(p.id, {
              id: p.id,
              name: mPName.value.trim(),
              description: mPDesc.value.trim(),
              active: mPActive.value
            });
            await reload();
          }
        );
      });

      tr.querySelector('[data-act="del"]')?.addEventListener("click", () => {
        openConfirm(
          "Delete Permission",
          `<p>Delete <strong>${p.name}</strong>?</p>`,
          async () => {
            await deletePermission(p.id);
            await reload();
          }
        );
      });

      tb.appendChild(tr);
    }

    renderPager(pane.querySelector("#pPager"), { total, page, pages }, p => {
      page = p;
      fillTable();
    });
  }

  // NEW PERMISSION
  pane.querySelector("#pNew")?.addEventListener("click", () => {
    const defaultGroup = activeGroup !== "General" ? activeGroup + "." : "";

    openModal(
      "New Permission",
      body => {
        body.innerHTML = `
          <label>Name</label><input id="nPName" value="${defaultGroup}">
          <label>Description</label><textarea id="nPDesc"></textarea>
        `;
      },
      async () => {
        await createPermission({
          name: nPName.value.trim(),
          description: nPDesc.value.trim()
        });
        await reload();
      }
    );
  });

  await reload();
}

/* -----------------------------------------------------------------
   ROLE × PERMISSIONS
------------------------------------------------------------------ */

async function renderRolePermissions(pane) {
  if (!can("RolePermissions.Assign")) {
    pane.innerHTML = `<div class="alert error">No permission.</div>`;
    return;
  }

  const roles = await listRoles();
  const allPerms = await listPermissions();

  let activeRoleId = roles[0]?.id;
  let selected = new Set();
  let groupMap = new Map();
  let groups = [];
  let activeGroup = "General";
  let page = 1;

  pane.innerHTML = `
    <div class="view-header" style="gap:12px">
      <h3>Role × Permissions</h3>
      <label style="margin-left:auto; display:flex; gap:8px; align-items:center;">
        <span>Role</span>
        <select id="rp-role"></select>
      </label>
    </div>

    <div id="rp-tabs" class="tabs"></div>
    <div id="rp-grid"></div>
    <button class="primary" id="rp-save" style="margin-top:12px">Save</button>
  `;

  const sel = pane.querySelector("#rp-role");
  sel.innerHTML = roles.map(r => `<option value="${r.id}">${r.name}</option>`);
  sel.value = activeRoleId;

  // Organiza grupos
  for (const p of allPerms) {
    const name = p.name || "";
    const idx = name.indexOf(".");
    const g = idx > 0 ? name.slice(0, idx) : "General";

    if (!groupMap.has(g)) groupMap.set(g, []);
    groupMap.get(g).push(p);
  }

  groups = [...groupMap.keys()].sort();

  async function loadSelected() {
    const rp = await listRolePermissions(activeRoleId);
    selected = new Set(rp.map(x => x.permissionId ?? x.id));
  }

  function paintTabs() {
    const host = pane.querySelector("#rp-tabs");
    host.innerHTML = "";

    for (const g of groups) {
      const b = document.createElement("button");
      b.className = "tab" + (g === activeGroup ? " active" : "");
      b.textContent = g;
      b.onclick = () => {
        activeGroup = g;
        page = 1;
        paintTabs();
        renderGrid();
      };
      host.appendChild(b);
    }
  }

  function renderGrid() {
    const grid = pane.querySelector("#rp-grid");

    const perms = (groupMap.get(activeGroup) || []).sort((a, b) =>
      (a.name || "").localeCompare(b.name || "")
    );

    const { items, total, pages } = paginate(perms, page, getPageSize());

    grid.innerHTML = `
      <div class="table-wrap compact-table">
        <table class="table">
          <thead>
            <tr><th>Allow</th><th>Permission</th><th>Description</th></tr>
          </thead>
          <tbody id="rpBody"></tbody>
        </table>
      </div>
      <div id="rpPager"></div>
    `;

    const tb = grid.querySelector("#rpBody");

    for (const p of items) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td><input type="checkbox" data-id="${p.id}" ${selected.has(p.id) ? "checked" : ""}></td>
        <td>${p.name}</td>
        <td>${p.description}</td>
      `;

      tr.querySelector("input").onclick = e => {
        const id = Number(e.target.dataset.id);
        if (e.target.checked) selected.add(id);
        else selected.delete(id);
      };

      tb.appendChild(tr);
    }

    renderPager(grid.querySelector("#rpPager"), { total, page, pages }, p => {
      page = p;
      renderGrid();
    });
  }

  pane.querySelector("#rp-save").onclick = async () => {
    await assignRolePermissions({
      roleId: activeRoleId,
      permissionIds: [...selected]
    });
  };

  sel.onchange = async () => {
    activeRoleId = Number(sel.value);
    await loadSelected();
    renderGrid();
  };

  paintTabs();
  await loadSelected();
  renderGrid();
}

/* -----------------------------------------------------------------
   USER × ROLES
------------------------------------------------------------------ */

async function renderUserRoles(pane) {
  if (!can("UserRoles.Assign")) {
    pane.innerHTML = `<div class="alert error">No permission.</div>`;
    return;
  }

  const users = await listUsers();
  const roles = (await listRoles()).sort((a, b) =>
    (a.name || "").localeCompare(b.name || "")
  );

  let activeUserId = users[0]?.id;
  let selected = new Set();
  let page = 1;

  pane.innerHTML = `
    <div class="view-header" style="gap:12px">
      <h3>User × Roles</h3>
      <label style="margin-left:auto; display:flex; gap:8px; align-items:center;">
        <span>User</span>
        <select id="ur-user"></select>
      </label>
    </div>

    <div id="ur-grid"></div>
    <button class="primary" id="ur-save" style="margin-top:12px">Save</button>
  `;

  const sel = pane.querySelector("#ur-user");
  sel.innerHTML = users
    .map(u => `<option value="${u.id}">${u.username}</option>`)
    .join("");
  sel.value = activeUserId;

  async function loadSelected() {
    const ur = await listUserRoles(activeUserId);
    selected = new Set(ur.map(x => x.roleId ?? x.id));
  }

  function renderGrid() {
    const grid = pane.querySelector("#ur-grid");

    const { items, total, pages } = paginate(roles, page, getPageSize());

    grid.innerHTML = `
      <div class="table-wrap compact-table">
        <table class="table">
          <thead><tr><th>Allow</th><th>Role</th><th>Description</th></tr></thead>
          <tbody id="urBody"></tbody>
        </table>
      </div>
      <div id="urPager"></div>
    `;

    const tb = grid.querySelector("#urBody");

    for (const r of items) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td><input type="checkbox" data-id="${r.id}" ${selected.has(r.id) ? "checked" : ""}></td>
        <td>${r.name}</td>
        <td>${r.description}</td>
      `;

      tr.querySelector("input").onclick = e => {
        const id = Number(e.target.dataset.id);
        if (e.target.checked) selected.add(id);
        else selected.delete(id);
      };

      tb.appendChild(tr);
    }

    renderPager(grid.querySelector("#urPager"), { total, page, pages }, p => {
      page = p;
      renderGrid();
    });
  }

  pane.querySelector("#ur-save").onclick = async () => {
    await assignUserRoles({
      userId: activeUserId,
      roleIds: [...selected]
    });
  };

  sel.onchange = async () => {
    activeUserId = Number(sel.value);
    await loadSelected();
    renderGrid();
  };

  await loadSelected();
  renderGrid();
}
