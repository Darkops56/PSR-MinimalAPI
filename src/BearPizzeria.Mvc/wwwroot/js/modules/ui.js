export function initUI() {
  // Drawer Lateral Móvil
  const drawerBtn = document.getElementById('hamburger-toggle-btn');
  const drawer = document.getElementById('mobile-drawer');
  const drawerOverlay = document.getElementById('drawer-overlay');
  const drawerCloseBtn = document.getElementById('drawer-close-btn');

  function openDrawer() {
    drawer?.classList.add('active');
    drawerOverlay?.classList.add('active');
    document.body.style.overflow = 'hidden';
  }

  function closeDrawer() {
    drawer?.classList.remove('active');
    drawerOverlay?.classList.remove('active');
    document.body.style.overflow = '';
  }

  drawerBtn?.addEventListener('click', openDrawer);
  drawerCloseBtn?.addEventListener('click', closeDrawer);
  drawerOverlay?.addEventListener('click', closeDrawer);

  // Toast Manager
  window.showToast = function(message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast-item ${type === 'success' ? 'success' : ''}`;
    const iconClass = type === 'success' ? 'bi-check-circle-fill text-success' : type === 'error' ? 'bi-exclamation-triangle-fill text-danger' : 'bi-info-circle-fill text-warning';

    toast.innerHTML = `
      <i class="bi ${iconClass} fs-5"></i>
      <span class="small fw-semibold text-light">${message}</span>
    `;

    container.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(100%)';
      setTimeout(() => toast.remove(), 300);
    }, 4000);
  };

  // Auth Guard Modal Listener
  const authModal = document.getElementById('auth-guard-modal');
  if (authModal) {
    const closeModal = () => {
      authModal.classList.remove('active');
    };

    authModal.querySelectorAll('[data-close-modal]').forEach(btn => {
      btn.addEventListener('click', closeModal);
    });

    window.addEventListener('auth:required', () => {
      authModal.classList.add('active');
    });
  }

  // Normalización de texto: minusculas, sin tildes/acentos y sin símbolos especiales
  function normalizeText(text) {
    if (!text) return '';
    return text
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9\s]/g, '')
      .trim();
  }

  // Filtrado de Pizzas por Búsqueda y Categorías
  const searchInput = document.getElementById('global-search-input');
  const productCards = document.querySelectorAll('.product-card');
  const categoryChips = document.querySelectorAll('.category-chip');

  function filterProducts() {
    const rawTerm = searchInput?.value.trim() || '';
    const cleanTerm = normalizeText(rawTerm);
    const activeChip = document.querySelector('.category-chip.active');
    const selectedCategory = activeChip?.dataset.category || 'Todas';

    productCards.forEach(card => {
      // 1. Filtro por Categoría
      const cardCat = card.dataset.category || '';
      const matchesCategory = (selectedCategory === 'Todas') || (cardCat === selectedCategory);

      // 2. Filtro por Texto (Nombre / Descripción)
      let matchesText = true;
      if (rawTerm !== '') {
        if (cleanTerm === '') {
          matchesText = true;
        } else {
          const cleanTitle = normalizeText(card.querySelector('.product-title')?.textContent);
          const cleanDesc = normalizeText(card.querySelector('.product-desc')?.textContent);
          matchesText = cleanTitle.includes(cleanTerm) || cleanDesc.includes(cleanTerm);
        }
      }

      card.style.display = (matchesCategory && matchesText) ? '' : 'none';
    });
  }

  let debounceTimer;
  searchInput?.addEventListener('input', () => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(filterProducts, 300);
  });

  // Categorías Chips
  categoryChips.forEach(chip => {
    chip.addEventListener('click', () => {
      categoryChips.forEach(c => c.classList.remove('active'));
      chip.classList.add('active');
      filterProducts();
    });
  });
}
