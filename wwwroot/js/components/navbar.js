// wwwroot/js/components/navbar.js
import { auth, isSessionValid, logout } from "../api.js";
import { navigate } from "../router.js";

/* ============================================================
   CONFIGURAÇÃO DOS MÓDULOS (MENU DINÂMICO)
   Cada módulo controla:
   - título
   - abas e dropdowns
   - eventos
   - armazenamento do tab ativo
============================================================ */

const MODULE_MENUS = {
  "/access": {
    title: "Access Control",
    storageKey: "__acActiveTab",
    defaultTab: "users",
    eventChange: "ac-tab-change",
    eventChanged: "ac-tab-changed",
    groups: [
      {
        label: "Register",
        items: [
          { key: "users", label: "Users" },
          { key: "roles", label: "Roles" },
          { key: "permissions", label: "Permissions" }
        ]
      },
      {
        label: "Cross Register",
        items: [
          { key: "roleperms", label: "Role × Permissions" },
          { key: "userroles", label: "User × Roles" }
        ]
      }
    ]
  },

  "/items": {
    title: "Items",
    storageKey: "__itemsActiveTab",
    defaultTab: "list",
    eventChange: "items-tab-change",
    eventChanged: "items-tab-changed",
    groups: [
      {
        label: "Register",
        items: [
          { key: "list", label: "List" },
          { key: "create", label: "New Item" }
        ]
      }
    ]
  },

  "/static-data": {
    title: "Static Data",
    storageKey: "__staticActiveTab",
    defaultTab: "countries",
    eventChange: "static-tab-change",
    eventChanged: "static-tab-changed",
    groups: [
      {
        label: "Register",
        items: [
          { key: "countries", label: "Countries" },
          { key: "states", label: "States" }
        ]
      }
    ]
  },

  "/logs": {
    title: "Logs",
    storageKey: "__logsActiveTab",
    defaultTab: "system",
    eventChange: "logs-tab-change",
    eventChanged: "logs-tab-changed",
    groups: [
      {
        label: "Filter",
        items: [
          { key: "system", label: "System Logs" },
          { key: "access", label: "Access Logs" }
        ]
      }
    ]
  }
};

/* ============================================================
   HELPERS
============================================================ */

function getModuleConfig(path) {
  return MODULE_MENUS[path] || null;
}

function getActiveTab(cfg) {
  return window[cfg.storageKey] || cfg.defaultTab;
}

function setActiveTab(cfg, key) {
  window[cfg.storageKey] = key;
}

/* ============================================================
   RENDER DO MENU DO MÓDULO
============================================================ */

export function renderNavbar() {
  const hash = (location.hash || "#/home").replace("#", "");
  const currentPath = hash.split("?")[0];

  renderUserArea();
  renderModuleMenu(currentPath);
}

function renderModuleMenu(currentPath) {
  const navbar = document.querySelector(".navbar");
  if (!navbar) return;

  let sub = document.getElementById("module-links");
  if (!sub) {
    sub = document.createElement("div");
    sub.id = "module-links";
    sub.className = "subnav";
    navbar.appendChild(sub);
  }

  const cfg = getModuleConfig(currentPath);
  if (!cfg) {
    sub.style.display = "none";
    sub.innerHTML = "";
    return;
  }

  sub.style.display = "flex";
  sub.innerHTML = "";

  // Título
  const titleEl = document.createElement("span");
  titleEl.className = "nav-module-title";
  titleEl.textContent = cfg.title;
  sub.appendChild(titleEl);

  // Ações
  const actions = document.createElement("div");
  actions.className = "subnav-actions";

  const currentTab = getActiveTab(cfg);

  cfg.groups.forEach(group => {
    actions.appendChild(buildDropdown(cfg, group.label, group.items, currentTab));
  });

  sub.appendChild(actions);
}

function buildDropdown(cfg, groupLabel, items, currentTab) {
  const wrap = document.createElement("div");
  wrap.className = "dropdown";

  wrap.innerHTML = `
    <button class="dropdown-toggle">${groupLabel} ▾</button>
    <div class="dropdown-menu"></div>
  `;

  const menu = wrap.querySelector(".dropdown-menu");

  items.forEach(it => {
    const btn = document.createElement("button");
    btn.className = "dropdown-item";
    btn.textContent = it.label;

    if (it.key === currentTab) btn.classList.add("active");

    btn.onclick = () => {
      setActiveTab(cfg, it.key);

      window.dispatchEvent(
        new CustomEvent(cfg.eventChange, { detail: { key: it.key } })
      );

      menu.classList.remove("open");
      renderNavbar();
    };

    menu.appendChild(btn);
  });

  wrap.querySelector(".dropdown-toggle").onclick = e => {
    e.stopPropagation();
    document
      .querySelectorAll(".dropdown-menu.open")
      .forEach(m => m.classList.remove("open"));
    menu.classList.toggle("open");
  };

  return wrap;
}

/* Fecha dropdown ao clicar fora */
window.addEventListener("click", () => {
  document.querySelectorAll(".dropdown-menu.open").forEach(m => m.classList.remove("open"));
});

/* ============================================================
   ÁREA DO USUÁRIO (Home, Settings, Logout)
============================================================ */

export function renderUserArea() {
  const el = document.getElementById("topUserArea");
  if (!el) return;

  el.innerHTML = "";

  if (!isSessionValid()) return;

  const wrap = document.createElement("div");
  wrap.className = "top-actions";

  // Label com usuário
  const label = document.createElement("span");
  label.className = "label";
  label.textContent = "Logged User: " + auth.username;
  wrap.appendChild(label);

  // HOME
  const homeBtn = document.createElement('button');
  homeBtn.className = 'icon-btn';
  homeBtn.innerHTML = `
    <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M3 10.5 12 3l9 7.5" />
      <path d="M5 10v10h14V10" />
      <path d="M10 20v-6h4v6" />
    </svg>`;
  homeBtn.onclick = () => navigate('/home');
  wrap.appendChild(homeBtn);

  const settingsBtn = document.createElement('button');
  settingsBtn.className = 'icon-btn';
  settingsBtn.innerHTML = `
      <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M12 15.5A3.5 3.5 0 1 0 12 8.5a3.5 3.5 0 0 0 0 7z"/>
        <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09A1.65 1.65 0 0 0 8 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 3.6 15a1.65 1.65 0 0 0-1.51-1H2a2 2 0 1 1 0-4h.09A1.65 1.65 0 0 0 3.6 8a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 8 3.6a1.65 1.65 0 0 0 1-1.51V2a2 2 0 1 1 4 0v.09A1.65 1.65 0 0 0 16 3.6a1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 20.4 8c.32.52.5 1.13.5 1.77 0 .64-.18 1.25-.5 1.77z"/>
      </svg>`;
    settingsBtn.onclick = () => navigate('/settings');
  
  wrap.appendChild(settingsBtn);

  // SIGN OUT – ícone “power”
  const signOutBtn = document.createElement('button');
  signOutBtn.className = 'icon-btn';
  signOutBtn.title = 'Sign out';
  signOutBtn.innerHTML = `
    <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M12 2v10"/>
      <path d="M5.1 7.05a8 8 0 1 0 13.8 0"/>
    </svg>`;
  signOutBtn.onclick = async () => {
    try {
      await logout();
    } catch (err) {
      console.error('[navbar] erro ao chamar logout', err);
    }
    auth.clear?.();
    navigate('/login');
  };
  wrap.appendChild(signOutBtn);

  el.appendChild(wrap);
}

/* Re-render ao trocar hash */
window.addEventListener("hashchange", renderNavbar);


// -------

  