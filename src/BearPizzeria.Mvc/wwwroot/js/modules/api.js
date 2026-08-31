// Cliente API centralizado hacia http://localhost:5250
const API_BASE_URL = 'http://localhost:5250';

export async function apiFetch(endpoint, options = {}) {
  const token = document.body.dataset.token || '';

  const defaultHeaders = {
    'Content-Type': 'application/json',
    'X-Requested-With': 'XMLHttpRequest',
    'Cache-Control': 'no-cache, no-store, must-revalidate',
    'Pragma': 'no-cache'
  };

  if (token) {
    defaultHeaders['Authorization'] = `Bearer ${token}`;
  }

  const config = {
    cache: 'no-store',
    ...options,
    headers: {
      ...defaultHeaders,
      ...options.headers
    }
  };

  try {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, config);
    
    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw {
        status: response.status,
        message: errorData?.mensaje || 'Error en la petición al servidor',
        errores: errorData?.errores || []
      };
    }

    if (response.status === 204) return null;
    return await response.json();
  } catch (error) {
    console.error(`API Error [${endpoint}]:`, error);
    throw error;
  }
}
