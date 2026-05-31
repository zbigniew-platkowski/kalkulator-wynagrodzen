import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import client from '../../api/client';
import useIdleTimeout from '../../useIdleTimeout';

interface Pasek {
  pensjaId: number;
  miesiac: number;
  rok: number;
  brutto: number;
  netto: number;
  superBrutto: number;
}

interface SzczegolyPaska {
  id: number;
  miesiac: number;
  rok: number;
  daneWejsciowe: any;
  potraceniaPracownik: any;
  wynagrodzenieNetto: number;
  kosztyPracodawcy: any;
}

const SalarySlip: React.FC = () => {
  const navigate = useNavigate();
  useIdleTimeout(15);

  const [paski, setPaski] = useState<Pasek[]>([]);
  const [wybranyPasek, setWybranyPasek] = useState<SzczegolyPaska | null>(null);
  const [ukryjKwoty, setUkryjKwoty] = useState(false);
  const [blad, setBlad] = useState('');

  useEffect(() => {
    client.get('/api/employee/salary')
      .then((res: any) => setPaski(res.data))
      .catch(() => setBlad('Nie można pobrać listy pasków.'));
  }, []);

  const pobierzSzczegoly = async (pensjaId: number) => {
    try {
      const res = await client.get(`/api/employee/salary/${pensjaId}`);
      setWybranyPasek(res.data);
    } catch {
      setBlad('Nie można pobrać szczegółów paska.');
    }
  };

  const kwota = (value: number) =>
    ukryjKwoty ? '***' : `${value.toFixed(2)} PLN`;

  const handleLogout = () => {
    localStorage.clear();
    navigate('/login');
  };

  const nazwyMiesiecy = ['', 'Styczeń', 'Luty', 'Marzec', 'Kwiecień', 'Maj', 'Czerwiec',
    'Lipiec', 'Sierpień', 'Wrzesień', 'Październik', 'Listopad', 'Grudzień'];

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h1 style={styles.headerTitle}>Panel Pracownika</h1>
        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <button onClick={() => setUkryjKwoty(!ukryjKwoty)} style={styles.maskBtn}>
            {ukryjKwoty ? ' Pokaż kwoty' : ' Ukryj kwoty'}
          </button>
          <button onClick={() => navigate('/employee/retirement')} style={styles.maskBtn}>
             Symulator emerytalny
          </button>
          <button onClick={() => navigate('/employee/portfolio')} style={styles.maskBtn}>
             Portfel emerytalny
          </button>
          <button onClick={() => navigate('/employee/knowledge')} style={styles.maskBtn}>
             Baza wiedzy
          </button>
          <button onClick={handleLogout} style={styles.logoutBtn}>Wyloguj</button>
        </div>
      </div>

      <div style={styles.content}>
        <div style={styles.lista}>
          <h2 style={styles.sectionTitle}>Historia pasków</h2>
          {blad && <p style={styles.blad}>{blad}</p>}
          {paski.length === 0 && !blad && (
            <p style={styles.info}>Brak pasków płacowych.</p>
          )}
          {paski.map(p => (
            <div
              key={p.pensjaId}
              style={{
                ...styles.pasekItem,
                ...(wybranyPasek?.id === p.pensjaId ? styles.pasekItemAktywny : {})
              }}
              onClick={() => pobierzSzczegoly(p.pensjaId)}
            >
              <div style={styles.pasekNazwa}>
                {nazwyMiesiecy[p.miesiac]} {p.rok}
              </div>
              <div style={styles.pasekKwoty}>
                <span style={styles.nettoLabel}>Netto: </span>
                <span style={styles.nettoKwota}>{kwota(p.netto)}</span>
              </div>
            </div>
          ))}
        </div>

        {wybranyPasek && (
          <div style={styles.szczegoly}>
            <h2 style={styles.sectionTitle}>
              Szczegółowy pasek — {nazwyMiesiecy[wybranyPasek.miesiac]} {wybranyPasek.rok}
            </h2>

            <div style={styles.sekcja}>
              <h3 style={styles.sekcjaTytul}>Twoje wynagrodzenie (Przychody)</h3>
              <div style={styles.wiersz}>
                <span>Wynagrodzenie zasadnicze</span>
                <span>{kwota(wybranyPasek.daneWejsciowe.wynagrodzenieZasadnicze)}</span>
              </div>
              {wybranyPasek.daneWejsciowe.premia > 0 && (
                <div style={styles.wiersz}>
                  <span>Premia</span>
                  <span>{kwota(wybranyPasek.daneWejsciowe.premia)}</span>
                </div>
              )}
              {wybranyPasek.daneWejsciowe.wynagrodzenieChoroboweFirma > 0 && (
                <div style={styles.wiersz}>
                  <span>Wynagrodzenie chorobowe</span>
                  <span>{kwota(wybranyPasek.daneWejsciowe.wynagrodzenieChoroboweFirma)}</span>
                </div>
              )}
            </div>

            <div style={styles.sekcja}>
              <h3 style={styles.sekcjaTytul}>Potrącenia i składki</h3>
              <div style={styles.wiersz}>
                <span>Składka emerytalna (9,76%)</span>
                <span style={styles.potracenieKwota}>-{kwota(wybranyPasek.potraceniaPracownik.skladkaEmerytalna)}</span>
              </div>
              <div style={styles.wiersz}>
                <span>Składka rentowa (1,50%)</span>
                <span style={styles.potracenieKwota}>-{kwota(wybranyPasek.potraceniaPracownik.skladkaRentowa)}</span>
              </div>
              <div style={styles.wiersz}>
                <span>Składka chorobowa (2,45%)</span>
                <span style={styles.potracenieKwota}>-{kwota(wybranyPasek.potraceniaPracownik.skladkaChorobowa)}</span>
              </div>
              <div style={styles.wiersz}>
                <span>Ubezpieczenie zdrowotne (9,00%)</span>
                <span style={styles.potracenieKwota}>-{kwota(wybranyPasek.potraceniaPracownik.skladkaZdrowotna)}</span>
              </div>
              <div style={styles.wiersz}>
                <span>Zaliczka PIT</span>
                <span style={styles.potracenieKwota}>-{kwota(wybranyPasek.potraceniaPracownik.zaliczkaPit)}</span>
              </div>
              {wybranyPasek.potraceniaPracownik.ppkPracownik > 0 && (
                <div style={styles.wiersz}>
                  <span>PPK (pracownik)</span>
                  <span style={styles.potracenieKwota}>-{kwota(wybranyPasek.potraceniaPracownik.ppkPracownik)}</span>
                </div>
              )}
            </div>

            <div style={styles.nettoBox}>
              <span style={styles.nettoBoxLabel}>Do wypłaty (NETTO)</span>
              <span style={styles.nettoBoxKwota}>{kwota(wybranyPasek.wynagrodzenieNetto)}</span>
            </div>

            <div style={styles.sekcja}>
              <h3 style={styles.sekcjaTytul}>Koszty pracodawcy (Super Brutto)</h3>
              <div style={styles.wiersz}>
                <span>Składka emerytalna pracodawcy</span>
                <span>{kwota(wybranyPasek.kosztyPracodawcy.skladkaEmerytalna)}</span>
              </div>
              <div style={styles.wiersz}>
                <span>Składka rentowa pracodawcy</span>
                <span>{kwota(wybranyPasek.kosztyPracodawcy.skladkaRentowa)}</span>
              </div>
              <div style={styles.wiersz}>
                <span>Składka wypadkowa</span>
                <span>{kwota(wybranyPasek.kosztyPracodawcy.skladkaWypadkowa)}</span>
              </div>
              <div style={styles.wiersz}>
                <span>Fundusz Pracy</span>
                <span>{kwota(wybranyPasek.kosztyPracodawcy.funduszPracy)}</span>
              </div>
              <div style={{ ...styles.wiersz, fontWeight: 'bold', borderTop: '2px solid #e5e7eb', paddingTop: '0.5rem', marginTop: '0.5rem' }}>
                <span>SUPER BRUTTO (całkowity koszt)</span>
                <span>{kwota(wybranyPasek.kosztyPracodawcy.superBrutto)}</span>
              </div>
            </div>
          </div>
        )}
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
  logoutBtn: {
    backgroundColor: 'transparent', border: '1px solid white',
    color: 'white', padding: '0.5rem 1rem', borderRadius: '4px', cursor: 'pointer',
  },
  maskBtn: {
    backgroundColor: 'transparent', border: '1px solid white',
    color: 'white', padding: '0.5rem 1rem', borderRadius: '4px', cursor: 'pointer',
  },
  content: { display: 'flex', gap: '1.5rem', padding: '2rem', maxWidth: '1200px', margin: '0 auto' },
  lista: { width: '280px', flexShrink: 0 },
  szczegoly: {
    flex: 1, backgroundColor: 'white', padding: '1.5rem',
    borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
  },
  sectionTitle: { marginTop: 0, color: '#1f2937' },
  pasekItem: {
    backgroundColor: 'white', padding: '1rem', borderRadius: '8px',
    marginBottom: '0.5rem', cursor: 'pointer', boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    border: '2px solid transparent',
  },
  pasekItemAktywny: { border: '2px solid #16a34a' },
  pasekNazwa: { fontWeight: '600', color: '#1f2937', marginBottom: '0.25rem' },
  pasekKwoty: { fontSize: '0.875rem', color: '#6b7280' },
  nettoLabel: {},
  nettoKwota: { fontWeight: 'bold', color: '#16a34a' },
  sekcja: { marginBottom: '1.5rem' },
  sekcjaTytul: { fontSize: '0.875rem', fontWeight: '600', color: '#6b7280', textTransform: 'uppercase', marginBottom: '0.75rem' },
  wiersz: {
    display: 'flex', justifyContent: 'space-between',
    padding: '0.4rem 0', borderBottom: '1px solid #f3f4f6',
  },
  potracenieKwota: { color: '#dc2626' },
  nettoBox: {
    backgroundColor: '#f0fdf4', border: '2px solid #16a34a', borderRadius: '8px',
    padding: '1rem 1.5rem', display: 'flex', justifyContent: 'space-between',
    alignItems: 'center', marginBottom: '1.5rem',
  },
  nettoBoxLabel: { fontWeight: '600', fontSize: '1.1rem', color: '#15803d' },
  nettoBoxKwota: { fontWeight: 'bold', fontSize: '1.5rem', color: '#15803d' },
  blad: { color: '#dc2626' },
  info: { color: '#6b7280' },
};

export default SalarySlip;