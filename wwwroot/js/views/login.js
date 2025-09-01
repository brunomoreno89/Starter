// wwwroot/js/views/login.js
import { login, auth, errorToString } from '../api.js';
import { navigate } from '../router.js';
import { renderNavbar, renderUserArea } from '../components/navbar.js';

export async function LoginView(container) {
  const card = document.createElement('div');
  card.className = 'card';
  card.innerHTML = `
    <div class="login-viewport">
      <div class="card login-card">
        <label>Username</label>
        <input id="lUser" value="admin">

        <label>Password</label>
        <input id="lPass" type="password" value="ChangeMe123!">

        <div class="row" style="margin-top:8px;">
          <button class="primary" id="btnLogin">Login</button>
        </div>

        <div id="lOut" class="alert hidden"></div>
      </div>
    </div>
  `;
  container.appendChild(card);

  // Ajuste de altura (equivalente ao <script> inline que estava no HTML)
  const shell = card.querySelector('.login-viewport');
  function fit() {
    const top = (document.querySelector('.topbar')?.offsetHeight || 0)
              + (document.querySelector('.navbar')?.offsetHeight || 0);
    const bottom = (document.querySelector('.app-footer')?.offsetHeight || 0);
    shell.style.setProperty('--top-offset', top + 'px');
    shell.style.setProperty('--bottom-offset', bottom + 'px');
  }
  fit();
  window.addEventListener('resize', fit, { passive: true });

  const btn = card.querySelector('#btnLogin');
  const userInput = card.querySelector('#lUser');
  const passInput = card.querySelector('#lPass');
  const out = card.querySelector('#lOut');

  function showError(msg) {
    out.className = 'alert error';
    out.textContent = msg;
  }
  function clearError() {
    out.className = 'alert hidden';
    out.textContent = '';
  }

  async function doLogin() {
    const u = userInput.value.trim();
    const p = passInput.value;

    clearError();
    if (!u || !p) {
      showError('Please enter username and password.');
      return;
    }

    try {
      btn.disabled = true;
      const oldText = btn.textContent;
      btn.textContent = 'Signing in...';

      // Chama a API de login; espera { token, ... }
      const resp = await login(u, p);

      // Salva o token **sem** "Bearer " e atualiza UI
      auth.saveToken(resp?.token || '');

      // Re-render da barra e navegação
      renderNavbar();
      renderUserArea();
      navigate('/home');
    } catch (e) {
      showError('Login failed: ' + (e.friendly || errorToString(e)));
    } finally {
      btn.disabled = false;
      btn.textContent = 'Login';
    }
  }

  btn.onclick = doLogin;

  // Enter para submeter
  [userInput, passInput].forEach(el => {
    el.addEventListener('keydown', (ev) => {
      if (ev.key === 'Enter') doLogin();
    });
  });
}
