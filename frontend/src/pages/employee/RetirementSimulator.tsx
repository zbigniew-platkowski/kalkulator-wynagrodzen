import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer
} from 'recharts';
import client from '../../api/client';

interface WynikPrognozy {
  latDoEmerytury: number;
  wiekEmerytalny: number;
  metoda1Nominalna: number;
  metoda2Nominalna: number;
  metoda3Nominalna: number;
  metoda1Realna: number;
  metoda2Realna: number;
  metoda3Realna: number;
  maStazDoEmerytury: boolean;
  przyslugujeEmeryturaMinimalna2: boolean;
  przyslugujeEmeryturaMinimalna3: boolean;
  emeryturaMinimalna: number;
  daneWykresu: Array<{
    rok: number;
    metoda1Nominalna: number;
    metoda2Nominalna: number;
    metoda3Nominalna: number;
    metoda1Realna: number;
    metoda2Realna: number;
    metoda3Realna: number;
  }>;
}

const RetirementSimulator: React.FC = () => {
  const navigate = useNavigate();

  // Dane pracownika (zapisywane w bazie)
  const [wiek, setWiek] = useState(0);
  const [staz, setStaz] = useState(0);
  const [kapitalZUS, setKapitalZUS] = useState(0);
  const [plec, setPlec] = useState('M');

  // Max wiek zależy od płci - kobieta max 59, mężczyzna max 64
  const maxWiek = plec === 'K' ? 59 : 64;

  // Parametry symulacji (nie zapisywane)
  const [brutto, setBrutto] = useState(8500);
  const [stopaWaloryzacji, setStopaWaloryzacji] = useState(5);
  const [stopaWzrostu, setStopaWzrostu] = useState(3);
  const [przerwa, setPrzerwa] = useState(0);
  const [etat, setEtat] = useState(100);

  // Stan UI
  const [pokazRealne, setPokazRealne] = useState(false);
  const [wynik, setWynik] = useState<WynikPrognozy | null>(null);
  const [ladowanie, setLadowanie] = useState(false);
  const [zapisywanie, setZapisywanie] = useState(false);
  const [blad, setBlad] = useState('');
  const [komunikatZapis, setKomunikatZapis] = useState('');
  const [daneZapisane, setDaneZapisane] = useState(false);

  useEffect(() => {
    client.get('/api/employee/retirement/profile')
      .then((res: any) => {
        setWiek(res.data.wiekObecny);
        setStaz(res.data.stazPracyLata);
        setKapitalZUS(res.data.kapitalZus);
        setPlec(res.data.plec || 'M');
        if (res.data.wiekObecny > 0 || res.data.stazPracyLata > 0 || res.data.kapitalZus > 0) {
          setDaneZapisane(true);
        }
      })
      .catch(() => {});
  }, []);

  const zapiszDane = async () => {
    setZapisywanie(true);
    setKomunikatZapis('');
    try {
      await client.put('/api/employee/retirement/profile', {
        wiekObecny: wiek,
        stazPracyLata: staz,
        kapitalZus: kapitalZUS,
      });
      setKomunikatZapis('Dane zostały zapisane.');
      setDaneZapisane(true);
    } catch {
      setKomunikatZapis('Błąd podczas zapisywania.');
    } finally {
      setZapisywanie(false);
    }
  };

  const oblicz = async () => {
    if (wiek === 0) {
      setBlad('Podaj swój wiek przed obliczeniem prognozy.');
      return;
    }
    setLadowanie(true);
    setBlad('');
    try {
      const res = await client.post('/api/employee/retirement/calculate', {
        wiekObecny: wiek,
        stazPracyLata: staz,
        miesieczneBrutto: brutto,
        kapitalZUS: kapitalZUS,
        stopaWaloryzacji: stopaWaloryzacji / 100,
        stopaWzrostuWynagrodzen: stopaWzrostu / 100,
        przerwaCourierLata: przerwa,
        wymiarEtatu: etat / 100,
      });
      setWynik(res.data);
    } catch {
      setBlad('Błąd podczas obliczania prognozy.');
    } finally {
      setLadowanie(false);
    }
  };

  const fmt = (v: number) => `${v.toLocaleString('pl-PL')} PLN`;

  const daneWykresu = wynik?.daneWykresu.map(d => ({
    rok: d.rok,
    'Brak pracy': pokazRealne ? d.metoda1Realna : d.metoda1Nominalna,
    'Stała pensja': pokazRealne ? d.metoda2Realna : d.metoda2Nominalna,
    'Rosnąca pensja': pokazRealne ? d.metoda3Realna : d.metoda3Nominalna,
  })) ?? [];

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h1 style={styles.headerTitle}>Symulator Emerytalny</h1>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button onClick={() => navigate('/employee')} style={styles.backBtn}>
            ← Paski płacowe
          </button>
          <button onClick={() => { localStorage.clear(); navigate('/login'); }} style={styles.logoutBtn}>
            Wyloguj
          </button>
        </div>
      </div>

      <div style={styles.content}>
        <div style={styles.panel}>

          <div style={styles.sekcjaDanych}>
            <h2 style={{ ...styles.panelTitle, marginBottom: '0.25rem' }}>Twoje dane</h2>
            <p style={styles.hint}>
              {daneZapisane
                ? ' Dane zapisane w systemie'
                : ' Uzupełnij i zapisz swoje dane'}
            </p>
            <p style={{ ...styles.hint, color: '#6b7280' }}>
              Płeć: {plec === 'K' ? 'Kobieta (wiek emerytalny: 60 lat)' : 'Mężczyzna (wiek emerytalny: 65 lat)'}
            </p>

            <div style={styles.field}>
              <label style={styles.label}>
                Obecny wiek: {wiek > 0 ? `${wiek} lat` : 'nie podano'}
              </label>
              <input type="range" min={15} max={maxWiek} value={wiek}
                onChange={e => { setWiek(parseInt(e.target.value)); setDaneZapisane(false); }}
                style={styles.slider} />
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', color: '#6b7280' }}>
                <span>15</span><span>{maxWiek}</span>
              </div>
            </div>

            <div style={styles.field}>
              <label style={styles.label}>Staż pracy: {staz} lat</label>
              <input type="range" min={0} max={45} value={staz}
                onChange={e => { setStaz(parseInt(e.target.value)); setDaneZapisane(false); }}
                style={styles.slider} />
            </div>

            <div style={styles.field}>
              <label style={styles.label}>Kapitał zgromadzony w ZUS (PLN)</label>
              <input type="number" value={kapitalZUS}
                onChange={e => { setKapitalZUS(parseFloat(e.target.value) || 0); setDaneZapisane(false); }}
                style={styles.input} placeholder="0" />
              <small style={styles.smallHint}>Sprawdź na eZUS.gov.pl</small>
            </div>

            {komunikatZapis && <p style={styles.komunikatZapis}>{komunikatZapis}</p>}

            <button onClick={zapiszDane} disabled={zapisywanie || wiek === 0}
              style={wiek === 0 ? styles.btnZapisDisabled : styles.btnZapis}>
              {zapisywanie ? 'Zapisywanie...' : ' Zapisz moje dane'}
            </button>
            {wiek === 0 && (
              <small style={{ color: '#f59e0b', fontSize: '0.75rem' }}>
                Ustaw wiek żeby zapisać dane
              </small>
            )}
          </div>

          <div style={styles.field}>
            <label style={styles.label}>Miesięczne brutto (PLN)</label>
            <input type="number" value={brutto}
              onChange={e => setBrutto(parseFloat(e.target.value) || 0)}
              style={styles.input} />
          </div>

          <h3 style={styles.sectionTitle}>Parametry symulacji</h3>

          <div style={styles.field}>
            <label style={styles.label}>Stopa waloryzacji ZUS: {stopaWaloryzacji}%</label>
            <input type="range" min={1} max={10} value={stopaWaloryzacji}
              onChange={e => setStopaWaloryzacji(parseInt(e.target.value))}
              style={styles.slider} />
          </div>

          <div style={styles.field}>
            <label style={styles.label}>Wzrost wynagrodzenia: {stopaWzrostu}%</label>
            <input type="range" min={0} max={10} value={stopaWzrostu}
              onChange={e => setStopaWzrostu(parseInt(e.target.value))}
              style={styles.slider} />
          </div>

          <h3 style={styles.sectionTitle}>Symulacja "Co jeśli?"</h3>

          <div style={styles.field}>
            <label style={styles.label}>
              Przerwa w karierze: {przerwa} {przerwa === 1 ? 'rok' : przerwa < 5 ? 'lata' : 'lat'}
            </label>
            <input type="range" min={0} max={10} value={przerwa}
              onChange={e => setPrzerwa(parseInt(e.target.value))}
              style={styles.slider} />
          </div>

          <div style={styles.field}>
            <label style={styles.label}>Wymiar etatu: {etat}%</label>
            <input type="range" min={25} max={100} step={25} value={etat}
              onChange={e => setEtat(parseInt(e.target.value))}
              style={styles.slider} />
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', color: '#6b7280' }}>
              <span>1/4</span><span>1/2</span><span>3/4</span><span>Pełny</span>
            </div>
          </div>

          {blad && <p style={styles.blad}>{blad}</p>}

          <button onClick={oblicz} disabled={ladowanie || wiek === 0}
            style={ladowanie || wiek === 0 ? styles.btnDisabled : styles.btn}>
            {ladowanie ? 'Obliczam...' : ' Oblicz prognozę'}
          </button>
        </div>

        <div style={styles.wyniki}>
          {!wynik && !ladowanie && (
            <div style={styles.placeholder}>
              <p style={{ fontSize: '3rem' }}></p>
              {wiek === 0
                ? <p>Najpierw podaj swój wiek w sekcji "Twoje dane"</p>
                : <p>Ustaw parametry i kliknij "Oblicz prognozę"</p>
              }
            </div>
          )}

          {wynik && (
            <>
              {!wynik.maStazDoEmerytury && (
                <div style={styles.warning}>
                   Przy stażu {staz} lat nie spełniasz wymogu stażowego do emerytury minimalnej.
                </div>
              )}

              <div style={styles.karty}>
                <div style={{ ...styles.karta, borderColor: '#ef4444' }}>
                  <div style={styles.kartaTytul}>Scenariusz 1</div>
                  <div style={styles.kartaPodtytul}>Brak dalszej pracy</div>
                  <div style={styles.kartaKwota}>{fmt(wynik.metoda1Realna)}</div>
                  <div style={styles.kartaNominalna}>Nominalnie: {fmt(wynik.metoda1Nominalna)}</div>
                </div>

                <div style={{ ...styles.karta, borderColor: '#f59e0b' }}>
                  <div style={styles.kartaTytul}>Scenariusz 2</div>
                  <div style={styles.kartaPodtytul}>Stała pensja</div>
                  <div style={styles.kartaKwota}>{fmt(wynik.metoda2Realna)}</div>
                  <div style={styles.kartaNominalna}>Nominalnie: {fmt(wynik.metoda2Nominalna)}</div>
                  {wynik.przyslugujeEmeryturaMinimalna2 && (
                    <div style={styles.kartaMin}>+ dopłata do min. {fmt(wynik.emeryturaMinimalna)}</div>
                  )}
                </div>

                <div style={{ ...styles.karta, borderColor: '#16a34a' }}>
                  <div style={styles.kartaTytul}>Scenariusz 3</div>
                  <div style={styles.kartaPodtytul}>Rosnąca pensja ({stopaWzrostu}%/rok)</div>
                  <div style={styles.kartaKwota}>{fmt(wynik.metoda3Realna)}</div>
                  <div style={styles.kartaNominalna}>Nominalnie: {fmt(wynik.metoda3Nominalna)}</div>
                  {wynik.przyslugujeEmeryturaMinimalna3 && (
                    <div style={styles.kartaMin}>+ dopłata do min. {fmt(wynik.emeryturaMinimalna)}</div>
                  )}
                </div>
              </div>

              <p style={styles.info}>
                Kwoty realne w dzisiejszej sile nabywczej (inflacja 2,5%/rok).
                Czas do emerytury: {wynik.latDoEmerytury} lat (wiek emerytalny: {wynik.wiekEmerytalny} lat).
              </p>

              <div style={styles.toggle}>
                <button onClick={() => setPokazRealne(false)}
                  style={!pokazRealne ? styles.toggleActive : styles.toggleInactive}>
                  Nominalne
                </button>
                <button onClick={() => setPokazRealne(true)}
                  style={pokazRealne ? styles.toggleActive : styles.toggleInactive}>
                  Realne
                </button>
              </div>

              <div style={styles.wykres}>
                <ResponsiveContainer width="100%" height={350}>
                  <LineChart data={daneWykresu}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="rok" />
                    <YAxis tickFormatter={v => `${(v / 1000).toFixed(0)}k`} />
                    <Tooltip formatter={(v: number) => fmt(v)} />
                    <Legend />
                    <Line type="monotone" dataKey="Brak pracy"
                      stroke="#ef4444" strokeWidth={2} dot={false} />
                    <Line type="monotone" dataKey="Stała pensja"
                      stroke="#f59e0b" strokeWidth={2} dot={false} />
                    <Line type="monotone" dataKey="Rosnąca pensja"
                      stroke="#16a34a" strokeWidth={2} dot={false} />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

const styles: Record<string, React.CSSProperties> = {
  container: { minHeight: '100vh', backgroundColor: '#f3f4f6' },
  header: {
    backgroundColor: '#16a34a', color: 'white', padding: '1rem 2rem',
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
  content: { display: 'flex', gap: '1.5rem', padding: '2rem', maxWidth: '1400px', margin: '0 auto' },
  panel: {
    width: '320px', flexShrink: 0, backgroundColor: 'white',
    padding: '1.5rem', borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
    height: 'fit-content',
  },
  sekcjaDanych: {
    backgroundColor: '#f0fdf4', border: '1px solid #bbf7d0',
    borderRadius: '8px', padding: '1rem', marginBottom: '1rem',
  },
  panelTitle: { marginTop: 0, color: '#1f2937' },
  sectionTitle: {
    color: '#374151', borderTop: '1px solid #e5e7eb',
    paddingTop: '1rem', marginTop: '1rem', fontSize: '0.875rem', fontWeight: '600',
  },
  field: { marginBottom: '1rem' },
  label: { display: 'block', marginBottom: '0.25rem', fontWeight: '500', color: '#374151', fontSize: '0.875rem' },
  slider: { width: '100%', marginTop: '0.25rem' },
  input: {
    width: '100%', padding: '0.5rem', border: '1px solid #d1d5db',
    borderRadius: '4px', boxSizing: 'border-box',
  },
  hint: { fontSize: '0.75rem', color: '#6b7280', margin: '0 0 0.5rem 0' },
  smallHint: { color: '#6b7280', fontSize: '0.75rem' },
  komunikatZapis: { fontSize: '0.875rem', margin: '0.5rem 0' },
  btnZapis: {
    width: '100%', padding: '0.5rem', backgroundColor: '#15803d',
    color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer',
    fontSize: '0.875rem', marginBottom: '0.25rem',
  },
  btnZapisDisabled: {
    width: '100%', padding: '0.5rem', backgroundColor: '#86efac',
    color: 'white', border: 'none', borderRadius: '4px', cursor: 'not-allowed',
    fontSize: '0.875rem', marginBottom: '0.25rem',
  },
  btn: {
    width: '100%', padding: '0.75rem', backgroundColor: '#16a34a',
    color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer',
    fontWeight: '600', marginTop: '0.5rem',
  },
  btnDisabled: {
    width: '100%', padding: '0.75rem', backgroundColor: '#86efac',
    color: 'white', border: 'none', borderRadius: '4px', cursor: 'not-allowed',
    fontWeight: '600', marginTop: '0.5rem',
  },
  wyniki: { flex: 1 },
  placeholder: {
    textAlign: 'center', color: '#9ca3af', padding: '4rem',
    backgroundColor: 'white', borderRadius: '8px',
  },
  warning: {
    backgroundColor: '#fef3c7', border: '1px solid #f59e0b',
    padding: '0.75rem 1rem', borderRadius: '8px', marginBottom: '1rem', color: '#92400e',
  },
  karty: { display: 'flex', gap: '1rem', marginBottom: '1rem' },
  karta: {
    flex: 1, backgroundColor: 'white', padding: '1rem', borderRadius: '8px',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)', borderTop: '4px solid',
  },
  kartaTytul: { fontWeight: '600', color: '#374151', fontSize: '0.875rem' },
  kartaPodtytul: { color: '#6b7280', fontSize: '0.75rem', marginBottom: '0.5rem' },
  kartaKwota: { fontSize: '1.5rem', fontWeight: 'bold', color: '#1f2937' },
  kartaNominalna: { fontSize: '0.75rem', color: '#6b7280', marginTop: '0.25rem' },
  kartaMin: { fontSize: '0.75rem', color: '#16a34a', marginTop: '0.25rem', fontWeight: '600' },
  info: { color: '#6b7280', fontSize: '0.875rem', marginBottom: '1rem' },
  toggle: {
    display: 'flex', marginBottom: '1rem', border: '1px solid #d1d5db',
    borderRadius: '4px', overflow: 'hidden', width: 'fit-content',
  },
  toggleActive: { padding: '0.5rem 1rem', backgroundColor: '#16a34a', color: 'white', border: 'none', cursor: 'pointer' },
  toggleInactive: { padding: '0.5rem 1rem', backgroundColor: 'white', color: '#374151', border: 'none', cursor: 'pointer' },
  wykres: { backgroundColor: 'white', padding: '1.5rem', borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)' },
  blad: { color: '#dc2626', fontSize: '0.875rem' },
};

export default RetirementSimulator;