import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import client from '../../api/client';
import useIdleTimeout from '../../useIdleTimeout';

interface Pracownik {
  id: number;
  imie: string;
  nazwisko: string;
}

const HrDashboard: React.FC = () => {
  const navigate = useNavigate();
  useIdleTimeout(15);

  const [pracownicy, setPracownicy] = useState<Pracownik[]>([]);
  const [pracownikId, setPracownikId] = useState('');
  const [miesiac, setMiesiac] = useState(new Date().getMonth() + 1);
  const [rok, setRok] = useState(new Date().getFullYear());
  const [brutto, setBrutto] = useState('');
  const [premia, setPremi] = useState('0');
  const [nadgodziny, setNadgodziny] = useState('0');
  const [zfss, setZfss] = useState('0');
  const [typAbsencji, setTypAbsencji] = useState('BRAK');
  const [dniAbsencji, setDniAbsencji] = useState('0');
  const [komunikat, setKomunikat] = useState('');
  const [blad, setBlad] = useState('');
  const [ladowanie, setLadowanie] = useState(false);

  useEffect(() => {
    client.get('/api/hr/pracownicy')
      .then((res: any) => setPracownicy(res.data))
      .catch(() => setBlad('Nie można pobrać listy pracowników.'));
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBlad('');
    setKomunikat('');
    setLadowanie(true);

    try {
      const absencje = typAbsencji !== 'BRAK' && parseInt(dniAbsencji) > 0
        ? [{ typ: typAbsencji, liczbaDni: parseInt(dniAbsencji), wspolczynnikZasilku: 0.80 }]
        : [];

      const response = await client.post('/api/hr/salary', {
        pracownikId: parseInt(pracownikId),
        miesiac,
        rok,
        wynagrodzenieZasadnicze: parseFloat(brutto),
        premia: parseFloat(premia),
        nadgodziny: parseFloat(nadgodziny),
        prowizja: 0,
        swiadczenieZfss: parseFloat(zfss),
        absencje,
      });

      setKomunikat(`✅ ${response.data.message} (ID paska: ${response.data.pensjaId})`);
    } catch (err: any) {
      setBlad(err.response?.data?.message || 'Błąd podczas zapisywania danych.');
    } finally {
      setLadowanie(false);
    }
  };

  const handleLogout = () => {
    localStorage.clear();
    navigate('/login');
  };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h1 style={styles.headerTitle}>Panel HR</h1>
        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
          <button onClick={() => navigate('/hr/tax-profile')} style={styles.logoutBtn}>
             Profile podatkowe
          </button>
          <button onClick={handleLogout} style={styles.logoutBtn}>Wyloguj</button>
        </div>
      </div>

      <div style={styles.card}>
        <h2 style={styles.cardTitle}>Wprowadzenie wynagrodzenia miesięcznego</h2>

        <form onSubmit={handleSubmit}>
          <div style={styles.field}>
            <label style={styles.label}>Pracownik</label>
            <select
              style={styles.input}
              value={pracownikId}
              onChange={e => setPracownikId(e.target.value)}
              required
            >
              <option value="">-- Wybierz pracownika --</option>
              {pracownicy.map(p => (
                <option key={p.id} value={p.id}>
                  {p.id} - {p.imie} {p.nazwisko}
                </option>
              ))}
            </select>
          </div>

          <div style={styles.row}>
            <div style={{ ...styles.field, flex: 1 }}>
              <label style={styles.label}>Miesiąc</label>
              <select style={styles.input} value={miesiac} onChange={e => setMiesiac(parseInt(e.target.value))}>
                {Array.from({ length: 12 }, (_, i) => (
                  <option key={i + 1} value={i + 1}>{i + 1}</option>
                ))}
              </select>
            </div>
            <div style={{ ...styles.field, flex: 1, marginLeft: '1rem' }}>
              <label style={styles.label}>Rok</label>
              <input style={styles.input} type="number" value={rok}
                onChange={e => setRok(parseInt(e.target.value))} />
            </div>
          </div>

          <div style={styles.field}>
            <label style={styles.label}>Wynagrodzenie zasadnicze (PLN)</label>
            <input style={styles.input} type="number" step="0.01"
              value={brutto} onChange={e => setBrutto(e.target.value)}
              placeholder="np. 8500" required />
          </div>

          <h3 style={styles.sectionTitle}>Dodatki</h3>
          <div style={styles.row}>
            <div style={{ ...styles.field, flex: 1 }}>
              <label style={styles.label}>Premia (PLN)</label>
              <input style={styles.input} type="number" step="0.01"
                value={premia} onChange={e => setPremi(e.target.value)} />
            </div>
            <div style={{ ...styles.field, flex: 1, marginLeft: '1rem' }}>
              <label style={styles.label}>Nadgodziny (PLN)</label>
              <input style={styles.input} type="number" step="0.01"
                value={nadgodziny} onChange={e => setNadgodziny(e.target.value)} />
            </div>
          </div>

          <div style={styles.field}>
            <label style={styles.label}>ZFŚS - Wczasy pod gruszą (PLN)</label>
            <input style={styles.input} type="number" step="0.01"
              value={zfss} onChange={e => setZfss(e.target.value)} />
          </div>

          <h3 style={styles.sectionTitle}>Absencje</h3>
          <div style={styles.row}>
            <div style={{ ...styles.field, flex: 2 }}>
              <label style={styles.label}>Typ absencji</label>
              <select style={styles.input} value={typAbsencji}
                onChange={e => setTypAbsencji(e.target.value)}>
                <option value="BRAK">Brak absencji</option>
                <option value="CHOROBA_L4">Choroba (L4)</option>
                <option value="OPIEKA">Opieka nad chorym</option>
                <option value="MACIERZYNSKI">Urlop macierzyński</option>
                <option value="OJCOWSKI">Urlop ojcowski</option>
                <option value="RODZICIELSKI">Urlop rodzicielski</option>
              </select>
            </div>
            <div style={{ ...styles.field, flex: 1, marginLeft: '1rem' }}>
              <label style={styles.label}>Liczba dni</label>
              <input style={styles.input} type="number"
                value={dniAbsencji} onChange={e => setDniAbsencji(e.target.value)}
                disabled={typAbsencji === 'BRAK'} />
            </div>
          </div>

          {blad && <p style={styles.blad}>{blad}</p>}
          {komunikat && <p style={styles.sukces}>{komunikat}</p>}

          <button type="submit" style={ladowanie ? styles.buttonDisabled : styles.button}
            disabled={ladowanie}>
            {ladowanie ? 'Zapisywanie...' : 'Zapisz i przelicz'}
          </button>
        </form>
      </div>
    </div>
  );
};

const styles: Record<string, React.CSSProperties> = {
  container: { minHeight: '100vh', backgroundColor: '#f3f4f6' },
  header: {
    backgroundColor: '#2563eb', color: 'white', padding: '1rem 2rem',
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
  },
  headerTitle: { margin: 0, fontSize: '1.25rem' },
  logoutBtn: {
    backgroundColor: 'transparent', border: '1px solid white',
    color: 'white', padding: '0.5rem 1rem', borderRadius: '4px', cursor: 'pointer',
  },
  card: {
    backgroundColor: 'white', margin: '2rem auto', padding: '2rem',
    borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
    maxWidth: '600px',
  },
  cardTitle: { marginTop: 0, color: '#1f2937', marginBottom: '1.5rem' },
  sectionTitle: { color: '#374151', marginTop: '1.5rem', marginBottom: '0.5rem' },
  field: { marginBottom: '1rem' },
  row: { display: 'flex' },
  label: { display: 'block', marginBottom: '0.25rem', fontWeight: '500', color: '#374151' },
  input: {
    width: '100%', padding: '0.5rem 0.75rem', border: '1px solid #d1d5db',
    borderRadius: '4px', fontSize: '1rem', boxSizing: 'border-box',
  },
  button: {
    width: '100%', padding: '0.75rem', backgroundColor: '#2563eb',
    color: 'white', border: 'none', borderRadius: '4px', fontSize: '1rem',
    cursor: 'pointer', marginTop: '1rem',
  },
  buttonDisabled: {
    width: '100%', padding: '0.75rem', backgroundColor: '#93c5fd',
    color: 'white', border: 'none', borderRadius: '4px', fontSize: '1rem',
    cursor: 'not-allowed', marginTop: '1rem',
  },
  blad: { color: '#dc2626', fontSize: '0.875rem' },
  sukces: { color: '#16a34a', fontSize: '0.875rem' },
};

export default HrDashboard;