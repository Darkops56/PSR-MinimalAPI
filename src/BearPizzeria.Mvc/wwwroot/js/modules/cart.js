import { apiFetch } from './api.js';
import { AuthManager } from './auth.js';

export class CartManager {
  static async loadCart() {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) {
      this.updateBadges(0);
      return null;
    }

    try {
      const cart = await apiFetch(`/api/carrito/${clienteId}`);
      if (cart) {
        this.updateBadges(cart.cantidadTotalItems || 0);
      }
      return cart;
    } catch (e) {
      console.warn('No se pudo cargar el carrito desde la base de datos', e);
      return null;
    }
  }

  static async addItem(pizzaId, tamano, cantidad) {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) {
      window.dispatchEvent(new CustomEvent('auth:required', { detail: { action: 'add-to-cart' } }));
      return null;
    }

    const updatedCart = await apiFetch(`/api/carrito/${clienteId}/items`, {
      method: 'POST',
      body: JSON.stringify({ pizzaId, tamano, cantidad })
    });

    if (updatedCart) {
      this.updateBadges(updatedCart.cantidadTotalItems || 0);
      window.dispatchEvent(new CustomEvent('cart:updated', { detail: updatedCart }));
    }

    return updatedCart;
  }

  static async updateItemQty(itemId, cantidad) {
    const updatedCart = await apiFetch(`/api/carrito/items/${itemId}`, {
      method: 'PUT',
      body: JSON.stringify({ cantidad })
    });

    if (updatedCart) {
      this.updateBadges(updatedCart.cantidadTotalItems || 0);
      window.dispatchEvent(new CustomEvent('cart:updated', { detail: updatedCart }));
    }

    return updatedCart;
  }

  static async removeItem(itemId) {
    const updatedCart = await apiFetch(`/api/carrito/items/${itemId}`, {
      method: 'DELETE'
    });

    if (updatedCart) {
      this.updateBadges(updatedCart.cantidadTotalItems || 0);
      window.dispatchEvent(new CustomEvent('cart:updated', { detail: updatedCart }));
    }

    return updatedCart;
  }

  static async clearCart() {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) return;

    const clearedCart = await apiFetch(`/api/carrito/${clienteId}/vaciar`, {
      method: 'DELETE'
    });

    if (clearedCart) {
      this.updateBadges(0);
      window.dispatchEvent(new CustomEvent('cart:updated', { detail: clearedCart }));
    }

    return clearedCart;
  }

  static async checkout() {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) {
      window.dispatchEvent(new CustomEvent('auth:required', { detail: { action: 'checkout' } }));
      return null;
    }

    const pedido = await apiFetch(`/api/carrito/${clienteId}/checkout`, {
      method: 'POST'
    });

    if (pedido) {
      this.updateBadges(0);
      window.dispatchEvent(new CustomEvent('cart:cleared'));
    }

    return pedido;
  }

  static updateBadges(count) {
    document.querySelectorAll('.cart-badge-count').forEach(el => {
      el.textContent = count;
      el.style.display = count > 0 ? 'inline-flex' : 'none';
    });
  }
}
