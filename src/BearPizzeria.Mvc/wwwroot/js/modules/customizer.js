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
    this.stock = 10;
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
      if (this.quantity < this.stock) {
        this.quantity++;
        this.updateView();
      } else {
        const enCarrito = CartManager.getPizzaQuantityInCart(this.pizzaId);
        if (enCarrito > 0) {
          window.showToast?.(`Ya tenés ${enCarrito} en tu carrito y el stock total es ${this.totalStock}. Máximo disponible adicional: ${this.stock}.`, 'warning');
        } else {
          window.showToast?.(`No podés agregar más de ${this.stock} unidades (stock máximo disponible).`, 'warning');
        }
      }
    });

    // Botón agregar al carrito
    const btnAdd = document.getElementById('customizer-add-btn');
    btnAdd?.addEventListener('click', async () => {
      if (!AuthManager.isAuthenticated()) {
        this.close();
        window.dispatchEvent(new CustomEvent('auth:required', { detail: { action: 'customizer-add' } }));
        return;
      }

      if (this.quantity > this.stock) {
        window.showToast?.(`No hay suficiente stock disponible. Máximo a agregar: ${this.stock}`, 'error');
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
      const stock = parseInt(trigger.dataset.pizzaStock || '10', 10);

      this.open(id, name, desc, price, stock);
    });
  }

  open(id, name, desc, price, totalStock = 10) {
    this.pizzaId = id;
    this.basePrice = price;
    this.totalStock = totalStock;

    // Restar del stock total la cantidad que el cliente ya tiene en su carrito
    const enCarrito = CartManager.getPizzaQuantityInCart(id);
    this.stock = Math.max(0, totalStock - enCarrito);

    this.selectedSize = 'Grande';
    this.quantity = Math.min(1, this.stock);

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

    const btnMinus = document.getElementById('customizer-minus-btn');
    const btnPlus = document.getElementById('customizer-plus-btn');
    const btnAdd = document.getElementById('customizer-add-btn');

    const enCarrito = CartManager.getPizzaQuantityInCart(this.pizzaId);

    if (btnMinus) btnMinus.disabled = this.quantity <= 1;
    if (btnPlus) btnPlus.disabled = this.quantity >= this.stock;
    if (btnAdd) {
      if (this.stock === 0) {
        btnAdd.disabled = true;
        if (enCarrito > 0) {
          btnAdd.innerHTML = `<i class="bi bi-cart-check me-1"></i> <span>Máximo en carrito (${enCarrito}/${this.totalStock})</span>`;
        } else {
          btnAdd.innerHTML = '<i class="bi bi-x-circle me-1"></i> <span>Sin Stock Disponible</span>';
        }
      } else {
        btnAdd.disabled = false;
        btnAdd.innerHTML = '<i class="bi bi-cart-plus me-1"></i> <span>Añadir al Carrito</span>';
      }
    }
  }
}
