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

function rerender(){
  renderNavbar();
  renderUserArea();
}
startRouter(rerender);
rerender();
