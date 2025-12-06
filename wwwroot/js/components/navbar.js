// wwwroot/js/components/navbar.js
import { auth, isSessionValid, logout } from '../api.js';
import { navigate } from '../router.js';

let currentAccessTab = window.__acActiveTab || 'users';

window.addEventListener('ac-tab-changed', ev => {
  if (ev.detail?.key) {
    currentAccessTab = ev.detail.key;
    window.__acActiveTab = currentAccessTab;
    renderNavbar();
  }
});

function getModuleTitle(path) {
  switch (path) {
    case '/access': return "Access Control";
    default: return "";
  }
}

export function renderNavbar() {
  const hash = (location.hash || '#/home').replace('#', '');
  const currentPath = hash.split('?')[0];

  renderUserArea();
  renderModuleMenu(currentPath);
}

function renderModuleMenu(currentPath) {
  const navbar = document.querySelector('.navbar');
  if (!navbar) return;

  let sub = document.getElementById('module-links');
  if (!sub) {
    sub = document.createElement('div');
    sub.id = 'module-links';
    sub.className = 'subnav';
    navbar.appendChild(sub);
  }

  const title = getModuleTitle(currentPath);
  const isModule = !!title;

  if (!isModule) {
    sub.style.display = 'none';
    sub.innerHTML = '';
    return;
  }

  sub.style.display = 'flex';
  sub.innerHTML = '';

  // título
  const titleEl = document.createElement('span');
  titleEl.className = 'nav-module-title';
  titleEl.textContent = title;
  sub.appendChild(titleEl);

  const actions = document.createElement('div');
  actions.className = 'subnav-actions';

  actions.appendChild(buildDropdown("Register", [
    { key: "users", label: "Users" },
    { key: "roles", label: "Roles" },
    { key: "permissions", label: "Permissions" }
  ]));

  actions.appendChild(buildDropdown("Cross Register", [
    { key: "roleperms", label: "Role × Permissions" },
    { key: "userroles", label: "User × Roles" }
  ]));

  sub.appendChild(actions);
}

function buildDropdown(label, items) {
  const wrap = document.createElement('div');
  wrap.className = 'dropdown';

  wrap.innerHTML = `
    <button class="dropdown-toggle">${label} ▾</button>
    <div class="dropdown-menu"></div>
  `;

  const menu = wrap.querySelector('.dropdown-menu');

  items.forEach(it => {
    const btn = document.createElement('button');
    btn.className = 'dropdown-item';
    btn.textContent = it.label;
    if (it.key === currentAccessTab) btn.classList.add('active');

    btn.onclick = () => {
      currentAccessTab = it.key;
      window.__acActiveTab = it.key;
      window.dispatchEvent(new CustomEvent("ac-tab-change", { detail: { key: it.key } }));
      renderNavbar();
      menu.classList.remove('open');
    };

    menu.appendChild(btn);
  });

  wrap.querySelector('.dropdown-toggle').onclick = (e) => {
    e.stopPropagation();
    document.querySelectorAll('.dropdown-menu.open').forEach(m => m.classList.remove('open'));
    menu.classList.toggle('open');
  };

  return wrap;
}

// click fora fecha dropdown
window.addEventListener('click', () => {
  document.querySelectorAll('.dropdown-menu.open').forEach(m => m.classList.remove('open'));
});

export function renderUserArea() {
  const el = document.getElementById('topUserArea');
  if (!el) return;

  el.innerHTML = '';

  const wrap = document.createElement('div');
  wrap.className = 'top-actions';

  const label = document.createElement('span');
  label.className = 'label';
  label.textContent = `Logged User: ${auth.username}`;
  wrap.appendChild(label);

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

// rerender ao trocar rota
window.addEventListener('hashchange', renderNavbar);
