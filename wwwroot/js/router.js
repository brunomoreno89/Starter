// wwwroot/js/router.js
import { isSessionValid, auth } from './api.js';

const routes = [];
export function addRoute(path, view, options={}){
  routes.push({ path, view, options });
}
export function navigate(path){ location.hash = '#'+path; }

function matchRoute(hash){
  const clean = hash.replace(/^#/, '');
  let r = routes.find(x => x.path === clean);
  if (!r){
    // suporta /users/123 etc. se necessário no futuro
    r = routes.find(x => clean.startsWith(x.path));
  }
  return r;
}

export async function startRouter(renderNavbar){
  async function render(){
    const h = location.hash || '#/';
    const route = matchRoute(h);
    const app = document.getElementById('app');
    app.innerHTML = '';
    // guard auth
    if (!route){
      app.innerHTML = `<div class="card"><h2>Not found</h2><p>Route ${h} not found.</p></div>`;
      renderNavbar?.(); return;
    }
    if (route.options.auth && !isSessionValid()){
      navigate('/login'); renderNavbar?.(); return;
    }
    if (route.options.perm && !auth.hasPerm(route.options.perm)){
      app.innerHTML = `<div class="card"><h2>403</h2><p>Permission required: <code>${route.options.perm}</code>.</p></div>`;
      renderNavbar?.(); return;
    }
    await route.view(app);
    renderNavbar?.();
  }
  window.addEventListener('hashchange', render);
  await render();
}
