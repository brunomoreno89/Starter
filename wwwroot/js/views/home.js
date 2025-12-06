import { navigate } from "../router.js";

export async function HomeView(container) {

  const MODULES = [
    { label: "Items",       icon: "📦", route: "/items" },
    { label: "Access",      icon: "🔐", route: "/access" },
    { label: "Static Data", icon: "📘", route: "/static-data" },
    { label: "Logs",        icon: "📝", route: "/logs" }
  ];

  container.innerHTML = `


  <div class="view-container">  
    <div class="view-header">
        <h2 class="view-title">Modules</h2>
      </div>

      <div class="module-grid">
        ${MODULES.map(m => `
          <div class="module-card" data-route="${m.route}">
            <div class="module-icon">${m.icon}</div>
            <div class="module-title">${m.label}</div>
          </div>
        `).join("")}
      </div>
    </div>
  `;

  // Evento de clique nos cards
  container.querySelectorAll(".module-card").forEach(card => {
    card.addEventListener("click", () => {
      const r = card.dataset.route;
      navigate(r);
    });
  });
}
