import { initUI } from './modules/ui.js';
import { CartManager } from './modules/cart.js';
import { CustomizerModal } from './modules/customizer.js';
import { OrderTracker } from './modules/tracker.js';

async function initApp() {
  console.log('[BearPizzeria] Inicializando módulos de cliente...');
  
  // 1. Inicializar UI general (drawer, busqueda, modales, toasts)
  initUI();

  // 2. Inicializar modal de personalización
  new CustomizerModal();

  // 3. Inicializar tracker de actualización automática de pedidos
  new OrderTracker();

  // 4. Cargar carrito desde la base de datos y actualizar contador
  await CartManager.loadCart();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initApp);
} else {
  initApp();
}
