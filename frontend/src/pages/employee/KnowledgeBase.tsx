import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const KnowledgeBase: React.FC = () => {
  const navigate = useNavigate();
  const [aktywnaSekcja, setAktywnaSekcja] = useState('zus');

  const handleLogout = () => { localStorage.clear(); navigate('/login'); };

  const sekcje = [
    { id: 'zus', tytul: ' System ZUS' },
    { id: 'skladki', tytul: ' Składki społeczne' },
    { id: 'ppk', tytul: ' PPK' },
    { id: 'emerytura', tytul: ' Emerytura' },
    { id: 'l4', tytul: ' L4 i zasiłki' },
  ];

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h1 style={styles.headerTitle}>Baza wiedzy — Ubezpieczenia społeczne</h1>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button onClick={() => navigate('/employee')} style={styles.backBtn}>
            ← Paski płacowe
          </button>
          <button onClick={handleLogout} style={styles.logoutBtn}>Wyloguj</button>
        </div>
      </div>

      <div style={styles.content}>
        {/* Menu boczne */}
        <div style={styles.menu}>
          {sekcje.map(s => (
            <button key={s.id}
              onClick={() => setAktywnaSekcja(s.id)}
              style={aktywnaSekcja === s.id ? styles.menuItemAktywny : styles.menuItem}>
              {s.tytul}
            </button>
          ))}
        </div>

        {/* Treść */}
        <div style={styles.tresc}>

          {aktywnaSekcja === 'zus' && (
            <div>
              <h2>System ubezpieczeń społecznych w Polsce</h2>
              <p>Zakład Ubezpieczeń Społecznych (ZUS) to państwowa instytucja zarządzająca systemem ubezpieczeń społecznych w Polsce. Każdy pracownik zatrudniony na umowę o pracę podlega obowiązkowemu ubezpieczeniu.</p>

              <h3>Filary systemu emerytalnego</h3>
              <div style={styles.karta}>
                <h4 style={styles.kartaTytul}>I Filar — ZUS (obowiązkowy)</h4>
                <p>Składki trafiają do ZUS i są zapisywane na indywidualnym koncie ubezpieczonego. Kapitał jest corocznie waloryzowany wskaźnikiem publikowanym przez ZUS. Emerytura z I filaru wyliczana jest jako zgromadzony kapitał podzielony przez średnie dalsze trwanie życia.</p>
              </div>
              <div style={styles.karta}>
                <h4 style={styles.kartaTytul}>II Filar — OFE (opcjonalny)</h4>
                <p>Otwarte Fundusze Emerytalne inwestują część składki na rynku kapitałowym. Uczestnictwo jest dobrowolne od 2014 roku.</p>
              </div>
              <div style={styles.karta}>
                <h4 style={styles.kartaTytul}>III Filar — PPK/IKE/IKZE (dobrowolny)</h4>
                <p>Pracownicze Plany Kapitałowe (PPK) to program dobrowolnego oszczędzania na emeryturę, współfinansowany przez pracodawcę i państwo.</p>
              </div>

              <h3>Wiek emerytalny w Polsce (2026)</h3>
              <table style={styles.tabela}>
                <thead>
                  <tr style={styles.thead}>
                    <th style={styles.th}>Płeć</th>
                    <th style={styles.th}>Wiek emerytalny</th>
                    <th style={styles.th}>Min. staż do emerytury minimalnej</th>
                  </tr>
                </thead>
                <tbody>
                  <tr><td style={styles.td}>Kobieta</td><td style={styles.td}>60 lat</td><td style={styles.td}>20 lat</td></tr>
                  <tr><td style={styles.td}>Mężczyzna</td><td style={styles.td}>65 lat</td><td style={styles.td}>25 lat</td></tr>
                </tbody>
              </table>
            </div>
          )}

          {aktywnaSekcja === 'skladki' && (
            <div>
              <h2>Składki na ubezpieczenia społeczne</h2>
              <p>Składki dzielą się na część finansowaną przez pracownika i przez pracodawcę. Podstawą wymiaru jest wynagrodzenie brutto.</p>

              <h3>Składki pracownika (potrącane z wynagrodzenia brutto)</h3>
              <table style={styles.tabela}>
                <thead>
                  <tr style={styles.thead}>
                    <th style={styles.th}>Rodzaj składki</th>
                    <th style={styles.th}>Stawka</th>
                    <th style={styles.th}>Opis</th>
                  </tr>
                </thead>
                <tbody>
                  <tr><td style={styles.td}>Emerytalna</td><td style={styles.td}>9,76%</td><td style={styles.td}>Trafia na konto ZUS — buduje kapitał emerytalny</td></tr>
                  <tr><td style={styles.td}>Rentowa</td><td style={styles.td}>1,50%</td><td style={styles.td}>Finansuje renty z tytułu niezdolności do pracy</td></tr>
                  <tr><td style={styles.td}>Chorobowa</td><td style={styles.td}>2,45%</td><td style={styles.td}>Uprawnia do zasiłku chorobowego</td></tr>
                  <tr><td style={styles.td}>Zdrowotna</td><td style={styles.td}>9,00%</td><td style={styles.td}>Finansuje NFZ — nie wpływa na emeryturę</td></tr>
                  <tr style={{ fontWeight: 'bold', backgroundColor: '#f9fafb' }}>
                    <td style={styles.td}>RAZEM</td><td style={styles.td}>~22,71%</td><td style={styles.td}>Łączne potrącenie z brutto</td>
                  </tr>
                </tbody>
              </table>

              <h3>Składki pracodawcy (nie widoczne na pasku, ale wpływają na koszt zatrudnienia)</h3>
              <table style={styles.tabela}>
                <thead>
                  <tr style={styles.thead}>
                    <th style={styles.th}>Rodzaj składki</th>
                    <th style={styles.th}>Stawka</th>
                  </tr>
                </thead>
                <tbody>
                  <tr><td style={styles.td}>Emerytalna</td><td style={styles.td}>9,76%</td></tr>
                  <tr><td style={styles.td}>Rentowa</td><td style={styles.td}>6,50%</td></tr>
                  <tr><td style={styles.td}>Wypadkowa</td><td style={styles.td}>0,67% – 3,30%</td></tr>
                  <tr><td style={styles.td}>Fundusz Pracy + FS</td><td style={styles.td}>2,45%</td></tr>
                  <tr><td style={styles.td}>FGŚP</td><td style={styles.td}>0,10%</td></tr>
                </tbody>
              </table>

              <div style={styles.infoBox}>
                <strong> Super Brutto</strong> — to całkowity koszt zatrudnienia ponoszony przez pracodawcę. Obejmuje wynagrodzenie brutto plus wszystkie składki pracodawcy. Dla wynagrodzenia 8 500 PLN brutto, Super Brutto wynosi ok. 10 200 PLN.
              </div>
            </div>
          )}

          {aktywnaSekcja === 'ppk' && (
            <div>
              <h2>Pracownicze Plany Kapitałowe (PPK)</h2>
              <p>PPK to dobrowolny program długoterminowego oszczędzania na emeryturę, wprowadzony w Polsce w 2019 roku. Środki są gromadzone na prywatnym rachunku pracownika.</p>

              <h3>Kto finansuje PPK?</h3>
              <table style={styles.tabela}>
                <thead>
                  <tr style={styles.thead}>
                    <th style={styles.th}>Uczestnik</th>
                    <th style={styles.th}>Wpłata podstawowa</th>
                    <th style={styles.th}>Wpłata dobrowolna</th>
                  </tr>
                </thead>
                <tbody>
                  <tr><td style={styles.td}>Pracownik</td><td style={styles.td}>2%</td><td style={styles.td}>do 2%</td></tr>
                  <tr><td style={styles.td}>Pracodawca</td><td style={styles.td}>1,5%</td><td style={styles.td}>do 2,5%</td></tr>
                  <tr><td style={styles.td}>Państwo</td><td style={styles.td}>250 PLN/rok (wpłata powitalna) + 240 PLN/rok</td><td style={styles.td}>—</td></tr>
                </tbody>
              </table>

              <h3>Ważne zasady PPK</h3>
              <ul style={styles.lista}>
                <li>Pracownik jest automatycznie zapisywany do PPK (opt-out — trzeba się wypisać aktywnie)</li>
                <li>Wpłata pracownika obniża wynagrodzenie netto ale jest potrącana PO obliczeniu podatku</li>
                <li>Wpłata pracodawcy stanowi dodatkowy przychód pracownika i powiększa podstawę opodatkowania</li>
                <li>Środki zgromadzone w PPK są prywatną własnością pracownika</li>
                <li>Wypłata bez podatku możliwa po 60. roku życia (przy wypłacie 25% jednorazowo i 75% w ratach)</li>
              </ul>

              <div style={styles.infoBox}>
                <strong> Przykład:</strong> Przy wynagrodzeniu 8 500 PLN brutto, pracownik wpłaca 170 PLN (2%), pracodawca 127,50 PLN (1,5%). Miesięcznie na rachunek PPK trafia 297,50 PLN.
              </div>
            </div>
          )}

          {aktywnaSekcja === 'emerytura' && (
            <div>
              <h2>Jak obliczana jest emerytura?</h2>
              <p>Polski system emerytalny oparty jest na zasadzie zdefiniowanej składki (DC — Defined Contribution). Oznacza to, że wysokość emerytury zależy bezpośrednio od zgromadzonego kapitału.</p>

              <div style={styles.wzorBox}>
                <strong>Wzór na emeryturę:</strong><br />
                E = (Kapitał zgromadzony w ZUS) ÷ (Średnie dalsze trwanie życia w miesiącach)
              </div>

              <h3>Średnie dalsze trwanie życia (tablice GUS 2026)</h3>
              <table style={styles.tabela}>
                <thead>
                  <tr style={styles.thead}>
                    <th style={styles.th}>Płeć</th>
                    <th style={styles.th}>Wiek emerytalny</th>
                    <th style={styles.th}>Średnie dalsze trwanie życia</th>
                  </tr>
                </thead>
                <tbody>
                  <tr><td style={styles.td}>Kobieta</td><td style={styles.td}>60 lat</td><td style={styles.td}>266,4 miesięcy (~22 lata)</td></tr>
                  <tr><td style={styles.td}>Mężczyzna</td><td style={styles.td}>65 lat</td><td style={styles.td}>220,8 miesięcy (~18 lat)</td></tr>
                </tbody>
              </table>

              <h3>Waloryzacja kapitału emerytalnego</h3>
              <p>Każdego roku ZUS waloryzuje zgromadzony kapitał wskaźnikiem waloryzacji, który uwzględnia inflację i wzrost funduszu wynagrodzeń. Historycznie wskaźnik ten wynosił od 3% do 7% rocznie.</p>

              <h3>Emerytura minimalna</h3>
              <p>Jeśli wyliczona emerytura jest niższa od ustawowego minimum, ZUS dopłaca różnicę — ale tylko jeśli spełniony jest wymóg stażowy (20 lat dla kobiet, 25 lat dla mężczyzn). Emerytura minimalna w 2026 roku wynosi <strong>1 780,96 PLN brutto</strong>.</p>

              <div style={styles.infoBox}>
                <strong> Wskazówka:</strong> Im wcześniej zaczniesz odkładać na emeryturę i im dłużej będziesz pracować, tym wyższy kapitał zgromadzisz. Każdy dodatkowy rok pracy znacząco zwiększa przyszłe świadczenie dzięki efektowi procentu składanego.
              </div>
            </div>
          )}

          {aktywnaSekcja === 'l4' && (
            <div>
              <h2>Zwolnienia lekarskie i zasiłki</h2>
              <p>Pracownik zatrudniony na umowę o pracę jest objęty ubezpieczeniem chorobowym, które finansuje świadczenia w czasie niezdolności do pracy.</p>

              <h3>Wynagrodzenie chorobowe vs Zasiłek chorobowy</h3>
              <table style={styles.tabela}>
                <thead>
                  <tr style={styles.thead}>
                    <th style={styles.th}>Okres</th>
                    <th style={styles.th}>Świadczenie</th>
                    <th style={styles.th}>Kto płaci</th>
                    <th style={styles.th}>Wysokość</th>
                  </tr>
                </thead>
                <tbody>
                  <tr><td style={styles.td}>1–33 dzień choroby w roku</td><td style={styles.td}>Wynagrodzenie chorobowe</td><td style={styles.td}>Pracodawca</td><td style={styles.td}>80% podstawy (100% przy chorobie w ciąży)</td></tr>
                  <tr><td style={styles.td}>Od 34. dnia choroby</td><td style={styles.td}>Zasiłek chorobowy</td><td style={styles.td}>ZUS</td><td style={styles.td}>80% podstawy</td></tr>
                  <tr><td style={styles.td}>Opieka nad chorym</td><td style={styles.td}>Zasiłek opiekuńczy</td><td style={styles.td}>ZUS</td><td style={styles.td}>80% podstawy</td></tr>
                </tbody>
              </table>

              <h3>Urlopy rodzicielskie</h3>
              <table style={styles.tabela}>
                <thead>
                  <tr style={styles.thead}>
                    <th style={styles.th}>Urlop</th>
                    <th style={styles.th}>Długość</th>
                    <th style={styles.th}>Zasiłek</th>
                  </tr>
                </thead>
                <tbody>
                  <tr><td style={styles.td}>Macierzyński</td><td style={styles.td}>20 tygodni</td><td style={styles.td}>100% podstawy</td></tr>
                  <tr><td style={styles.td}>Rodzicielski</td><td style={styles.td}>32 tygodnie</td><td style={styles.td}>70% lub 81,5% (długi wniosek)</td></tr>
                  <tr><td style={styles.td}>Ojcowski</td><td style={styles.td}>2 tygodnie</td><td style={styles.td}>100% podstawy</td></tr>
                </tbody>
              </table>

              <div style={styles.infoBox}>
                <strong> Ważne:</strong> Zasiłki z ZUS (chorobowy, macierzyński, opiekuńczy) są zwolnione ze składek społecznych i zdrowotnych. Podlegają jedynie opodatkowaniu PIT.
              </div>
            </div>
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
  content: { display: 'flex', gap: '0', maxWidth: '1200px', margin: '2rem auto' },
  menu: {
    width: '220px', flexShrink: 0, backgroundColor: 'white',
    borderRadius: '8px 0 0 8px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
    padding: '1rem 0', height: 'fit-content',
  },
  menuItem: {
    display: 'block', width: '100%', padding: '0.75rem 1.25rem',
    border: 'none', backgroundColor: 'transparent', textAlign: 'left',
    cursor: 'pointer', color: '#374151', fontSize: '0.875rem',
  },
  menuItemAktywny: {
    display: 'block', width: '100%', padding: '0.75rem 1.25rem',
    border: 'none', backgroundColor: '#f0fdf4', textAlign: 'left',
    cursor: 'pointer', color: '#15803d', fontSize: '0.875rem',
    fontWeight: '600', borderLeft: '3px solid #16a34a',
  },
  tresc: {
    flex: 1, backgroundColor: 'white', padding: '2rem',
    borderRadius: '0 8px 8px 0', boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
  },
  karta: {
    backgroundColor: '#f9fafb', border: '1px solid #e5e7eb',
    borderRadius: '8px', padding: '1rem', marginBottom: '1rem',
  },
  kartaTytul: { margin: '0 0 0.5rem 0', color: '#1f2937' },
  tabela: { width: '100%', borderCollapse: 'collapse', marginBottom: '1.5rem' },
  thead: { backgroundColor: '#f9fafb' },
  th: {
    padding: '0.75rem', textAlign: 'left', fontWeight: '600',
    color: '#374151', borderBottom: '2px solid #e5e7eb', fontSize: '0.875rem',
  },
  td: { padding: '0.75rem', borderBottom: '1px solid #f3f4f6', fontSize: '0.875rem' },
  infoBox: {
    backgroundColor: '#eff6ff', border: '1px solid #bfdbfe',
    borderRadius: '8px', padding: '1rem', marginTop: '1rem', color: '#1e40af',
  },
  wzorBox: {
    backgroundColor: '#f0fdf4', border: '1px solid #bbf7d0',
    borderRadius: '8px', padding: '1rem', marginBottom: '1.5rem',
    fontFamily: 'monospace', color: '#15803d',
  },
  lista: { paddingLeft: '1.5rem', lineHeight: '1.8' },
};

export default KnowledgeBase;