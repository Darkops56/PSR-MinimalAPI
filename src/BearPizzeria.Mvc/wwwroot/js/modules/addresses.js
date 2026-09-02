import { apiFetch } from './api.js';
import { AuthManager } from './auth.js';

export class AddressManager {
  static cachedAddresses = [];
  static activeAddressId = null;

  static async getAddresses() {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) return [];

    try {
      const addresses = await apiFetch(`/api/clientes/${clienteId}/direcciones`);
      this.cachedAddresses = addresses || [];
      const principal = this.cachedAddresses.find(a => a.esPrincipal) || this.cachedAddresses[0];
      if (principal && !this.activeAddressId) {
        this.activeAddressId = principal.id;
      }
      return this.cachedAddresses;
    } catch (err) {
      console.warn('Error al cargar direcciones:', err);
      return [];
    }
  }

  static async addAddress(nombre, direccionCompleta, notas = '', esPrincipal = false) {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) return null;

    const newDir = await apiFetch(`/api/clientes/${clienteId}/direcciones`, {
      method: 'POST',
      body: JSON.stringify({ nombre, direccionCompleta, notas, esPrincipal })
    });

    if (newDir) {
      await this.getAddresses();
      if (esPrincipal || this.cachedAddresses.length === 1) {
        this.activeAddressId = newDir.id;
      }
    }
    return newDir;
  }

  static async deleteAddress(id) {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) return;

    await apiFetch(`/api/clientes/${clienteId}/direcciones/${id}`, {
      method: 'DELETE'
    });

    if (this.activeAddressId === id) {
      this.activeAddressId = null;
    }
    await this.getAddresses();
  }

  static async setPrincipal(id) {
    const clienteId = AuthManager.getClienteId();
    if (!clienteId) return;

    const updated = await apiFetch(`/api/clientes/${clienteId}/direcciones/${id}/principal`, {
      method: 'PUT'
    });

    if (updated) {
      this.activeAddressId = updated.id;
      await this.getAddresses();
    }
    return updated;
  }

  static setActiveAddress(id) {
    this.activeAddressId = parseInt(id, 10);
  }

  static getActiveAddress() {
    if (!this.cachedAddresses || this.cachedAddresses.length === 0) return null;
    if (this.activeAddressId) {
      const found = this.cachedAddresses.find(a => a.id === this.activeAddressId);
      if (found) return found;
    }
    return this.cachedAddresses.find(a => a.esPrincipal) || this.cachedAddresses[0] || null;
  }
}
