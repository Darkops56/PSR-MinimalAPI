import { CartManager } from './cart.js';
import { AuthManager } from './auth.js';

const SIZE_MULTIPLIERS = {
  Personal: 0.70,
  Mediana: 0.85,
  Grande: 1.00,
  Familiar: 1.30
};

const SIZE_PORTIONS = {
  Personal: 4,
  Mediana: 6,
  Grande: 8,
  Familiar: 12
};

export class CustomizerModal {
  constructor() {
    this.modal = document.getElementById('customizer-modal');
    if (!this.modal) return;

    this.pizzaId = 0;
    this.basePrice = 0;
    this.selectedSize = 'Grande';
    this.quantity = 1;

    this.titleEl = document.getElementById('customizer-pizza-name');
    this.descEl = document.getElementById('customizer-pizza-desc');
    this.priceEl = document.getElementById('customizer-total-price');
    this.qtyDisplayEl = document.getElementById('customizer-qty');
    this.portionsTextEl = document.getElementById('customizer-portions-text');

    this.initEvents();
  }

  initEvents() {
    // Cerrar modal
    this.modal.querySelectorAll('[data-close-modal]').forEach(btn => {
      btn.addEventListener('click', () => this.close());
    });

    // Cambiar tamaño
    this.modal.querySelectorAll('.size-option-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        const size = btn.dataset.size;
        this.setSize(size);
      });
    });

    // Control de cantidad
    const btnMinus = document.getElementById('customizer-minus-btn');
    const btnPlus = document.getElementById('customizer-plus-btn');

    btnMinus?.addEventListener('click', () => {
      if (this.quantity > 1) {
        this.quantity--;
        this.updateView();
      }
    });

    btnPlus?.addEventListener('click', () => {
      this.quantity++;
      this.updateView();
    });

    // Botón agregar al carrito
    const btnAdd = document.getElementById('customizer-add-btn');
    btnAdd?.addEventListener('click', async () => {
      if (!AuthManager.isAuthenticated()) {
        this.close();
        window.dispatchEvent(new CustomEvent('auth:required', { detail: { action: 'customizer-add' } }));
        return;
      }

      btnAdd.disabled = true;
      btnAdd.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span> Guardando...';

      try {
        await CartManager.addItem(this.pizzaId, this.selectedSize, this.quantity);
        window.showToast?.(`¡Pizza ${this.titleEl.textContent} agregada al carrito!`, 'success');
        this.close();
      } catch (err) {
        window.showToast?.(err.message || 'Error al guardar en el carrito', 'error');
      } finally {
        btnAdd.disabled = false;
        btnAdd.innerHTML = '<i class="bi bi-cart-plus me-1"></i> <span>Añadir al Carrito</span>';
      }
    });

    // Listeners globales para abrir desde cards
    document.addEventListener('click', (e) => {
      const trigger = e.target.closest('[data-open-customizer]');
      if (!trigger) return;

      const id = parseInt(trigger.dataset.pizzaId, 10);
      const name = trigger.dataset.pizzaName;
      const desc = trigger.dataset.pizzaDesc;
      const price = parseFloat(trigger.dataset.pizzaPrice);

      this.open(id, name, desc, price);
    });
  }

  open(id, name, desc, price) {
    this.pizzaId = id;
    this.basePrice = price;
    this.selectedSize = 'Grande';
    this.quantity = 1;

    if (this.titleEl) this.titleEl.textContent = name;
    if (this.descEl) this.descEl.textContent = desc || '';

    this.setSize('Grande');
    this.modal.classList.add('active');
  }

  close() {
    this.modal.classList.remove('active');
  }

  setSize(size) {
    this.selectedSize = size;
    this.modal.querySelectorAll('.size-option-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.size === size);
    });

    const portions = SIZE_PORTIONS[size] || 8;
    if (this.portionsTextEl) {
      this.portionsTextEl.textContent = `Es una pizza ${size} que tiene ${portions} porciones.`;
    }

    this.updateView();
  }

  updateView() {
    const multiplier = SIZE_MULTIPLIERS[this.selectedSize] || 1.00;
    const unitPrice = Math.round(this.basePrice * multiplier * 100) / 100;
    const totalPrice = unitPrice * this.quantity;

    if (this.priceEl) {
      this.priceEl.textContent = `$${totalPrice.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`;
    }
    if (this.qtyDisplayEl) {
      this.qtyDisplayEl.textContent = this.quantity;
    }
  }
}
