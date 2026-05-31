import axios from 'axios';

// W Dockerze nginx proxy przekierowuje /api/* do backendu - baseURL = ''
// Lokalnie (npm start) używa REACT_APP_API_URL z .env lub fallback na localhost:5000
const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

const client = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor - automatycznie dodaje token JWT do każdego żądania
// Dzięki temu nie musicie ręcznie dodawać tokenu w każdym miejscu
client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor odpowiedzi - obsługuje wygaśnięcie tokenu
client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token wygasł - wyloguj użytkownika
      localStorage.removeItem('token');
      localStorage.removeItem('rola');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default client;