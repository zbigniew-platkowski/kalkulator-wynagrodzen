import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import client from '../api/client';

const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const [login, setLogin] = useState('');
  const [haslo, setHaslo] = useState('');
  const [blad, setBlad] = useState('');
  const [ladowanie, setLadowanie] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setBlad('');
    setLadowanie(true);

    try {
      const response = await client.post('/api/auth/login', { login, haslo });
      const { token, rola } = response.data;

      // Zapisz token i rolę w localStorage
      localStorage.setItem('token', token);
      localStorage.setItem('rola', rola);

      // Przekieruj na odpowiedni panel w zależności od roli
      if (rola === 'HR') navigate('/hr');
      else if (rola === 'PRACOWNIK') navigate('/employee');
      else if (rola === 'ADMIN_IT') navigate('/admin');

    } catch (err: any) {
      setBlad(err.response?.data?.message || 'Błąd logowania. Spróbuj ponownie.');
    } finally {
      setLadowanie(false);
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>Kalkulator Wynagrodzeń</h1>
        <p style={styles.subtitle}>Zaloguj się do swojego konta</p>

        <form onSubmit={handleLogin}>
          <div style={styles.field}>
            <label style={styles.label}>Login</label>
            <input
              style={styles.input}
              type="text"
              value={login}
              onChange={(e) => setLogin(e.target.value)}
              placeholder="Wpisz login"
              required
            />
          </div>

          <div style={styles.field}>
            <label style={styles.label}>Hasło</label>
            <input
              style={styles.input}
              type="password"
              value={haslo}
              onChange={(e) => setHaslo(e.target.value)}
              placeholder="Wpisz hasło"
              required
            />
          </div>

          {blad && <p style={styles.blad}>{blad}</p>}

          <button
            style={ladowanie ? styles.buttonDisabled : styles.button}
            type="submit"
            disabled={ladowanie}
          >
            {ladowanie ? 'Logowanie...' : 'Zaloguj się'}
          </button>
        </form>
      </div>
    </div>
  );
};

const styles: Record<string, React.CSSProperties> = {
  container: {
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#f3f4f6',
  },
  card: {
    backgroundColor: 'white',
    padding: '2rem',
    borderRadius: '8px',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
    width: '100%',
    maxWidth: '400px',
  },
  title: {
    fontSize: '1.5rem',
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: '0.5rem',
    color: '#1f2937',
  },
  subtitle: {
    textAlign: 'center',
    color: '#6b7280',
    marginBottom: '1.5rem',
  },
  field: {
    marginBottom: '1rem',
  },
  label: {
    display: 'block',
    marginBottom: '0.25rem',
    fontWeight: '500',
    color: '#374151',
  },
  input: {
    width: '100%',
    padding: '0.5rem 0.75rem',
    border: '1px solid #d1d5db',
    borderRadius: '4px',
    fontSize: '1rem',
    boxSizing: 'border-box',
  },
  button: {
    width: '100%',
    padding: '0.75rem',
    backgroundColor: '#2563eb',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    fontSize: '1rem',
    cursor: 'pointer',
    marginTop: '0.5rem',
  },
  buttonDisabled: {
    width: '100%',
    padding: '0.75rem',
    backgroundColor: '#93c5fd',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    fontSize: '1rem',
    cursor: 'not-allowed',
    marginTop: '0.5rem',
  },
  blad: {
    color: '#dc2626',
    fontSize: '0.875rem',
    marginBottom: '0.5rem',
  },
};

export default LoginPage;