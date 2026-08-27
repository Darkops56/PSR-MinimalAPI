import { apiFetch } from './api.js';

export class AuthManager {
  static getClienteId() {
    return parseInt(document.body.dataset.clienteId || '0', 10);
  }

  static getToken() {
    return document.body.dataset.token || '';
  }

  static isAuthenticated() {
    return this.getClienteId() > 0;
  }

  static getCurrentUser() {
    const raw = document.body.dataset.user;
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }

  static async register(data) {
    return await apiFetch('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }

  static async login(username, password) {
    return await apiFetch('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password })
    });
  }
}
