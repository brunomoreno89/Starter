// wwwroot/js/components/navbar.js
import { auth, isSessionValid , logout, getUser} from '../api.js';
import { navigate } from '../router.js';

export function renderNavbar() {
  const links = document.getElementById('nav-links');
  if (!links) return;
  links.innerHTML = '';

  if (isSessionValid()) {
    // Home (sempre visível após login)
    const homeLink = document.createElement('a');
    homeLink.href = '#/home';
    homeLink.textContent = 'Home';
    links.appendChild(homeLink);

    // Items
    if (auth.hasPerm && auth.hasPerm('Items.Read')) {
      const a = document.createElement('a');
      a.href = '#/items';
      a.textContent = 'Items';
      links.appendChild(a);
    }
    
    // Access
    if (auth.hasPerm && auth.hasPerm('Users.Read')) {
      const a = document.createElement('a');
      a.href = '#/access';
      a.textContent = 'Access';
      links.appendChild(a);
    }

    // Logs
    if (auth.hasPerm && auth.hasPerm('Logs.Read')) {
      const a = document.createElement('a');
      a.href = '#/logs';
      a.textContent = 'Logs';
      links.appendChild(a);
    }
    setActiveLink();
  }

  renderUserArea();
}

  export async function renderUserArea() {
    const el = document.getElementById('topUserArea');
    if (!el) return;
    el.innerHTML = '';

    if (!isSessionValid()) return;

    const wrap = document.createElement('div');
    wrap.className = 'top-actions';

    // Username / Nome na navbar
    const label = document.createElement('span');
    label.className = 'label';
    label.textContent = 'Logged User: '+auth.username;
    wrap.appendChild(label);


  // Settings (gear icon) — mostra apenas se tiver permissão
  if (auth.hasPerm && auth.hasPerm('Settings.Access')) {
    const settingsBtn = document.createElement('button');
    settingsBtn.className = 'icon-btn';
    settingsBtn.title = 'Settings';
    settingsBtn.innerHTML = `
      <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M12 15.5A3.5 3.5 0 1 0 12 8.5a3.5 3.5 0 0 0 0 7z"/>
        <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09A1.65 1.65 0 0 0 8 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 3.6 15a1.65 1.65 0 0 0-1.51-1H2a2 2 0 1 1 0-4h.09A1.65 1.65 0 0 0 3.6 8a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 8 3.6a1.65 1.65 0 0 0 1-1.51V2a2 2 0 1 1 4 0v.09A1.65 1.65 0 0 0 16 3.6a1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 20.4 8c.32.52.5 1.13.5 1.77 0 .64-.18 1.25-.5 1.77z"/>
      </svg>`;
    settingsBtn.onclick = () => navigate('/settings');
    wrap.appendChild(settingsBtn);
  }

  // Sign out (power icon)
  const signOutBtn = document.createElement('button');
  signOutBtn.className = 'icon-btn';
  signOutBtn.title = 'Sign out';
  signOutBtn.innerHTML = `
    <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M12 2v10"/>
      <path d="M5.1 7.05a8 8 0 1 0 13.8 0"/>
    </svg>`;
  /* signOutBtn.onclick = () => { */
  signOutBtn.onclick = async () => {
    console.log('[navbar] logout clicked');
    try {
      await logout(); // chama API e deve aparecer no Network
    } catch (err) {
      console.error('[navbar] erro ao chamar logout', err);
    }


    auth.clear?.();
    renderNavbar();
    renderUserArea();
    navigate('/login');
  };
  wrap.appendChild(signOutBtn);

  el.appendChild(wrap);
}

export function setActiveLink() {
  const links = document.getElementById('nav-links');
  if (!links) return;
  const anchors = [...links.querySelectorAll('a')];
  anchors.forEach(a => a.classList.remove('active'));
  const current = location.hash || '#/home';
  anchors.forEach(a => {
    if (current.startsWith(a.getAttribute('href'))) a.classList.add('active');
  });
}
window.addEventListener('hashchange', setActiveLink);
