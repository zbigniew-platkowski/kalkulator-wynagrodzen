import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import client from '../../api/client';

interface Pracownik {
  id: number;
  imie: string;
  nazwisko: string;
}

interface ProfilPodatkowy {
  pracownikId: number;
  imie: string;
  nazwisko: string;
  statusPitZero: string;
  kupStandardKwota: number;
  pit2Kwota: number;
  wspolczynnikAutorskiKup: number;
  ppkStawkaPracownika: number;
  ppkStawkaPracodawcy: number;
}

const TaxProfileEditor: React.FC = () => {
  const navigate = useNavigate();
  const [pracownicy, setPracownicy] = useState<Pracownik[]>([]);
  const [wybranyId, setWybranyId] = useState('');
  const [profil, setProfil] = useState<ProfilPodatkowy | null>(null);
  const [blad, setBlad] = useState('');
  const [komunikat, setKomunikat] = useState('');
  const [ladowanie, setLadowanie] = useState(false);

  useEffect(() => {
    client.get('/api/hr/pracownicy')
      .then((res: any) => setPracownicy(res.data))
      .catch(() => setBlad('Nie można pobrać listy pracowników.'));
  }, []);

  const pobierzProfil = async (id: string) => {
    if (!id) return;
    setBlad('');
    setKomunikat('');
    try {
      const res = await client.get(`/api/hr/tax-profile/${id}`);
      setProfil(res.data);
    } catch {
      setBlad('Nie można pobrać profilu podatkowego.');
    }
  };

  const zapiszProfil = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!profil) return;
    setLadowanie(true);
    setBlad('');
    setKomunikat('');
    try {
      await client.put(`/api/hr/tax-profile/${profil.pracownikId}`, {
        statusPitZero: profil.statusPitZero,
        kupStandardKwota: profil.kupStandardKwota,
        pit2Kwota: profil.pit2Kwota,
        wspolczynnikAutorskiKup: profil.wspolczynnikAutorskiKup,
        ppkStawkaPracownika: profil.ppkStawkaPracownika,
        ppkStawkaPracodawcy: profil.ppkStawkaPracodawcy,
      });
      setKomunikat('✅ Profil podatkowy został zaktualizowany.');
    } catch (err: any) {
      setBlad(err.response?.data?.message || 'Błąd podczas zapisywania.');
    } finally {
      setLadowanie(false);
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h1 style={styles.headerTitle}>Profile podatkowe pracowników</h1>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button onClick={() => navigate('/hr')} style={styles.backBtn}>
            ← Wynagrodzenia
          </button>
          <button onClick={() => { localStorage.clear(); navigate('/login'); }} style={styles.logoutBtn}>
            Wyloguj
          </button>
        </div>
      </div>

      <div style={styles.content}>
        {/* Wybór pracownika */}
        <div style={styles.card}>
          <h2 style={styles.cardTitle}>Wybierz pracownika</h2>
          <select
            style={styles.input}
            value={wybranyId}
            onChange={e => {
              setWybranyId(e.target.value);
              pobierzProfil(e.target.value);
            }}
          >
            <option value="">-- Wybierz pracownika --</option>
            {pracownicy.map(p => (
              <option key={p.id} value={p.id}>
                {p.id} - {p.imie} {p.nazwisko}
              </option>
            ))}
          </select>
        </div>

        {/* Formularz profilu */}
        {profil && (
          <div style={styles.card}>
            <h2 style={styles.cardTitle}>
              Profil podatkowy — {profil.imie} {profil.nazwisko}
            </h2>

            <form onSubmit={zapiszProfil}>

              {/* PIT-0 */}
              <div style={styles.sekcja}>
                <h3 style={styles.sekcjaTytul}>Ulga PIT-0 (zwolnienie z podatku do 85 528 PLN)</h3>
                <div style={styles.field}>
                  <label style={styles.label}>Status PIT-0</label>
                  <select style={styles.input} value={profil.statusPitZero}
                    onChange={e => setProfil({ ...profil, statusPitZero: e.target.value })}>
                    <option value="BRAK">Brak ulgi</option>
                    <option value="MLODY_DO_26">Ulga dla młodych (do 26 lat)</option>
                    <option value="PRACUJACY_EMERYT">Pracujący emeryt</option>
                    <option value="RODZINA_4_PLUS">Rodzina 4+ (min. 4 dzieci)</option>
                    <option value="POWROT_Z_ZAGRANICY">Powrót z zagranicy (4 lata)</option>
                  </select>
                </div>
              </div>

              {/* KUP */}
              <div style={styles.sekcja}>
                <h3 style={styles.sekcjaTytul}>Koszty uzyskania przychodu (KUP)</h3>
                <div style={styles.row}>
                  <div style={styles.field}>
                    <label style={styles.label}>KUP standardowe (PLN)</label>
                    <select style={styles.input} value={profil.kupStandardKwota}
                      onChange={e => setProfil({ ...profil, kupStandardKwota: parseFloat(e.target.value) })}>
                      <option value={250}>250 PLN — miejscowy</option>
                      <option value={300}>300 PLN — dojeżdżający</option>
                    </select>
                  </div>
                  <div style={styles.field}>
                    <label style={styles.label}>Kwota zmniejszająca PIT-2 (PLN)</label>
                    <select style={styles.input} value={profil.pit2Kwota}
                      onChange={e => setProfil({ ...profil, pit2Kwota: parseFloat(e.target.value) })}>
                      <option value={300}>300 PLN — jedno źródło dochodu</option>
                      <option value={150}>150 PLN — dwa źródła dochodu</option>
                      <option value={100}>100 PLN — trzy źródła dochodu</option>
                      <option value={0}>0 PLN — brak oświadczenia PIT-2</option>
                    </select>
                  </div>
                </div>

                <div style={styles.field}>
                  <label style={styles.label}>
                    Współczynnik pracy twórczej (KUP 50%) — 0 = brak, 1 = 100% wynagrodzenia
                  </label>
                  <input type="number" style={styles.input}
                    min={0} max={1} step={0.1}
                    value={profil.wspolczynnikAutorskiKup}
                    onChange={e => setProfil({ ...profil, wspolczynnikAutorskiKup: parseFloat(e.target.value) || 0 })} />
                  <small style={styles.hint}>
                    Dotyczy programistów, projektantów, architektów i innych twórców. Limit roczny: 120 000 PLN.
                  </small>
                </div>
              </div>

              {/* PPK */}
              <div style={styles.sekcja}>
                <h3 style={styles.sekcjaTytul}>Pracownicze Plany Kapitałowe (PPK)</h3>
                <div style={styles.row}>
                  <div style={styles.field}>
                    <label style={styles.label}>Stawka PPK pracownika (%)</label>
                    <select style={styles.input}
                      value={profil.ppkStawkaPracownika}
                      onChange={e => setProfil({ ...profil, ppkStawkaPracownika: parseFloat(e.target.value) })}>
                      <option value={0}>0% — wypisany z PPK</option>
                      <option value={0.005}>0,5% — obniżona (niskie dochody)</option>
                      <option value={0.01}>1%</option>
                      <option value={0.015}>1,5%</option>
                      <option value={0.02}>2% — podstawowa (domyślna)</option>
                      <option value={0.025}>2,5%</option>
                      <option value={0.03}>3%</option>
                      <option value={0.035}>3,5%</option>
                      <option value={0.04}>4% — maksymalna</option>
                    </select>
                  </div>
                  <div style={styles.field}>
                    <label style={styles.label}>Stawka PPK pracodawcy (%)</label>
                    <select style={styles.input}
                      value={profil.ppkStawkaPracodawcy}
                      onChange={e => setProfil({ ...profil, ppkStawkaPracodawcy: parseFloat(e.target.value) })}>
                      <option value={0.015}>1,5% — obowiązkowa (domyślna)</option>
                      <option value={0.02}>2%</option>
                      <option value={0.025}>2,5%</option>
                      <option value={0.03}>3%</option>
                      <option value={0.035}>3,5%</option>
                      <option value={0.04}>4% — maksymalna</option>
                    </select>
                  </div>
                </div>
              </div>

              {blad && <p style={styles.blad}>{blad}</p>}
              {komunikat && <p style={styles.sukces}>{komunikat}</p>}

              <button type="submit" disabled={ladowanie}
                style={ladowanie ? styles.btnDisabled : styles.btn}>
                {ladowanie ? 'Zapisywanie...' : '💾 Zapisz profil podatkowy'}
              </button>
            </form>
          </div>
        )}
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
  backBtn: {
    backgroundColor: 'transparent', border: '1px solid white',
    color: 'white', padding: '0.5rem 1rem', borderRadius: '4px', cursor: 'pointer',
  },
  logoutBtn: {
    backgroundColor: 'transparent', border: '1px solid white',
    color: 'white', padding: '0.5rem 1rem', borderRadius: '4px', cursor: 'pointer',
  },
  content: { padding: '2rem', maxWidth: '800px', margin: '0 auto' },
  card: {
    backgroundColor: 'white', padding: '1.5rem', borderRadius: '8px',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)', marginBottom: '1.5rem',
  },
  cardTitle: { marginTop: 0, color: '#1f2937' },
  sekcja: { marginBottom: '1.5rem', paddingBottom: '1.5rem', borderBottom: '1px solid #e5e7eb' },
  sekcjaTytul: { color: '#374151', fontSize: '0.875rem', fontWeight: '600', marginBottom: '1rem' },
  row: { display: 'flex', gap: '1rem' },
  field: { flex: 1, marginBottom: '0.75rem' },
  label: { display: 'block', marginBottom: '0.25rem', fontWeight: '500', color: '#374151', fontSize: '0.875rem' },
  input: {
    width: '100%', padding: '0.5rem', border: '1px solid #d1d5db',
    borderRadius: '4px', boxSizing: 'border-box',
  },
  hint: { color: '#6b7280', fontSize: '0.75rem' },
  btn: {
    padding: '0.75rem 2rem', backgroundColor: '#2563eb', color: 'white',
    border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: '600',
  },
  btnDisabled: {
    padding: '0.75rem 2rem', backgroundColor: '#93c5fd', color: 'white',
    border: 'none', borderRadius: '4px', cursor: 'not-allowed', fontWeight: '600',
  },
  blad: { color: '#dc2626', fontSize: '0.875rem' },
  sukces: { color: '#16a34a', fontSize: '0.875rem' },
};

export default TaxProfileEditor;