// wwwroot/js/main.js
import { startRouter, addRoute, navigate } from './router.js';
import { isSessionValid } from './api.js';
import { renderNavbar, renderUserArea } from './components/navbar.js';
import { HomeView } from './views/home.js';
import { LoginView } from './views/login.js';
import { ItemsView } from './views/items.js';
import { AccessView } from './views/access.js';
import { UsersView } from './views/users.js';
import { PermissionsView } from './views/permissions.js';
import { RolesView } from './views/roles.js';
import { LogsView } from './views/logs.js';
import { RolePermissionsView } from './views/role-permissions.js';
import { UserRolesView } from './views/user-roles.js';
import { SettingsView } from './views/settings.js';
import { StaticDataView } from './views/static-data.js';
import { HolidaysView } from './views/holidays.js';
//import { RegionsView } from './views/regions.js';
//import { BranchesView } from './views/branches.js';

function bootTheme(){
  const t = localStorage.getItem('theme') || 'dark';
  document.body.classList.toggle('light', t==='light');
}
bootTheme();

addRoute('/', async c => { if (isSessionValid()) navigate('/home'); else navigate('/login'); });
addRoute('/home', HomeView, { auth:true, perm:'Settings.Access' }); // ou remova perm se quiser sempre visível após login
addRoute('/login', LoginView);
addRoute('/items', ItemsView, { auth:true, perm:'Items.Read' });
addRoute('/access', AccessView, { auth:true, perm:'Users.Read' });
addRoute('/users', UsersView, { auth:true, perm:'Users.Read' });
addRoute('/permissions', PermissionsView, { auth:true, perm:'Permissions.Read' });
addRoute('/roles', RolesView, { auth:true, perm:'Roles.Read' });
addRoute('/logs', LogsView, { auth:true, perm:'Logs.Read' });
addRoute('/role-permissions', RolePermissionsView, { auth:true, perm:'RolePermissions.Assign' });
addRoute('/user-roles', UserRolesView, { auth:true, perm:'UserRoles.Assign' });
addRoute('/settings', SettingsView, { auth:true, perm:'Settings.Access' });
addRoute('/static-data', StaticDataView, { auth:true, perm:'StaticData.Read' });
addRoute('/holidays', HolidaysView, { auth:true, perm:'Holidays.Read' });
//addRoute('/regions', RegionsView, { auth:true, perm:'Regions.Read' });
//addRoute('/branches', BranchesView, { auth:true, perm:'Branches.Read' });

function rerender(){
  renderNavbar();   // já chama renderUserArea por dentro

  const navbarEl = document.querySelector('.navbar');
  if (!navbarEl) return;

  const hash = (location.hash || '').replace('#/','');
  const isLogin = hash === 'login' || hash === '' || hash === 'home';
  const hideNav = !isSessionValid() || isLogin;

  // esconde navbar apenas no login
  navbarEl.style.display = hideNav ? 'none' : 'block';

  // ajusta padding do body quando não houver navbar
  document.body.classList.toggle('no-navbar', hideNav);
}

startRouter(rerender);
rerender();
