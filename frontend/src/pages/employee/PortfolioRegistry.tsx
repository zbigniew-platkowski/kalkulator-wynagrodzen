import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import client from '../../api/client';

interface WpisPortfela {
  pensjaId: number;
  miesiac: number;
  rok: number;
  wynagrodzenieBrutto: number;
  skladkaEmerytalnaPracownik: number;
  skladkaEmerytalnaPracodawca: number;
  lacznaSkladkaEmerytalna: number;
  premia: number;
  nadgodziny: number;
}

const PortfolioRegistry: React.FC = () => {
  const navigate = useNavigate();
  const [wpisy, setWpisy] = useState<WpisPortfela[]>([]);
  const [blad, setBlad] = useState('');
  const [sumaLaczna, setSumaLaczna] = useState(0);

  const nazwyMiesiecy = ['', 'Styczeń', 'Luty', 'Marzec', 'Kwiecień', 'Maj', 'Czerwiec',
    'Lipiec', 'Sierpień', 'Wrzesień', 'Październik', 'Listopad', 'Grudzień'];

  useEffect(() => {
    client.get('/api/employee/portfolio')
      .then((res: any) => {
        setWpisy(res.data.wpisy);
        setSumaLaczna(res.data.sumaLaczna);
      })
      .catch(() => setBlad('Nie można pobrać rejestru zasileń.'));
  }, []);

  const handleLogout = () => { localStorage.clear(); navigate('/login'); };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h1 style={styles.headerTitle}>Rejestr Zasileń Portfela Emerytalnego</h1>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button onClick={() => navigate('/employee')} style={styles.backBtn}>
            ← Paski płacowe
          </button>
          <button onClick={handleLogout} style={styles.logoutBtn}>Wyloguj</button>
        </div>
      </div>

      <div style={styles.content}>
        {/* Podsumowanie */}
        <div style={styles.podsumowanie}>
          <div style={styles.kartaSum}>
            <div style={styles.kartaSumTytul}>Łączne składki emerytalne odprowadzone przez system</div>
            <div style={styles.kartaSumKwota}>{sumaLaczna.toFixed(2)} PLN</div>
            <div style={styles.kartaSumOpis}>Suma składek emerytalnych pracownika i pracodawcy od początku zatrudnienia w systemie</div>
          </div>
          <div style={styles.infoBox}>
            <strong> Jak to działa?</strong> Każdego miesiąca 19,52% Twojego wynagrodzenia brutto (9,76% Twoja część + 9,76% pracodawcy) trafia na Twoje konto w ZUS i buduje kapitał emerytalny. Składki są corocznie waloryzowane.
          </div>
        </div>

        {/* Tabela zasileń */}
        {blad && <p style={styles.blad}>{blad}</p>}
        {wpisy.length === 0 && !blad && (
          <p style={styles.info}>Brak danych — HR nie wprowadził jeszcze żadnych pasków.</p>
        )}

        {wpisy.length > 0 && (
          <table style={styles.tabela}>
            <thead>
              <tr style={styles.thead}>
                <th style={styles.th}>Okres</th>
                <th style={styles.th}>Brutto</th>
                <th style={styles.th}>Składka emerytalna (Ty)</th>
                <th style={styles.th}>Składka emerytalna (pracodawca)</th>
                <th style={styles.th}>Łączne zasilenie ZUS</th>
                <th style={styles.th}>Wpływ premii/nadgodzin</th>
              </tr>
            </thead>
            <tbody>
              {wpisy.map(w => (
                <tr key={w.pensjaId} style={styles.tr}>
                  <td style={styles.td}>{nazwyMiesiecy[w.miesiac]} {w.rok}</td>
                  <td style={styles.td}>{w.wynagrodzenieBrutto.toFixed(2)} PLN</td>
                  <td style={styles.td}>{w.skladkaEmerytalnaPracownik.toFixed(2)} PLN</td>
                  <td style={styles.td}>{w.skladkaEmerytalnaPracodawca.toFixed(2)} PLN</td>
                  <td style={{ ...styles.td, fontWeight: 'bold', color: '#15803d' }}>
                    {w.lacznaSkladkaEmerytalna.toFixed(2)} PLN
                  </td>
                  <td style={styles.td}>
                    {(w.premia + w.nadgodziny) > 0
                      ? <span style={{ color: '#16a34a' }}>+{(w.premia + w.nadgodziny).toFixed(2)} PLN</span>
                      : <span style={{ color: '#9ca3af' }}>—</span>
                    }
                  </td>
                </tr>
              ))}
              <tr style={{ backgroundColor: '#f0fdf4', fontWeight: 'bold' }}>
                <td style={styles.td}>SUMA</td>
                <td style={styles.td}>—</td>
                <td style={styles.td}>{wpisy.reduce((s, w) => s + w.skladkaEmerytalnaPracownik, 0).toFixed(2)} PLN</td>
                <td style={styles.td}>{wpisy.reduce((s, w) => s + w.skladkaEmerytalnaPracodawca, 0).toFixed(2)} PLN</td>
                <td style={{ ...styles.td, color: '#15803d' }}>{sumaLaczna.toFixed(2)} PLN</td>
                <td style={styles.td}>—</td>
              </tr>
            </tbody>
          </table>
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
  backBtn: {
    backgroundColor: 'transparent', border: '1px solid white',
    color: 'white', padding: '0.5rem 1rem', borderRadius: '4px', cursor: 'pointer',
  },
  logoutBtn: {
    backgroundColor: 'transparent', border: '1px solid white',
    color: 'white', padding: '0.5rem 1rem', borderRadius: '4px', cursor: 'pointer',
  },
  content: { padding: '2rem', maxWidth: '1200px', margin: '0 auto' },
  podsumowanie: { display: 'flex', gap: '1.5rem', marginBottom: '2rem', flexWrap: 'wrap' },
  kartaSum: {
    backgroundColor: 'white', padding: '1.5rem', borderRadius: '8px',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)', borderTop: '4px solid #16a34a', flex: '1',
  },
  kartaSumTytul: { fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.5rem' },
  kartaSumKwota: { fontSize: '2rem', fontWeight: 'bold', color: '#15803d', marginBottom: '0.5rem' },
  kartaSumOpis: { fontSize: '0.75rem', color: '#9ca3af' },
  infoBox: {
    backgroundColor: '#eff6ff', border: '1px solid #bfdbfe',
    borderRadius: '8px', padding: '1rem', color: '#1e40af', flex: '1',
  },
  tabela: {
    width: '100%', borderCollapse: 'collapse', backgroundColor: 'white',
    borderRadius: '8px', overflow: 'hidden', boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
  },
  thead: { backgroundColor: '#f9fafb' },
  th: {
    padding: '0.75rem 1rem', textAlign: 'left', fontWeight: '600',
    color: '#374151', borderBottom: '1px solid #e5e7eb', fontSize: '0.875rem',
  },
  tr: { borderBottom: '1px solid #f3f4f6' },
  td: { padding: '0.75rem 1rem', color: '#374151', fontSize: '0.875rem' },
  blad: { color: '#dc2626' },
  info: { color: '#6b7280' },
};

export default PortfolioRegistry;