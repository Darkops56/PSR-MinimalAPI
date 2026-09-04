import { apiFetch } from './api.js';
import { AuthManager } from './auth.js';

export class CartManager {
  static currentCart = null;

  static async loadCart() {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) {
      this.currentCart = null;
      this.updateBadges(0);
      return null;
    }

    try {
      const cart = await apiFetch(`/api/carrito/${clienteId}`);
      if (cart) {
        this.currentCart = cart;
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
      this.currentCart = updatedCart;
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
      this.currentCart = updatedCart;
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
      this.currentCart = updatedCart;
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
      this.currentCart = clearedCart;
      this.updateBadges(0);
      window.dispatchEvent(new CustomEvent('cart:updated', { detail: clearedCart }));
    }

    return clearedCart;
  }

  static async checkout(direccionId = null, direccionEntrega = null) {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) {
      window.dispatchEvent(new CustomEvent('auth:required', { detail: { action: 'checkout' } }));
      return null;
    }

    const payload = {};
    if (direccionId) payload.direccionId = parseInt(direccionId, 10);
    if (direccionEntrega) payload.direccionEntrega = direccionEntrega;

    const pedido = await apiFetch(`/api/carrito/${clienteId}/checkout`, {
      method: 'POST',
      body: JSON.stringify(payload)
    });

    if (pedido) {
      this.currentCart = null;
      this.updateBadges(0);
      window.dispatchEvent(new CustomEvent('cart:cleared'));
      window.dispatchEvent(new CustomEvent('orders:refresh'));
    }

    return pedido;
  }

  static async repeatOrder(pedidoId) {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) {
      window.dispatchEvent(new CustomEvent('auth:required', { detail: { action: 'repeat-order' } }));
      return null;
    }

    const result = await apiFetch(`/api/pedidos/${pedidoId}/repetir?clienteId=${clienteId}`, {
      method: 'POST'
    });

    if (result && result.carritoActualizado) {
      this.currentCart = result.carritoActualizado;
      this.updateBadges(result.carritoActualizado.cantidadTotalItems || 0);
      window.dispatchEvent(new CustomEvent('cart:updated', { detail: result.carritoActualizado }));
    }

    return result;
  }

  static getPizzaQuantityInCart(pizzaId) {
    if (!this.currentCart || !Array.isArray(this.currentCart.items)) return 0;
    const targetId = parseInt(pizzaId, 10);
    return this.currentCart.items
      .filter(item => (item.pizzaId ?? item.PizzaId) === targetId)
      .reduce((sum, item) => sum + (item.cantidad ?? item.Cantidad ?? 0), 0);
  }

  static updateBadges(count) {
    document.querySelectorAll('.cart-badge-count').forEach(el => {
      el.textContent = count;
      el.style.display = count > 0 ? 'inline-flex' : 'none';
    });
  }
}
