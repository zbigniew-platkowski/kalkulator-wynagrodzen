import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import client from '../../api/client';

interface Uzytkownik {
  id: number;
  login: string;
  rola: string;
  czyAktywny: boolean;
  dataOstatniegoLogowania: string | null;
}

const UserManagement: React.FC = () => {
  const navigate = useNavigate();
  const [uzytkownicy, setUzytkownicy] = useState<Uzytkownik[]>([]);
  const [blad, setBlad] = useState('');
  const [komunikat, setKomunikat] = useState('');

  const [nowyLogin, setNowyLogin] = useState('');
  const [noweHaslo, setNoweHaslo] = useState('');
  const [nowaRola, setNowaRola] = useState('PRACOWNIK');
  const [noweImie, setNoweImie] = useState('');
  const [noweNazwisko, setNoweNazwisko] = useState('');
  const [nowaPlec, setNowaPlec] = useState('M');
  const [pokazFormularz, setPokazFormularz] = useState(false);

  const pobierzUzytkownikow = async () => {
    try {
      const res = await client.get('/api/admin/users');
      setUzytkownicy(res.data.uzytkownicy);
    } catch {
      setBlad('Nie można pobrać listy użytkowników.');
    }
  };

  useEffect(() => { pobierzUzytkownikow(); }, []);

  const toggleKonto = async (id: number) => {
    try {
      await client.patch(`/api/admin/users/${id}/toggle`);
      setKomunikat('Status konta został zmieniony.');
      pobierzUzytkownikow();
    } catch {
      setBlad('Błąd podczas zmiany statusu konta.');
    }
  };

  const usunKonto = async (id: number, login: string) => {
    if (!window.confirm(`Czy na pewno usunąć konto "${login}"?`)) return;
    try {
      await client.delete(`/api/admin/users/${id}`);
      setKomunikat('Konto zostało usunięte.');
      pobierzUzytkownikow();
    } catch {
      setBlad('Błąd podczas usuwania konta.');
    }
  };

  const dodajUzytkownika = async (e: React.FormEvent) => {
    e.preventDefault();
    setBlad('');
    setKomunikat('');
    try {
      await client.post('/api/admin/users', {
        login: nowyLogin,
        haslo: noweHaslo,
        rola: nowaRola,
        imie: noweImie,
        nazwisko: noweNazwisko,
        plec: nowaPlec,
      });
      setKomunikat('Konto zostało utworzone. Pracownik uzupełni staż i kapitał ZUS samodzielnie.');
      setPokazFormularz(false);
      setNowyLogin(''); setNoweHaslo(''); setNoweImie(''); setNoweNazwisko('');
      pobierzUzytkownikow();
    } catch (err: any) {
      setBlad(err.response?.data?.message || 'Błąd podczas tworzenia konta.');
    }
  };

  const handleLogout = () => { localStorage.clear(); navigate('/login'); };

  const formatData = (data: string | null) => {
    if (!data) return 'Nigdy';
    return new Date(data).toLocaleString('pl-PL');
  };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h1 style={styles.headerTitle}>Panel Administratora IT</h1>
        <button onClick={handleLogout} style={styles.logoutBtn}>Wyloguj</button>
      </div>

      <div style={styles.content}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <h2 style={{ margin: 0 }}>Zarządzanie użytkownikami</h2>
          <button onClick={() => setPokazFormularz(!pokazFormularz)} style={styles.addBtn}>
            + Nowe konto
          </button>
        </div>

        {pokazFormularz && (
          <div style={styles.formularz}>
            <h3 style={{ marginTop: 0 }}>Nowe konto</h3>
            <p style={styles.info}>
               Staż pracy i kapitał ZUS pracownik uzupełni samodzielnie w symulatorze emerytalnym.
            </p>
            <form onSubmit={dodajUzytkownika}>
              <div style={styles.row}>
                <div style={styles.field}>
                  <label style={styles.label}>Login</label>
                  <input style={styles.input} value={nowyLogin}
                    onChange={e => setNowyLogin(e.target.value)} required />
                </div>
                <div style={styles.field}>
                  <label style={styles.label}>Hasło</label>
                  <input style={styles.input} type="password" value={noweHaslo}
                    onChange={e => setNoweHaslo(e.target.value)} required />
                </div>
                <div style={styles.field}>
                  <label style={styles.label}>Rola</label>
                  <select style={styles.input} value={nowaRola}
                    onChange={e => setNowaRola(e.target.value)}>
                    <option value="PRACOWNIK">Pracownik</option>
                    <option value="HR">HR</option>
                  </select>
                </div>
              </div>

              {nowaRola === 'PRACOWNIK' && (
                <div style={styles.row}>
                  <div style={styles.field}>
                    <label style={styles.label}>Imię</label>
                    <input style={styles.input} value={noweImie}
                      onChange={e => setNoweImie(e.target.value)} required />
                  </div>
                  <div style={styles.field}>
                    <label style={styles.label}>Nazwisko</label>
                    <input style={styles.input} value={noweNazwisko}
                      onChange={e => setNoweNazwisko(e.target.value)} required />
                  </div>
                  <div style={styles.field}>
                    <label style={styles.label}>Płeć</label>
                    <select style={styles.input} value={nowaPlec}
                      onChange={e => setNowaPlec(e.target.value)}>
                      <option value="M">Mężczyzna</option>
                      <option value="K">Kobieta</option>
                    </select>
                  </div>
                </div>
              )}

              <button type="submit" style={styles.saveBtn}>Utwórz konto</button>
            </form>
          </div>
        )}

        {blad && <p style={styles.blad}>{blad}</p>}
        {komunikat && <p style={styles.sukces}>{komunikat}</p>}

        <table style={styles.tabela}>
          <thead>
            <tr style={styles.thead}>
              <th style={styles.th}>ID</th>
              <th style={styles.th}>Login</th>
              <th style={styles.th}>Rola</th>
              <th style={styles.th}>Status</th>
              <th style={styles.th}>Ostatnie logowanie</th>
              <th style={styles.th}>Akcje</th>
            </tr>
          </thead>
          <tbody>
            {uzytkownicy.map(u => (
              <tr key={u.id} style={styles.tr}>
                <td style={styles.td}>{u.id}</td>
                <td style={styles.td}>{u.login}</td>
                <td style={styles.td}>
                  <span style={{ ...styles.badge, ...getBadgeStyle(u.rola) }}>{u.rola}</span>
                </td>
                <td style={styles.td}>
                  <span style={{ ...styles.badge, ...(u.czyAktywny ? styles.aktywny : styles.zablokowany) }}>
                    {u.czyAktywny ? 'Aktywny' : 'Zablokowany'}
                  </span>
                </td>
                <td style={styles.td}>{formatData(u.dataOstatniegoLogowania)}</td>
                <td style={styles.td}>
                  <button onClick={() => toggleKonto(u.id)} style={styles.actionBtn}>
                    {u.czyAktywny ? 'Zablokuj' : 'Odblokuj'}
                  </button>
                  <button onClick={() => usunKonto(u.id, u.login)}
                    style={{ ...styles.actionBtn, ...styles.deleteBtn }}>
                    Usuń
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const getBadgeStyle = (rola: string): React.CSSProperties => {
  if (rola === 'HR') return { backgroundColor: '#dbeafe', color: '#1d4ed8' };
  if (rola === 'ADMIN_IT') return { backgroundColor: '#fce7f3', color: '#9d174d' };
  return { backgroundColor: '#d1fae5', color: '#065f46' };
};

const styles: Record<string, React.CSSProperties> = {
  container: { minHeight: '100vh', backgroundColor: '#f3f4f6' },
  header: {
    backgroundColor: '#7c3aed', color: 'white', padding: '1rem 2rem',
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
  },
  headerTitle: { margin: 0, fontSize: '1.25rem' },
  logoutBtn: {
    backgroundColor: 'transparent', border: '1px solid white',
    color: 'white', padding: '0.5rem 1rem', borderRadius: '4px', cursor: 'pointer',
  },
  content: { padding: '2rem', maxWidth: '1200px', margin: '0 auto' },
  addBtn: {
    backgroundColor: '#7c3aed', color: 'white', border: 'none',
    padding: '0.5rem 1rem', borderRadius: '4px', cursor: 'pointer',
  },
  formularz: {
    backgroundColor: 'white', padding: '1.5rem', borderRadius: '8px',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)', marginBottom: '1.5rem',
  },
  info: { color: '#6b7280', fontSize: '0.875rem', marginBottom: '1rem' },
  row: { display: 'flex', gap: '1rem' },
  field: { flex: 1, marginBottom: '0.75rem' },
  label: { display: 'block', marginBottom: '0.25rem', fontWeight: '500' },
  input: {
    width: '100%', padding: '0.5rem', border: '1px solid #d1d5db',
    borderRadius: '4px', boxSizing: 'border-box',
  },
  saveBtn: {
    backgroundColor: '#7c3aed', color: 'white', border: 'none',
    padding: '0.5rem 1.5rem', borderRadius: '4px', cursor: 'pointer',
  },
  tabela: {
    width: '100%', borderCollapse: 'collapse', backgroundColor: 'white',
    borderRadius: '8px', overflow: 'hidden', boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
  },
  thead: { backgroundColor: '#f9fafb' },
  th: {
    padding: '0.75rem 1rem', textAlign: 'left', fontWeight: '600',
    color: '#374151', borderBottom: '1px solid #e5e7eb',
  },
  tr: { borderBottom: '1px solid #f3f4f6' },
  td: { padding: '0.75rem 1rem', color: '#374151' },
  badge: { padding: '0.25rem 0.75rem', borderRadius: '9999px', fontSize: '0.75rem', fontWeight: '600' },
  aktywny: { backgroundColor: '#d1fae5', color: '#065f46' },
  zablokowany: { backgroundColor: '#fee2e2', color: '#991b1b' },
  actionBtn: {
    padding: '0.25rem 0.75rem', border: '1px solid #d1d5db',
    borderRadius: '4px', cursor: 'pointer', marginRight: '0.5rem', backgroundColor: 'white',
  },
  deleteBtn: { borderColor: '#fca5a5', color: '#dc2626' },
  blad: { color: '#dc2626' },
  sukces: { color: '#16a34a' },
};

export default UserManagement;